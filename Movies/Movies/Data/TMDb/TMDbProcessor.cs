using FFImageLoading.Helpers.Exif;
using Movies.Models;
using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public abstract class TMDbProcessor : AsyncGroupEventProcessor<DatastoreKeyValueReadArgs<Uri>, DatastoreKeyValueReadArgs<Uri>>
    {
        public TMDbResolver Resolver { get; }

        public TMDbProcessor(IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>> processor, TMDbResolver resolver) : base(new TrojanProcessor(processor, resolver))
        {
            Resolver = resolver;
        }

        protected override bool Handle(DatastoreKeyValueReadArgs<Uri> grouped, IEnumerable<DatastoreKeyValueReadArgs<Uri>> singles)
        {
            if (false == (grouped.Key as TMDbResolver.TrojanTMDbUri)?.Converter is HttpResourceCollectionConverter resources)
            {
                return grouped.IsHandled;
            }

            if ((grouped.Response as RestResponse)?.TryGetRepresentation<IEnumerable<KeyValuePair<Uri, object>>>(out var collection) != true)
            {
                collection = grouped.Response.RawValue as IEnumerable<KeyValuePair<Uri, object>>;
            }

            var index = collection?.ToReadOnlyDictionary() ?? new Dictionary<Uri, object>();
            var result = true;

            foreach (var request in singles)
            {
                var response = grouped.Response;

                if (collection.TryGetValue(request.Key, out var value))
                {
                    if (value is IEnumerable<Entity> entities)
                    {
                        IEnumerable<KeyValuePair<string, IEnumerable<string>>> controlData;
                        IEnumerable<KeyValuePair<string, string>> metadata;

                        if (response is RestResponse restResponse)
                        {
                            controlData = restResponse.ControlData;
                            metadata = restResponse.Metadata;
                        }
                        else
                        {
                            controlData = null;
                            metadata = null;
                        }

                        response = new RestResponse(entities, controlData, metadata)
                        {
                            Expected = request.Expected
                        };
                    }
                    else
                    {
                        response = new DatastoreResponse<object>(value);
                    }
                }

                result &= request.Handle(response);
            }

            return result;
        }

        protected override IEnumerable<KeyValuePair<DatastoreKeyValueReadArgs<Uri>, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> GroupRequests(IEnumerable<DatastoreKeyValueReadArgs<Uri>> args)
        {
            var item = args.Select(arg => arg.Key).OfType<UniformItemIdentifier>().Select(uii => uii.Item).FirstOrDefault(item => item != null);

            foreach (var kvp in GroupUrls(args))
            {
                var url = kvp.Key;
                var requests = kvp.Value.ToArray();
                var uri = GetUri(item, url, requests);
                IEnumerable<DatastoreKeyValueReadArgs<Uri>> grouped = requests;

                if (uri.Converter is HttpResourceCollectionConverter converter && args is BulkEventArgs<DatastoreKeyValueReadArgs<Uri>> batch)
                {
                    var index = requests.ToDictionary(req => req.Key, req => req);

                    foreach (var resource in converter.Resources)
                    {
                        if (!index.ContainsKey(resource))
                        {
                            var request = new DatastoreKeyValueReadArgs<Uri>(resource);

                            index.Add(resource, request);
                            batch.Add(request);
                        }
                    }

                    grouped = index.Values;
                }

                var groupRequest = new RestRequestArgs(uri);
                if (AllSameEtag(requests, out var etag))
                {
                    groupRequest.ControlData[REpresentationalStateTransfer.Rest.IF_NONE_MATCH] = new List<string> { etag };
                }

                yield return new KeyValuePair<DatastoreKeyValueReadArgs<Uri>, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(groupRequest, grouped);
            }
        }

        private static bool AllSameEtag(IEnumerable<DatastoreKeyValueReadArgs<Uri>> requests, out string etag)
        {
            etag = null;

            foreach (var request in requests)
            {
                if (request.Response is RestResponse restResponse && restResponse.ControlData.TryGetValue(REpresentationalStateTransfer.Rest.ETAG, out var etags))
                {
                    var itr = etags.GetEnumerator();

                    if (itr.MoveNext())
                    {
                        etag ??= itr.Current;

                        if (etag == itr.Current && !itr.MoveNext())
                        {
                            continue;
                        }
                    }
                }

                return false;
            }

            return true;
        }

        protected virtual IEnumerable<KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> GroupUrls(IEnumerable<DatastoreKeyValueReadArgs<Uri>> args) => args.Select(arg => new KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(Resolver.ResolveUrl(arg.Key), arg.AsEnumerable()));

        private TMDbResolver.TrojanTMDbUri GetUri(Item item, string url, DatastoreKeyValueReadArgs<Uri>[] requests)
        {
            var properties = requests
                    .Select(arg => arg.Key)
                    .OfType<UniformItemIdentifier>()
                    .Select(uii => uii.Property)
                    .ToHashSet();
            bool parentCollectionWasRequested = properties.Contains(Movie.PARENT_COLLECTION);
            var uri = new TMDbResolver.TrojanTMDbUri(url, item, parentCollectionWasRequested)
            {
                RequestedProperties = properties
            };

            if (Resolver.TryGetConverter(uri, out var converter))
            {
                uri.Converter = converter;
            }

            return uri;
        }

        private class TrojanProcessor : IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>>
        {
            public IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>> Processor { get; }
            public TMDbResolver Resolver { get; }

            public TrojanProcessor(IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>> processor, TMDbResolver resolver)
            {
                Processor = processor;
                Resolver = resolver;
            }

            public async Task<bool> ProcessAsync(DatastoreKeyValueReadArgs<Uri> e)
            {
                var response = await Processor.ProcessAsync(e);

                if (e.Response is RestResponse restResponse)
                {
                    var converter = (e.Key as TMDbResolver.TrojanTMDbUri)?.Converter ?? (Resolver.TryGetConverter(e.Key, out var temp) ? temp : null);

                    if (converter != null)
                    {
                        if (restResponse is HttpResponse httpResponse)
                        {
                            await httpResponse.BindingDelay;
                        }

                        if (restResponse.Entities is State state && restResponse.TryGetRepresentation<IEnumerable<byte>>(out var bytes))
                        {
                            var converted = await converter.Convert(new System.Net.Http.ByteArrayContent(bytes as byte[] ?? bytes.ToArray()));
                            state.Add(converted.GetType(), converted);
                        }
                    }
                }

                return response;
            }
        }
    }
}
