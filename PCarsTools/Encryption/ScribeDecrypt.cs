using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Numerics;

namespace PCarsTools.Encryption
{
    // RC5 or RC6
    public class ScribeDecrypt
    {
        public uint[] Key = new uint[]
        {
            0xbb9fcbf5, 0xb296fa98, 0xdd9ccbdb, 0x96c2e3f2,
            0x93e8dcf3, 0xbadbbc99, 0xcc9acd89, 0xaae9bc98,
            0xa8f9a8c5, 0xb6d8fbd0, 0xc6cea888, 0
        };

        public const int Rounds = 30;
        public const int KeySize = 44;
        public const int Factor = (Rounds * 2) + 4;

        private uint[] schedule = new uint[64];

        public void CreateSchedule()
        {
            uint[] tmpKey = new uint[KeySize];

            uint y = 0;
            for (uint x = 0; x < KeySize; x += 4)
            {
                uint c = Key[y];
                tmpKey[x] = (byte)(c >> 8);
                tmpKey[x + 1] = (byte)(c >> 24);
                tmpKey[x + 2] = (byte)(c);
                tmpKey[x + 3] = (byte)(c >> 16);

                y += 1;
            }

            schedule[0] = 0xB7E15163;

            for (uint x = 1; x <= (Rounds * 2) + 3; x += 1)
            {
                y = x - 1;
                schedule[x] = schedule[y] + 0x9E3779B9;
            }

            uint a = 0, b = 0, i = 0, j = 0;

            int count = KeySize > Factor ?
                3 * KeySize :
                3 * Factor;

            for (int x = 1; x <= count; x += 1)
            {
                uint arg0 = schedule[i];
                arg0 = arg0 + a + b;
                arg0 = BitOperations.RotateLeft(arg0, 3);

                a = arg0;
                schedule[i] = a;

                uint kr = tmpKey[j];
                if (x <= KeySize)
                {
                    arg0 = (0xAEB3F79Au >> (int)((j % 4) * 8));
                    kr ^= (byte)arg0;
                }

                arg0 = kr + a + b;

                arg0 = BitOperations.RotateLeft(arg0, (int)((a + b) & 31));
                b = arg0;
                tmpKey[j] = b;

                i = (i + 1) % Factor;
                j = (j + 1) % KeySize;
            }

        }

        public void Decrypt(Span<uint> data)
        {
            for (int x = 0; x < data.Length; x += 4)
            {
                uint k0 = data[x];
                uint k1 = data[x + 1];
                uint k2 = data[x + 2];
                uint k3 = data[x + 3];

                k2 -= schedule[(Rounds * 2) + 3];
                k0 -= schedule[(Rounds * 2) + 2];

                for (int i = Rounds; i >= 1; i -= 1)
                {
                    uint kr = k3;
                    k3 = k2;
                    k2 = k1;
                    k1 = k0;
                    k0 = kr;
                    uint arg0;

                    arg0 = k3 * ((k3 << 1) + 1);
                    arg0 = BitOperations.RotateLeft(arg0, 2);
                    uint a = arg0;

                    arg0 = k1 * ((k1 << 1) + 1);
                    arg0 = BitOperations.RotateLeft(arg0, 2);
                    uint b = arg0;

                    arg0 = k2 - schedule[(i << 1) + 1];
                    arg0 = BitOperations.RotateRight(arg0, (int)(b & 31));
                    k2 = arg0 ^ a;

                    arg0 = k0 - schedule[(i << 1)];
                    arg0 = BitOperations.RotateRight(arg0, (int)(a & 31));
                    k0 = arg0 ^ b;
                }

                k3 -= schedule[1];
                k1 -= schedule[0];

                data[x] = k0;
                data[x + 1] = k1;
                data[x + 2] = k2;
                data[x + 3] = k3;
            }
        }

        public static void HashString(string str)
        {
            byte[] buf = new byte[0x400];
            Encoding.ASCII.GetBytes(str, buf);
            Hash(buf, (uint)str.Length, 0);
        }

        public static uint Hash(Span<byte> k, uint length, uint initval)
        {
            if (k.IsEmpty)
                return 0xBD49D10D;

            uint a, b, c, len;

            /* Set up the internal state */
            len = length;
            a = b = 0x9e3779b9;     /* the golden ratio; an arbitrary value */
            c = initval;            /* the previous hash value */

            /* handle most of the key */
            while (len >= 12)
            {
                a += (k[3] + ((uint)k[2] << 8) +
                      ((uint)k[1] << 16) + ((uint)k[0] << 24));
                b += (k[7] + ((uint)k[6] << 8) + ((uint)k[5] << 16) +
                      ((uint)k[4] << 24));
                c += (k[11] + ((uint)k[10] << 8) + ((uint)k[9] << 16) +
                      ((uint)k[8] << 24));
                Mix32(ref a, ref b, ref c);
                k = k[12..]; len -= 12;
            }

            /* handle the last 11 bytes */
            c += length;
            while (len > 0)
            {
                switch (len)
                {       /* all the case statements fall through */
                    case 11: c += ((uint)k[10] << 24); break;
                    case 10: c += ((uint)k[9] << 16); break;
                    case 9: c += ((uint)k[8] << 8); break;
                    /* the first byte of c is reserved for the length */
                    case 8: b += ((uint)k[7] << 24); break;
                    case 7: b += ((uint)k[6] << 16); break;
                    case 6: b += ((uint)k[5] << 8); break;
                    case 5: b += k[4]; break;
                    case 4: a += ((uint)k[3] << 24); break;
                    case 3: a += ((uint)k[2] << 16); break;
                    case 2: a += ((uint)k[1] << 8); break;
                    case 1: a += k[0]; break;
                        /* case 0: nothing left to add */
                }
                len--;
            }

            Mix32(ref a, ref b, ref c);
            return c;
        }

        private static void Mix32(ref uint a, ref uint b, ref uint c)
        {
            a -= b; a -= c; a ^= (c >> 13);
            b -= c; b -= a; b ^= (a << 8);
            c -= a; c -= b; c ^= (b >> 13);
            a -= b; a -= c; a ^= (c >> 12);
            b -= c; b -= a; b ^= (a << 16);
            c -= a; c -= b; c ^= (b >> 5);
            a -= b; a -= c; a ^= (c >> 3);
            b -= c; b -= a; b ^= (a << 10);
            c -= a; c -= b; c ^= (b >> 15);
        }
    }
}
