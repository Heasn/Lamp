#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Utilities
// 文件名：RsaCryptor.cs
// 创建日期：2017-09-05

#endregion

using System.Security.Cryptography;

namespace Lamp.Utilities.Crypto.RSA
{
    public static class RsaCryptor
    {
        private static readonly System.Security.Cryptography.RSA cryptor = System.Security.Cryptography.RSA.Create();

        static RsaCryptor()
        {
            cryptor.ImportFromXmlString("key.xml");
        }

        public static byte[] Decrypt(byte[] data)
        {
            return cryptor.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        }

        public static byte[] Encrypt(byte[] data)
        {
            return cryptor.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        }
    }
}