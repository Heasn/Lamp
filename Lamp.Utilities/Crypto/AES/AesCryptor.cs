#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Utilities
// 文件名：AesCryptor.cs
// 创建日期：2017-09-05

#endregion

using System;
using System.Runtime.CompilerServices;

namespace Lamp.Utilities.Crypto.AES
{
    public class AesCryptor
    {
        private AesBlockStruct mBlockStruct;

        public AesCryptor(AesBlock block, byte[] iv)
        {
            if (iv.Length < AesBlock.BLOCKSIZE)
                throw new ArgumentException("Iv长度不够");

            mBlockStruct = new AesBlockStruct
            {
                Block = block,
                NextBuffer = new byte[AesBlock.BLOCKSIZE],
                OutBuffer = new byte[AesBlock.BLOCKSIZE],
                OutBufferUsed = AesBlock.BLOCKSIZE,
                IsDecrypt = false
            };

            Buffer.BlockCopy(iv, 0, mBlockStruct.NextBuffer, 0, mBlockStruct.NextBuffer.Length);
        }

        public void XorKeyStream(byte[] dst, byte[] src)
        {
            while (src.Length > 0)
            {
                if (mBlockStruct.OutBufferUsed == mBlockStruct.OutBuffer.Length)
                {
                    mBlockStruct.Block.Encrypt(mBlockStruct.OutBuffer, mBlockStruct.NextBuffer);
                    mBlockStruct.OutBufferUsed = 0;
                }

                if (mBlockStruct.IsDecrypt)
                    Buffer.BlockCopy(src, 0, mBlockStruct.NextBuffer, mBlockStruct.OutBufferUsed,
                        Math.Min(mBlockStruct.NextBuffer.Length - mBlockStruct.OutBufferUsed, src.Length));

                var temp = new byte[mBlockStruct.OutBuffer.Length - mBlockStruct.OutBufferUsed];
                Buffer.BlockCopy(mBlockStruct.OutBuffer, mBlockStruct.OutBufferUsed, temp, 0, temp.Length);
                var n = SafeXorBytes(dst, src, temp);

                if (!mBlockStruct.IsDecrypt)
                    Buffer.BlockCopy(dst, 0, mBlockStruct.NextBuffer, mBlockStruct.OutBufferUsed,
                        Math.Min(mBlockStruct.NextBuffer.Length - mBlockStruct.OutBufferUsed, dst.Length));

                temp = new byte[dst.Length - n];
                Buffer.BlockCopy(dst, n, temp, 0, temp.Length);
                dst = temp;

                temp = new byte[src.Length - n];
                Buffer.BlockCopy(src, n, temp, 0, temp.Length);
                src = temp;

                mBlockStruct.OutBufferUsed += n;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SafeXorBytes(byte[] dst, byte[] a, byte[] b)
        {
            var n = Math.Min(a.Length, b.Length);

            for (var i = 0; i < n; i++)
                dst[i] = (byte) (a[i] ^ b[i]);

            return n;
        }
    }
}