using System;
using System.Collections.Generic;
using System.Text;

namespace Lamp.Agent.Crypto.AES
{
    unsafe class AESCrypto
    {
        AES m_aes;
        byte[] m_key;
        byte[] m_iv;

        public AESCrypto(byte[] key, byte[] iv)
        {
            m_key = key;
            m_iv = iv;

            m_aes = new AES(m_key);
        }

        void set_iv(byte[] _iv)
        {
            m_iv = _iv;
        }

        public int Encrypt(byte[] _in, int _length, byte[] _out)
        {
            bool first_round = true;
            int rounds = 0;
            int start = 0;
            int end = 0;
            byte[] input = new byte[16];
            byte[] output = new byte[16];
            byte[] ciphertext = new byte[16];
            byte[] cipherout = new byte[256];
            byte[] plaintext = new byte[16];
            int co_index = 0;
            // 1. get rounds
            if (_length % 16 == 0)
            {
                rounds = _length / 16;
            }
            else
            {
                rounds = _length / 16 + 1;
            }
            // 2. for all rounds 
            for (int j = 0; j < rounds; ++j)
            {
                start = j * 16;
                end = j * 16 + 16;
                if (end > _length) end = _length;   // end of input
                                                    // 3. copyt input to m_plaintext
                plaintext.Initialize();
                Array.Copy(_in, start, plaintext,0, end - start);

                // 4. handle all modes
                //if (m_mode == MODE_CFB)
                //{
                if (first_round == true)
                {
                    fixed (byte* iv_ptr = &m_iv[0])
                    {
                        fixed (byte* output_ptr = &output[0])
                        {
                             m_aes. Cipher(iv_ptr, output_ptr);
                            first_round = false;
                        }
                    }   
                }
                else
                {
                    fixed (byte* iv_ptr = &m_iv[0])
                    {
                        fixed (byte* output_ptr = &output[0])
                        {
                            m_aes.Cipher(iv_ptr, output_ptr);
                        }
                    }
                }
                for (int i = 0; i < 16; ++i)
                {
                    if ((end - start) - 1 < i)
                    {
                        ciphertext[i] = (byte) (0 ^ output[i]);
                    }
                    else
                    {
                        ciphertext[i] = (byte) (plaintext[i] ^ output[i]);
                    }
                }
                for (int k = 0; k < end - start; ++k)
                {
                    cipherout[co_index++] = ciphertext[k];
                }
                //memset(input,0, 16);
                Array.Copy(ciphertext, input, 16);
                //}
                //else if (m_mode == MODE_OFB)
                //{           // MODE_OFB
                //    if (first_round == true)
                //    {
                //        m_aes->Cipher(m_iv, output); // 
                //        first_round = false;
                //    }
                //    else
                //    {
                //        m_aes->Cipher(input, output);
                //    }
                //    // ciphertext = plaintext ^ output
                //    for (int i = 0; i < 16; ++i)
                //    {
                //        if ((end - start) - 1 < i)
                //        {
                //            ciphertext[i] = 0 ^ output[i];
                //        }
                //        else
                //        {
                //            ciphertext[i] = plaintext[i] ^ output[i];
                //        }
                //    }
                //    // 
                //    for (int k = 0; k < end - start; ++k)
                //    {
                //        cipherout[co_index++] = ciphertext[k];
                //    }
                //    //memset(input,0,16);
                //    memcpy(input, output, 16);
                //}
                //else if (m_mode == MODE_CBC)
                //{           // MODE_CBC
                //    printf("-----plaintext------");
                //    print(plaintext, 16);
                //    printf("--------------------\n");
                //    //			printf("-----m_iv-----------\n");
                //    //			print (m_iv, 16);
                //    //			printf("--------------------\n");
                //    for (int i = 0; i < 16; ++i)
                //    {
                //        if (first_round == true)
                //        {
                //            input[i] = plaintext[i] ^ m_iv[i];
                //        }
                //        else
                //        {
                //            input[i] = plaintext[i] ^ ciphertext[i];
                //        }
                //    }
                //    first_round = false;
                //    //			printf("^^^^^^^^^^^^\n");
                //    //			print(input, 16);
                //    //			printf("^^^^^^^^^^^^\n");
                //    m_aes->Cipher(input, ciphertext);
                //    printf("****ciphertext****");
                //    print(ciphertext, 16);
                //    printf("************\n");
                //    for (int k = 0; k < end - start; ++k)
                //    {
                //        cipherout[co_index++] = ciphertext[k];
                //    }
                //    //memcpy(cipherout, ciphertext, 16);
                //    //co_index = 16;
                //}
                //else if (m_mode == MODE_ECB)
                //{
                //    // TODO: 
                //}
            }
            Array.Copy(cipherout, _out, co_index);
            return co_index;
        }

        int Decrypt(byte[] _in, int _length, byte[] _out)
        {
            // TODO :
            bool first_round = true;
            int rounds = 0;
            byte[] ciphertext = new byte[16];
            byte[] input = new byte[16];
            byte[] output = new byte[16];
            byte[] plaintext = new byte[16];
            byte[] plainout = new byte[256];
            int po_index = 0;
            if (_length % 16 == 0)
            {
                rounds = _length / 16;
            }
            else
            {
                rounds = _length / 16 + 1;
            }

            int start = 0;
            int end = 0;

            for (int j = 0; j < rounds; j++)
            {
                start = j * 16;
                end = start + 16;
                if (end > _length)
                {
                    end = _length;
                }
                plaintext.Initialize();
                Array.Copy(_in, start, plaintext, 0, end - start);
                //if (m_mode == MODE_CFB)
                //{
                if (first_round == true)
                {
                    fixed (byte* iv_ptr = &m_iv[0])
                    {
                        fixed (byte* output_ptr = &output[0])
                        {
                            m_aes.Cipher(iv_ptr, output_ptr);
                            first_round = false;
                        }
                    }
                }
                else
                {
                    fixed (byte* iv_ptr = &m_iv[0])
                    {
                        fixed (byte* output_ptr = &output[0])
                        {
                            m_aes.Cipher(iv_ptr, output_ptr);
                        }
                    }
                }
                for (int i = 0; i < 16; ++i)
                {
                    if ((end - start) - 1 < i)
                    {
                        plaintext[i] = (byte)(0 ^ output[i]);
                    }
                    else
                    {
                        plaintext[i] = (byte)(ciphertext[i] ^ output[i]);
                    }
                }
                for (int k = 0; k < end - start; ++k)
                {
                    plainout[po_index++] = plaintext[k];
                }
                //memset(input, 0, 16);
                memcpy(input, ciphertext, 16);
                //}
                //else if (m_mode == MODE_OFB)
                //{
                //    if (first_round == true)
                //    {
                //        m_aes->Cipher(m_iv, output);
                //        first_round = false;
                //    }
                //    else
                //    {
                //        m_aes->Cipher(input, output);
                //    }
                //    for (int i = 0; i < 16; i++)
                //    {
                //        if (end - start - 1 < i)
                //        {
                //            plaintext[i] = 0 ^ ciphertext[i];
                //            first_round = false;
                //        }
                //        else
                //        {
                //            plaintext[i] = output[i] ^ ciphertext[i];
                //        }
                //    }
                //    for (int k = 0; k < end - start; ++k)
                //    {
                //        plainout[po_index++] = plaintext[k];
                //    }
                //    memcpy(input, output, 16);
                //}
                //else if (m_mode == MODE_CBC)
                //{
                //    printf("------ciphertext------");
                //    print(ciphertext, 16);
                //    printf("----------------------\n");
                //    m_aes->InvCipher(ciphertext, output);
                //    printf("------output------");
                //    print(output, 16);
                //    printf("----------------------\n");
                //    for (int i = 0; i < 16; ++i)
                //    {
                //        if (first_round == true)
                //        {
                //            plaintext[i] = m_iv[i] ^ output[i];
                //        }
                //        else
                //        {
                //            plaintext[i] = input[i] ^ output[i];
                //        }
                //    }
                //    first_round = false;
                //    for (int k = 0; k < end - start; ++k)
                //    {
                //        plainout[po_index++] = plaintext[k];
                //    }
                //    memcpy(input, ciphertext, 16);
                //}
                //else
                //{
                //    // TODO
                //}
            }
            memcpy(_out, plainout, po_index);
            return po_index;
        }
    }
}
