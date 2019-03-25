using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cryptopia.API
{
	public class Currency
	{
		/// <summary></summary>
		[JsonProperty("Id")]
		public int Id { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Name")]
		public string Name { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Symbol")]
		public string Symbol { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Algorithm")]
		public string Algorithm { get; internal set; }
	}

	public class CurrenciesResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public List<Currency> Data { get; internal set; }
	}
}
