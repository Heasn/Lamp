#region 文件描述

// 开发者：陈柏宇
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：RsaCryptor.cs
// 创建日期：2017-08-28

#endregion

using System.Security.Cryptography;

namespace Lamp.Agent.Crypto.RSA
{
    internal static class RsaCryptor
    {
        private static readonly System.Security.Cryptography.RSA cryptor = System.Security.Cryptography.RSA.Create();

        static RsaCryptor()
        {
            cryptor.ImportFromXmlString("privateKey.xml");
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