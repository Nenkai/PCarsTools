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
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using MD5 = System.Security.Cryptography.MD5;

namespace XCompression
{
    internal static class XnaNative
    {
        /// <summary>
        /// Ordinally you could just use the normal exports provided by XnaNative.dll,
        /// but the provided exports don't allow you to specify window and partition sizes
        /// so we need to call the underlying functions the exports themselves wrap, which
        /// are not exported via IAT.
        /// 
        /// Since we are dealing with raw memory addresses of target functions rather than
        /// export names, we must identify an appropriate version by its MD5 hash.
        /// </summary>
        private static readonly NativeInfo[] _NativeInfos = new[]
        {
            new NativeInfo("3.0.11010.0", "08dde3aeaa90772e9bf841aa30ad409d", 0x1018D303, 0x1018D293, 0x1018D2DF, 0x1018D3DA, 0x1018D36A, 0x1018D3B6),
            new NativeInfo("3.1.10527.0", "fb193b2a3b5dc72d6f0ff6b86723c1ed", 0x101963F1, 0x1019633F, 0x101963CB, 0x101964DB, 0x1019645F, 0x101964B5),
            new NativeInfo("4.0.20823.0", "993d6b608c47e867bcf10a064ff2d61a", 0x10197933, 0x10197881, 0x1019790D, 0x10197A1D, 0x101979A1, 0x101979F7),
            new NativeInfo("4.0.30901.0", "cbffc669518ee511890f236fefffb4c1", 0x10197933, 0x10197881, 0x1019790D, 0x10197A1D, 0x101979A1, 0x101979F7),
        };

        private static string ComputeMD5(string path)
        {
            using (var md5 = MD5.Create())
            using (var input = File.OpenRead(path))
            {
                var sb = new StringBuilder();
                foreach (var b in md5.ComputeHash(input))
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static NativeInfo? FindAcceptableInfo(out string path)
        {
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using (baseKey)
            {
                if (baseKey == null)
                {
                    path = null;
                    return null;
                }

                var versions = new[] { "v4.0", "v3.1", "v3.0" };
                foreach (var version in versions)
                {
                    var subKeyName = @"SOFTWARE\Microsoft\XNA\Framework\" + version;
                    var subKey = baseKey.OpenSubKey(subKeyName);
                    using (subKey)
                    {
                        if (subKey == null)
                        {
                            continue;
                        }
                        path = subKey.GetValue("NativeLibraryPath", null) as string;
                        if (string.IsNullOrEmpty(path) == true)
                        {
                            continue;
                        }
                    }

                    path = Path.GetFullPath(Path.Combine(path, "XnaNative.dll"));
                    if (File.Exists(path) == false)
                    {
                        continue;
                    }

                    string hash;
                    try
                    {
                        hash = ComputeMD5(path);
                    }
                    catch (FileNotFoundException)
                    {
                        continue;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        continue;
                    }
                    catch (NotSupportedException)
                    {
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }

                    var info = _NativeInfos.FirstOrDefault(i => i.Hash == hash);
                    if (info.Valid == true)
                    {
                        return info;
                    }
                }
            }

            path = null;
            return null;
        }

        private static AcceptableInfo _AcceptableVersion;

        internal static bool Load(BaseContext context)
        {
            if (context.XnaNativeHandle != IntPtr.Zero)
            {
                return true;
            }

            if (_AcceptableVersion.Valid == false)
            {
                string path;
                var info = FindAcceptableInfo(out path);
                if (info == null)
                {
                    throw new FileNotFoundException("could not find an acceptable version of XnaNative installed to use");
                }

                _AcceptableVersion = new AcceptableInfo(info.Value, path);
            }

            var library = Kernel32.LoadLibrary(_AcceptableVersion.Path);
            if (library == IntPtr.Zero)
            {
                throw new Win32Exception($"could not load XnaNative {_AcceptableVersion.Info.Version}");
            }

            var process = Process.GetCurrentProcess();
            var module =
                process.Modules.Cast<ProcessModule>().FirstOrDefault(pm => pm.FileName == _AcceptableVersion.Path);
            if (module == null)
            {
                Kernel32.FreeLibrary(library);
                throw new InvalidOperationException("could not find loaded XnaNative module");
            }

            context.XnaNativeHandle = library;

            context.NativeCreateCompressionContext = GetFunction<Delegates.CreateCompressionContext>(
                module,
                _AcceptableVersion.Info.CreateCompressionContextAddress);
            context.NativeCompress = GetFunction<Delegates.Compress>(
                module,
                _AcceptableVersion.Info.CompressAddress);
            context.NativeDestroyCompressionContext = GetFunction<Delegates.DestroyCompressionContext>(
                module,
                _AcceptableVersion.Info.DestroyCompressionContextAddress);
            context.NativeCreateDecompressionContext = GetFunction<Delegates.CreateDecompressionContext>(
                module,
                _AcceptableVersion.Info.CreateDecompressionContextAddress);
            context.NativeDecompress = GetFunction<Delegates.Decompress>(
                module,
                _AcceptableVersion.Info.DecompressAddress);
            context.NativeDestroyDecompressionContext = GetFunction<Delegates.DestroyDecompressionContext>(
                module,
                _AcceptableVersion.Info.DestroyDecompressionContextAddress);
            return true;
        }

        private static Delegate GetDelegate<TDelegate>(IntPtr address)
        {
            if (address == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(address));
            }

            return Marshal.GetDelegateForFunctionPointer(address, typeof(TDelegate));
        }

        private static TDelegate GetFunction<TDelegate>(ProcessModule module, IntPtr address)
            where TDelegate : class
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (address == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(address));
            }

            address -= 0x10000000;
            address += module.BaseAddress.ToInt32();

            return (TDelegate)((object)GetDelegate<TDelegate>(address));
        }

        #region AcceptableInfo
        private struct AcceptableInfo
        {
            public readonly bool Valid;
            public readonly NativeInfo Info;
            public readonly string Path;

            public AcceptableInfo(NativeInfo info, string path)
            {
                this.Valid = true;
                this.Info = info;
                this.Path = path;
            }
        }
        #endregion

        #region NativeInfo
        public struct NativeInfo
        {
            public readonly bool Valid;

            public readonly string Version;
            public readonly string Hash;

            public readonly IntPtr CreateCompressionContextAddress;
            public readonly IntPtr CompressAddress;
            public readonly IntPtr DestroyCompressionContextAddress;

            public readonly IntPtr CreateDecompressionContextAddress;
            public readonly IntPtr DecompressAddress;
            public readonly IntPtr DestroyDecompressionContextAddress;

            public NativeInfo(
                string version,
                string hash,
                IntPtr createCompressionContextAddress,
                IntPtr compressAddress,
                IntPtr destroyCompressionContextAddress,
                IntPtr createDecompressionContextAddress,
                IntPtr decompressAddress,
                IntPtr destroyDecompressionContextAddress)
            {
                this.Valid = true;
                this.Version = version;
                this.Hash = hash;
                this.CreateCompressionContextAddress = createCompressionContextAddress;
                this.CompressAddress = compressAddress;
                this.DestroyCompressionContextAddress = destroyCompressionContextAddress;
                this.CreateDecompressionContextAddress = createDecompressionContextAddress;
                this.DecompressAddress = decompressAddress;
                this.DestroyDecompressionContextAddress = destroyDecompressionContextAddress;
            }

            public NativeInfo(
                string version,
                string hash,
                int createCompressionContextAddress,
                int compressAddress,
                int destroyCompressionContextAddress,
                int createDecompressionContextAddress,
                int decompressAddress,
                int destroyDecompressionContextAddress)
                : this(
                    version,
                    hash,
                    (IntPtr)createCompressionContextAddress,
                    (IntPtr)compressAddress,
                    (IntPtr)destroyCompressionContextAddress,
                    (IntPtr)createDecompressionContextAddress,
                    (IntPtr)decompressAddress,
                    (IntPtr)destroyDecompressionContextAddress)
            {
            }
        }
        #endregion
    }
}
