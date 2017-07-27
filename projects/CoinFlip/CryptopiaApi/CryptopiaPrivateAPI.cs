using Cryptopia.API.DataObjects;
using Cryptopia.API.Implementation;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace Cryptopia.API
{
	public class CryptopiaApiPrivate : ICryptopiaApiPrivate
	{
		private HttpClient _client;
		private string _apiBaseAddress;
		private CancellationToken m_cancel_token;

		public CryptopiaApiPrivate(string key, string secret, CancellationToken cancel_token, string apiBaseAddress = "https://www.cryptopia.co.nz")
		{
			m_cancel_token = cancel_token;
			_apiBaseAddress = apiBaseAddress;
			_client = HttpClientFactory.Create(new AuthDelegatingHandler(key, secret));
		}
		public void Dispose()
		{
			if (_client != null)
			{
				_client.Dispose();
				_client = null;
			}
		}

		#region Api Calls

		public async Task<CancelTradeResponse> CancelTrade(CancelTradeRequest request)
		{
			return await GetResult<CancelTradeResponse, CancelTradeRequest>(PrivateApiCall.CancelTrade, request);
		}

		public async Task<SubmitTradeResponse> SubmitTrade(SubmitTradeRequest request)
		{
			return await GetResult<SubmitTradeResponse, SubmitTradeRequest>(PrivateApiCall.SubmitTrade, request);
		}

		public async Task<BalanceResponse> GetBalances(BalanceRequest request)
		{
			return await GetResult<BalanceResponse, BalanceRequest>(PrivateApiCall.GetBalance, request);
		}

		public async Task<OpenOrdersResponse> GetOpenOrders(OpenOrdersRequest request)
		{
			return await GetResult<OpenOrdersResponse, OpenOrdersRequest>(PrivateApiCall.GetOpenOrders, request);
		}

		public async Task<TradeHistoryResponse> GetTradeHistory(TradeHistoryRequest request)
		{
			return await GetResult<TradeHistoryResponse, TradeHistoryRequest>(PrivateApiCall.GetTradeHistory, request);
		}

		public async Task<TransactionResponse> GetTransactions(TransactionRequest request)
		{
			return await GetResult<TransactionResponse, TransactionRequest>(PrivateApiCall.GetTransactions, request);
		}

		public async Task<DepositAddressResponse> GetDepositAddress(DepositAddressRequest request)
		{
			return await GetResult<DepositAddressResponse, DepositAddressRequest>(PrivateApiCall.GetDepositAddress, request);
		}

		public async Task<SubmitTipResponse> SubmitTip(SubmitTipRequest request)
		{
			return await GetResult<SubmitTipResponse, SubmitTipRequest>(PrivateApiCall.SubmitTip, request);
		}

		public async Task<SubmitWithdrawResponse> SubmitWithdraw(SubmitWithdrawRequest request)
		{
			return await GetResult<SubmitWithdrawResponse, SubmitWithdrawRequest>(PrivateApiCall.SubmitWithdraw, request);
		}

		public async Task<T> GetResult<T, U>(PrivateApiCall call, U requestData)
			where T : IResponse
			where U : IRequest
		{
			var url = string.Format("{0}/Api/{1}", _apiBaseAddress.TrimEnd('/'), call);
			var response = await _client.PostAsJsonAsync(url, requestData, m_cancel_token);
			return await response.Content.ReadAsAsync<T>(m_cancel_token);
		}

		#endregion
	}
}
