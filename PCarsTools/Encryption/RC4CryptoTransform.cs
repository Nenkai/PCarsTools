using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace PCarsTools.Encryption
{
    class RC4CryptoTransform : ICryptoTransform
    {
        private int[] box;
        private int _offset;

        public RC4CryptoTransform(Span<byte> pwd)
        {
            int i, j, tmp;

            int[] key = new int[256];
            box = new int[256];

            for (i = 0; i < 256; i++)
            {
                key[i] = pwd[i % pwd.Length];
                box[i] = i;
            }
            for (j = i = 0; i < 256; i++)
            {
                j = (j + box[i] + key[i]) % 256;
                tmp = box[i];
                box[i] = box[j];
                box[j] = tmp;
            }
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            int a, i, j, k, tmp;
            for (a = j = i = _offset; i < inputCount; i++)
            {
                a++;
                a %= 256;
                j += box[a];
                j %= 256;
                tmp = box[a];
                box[a] = box[j];
                box[j] = tmp;
                k = box[((box[a] + box[j]) % 256)];
                outputBuffer[i] = (byte)(inputBuffer[i] ^ k);
            }

            _offset += inputCount;
            return inputCount;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var transformed = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, transformed, 0);
            return transformed;
        }

        public bool CanReuseTransform
        {
            get { return true; }
        }

        public bool CanTransformMultipleBlocks
        {
            get { return true; }
        }

        public int InputBlockSize
        {
            // 4 bytes in uint
            get { return 4; }
        }

        public int OutputBlockSize
        {
            get { return 4; }
        }

        public void Dispose()
        {
        }
    }
}
