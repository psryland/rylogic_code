using System.Diagnostics;

namespace CoinFlip
{
	/// <summary>A fund is a partition of the user's balances on all exchanges</summary>
	[DebuggerDisplay("{Id}")]
	public class Fund
	{
		// Notes:
		// - Balances are a 3-D array; Exchange per Coins per Fund.
		//   Each exchange has a sub-set of the possible coins.
		//   Each coin on each exchange is partitioned into funds (Main, plus others)
		//   Each Fund Id exists across all exchanges
		// - A fund is a virtual sub-account.
		// - Fund provides an interface to the balances of a single fund.
		// - A fund only manages the creation/destruction of balance contexts,
		//   not the amounts in each balance context.
		// - 'Fund' is used so that exchanges can create the balance contexts.
		// - This is basically a wrapper around a string

		/// <summary>The default fund</summary>
		public static Fund Default => new Fund(Main);
		public const string Main = "Main";
		
		public Fund(string id)
		{
			Id = id;
		}

		/// <summary>The unique Id for this fund</summary>
		public string Id { get; }

		/// <summary>Return the balance of 'coin' associated with this fund. (Coin implies the exchange)</summary>
		public IBalance this[Coin coin] => coin.Balances[this];

		#region Equals
		public static bool operator ==(Fund lhs, Fund rhs)
		{
			return lhs?.Id == rhs?.Id;
		}
		public static bool operator !=(Fund lhs, Fund rhs)
		{
			return lhs?.Id != rhs?.Id;
		}
		public bool Equals(Fund rhs)
		{
			return this == rhs;;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Fund);
		}
		public override int GetHashCode()
		{
			return new { Id }.GetHashCode();
		}
		public override string ToString()
		{
			return Id;
		}
		#endregion
	}
}
