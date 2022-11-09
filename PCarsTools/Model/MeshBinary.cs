using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Syroot.BinaryData;

using PCarsTools.Encryption;

namespace PCarsTools.Model
{
    public class MeshBinary
    {
        public static void Load(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            using var bs = new BinaryStream(fs);

            uint hash = BHashCode.CreateHash32NoCase(fileName);
        }
    }
}
