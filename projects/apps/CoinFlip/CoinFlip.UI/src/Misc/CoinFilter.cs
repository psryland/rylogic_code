using System;
using System.ComponentModel;

namespace CoinFlip.UI
{
	public class CoinFilter :INotifyPropertyChanged
	{
		public CoinFilter(Func<object, IValueTotalAvail> coin_data_adapter)
		{
			CoinDataAdapter = coin_data_adapter;
		}

		/// <summary>Function for converting whatever items are in 'Coins' to 'CoinData'</summary>
		private Func<object, IValueTotalAvail> CoinDataAdapter { get; }

		/// <summary>True if coins should be filtered</summary>
		public bool Enabled
		{
			get;
			set => SetProp(ref field, value, nameof(Enabled));
		}

		/// <summary>Filter balances with a value or amount less than this threshold</summary>
		public decimal Threshold
		{
			get;
			set => SetProp(ref field, value, nameof(Threshold));
		}

		/// <summary>What to filter on</summary>
		public ECoinFilterType Type
		{
			get;
			set => SetProp(ref field, value, nameof(Type));
		}

		/// <summary>Filter for the 'Coins' collection view</summary>
		public bool Predicate(object obj)
		{
			if (!Enabled)
				return true;

			var coin = CoinDataAdapter(obj);
			if (coin == null)
				return false;

			// Use '>' rather than '>=' so that filtering against '0' removes all zero amounts.
			switch (Type)
			{
				case ECoinFilterType.Value: return coin.Value > Threshold;
				case ECoinFilterType.Total: return coin.Total > Threshold;
				case ECoinFilterType.Available: return coin.Available > Threshold;
				case ECoinFilterType.Balance: return (coin.Value * coin.Total) > Threshold;
				default: throw new Exception("Unknown Filter Type");
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void SetProp<T>(ref T prop, T value, string prop_name)
		{
			if (Equals(prop, value)) return;
			prop = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
