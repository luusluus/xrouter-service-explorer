// Copyright (c) 2014 - 2016 George Kimionis
// See the accompanying file LICENSE for the Software License Aggrement

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BlocknetLib.Requests.AddNode;
using BlocknetLib.Requests.CreateRawTransaction;
using BlocknetLib.Requests.SignRawTransaction;
using BlocknetLib.Responses;
using BlocknetLib.RPC.Connector;
using BlocknetLib.RPC.Specifications;
using BlocknetLib.Services.Coins.Base;
using Newtonsoft.Json.Linq;
using Xrouter.Service.Explorer.BitcoinLib.Services;

namespace BlocknetLib.Services
{
    //   Implementation of API calls list, as found at: https://en.bitcoin.it/wiki/Original_Bitcoin_client/API_Calls_list (note: this list is often out-of-date so call "help" in your bitcoin-cli to get the latest signatures)
    public partial class CoinService : ICoinService
    {
        protected readonly IRpcConnector _rpcConnector;

        public CoinService()
        {
            _rpcConnector = new RpcConnector(this);
            Parameters = new CoinParameters(this, null, null, null, null, 0);
        }

        public CoinService(bool useTestnet) : this()
        {
            Parameters.UseTestnet = useTestnet;
        }

        public CoinService(string daemonUrl, string rpcUsername, string rpcPassword, string walletPassword)
        {
            _rpcConnector = new RpcConnector(this);
            Parameters = new CoinParameters(this, daemonUrl, rpcUsername, rpcPassword, walletPassword, 0);
        }

        //  this provides support for cases where *.config files are not an option
        public CoinService(string daemonUrl, string rpcUsername, string rpcPassword, string walletPassword, short rpcRequestTimeoutInSeconds)
        {
            _rpcConnector = new RpcConnector(this);
            Parameters = new CoinParameters(this, daemonUrl, rpcUsername, rpcPassword, walletPassword, rpcRequestTimeoutInSeconds);
        }

        public string AddMultiSigAddress(int nRquired, List<string> publicKeys, string account)
        {
            return account != null
                ? _rpcConnector.MakeRequest<string>(RpcMethods.addmultisigaddress, nRquired, publicKeys, account)
                : _rpcConnector.MakeRequest<string>(RpcMethods.addmultisigaddress, nRquired, publicKeys);
        }

        public void AddNode(string node, NodeAction action)
        {
            _rpcConnector.MakeRequest<string>(RpcMethods.addnode, node, action.ToString());
        }

        public void BackupWallet(string destination)
        {
            _rpcConnector.MakeRequest<string>(RpcMethods.backupwallet, destination);
        }

        public CreateMultiSigResponse CreateMultiSig(int nRquired, List<string> publicKeys)
        {
            return _rpcConnector.MakeRequest<CreateMultiSigResponse>(RpcMethods.createmultisig, nRquired, publicKeys);
        }

        public string CreateRawTransaction(CreateRawTransactionRequest rawTransaction)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.createrawtransaction, rawTransaction.Inputs, rawTransaction.Outputs);
        }

				/// <summary>
				/// Lower level CreateRawTransaction RPC request to allow other kinds of output, e.g.
				/// "data":"text" for OP_RETURN Null Data for chat on the blockchain. CreateRawTransaction(
				/// CreateRawTransactionRequest) only allows for "receiver":amount outputs.
				/// </summary>
				public string CreateRawTransaction(IList<CreateRawTransactionInput> inputs,
					string chatHex, string receiverAddress, decimal receiverAmount)
				{
					// Must be a dictionary to become an json object, an array will fail on the RPC side
					var outputs = new Dictionary<string, string>
					{
						{ "data", chatHex },
						{ receiverAddress, receiverAmount.ToString(NumberFormatInfo.InvariantInfo) }
					};
					return _rpcConnector.MakeRequest<string>(RpcMethods.createrawtransaction, inputs, outputs);
				}

        public DecodeRawTransactionResponse DecodeRawTransaction(string rawTransactionHexString)
        {
            return _rpcConnector.MakeRequest<DecodeRawTransactionResponse>(RpcMethods.decoderawtransaction, rawTransactionHexString);
        }

        public DecodeScriptResponse DecodeScript(string hexString)
        {
            return _rpcConnector.MakeRequest<DecodeScriptResponse>(RpcMethods.decodescript, hexString);
        }

        public string DumpPrivKey(string bitcoinAddress)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.dumpprivkey, bitcoinAddress);
        }

        public void DumpWallet(string filename)
        {
            _rpcConnector.MakeRequest<string>(RpcMethods.dumpwallet, filename);
        }

        public EstimateSmartFeeResponse EstimateSmartFee(ushort nBlocks)
        {
            return _rpcConnector.MakeRequest<EstimateSmartFeeResponse>(RpcMethods.estimatesmartfee, nBlocks);
        }

        public GetAddedNodeInfoResponse GetAddedNodeInfo(string dns, string node)
        {
            return string.IsNullOrWhiteSpace(node)
                ? _rpcConnector.MakeRequest<GetAddedNodeInfoResponse>(RpcMethods.getaddednodeinfo, dns)
                : _rpcConnector.MakeRequest<GetAddedNodeInfoResponse>(RpcMethods.getaddednodeinfo, dns, node);
        }

        public Dictionary<string, GetAddressesByLabelResponse> GetAddressesByLabel(string label)
        {
            return _rpcConnector.MakeRequest<Dictionary<string, GetAddressesByLabelResponse>>(RpcMethods.getaddressesbylabel, label);
        }

        public GetAddressInfoResponse GetAddressInfo(string bitcoinAddress)
        {
            return _rpcConnector.MakeRequest<GetAddressInfoResponse>(RpcMethods.getaddressinfo, bitcoinAddress);
        }

        public decimal GetBalance(string account, int minConf, bool? includeWatchonly)
        {
            return includeWatchonly == null
                ? _rpcConnector.MakeRequest<decimal>(RpcMethods.getbalance, (string.IsNullOrWhiteSpace(account) ? "*" : account), minConf)
                : _rpcConnector.MakeRequest<decimal>(RpcMethods.getbalance, (string.IsNullOrWhiteSpace(account) ? "*" : account), minConf, includeWatchonly);
        }

        public string GetBestBlockHash()
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.getbestblockhash);
        }

        public GetBlockResponse GetBlock(string hash, bool verbose)
        {
            return _rpcConnector.MakeRequest<GetBlockResponse>(RpcMethods.getblock, hash, verbose);
        }

        public GetBlockchainInfoResponse GetBlockchainInfo()
        {
            return _rpcConnector.MakeRequest<GetBlockchainInfoResponse>(RpcMethods.getblockchaininfo);
        }

        public uint GetBlockCount()
        {
            return _rpcConnector.MakeRequest<uint>(RpcMethods.getblockcount);
        }

        public string GetBlockHash(long index)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.getblockhash, index);
        }

        public GetBlockTemplateResponse GetBlockTemplate(params object[] parameters)
        {
            return parameters == null
                ? _rpcConnector.MakeRequest<GetBlockTemplateResponse>(RpcMethods.getblocktemplate)
                : _rpcConnector.MakeRequest<GetBlockTemplateResponse>(RpcMethods.getblocktemplate, parameters);
        }

        public List<GetChainTipsResponse> GetChainTips()
        {
            return _rpcConnector.MakeRequest<List<GetChainTipsResponse>>(RpcMethods.getchaintips);
        }

        public int GetConnectionCount()
        {
            return _rpcConnector.MakeRequest<int>(RpcMethods.getconnectioncount);
        }

        public double GetDifficulty()
        {
            return _rpcConnector.MakeRequest<double>(RpcMethods.getdifficulty);
        }

        public GetMemPoolInfoResponse GetMemPoolInfo()
        {
            return _rpcConnector.MakeRequest<GetMemPoolInfoResponse>(RpcMethods.getmempoolinfo);
        }

        public GetMiningInfoResponse GetMiningInfo()
        {
            return _rpcConnector.MakeRequest<GetMiningInfoResponse>(RpcMethods.getmininginfo);
        }

        public GetNetTotalsResponse GetNetTotals()
        {
            return _rpcConnector.MakeRequest<GetNetTotalsResponse>(RpcMethods.getnettotals);
        }

        public ulong GetNetworkHashPs(uint blocks, long height)
        {
            return _rpcConnector.MakeRequest<ulong>(RpcMethods.getnetworkhashps);
        }

        public GetNetworkInfoResponse GetNetworkInfo()
        {
            return _rpcConnector.MakeRequest<GetNetworkInfoResponse>(RpcMethods.getnetworkinfo);
        }

        public string GetNewAddress(string account)
        {
            return string.IsNullOrWhiteSpace(account)
                ? _rpcConnector.MakeRequest<string>(RpcMethods.getnewaddress)
                : _rpcConnector.MakeRequest<string>(RpcMethods.getnewaddress, account);
        }

        public List<GetPeerInfoResponse> GetPeerInfo()
        {
            return _rpcConnector.MakeRequest<List<GetPeerInfoResponse>>(RpcMethods.getpeerinfo);
        }

        public GetRawMemPoolResponse GetRawMemPool(bool verbose)
        {
            var getRawMemPoolResponse = new GetRawMemPoolResponse
            {
                IsVerbose = verbose
            };

            var rpcResponse = _rpcConnector.MakeRequest<object>(RpcMethods.getrawmempool, verbose);

            if (!verbose)
            {
                var rpcResponseAsArray = (JArray) rpcResponse;

                foreach (string txId in rpcResponseAsArray)
                {
                    getRawMemPoolResponse.TxIds.Add(txId);
                }

                return getRawMemPoolResponse;
            }

            IList<KeyValuePair<string, JToken>> rpcResponseAsKvp = (new EnumerableQuery<KeyValuePair<string, JToken>>(((JObject) (rpcResponse)))).ToList();
            IList<JToken> children = JObject.Parse(rpcResponse.ToString()).Children().ToList();

            for (var i = 0; i < children.Count(); i++)
            {
                var getRawMemPoolVerboseResponse = new GetRawMemPoolVerboseResponse
                {
                    TxId = rpcResponseAsKvp[i].Key
                };

                getRawMemPoolResponse.TxIds.Add(getRawMemPoolVerboseResponse.TxId);

                foreach (var property in children[i].SelectMany(grandChild => grandChild.OfType<JProperty>()))
                {
                    switch (property.Name)
                    {
                        case "currentpriority":

                            double currentPriority;

                            if (double.TryParse(property.Value.ToString(), out currentPriority))
                            {
                                getRawMemPoolVerboseResponse.CurrentPriority = currentPriority;
                            }

                            break;

                        case "depends":

                            foreach (var jToken in property.Value)
                            {
                                getRawMemPoolVerboseResponse.Depends.Add(jToken.Value<string>());
                            }

                            break;

                        case "fee":

                            decimal fee;

                            if (decimal.TryParse(property.Value.ToString(), out fee))
                            {
                                getRawMemPoolVerboseResponse.Fee = fee;
                            }

                            break;

                        case "height":

                            int height;

                            if (int.TryParse(property.Value.ToString(), out height))
                            {
                                getRawMemPoolVerboseResponse.Height = height;
                            }

                            break;

                        case "size":

                            int size;

                            if (int.TryParse(property.Value.ToString(), out size))
                            {
                                getRawMemPoolVerboseResponse.Size = size;
                            }

                            break;

                        case "startingpriority":

                            double startingPriority;

                            if (double.TryParse(property.Value.ToString(), out startingPriority))
                            {
                                getRawMemPoolVerboseResponse.StartingPriority = startingPriority;
                            }

                            break;

                        case "time":

                            int time;

                            if (int.TryParse(property.Value.ToString(), out time))
                            {
                                getRawMemPoolVerboseResponse.Time = time;
                            }

                            break;

                        default:

                            throw new Exception("Unkown property: " + property.Name + " in GetRawMemPool()");
                    }
                }
                getRawMemPoolResponse.VerboseResponses.Add(getRawMemPoolVerboseResponse);
            }
            return getRawMemPoolResponse;
        }

        public string GetRawChangeAddress()
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.getrawchangeaddress);
        }

        public GetRawTransactionResponse GetRawTransaction(string txId, int verbose)
        {
            if (verbose == 0)
            {
                return new GetRawTransactionResponse
                {
                    Hex = _rpcConnector.MakeRequest<string>(RpcMethods.getrawtransaction, txId, verbose)
                };
            }

            if (verbose == 1)
            {
                return _rpcConnector.MakeRequest<GetRawTransactionResponse>(RpcMethods.getrawtransaction, txId, verbose);
            }

            throw new Exception("Invalid verbose value: " + verbose + " in GetRawTransaction()!");
        }

        public decimal GetReceivedByAddress(string bitcoinAddress, int minConf)
        {
            return _rpcConnector.MakeRequest<decimal>(RpcMethods.getreceivedbyaddress, bitcoinAddress, minConf);
        }

        public decimal GetReceivedByLabel(string bitcoinAddress, int minConf)
        {
            return _rpcConnector.MakeRequest<decimal>(RpcMethods.getreceivedbylabel, bitcoinAddress, minConf);
        }

        public GetTransactionResponse GetTransaction(string txId, bool? includeWatchonly)
        {
            return includeWatchonly == null
                ? _rpcConnector.MakeRequest<GetTransactionResponse>(RpcMethods.gettransaction, txId)
                : _rpcConnector.MakeRequest<GetTransactionResponse>(RpcMethods.gettransaction, txId, includeWatchonly);
        }

        public GetTransactionResponse GetTxOut(string txId, int n, bool includeMemPool)
        {
            return _rpcConnector.MakeRequest<GetTransactionResponse>(RpcMethods.gettxout, txId, n, includeMemPool);
        }

        public GetTxOutSetInfoResponse GetTxOutSetInfo()
        {
            return _rpcConnector.MakeRequest<GetTxOutSetInfoResponse>(RpcMethods.gettxoutsetinfo);
        }

        public decimal GetUnconfirmedBalance()
        {
            return _rpcConnector.MakeRequest<decimal>(RpcMethods.getunconfirmedbalance);
        }

        public GetWalletInfoResponse GetWalletInfo()
        {
            return _rpcConnector.MakeRequest<GetWalletInfoResponse>(RpcMethods.getwalletinfo);
        }

        public string Help(string command)
        {
            return string.IsNullOrWhiteSpace(command)
                ? _rpcConnector.MakeRequest<string>(RpcMethods.help)
                : _rpcConnector.MakeRequest<string>(RpcMethods.help, command);
        }

        public void ImportAddress(string address, string label, bool rescan)
        {
            _rpcConnector.MakeRequest<string>(RpcMethods.importaddress, address, label, rescan);
        }

        public string ImportPrivKey(string privateKey, string label, bool rescan)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.importprivkey, privateKey, label, rescan);
        }

        public void ImportWallet(string filename)
        {
            _rpcConnector.MakeRequest<string>(RpcMethods.importwallet, filename);
        }

        public string KeyPoolRefill(uint newSize)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.keypoolrefill, newSize);
        }

        public List<List<ListAddressGroupingsResponse>> ListAddressGroupings()
        {
            var unstructuredResponse = _rpcConnector.MakeRequest<List<List<List<object>>>>(RpcMethods.listaddressgroupings);
            var structuredResponse = new List<List<ListAddressGroupingsResponse>>(unstructuredResponse.Count);

            for (var i = 0; i < unstructuredResponse.Count; i++)
            {
                for (var j = 0; j < unstructuredResponse[i].Count; j++)
                {
                    if (unstructuredResponse[i][j].Count > 1)
                    {
                        var response = new ListAddressGroupingsResponse
                        {
                            Address = unstructuredResponse[i][j][0].ToString()
                        };

                        decimal balance;
                        if (decimal.TryParse(unstructuredResponse[i][j][1].ToString(), out balance))
                        {
                            response.Balance = balance;
                        }

                        if (unstructuredResponse[i][j].Count > 2)
                        {
                            response.Account = unstructuredResponse[i][j][2].ToString();
                        }

                        if (structuredResponse.Count < i + 1)
                        {
                            structuredResponse.Add(new List<ListAddressGroupingsResponse>());
                        }

                        structuredResponse[i].Add(response);
                    }
                }
            }
            return structuredResponse;
        }

        public List<string> ListLabels()
        {
            return _rpcConnector.MakeRequest<List<string>>(RpcMethods.listlabels);
        }

        public string ListLockUnspent()
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.listlockunspent);
        }

        public List<ListReceivedByAddressResponse> ListReceivedByAddress(int minConf, bool includeEmpty, bool? includeWatchonly)
        {
            return includeWatchonly == null
                ? _rpcConnector.MakeRequest<List<ListReceivedByAddressResponse>>(RpcMethods.listreceivedbyaddress, minConf, includeEmpty)
                : _rpcConnector.MakeRequest<List<ListReceivedByAddressResponse>>(RpcMethods.listreceivedbyaddress, minConf, includeEmpty, includeWatchonly);
        }

        public List<ListReceivedByLabelResponse> ListReceivedByLabel(int minConf, bool includeEmpty, bool? includeWatchonly)
        {
            return _rpcConnector.MakeRequest<List<ListReceivedByLabelResponse>>(RpcMethods.listreceivedbylabel, minConf, includeEmpty, includeWatchonly);
        }

        public ListSinceBlockResponse ListSinceBlock(string blockHash, int targetConfirmations, bool? includeWatchonly)
        {
            return includeWatchonly == null
                ? _rpcConnector.MakeRequest<ListSinceBlockResponse>(RpcMethods.listsinceblock, (string.IsNullOrWhiteSpace(blockHash) ? "" : blockHash), targetConfirmations)
                : _rpcConnector.MakeRequest<ListSinceBlockResponse>(RpcMethods.listsinceblock, (string.IsNullOrWhiteSpace(blockHash) ? "" : blockHash), targetConfirmations, includeWatchonly);
        }

        public List<ListTransactionsResponse> ListTransactions(string account, int count, int from, bool? includeWatchonly)
        {
            return includeWatchonly == null
                ? _rpcConnector.MakeRequest<List<ListTransactionsResponse>>(RpcMethods.listtransactions, (string.IsNullOrWhiteSpace(account) ? "*" : account), count, from)
                : _rpcConnector.MakeRequest<List<ListTransactionsResponse>>(RpcMethods.listtransactions, (string.IsNullOrWhiteSpace(account) ? "*" : account), count, from, includeWatchonly);
        }

        public List<ListUnspentResponse> ListUnspent(int minConf, int maxConf, List<string> addresses)
        {
            return _rpcConnector.MakeRequest<List<ListUnspentResponse>>(RpcMethods.listunspent, minConf, maxConf, (addresses ?? new List<string>()));
        }

        public bool LockUnspent(bool unlock, IList<ListUnspentResponse> listUnspentResponses)
        {
            IList<object> transactions = new List<object>();

            foreach (var listUnspentResponse in listUnspentResponses)
            {
                transactions.Add(new
                {
                    txid = listUnspentResponse.TxId, vout = listUnspentResponse.Vout
                });
            }

            return _rpcConnector.MakeRequest<bool>(RpcMethods.lockunspent, unlock, transactions.ToArray());
        }

        public void Ping()
        {
            _rpcConnector.MakeRequest<string>(RpcMethods.ping);
        }

        public bool PrioritiseTransaction(string txId, decimal priorityDelta, decimal feeDelta)
        {
            return _rpcConnector.MakeRequest<bool>(RpcMethods.prioritisetransaction, txId, priorityDelta, feeDelta);
        }

        public string SendMany(string fromAccount, Dictionary<string, decimal> toBitcoinAddress, int minConf, string comment)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.sendmany, fromAccount, toBitcoinAddress, minConf, comment);
        }

        public string SendRawTransaction(string rawTransactionHexString, bool? allowHighFees)
        {
            return allowHighFees == null
                ? _rpcConnector.MakeRequest<string>(RpcMethods.sendrawtransaction, rawTransactionHexString)
                : _rpcConnector.MakeRequest<string>(RpcMethods.sendrawtransaction, rawTransactionHexString, allowHighFees);
        }

        public string SendToAddress(string bitcoinAddress, decimal amount, string comment, string commentTo, bool subtractFeeFromAmount)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.sendtoaddress, bitcoinAddress, amount, comment, commentTo, subtractFeeFromAmount);
        }


        public string SetLabel(string bitcoinAddress, string label)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.setlabel, bitcoinAddress, label);
        }

        public string SetTxFee(decimal amount)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.settxfee, amount);
        }

        public string SignMessage(string bitcoinAddress, string message)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.signmessage, bitcoinAddress, message);
        }

        public SignRawTransactionWithKeyResponse SignRawTransactionWithKey(SignRawTransactionWithKeyRequest request)
        {
            #region default values

            if (request.PrivateKeys.Count == 0)
            {
                request.PrivateKeys = null;
            }

            if (request.Inputs.Count == 0)
            {
                request.Inputs = null;
            }

            if (string.IsNullOrWhiteSpace(request.SigHashType))
            {
                request.SigHashType = SigHashType.All;
            }

            #endregion

            return _rpcConnector.MakeRequest<SignRawTransactionWithKeyResponse>(RpcMethods.signrawtransactionwithkey, request.RawTransactionHex, request.PrivateKeys, request.Inputs, request.SigHashType);
        }

        public SignRawTransactionWithWalletResponse SignRawTransactionWithWallet(SignRawTransactionWithWalletRequest request)
        {
            #region default values

            if (request.Inputs.Count == 0)
            {
                request.Inputs = null;
            }

            if (string.IsNullOrWhiteSpace(request.SigHashType))
            {
                request.SigHashType = SigHashType.All;
            }

            #endregion

            return _rpcConnector.MakeRequest<SignRawTransactionWithWalletResponse>(RpcMethods.signrawtransactionwithwallet, request.RawTransactionHex, request.Inputs, request.SigHashType);
        }

        public GetFundRawTransactionResponse GetFundRawTransaction(string rawTransactionHex)
        {
            return _rpcConnector.MakeRequest<GetFundRawTransactionResponse>(RpcMethods.fundrawtransaction, rawTransactionHex);
        }

        public string Stop()
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.stop);
        }

        public string SubmitBlock(string hexData, params object[] parameters)
        {
            return parameters == null
                ? _rpcConnector.MakeRequest<string>(RpcMethods.submitblock, hexData)
                : _rpcConnector.MakeRequest<string>(RpcMethods.submitblock, hexData, parameters);
        }

        public ValidateAddressResponse ValidateAddress(string bitcoinAddress)
        {
            return _rpcConnector.MakeRequest<ValidateAddressResponse>(RpcMethods.validateaddress, bitcoinAddress);
        }

        public bool VerifyChain(ushort checkLevel, uint numBlocks)
        {
            return _rpcConnector.MakeRequest<bool>(RpcMethods.verifychain, checkLevel, numBlocks);
        }

        public bool VerifyMessage(string bitcoinAddress, string signature, string message)
        {
            return _rpcConnector.MakeRequest<bool>(RpcMethods.verifymessage, bitcoinAddress, signature, message);
        }

        public string WalletLock()
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.walletlock);
        }

        public string WalletPassphrase(string passphrase, int timeoutInSeconds)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.walletpassphrase, passphrase, timeoutInSeconds);
        }

        public string WalletPassphraseChange(string oldPassphrase, string newPassphrase)
        {
            return _rpcConnector.MakeRequest<string>(RpcMethods.walletpassphrasechange, oldPassphrase, newPassphrase);
        }

        public override string ToString()
        {
            return Parameters.CoinLongName;
        }
    }
}