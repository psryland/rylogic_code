using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	/// <summary>Helper for decoding error responses</summary>
	internal class ErrorResult
	{
		[JsonProperty("error")]
		public string Message { get; private set; }
	}
}
