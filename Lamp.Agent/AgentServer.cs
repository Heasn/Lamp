using System.Threading.Tasks;
using DotNetty.Common.Internal.Logging;
using Lamp.Agent.Server;

namespace Lamp.Agent
{
    class AgentServer
    {
        private readonly LampTcpServer mTcpServer;
        private readonly LampUdpServer mUdpServer;

        private readonly IInternalLogger mLogger = InternalLoggerFactory.GetInstance<AgentServer>();

        public AgentServer(int port)
        {
            mTcpServer = new LampTcpServer(port);
            mUdpServer = new LampUdpServer(port);
        }

        public async Task Run()
        {
            mLogger.Info("正在启动TCPServer");
            await mTcpServer.Run();
            mLogger.Info("TCPServer启动完成");

            mLogger.Info("正在启动UDPServer");
            await mUdpServer.Run();
            mLogger.Info("UDPServer启动完成");
        }

        public async Task Stop()
        {
            mLogger.Info("正在停止TCPServer");
            await mTcpServer.Stop();
            mLogger.Info("TCPServer停止完成");

            mLogger.Info("正在停止UDPServer");
            await mUdpServer.Stop();
            mLogger.Info("UDPServer停止完成");
        }
    }
}
