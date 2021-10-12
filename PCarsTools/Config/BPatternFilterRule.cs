using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools.Config
{
    public class BPatternFilterRule
    {
        public byte[] Pattern { get; set; }
        public string PatternDecrypted { get; set; }

        public uint Val { get; set; }

        public BPatternFilterRule()
        {
            Val = 0;
        }

        public bool Matches(byte[] input)
        {
            if (input is null)
                return false;

            if ( (((Val >> 1) & 1) == 0) && (((Val >> 2) & 1) == 0))
                 return false; // TODO implement Stristr

            byte xor = (byte)(Val >> 3);
            for (int i = 0; i < input.Length; i++)
            {
                while (i < input.Length)
                {
                    bool isUnk = Pattern[0] == (byte)'?';
                    char currentUp = char.ToUpper((char)input[i]);
                     
                    bool match = currentUp == char.ToUpper((char)(xor ^ Pattern[0])); // decrypt pattern
                    if (isUnk || match)
                        break;

                    i++;
                }

                if (i >= input.Length)
                    return false;

                // Try to find match across whole pattern
                int j = 0;
                while (j < Pattern.Length)
                {
                    bool isUnk = Pattern[j] == (byte)'?';
                    char currentUp = char.ToUpper((char)input[i]);
                    

                    bool match = currentUp == char.ToUpper((char)(xor ^ Pattern[j])); // decrypt pattern
                    if (!isUnk && !match)
                        break;

                    if (j == Pattern.Length - 1)
                        return true;

                    i++;
                    j++;
                }
            }

            return false;
        }
    }
}
