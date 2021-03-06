﻿// Copyright (c) 2014 - 2016 George Kimionis
// See the accompanying file LICENSE for the Software License Aggrement

using System;

namespace BlocknetLib.ExceptionHandling.Rpc
{
    [Serializable]
    public class RpcException : Exception
    {
        public RpcException()
        {
        }

        public RpcException(string customMessage) : base(customMessage)
        {
        }

        public RpcException(string customMessage, Exception exception) : base(customMessage, exception)
        {
        }
    }
}