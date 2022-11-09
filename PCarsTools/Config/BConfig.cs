using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using PCarsTools.Encryption;
using PCarsTools.Xml;

namespace PCarsTools.Config
{
    public class BConfig
    {
        public readonly static BConfig Instance = new BConfig();

        private BXmlFile _xml;

        private byte[] _configKey = new byte[]
        {
            0xEF, 0xEE, 0xC0, 0xCD, 0x92, 0xA6, 0x97, 0xF2, 0xE8, 0xDC, 0x80, 0xDC, 0xEE, 0xA1, 0xA0, 0xDA,
        };

        public List<BPatternFilter> PatternFilters { get; set; } = new();

        public bool LoadConfig(string fileName)
        {
            var fileData = File.ReadAllBytes(fileName);
            BPakFileEncryption.DecryptTwoFish(fileData, _configKey);

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
