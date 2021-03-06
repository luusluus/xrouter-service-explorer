using System.Collections.Generic;
using BlocknetLib.Responses.Ethereum;
using BlocknetLib.RPC.Deserializer;
using BlocknetLib.RPC.RequestResponse;
using Newtonsoft.Json;

namespace BlocknetLib.Services.Coins.Blocknet.Xrouter.Neo
{
    [JsonConverter(typeof(ValidOrErrorConverterNeo))]
    public class GetBlockHashResponse : ErrorResponse
    {
        public string Reply { get; set; }
        public string Uuid { get; set; }
    }
}