using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Buffers.Binary;

namespace PCarsTools.Base
{
    class BHashCode
    {
        // lookup8.c https://burtleburtle.net/bob/c/lookup8.c
        private static void Mix64(ref ulong a, ref ulong b, ref ulong c)
        {
            a -= b; a -= c; a ^= c >> 43;
            b -= c; b -= a; b ^= a << 9;
            c -= a; c -= b; c ^= b >> 8;
            a -= b; a -= c; a ^= c >> 38;
            b -= c; b -= a; b ^= a << 23;
            c -= a; c -= b; c ^= b >> 5;
            a -= b; a -= c; a ^= c >> 35;
            b -= c; b -= a; b ^= a << 49;
            c -= a; c -= b; c ^= b >> 11;
            a -= b; a -= c; a ^= c >> 12;
            b -= c; b -= a; b ^= a << 18;
            c -= a; c -= b; c ^= b >> 22;
        }

        public static ulong CreateHashCode64(string str, uint length, uint initval, bool caseSensitive = false)
        {
            string k = str;
            if (!caseSensitive)
                k = str.ToUpper();

            ulong len = length;
            ulong a = initval, b = initval;
            ulong c = 0x9e3779b97f4a7c13;

            int len_x = 0;
            while (len >= 24)
            {
                a += ((ulong)k[len_x + 0] << 56) + ((ulong)k[len_x + 1] << 48) + ((ulong)k[len_x + 2] << 40) + ((ulong)k[len_x + 3] << 32)
                    + ((ulong)k[len_x + 4] << 24) + ((ulong)k[len_x + 5] << 16) + ((ulong)k[len_x + 6] << 8) + k[len_x + 7];

                b += ((ulong)k[len_x + 8] << 56) + ((ulong)k[len_x + 9] << 48) + ((ulong)k[len_x + 10] << 40) + ((ulong)k[len_x + 11] << 32)
                    + ((ulong)k[len_x + 12] << 24) + ((ulong)k[len_x + 13] << 16) + ((ulong)k[len_x + 14] << 8) + k[len_x + 15];

                c += ((ulong)k[len_x + 16] << 56) + ((ulong)k[len_x + 17] << 48) + ((ulong)k[len_x + 18] << 40) + ((ulong)k[len_x + 19] << 32)
                    + ((ulong)k[len_x + 20] << 24) + ((ulong)k[len_x + 21] << 16) + ((ulong)k[len_x + 22] << 8) + k[len_x + 23];

                Mix64(ref a, ref b, ref c);
                len_x += 24; len -= 24;
            }

            c += length;

            if (len <= 23)
            {
                while (len > 0)
                {

                    switch (len)
                    {              // all the case statements fall through
                        case 23:
                            c += (ulong)k[len_x + 22] << 56;
                            break;
                        case 22:
                            c += (ulong)k[len_x + 21] << 48;
                            break;
                        case 21:
                            c += (ulong)k[len_x + 20] << 40;
                            break;
                        case 20:
                            c += (ulong)k[len_x + 19] << 32;
                            break;
                        case 19:
                            c += (ulong)k[len_x + 18] << 24;
                            break;
                        case 18:
                            c += (ulong)k[len_x + 17] << 16;
                            break;
                        case 17:
                            c += (ulong)k[len_x + 16] << 8;
                            break;
                        /* the first byte of c is reserved for the length */
                        case 16:
                            b += (ulong)k[len_x + 15] << 56;
                            break;
                        case 15:
                            b += (ulong)k[len_x + 14] << 48;
                            break;
                        case 14:
                            b += (ulong)k[len_x + 13] << 40;
                            break;
                        case 13:
                            b += (ulong)k[len_x + 12] << 32;
                            break;
                        case 12:
                            b += (ulong)k[len_x + 11] << 24;
                            break;
                        case 11:
                            b += (ulong)k[len_x + 10] << 16;
                            break;
                        case 10:
                            b += (ulong)k[len_x + 9] << 8;
                            break;
                        case 9:
                            b += k[len_x + 8];
                            break;
                        case 8:
                            a += (ulong)k[len_x + 7] << 56;
                            break;
                        case 7:
                            a += (ulong)k[len_x + 6] << 48;
                            break;
                        case 6:
                            a += (ulong)k[len_x + 5] << 40;
                            break;
                        case 5:
                            a += (ulong)k[len_x + 4] << 32;
                            break;
                        case 4:
                            a += (ulong)k[len_x + 3] << 24;
                            break;
                        case 3:
                            a += (ulong)k[len_x + 2] << 16;
                            break;
                        case 2:
                            a += (ulong)k[len_x + 1] << 8;
                            break;
                        case 1:
                            a += k[len_x + 0];
                            break;
                            /* case 0: nothing left to add */
                    }
                    len--;
                }
            }
            Mix64(ref a, ref b, ref c);
            /*-------------------------------------------- report the result */
            return c;
        }

        public static ulong CreateUidRaw(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0x8DB63936938575BF;

            string str = input.ToLower();
            str = str.Replace('/', '\\');
            return CreateHashCode64(str, (uint)str.Length, 0, true);
        }

        private static void Mix32(ref uint a, ref uint b, ref uint c)
        {
            a -= b; a -= c; a ^= c >> 13;
            b -= c; b -= a; b ^= a << 8;
            c -= a; c -= b; c ^= b >> 13;
            a -= b; a -= c; a ^= c >> 12;
            b -= c; b -= a; b ^= a << 16;
            c -= a; c -= b; c ^= b >> 5;
            a -= b; a -= c; a ^= c >> 3;
            b -= c; b -= a; b ^= a << 10;
            c -= a; c -= b; c ^= b >> 15;
        }

        public static uint CreateHashCode32(string str, uint length, uint initval, bool caseSensitive = false)
        {
            string k = str;
            if (!caseSensitive)
                k = str.ToUpper();

            uint a, b, c, len;

            /* Set up the internal state */
            len = length;
            a = b = 0x9e3779b9;     /* the golden ratio; an arbitrary value */
            c = initval;            /* the previous hash value */

            /* handle most of the key */
            while (len >= 12)
            {
                a += k[3] + ((uint)k[2] << 8) +
                      ((uint)k[1] << 16) + ((uint)k[0] << 24);
                b += k[7] + ((uint)k[6] << 8) + ((uint)k[5] << 16) +
                      ((uint)k[4] << 24);
                c += k[11] + ((uint)k[10] << 8) + ((uint)k[9] << 16) +
                      ((uint)k[8] << 24);
                Mix32(ref a, ref b, ref c);
                k = k[12..]; len -= 12;
            }

            /* handle the last 11 bytes */
            c += length;
            while (len > 0)
            {
                switch (len)
                {       /* all the case statements fall through */
                    case 11: c += (uint)k[10] << 24; break;
                    case 10: c += (uint)k[9] << 16; break;
                    case 9: c += (uint)k[8] << 8; break;
                    /* the first byte of c is reserved for the length */
                    case 8: b += (uint)k[7] << 24; break;
                    case 7: b += (uint)k[6] << 16; break;
                    case 6: b += (uint)k[5] << 8; break;
                    case 5: b += k[4]; break;
                    case 4: a += (uint)k[3] << 24; break;
                    case 3: a += (uint)k[2] << 16; break;
                    case 2: a += (uint)k[1] << 8; break;
                    case 1: a += k[0]; break;
                        /* case 0: nothing left to add */
                }
                len--;
            }

            Mix32(ref a, ref b, ref c);
            return c;
        }

        public static uint CreateHashRaw(string str, uint length, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(str))
                return 0xBD49D10D;

            return CreateHashCode32(str, length, 0, caseSensitive);
        }

        public static uint CreateHash32(string str)
        {
            return CreateHashRaw(str, (uint)str.Length, true);
        }

        public static uint CreateHash32NoCase(string str)
        {
            return CreateHashRaw(str, (uint)str.Length, false);
        }
    }
}
