using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRDiscord.Models
{
	public class Embed
	{
		[JsonProperty("title")]
		public string Title { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("url")]
		public string URL { get; set; }

		[JsonProperty("timestamp")]
		public DateTime Timestamp { get; set; }

		[JsonProperty("color")]
		public int Color { get; set; }

		[JsonProperty("footer")]
		public Footer Footer { get; set; }
	}
}
