using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Lamp.Network.KcpLib;

namespace Lamp.Network.Server
{
    abstract class BedRockUdpServer
    {
        private IEventLoopGroup m_Group;
        private IChannel m_BootstrapChannel;

        private IPEndPoint addr;
        private int nodelay;
        private int interval = Kcp.IKCP_INTERVAL;
        private int resend;
        private int nc;
        private int sndwnd = Kcp.IKCP_WND_SND;
        private int rcvwnd = Kcp.IKCP_WND_RCV;
        private int mtu = Kcp.IKCP_MTU_DEF;
        private bool stream;
        private int minRto = Kcp.IKCP_RTO_MIN;
        private volatile bool running;
        private long timeout;

        protected BedRockUdpServer()
        {
        }

        protected async Task Run()
        {
            //InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));

            m_Group = new MultithreadEventLoopGroup();

            var bootstrap = new Bootstrap();
            bootstrap
                .Group(m_Group)
                .Channel<SocketDatagramChannel>()
                .Option(ChannelOption.SoBroadcast, true)
                .Handler(new ActionChannelInitializer<SocketDatagramChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new BedRockUdpServerHandler());
                }));

            m_BootstrapChannel = await bootstrap.BindAsync(8686);
        }

        protected async Task Stop()
        {
            if (m_Group == null || m_BootstrapChannel == null)
                return;

            await m_BootstrapChannel.CloseAsync();
            await m_Group.ShutdownGracefullyAsync();
        }

        public void SendDelegate(IByteBuffer buf, Kcp kcp, object user)
        {
            var packet = new DatagramPacket(buf, (EndPoint)user, addr );
            m_BootstrapChannel.WriteAndFlushAsync(packet);
        }

    }
}
