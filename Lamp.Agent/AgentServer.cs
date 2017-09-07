#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：AgentServer.cs
// 创建日期：2017-08-28

#endregion

using System.Threading.Tasks;
using Lamp.Agent.Server;
using Lamp.Utilities;
using Microsoft.Extensions.Logging;

namespace Lamp.Agent
{
    internal class AgentServer
    {
        private readonly ILogger mLogger = ApplicationLogging.CreateLogger<AgentServer>();
        private readonly LampTcpServer mTcpServer;
        private readonly LampUdpServer mUdpServer;

        public AgentServer(int port)
        {
            mTcpServer = new LampTcpServer(port);
            mUdpServer = new LampUdpServer(port);
        }

        public async Task Run()
        {
            mLogger.LogInformation("正在启动TCPServer");
            await mTcpServer.Run();
            mLogger.LogInformation($"TCPServer启动完成，监听端口：{mTcpServer.Port}");

            mLogger.LogInformation("正在启动UDPServer");
            await mUdpServer.Run();
            mLogger.LogInformation($"UDPServer启动完成，绑定地址：{mUdpServer.Port}");
        }

        public async Task Stop()
        {
            mLogger.LogInformation("正在停止TCPServer");
            await mTcpServer.Stop();
            mLogger.LogInformation("TCPServer停止完成");

            mLogger.LogInformation("正在停止UDPServer");
            await mUdpServer.Stop();
            mLogger.LogInformation("UDPServer停止完成");
        }
    }
}