using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRDiscord.Models
{
	public class User
	{
		[JsonProperty("id")]
		public ulong Id { get; set; }

		[JsonProperty("username")]
		public string Username { get; set; }

		[JsonProperty("discriminator")]
		public string Discriminator { get; set; }

		[JsonProperty("avatar")]
		public string Avatar { get; set; }

		[JsonProperty("bot")]
		public bool IsBot { get; set; } = false;
	}
}