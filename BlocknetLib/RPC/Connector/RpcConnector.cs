// Copyright (c) 2014 - 2016 George Kimionis
// See the accompanying file LICENSE for the Software License Aggrement

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using BlocknetLib.Auxiliary;
using BlocknetLib.ExceptionHandling.Rpc;
using BlocknetLib.ExtensionMethods;
using BlocknetLib.RPC.RequestResponse;
using BlocknetLib.RPC.Specifications;
using BlocknetLib.Services.Coins.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace BlocknetLib.RPC.Connector
{
    public sealed class RpcConnector : IRpcConnector
    {
        private readonly ICoinService _coinService;

        public RpcConnector(ICoinService coinService)
        {
            _coinService = coinService;
        }

        public T MakeRequest<T>(RpcMethods rpcMethod, params object[] parameters)
        {
            if(RpcMethods.xrGetTransactions == rpcMethod || RpcMethods.xrGetBlocks == rpcMethod || RpcMethods.xrService == rpcMethod)
            {
                var parameterList = new List<object>();
                foreach (var param in parameters)
                {
                    if(param != null)
                    {
                        if(!param.GetType().IsArray)
                            parameterList.Add(param);
                        else
                            foreach (var p in (IEnumerable) param) 
                                parameterList.Add(p);
                    }
                }
                parameters = parameterList.ToArray();
            }
            

            var jsonRpcRequest = new JsonRpcRequest(1, rpcMethod.ToString(), parameters);
            var webRequest = (HttpWebRequest) WebRequest.Create(_coinService.Parameters.SelectedDaemonUrl);
            SetBasicAuthHeader(webRequest, _coinService.Parameters.RpcUsername, _coinService.Parameters.RpcPassword);
            webRequest.Credentials = new NetworkCredential(_coinService.Parameters.RpcUsername, _coinService.Parameters.RpcPassword);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";
            webRequest.Proxy = null;
            webRequest.Timeout = _coinService.Parameters.RpcRequestTimeoutInSeconds * GlobalConstants.MillisecondsInASecond;
            var byteArray = jsonRpcRequest.GetBytes();
            webRequest.ContentLength = jsonRpcRequest.GetBytes().Length;

            try
            {
                using (var dataStream = webRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Dispose();
                }
            }
            catch (Exception exception)
            {
                throw new RpcException("There was a problem sending the request to the wallet", exception);
            }

            try
            {
                string json;
                Console.WriteLine("Executing RPC Call: " + rpcMethod.ToString());
                var timer = new Stopwatch();
                timer.Start();
                using (var webResponse = webRequest.GetResponse())
                {
                    using (var stream = webResponse.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var result = reader.ReadToEnd();
                            reader.Dispose();
                            json = result;
                        }
                    }
                }
                timer.Stop();
                Console.WriteLine("Rpc Call Time Elapsed: {0} ms", timer.ElapsedMilliseconds); 

                var rpcResponse = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(json);

                var errorProperty = rpcResponse.Result.GetType().GetProperty("Error");

                if (errorProperty != null)
                {
                    var errorValue = (JsonRpcError)errorProperty.GetValue(rpcResponse.Result);
                    if (errorValue != null)
                    {
                        var internalServerErrorException = new RpcInternalServerErrorException(errorValue.Message)
                        {
                            RpcErrorCode = errorValue.Code
                        };
                        internalServerErrorException.Data["rpcResponse"] = rpcResponse.Result;
                        throw internalServerErrorException;
                    }
                }
                
                return rpcResponse.Result;
            }
            catch (WebException webException)
            {
                #region RPC Internal Server Error (with an Error Code)

                var webResponse = webException.Response as HttpWebResponse;

                if (webResponse != null)
                {
                    switch (webResponse.StatusCode)
                    {
                        case HttpStatusCode.InternalServerError:
                        {
                            using (var stream = webResponse.GetResponseStream())
                            {
                                if (stream == null)
                                {
                                    throw new RpcException("The RPC request was either not understood by the server or there was a problem executing the request", webException);
                                }

                                using (var reader = new StreamReader(stream))
                                {
                                    var result = reader.ReadToEnd();
                                    reader.Dispose();

                                    try
                                    {
                                        var jsonRpcResponseObject = JsonConvert.DeserializeObject<JsonRpcResponse<object>>(result);

                                        var internalServerErrorException = new RpcInternalServerErrorException(jsonRpcResponseObject.Error.Message, webException)
                                        {
                                            RpcErrorCode = jsonRpcResponseObject.Error.Code
                                        };

                                        throw internalServerErrorException;
                                    }
                                    catch (JsonException)
                                    {
                                        throw new RpcException(result, webException);
                                    }
                                }
                            }
                        }

                        default:
                            throw new RpcException("The RPC request was either not understood by the server or there was a problem executing the request", webException);
                    }
                }

                #endregion

                #region RPC Time-Out

                if (webException.Message == "The operation has timed out.")
                {
                    throw new RpcRequestTimeoutException(webException.Message);
                }

                #endregion

                throw new RpcException("An unknown web exception occured while trying to read the JSON response", webException);
            }
            catch (JsonException jsonException)
            {
                throw new RpcResponseDeserializationException("There was a problem deserializing the response from the wallet", jsonException);
            }
            catch (ProtocolViolationException protocolViolationException)
            {
                throw new RpcException("Unable to connect to the server", protocolViolationException);
            }
            catch (RpcInternalServerErrorException rpcInternalServerErrorException)
            {
                throw new RpcInternalServerErrorException(rpcInternalServerErrorException.Message, rpcInternalServerErrorException);
            }
            catch (Exception exception)
            {
                var queryParameters = jsonRpcRequest.Parameters.Cast<string>().Aggregate(string.Empty, (current, parameter) => current + (parameter + " "));
                throw new Exception($"A problem was encountered while calling MakeRpcRequest() for: {jsonRpcRequest.Method} with parameters: {queryParameters}. \nException: {exception.Message}");
            }
        }

        private static void SetBasicAuthHeader(WebRequest webRequest, string username, string password)
        {
            var authInfo = username + ":" + password;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            webRequest.Headers["Authorization"] = "Basic " + authInfo;
        }
            
    }
}
