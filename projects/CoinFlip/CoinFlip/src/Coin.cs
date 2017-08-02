using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using pr.extn;

namespace CoinFlip
{
	/// <summary>A Coin, owned by an exchange</summary>
	[DebuggerDisplay("{Symbol} ({Exchange})")]
	public class Coin
	{
		public Coin(string sym, Exchange exch)
		{
			Pairs = new HashSet<TradePair>();
			Meta = exch.Model.Coins[sym];
			Exchange = exch;
		}

		/// <summary>Meta data for the coin</summary>
		public CoinData Meta { get; private set; }

		/// <summary>Coin name</summary>
		public string Symbol
		{
			get { return Meta.Symbol; }
		}

		/// <summary>Return the Coin with the exchange</summary>
		public string SymbolWithExchange
		{
			get { return "{0} - {1}".Fmt(Symbol, Exchange.Name); }
		}

		/// <summary>The Exchange trading this coin</summary>
		public Exchange Exchange { get; private set; }

		/// <summary>Return the balance for this coin on its associated exchange</summary>
		public Balance Balance
		{
			get { return Exchange.Balance[this]; }
		}

		/// <summary>Trade pairs involving this coin</summary>
		public HashSet<TradePair> Pairs
		{
			get;
			private set;
		}

		/// <summary>The value of this currency in units set by the user</summary>
		public decimal NormalisedValue
		{
			get { return Meta.Value; }
			set { Meta.Value = value; }
		}

		/// <summary></summary>
		public override string ToString()
		{
			return Symbol;
		}

		/// <summary>Allow implicit conversion to string symbol name</summary>
		[DebuggerStepThrough] public static implicit operator string(Coin coin)
		{
			return coin?.Symbol;
		}

		#region Equals
		[DebuggerStepThrough] public static bool operator == (Coin lhs, Coin rhs)
		{
			if ((object)lhs == null && (object)rhs == null) return true;
			if ((object)lhs == null || (object)rhs == null) return false;
			return lhs.Equals(rhs);
		}
		[DebuggerStepThrough] public static bool operator != (Coin lhs, Coin rhs)
		{
			return !(lhs == rhs);
		}
		public bool Equals(Coin rhs)
		{
			return
				rhs != null &&
				Symbol == rhs.Symbol &&
				Exchange == rhs.Exchange;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Coin);
		}
		public override int GetHashCode()
		{
			return new { Symbol, Exchange }.GetHashCode();
		}
		#endregion
	}

	/// <summary>Meta data for a coin</summary>
	[DebuggerDisplay("{Symbol} {Value} {OfInterest}")]
	public class CoinData :INotifyPropertyChanged
	{
		public CoinData(string symbol, decimal value)
		{
			Symbol = symbol;
			Value = value;
			OfInterest = false;
		}
		public CoinData(XElement node)
		{
			Symbol = node.Element(nameof(Symbol)).As(Symbol);
			Value  = node.Element(nameof(Value)).As(Value);
			OfInterest = node.Element(nameof(OfInterest)).As(OfInterest);
		}
		public XElement ToXml(XElement node)
		{
			node.Add2(nameof(Symbol), Symbol, false);
			node.Add2(nameof(Value), Value, false);
			node.Add2(nameof(OfInterest), OfInterest, false);
			return node;
		}

		/// <summary>The symbol name for the coin</summary>
		public string Symbol { get; private set; }

		/// <summary>Value assigned to this coin</summary>
		public decimal Value
		{
			get { return m_value; }
			set { SetProp(ref m_value, value, nameof(Value)); }
		}
		private decimal m_value;

		/// <summary>True if coins of this type should be included in loops</summary>
		public bool OfInterest
		{
			get { return m_of_interest; }
			set { SetProp(ref m_of_interest, value, nameof(OfInterest)); }
		}
		private bool m_of_interest;

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void SetProp<T>(ref T prop, T value, string name)
		{
			if (Equals(prop, value)) return;
			prop = value;
			PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
		}
	}
}
