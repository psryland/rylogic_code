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
		Unit<double> HeldOnExch { get; set; }

		/// <summary>Reserve 'amount' until the next balance update</summary>
		Guid Hold(Unit<double> amount);

		/// <summary>Reserve 'amount' until 'still_needed' returns false.</summary>
		Guid Hold(Unit<double> amount, Func<IBalance, bool> still_needed);

		/// <summary>Update the 'still_needed' function for a balance hold</summary>
		void Hold(Guid hold_id, Func<IBalance, bool> still_needed);

		/// <summary>Release a hold on funds</summary>
		void Release(Guid hold_id);

		/// <summary>Return the amount held for the given id</summary>
		Unit<double> Reserved(Guid hold_id);
	}
}
