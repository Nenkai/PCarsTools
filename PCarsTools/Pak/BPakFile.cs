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
using PCarsTools.Base;

namespace PCarsTools.Pak
{
    public class BPakFile
    {
        public const string TagId = "PAK ";

        public PakFileHeader Header { get; private set; }
        public PakFileExtHeader ExtHeader { get; private set; }

        public List<PakFileTocEntry> Entries { get; set; }
        public List<PakFileExtEntry> ExtEntries { get; set; }

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
        public void FromFile(string inputFile, bool withExtraInfo = true)
        {
            var fs = new FileStream(inputFile, FileMode.Open);
            FromStream(fs, withExtraInfo: withExtraInfo, tocFileName: inputFile);
            _fs = fs;
        }

        public void FromStream(Stream stream, bool withExtraInfo = false, string tocFileName = null)
        {
            int pakOffset = (int)stream.Position;
            Path = tocFileName.ToLower().Replace('/', '\\');

            var bs = new BinaryStream(stream);
            Header = new PakFileHeader();
            Header.Read(bs);

            bool keyIndexFound = false;
            KeyIndex = BConfig.Instance.GetPatternIdx(Header.mFileName);
            if (KeyIndex == 0 && !string.IsNullOrEmpty(Path)) // Default key found, try to see if its in the path
            {
                foreach (var filter in BConfig.Instance.PatternFilters)
                {
                    foreach (var rule in filter.PatternRules)
                    {
                        if (Path.Contains(rule.PatternDecrypted))
                        {
                            KeyIndex = filter.Index;
                            keyIndexFound = true;
                            goto found;
                        }
                    }
                }
            found:
                ;
            }

            var pakTocBuffer = bs.ReadBytes((int)Header.mTocSize);

            if (Header.mEncryption != eEncryptionType.None)
            {
                if (!keyIndexFound && KeyIndex == 0) // Still not found? Attempt bruteforce
                {
                    int j = 0;
                    for (int i = 0; i < 32; i++)
                    {
                        var tmpData = pakTocBuffer.ToArray();
                        BPakFileEncryption.DecryptData(Header.mEncryption, tmpData, tmpData.Length, i);
                        if (tmpData[14] == 0 && tmpData[15] == 0)
                        {
                            KeyIndex = i;
                            break;
                        }
                    }
                }

                BPakFileEncryption.DecryptData(Header.mEncryption, pakTocBuffer, pakTocBuffer.Length, KeyIndex);
            }

            if (pakTocBuffer[14] != 0 && pakTocBuffer[15] != 0) // Check if first entry offset is absurdly too big that its possibly not decrypted correctly
                Console.WriteLine($"Warning - possible crash: {Header.mFileName} toc could most likely not be decrypted correctly using key No.{KeyIndex}");

            Entries = new List<PakFileTocEntry>((int)Header.mFileCount);
            SpanReader sr = new SpanReader(pakTocBuffer);
            for (int i = 0; i < Header.mFileCount; i++)
            {
                sr.Position = i * 0x2A;

                var pakFileToCEntry = new PakFileTocEntry();
                pakFileToCEntry.Read(ref sr);

                Entries.Add(pakFileToCEntry);
            }

            if (withExtraInfo)
            {
                ExtHeader = new PakFileExtHeader();
                ExtHeader.Read(bs);

                int baseExtOffset = (int)bs.Position - pakOffset;
                int extInfoEntriesSize = (int)Utils.AlignValue(Header.mExtInfoSize - PakFileExtHeader.GetSize(), 0x10);
                var extTocBuffer = bs.ReadBytes(extInfoEntriesSize);

                bool returnBuffer = false;
                if (Header.mEncryption != eEncryptionType.None)
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
                        BPakFileEncryption.DecryptData(Header.mEncryption, tmp, tmp.Length, 0);

                        if (tmp[6] != 0 && tmp[7] != 0)
                            Console.WriteLine("Warning: possibly failed to decrypt Extended Info Table");

                        extTocBuffer = tmp;
                    }

                    extTocBuffer = tmp;
                }

                ExtEntries = new List<PakFileExtEntry>((int)Header.mFileCount);
                sr = new SpanReader(extTocBuffer);
                for (int i = 0; i < Header.mFileCount; i++)
                {
                    sr.Position = i * 0x10;

                    var extEntry = new PakFileExtEntry();
                    extEntry.Read(ref sr);

                    sr.Position = (int)extEntry.mNameOffset - baseExtOffset;
                    extEntry.Path = sr.ReadString1();

                    ulong uid = BHashCode.CreateUidRaw(extEntry.Path);
                    if (Entries[i].mUid != uid)
                        Console.WriteLine($"Warning - unmatched UID/Hash: {extEntry.Path} (target={Entries[i].mUid:X16}, got={uid}");

                    ExtEntries.Add(extEntry);
                }

                if (returnBuffer)
                    ArrayPool<byte>.Shared.Return(extTocBuffer);
            }
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
                    Console.WriteLine($"Unpacked: [{Header.mFileName}]\\{extEntry.Path}");
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

        public bool UnpackFromLocalStoredFile(string outputDir, PakFileTocEntry entry, PakFileExtEntry extEntry)
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

        public bool UnpackFromStream(PakFileTocEntry entry, PakFileExtEntry extEntry, string output)
        {
            if (_fs is null)
                throw new InvalidOperationException("Can't extract from stream from a toc file based pak");

            _fs.Position = (long)entry.mDataPos;

            var bytes = ArrayPool<byte>.Shared.Rent((int)entry.mSizeInPak);
            _fs.Read(bytes);

            bool result = Unpack(bytes, entry, extEntry, output);

            var time = new BDateTime(entry.mModifiedTime >> 12); // For some reason the time here is 12 bits higher

            File.SetCreationTime(output, new DateTime((int)time.Year, (int)time.Month, (int)time.Day, (int)time.Hour, (int)time.Min, (int)time.Sec));
            File.SetLastWriteTime(output, new DateTime((int)time.Year, (int)time.Month, (int)time.Day, (int)time.Hour, (int)time.Min, (int)time.Sec));

            ArrayPool<byte>.Shared.Return(bytes);
            return result;
        }

        private bool UnpackFromFile(PakFileTocEntry entry, PakFileExtEntry extEntry, string inputFile, string output)
        {
            var bytes = File.ReadAllBytes(inputFile);
            return Unpack(bytes, entry, extEntry, output);
        }

        private static bool _checkedOodle;
        private static bool _checkedXMem;

        private bool Unpack(byte[] bytes, PakFileTocEntry entry, PakFileExtEntry extEntry, string output)
        {
            if (Header.mEncryption != eEncryptionType.None)
                BPakFileEncryption.DecryptData(Header.mEncryption, bytes, bytes.Length, KeyIndex);

            if (entry.mFileType == PakFileCompressionType.Mermaid || entry.mFileType == PakFileCompressionType.Kraken)
            {
                if (!_checkedOodle)
                {
                    if (!Environment.Is64BitProcess)
                        throw new NotSupportedException("Use the 64 bit executable to extract (pak/bff uses Oodle)");

                    _checkedOodle = true;
                }

                byte[] decBuffer = ArrayPool<byte>.Shared.Rent((int)entry.mOriginalSize);
                bool res = Oodle.Decompress(bytes, decBuffer, entry.mOriginalSize);// Implement this
                if (res)
                {
                    using var fs = new FileStream(output, FileMode.Create);
                    fs.Write(decBuffer, 0, (int)entry.mOriginalSize);
                }

                ArrayPool<byte>.Shared.Return(decBuffer);
                return res;
            }
            else if (entry.mFileType == PakFileCompressionType.ZLib)
            {
                byte[] decBuffer = ArrayPool<byte>.Shared.Rent((int)entry.mOriginalSize);

                int len;
                if (bytes[0] == 0x78 && bytes[1] == 0x9C) // Zlib magic
                {
                    Inflater inflater = new Inflater(noHeader: false);
                    inflater.SetInput(bytes, 0, (int)entry.mSizeInPak);
                    len = inflater.Inflate(decBuffer, 0, (int)entry.mOriginalSize);
                }
                else
                {
                    using var ms = new MemoryStream(bytes);
                    using var uncompStream = new DeflateStream(ms, CompressionMode.Decompress);
                    len = uncompStream.Read(decBuffer.AsSpan(0, (int)entry.mOriginalSize));
                }

                if (len == entry.mOriginalSize)
                {
                    using var fs = new FileStream(output, FileMode.Create);
                    fs.Write(decBuffer, 0, len);
                }

                ArrayPool<byte>.Shared.Return(decBuffer);
                return len == entry.mOriginalSize;
            }
            else if (entry.mFileType == PakFileCompressionType.LZX)
            {
                if (!_checkedXMem)
                {
                    if (Environment.Is64BitProcess)
                        throw new NotSupportedException("Use the 32 bit executable to extract (pak/bff uses XMemDecompress)");

                    _checkedXMem = true;
                }

                byte[] decBuffer = ArrayPool<byte>.Shared.Rent((int)entry.mOriginalSize);

                var decompContext = new DecompressionContext();
                int pakLen = (int)entry.mSizeInPak;
                int outLen = (int)entry.mOriginalSize;
                ErrorCode err = decompContext.Decompress(bytes, 0, ref pakLen, decBuffer, 0, ref outLen);
                if (err != ErrorCode.None)
                    Console.WriteLine($"Error: Failed to unpack {extEntry.Path} (XMemDecompress/LZX) - Code: {(int)err:X8}");

                if (outLen == entry.mOriginalSize)
                {
                    using var fs = new FileStream(output, FileMode.Create);
                    fs.Write(decBuffer, 0, outLen);
                }

                ArrayPool<byte>.Shared.Return(decBuffer);
                return outLen == entry.mOriginalSize;
            }
            else if (entry.mFileType != PakFileCompressionType.None)
            {
                Console.WriteLine($"Warning: Unrecognized compression type {entry.mFileType} for {extEntry.Path}");
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
