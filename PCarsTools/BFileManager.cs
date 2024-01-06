using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

using PCarsTools.Encryption;
using PCarsTools.Pak;
using PCarsTools.Base;

namespace PCarsTools
{
    public class BFileManager
    {
        public List<BPakInfo> PakInfos { get; set; } = new();
        public List<BPakFile> Paks { get; set; } = new();

        public const string TagId = "TOCL";

        public string TocFilePath;
        public BFileManager()
        {

        }

        public bool LoadFromCompressedToc(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            using var bs = new BinaryStream(fs, ByteConverter.Little);

            string mID = bs.ReadString(4, Encoding.ASCII);
            if (mID != new string(TagId.Reverse().ToArray()))
                return false;

            uint unk = bs.ReadUInt32();
            uint nPaks = bs.ReadUInt32();

            for (int i = 0; i < nPaks; i++)
            {
                bs.Position = 0x10 + (i * 0x110);

                var pakInfo = new BPakInfo();
                pakInfo.AssetCount = bs.ReadUInt32();
                pakInfo.PakSize = bs.ReadUInt32();
                pakInfo.PakOffset = bs.ReadUInt32();
                bs.ReadUInt32();
                pakInfo.PakName = bs.ReadString(0x100).TrimEnd('\0');

                ulong uid = BHashCode.CreateUidRaw(pakInfo.PakName);
                PakInfos.Add(pakInfo);
            }

            for (int i = 0; i < nPaks; i++)
            {
                var pakInfo = PakInfos[i];
                bs.Position = pakInfo.PakOffset;

                bool hasExtraInfo = false;
                if (fileName.Contains("compressed.toc")) // PCars GO
                    hasExtraInfo = true;

                var pak = new BPakFile();
                pak.FromStream(bs, withExtraInfo: hasExtraInfo, pakInfo.PakName);
                Paks.Add(pak);
            }

            TocFilePath = Path.GetDirectoryName(fileName);
            return true;
        }

        public void UnpackAll(string gameDirectory)
        {
            int totalCount = 0;
            int failed = 0;
            foreach (var pak in Paks)
            {
                // In PCARS, the ToC file entries contain metadata for each pak that it refers to, no actual data
                // Each pak entry does contain the entries but not the extra infos, it merely links to the bff anyway.
                if (File.Exists(Path.Combine(gameDirectory, pak.Path)))
                {
                    Console.WriteLine($"PAK Reference in ToC: {pak.Path}");
                    string pakPath = Path.Combine(gameDirectory, pak.Path);

                    var pakWithData = new BPakFile();
                    pakWithData.FromFile(pakPath, withExtraInfo: true); // Actual packs have extra infos

                    string outputDir = Path.Combine(Path.GetDirectoryName(pakPath), pak.Header.mFileName + "_extracted");

                    Directory.CreateDirectory(outputDir);
                    pakWithData.UnpackAll(outputDir);
                }
                else
                {
                    // Assume PCars GO where the paths don't point to an existing bff, more of a mount point of some sort
                    // In PCARS GO, each pak entries does have the extra infos, and link to actual files that are encrypted on disk
                    for (int i = 0; i < pak.Entries.Count; i++)
                    {
                        var entry = pak.Entries[i];
                        PakFileExtEntry extEntry = null;
                        if (pak.Header.mFlags.HasFlag(ePakFlags.FilesOnDisk))
                            extEntry = pak.ExtEntries[i];

                        // PCars GO, where files are just stored but encrypted, referenced by the toc
                        if (pak.UnpackFromLocalStoredFile(TocFilePath, entry, extEntry))
                        {
                            Console.WriteLine($"Unpacked: [{pak.Header.mFileName}]\\{extEntry.Path}");
                            totalCount++;
                        }
                        else
                        {
                            Console.WriteLine($"Failed to unpack: {extEntry.Path}");
                            failed++;
                        }
                    }
                }
            }

            Console.WriteLine($"Done. Extracted {totalCount} files ({failed} not extracted)");
        }

        public void Dumpfiles(string outfile)
        {
            using var sw = new StreamWriter(outfile);
            foreach (var pak in Paks)
            {
                sw.WriteLine($"Pak Name: {pak.Header.mFileName}");
                for (int i = 0; i < pak.Entries.Count; i++)
                {
                    sw.WriteLine(pak.ExtEntries[i].Path);
                }
                sw.WriteLine();
            }
        }
    }
}
