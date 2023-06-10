using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Movies
{
    public class HttpResourceCollection : HttpResource<IReadOnlyDictionary<Uri, object>>
    {
        public IEnumerable<Uri> Uris { get; }

        public HttpResourceCollection(Task<IReadOnlyDictionary<Uri, object>> task, IEnumerable<Uri> uris) : base(task)
        {
            Uris = uris;
        }
    }
}