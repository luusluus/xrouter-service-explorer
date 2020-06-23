using System.Collections.Generic;
using BlocknetLib.Responses.Monero;
using BlocknetLib.RPC.Deserializer;
using BlocknetLib.RPC.RequestResponse;
using Newtonsoft.Json;

namespace BlocknetLib.Services.Coins.Blocknet.Xrouter.Monero
{
    [JsonConverter(typeof(ValidOrErrorConverterMonero))]
    public class GetDecodeRawTransactionResponse : ErrorResponse
    {
        public TransactionResponse Reply { get; set; }
        public string Uuid { get; set; } 
    } 
}