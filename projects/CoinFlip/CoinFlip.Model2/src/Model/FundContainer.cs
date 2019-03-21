using System.Collections.ObjectModel;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Extn;

namespace CoinFlip
{
	public class FundContainer : ObservableCollection<Fund>
	{
		public FundContainer()
		{
			this[Fund.Main] = new Fund(Fund.Main);
			foreach (var fund in SettingsData.Settings.Funds.Where(x => x.Id != Fund.Main))
				this[fund.Id] = new Fund(fund.Id);
		}

		/// <summary>Return the fund associated with 'fund_id' (or null)</summary>
		public Fund this[string fund_id]
		{
			get
			{
				var idx = this.IndexOf(x => x.Id == fund_id);
				return idx >= 0 ? this[idx] : null;
			}
			set
			{
				var idx = this.IndexOf(x => x.Id == fund_id);
				if (idx >= 0)
					this[idx] = value;
				else
					Add(value);
			}
		}
	}
}
