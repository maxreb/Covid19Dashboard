using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Covid19Dashboard.Entities
{
	public class Campaign
	{
		public string Text { get; set; } = string.Empty;
		public List<(Uri uri, string text)>? Links { get; set; }
		public DateTime DateUTC { get; set; }
	}
}
