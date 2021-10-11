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

namespace PCarsTools
{
    public class BPakFile
    {
        public const string TagId = "PAK ";

        public BVersion Version { get; set; }
        public string Name { get; set; }
        public bool BigEndian { get; set; }
        public eEncryptionType EncryptionType { get; set; }

        public List<BPakFileTocEntry> Entries { get; set; }
        public List<BExtendedFileInfoEntry> ExtEntries { get; set; }

        public int KeyIndex { get; set; }

        public static BPakFile FromStream(Stream stream, int index = 0)
        {
            var pak = new BPakFile();
            int pakOffset = (int)stream.Position;

            using var bs = new BinaryStream(stream, leaveOpen: true);
            int mID = bs.ReadInt32();
            pak.Version = new BVersion(bs.ReadUInt32());
            int fileCount = bs.ReadInt32();
            bs.Position += 12;
            pak.Name = bs.ReadString(0x100).TrimEnd('\0');
            pak.KeyIndex = BConfig.Instance.GetPatternIdx(pak.Name);

            uint pakFileTocEntrySize = bs.ReadUInt32();
            uint crc = bs.ReadUInt32();
            uint extInfoSize = bs.ReadUInt32();
            bs.Position += 8;
            pak.BigEndian = bs.ReadBoolean(BooleanCoding.Byte);
            pak.EncryptionType = (eEncryptionType)bs.Read1Byte();
            bs.Position += 2;

            var pakTocBuffer = bs.ReadBytes((int)pakFileTocEntrySize);
            if (pak.EncryptionType != eEncryptionType.None)
                BPakFileEncryption.DecryptData(pak.EncryptionType, pakTocBuffer, pakTocBuffer.Length, pak.KeyIndex);

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

            const int unkCertXmlSize = 0x308;
            bs.Position += unkCertXmlSize;

            int baseExtOffset = (int)bs.Position - pakOffset;
            int extInfoEntriesSize = (int)Utils.AlignValue(extInfoSize - unkCertXmlSize, 0x10);
            var extTocBuffer = bs.ReadBytes(extInfoEntriesSize);

            bool returnBuffer = false;
            if (pak.EncryptionType != eEncryptionType.None)
            {
                var scribeDecrypt = new ScribeDecrypt();
                scribeDecrypt.CreateSchedule();

                if (extTocBuffer.Length % 0x10 != 0) // Must be aligned to 0x10
                {
                    int rem = extTocBuffer.Length % 0x10;
                    byte[] extTocBufferAligned = ArrayPool<byte>.Shared.Rent(extTocBuffer.Length + rem);
                    extTocBuffer.AsSpan().CopyTo(extTocBufferAligned);
                    extTocBuffer = extTocBufferAligned;
                    returnBuffer = true;
                }

                var d = MemoryMarshal.Cast<byte, uint>(extTocBuffer);
                scribeDecrypt.Decrypt(d);
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

                pak.ExtEntries.Add(extEntry);

                if (extEntry.Path.Contains(@"Properties\GUI\FontsMetadata.bin", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = File.ReadAllBytes(@"C:\Users\nenkai\Desktop\Hydra\64bit\Properties\GUI\FontsMetadata.bin");
                    BPakFileEncryption.DecryptData(pak.EncryptionType, bytes, bytes.Length, pak.KeyIndex);
                }
            }

            if (returnBuffer)
                ArrayPool<byte>.Shared.Return(extTocBuffer);

            return pak;
        }

        public bool UnpackFromLocalFile(string outputDir, BPakFileTocEntry entry, BExtendedFileInfoEntry extEntry)
        {
            string localPath = Path.Combine(outputDir, extEntry.Path);
            if (File.Exists(localPath))
            {
                return Unpack(entry, extEntry, localPath, localPath + ".dec");
            }
            else
            {
                Console.WriteLine($"File {extEntry.Path} not found to extract, can be ignored");
            }

            return false;
        }

        private bool Unpack(BPakFileTocEntry entry, BExtendedFileInfoEntry extEntry, string inputFile, string output)
        {
            var bytes = File.ReadAllBytes(inputFile);
            if (this.EncryptionType != eEncryptionType.None)
               BPakFileEncryption.DecryptData(this.EncryptionType, bytes, bytes.Length, this.KeyIndex);

            if (entry.Compression == PakFileCompressionType.Mermaid || entry.Compression == PakFileCompressionType.Kraken)
            {
                byte[] dec = ArrayPool<byte>.Shared.Rent((int)entry.FileSize);
                bool res = Oodle.Decompress(bytes, dec, entry.FileSize);// Implement this
                if (res)
                    File.WriteAllBytes(output, dec);
                return res;
            }
            else if (entry.Compression == PakFileCompressionType.ZLib)
            {
                byte[] dec = ArrayPool<byte>.Shared.Rent((int)entry.FileSize);
                using var ms = new MemoryStream(bytes);
                using var uncompStream = new DeflateStream(ms, CompressionMode.Decompress);
                int len = uncompStream.Read(dec);
                if (len == entry.FileSize)
                    File.WriteAllBytes(output, dec);
                return len == entry.FileSize;
            }
            else if (entry.Compression != PakFileCompressionType.None)
            {
                Console.WriteLine($"Warning: Unrecognized compression type {entry.Compression} for {extEntry.Path}");
                return false;
            }

            return false;
        }
    }
}
