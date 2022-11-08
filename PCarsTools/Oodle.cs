using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Runtime.InteropServices;

namespace PCarsTools
{
    public class Oodle
    {
        /// <summary>
        /// Oodle Library Path
        /// </summary>
        private const string OodleLibraryPath = "oo2core_7_win64";

        /// <summary>
        /// Oodle64 Decompression Method 
        /// </summary>
        [DllImport(OodleLibraryPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern long OodleLZ_Decompress(byte[] compBuf, long bufferSize, byte[] decodeTo, long outputBufferSize, int fuzz,
            int crc, int verbose, long dst_base, long e, long cb, long cb_ctx, long scratch, long scratch_size, int threadPhase);

        /// <summary>
        /// Decompresses a byte array of Oodle Compressed Data (Requires Oodle DLL)
        /// </summary>
        /// <param name="input">Input Compressed Data</param>
        /// <param name="decompressedLength">Decompressed Size</param>
        /// <returns>Resulting Array if success, otherwise null.</returns>
        public static bool Decompress(byte[] input, byte[] output, long decompressedLength)
        {
            // Decode the data (other parameters such as callbacks not required)
            long decodedSize = OodleLZ_Decompress(input, input.Length, output, decompressedLength, 1, 0, 0, 0, 0, 0, 0, 0, 0, 3);

            // Check did we fail
            return decompressedLength == decodedSize;
        }
    }

    public class OodleStream : Stream
    {
        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
