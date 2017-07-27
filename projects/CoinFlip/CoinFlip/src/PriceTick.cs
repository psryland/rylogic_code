using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using pr.extn;

namespace CoinFlip
{
	public class PriceTick
	{
		[JsonProperty]
		public List<object> Values { get; set; }
	//	[JsonProperty]
	//	public string Instrument { get; set; }
	//
	//	[JsonProperty]
	//	public string Last { get; set; }
	//
	//	[JsonProperty]
	//	public string LowestAsk { get; set; }
	//
	//	[JsonProperty]
	//	public string HighestBid { get; set; }
	//
	//	[JsonProperty]
	//	public string PercentChange { get; set; }
	//
	//	[JsonProperty]
	//	public string BaseVolume { get; set; }
	//
	//	[JsonProperty]
	//	public string QuoteVolume { get; set; }
	//
	//	[JsonProperty]
	//	public int IsFrozen { get; set; }
	//
	//	[JsonProperty]
	//	public string DaysHigh { get; set; }
	//
	//	[JsonProperty]
	//	public string DaysLow { get; set; }

		public override string ToString()
		{
			return string.Join(", ", Values);
		}
	}
}
