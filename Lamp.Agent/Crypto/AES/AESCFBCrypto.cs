using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;

namespace Lamp.Agent.Crypto.AES
{
    class AescfbCrypto
    {
        private readonly AES mAes;

        public byte[] Key { get; private set; }
        public byte[] Iv { get; private set; }

        public AescfbCrypto(byte[] key, byte[] iv)
        {
            Key = key;
            Iv = iv;

            mAes = new AES(Key);
        }

        public void SetIv(byte[] iv)
        {
            Iv = iv;
        }

        public void SetKey(byte[] key)
        {
            Key = key;
        }

        public int Encrypt(IByteBuffer buf)
        {
            var length = buf.ReadableBytes;
            var isFirstRound = true;
            var output = new byte[16];
            var coIndex = 0;
            int rounds;

            if (length % 16 == 0)
            {
                rounds = length / 16;
            }
            else
            {
                rounds = length / 16 + 1;
            }

            for (int j = 0; j < rounds; ++j)
            {
                var start = j * 16;
                var end = j * 16 + 16;

                if (end > length)
                    end = length;

                var plaintext = buf.Slice(start, end - start);

                if (isFirstRound)
                {
                    mAes.Cipher(Iv, output);
                    isFirstRound = false;
                }
                else
                {
                    mAes.Cipher(Iv, output);
                }

                for (var i = 0; i < 16; ++i)
                {
                    if (end - start - 1 < i)
                    {
                        buf.SetByte(coIndex++, 0 ^ output[i]);
                    }
                    else
                    {
                        buf.SetByte(coIndex++, plaintext.GetByte(i) ^ output[i]);
                    }
                }
            }

            return coIndex;
        }
    }
}
