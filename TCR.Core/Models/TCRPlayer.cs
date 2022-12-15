using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRCore
{
	public class TCRPlayer
	{
		public static TCRPlayer Server = new TCRPlayer()
		{
			PlayerId = -1,
			Name = "Server"
		};

		public int PlayerId { get; set; }
		public string Name { get; set; }
		public string GroupPrefix { get; set; }
		public string GroupSuffix { get; set; }
	}
}
