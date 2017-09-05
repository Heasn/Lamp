#region 文件描述

// 开发者：陈柏宇
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：LampTcpServer.cs
// 创建日期：2017-08-28

#endregion

using System;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Lamp.Agent.Server
{
    public sealed class LampTcpServer
    {
        private readonly int mPort;
        private IChannel mBootstrapChannel;
        private IEventLoopGroup mBossGroup;

        private volatile bool mRunning;
        private IEventLoopGroup mWorkerGroup;

        public LampTcpServer(int port)
        {
            mPort = port;
        }

        public async Task Run()
        {
            if (mRunning)
                return;

            mBossGroup = new MultithreadEventLoopGroup(1);
            mWorkerGroup = new MultithreadEventLoopGroup();

            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(mBossGroup, mWorkerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100)
                .Handler(new LoggingHandler("SRV-LSTN"))
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;

                    pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                    pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                    pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                    pipeline.AddLast("echo", new LampTcpServerHandler());
                }));

            mBootstrapChannel = await bootstrap.BindAsync(mPort);

            mRunning = true;
        }

        public async Task Stop()
        {
            if (!mRunning)
                return;

            if (mBossGroup == null || mWorkerGroup == null || mBootstrapChannel == null)
                return;

            await mBootstrapChannel.CloseAsync();

            await Task.WhenAll(
                mBossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                mWorkerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));

            mRunning = false;
        }
    }
}