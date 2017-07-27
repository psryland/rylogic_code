using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Bittrex.API
{
	public class BittrexApiPublic :IDisposable
	{
		private string UrlBaseAddress;
		private HttpClient m_client;
		private JsonSerializer m_json;
		private CancellationToken m_cancel_token;

		public BittrexApiPublic(CancellationToken cancel_token, string base_address = "https://bittrex.com/")
		{
			m_cancel_token = cancel_token;
			UrlBaseAddress = base_address;
			m_client = HttpClientFactory.Create();
			m_json = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
		}
		public virtual void Dispose()
		{
			if (m_client != null)
			{
				m_client.Dispose();
				m_client = null;
			}
		}

		#region REST API Functions
		//https://bittrex.com/api/v1.1/public/getcurrencies 

		/// <summary>Return all available trading pairs and their latest price data</summary>
		public async Task<MarketsResponse> GetMarkets()
		{
			// https://bittrex.com/api/v1.1/public/getmarkets
			return await GetData<MarketsResponse>("getmarkets");
		}

		/// <summary>Return the order book for a market</summary>
		public async Task<OrderBookResponse> GetOrderBook(CurrencyPair pair, EGetOrderBookType type = EGetOrderBookType.Both, int depth = 20)
		{
			// https://bittrex.com/api/v1.1/public/getorderbook?market=BTC-LTC&type=both&depth=50  
			var response = await GetData<OrderBookResponse>("getorderbook", "market="+pair.Id, $"type={type.ToString().ToLowerInvariant()}", $"depth={depth}");
			if (response.Data != null)
				response.Data.Pair = pair;

			return response;
		}
		public enum EGetOrderBookType { Buy, Sell, Both }

		/// <summary>Helper for GETs</summary>
		private async Task<T> GetData<T>(string command, params object[] parameters)
		{
			// Create the URL for the command + parameters
			var url = string.Format("{0}api/v1.1/public/{1}{2}", UrlBaseAddress, command, (parameters.Length != 0 ? "?" + string.Join("&", parameters) : string.Empty));

			// Submit the request
			var response = await m_client.GetAsync(url, m_cancel_token);
			if (!response.IsSuccessStatusCode)
				throw new Exception(response.ReasonPhrase);

			// Interpret the reply
			var reply = await response.Content.ReadAsStringAsync();
			using (var tr = new JsonTextReader(new StringReader(reply)))
				return (T)m_json.Deserialize(tr, typeof(T));
		}

		#endregion
	}
}
