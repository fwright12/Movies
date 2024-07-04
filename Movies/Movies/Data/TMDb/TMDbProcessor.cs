using Movies.Models;
using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbProcessor : AsyncGroupEventProcessor<KeyValueRequestArgs<Uri>, KeyValueRequestArgs<Uri>>
    {
        public TMDbResolver Resolver { get; }

        public TMDbProcessor(IAsyncEventProcessor<KeyValueRequestArgs<Uri>> processor, TMDbResolver resolver) : base(new TrojanProcessor(processor, resolver))
        {
            Resolver = resolver;
        }

        protected override bool Handle(KeyValueRequestArgs<Uri> grouped, IEnumerable<KeyValueRequestArgs<Uri>> singles)
        {
            if (!grouped.IsHandled || false == (grouped.Request.Key as TMDbResolver.TrojanTMDbUri)?.Converter is HttpResourceCollectionConverter resources)
            {
                return grouped.IsHandled;
            }

            if (!(grouped.Response as ResourceResponse).TryGetRepresentation<IEnumerable<KeyValuePair<Uri, object>>>(out var collection))
            {
                collection = null;
            }

            var index = collection?.ToReadOnlyDictionary() ?? new Dictionary<Uri, object>();
            var result = true;
            
            foreach (var request in singles)
            {
                var response = grouped.Response;

                /*if (response is HttpResponse httpResponse && httpResponse.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    response = request.Response ?? response;
                }
                else*/ if (true == collection?.TryGetValue(request.Request.Key, out var value))
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

                        response = new RestResponse(entities, controlData, metadata, request.Request.Expected);
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

        protected override IEnumerable<KeyValuePair<KeyValueRequestArgs<Uri>, IEnumerable<KeyValueRequestArgs<Uri>>>> GroupRequests(IEnumerable<KeyValueRequestArgs<Uri>> args)
        {
            var item = args.Select(arg => arg.Request.Key).OfType<UniformItemIdentifier>().Select(uii => uii.Item).FirstOrDefault(item => item != null);

            foreach (var kvp in GroupUrls(args))
            {
                var url = kvp.Key;
                var requests = kvp.Value.ToArray();
                var uri = GetUri(item, url, requests);
                IEnumerable<KeyValueRequestArgs<Uri>> grouped = requests;

                if (uri.Converter is HttpResourceCollectionConverter converter && args is BulkEventArgs<KeyValueRequestArgs<Uri>> batch)
                {
                    var index = requests.ToDictionary(req => req.Request.Key, req => req);

                    foreach (var resource in converter.Resources)
                    {
                        if (!index.ContainsKey(resource))
                        {
                            var request = new KeyValueRequestArgs<Uri>(resource);//, (resource as UniformItemIdentifier)?.Property.FullType);

                            index.Add(resource, request);
                            //batch.Add(request);
                        }
                    }

                    grouped = index.Values;
                }

                var groupRequest = new RestRequestEventArgs(uri);
                if (TryGetSingleIfNoneMatch(requests, out var etag))
                {
                    groupRequest.ControlData[REpresentationalStateTransfer.Rest.IF_NONE_MATCH] = new List<string> { etag };
                }

                yield return new KeyValuePair<KeyValueRequestArgs<Uri>, IEnumerable<KeyValueRequestArgs<Uri>>>(new KeyValueRequestArgs<Uri>(groupRequest), grouped);
            }
        }

        private static bool TryGetSingleIfNoneMatch(IEnumerable<KeyValueRequestArgs<Uri>> requests, out string etag)
        {
            etag = null;

            foreach (var request in requests)
            {
                if (request.Request is RestRequestEventArgs restRequest && restRequest.ControlData.TryGetValue(REpresentationalStateTransfer.Rest.IF_NONE_MATCH, out var etags))
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

        protected virtual IEnumerable<KeyValuePair<string, IEnumerable<KeyValueRequestArgs<Uri>>>> GroupUrls(IEnumerable<KeyValueRequestArgs<Uri>> args) => args
            .Where(arg => !arg.IsHandled)
            .Select(arg => new KeyValuePair<string, IEnumerable<KeyValueRequestArgs<Uri>>>(Resolver.ResolveUrl(arg.Request.Key), arg.AsEnumerable()))
            .GroupBy(kvp => kvp.Key, (key, group) => new KeyValuePair<string, IEnumerable<KeyValueRequestArgs<Uri>>>(key, group.SelectMany(pair => pair.Value)));

        private TMDbResolver.TrojanTMDbUri GetUri(Item item, string url, KeyValueRequestArgs<Uri>[] requests)
        {
            var properties = requests
                    .Select(arg => arg.Request.Key)
                    .OfType<UniformItemIdentifier>()
                    .Select(uii => uii.Property)
                    .ToHashSet();
            bool parentCollectionWasRequested = properties.Contains(Movie.PARENT_COLLECTION);
            var uri = new TMDbResolver.TrojanTMDbUri(url, item, parentCollectionWasRequested)
            {
                //RequestedProperties = properties
            };

            if (Resolver.TryGetConverter(uri, out var converter))
            {
                uri.Converter = converter;
            }

            return uri;
        }

        private class TrojanProcessor : IAsyncEventProcessor<KeyValueRequestArgs<Uri>>
        {
            public IAsyncEventProcessor<KeyValueRequestArgs<Uri>> Processor { get; }
            public TMDbResolver Resolver { get; }

            public TrojanProcessor(IAsyncEventProcessor<KeyValueRequestArgs<Uri>> processor, TMDbResolver resolver)
            {
                Processor = processor;
                Resolver = resolver;
            }

            public async Task<bool> ProcessAsync(KeyValueRequestArgs<Uri> e)
            {
                var response = await Processor.ProcessAsync(e);

                if (e.Response is RestResponse restResponse)
                {
                    var converter = (e.Request.Key as TMDbResolver.TrojanTMDbUri)?.Converter ?? (Resolver.TryGetConverter(e.Request.Key, out var temp) ? temp : null);

                    if (converter != null)
                    {
                        if (restResponse is HttpResponse httpResponse)
                        {
                            await httpResponse.BindingDelay;
                        }

                        if (restResponse.Resource.Get() is State state && restResponse.TryGetRepresentation<IEnumerable<byte>>(out var bytes))
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
