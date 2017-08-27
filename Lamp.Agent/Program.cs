using System;
using Lamp.Network;

namespace Lamp.Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new AgentServer();
            server.Run().Wait();
            Console.WriteLine("启动成功");
            Console.ReadKey();
        }
    }
}
