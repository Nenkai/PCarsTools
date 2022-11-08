using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools
{
    class BUid
    {
        // lookup8.c https://burtleburtle.net/bob/c/lookup8.c
        private static void Mix64(ref ulong a, ref ulong b, ref ulong c)
        {
            a -= b; a -= c; a ^= (c >> 43);
            b -= c; b -= a; b ^= (a << 9);
            c -= a; c -= b; c ^= (b >> 8);
            a -= b; a -= c; a ^= (c >> 38);
            b -= c; b -= a; b ^= (a << 23);
            c -= a; c -= b; c ^= (b >> 5);
            a -= b; a -= c; a ^= (c >> 35);
            b -= c; b -= a; b ^= (a << 49);
            c -= a; c -= b; c ^= (b >> 11);
            a -= b; a -= c; a ^= (c >> 12);
            b -= c; b -= a; b ^= (a << 18);
            c -= a; c -= b; c ^= (b >> 22);
        }

        public static ulong Hash(Span<byte> k, ulong length, ulong level)
        {
            ulong len = length;
            ulong a = level, b = level;
            ulong c = 0x9e3779b97f4a7c13;

            int len_x = 0;
            while (len >= 24)
            {
                a += (k[len_x + 0] + (((ulong)k[len_x + 1]) << 8) + (((ulong)k[len_x + 2]) << 16) + (((ulong)k[len_x + 3]) << 24)
                    + (((ulong)k[len_x + 4]) << 32) + (((ulong)k[len_x + 5]) << 40) + (((ulong)k[len_x + 6]) << 48) + (((ulong)k[len_x + 7]) << 56));
                b += (k[len_x + 8] + (((ulong)k[len_x + 9]) << 8) + (((ulong)k[len_x + 10]) << 16) + (((ulong)k[len_x + 11]) << 24)
                    + (((ulong)k[len_x + 12]) << 32) + (((ulong)k[len_x + 13]) << 40) + (((ulong)k[len_x + 14]) << 48) + (((ulong)k[len_x + 15]) << 56));
                c += (k[len_x + 16] + (((ulong)k[len_x + 17]) << 8) + (((ulong)k[len_x + 18]) << 16) + (((ulong)k[len_x + 19]) << 24)
                    + (((ulong)k[len_x + 20]) << 32) + (((ulong)k[len_x + 21]) << 40) + (((ulong)k[len_x + 22]) << 48) + (((ulong)k[len_x + 23]) << 56));
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
                            c += ((ulong)k[len_x + 22] << 56);
                            break;
                        case 22:
                            c += ((ulong)k[len_x + 21] << 48);
                            break;
                        case 21:
                            c += ((ulong)k[len_x + 20] << 40);
                            break;
                        case 20:
                            c += ((ulong)k[len_x + 19] << 32);
                            break;
                        case 19:
                            c += ((ulong)k[len_x + 18] << 24);
                            break;
                        case 18:
                            c += ((ulong)k[len_x + 17] << 16);
                            break;
                        case 17:
                            c += ((ulong)k[len_x + 16] << 8);
                            break;
                        /* the first byte of c is reserved for the length */
                        case 16:
                            b += ((ulong)k[len_x + 15] << 56);
                            break;
                        case 15:
                            b += ((ulong)k[len_x + 14] << 48);
                            break;
                        case 14:
                            b += ((ulong)k[len_x + 13] << 40);
                            break;
                        case 13:
                            b += ((ulong)k[len_x + 12] << 32);
                            break;
                        case 12:
                            b += ((ulong)k[len_x + 11] << 24);
                            break;
                        case 11:
                            b += ((ulong)k[len_x + 10] << 16);
                            break;
                        case 10:
                            b += ((ulong)k[len_x + 9] << 8);
                            break;
                        case 9:
                            b += ((ulong)k[len_x + 8]);
                            break;
                        case 8:
                            a += ((ulong)k[len_x + 7] << 56);
                            break;
                        case 7:
                            a += ((ulong)k[len_x + 6] << 48);
                            break;
                        case 6:
                            a += ((ulong)k[len_x + 5] << 40);
                            break;
                        case 5:
                            a += ((ulong)k[len_x + 4] << 32);
                            break;
                        case 4:
                            a += ((ulong)k[len_x + 3] << 24);
                            break;
                        case 3:
                            a += ((ulong)k[len_x + 2] << 16);
                            break;
                        case 2:
                            a += ((ulong)k[len_x + 1] << 8);
                            break;
                        case 1:
                            a += ((ulong)k[len_x + 0]);
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

        public static ulong HashString(string input, ulong level = 0)
        {
            return Hash(Encoding.UTF8.GetBytes(input), (ulong)Encoding.UTF8.GetByteCount(input), level);
        }
    }
}
