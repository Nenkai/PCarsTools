/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 *    
 */

using System;
using System.Runtime.InteropServices;

namespace XCompression
{
    public sealed class CompressionContext : BaseContext
    {
        private IntPtr _Handle;
        public readonly uint WindowSize;
        public readonly uint ChunkSize;

        public CompressionContext()
            : this(Constants.DefaultWindowSize, Constants.DefaultChunkSize)
        {
        }

        public CompressionContext(uint windowSize)
            : this(windowSize, Constants.DefaultChunkSize)
        {
        }

        private CompressionContext(uint windowSize, uint chunkSize)
        {
            CompressionSettings settings;
            settings.Flags = 0;
            settings.WindowSize = windowSize;
            settings.ChunkSize = chunkSize;

            var result = this.CreateCompressionContext(1, settings, 1, out this._Handle);
            if (result != ErrorCode.None)
            {
                throw new InvalidOperationException($"unknown error when creating compression context: {result:X8}");
            }

            this.WindowSize = windowSize;
            this.ChunkSize = chunkSize;
        }

        public ErrorCode Compress(
            byte[] inputBytes,
            int inputOffset,
            ref int inputCount,
            byte[] outputBytes,
            int outputOffset,
            ref int outputCount)
        {
            if (inputBytes == null)
            {
                throw new ArgumentNullException(nameof(inputBytes));
            }

            if (inputOffset < 0 || inputOffset >= inputBytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            }

            if (inputCount <= 0 || inputOffset + inputCount > inputBytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(inputCount));
            }

            if (outputBytes == null)
            {
                throw new ArgumentNullException(nameof(outputBytes));
            }

            if (outputOffset < 0 || outputOffset >= outputBytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(outputOffset));
            }

            if (outputCount <= 0 || outputOffset + outputCount > outputBytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(outputCount));
            }

            var outputHandle = GCHandle.Alloc(outputBytes, GCHandleType.Pinned);
            var inputHandle = GCHandle.Alloc(inputBytes, GCHandleType.Pinned);
            var result = this.Compress(
                this._Handle,
                outputHandle.AddrOfPinnedObject() + outputOffset,
                ref outputCount,
                inputHandle.AddrOfPinnedObject() + inputOffset,
                ref inputCount);
            if (result != ErrorCode.None)
            {
                throw new InvalidOperationException($"unknown error during compression: {result}");
            }
            inputHandle.Free();
            outputHandle.Free();
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (this._Handle != IntPtr.Zero)
            {
                this.DestroyCompressionContext(this._Handle);
                this._Handle = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }
    }
}
