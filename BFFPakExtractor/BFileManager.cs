using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

namespace BFFPakExtractor
{
    public class BFileManager
    {
        public List<BPakInfo> PakInfos { get; set; } = new();
        public List<BPakFile> Paks { get; set; } = new();

        public const string TagId = "TOCL";

        public BFileManager()
        {

        }

        public void LoadFromCompressedToc(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            using var bs = new BinaryStream(fs, ByteConverter.Little);

            string mID = bs.ReadString(4, Encoding.ASCII);
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
                Console.WriteLine($"{pakInfo.PakName}: {uid.ToString("X16")}");
                PakInfos.Add(pakInfo);
            }

            for (int i = 0; i < nPaks; i++)
            {
                var pakInfo = PakInfos[i];
                bs.Position = pakInfo.PakOffset;
                var pak = BPakFile.FromStream(bs, i);
            }
        }
    }
}
