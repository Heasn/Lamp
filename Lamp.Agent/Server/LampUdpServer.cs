#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：LampUdpServer.cs
// 创建日期：2017-08-28

#endregion

using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Lamp.Agent.Server
{
    public sealed class LampUdpServer
    {
        private IChannel mBootstrapChannel;
        private IEventLoopGroup mGroup;
        private volatile bool mRunning;

        public LampUdpServer(int port)
        {
            Port = port;
        }

        public int Port { get; }

        public async Task Run()
        {
            if (mRunning)
                return;

            mGroup = new MultithreadEventLoopGroup();

            var bootstrap = new Bootstrap();
            bootstrap
                .Group(mGroup)
                .Channel<SocketDatagramChannel>()
                .Option(ChannelOption.SoBroadcast, true)
                .Handler(new ActionChannelInitializer<SocketDatagramChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;

                    pipeline.AddLast(new LampUdpServerHandler());
                }));

            mBootstrapChannel = await bootstrap.BindAsync(Port);

            mRunning = true;
        }

        public async Task Stop()
        {
            if (!mRunning)
                return;

            if (mGroup == null || mBootstrapChannel == null)
                return;

            await mBootstrapChannel.CloseAsync();
            await mGroup.ShutdownGracefullyAsync();

            mRunning = false;
        }
    }
}