#region 文件描述

// 开发者：陈柏宇
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：AesBlock.cs
// 创建日期：2017-09-02

#endregion

using System;

namespace Lamp.Agent.Crypto.AES
{
    internal class AesBlock
    {
        public const int BLOCKSIZE = 16;
        private readonly uint[] mDecKey;

        private readonly uint[] mEncKey;

        public AesBlock(byte[] key)
        {
            var length = key.Length;
            switch (length)
            {
                case 16:
                case 24:
                case 32:
                    break;
                default:
                    throw new ArgumentException("密钥长度不正确，密钥长度应为16、24或32字节！");
            }

            mEncKey = new uint[length + 28];
            mDecKey = new uint[length + 28];

            Aes.ExpandKey(key, mEncKey, mDecKey);
        }


        public void Decrypt(byte[] dst, byte[] src)
        {
            if (src.Length < BLOCKSIZE)
                throw new ArgumentException("AES：明文不是一个完整的块");
            if (dst.Length < BLOCKSIZE)
                throw new ArgumentException("AES：密文不是一个完整的块");
            Aes.DecryptBlock(mDecKey, dst, src);
        }

        public void Encrypt(byte[] dst, byte[]src)
        {
            if (src.Length < BLOCKSIZE)
                throw new ArgumentException("AES：明文不是一个完整的块");
            if (dst.Length < BLOCKSIZE)
                throw new ArgumentException("AES：密文不是一个完整的块");
            Aes.EncryptBlock(mEncKey, dst, src);
        }
    }
}