using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cryptopia.API.DataObjects;
using Cryptopia.API.Implementation;

namespace Cryptopia.API
{
#if false
	public class CryptopiaApiPrivate : ICryptopiaApiPrivate
	{
		private const int MaxRequestsPerSecond = 6;
		private string UrlBaseAddress;
		private HttpClient m_client;
		private JsonSerializer m_json;
		private CancellationToken m_cancel_token;
		private readonly string m_key;
		private readonly string m_secret;

		public CryptopiaApiPrivate(string key, string secret, CancellationToken cancel_token, string apiBaseAddress = "https://www.cryptopia.co.nz")
		{
			m_key = key;
			m_secret = secret;
			m_cancel_token = cancel_token;
			UrlBaseAddress = apiBaseAddress;

			m_client = new HttpClient { BaseAddress = new Uri(UrlBaseAddress) };

		}
		public void Dispose()
		{
			Debug.Assert(m_cancel_token.IsCancellationRequested, "Cancel should have been signalled before here");
			if (m_client != null)
			{
				m_client.Dispose();
				m_client = null;
			}
		}

		/// <summary>Hasher</summary>
		private HMACSHA512 Hasher { get; set; }

	#region API Calls

		/// <summary>Return the balances for the account</summary>
		public async Task<BalanceResponse> GetBalances(BalanceRequest request)
		{
			return await GetResult<BalanceResponse, BalanceRequest>(PrivateApiCall.GetBalance, request);
		}

		public async Task<CancelTradeResponse> CancelTrade(CancelTradeRequest request)
		{
			return await GetResult<CancelTradeResponse, CancelTradeRequest>(PrivateApiCall.CancelTrade, request);
		}

		public async Task<SubmitTradeResponse> SubmitTrade(SubmitTradeRequest request)
		{
			return await GetResult<SubmitTradeResponse, SubmitTradeRequest>(PrivateApiCall.SubmitTrade, request);
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
			var url = string.Format("{0}/Api/{1}", UrlBaseAddress.TrimEnd('/'), call);
			var response = await m_client.PostAsJsonAsync(url, requestData, m_cancel_token);
			return await response.Content.ReadAsAsync<T>(m_cancel_token);
		}

	#endregion
	}

#else

	public class CryptopiaApiPrivate : ICryptopiaApiPrivate
	{
		private const int MaxRequestsPerSecond = 6;
		private string UrlBaseAddress;
		private HttpClient m_client;
		private CancellationToken m_cancel_token;

		public CryptopiaApiPrivate(string key, string secret, CancellationToken cancel_token, string base_Address = "https://www.cryptopia.co.nz/")
		{
			m_cancel_token = cancel_token;
			UrlBaseAddress = base_Address;
			m_client = HttpClientFactory.Create(new AuthDelegatingHandler(key, secret));
		}
		public void Dispose()
		{
			Debug.Assert(m_cancel_token.IsCancellationRequested, "Cancel should have been signalled before here");
			if (m_client != null)
			{
				m_client.Dispose();
				m_client = null;
			}
		}

		#region API Calls

		public Task<BalanceResponse> GetBalances(BalanceRequest request)
		{
			return PostData<BalanceResponse, BalanceRequest>(PrivateApiCall.GetBalance, request);
		}

		public Task<CancelTradeResponse> CancelTrade(CancelTradeRequest request)
		{
			return PostData<CancelTradeResponse, CancelTradeRequest>(PrivateApiCall.CancelTrade, request);
		}

		public Task<SubmitTradeResponse> SubmitTrade(SubmitTradeRequest request)
		{
			return PostData<SubmitTradeResponse, SubmitTradeRequest>(PrivateApiCall.SubmitTrade, request);
		}

		public Task<OpenOrdersResponse> GetOpenOrders(OpenOrdersRequest request)
		{
			return PostData<OpenOrdersResponse, OpenOrdersRequest>(PrivateApiCall.GetOpenOrders, request);
		}

		public Task<TradeHistoryResponse> GetTradeHistory(TradeHistoryRequest request)
		{
			return PostData<TradeHistoryResponse, TradeHistoryRequest>(PrivateApiCall.GetTradeHistory, request);
		}

		public Task<TransactionResponse> GetTransactions(TransactionRequest request)
		{
			return PostData<TransactionResponse, TransactionRequest>(PrivateApiCall.GetTransactions, request);
		}

		public Task<DepositAddressResponse> GetDepositAddress(DepositAddressRequest request)
		{
			return PostData<DepositAddressResponse, DepositAddressRequest>(PrivateApiCall.GetDepositAddress, request);
		}

		public Task<SubmitTipResponse> SubmitTip(SubmitTipRequest request)
		{
			return PostData<SubmitTipResponse, SubmitTipRequest>(PrivateApiCall.SubmitTip, request);
		}

		public Task<SubmitWithdrawResponse> SubmitWithdraw(SubmitWithdrawRequest request)
		{
			return PostData<SubmitWithdrawResponse, SubmitWithdrawRequest>(PrivateApiCall.SubmitWithdraw, request);
		}

		/// <summary>POST request</summary>
		public Task<T> PostData<T, U>(PrivateApiCall call, U requestData) where T : IResponse where U : IRequest
		{
			Debug.Assert(!m_cancel_token.IsCancellationRequested, "Shouldn't be making new requests when shutdown is signalled");
			return Task.Run(() =>
			{
				// Serialize all private API calls
				lock (m_post_lock)
				{
					m_cancel_token.ThrowIfCancellationRequested();

					var url = $"{UrlBaseAddress}Api/{call}";

					// Submit the request
					var response = m_client.PostAsJsonAsync(url, requestData, m_cancel_token).Result;
					if (!response.IsSuccessStatusCode)
						throw new HttpResponseException(response);

					// Interpret the reply
					return response.Content.ReadAsAsync<T>(m_cancel_token).Result;
				}
			}, m_cancel_token);
		}
		private object m_post_lock = new object();

		#endregion
	}

	/// <summary>Http response exception</summary>
	public class HttpResponseException :Exception
	{
		public HttpResponseException(HttpResponseMessage response)
			:base(response.ReasonPhrase)
		{
			StatusCode = response.StatusCode;
		}

		/// <summary>Status code returned in the HTTP response</summary>
		public HttpStatusCode StatusCode { get; private set; }
	}

	#endif
}
