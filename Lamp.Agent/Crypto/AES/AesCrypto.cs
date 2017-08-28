using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using DotNetty.Common.Internal.Logging;

namespace Lamp.Agent.Crypto.AES
{
    internal sealed class AesCrypto
    {
        private readonly IInternalLogger mLogger = InternalLoggerFactory.GetInstance<Program>();
        private readonly Aes mAes = Aes.Create();

        public byte[] Key => mAes.Key;
        public byte[] Iv => mAes.IV;

        public AesCrypto()
        {
            mAes.BlockSize = 128;
            mAes.KeySize = 128;
            mAes.Padding = PaddingMode.PKCS7;
            mAes.Mode = CipherMode.CBC;

            mAes.GenerateIV();
        }

        public AesCrypto(byte[] key)
            : this()
        {
            if (mAes.ValidKeySize(key.Length))
                mAes.Key = key;
            else
                mLogger.Error(new ArgumentException("AES密钥长度不正确"));
        }

        public byte[] Encrypt(byte[] data)
        {
            using (var cryptor = mAes.CreateEncryptor())
            {
                 return cryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }

        public byte[] Decrypt(byte[] data)
        {
            using (var cryptor = mAes.CreateDecryptor())
            {
                return cryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }
    }
}
