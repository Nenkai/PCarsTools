using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using PCarsTools.Encryption;
using PCarsTools.Xml;
using System.Buffers.Binary;

namespace PCarsTools.Config
{
    public class BConfig
    {
        public readonly static BConfig Instance = new BConfig();

        private BXmlFile _xml;

        private byte[] _configKey_PC1 = new byte[]
        {
            0xCA, 0xE9, 0xA1, 0xC1, 0x93, 0xC7, 0xC6, 0xAD, 0xA4, 0x98, 0x94, 0xCC, 0xCE, 0xFA, 0xF6, 0x93
        };

        private byte[] _configKey_PC2AndAbove = new byte[]
        {
            0xEF, 0xEE, 0xC0, 0xCD, 0x92, 0xA6, 0x97, 0xF2, 0xE8, 0xDC, 0x80, 0xDC, 0xEE, 0xA1, 0xA0, 0xDA,
        };

        public List<BPatternFilter> PatternFilters { get; set; } = new();

        public bool LoadConfig(string fileName)
        {
            var fileData = File.ReadAllBytes(fileName);

            byte[] tmpData = new byte[fileData.Length];
            fileData.AsSpan().CopyTo(tmpData);

            // Try PC1 Key
            BPakFileEncryption.DecryptTwoFish(tmpData, _configKey_PC1);
            if (BinaryPrimitives.ReadInt32LittleEndian(tmpData) == 0x594D4C42)
            {
                fileData = tmpData;
            }
            else
            {
                // Try PC2+ Key
                fileData.AsSpan().CopyTo(tmpData);
                BPakFileEncryption.DecryptTwoFish(tmpData, _configKey_PC2AndAbove);

                if (BinaryPrimitives.ReadInt32LittleEndian(tmpData) == 0x594D4C42)
                {
                    fileData = tmpData;
                }
                else
                {
                    throw new Exception("Failed to decrypt config file (Languages/Languages.bml)");
                }
            }

            if (fileData.Length < 0x10)
                return false;

            _xml = new BXmlFile();
            _xml.Load(fileData);

            var node = _xml.GetNodes("Languages/Encryption");
            if (!node.IsValid())
                return false;

            var patternNode = node.GetFirstChildByName("Pattern");
            if (!patternNode.IsValid())
                return false;

            while (patternNode.IsValid())
            {
                var pattern = new BPatternFilter();
                patternNode.GetAttribute("Index").GetNumber(0, out float index);
                patternNode.GetAttribute("Data").GetStringBytes(0, out Memory<byte> data);

                pattern.Index = (int)index;
                pattern.SetDefaultAction(false);
                pattern.SetXOR(0xB3);
                pattern.AppendRule(data.ToArray(), true, false);
                PatternFilters.Add(pattern);

                patternNode = patternNode.GetNextSibling();
            }

            foreach (var i in PatternFilters)
            {
                foreach (var j in i.PatternRules)
                {
                    char[] decrypted = new char[j.Pattern.Length];
                    for (int k = 0; k < decrypted.Length; k++)
                        decrypted[k] = (char)((byte)j.Pattern[k] ^ (PatternFilters[0].Val >> 3)); // Xor

                    j.PatternDecrypted = new string(decrypted);
                }
            }
            return true;
        }

        public int GetPatternIdx(string fileName)
        {
            string str = fileName.ToLower().Replace('/', '\\');
            foreach (var patFilters in PatternFilters)
            {
                if (patFilters.IsAccepted(str))
                    return patFilters.Index;
            }

            return 0;
        }
    }
}
