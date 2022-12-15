using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCRCore.Command;

namespace TCRCore
{
	public class TCRClientUser
	{
		public string Client { get; set; }
		public string Username { get; set; }
		public Permission PermissionLevel { get; set; }
		public TCRClientUser(string client, string username, Permission permissionLevel)
		{
			Client = client;
			Username = username;
			PermissionLevel = permissionLevel;
		}
	}
}
