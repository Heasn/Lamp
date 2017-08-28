using System.Net;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Lamp.Agent.KcpLib;

namespace Lamp.Agent.Server
{
    public class Session
    {
        public static AttributeKey<Session> SessionIdentity { get; } = AttributeKey<Session>.ValueOf("SessionIdentity");

        private Kcp mKcp;
        private IChannel mIChannel;
        private EndPoint mLocalAddress;

        private Session()
        {

        }

        public static Session Create(IChannel channel, EndPoint endPoint, int sessionId)
        {
            var session = new Session
            {
                mIChannel = channel
            };

            session.mKcp = new Kcp((buf, kcp, user) =>
            {
                var packet = new DatagramPacket(buf, user, session.mIChannel.LocalAddress);
                session.mIChannel.WriteAndFlushAsync(packet);
            }, endPoint);

            session.mKcp.NoDelay(1, 10, 2, 1);
            session.mKcp.WndSize(128, 128);
            session.mKcp.SetConv(sessionId);

            return session;
        }

        public void RecvData(IByteBuffer buf,out IByteBuffer outBuffer)
        {
            buf = buf.WithOrder(ByteOrder.LittleEndian);

            mKcp.Input(buf);

            outBuffer = PooledByteBufferAllocator.Default.Buffer().WithOrder(ByteOrder.LittleEndian);

            for (var size = mKcp.PeekSize(); size > 0; size = mKcp.PeekSize())
            {
                if (mKcp.Receive(outBuffer) > 0)
                {
                    
                }
            }
        }
    }
}
