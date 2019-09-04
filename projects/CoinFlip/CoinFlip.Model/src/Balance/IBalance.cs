using System;
using System.ComponentModel;
using Rylogic.Utility;

namespace CoinFlip
{
	public interface IBalance :INotifyPropertyChanged
	{
		// Notes:
		//  - This interface is used to isolate the rest of the code from the
		//    Fund/Balance system (which is a bit confusing).

		/// <summary>The fund that this balance belongs to</summary>
		Fund Fund { get; }

		/// <summary>The currency that the balance is in</summary>
		Coin Coin { get; }

		/// <summary>The time when this balance was last updated</summary>
		DateTimeOffset LastUpdated { get; }

		/// <summary>The total amount of the currency 'Coin' associated with this fund on 'Coin.Exchange'</summary>
		Unit<double> Total { get; }

		/// <summary>Total amount able to be used for new trades</summary>
		Unit<double> Available { get; }

		/// <summary>Total amount set aside for pending orders and trade strategies</summary>
		Unit<double> Held { get; }

		/// <summary>Holds on this fund balance</summary>
		FundHoldContainer Holds { get; }

		/// <summary>Notify property changed for 'Total', 'Available', and 'Held'</summary>
		void Invalidate();
	}
}
