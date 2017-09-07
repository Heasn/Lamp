#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Utilities
// 文件名：ApplicationLogging.cs
// 创建日期：2017-09-05

#endregion

using Microsoft.Extensions.Logging;

namespace Lamp.Utilities
{
    public static class ApplicationLogging
    {
        static ApplicationLogging()
        {
            LoggerFactory.AddConsole(true);
        }

        public static ILoggerFactory LoggerFactory { get; } = new LoggerFactory();

        public static ILogger CreateLogger<T>()
        {
            return LoggerFactory.CreateLogger<T>();
        }
    }
}