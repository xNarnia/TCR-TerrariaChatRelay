using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay
{
	public class TCRColor
	{
		public byte R;
		public byte G;
		public byte B;

		public TCRColor()
		{
			R = 0;
			G = 0;
			B = 0;
		}

		public TCRColor(byte R, byte G, byte B)
		{
			this.R = R;
			this.G = G;
			this.B = B;
		}
	}
}
