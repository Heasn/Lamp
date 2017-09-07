#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Utilities
// 文件名：AesBlockStruct.cs
// 创建日期：2017-09-05

#endregion

namespace Lamp.Utilities.Crypto.AES
{
    internal struct AesBlockStruct
    {
        public AesBlock Block;
        public byte[] NextBuffer;
        public byte[] OutBuffer;
        public int OutBufferUsed;
        public bool IsDecrypt;
    }
}