#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：Session.cs
// 创建日期：2017-08-28

#endregion

using System.Net;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Lamp.Agent.KcpLib;
using Lamp.Utilities;
using Lamp.Utilities.Crypto.AES;

namespace Lamp.Agent.Server
{
    public class Session
    {
        private AesCryptor mCryptor;
        private IChannel mIChannel;

        private Kcp mKcp;

        private Session()
        {
            var key = new byte[AesBlock.BLOCKSIZE];
            var iv = new byte[AesBlock.BLOCKSIZE];

            Randomizer.GetBytes(key);
            Randomizer.GetBytes(iv);

            var block = new AesBlock(key);
            mCryptor = new AesCryptor(block, iv);
        }

        public static AttributeKey<Session> SessionIdentity { get; } = AttributeKey<Session>.ValueOf("SessionIdentity");

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

        public void RecvData(IByteBuffer buf, out IByteBuffer outBuffer)
        {
            buf = buf.WithOrder(ByteOrder.LittleEndian);

            mKcp.Input(buf);

            outBuffer = PooledByteBufferAllocator.Default.Buffer().WithOrder(ByteOrder.LittleEndian);

            for (var size = mKcp.PeekSize(); size > 0; size = mKcp.PeekSize())
                if (mKcp.Receive(outBuffer) > 0)
                {
                }
        }
    }
}