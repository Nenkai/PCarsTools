using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFFPakExtractor
{
    public class Utils
    {
		public static int ClampWrap(int value, int min, int max)
		{
			if (value > max)
				return min;
			else if (value < min)
				return max;

			return value;
		}
	}
}
