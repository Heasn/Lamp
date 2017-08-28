using System.Security.Cryptography;

namespace Lamp.Agent.Crypto.RSA
{
    internal static class RsaCryptoService
    {
        private static readonly System.Security.Cryptography.RSA RsaCryptor = System.Security.Cryptography.RSA.Create();

        static RsaCryptoService()
        {
            RsaCryptor.ImportFromXmlString("privateKey.xml");
        }

        public static byte[] Encrypt(byte[] data)
        {
            return RsaCryptor.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        }

        public static byte[] Decrypt(byte[] data)
        {
            return RsaCryptor.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        }
    }
}
