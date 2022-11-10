using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData.Memory;
using PCarsTools.Encryption;

namespace PCarsTools.Script
{
    public class ScriptDecrypt
    {
        public const uint Magic = 0x9B4EAF02;
        private static byte[] Key = new byte[]
        {
            /* 0x52, 0x33, 0x2D, 0x7B, 0x3A */ 0x98,0xF9,0x89,0xE8,0xC4,0xF3,0x8D,0xF2,0xBD,0xF4,0x8D,0xB1,0x9F,0xA9,0x86,0xE0,
            0xCE,0xA7,0x82,0xB3,0x95,0xF7,0xDA,0x89,0xB5,0xE9,0x95,0xA6,0xCE,0x88,0x9F,0xBE,
            0xD7,//0x00,0x48,0x61,0x28,0x70,0x41,0x65,0x47,0x38,0x77,0x6F,0x45,0x31,0x78
        };

        // They hid a god damn proprietary "scribe" script in a fake bitmap as an executable resource, amazing
        public static void Decrypt(byte[] bytes)
        {
            SpanReader sr = new SpanReader(bytes);
            sr.Position = 0x28;

            if (sr.ReadUInt32() != Magic)
            {
                sr.Position = 0x36;
                if (sr.ReadUInt32() != Magic)
                    return;
            }

            int fileCount = sr.ReadInt32();
            var key = GetDecKeyScribe();

            for (int i = 0; i < fileCount; i++)
            {
                int size = sr.ReadInt32();
                ulong scriptNameUid = sr.ReadUInt64();
                sr.Position += 4; // Empty

                var encBuffer = sr.ReadBytes(size);
                RC4.Crypt(key, encBuffer, encBuffer, encBuffer.Length);
            }
        }

        private static byte[] GetDecKeyScribe()
        {
            var key = GetEncKeyScribe();

            int i;
            for (i = 0; i + 1 < key.Length; i += 2)
            {
                byte tmp1 = (byte)(key[i] ^ 0xEF);
                byte tmp2 = (byte)(key[i + 1] ^ 0xDA);

                key[i] = tmp2;
                key[i + 1] = tmp1;
            }

            while (i < key.Length)
            {
                byte xor;
                if ((i & 1) != 0)
                    xor = 0xDA;
                else
                    xor = 0xEF;

                key[i++] ^= xor;
            }

            return key;
        }

        private static byte[] GetEncKeyScribe()
            => Key.ToArray();
    }
}
