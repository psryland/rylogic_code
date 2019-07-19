using System.Collections.ObjectModel;
using Rylogic.Extn;

namespace CoinFlip
{
	public class ExchangeContainer : ObservableCollection<Exchange>
	{
		public ExchangeContainer()
		{}

		/// <summary>Access an exchange by name</summary>
		public Exchange this[string name]
		{
			get
			{
				var idx = this.IndexOf(x => x.Name == name);
				return idx >= 0 ? base[idx] : null;
			}
		}
	}
}
