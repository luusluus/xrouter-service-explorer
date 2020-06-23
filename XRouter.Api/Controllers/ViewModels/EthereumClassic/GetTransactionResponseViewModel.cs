using System.Collections.Generic;
using BlocknetLib.Responses;
using Newtonsoft.Json;

namespace XRouter.Api.Controllers.ViewModels.EthereumClassic
{
    public class GetTransactionResponseViewModel: XRouterBaseResponseViewModel
    {
        public TransactionResponseViewModel Reply { get; set; }
    }
}