using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	/// <summary>Helper for decoding error responses</summary>
	internal class ErrorResult
	{
		[JsonProperty("error")]
		public string? Message { get; set; }
	}
}
