using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

using PCarsTools.Encryption;

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

                ulong uid = BUid.HashString(pakInfo.PakName);
                PakInfos.Add(pakInfo);
            }

            for (int i = 0; i < nPaks; i++)
            {
                var pakInfo = PakInfos[i];
                bs.Position = pakInfo.PakOffset;
                var pak = BPakFile.FromStream(bs, null);
                Paks.Add(pak);
            }

            TocFilePath = Path.GetDirectoryName(fileName);
            return true;
        }

        public void UnpackAll()
        {
            int totalCount = 0;
            int failed = 0;
            foreach (var pak in Paks)
            {
                for (int i = 0; i < pak.Entries.Count; i++)
                {
                    var entry = pak.Entries[i];
                    var extEntry = pak.ExtEntries[i];

                    if (pak.UnpackFromLocalFile(TocFilePath, entry, extEntry))
                    {
                        Console.WriteLine($"Unpacked: [{pak.Name}]\\{extEntry.Path}");
                        totalCount++;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to unpack: {extEntry.Path}");
                        failed++;
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
                sw.WriteLine($"Pak Name: {pak.Name}");
                for (int i = 0; i < pak.Entries.Count; i++)
                {
                    sw.WriteLine(pak.ExtEntries[i].Path);
                }
                sw.WriteLine();
            }
        }
    }
}
