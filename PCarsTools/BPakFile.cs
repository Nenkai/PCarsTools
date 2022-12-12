using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Buffers;

using Syroot.BinaryData;
using Syroot.BinaryData.Memory;

using PCarsTools.Encryption;
using PCarsTools.Config;

using ICSharpCode.SharpZipLib.Zip.Compression;
using PCarsTools.Compression;
using XCompression;

namespace PCarsTools
{
    public class BPakFile
    {
        public const string TagId = "PAK ";

        public BVersion Version { get; set; }
        public string Name { get; set; }
        public ePakFlags Flags { get; set; }
        public eEncryptionType EncryptionType { get; set; }

        public List<BPakFileTocEntry> Entries { get; set; }
        public List<BExtendedFileInfoEntry> ExtEntries { get; set; }

        public int KeyIndex { get; set; }

        private FileStream _fs;

        /// <summary>
        /// From TOC file
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Reads a pak file from a provided file name.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="withExtraInfo"></param>
        /// <returns></returns>
        public static BPakFile FromFile(string inputFile, bool withExtraInfo = true)
        {
            var fs = new FileStream(inputFile, FileMode.Open);
            var pak = FromStream(fs, withExtraInfo: withExtraInfo, tocFileName: inputFile);
            pak._fs = fs;

            return pak;
        }

        public static BPakFile FromStream(Stream stream, bool withExtraInfo = false, string tocFileName = null)
        {
            var pak = new BPakFile();
            int pakOffset = (int)stream.Position;
            pak.Path = tocFileName.ToLower().Replace('/', '\\');

            using var bs = new BinaryStream(stream, leaveOpen: true);
            int mID = bs.ReadInt32();
            pak.Version = new BVersion(bs.ReadUInt32());
            int fileCount = bs.ReadInt32();
            bs.Position += 12;
            pak.Name = bs.ReadString(0x100).TrimEnd('\0');

            bool keyIndexFound = false;
            pak.KeyIndex = BConfig.Instance.GetPatternIdx(pak.Name);
            if (pak.KeyIndex == 0 && !string.IsNullOrEmpty(pak.Path)) // Default key found, try to see if its in the path
            {
                foreach (var filter in BConfig.Instance.PatternFilters)
                {
                    foreach (var rule in filter.PatternRules)
                    {
                        if (pak.Path.Contains(rule.PatternDecrypted))
                        {
                            pak.KeyIndex = filter.Index;
                            keyIndexFound = true;
                            goto found;
                        }
                    }
                }
            found:
                ;
            }

            uint pakFileTocEntrySize = bs.ReadUInt32();
            uint crc = bs.ReadUInt32();
            uint extInfoSize = bs.ReadUInt32();
            bs.Position += 8;
            pak.Flags = (ePakFlags)bs.Read1Byte();
            pak.EncryptionType = (eEncryptionType)bs.Read1Byte();
            bs.Position += 2;

            var pakTocBuffer = bs.ReadBytes((int)pakFileTocEntrySize);

            if (pak.EncryptionType != eEncryptionType.None)
            {
                if (!keyIndexFound && pak.KeyIndex == 0) // Still not found? Attempt bruteforce
                {
                    int j = 0;
                    for (int i = 0; i < 32; i++)
                    {
                        var tmpData = pakTocBuffer.ToArray();
                        BPakFileEncryption.DecryptData(pak.EncryptionType, tmpData, tmpData.Length, i);
                        if (tmpData[14] == 0 && tmpData[15] == 0)
                        {
                            pak.KeyIndex = i;
                            break;
                        }
                    }
                }

                BPakFileEncryption.DecryptData(pak.EncryptionType, pakTocBuffer, pakTocBuffer.Length, pak.KeyIndex);
            }

            if (pakTocBuffer[14] != 0 && pakTocBuffer[15] != 0) // Check if first entry offset is absurdly too big that its possibly not decrypted correctly
                Console.WriteLine($"Warning - possible crash: {pak.Name} toc could most likely not be decrypted correctly using key No.{pak.KeyIndex}");

            pak.Entries = new List<BPakFileTocEntry>(fileCount);
            SpanReader sr = new SpanReader(pakTocBuffer);
            for (int i = 0; i < fileCount; i++)
            {
                sr.Position = (i * 0x2A);
                var pakFileToCEntry = new BPakFileTocEntry();
                pakFileToCEntry.UId = sr.ReadUInt64();
                pakFileToCEntry.Offset = sr.ReadUInt64();
                pakFileToCEntry.PakSize = sr.ReadUInt32();
                pakFileToCEntry.FileSize = sr.ReadUInt32();
                pakFileToCEntry.TimeStamp = sr.ReadUInt64();
                pakFileToCEntry.Compression = (PakFileCompressionType)sr.ReadByte();
                pakFileToCEntry.UnkFlag = sr.ReadByte();
                pakFileToCEntry.CRC = sr.ReadUInt32();
                pakFileToCEntry.Extension = sr.ReadStringRaw(4).ToCharArray();
                pak.Entries.Add(pakFileToCEntry);
            }

            if (withExtraInfo)
            {
                const int unkCertXmlSize = 0x308;
                bs.Position += unkCertXmlSize;

                int baseExtOffset = (int)bs.Position - pakOffset;
                int extInfoEntriesSize = (int)Utils.AlignValue(extInfoSize - unkCertXmlSize, 0x10);
                var extTocBuffer = bs.ReadBytes(extInfoEntriesSize);

                bool returnBuffer = false;
                if (pak.EncryptionType != eEncryptionType.None)
                {
                    if (extTocBuffer.Length % 0x10 != 0) // Must be aligned to 0x10
                    {
                        int rem = extTocBuffer.Length % 0x10;
                        byte[] extTocBufferAligned = ArrayPool<byte>.Shared.Rent(extTocBuffer.Length + rem);
                        extTocBuffer.AsSpan().CopyTo(extTocBufferAligned);
                        extTocBuffer = extTocBufferAligned;
                        returnBuffer = true;
                    }

                    byte[] tmp = new byte[extTocBuffer.Length];
                    extTocBuffer.AsSpan().CopyTo(tmp);
                    var tmpInts = MemoryMarshal.Cast<byte, uint>(tmp);

                    var scribeDecrypt = new ScribeDecrypt();
                    scribeDecrypt.CreateSchedule();
                    scribeDecrypt.Decrypt(tmpInts);

                    if (tmp[6] != 0 || tmp[7] != 0)
                    {
                        // presumably failed to decrypt, try RC4 with key 0 (used in TDFRL)
                        // assumingly older than PC1 just used regular RC4
                        extTocBuffer.AsSpan().CopyTo(tmp);
                        BPakFileEncryption.DecryptData(pak.EncryptionType, tmp, tmp.Length, 0);

                        if (tmp[6] != 0 && tmp[7] != 0)
                            Console.WriteLine("Warning: possibly failed to decrypt Extended Info Table");

                        extTocBuffer = tmp;
                    }

                    extTocBuffer = tmp;
                }

                pak.ExtEntries = new List<BExtendedFileInfoEntry>(fileCount);
                sr = new SpanReader(extTocBuffer);
                for (int i = 0; i < fileCount; i++)
                {
                    sr.Position = i * 0x10;

                    var extEntry = new BExtendedFileInfoEntry();
                    extEntry.Offset = sr.ReadInt64();
                    extEntry.TimeStamp = sr.ReadInt64();

                    sr.Position = (int)extEntry.Offset - baseExtOffset;
                    extEntry.Path = sr.ReadString1();

                    ulong uid = BHashCode.CreateUidRaw(extEntry.Path);
                    if (pak.Entries[i].UId != uid)
                        Console.WriteLine($"Warning - unmatched UID/Hash: {extEntry.Path} (target={pak.Entries[i].UId:X16}, got={uid}");

                    pak.ExtEntries.Add(extEntry);
                }

                if (returnBuffer)
                    ArrayPool<byte>.Shared.Return(extTocBuffer);
            }

            return pak;
        }

        public void UnpackAll(string outputDir)
        {
            int totalCount = 0;
            int failed = 0;

            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                var extEntry = ExtEntries[i];

                string outPath = System.IO.Path.Combine(outputDir, extEntry.Path);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));

                if (UnpackFromStream(entry, extEntry, outPath))
                {
                    Console.WriteLine($"Unpacked: [{Name}]\\{extEntry.Path}");
                    totalCount++;
                }
                else
                {
                    Console.WriteLine($"Failed to unpack: {extEntry.Path}");
                    failed++;
                }
            }

            Console.WriteLine($"Done. Extracted {totalCount} files ({failed} not extracted)");
        }

        public bool UnpackFromLocalStoredFile(string outputDir, BPakFileTocEntry entry, BExtendedFileInfoEntry extEntry)
        {
            if (_fs is not null)
                throw new InvalidOperationException("Can't extract from local file when the pak is an actual file with data");

            string localPath = System.IO.Path.Combine(outputDir, extEntry.Path);
            if (File.Exists(localPath))
            {
                return UnpackFromFile(entry, extEntry, localPath, localPath + ".dec");
            }
            else
            {
                Console.WriteLine($"File {extEntry.Path} not found to extract, can be ignored");
            }

            return false;
        }

        public bool UnpackFromStream(BPakFileTocEntry entry, BExtendedFileInfoEntry extEntry, string output)
        {
            if (_fs is null)
                throw new InvalidOperationException("Can't extract from stream from a toc file based pak");

            _fs.Position = (long)entry.Offset;

            var bytes = ArrayPool<byte>.Shared.Rent((int)entry.PakSize);
            _fs.Read(bytes);

            bool result = Unpack(bytes, entry, extEntry, output);
            ArrayPool<byte>.Shared.Return(bytes);
            return result;
        }

        private bool UnpackFromFile(BPakFileTocEntry entry, BExtendedFileInfoEntry extEntry, string inputFile, string output)
        {
            var bytes = File.ReadAllBytes(inputFile);
            return Unpack(bytes, entry, extEntry, output);
        }

        private static bool _checkedOodle;
        private static bool _checkedXMem;

        private bool Unpack(byte[] bytes, BPakFileTocEntry entry, BExtendedFileInfoEntry extEntry, string output)
        {
            if (this.EncryptionType != eEncryptionType.None)
                BPakFileEncryption.DecryptData(this.EncryptionType, bytes, bytes.Length, this.KeyIndex);

            if (entry.Compression == PakFileCompressionType.Mermaid || entry.Compression == PakFileCompressionType.Kraken)
            {
                if (!_checkedOodle)
                {
                    if (!Environment.Is64BitProcess)
                        throw new NotSupportedException("Use the 64 bit executable to extract (pak/bff uses Oodle)");

                    _checkedOodle = true;
                }

                byte[] decBuffer = ArrayPool<byte>.Shared.Rent((int)entry.FileSize);
                bool res = Oodle.Decompress(bytes, decBuffer, entry.FileSize);// Implement this
                if (res)
                {
                    using var fs = new FileStream(output, FileMode.Create);
                    fs.Write(decBuffer, 0, (int)entry.FileSize);
                }

                ArrayPool<byte>.Shared.Return(decBuffer);
                return res;
            }
            else if (entry.Compression == PakFileCompressionType.ZLib)
            {
                byte[] decBuffer = ArrayPool<byte>.Shared.Rent((int)entry.FileSize);

                int len;
                if (bytes[0] == 0x78 && bytes[1] == 0x9C) // Zlib magic
                {
                    Inflater inflater = new Inflater(noHeader: false);
                    inflater.SetInput(bytes, 0, (int)entry.PakSize);
                    len = inflater.Inflate(decBuffer, 0, (int)entry.FileSize);
                }
                else
                {
                    using var ms = new MemoryStream(bytes);
                    using var uncompStream = new DeflateStream(ms, CompressionMode.Decompress);
                    len = uncompStream.Read(decBuffer.AsSpan(0, (int)entry.FileSize));
                }

                if (len == entry.FileSize)
                {
                    using var fs = new FileStream(output, FileMode.Create);
                    fs.Write(decBuffer, 0, len);
                }

                ArrayPool<byte>.Shared.Return(decBuffer);
                return len == entry.FileSize;
            }
            else if (entry.Compression == PakFileCompressionType.LZX)
            {
                if (!_checkedXMem)
                {
                    if (Environment.Is64BitProcess)
                        throw new NotSupportedException("Use the 32 bit executable to extract (pak/bff uses XMemDecompress)");

                    _checkedXMem = true;
                }

                byte[] decBuffer = ArrayPool<byte>.Shared.Rent((int)entry.FileSize);

                var decompContext = new DecompressionContext();
                int pakLen = (int)entry.PakSize;
                int outLen = (int)entry.FileSize;
                ErrorCode err = decompContext.Decompress(bytes, 0, ref pakLen, decBuffer, 0, ref outLen);
                if (err != ErrorCode.None)
                    Console.WriteLine($"Error: Failed to unpack {extEntry.Path} (XMemDecompress/LZX) - Code: {(int)err:X8}");

                if (outLen == entry.FileSize)
                {
                    using var fs = new FileStream(output, FileMode.Create);
                    fs.Write(decBuffer, 0, outLen);
                }

                ArrayPool<byte>.Shared.Return(decBuffer);
                return outLen == entry.FileSize;
            }
            else if (entry.Compression != PakFileCompressionType.None)
            {
                Console.WriteLine($"Warning: Unrecognized compression type {entry.Compression} for {extEntry.Path}");
                return false;
            }
            else
            {
                // No compression
                File.WriteAllBytes(output, bytes);
                return true;
            }
        }
    }


    [Flags]
    public enum ePakFlags
    {
        None,
        BigEndian = 1,
        FilesOnDisk = 2
    }
}
