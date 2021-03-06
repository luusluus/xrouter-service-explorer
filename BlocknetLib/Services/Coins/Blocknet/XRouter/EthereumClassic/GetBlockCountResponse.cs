using System.Collections.Generic;
using BlocknetLib.RPC.RequestResponse;
using Newtonsoft.Json;
using Xrouter.Service.Explorer.BitcoinLib.Services.Coins.Blocknet.XRouter;
using BlocknetLib.Services.Coins.Blocknet;
using BlocknetLib.RPC.Deserializer;

namespace BlocknetLib.Services.Coins.Blocknet.Xrouter.EthereumClassic
{
    [JsonConverter(typeof(ValidOrErrorEthereumClassicConverter))]
    public class GetBlockCountResponse : ErrorResponse
    {
        public string Reply { get; set; }
        public string Uuid { get; set; }
    }
}
