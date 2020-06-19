using System.Collections.Generic;
using BlocknetLib.Responses;
using BlocknetLib.RPC.RequestResponse;
using Newtonsoft.Json;

namespace XRouter.Api.Controllers.ViewModels.Ethereum
{
    public class BlockResponseViewModel : XRouterBaseResponseViewModel
    {
        public GetBlockResponseViewModel Reply { get; set; }
    }
}