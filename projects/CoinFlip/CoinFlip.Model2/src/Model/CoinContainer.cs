using System.Collections.ObjectModel;
using Rylogic.Extn;

namespace CoinFlip
{
	public class CoinContainer : ObservableCollection<Coin>
	{
		public CoinContainer()
		{ }

		/// <summary>Access a coin by symbol</summary>
		public Coin this[string sym]
		{
			get
			{
				var idx = this.IndexOf(x => x.Symbol == sym);
				return idx >= 0 ? base[idx] : null;
			}
		}
	}
}
