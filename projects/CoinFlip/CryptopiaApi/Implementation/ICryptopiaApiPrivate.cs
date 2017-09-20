using Cryptopia.API.DataObjects;
using System;
using System.Threading.Tasks;

namespace Cryptopia.API.Implementation
{
	public interface ICryptopiaApiPrivate : IDisposable
	{
		Task<T> PostData<T, U>(PrivateApiCall call, U requestData)
			where T : IResponse
			where U : IRequest;
		Task<DataObjects.CancelTradeResponse> CancelTrade(CancelTradeRequest request);
		Task<DataObjects.BalanceResponse> GetBalances(BalanceRequest request);
		Task<DataObjects.DepositAddressResponse> GetDepositAddress(DepositAddressRequest request);
		Task<DataObjects.OpenOrdersResponse> GetOpenOrders(OpenOrdersRequest request);
		Task<DataObjects.TradeHistoryResponse> GetTradeHistory(TradeHistoryRequest request);
		Task<DataObjects.TransactionResponse> GetTransactions(TransactionRequest request);
		Task<DataObjects.SubmitTipResponse> SubmitTip(SubmitTipRequest request);
		Task<DataObjects.SubmitTradeResponse> SubmitTrade(SubmitTradeRequest request);
		Task<DataObjects.SubmitWithdrawResponse> SubmitWithdraw(SubmitWithdrawRequest request);
	}
}
