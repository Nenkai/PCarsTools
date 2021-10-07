using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFFPakExtractor.Config
{
    class BPatternFilter
    {
        /// <summary>
        /// 11 bits - 8 byte xor "key", 1 bit xor set, 1 bit has wildcard (?), 1 bit default action
        /// </summary>
        public uint Val { get; set; }
        public List<BPatternFilterRule> PatternRules;
        public int Index { get; set; }

        public BPatternFilter()
        {
            PatternRules = new List<BPatternFilterRule>();
            Val |= 1; // Set default action
            Val &= ~2u; // Clear wildcard
            Val &= ~4u; // Clear xor bit set
            Val &= 0xFFFFF807; // Clear xor
        }

        public void SetDefaultAction(bool value)
        {
            Val = (Val & 0xFFFFFFFE | (value ? 1u : 0u)); // First bit
        }

        public bool IsAccepted(string str)
        {
            var inputBytes = Encoding.ASCII.GetBytes(str);
            foreach (var rule in PatternRules)
            {
                if (rule.Matches(inputBytes))
                    return true;
            }

            return false;
        }

        public void SetXOR(byte xor)
        {
            Val = (uint)(Val & 0xFFFFF807u | (uint)(xor << 3)); // 8 bits xor

            uint hasXorBitSet = (xor != 0) ? 1u : 0u;
            Val = (uint)(Val & 0xFFFFFFFBu | (hasXorBitSet << 2)); // Third bit, set has xor bit
        }

        public void AppendRule(byte[] pattern, bool setDefaultAction, bool applyXorOnPattern)
        {
            var rule = new BPatternFilterRule();
            rule.Pattern = pattern;
            rule.Val = (setDefaultAction ? 1u : 0u) | rule.Val & 0xFFFFFFFE;

            if (pattern.Contains((byte)'?'))
                rule.Val |= 2;

            if (((this.Val >> 2) & 1) != 0) // Inherit xor?
            {
                if (applyXorOnPattern)
                {
                    for (int i = 0; i < pattern.Length; i++)
                        pattern[i] ^= (byte)(Val >> 3);
                }

                rule.Val |= 4; // Set xor set bit
                rule.Val = (uint)(8 * (byte)(this.Val >> 3)) | rule.Val & 0xFFFFF807; // Inherit xor
            }
            else
            {
                rule.Val &= ~4u; // Remove xor flag set
                rule.Val &= 0xFFFFF807; // Remove xor
            }

            PatternRules.Add(rule);
        }
    }
}
