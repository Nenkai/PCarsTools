﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using BFFPakExtractor.Encryption;
using BFFPakExtractor.Xml;

namespace BFFPakExtractor.Config
{
    public class BConfig
    {
        public readonly static BConfig Instance = new BConfig();

        private BXmlFile _xml;

        private byte[] _configKey = new byte[]
        {
            0xEF, 0xEE, 0xC0, 0xCD, 0x92, 0xA6, 0x97, 0xF2, 0xE8, 0xDC, 0x80, 0xDC, 0xEE, 0xA1, 0xA0, 0xDA,
        };

        private List<BPatternFilter> _patternFilter = new List<BPatternFilter>();

        public void LoadConfig(string fileName)
        {
            var fileData = File.ReadAllBytes(fileName);
            BPakFileEncryption.DecryptTwoFish(fileData, _configKey);

            _xml = new BXmlFile();
            _xml.Load(fileData);
            _xml.Dump();

            var node = _xml.GetNodes("Languages/Encryption");

            var patternNode = node.GetFirstChildByName("Pattern");
            while (patternNode.IsValid())
            {
                var pattern = new BPatternFilter();
                patternNode.GetAttribute("Index").GetNumber(0, out float index);
                patternNode.GetAttribute("Data").GetStringBytes(0, out Memory<byte> data);

                pattern.Index = (int)index;
                pattern.SetDefaultAction(false);
                pattern.SetXOR(0xB3);
                pattern.AppendRule(data.ToArray(), true, false);
                _patternFilter.Add(pattern);

                patternNode = patternNode.GetNextSibling();
            }
        }

        public int GetPatternIdx(string fileName)
        {
            string str = fileName.ToLower().Replace('/', '\\');
            foreach (var patFilters in _patternFilter)
            {
                if (patFilters.IsAccepted(str))
                    return patFilters.Index;
            }

            return 0;
        }
    }
}