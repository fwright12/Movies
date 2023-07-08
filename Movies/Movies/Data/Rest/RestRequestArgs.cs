using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Movies
{
    public class RestRequestArgs : AsyncChainEventArgs
    {
        public RestRequest Request { get; }
        public RestResponse RestResponse { get; private set; }
        public Uri Uri { get; }
        public State Body { get; }
        public Task<State> ResponseAsync { get; private set; }
        public State Response { get; private set; }
        public Type Expected { get; }

        public RestRequestArgs(Uri uri, Type expected = null)
        {
            Uri = uri;
            Expected = expected;
        }

        public RestRequestArgs(Uri uri, State body)
        {
            Uri = uri;
            Body = body;
        }

        private static async Task<State> Convert<T>(Task<T> value) => State.Create(await value);

        public async Task<bool> Handle<T>(Task<T> response)
        {
            await Handle(Convert(response));
            return true;
        }

        public bool Handle<T>(T response) => Handle(response as State ?? State.Create<T>(response));

        public bool Handle<T>(IConverter<T> converter)
        {
            if (Expected != null && Expected != typeof(T) && Response?.Add(Expected, converter) == true)
            {
                Handle();
                return true;
            }

            return false;
        }

        public bool Handle(State response)
        {
            Response = response;

            if (Expected == null || response.HasRepresentation(Expected))
            {
                Handle();
                return true;
            }

            return false;
        }

        public Task Handle(Task<State> response)
        {
            var result = HandleAsync(response);
            RequestSuspension(response);
            return response;
        }

        private async Task<bool> HandleAsync(Task<State> response) => Handle(await response);
    }

    public class RestRequestArgs<T> : RestRequestArgs
    {
        public RestRequestArgs(Uri uri) : base(uri, typeof(T)) { }
    }
}
