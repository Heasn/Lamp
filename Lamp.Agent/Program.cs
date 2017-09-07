#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：Program.cs
// 创建日期：2017-09-01

#endregion

using System;
using DotNetty.Common.Internal.Logging;
using Lamp.Utilities;
using Microsoft.Extensions.Logging;

namespace Lamp.Agent
{
    internal class Program
    {
        private static readonly ILogger logger = ApplicationLogging.CreateLogger<Program>();

        private static void Main(string[] args)
        {
            InternalLoggerFactory.DefaultFactory = ApplicationLogging.LoggerFactory;

            logger.LogInformation("开始启动服务器");
            var server = new AgentServer(8686);
            server.Run().Wait();
            logger.LogInformation("服务器启动完成");

            Console.ReadLine();
            logger.LogInformation("正在关闭服务器");
        }
    }
}