using System.Collections.Generic;
using Cryptopia.API.Implementation;

namespace Cryptopia.API.DataObjects
{

	public class SubmitTradeResponse : IResponse
	{
		/// <summary>Gets or sets a value indicating whether this response was successful.</summary>
		public bool Success { get; set; }

		/// <summary>Gets or sets the error if the response is not successful.</summary>
		public string Error { get; set; }

		/// <summary>Gets or sets the data.</summary>
		public SubmitTradeData Data { get; set; }
	}

	public class SubmitTradeData
	{
		/// <summary>Get/Set the created order identifier. This is the ID of an order added to the order book for the pair</summary>
		public int? OrderId { get; set; }

		/// <summary>
		/// Get/Set the list of any filled orders. This is the IDs of completed trades.
		/// If an order can be filled (possibly partially) by orders in the order book, then these trades
		/// are performed immediately and their trade IDs are returned here</summary>
		public List<int> FilledOrders { get; set; }
	}
}
