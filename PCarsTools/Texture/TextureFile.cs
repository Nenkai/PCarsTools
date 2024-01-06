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
            FileInfo file = new FileInfo(fileName);

			int i = 0;
			uint j = bs.ReadUInt32();
			while (j != 542327876){
				i++;
				bs.Seek(i, SeekOrigin.Begin);
				j = bs.ReadUInt32();
			}
			using var fs2 = new FileStream(fileName.Substring(0, fileName.Length - 4) + ".dds", FileMode.CreateNew);
			using var bs2 = new BinaryStream(fs2);
			bs.Seek(i, SeekOrigin.Begin);
            byte[] bytes = bs.ReadBytes((int)file.Length - i);
            bs2.Write(bytes);
            bs2.Flush();
            bs.Close();
            fs.Close();
            file.Delete();
            bs2.Close();
            fs2.Close();
        }
    }
}
