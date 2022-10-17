using System;
using System.Collections.Generic;
using System.Text;

namespace EasySocket
{
    public interface IEncryptor
    {
        byte[] Encrypt(byte[] input);
        byte[] Decrypt(byte[] input);
    }
}
