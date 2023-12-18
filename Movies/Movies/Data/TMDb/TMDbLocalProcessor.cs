using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbLocalProcessor : TMDbProcessor
    {
        public TMDbLocalProcessor(IAsyncEventProcessor<ResourceReadArgs<Uri>> processor, TMDbResolver resolver) : base(processor, resolver) { }

        protected override IEnumerable<KeyValuePair<string, IEnumerable<ResourceReadArgs<Uri>>>> GroupUrls(IEnumerable<ResourceReadArgs<Uri>> args) => base.GroupUrls(args.Where(arg => !arg.IsHandled));

        //protected override bool Handle(ResourceReadArgs<Uri> grouped, IEnumerable<ResourceReadArgs<Uri>> singles) => base.Handle(grouped, singles) && (grouped.Response as RestResponse)?.ControlData.TryGetValue(Rest.ETAG, out _) != true;
    }
}
