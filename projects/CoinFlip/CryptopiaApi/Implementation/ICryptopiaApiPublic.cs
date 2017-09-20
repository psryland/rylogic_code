using Cryptopia.API.DataObjects;
using System;
using System.Threading.Tasks;

namespace Cryptopia.API.Implementation
{
	public interface ICryptopiaApiPublic : IDisposable
	{
		Task<T> GetResult<T>(PublicApiCall call, string requestData) where T : IResponse, new();
		Task<DataObjects.CurrenciesResponse> GetCurrencies();
		Task<DataObjects.MarketResponse> GetMarket(DataObjects.MarketRequest request);
		Task<DataObjects.MarketHistoryResponse> GetMarketHistory(DataObjects.MarketHistoryRequest request);
		Task<DataObjects.MarketOrdersResponse> GetMarketOrders(DataObjects.MarketOrdersRequest request);
		Task<DataObjects.MarketsResponse> GetMarkets(DataObjects.MarketsRequest request);
		Task<DataObjects.TradePairsResponse> GetTradePairs();
	}
}
