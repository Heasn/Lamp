#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Utilities
// 文件名：Randomizer.cs
// 创建日期：2017-09-05

#endregion

using System.Security.Cryptography;

namespace Lamp.Utilities
{
    /// <summary>
    ///     随机数发生器
    /// </summary>
    public static class Randomizer
    {
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

        /// <summary>
        ///     用强随机数填充数组
        /// </summary>
        /// <param name="data">需要填充的数组</param>
        public static void GetBytes(byte[]data)
        {
            rng.GetBytes(data);
        }

        /// <summary>
        ///     用墙随机数填充数组
        /// </summary>
        /// <param name="data">需要填充的数组</param>
        /// <param name="offset">填充起始偏移量</param>
        /// <param name="count">填充数量</param>
        public static void GetBytes(byte[] data, int offset, int count)
        {
            rng.GetBytes(data, offset, count);
        }

        /// <summary>
        ///     用强随机非零数填充数组
        /// </summary>
        /// <param name="data">需要填充的数组</param>
        public static void GetNonZeroBytes(byte[] data)
        {
            rng.GetNonZeroBytes(data);
        }
    }
}