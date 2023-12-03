using FFImageLoading.Helpers.Exif;
using Movies.Models;
using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public abstract class TMDbProcessor : AsyncGroupEventProcessor<ResourceReadArgs<Uri>, ResourceReadArgs<Uri>>
    {
        public TMDbResolver Resolver { get; }

        public TMDbProcessor(IAsyncEventProcessor<ResourceReadArgs<Uri>> processor, TMDbResolver resolver) : base(new TrojanProcessor(processor, resolver))
        {
            Resolver = resolver;
        }

        protected override bool Handle(ResourceReadArgs<Uri> grouped, IEnumerable<ResourceReadArgs<Uri>> singles)
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
                        response = new ResourceResponse<object>(value);
                    }
                }

                result &= request.Handle(response);
            }

            return result;
        }

        protected override IEnumerable<KeyValuePair<ResourceReadArgs<Uri>, IEnumerable<ResourceReadArgs<Uri>>>> GroupRequests(IEnumerable<ResourceReadArgs<Uri>> args)
        {
            var item = args.Select(arg => arg.Key).OfType<UniformItemIdentifier>().Select(uii => uii.Item).FirstOrDefault(item => item != null);

            foreach (var kvp in GroupUrls(args))
            {
                var url = kvp.Key;
                var requests = kvp.Value.ToArray();
                var uri = GetUri(item, url, requests);
                IEnumerable<ResourceReadArgs<Uri>> grouped = requests;

                if (uri.Converter is HttpResourceCollectionConverter converter && args is BulkEventArgs<ResourceReadArgs<Uri>> batch)
                {
                    var index = requests.ToDictionary(req => req.Key, req => req);

                    foreach (var resource in converter.Resources)
                    {
                        if (!index.ContainsKey(resource))
                        {
                            var request = new ResourceReadArgs<Uri>(resource);

                            index.Add(resource, request);
                            batch.Add(request);
                        }
                    }

                    grouped = index.Values;
                }

                var groupRequest = new RestRequestArgs(uri);
                if (TryGetSingleETag(requests, out var etag))
                {
                    groupRequest.ControlData[REpresentationalStateTransfer.Rest.IF_NONE_MATCH] = new List<string> { etag };
                }

                yield return new KeyValuePair<ResourceReadArgs<Uri>, IEnumerable<ResourceReadArgs<Uri>>>(groupRequest, grouped);
            }
        }

        private static bool TryGetSingleETag(IEnumerable<ResourceReadArgs<Uri>> requests, out string etag)
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

        protected virtual IEnumerable<KeyValuePair<string, IEnumerable<ResourceReadArgs<Uri>>>> GroupUrls(IEnumerable<ResourceReadArgs<Uri>> args) => args
            .Select(arg => new KeyValuePair<string, IEnumerable<ResourceReadArgs<Uri>>>(Resolver.ResolveUrl(arg.Key), arg.AsEnumerable()))
            .GroupBy(kvp => kvp.Key, (key, group) => new KeyValuePair<string, IEnumerable<ResourceReadArgs<Uri>>>(key, group.SelectMany(pair => pair.Value)));

        private TMDbResolver.TrojanTMDbUri GetUri(Item item, string url, ResourceReadArgs<Uri>[] requests)
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

        private class TrojanProcessor : IAsyncEventProcessor<ResourceReadArgs<Uri>>
        {
            public IAsyncEventProcessor<ResourceReadArgs<Uri>> Processor { get; }
            public TMDbResolver Resolver { get; }

            public TrojanProcessor(IAsyncEventProcessor<ResourceReadArgs<Uri>> processor, TMDbResolver resolver)
            {
                Processor = processor;
                Resolver = resolver;
            }

            public async Task<bool> ProcessAsync(ResourceReadArgs<Uri> e)
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
