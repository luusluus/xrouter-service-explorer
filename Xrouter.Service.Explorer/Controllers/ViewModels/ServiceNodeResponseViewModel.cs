using System;
using System.Collections.Generic;
using BitcoinLib.RPC.RequestResponse;
using Newtonsoft.Json;

namespace blocknet_xrouter.Controllers.ViewModels
{
    public class ServiceNodeViewModel
    {
		public string SNodeKey { get; set; }
		public string Tier { get; set; }
		public string Status { get; set; }
		public string Address { get; set; }
		public int Score { get; set; }
		public DateTime TimeLastSeenStr { get; set; }
		public long TimeLastSeen { get; set; }
        public List<string> SpvWallets { get; set; }
        public List<string> XCloudServices { get; set; }
    }
}
