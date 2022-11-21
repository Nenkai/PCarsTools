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

namespace XCompression
{
    public abstract class BaseContext : IDisposable
    {
        internal BaseContext()
        {
            if (XnaNative.Load(this) == false)
            {
                throw new InvalidOperationException();
            }
        }

        ~BaseContext()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.XnaNativeHandle != IntPtr.Zero)
            {
                this.NativeCreateCompressionContext = null;
                this.NativeCompress = null;
                this.NativeDestroyCompressionContext = null;
                this.NativeCreateDecompressionContext = null;
                this.NativeDecompress = null;
                this.NativeDestroyDecompressionContext = null;
                //todo: add NativeContext
                //Kernel32.FreeLibrary(this.XnaNativeHandle);
                this.XnaNativeHandle = IntPtr.Zero;
            }
        }

        internal IntPtr XnaNativeHandle;
        internal Delegates.CreateCompressionContext NativeCreateCompressionContext;
        internal Delegates.Compress NativeCompress;
        internal Delegates.DestroyCompressionContext NativeDestroyCompressionContext;
        internal Delegates.CreateDecompressionContext NativeCreateDecompressionContext;
        internal Delegates.Decompress NativeDecompress;
        internal Delegates.DestroyDecompressionContext NativeDestroyDecompressionContext;

        internal ErrorCode CreateCompressionContext(
            int type,
            CompressionSettings settings,
            int flags,
            out IntPtr context)
        {
            if (this.XnaNativeHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("XnaNative is not loaded");
            }

            context = IntPtr.Zero;
            return (ErrorCode)this.NativeCreateCompressionContext(1, ref settings, flags, ref context);
        }

        internal ErrorCode Compress(
            IntPtr context,
            IntPtr output,
            ref int outputSize,
            IntPtr input,
            ref int inputSize)
        {
            if (this.XnaNativeHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("XnaNative is not loaded");
            }

            return (ErrorCode)this.NativeCompress(context, output, ref outputSize, input, ref inputSize);
        }

        internal void DestroyCompressionContext(IntPtr context)
        {
            if (this.XnaNativeHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("XnaNative is not loaded");
            }

            this.NativeDestroyCompressionContext(context);
        }

        internal ErrorCode CreateDecompressionContext(
            int type,
            CompressionSettings settings,
            int flags,
            out IntPtr context)
        {
            if (this.XnaNativeHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("XnaNative is not loaded");
            }

            context = IntPtr.Zero;
            return (ErrorCode)this.NativeCreateDecompressionContext(1, ref settings, flags, ref context);
        }

        internal ErrorCode Decompress(
            IntPtr context,
            IntPtr output,
            ref int outputSize,
            IntPtr input,
            ref int inputSize)
        {
            if (this.XnaNativeHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("XnaNative is not loaded");
            }

            return (ErrorCode)this.NativeDecompress(context, output, ref outputSize, input, ref inputSize);
        }

        internal void DestroyDecompressionContext(IntPtr context)
        {
            if (this.XnaNativeHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("XnaNative is not loaded");
            }

            this.NativeDestroyDecompressionContext(context);
        }
    }
}
