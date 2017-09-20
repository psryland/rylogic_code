using Cryptopia.API.DataObjects;
using Cryptopia.API.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Cryptopia.API
{
	public class CryptopiaApiPublic : ICryptopiaApiPublic
	{
		private HttpClient _client;
		private string _apiBaseAddress;
		private CancellationToken m_cancel_token;

		public CryptopiaApiPublic(CancellationToken cancel_token, string apiBaseAddress = "https://www.cryptopia.co.nz")
		{
			m_cancel_token = cancel_token;
			_apiBaseAddress = apiBaseAddress;
			_client = HttpClientFactory.Create();
		}
		public void Dispose()
		{
			if (_client != null)
			{
				_client.Dispose();
				_client = null;
			}
		}

		#region API Calls

		public async Task<DataObjects.CurrenciesResponse> GetCurrencies()
		{
			return await GetResult<DataObjects.CurrenciesResponse>(PublicApiCall.GetCurrencies, null);
		}

		public async Task<DataObjects.TradePairsResponse> GetTradePairs()
		{
			return await GetResult<DataObjects.TradePairsResponse>(PublicApiCall.GetTradePairs, null);
		}

		public async Task<DataObjects.MarketsResponse> GetMarkets(MarketsRequest request)
		{
			var query = request.Hours.HasValue ? $"/{request.Hours}" : null;
			return await GetResult<DataObjects.MarketsResponse>(PublicApiCall.GetMarkets, query);
		}

		public async Task<DataObjects.MarketResponse> GetMarket(MarketRequest request)
		{
			var query = request.Hours.HasValue ? $"/{request.TradePairId}/{request.Hours}" : $"/{request.TradePairId}";
			return await GetResult<DataObjects.MarketResponse>(PublicApiCall.GetMarket, query);
		}

		public async Task<DataObjects.MarketHistoryResponse> GetMarketHistory(MarketHistoryRequest request)
		{
			var query = $"/{request.TradePairId}";
			return await GetResult<DataObjects.MarketHistoryResponse>(PublicApiCall.GetMarketHistory, query);
		}

		public async Task<DataObjects.MarketOrdersResponse> GetMarketOrders(MarketOrdersRequest request)
		{
			var query = request.OrderCount.HasValue ? $"/{request.TradePairId}/{request.OrderCount}" : $"/{request.TradePairId}";
			return await GetResult<DataObjects.MarketOrdersResponse>(PublicApiCall.GetMarketOrders, query);
		}

		public async Task<DataObjects.MarketOrderGroupsResponse> GetMarketOrderGroups(MarketOrderGroupsRequest request)
		{
			var query = request.OrderCount.HasValue ? $"/{string.Join("-", request.TradePairIds)}/{request.OrderCount}" : $"/{string.Join("-", request.TradePairIds)}";
			return await GetResult<DataObjects.MarketOrderGroupsResponse>(PublicApiCall.GetMarketOrderGroups, query);
		}

		/// <summary>Helper for GETs</summary>
		public async Task<T> GetResult<T>(PublicApiCall call, string requestData) where T : IResponse, new()
		{
			// Create the URL for the command + parameters
			var url = string.Format("{0}/Api/{1}{2}", _apiBaseAddress, call, requestData);

			// Submit the request
			var response = await _client.GetAsync(url, m_cancel_token);
			if (!response.IsSuccessStatusCode)
				throw new HttpResponseException(response);

			// Interpret the reply
			var reply = await response.Content.ReadAsStringAsync();
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(reply)))
			{
				var serializer = new DataContractJsonSerializer(typeof(T));
				return (T)(object)serializer.ReadObject(stream);
			}
		}

		#endregion
	}
}
