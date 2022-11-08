using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools.Encryption
{
	public class RC4
	{
		public static void Crypt(Span<byte> pwd, Span<byte> input, Span<byte> output, int len)
		{
			int a, i, j, k, tmp;
			int[] key, box;

			key = new int[256];
			box = new int[256];

			for (i = 0; i < 256; i++)
			{
				key[i] = pwd[i % pwd.Length];
				box[i] = i;
			}
			for (j = i = 0; i < 256; i++)
			{
				j = (j + box[i] + key[i]) % 256;
				tmp = box[i];
				box[i] = box[j];
				box[j] = tmp;
			}
			for (a = j = i = 0; i < len; i++)
			{
				a++;
				a %= 256;
				j += box[a];
				j %= 256;
				tmp = box[a];
				box[a] = box[j];
				box[j] = tmp;
				k = box[((box[a] + box[j]) % 256)];
				output[i] = (byte)(input[i] ^ k);
			}
		}
	}
}
