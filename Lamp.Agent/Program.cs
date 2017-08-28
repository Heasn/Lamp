using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using DotNetty.Common.Internal.Logging;
using Lamp.Agent.Crypto;
using Lamp.Agent.Crypto.RSA;
using Microsoft.Extensions.Logging.Console;
using System.Diagnostics;
using Lamp.Agent.Crypto.AES;

namespace Lamp.Agent
{
    class Program
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Program>();

        static void Main(string[] args)
        {
            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));

            var server = new AgentServer(8686);
            server.Run().Wait();

            Logger.Info("服务器启动完成");

            Console.ReadLine();
        }
    }
}
