using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Bittrex.API.DomainObjects
{
	[DebuggerDisplay("{Id}")]
	public class TradeResult
	{
		/// <summary>The order Id for an order added to the order book of the traded pair</summary>
		[JsonProperty("uuid")]
		public Guid Id { get; set; }
	}
}
