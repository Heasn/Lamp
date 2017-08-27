using System.Net;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging.Console;

namespace Lamp.Network.Server
{
    public abstract class BedRockUdpServer
    {
        private IEventLoopGroup mGroup;
        private IChannel mBootstrapChannel;

        private volatile bool mRunning;

        private readonly int mPort;

        protected BedRockUdpServer(int port)
        {
            mPort = port;
        }

        public async Task Run()
        {
            if (mRunning)
                return;

            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));

            mGroup = new MultithreadEventLoopGroup();

            var bootstrap = new Bootstrap();
            bootstrap
                .Group(mGroup)
                .Channel<SocketDatagramChannel>()
                .Option(ChannelOption.SoBroadcast, true)
                .Handler(new ActionChannelInitializer<SocketDatagramChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;

                    pipeline.AddLast(new BedRockUdpServerHandler());
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
