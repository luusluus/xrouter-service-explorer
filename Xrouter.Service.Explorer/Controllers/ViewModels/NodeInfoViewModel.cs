using System.Collections.Generic;
using BitcoinLib.RPC.RequestResponse;
using Newtonsoft.Json;

namespace blocknet_xrouter.Controllers.ViewModels
{
    public class NodeInfoViewModel
    {
        public string NodePubKey { get; set; }
        public int Score { get; set; }
        public bool Banned { get; set; }
        public string PaymentAddress { get; set; }
        public List<string> SpvWallets { get; set; }
        public List<SpvConfigViewModel> SpvConfigs { get; set; }
        public double FeeDefault { get; set; }
        public Dictionary<string,double> Fees { get; set; }
        public List<string> Services { get; set; }
    }

}