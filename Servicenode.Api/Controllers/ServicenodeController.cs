using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BlocknetLib.ExceptionHandling.Rpc;
using BlocknetLib.RPC.RequestResponse;
using BlocknetLib.Services.Coins.Base;
using BlocknetLib.Services.Coins.Blocknet;
using BlocknetLib.Services.Coins.Blocknet.Xrouter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using BlocknetLib.Services.Coins.Blocknet.Xrouter.BitcoinBased;
using BlocknetLib.Services.Coins.Blocknet.Xrouter.Ethereum;
using Servicenode.Api.Controllers.ViewModels;
using Servicenode.Api.Core.Models;
using Servicenode.Api.Extensions;
using System.Text;
using Servicenode.Api.Helpers;

namespace Servicenode.Api.Controllers
{
    [Route("api/servicenode")]
    public class ServicenodeController : ControllerBase 
    {
        private readonly IXRouterService xrouterService;
        private readonly IServicenodeService servicenodeService;
        public ServicenodeController(IXRouterService xrouterService, IServicenodeService servicenodeService){
            this.xrouterService = xrouterService;
            this.servicenodeService = servicenodeService;
        }

        [HttpGet("[action]")]
        public IActionResult GetServiceNodeCount()
        {
            List<ShowConfigsResponse> configReply;
            try
            {                
                configReply = xrouterService.xrShowConfigs();
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError
                {
                    Error = e.Message,
                    Code = (int)e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError
                {
                    Error = e.Message,
                });
            }

            return Ok(configReply.Count());
        }

        [HttpGet("[action]")]
        public IActionResult GetNodesByService(string service, int node_count = 1)
        {
            ConnectResponse connectResponse;
            List<ShowConfigsResponse> configReply;
            try
            {
                connectResponse = xrouterService.xrConnect(service, node_count);
                configReply = xrouterService.xrShowConfigs();
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError
                {
                    Error = e.Message,
                    Code = (int)e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError
                {
                    Error = e.Message,
                });
            }
            var connectReply = connectResponse.Reply;

            if(service.Contains("xrs::"))
            {
                connectReply = connectReply.Where(cr => cr.Services.Keys.Contains(service.Replace("xrs::",""))).ToList();
            }
            else
            {
                connectReply = connectReply.Where(cr => cr.SpvWallets.Contains(service.Replace("xr::", ""))).ToList();
            }
            
            var nodes = connectReply.Select(cr => 
            {
                var serviceNodeConfig = configReply.Find(c => c.NodePubKey == cr.NodePubKey);
                var cfg = configReply.Find(c => c.NodePubKey == cr.NodePubKey);

                var cfgElements = new List<string[]>();
                if (cfg != null)
                {
                    cfgElements = cfg.Config.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(value => value.Split('=')).ToList();
                }

                string port = null;
                if (cfgElements.Any(lc => lc[0] == "port"))
                {
                    port = cfgElements.FirstOrDefault(e => e[0] == "port")[1];
                }

                return new NodeInfoViewModel
                {
                    Type = (port == "41412" || string.IsNullOrEmpty(port)) ? "Regular" : "Enterprise",
                    Banned = cr.Banned,
                    NodePubKey = cr.NodePubKey,
                    PaymentAddress = cr.PaymentAddress,
                    Score = cr.Score
                };
            }).ToList();
            return Ok(nodes);
        }

        [HttpGet("[action]")]
        public IActionResult GetSpvWalletInfo(string service, string nodePubKey = null, int node_count = 1)
        {
            ConnectedNodeResponse serviceNode;
            List<ConnectedNodeResponse> otherNodes;

            ConnectResponse connectResponse;
            List<ShowConfigsResponse> configReply;
            try
            {
                connectResponse = xrouterService.xrConnect(service, node_count);    
                configReply = xrouterService.xrShowConfigs();
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError
                {
                    Error = e.Message,
                    Code = (int)e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError
                {
                    Error = e.Message,
                });                
            }
            
            if(connectResponse.Error != null){
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError{
                    Error = connectResponse.Error,
                    Code = connectResponse.Code
                });
            }

            var connectReply = connectResponse.Reply;                

            if(string.IsNullOrWhiteSpace(nodePubKey)){
                serviceNode = connectReply.OrderByDescending(n => n.Score).FirstOrDefault();
                // split node list
                otherNodes = connectReply.Where(s => s.NodePubKey != serviceNode.NodePubKey).ToList();
            }
            else{
                serviceNode = connectReply.Find(s => s.NodePubKey == nodePubKey);
                otherNodes = connectReply.Where(s => s.NodePubKey != nodePubKey).ToList();
            }

            if(serviceNode == null)
                return StatusCode(StatusCodes.Status500InternalServerError, "Service node not reachable with xrConnect");

            var serviceNodeConfig = configReply.Find(c => c.NodePubKey == serviceNode.NodePubKey);

            var serviceNodeConfigElements = serviceNodeConfig.Config.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(value => value.Split('='));

            string serviceNodeHost = null;
            
            if(serviceNodeConfigElements.Any(lc => lc[0] == "host"))
            {
                serviceNodeHost = serviceNodeConfigElements.FirstOrDefault(e => e[0] == "host")[1];
            }

            string serviceNodePort = null;
            if(serviceNodeConfigElements.Any(lc => lc[0] == "port"))
            {
                serviceNodePort = serviceNodeConfigElements.FirstOrDefault(e => e[0] == "port")[1];
            }

            var serviceName = service.Replace("xr::", "");
                
            var spvConfig = serviceNode.SpvConfigs.Find(c => c.SpvWallet == serviceName);

            //TODO: Add AutoMapper to replace         
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
                        RequestLimit = c.RequestLimit,
                        FetchLimit = c.FetchLimit
                    }).ToList()
                },
                Node = new NodeInfoViewModel
                {
                    Type = (serviceNodePort == "41412" || string.IsNullOrEmpty(serviceNodePort)) ? "Regular" : "Enterprise",
                    Host = serviceNodeHost,
                    Port = serviceNodePort,
                    Banned = serviceNode.Banned,
                    NodePubKey = serviceNode.NodePubKey,
                    PaymentAddress = serviceNode.PaymentAddress,
                    Score = serviceNode.Score
                },
                OtherNodes = otherNodes.Select(n => {
                    var cfg = configReply.Find(c => c.NodePubKey == n.NodePubKey);

                    var cfgElements = new List<string[]>();
                    if(cfg != null)
                    {
                        cfgElements = cfg.Config.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(value => value.Split('=')).ToList();
                    }

                    string port = null;
                    if(cfgElements.Any(lc => lc[0] == "port"))
                    {
                        port = cfgElements.FirstOrDefault(e => e[0] == "port")[1];
                    }
                    return new NodeInfoViewModel{
                        Type = (port == "41412" || string.IsNullOrEmpty(port)) ? "Regular" : "Enterprise",
                        Banned = n.Banned,
                        NodePubKey = n.NodePubKey,
                        PaymentAddress = n.PaymentAddress,
                        Score = n.Score
                    };
                }).ToList()   
            };
            
            return Ok(viewModel);
        }

        [HttpGet("[action]")]
        public IActionResult GetServiceInfo(string service, string nodePubKey = null, int node_count = 1)
        {
            //TODO: refactor this into a class so it becomes a thin controller.

            ConnectedNodeResponse serviceNode;
            List<ConnectedNodeResponse> otherNodes;

            ConnectResponse connectResponse;
            List<ShowConfigsResponse> configReply;
            try
            {
                connectResponse = xrouterService.xrConnect(service, node_count);    
                configReply = xrouterService.xrShowConfigs();
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError
                {
                    Error = e.Message,
                    Code = (int)e.RpcErrorCode.Value
                });
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
                
            if(string.IsNullOrWhiteSpace(nodePubKey)){  
                serviceNode = serviceNode = connectReply.OrderByDescending(n => n.Score).FirstOrDefault();
                // split node list
                otherNodes = connectReply.Where(s => s.NodePubKey != serviceNode.NodePubKey).ToList();
            }
            else{
                serviceNode = connectReply.Find(s => s.NodePubKey == nodePubKey);
                otherNodes = connectReply.Where(s => s.NodePubKey != nodePubKey).ToList();
            }

            var serviceNodeConfig = configReply.Find(c => c.NodePubKey == serviceNode.NodePubKey);

            var serviceNodeConfigElements = serviceNodeConfig.Config.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(value => value.Split('='));

            string serviceNodeHost = null;
            
            if(serviceNodeConfigElements.Any(lc => lc[0] == "host"))
            {
                serviceNodeHost = serviceNodeConfigElements.FirstOrDefault(e => e[0] == "host")[1];
            }

            string serviceNodePort = null;
            if(serviceNodeConfigElements.Any(lc => lc[0] == "port"))
            {
                serviceNodePort = serviceNodeConfigElements.FirstOrDefault(e => e[0] == "port")[1];
            }
            
            string help = string.Empty;

            var serviceName = service.Replace("xrs::", "");
            var serviceConfig = serviceNode.Services[serviceName];

            var configReader = new ConfigReader(serviceNodeConfig.Config);

            var mainSectionSettings = configReader.EnumSection("Main");
            
            var sb = new StringBuilder();

            if(!string.IsNullOrEmpty(serviceConfig.Parameters))
                sb.Append("parameters=").Append(serviceConfig.Parameters).AppendLine();

            var mainFee = configReader.GetSetting("Main", "fee");
            if (!string.IsNullOrEmpty(mainFee))
            {
                if (serviceConfig.Fee != double.Parse(mainFee))
                    sb.Append("fee=").Append(serviceConfig.Fee).AppendLine();
            }
            else
            {
                sb.Append("fee=").Append(serviceConfig.Fee).AppendLine();
            }
                
            if(serviceConfig.Disabled)
                sb.Append("disabled=").Append(Convert.ToByte(serviceConfig.Disabled)).AppendLine();

            var mainFetchLimit = configReader.GetSetting("Main", "fetchlimit");
            if (!string.IsNullOrEmpty(mainFetchLimit))
            {
                if (serviceConfig.FetchLimit != int.Parse(mainFetchLimit))
                    sb.Append("fetchlimit=").Append(serviceConfig.FetchLimit).AppendLine();
            }
            else
            {
                sb.Append("fetchlimit=").Append(serviceConfig.FetchLimit).AppendLine();
            }

            var mainClientRequestLimit = configReader.GetSetting("Main", "clientrequestlimit");
            if (!string.IsNullOrEmpty(mainClientRequestLimit))
            {
                if (serviceConfig.RequestLimit != int.Parse(mainClientRequestLimit))
                    sb.Append("clientrequestlimit=").Append(serviceConfig.RequestLimit).AppendLine();
            }
            else
            {
                sb.Append("clientrequestlimit=").Append(serviceConfig.RequestLimit).AppendLine();
            }

            string xcloudConfig = sb.ToString();

            if (serviceNodeConfig?.Plugins.Count > 0)
            {
                // Try get help key from config string
                var listConfig = serviceNodeConfig.Plugins[serviceName]
                        .Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(value => value.Split('=')).ToList();

                if (listConfig.Any(lc => lc[0] == "help"))
                {
                    int i = 0;
                    foreach (var config in listConfig)
                    {
                        if (config[0] == "help")
                            break;
                        i++;
                    }
                    help = listConfig[i][1];

                    xcloudConfig = serviceNodeConfig.Plugins[serviceName];
                }
            }

            //TODO: Add AutoMapper to replace      
            var viewModel = new XCloudServiceResultViewModel
            {
                Service = new XCloudServiceViewModel
                {
                    HelpDescription = help,
                    Disabled = serviceConfig.Disabled,
                    Fee = serviceConfig.Fee,
                    FetchLimit = serviceConfig.FetchLimit,
                    Parameters = serviceConfig.Parameters,
                    ParametersList = serviceConfig.Parameters != string.Empty ? serviceConfig.Parameters.Split(',').ToList() : null,
                    PaymentAddress = serviceConfig.PaymentAddress,
                    RequestLimit = serviceConfig.RequestLimit,
                    Config = xcloudConfig
                },
                Node = new NodeInfoViewModel
                {
                    Type = (serviceNodePort == "41412" || string.IsNullOrEmpty(serviceNodePort)) ? "Regular" : "Enterprise",
                    Host = serviceNodeHost,
                    Port = serviceNodePort,
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

        [HttpGet("[action]")]
        public IActionResult GetNodeInfo(string nodePubKey, string service = null, int node_count = 1)
        {
            if (string.IsNullOrWhiteSpace(nodePubKey))
                return BadRequest("NodePubKey was not supplied");

            GetConnectedNodesResponse getConnectedResponse;
            try
            {
                getConnectedResponse = xrouterService.xrConnectedNodes();    
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
                if(service == null) service = "xr::BLOCK";
                try
                {
                    connectResponse = xrouterService.xrConnect(service, node_count);    
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
                        Error = e.Message + Environment.NewLine + "Node Public Ke5y: " + nodePubKey,
                    });                
                }
                 
                serviceNode = connectResponse.Reply.Find(s => s.NodePubKey == nodePubKey);

                if(serviceNode == null)
                    return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError
                    {
                        Error = "Servicenode info cannot be retrieved." + Environment.NewLine + "Node Public Key: " + nodePubKey,
                    });
            }
               
            var configReply = xrouterService.xrShowConfigs();            

            string config = string.Empty;
            var serviceNodeConfig = configReply.Find(c => c.NodePubKey == serviceNode.NodePubKey);
            
            if(serviceNodeConfig != null)
                config = serviceNodeConfig.Config;
            
            var serviceNodeConfigElements = config.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(value => value.Split('='));

            string serviceNodeHost = null;
            
            if(serviceNodeConfigElements.Any(lc => lc[0] == "host"))
            {
                serviceNodeHost = serviceNodeConfigElements.FirstOrDefault(e => e[0] == "host")[1];
            }

            string serviceNodePort = null;
            if(serviceNodeConfigElements.Any(lc => lc[0] == "port"))
            {
                serviceNodePort = serviceNodeConfigElements.FirstOrDefault(e => e[0] == "port")[1];
            }
            var viewModel = new NodeInfoViewModel
            {
                Type = (serviceNodePort == "41412" || string.IsNullOrEmpty(serviceNodePort))? "Regular" : "Enterprise",
                Host = serviceNodeHost,
                Port = serviceNodePort,
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

        //[HttpGet("[action]")]
        //public IActionResult FilterXCloudServiceServiceNode(XCloudServiceQueryViewModel filterViewModel)
        //{
        //    var connectedResponse = xrouterService.xrConnectedNodes();

        //    var serviceNode = connectedResponse.Reply.Find(c => c.NodePubKey == filterViewModel.NodePubKey);

        //    if(serviceNode == null){
        //        return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError
        //        {
        //            Error = "Servicenode info cannot be retrieved." + Environment.NewLine + "Node Public Key: " + filterViewModel.NodePubKey,
        //        });
        //    }

        //    var result = new QueryResult<string>();
        //    var query = serviceNode.Services.Select(s => s.Key).AsQueryable();
        //    var queryObj = new XCloudQuery
        //    {
        //        Page = (int) filterViewModel.Page,
        //        PageSize = (byte) filterViewModel.PageSize,
        //    };
        //    result.TotalItems = query.Count();

        //    query = query.ApplyPaging(queryObj);

        //    result.Items = query.ToList();

        //    var viewModel = new QueryResultViewModel<string>
        //    {
        //        Items = result.Items,
        //        TotalItems = result.TotalItems
        //    };

        //    return Ok(viewModel);
        //}

        [HttpGet("[action]")]
        public IActionResult GetServiceNodeList([FromQuery]ServiceNodeQueryViewModel filterViewModel)
        {
            var result = new ServiceNodeQueryResult<Core.Models.ServiceNodeInfoResponse>();
            List<ShowConfigsResponse> configs;
            List<BlocknetLib.Services.Coins.Blocknet.Xrouter.ServiceNodeInfoResponse> servicenodes;
            try
            {
                configs = xrouterService.xrShowConfigs();
                servicenodes = servicenodeService.serviceNodeList();
            }
            catch (RpcInternalServerErrorException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonRpcXrError
                {
                    Error = e.Message,
                    Code = (int)e.RpcErrorCode.Value
                });
            }
            catch (RpcRequestTimeoutException e)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new JsonRpcXrError
                {
                    Error = e.Message
                });
            }
            var xrouterEnabledServicenodes = servicenodes.Join(configs, sn => sn.SNodeKey, c => c.NodePubKey, (sn, c) => new {
                Config = c, ServiceNode = sn
            });
            
            var query = xrouterEnabledServicenodes.Select(q => {
                var serviceNodeConfigElements = q.Config.Config.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Split('='));
                string type = "Regular";
                if(serviceNodeConfigElements.Any(lc => lc[0] == "port"))
                {
                     type = (serviceNodeConfigElements.FirstOrDefault(e => e[0] == "port")[1] != "41412") ? "Enterprise" : type;
                }

                return new Core.Models.ServiceNodeInfoResponse
                { 
                    Type = type,
                    Address = q.ServiceNode.Address,
                    Score = q.ServiceNode.Score,
                    SNodeKey = q.ServiceNode.SNodeKey,
                    Status = q.ServiceNode.Status,
                    Tier = q.ServiceNode.Tier,
                    TimeLastSeen = q.ServiceNode.TimeLastSeen,
                    TimeLastSeenStr = q.ServiceNode.TimeLastSeenStr,
                    SpvWallets = q.ServiceNode.Services.Where(xw => xw.Split(':')[0].Equals("xr")).Where(xw => !xw.Equals("xr")).ToList(),
                    XCloudServices = q.ServiceNode.Services.Where(xw => xw.Split(':')[0].Equals("xrs")).Where(xw => !xw.Equals("xrs")).ToList()
                };
            }).AsQueryable();

            if(filterViewModel.Page != null && filterViewModel.PageSize != null)
            {
                var queryObj = new ServiceNodeQuery
                {
                    Page = (int) filterViewModel.Page,
                    PageSize = (byte) filterViewModel.PageSize,
                    Type = filterViewModel.Type,
                    SpvWallet = filterViewModel.SpvWallet,
                    XCloudService = filterViewModel.XCloudService,
                    Search = filterViewModel.Search
                };
                query = query.ApplyServiceNodeFiltering(queryObj);

                result.TotalItems = query.Count();

                result.TotalSpvWallets = query.SelectMany(sn => sn.SpvWallets)
                    .Distinct()
                    .Count();
                result.TotalXCloudServices = query.SelectMany(sn => sn.XCloudServices)
                    .Distinct()
                    .Count();

                query = query.ApplyPaging(queryObj);
            } 
            else
            {
                result.TotalItems = query.Count();

                result.TotalSpvWallets = query.SelectMany(sn => sn.SpvWallets)
                    .Distinct()
                    .Count();
                result.TotalXCloudServices = query.SelectMany(sn => sn.XCloudServices)
                    .Distinct()
                    .Count();
            }

            result.Items = query.ToList();

            //TODO: Add an automapper module
            var viewModel = new ServiceNodeQueryResultViewModel<ServiceNodeViewModel>
            {
                Items = result.Items.Select(sn => new ServiceNodeViewModel
                {
                    Type = sn.Type,
                    Score = sn.Score,
                    Tier = sn.Tier,
                    TimeLastSeen = sn.TimeLastSeen,
                    TimeLastSeenStr = sn.TimeLastSeenStr,
                    Address = sn.Address,
                    SNodeKey = sn.SNodeKey,
                    Status = sn.Status,
                    SpvWallets = sn.SpvWallets,
                    XCloudServices = sn.XCloudServices

                }).ToList(),
                TotalItems = result.TotalItems,
                TotalSpvWallets = result.TotalSpvWallets,
                TotalXCloudServices = result.TotalXCloudServices
            };

            return Ok(viewModel);
        }

        [HttpGet("[action]")]
        public IActionResult Test()
        {
            return Ok("Test");
        }
    }
}
