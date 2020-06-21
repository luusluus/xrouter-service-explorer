using System.Collections.Generic;
using BlocknetLib.Responses;
using BlocknetLib.Responses.Ethereum;
using BlocknetLib.RPC.Deserializer;
using BlocknetLib.RPC.RequestResponse;
using Newtonsoft.Json;

namespace BlocknetLib.Services.Coins.Blocknet.Xrouter.EthereumClassic
{
    [JsonConverter(typeof(ValidOrErrorConverterEthereum))]
    public class GetTransactionResponse : ErrorResponse
    {
        public GetTransactionResponse Reply { get; set; }
        public string Uuid { get; set; }
    }
}