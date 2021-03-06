using System.Collections.Generic;
using BlocknetLib.Responses.Ethereum;
using BlocknetLib.RPC.Deserializer;
using BlocknetLib.RPC.RequestResponse;
using Newtonsoft.Json;

namespace BlocknetLib.Services.Coins.Blocknet.Xrouter.Ethereum
{
    [JsonConverter(typeof(ValidOrErrorConverterEthereum))]
    public class GetBlocksResponse : ErrorResponse
    {
        public List<BlockResponse> Reply { get; set; }
        public string Uuid { get; set; }
    }
}