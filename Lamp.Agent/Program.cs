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
using DotNetty.Buffers;
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
            byte[] plain = Encoding.UTF8.GetBytes("123456");
            byte[] encoded = new byte[plain.Length];
            byte[] decoded = new byte[plain.Length];

            using (var g = RandomNumberGenerator.Create())
            {
                g.GetNonZeroBytes(key);
                g.GetNonZeroBytes(iv);
            }
                var buf = PooledByteBufferAllocator.Default.Buffer();

            buf.WriteBytes(plain);

            var aes = new AescfbCrypto(key, iv);
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            aes.Encrypt(buf);
            stopWatch.Stop();

            buf.GetBytes(buf.ReaderIndex, encoded);
            Console.WriteLine(BitConverter.ToString(encoded)+"  "+stopWatch.Elapsed);

            aes.Encrypt(buf);
            buf.GetBytes(buf.ReaderIndex, decoded );

            Console.WriteLine(Encoding.UTF8.GetString(decoded ));

            Console.ReadLine();
        }
    }
}
