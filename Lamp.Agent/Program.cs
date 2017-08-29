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
            //InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));

            //var server = new AgentServer(8686);
            //server.Run().Wait();

            //Logger.Info("服务器启动完成");

            byte[] key = new byte[16];
            byte[] iv = new byte[16];

            using (var g = RandomNumberGenerator.Create())
            {
                g.GetNonZeroBytes(key);
                g.GetNonZeroBytes(iv);
            }

            var plain = Encoding.UTF8.GetBytes("123456");
            var ended = new byte[plain.Length];

            AESCrypto a = new AESCrypto(key, iv);

            var stop = new Stopwatch();
            stop.Start();
            a.Encrypt(plain, plain.Length, ended);
            stop.Stop();

            Console.WriteLine(BitConverter.ToString(ended) + "    用时：" + stop.Elapsed);

            Console.ReadLine();
        }
    }
}
