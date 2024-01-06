using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Syroot.BinaryData;
using System.Numerics;
using System.Buffers.Binary;

namespace PCarsTools.Texture
{
    public class TextureFile
    {
        public static void RemovePC3Padding(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            using var bs = new BinaryStream(fs);

            if (fs.Length < 4)
                return;

            uint possibleMagic;
            do
            {
                possibleMagic = bs.ReadUInt32();
            } while (possibleMagic != 0x20534444);
            bs.Position -= 4;

            using var fs2 = new FileStream(fileName.Substring(0, fileName.Length - 4) + ".dds", FileMode.CreateNew);
			using var bs2 = new BinaryStream(fs2);
            byte[] bytes = bs.ReadBytes((int)(fs.Length - fs.Position));
            bs2.Write(bytes);
        }
    }
}
