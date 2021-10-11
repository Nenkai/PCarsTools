using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools.Encryption
{
    public class BuildDatDecrypt
    {
        public static void Crypt(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] ^= 0xAC;
        }
    }
}
