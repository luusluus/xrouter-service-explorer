using System.Collections.Generic;
using BitcoinLib.RPC.RequestResponse;
using Newtonsoft.Json;

namespace blocknet_xrouter.Controllers.ViewModels
{
    public class ServiceNodeQueryViewModel : QueryViewModel
    {
        public string SpvWallet{ get; set; }
        public string XCloudService{ get; set; }
        public bool AtleastOneSpvWallet { get; set; }
        public bool Reliable { get; set; }
    }

}
