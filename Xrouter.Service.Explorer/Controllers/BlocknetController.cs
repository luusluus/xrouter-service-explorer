using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BitcoinLib.ExceptionHandling.Rpc;
using BitcoinLib.RPC.RequestResponse;
using BitcoinLib.Services.Coins.Base;
using BitcoinLib.Services.Coins.Bitcoin;
using BitcoinLib.Services.Coins.Blocknet;
using BitcoinLib.Services.Coins.Blocknet.Xrouter;
using blocknet_xrouter.Controllers.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace blocknet_xrouter.Controllers
{
    [Route("api/[controller]")]
    public class BlocknetController : ControllerBase 
    {
        private readonly IBlocknetService _blocknetService;
        public BlocknetController(IBlocknetService blocknetService){
            this._blocknetService = blocknetService;
        }

        [HttpGet("[action]")]
        public uint BlockCount(){
            return this._blocknetService.GetBlockCount();
        }

        #region XRouter
        [HttpGet("Xrouter/[action]")]
        public ConnectResponse Connect(string service, int node_count = 1){
            var resp = this._blocknetService.xrConnect(service, node_count);
            return resp;
        }
        
        [HttpGet("Xrouter/[action]")]
        public IActionResult GetSpvWalletInfo(string service, string nodePubKey = null, int node_count = 1){
            ConnectedNodeResponse serviceNode;
            List<ConnectedNodeResponse> otherNodes;

            ConnectResponse connectResponse;
            try
            {
                connectResponse = this._blocknetService.xrConnect(service, node_count);    
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message
                });                
            }
            
            if(connectResponse.Error != null){
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = connectResponse.Error,
                    Code = connectResponse.Code
                });
            }

            var connectReply = connectResponse.Reply;                
            var configReply = this._blocknetService.xrShowConfigs();

            if(string.IsNullOrWhiteSpace(nodePubKey)){
                // Randomly choose a service node from the list if no nodePubKey is given
                var r = new Random();
                var index = r.Next(connectReply.Count);
                serviceNode = connectReply[index];
                // split node list
                otherNodes = connectReply.Where(s => s.NodePubKey != serviceNode.NodePubKey).ToList();
            }
            else{
                serviceNode = connectReply.Find(s => s.NodePubKey == nodePubKey);
                otherNodes = connectReply.Where(s => s.NodePubKey != nodePubKey).ToList();
            }

            var serviceNodeConfig = configReply.Find(c => c.NodePubKey == serviceNode.NodePubKey);
            var serviceName = service.Replace("xr::", "");
                
            var spvConfig = serviceNode.SpvConfigs.Find(c => c.SpvWallet == serviceName);

            //TODO: Add AutoMapper to replace this.        
            var viewModel = new SpvWalletResultViewModel
            {
                SpvConfig = new SpvConfigViewModel
                {
                    SpvWallet = spvConfig.SpvWallet,
                    Commands = spvConfig.Commands.Where(c => c.Command != "xrGetConfig").Select(c => new SpvCommandViewModel{
                        Command = c.Command,
                        Disabled = c.Disabled,
                        Fee = c.Fee,
                        PaymentAddress = c.PaymentAddress,
                        RequestLimit = c.RequestLimit
                    }).ToList()
                },
                Node = new NodeInfoViewModel
                {
                    Banned = serviceNode.Banned,
                    NodePubKey = serviceNode.NodePubKey,
                    PaymentAddress = serviceNode.PaymentAddress,
                    Score = serviceNode.Score
                },
                OtherNodes = otherNodes.Select(n => new NodeInfoViewModel{
                    Banned = n.Banned,
                    NodePubKey = n.NodePubKey,
                    PaymentAddress = n.PaymentAddress,
                    Score = n.Score
                }).ToList()   
            };
            
            return Ok(viewModel);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetServiceInfo(string service, string nodePubKey = null, int node_count = 1){
            //TODO: refactor this into a class so it becomes a thin controller.

            ConnectedNodeResponse serviceNode;
            List<ConnectedNodeResponse> otherNodes;

            ConnectResponse connectResponse;
            try
            {
                connectResponse = this._blocknetService.xrConnect(service, node_count);    
            }
            catch (RpcRequestTimeoutException e)
            {
                 return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message
                });                
            }

            if(connectResponse.Error != null){
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = connectResponse.Error,
                    Code = connectResponse.Code
                });
            }

            var connectReply = connectResponse.Reply;
            var configReply = this._blocknetService.xrShowConfigs();
                
            if(string.IsNullOrWhiteSpace(nodePubKey)){
                // Randomly choose a service node from the list if no nodePubKey is given
                var r = new Random();
                var index = r.Next(connectReply.Count);
                serviceNode = connectReply[index];
                // split node list
                otherNodes = connectReply.Where(s => s.NodePubKey != serviceNode.NodePubKey).ToList();
            }
            else{
                serviceNode = connectReply.Find(s => s.NodePubKey == nodePubKey);
                otherNodes = connectReply.Where(s => s.NodePubKey != nodePubKey).ToList();
            }

            var serviceNodeConfig = configReply.Find(c => c.NodePubKey == serviceNode.NodePubKey);
            
            var serviceName = service.Replace("xrs::", "");
            var serv = serviceNode.Services[serviceName];
            
            string help = string.Empty;
            string xcloudConfig = string.Empty;
            if(serviceNodeConfig.Plugins.Count > 0){
                // Try get help key from config string
                var dictConfig = serviceNodeConfig.Plugins[serviceName]
                        .Split(new string[] {"\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(value => value.Split('='))
                        .ToDictionary(pair => pair[0], pair => pair[1]);
                
                dictConfig.TryGetValue("help", out help);
                xcloudConfig = serviceNodeConfig.Plugins[serviceName];
            }

            //TODO: Add AutoMapper to replace this.     
            var viewModel = new XCloudServiceResultViewModel
            {
                Service = new XCloudServiceViewModel
                {
                    HelpDescription = help,
                    Disabled = serv.Disabled,
                    Fee = serv.Fee,
                    FetchLimit = serv.FetchLimit,
                    Parameters = serv.Parameters,
                    ParametersList = serv.Parameters != string.Empty ? serv.Parameters.Split(',').ToList() : null,
                    PaymentAddress = serv.PaymentAddress,
                    RequestLimit = serv.RequestLimit,
                    Config = xcloudConfig
                },
                Node = new NodeInfoViewModel
                {
                    Banned = serviceNode.Banned,
                    NodePubKey = serviceNode.NodePubKey,
                    PaymentAddress = serviceNode.PaymentAddress,
                    Score = serviceNode.Score
                },
                OtherNodes = otherNodes.Select(n => new NodeInfoViewModel{
                    Banned = n.Banned,
                    NodePubKey = n.NodePubKey,
                    PaymentAddress = n.PaymentAddress,
                    Score = n.Score
                }).ToList()    
            };       

            return Ok(viewModel);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetNodeInfo(string nodePubKey, string service = null, int node_count = 1){
            if (string.IsNullOrWhiteSpace(nodePubKey))
                return BadRequest("NodePubKey was not supplied");

            GetConnectedNodesResponse getConnectedResponse;
            try
            {
                getConnectedResponse = this._blocknetService.xrConnectedNodes();    
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message + Environment.NewLine + "Node Public Key: " + nodePubKey,
                    Code = (int) e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                 return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message + Environment.NewLine + "Node Public Key: " + nodePubKey,
                });                
            }

            var connectReply = getConnectedResponse.Reply;

            ConnectResponse connectResponse;
            var serviceNode = connectReply.Find(s => s.NodePubKey == nodePubKey);
            if (serviceNode == null)
            {
                try
                {
                    connectResponse = this._blocknetService.xrConnect(service, node_count);    
                }
                catch (RpcInternalServerErrorException e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                        Error = e.Message + Environment.NewLine + "Node Public Key: " + nodePubKey,
                        Code = (int) e.RpcErrorCode.Value
                    });
                }
                catch (RpcRequestTimeoutException e)
                {
                    return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                        Error = e.Message + Environment.NewLine + "Node Public Key: " + nodePubKey,
                    });                
                }
                 
                serviceNode = connectReply.Find(s => s.NodePubKey == nodePubKey);
            }
               
            var configReply = this._blocknetService.xrShowConfigs();            

            string config = string.Empty;
            var serviceNodeConfig = configReply.Find(c => c.NodePubKey == serviceNode.NodePubKey);
            
            if(serviceNodeConfig != null)
                config = serviceNodeConfig.Config;

            var viewModel = new NodeInfoViewModel
            {
                Banned = serviceNode.Banned,
                FeeDefault = serviceNode.FeeDefault,
                Fees = serviceNode.Fees,
                NodePubKey = serviceNode.NodePubKey,
                PaymentAddress = serviceNode.PaymentAddress,
                Score = serviceNode.Score,
                Config = config,
                Services = serviceNode.Services.Select(s => s.Key).ToList(),
                SpvWallets = serviceNode.SpvWallets,
                SpvConfigs = serviceNode.SpvConfigs.Select(s => new SpvConfigViewModel{
                    SpvWallet = s.SpvWallet,
                    Commands = s.Commands.Select(c => new SpvCommandViewModel{
                        Command = c.Command,
                        Disabled = c.Disabled,
                        Fee = c.Fee,
                        PaymentAddress = c.PaymentAddress,
                        RequestLimit = c.RequestLimit
                    }).ToList()
                }).ToList()
            };

            return Ok(viewModel);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetBlockCount([FromQuery(Name = "blockchain")]string blockchain,[FromQuery(Name = "node_count")] int node_count = 1){
            if(string.IsNullOrEmpty(blockchain)) 
                return BadRequest("No blockchain parameter supplied");

            GetBlockCountResponse response;
            try
            {
                response = this._blocknetService.xrGetBlockCount(blockchain, node_count);
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message,
                    Code = (int) e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message,
                });                
            }
            catch (RpcResponseDeserializationException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message,
                });                
            }
            return Ok(response);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult DecodeRawTransaction(string blockchain, string tx_hex, int node_count = 1){
            if(string.IsNullOrEmpty(blockchain)) 
                return BadRequest("No blockchain parameter supplied");
            
            DecodeRawTransactionResponse response;
            try
            {
                response = this._blocknetService.xrDecodeRawTransaction(blockchain, tx_hex, node_count);
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message,
                    Code = (int) e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message,
                });                
            }
            return Ok(response);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetBlockHash(string blockchain, string block_number, int node_count = 1){
            GetBlockHashResponse response;
            try
            {
                response = this._blocknetService.xrGetBlockHash(blockchain, block_number, node_count);
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message,
                    Code = (int) e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message,
                });                
            }
            return Ok(response);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetBlock(string blockchain, string block_hash, int node_count = 1){
            GetBlockResponse response;

            try
            {
                response =  this._blocknetService.xrGetBlock(blockchain, block_hash, node_count);
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message,
                    Code = (int) e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message,
                });                
            }
            return Ok(response);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetBlocks([FromQuery(Name = "blockchain")]string blockchain, [FromQuery(Name = "block_hashes")] string block_hashes, int node_count = 1){
            GetBlocksResponse response;

            try
            {
                var result = JsonConvert.DeserializeObject<List<string>>(block_hashes);
                response = this._blocknetService.xrGetBlocks(blockchain, string.Join(",", result), node_count);
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message,
                    Code = (int) e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message,
                });                
            }
            
            return Ok(response);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetTransaction(string blockchain, string txid, int node_count = 1){
            GetTransactionResponse response;
            try
            {
                response = this._blocknetService.xrGetTransaction(blockchain, txid, node_count);
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message,
                    Code = (int) e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message,
                });                
            }

            return Ok(response);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetTransactions([FromQuery(Name = "blockchain")]string blockchain, [FromQuery(Name = "txids")] string txids, int node_count = 1){  
            GetTransactionsResponse response;

            try
            {
                var result = JsonConvert.DeserializeObject<List<string>>(txids);        
                response = this._blocknetService.xrGetTransactions(blockchain, string.Join(",",result), node_count);
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message,
                    Code = (int) e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message,
                });                
            }

            return Ok(response);
        }

        [HttpPost("Xrouter/[action]")]
        public IActionResult SendTransaction(string blockchain, string signed_tx){
            SendTransactionResponse response;
            try
            {
                response = this._blocknetService.xrSendTransaction(blockchain, signed_tx);
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = e.Message,
                    Code = (int) e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError{
                    Error = e.Message,
                });                
            }
            return Ok(response);
        }

        [HttpGet("Xrouter/[action]")]
        public GetReplyResponse GetReply(string uuid){
            return this._blocknetService.xrGetReply(uuid);
        }

        [HttpGet("Xrouter/[action]")]
        public List<ShowConfigsResponse> ShowConfigs(){
            return this._blocknetService.xrShowConfigs();
        }

        [HttpPost("Xrouter/[action]")]
        public UpdateConfigsResponse UpdateConfigs(bool force_check = false){
            return this._blocknetService.xrUpdateConfigs(force_check);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetNetworkServices(){
            var response = this._blocknetService.xrGetNetworkServices();
            var services = response.Reply.NodeCounts
                .Join(response.Reply.Services, m => m.Key, m => m.ToString(), 
                    (s, sn) => new ServiceViewModel{
                        Name = s.Key,
                        NodeCount = s.Value
                    }).ToList();
                
            var viewModel = new NetworkServicesResponseViewModel
            {
                Items = services,
                TotalItems = services.Count
            };
            return Ok(viewModel);
        }

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetNetworkSpvWallets(){
            var response = this._blocknetService.xrGetNetworkServices();
            var services = response.Reply.NodeCounts
                .Join(response.Reply.SpvWallets, m => m.Key, m => m.ToString(), 
                    (s, sn) => new ServiceViewModel{
                        Name = s.Key,
                        NodeCount = s.Value
                    }).ToList();
                
            var viewModel = new NetworkServicesResponseViewModel
            {
                Items = services,
                TotalItems = services.Count                                   
            };
            return Ok(viewModel);
        }

        [HttpGet("Xrouter/[action]")]
        public GetConnectedNodesResponse GetConnectedNodes(){
            return this._blocknetService.xrConnectedNodes();
        }


        #endregion
        #region XCloud
        [HttpPost("Xrouter/[action]")]
        public IActionResult Service([FromBody]ServiceRequest request){
            if(request == null)
                return BadRequest("No service request supplied");
            return Ok(this._blocknetService.xrService(request.Service, request.Parameters));
        }
        #endregion

        [HttpGet("Xrouter/[action]")]
        public IActionResult GetServiceNodeList()
        {
            var result = this._blocknetService.serviceNodeList();

            //TODO: Add an automapper module
            var viewModel = result.Select(sn => new ServiceNodeResponseViewModel
            {
              ActiveTime = DateTimeOffset.FromUnixTimeSeconds(sn.ActiveTime).UtcDateTime,
              LastPaid = DateTimeOffset.FromUnixTimeSeconds(sn.LastPaid).UtcDateTime,
              LastSeen = DateTimeOffset.FromUnixTimeSeconds(sn.LastSeen).UtcDateTime,
              Addr = sn.Addr,
              NodePubKey = sn.NodePubKey,
              OutIdx = sn.OutIdx,
              Rank = sn.Rank,
              Status = sn.Status,
              TxHash = sn.TxHash,
              Version = sn.Version,
              XBridgeVersion = sn.XBridgeVersion,
              XRouterVersion = sn.XRouterVersion,
              SpvWallets = sn.XWallets.Split(',').Where(s => s.Split(':')[0].Equals("xr")).Where(s => !s.Equals("xr")).ToList(),
              XCloudServices = sn.XWallets.Split(',').Where(s => s.Split(':')[0].Equals("xrs")).Where(s => !s.Equals("xr")).ToList()
              
            }).Where(vm => vm.SpvWallets.Count > 0 || vm.XCloudServices.Count > 0).ToList();

            return Ok(viewModel);
        }

        [HttpGet("Xrouter")]
        public IActionResult Index(string searchString)
        {
            var response = this._blocknetService.xrGetNetworkServices();

            var services = response.Reply.NodeCounts
                .Join(response.Reply.Services, m => m.Key, m => m.ToString(), 
                    (s, sn) => new ServiceViewModel{
                        Name = s.Key,
                        NodeCount = s.Value
                    }).ToList();

            var spvWallets = response.Reply.NodeCounts
                .Join(response.Reply.SpvWallets, m => m.Key, m => m.ToString(), 
                    (s, sn) => new ServiceViewModel{
                        Name = s.Key,
                        NodeCount = s.Value
                    }).ToList();
            
            var allServices = services.Union(spvWallets).ToList();

            if (!String.IsNullOrEmpty(searchString))
                allServices = allServices.Where(s => s.Name.Contains(searchString)).ToList();
            
            var viewModel = new NetworkServicesResponseViewModel
            {
                Items = allServices,
                TotalItems = allServices.Count
            };
            return Ok(viewModel);
        }

    }
}
