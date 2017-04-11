using cAlgo.API;

namespace Rylobot
{
	public interface ITrade
	{
		/// <summary>Direction of trade</summary>
		TradeType TradeType { get; }

		/// <summary>The CAlgo Id for this trade</summary>
		int Id { get; }

		/// <summary>The string identifier for the symbol</summary>
		string SymbolCode { get; }

		/// <summary>The entry price</summary>
		QuoteCurrency EP { get; }

		/// <summary>The stop loss price (absolute, in quote currency)</summary>
		QuoteCurrency? SL { get; }

		/// <summary>The take profit price (absolute, in quote currency)</summary>
		QuoteCurrency? TP { get; }

		/// <summary>The size of the trade</summary>
		long Volume { get; }

		/// <summary>A string label for the trade</summary>
		string Label { get; }

		/// <summary>A string comment for the trade</summary>
		string Comment { get; }
	}
}
