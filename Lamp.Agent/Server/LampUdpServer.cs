using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging.Console;
using System.Threading.Tasks;

namespace Lamp.Agent.Server
{
    public sealed class LampUdpServer
    {
        private IEventLoopGroup mGroup;
        private IChannel mBootstrapChannel;

        private volatile bool mRunning;

        private readonly int mPort;

        public LampUdpServer(int port)
        {
            mPort = port;
        }

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

            mBootstrapChannel = await bootstrap.BindAsync(mPort);

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
