using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using CoinFlip;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace Bot.Portfolio
{
	[Plugin(typeof(IBot))]
    public class Portfolio :IBot
    {
		// Notes:
		//  - Choose a weighting for each currency of interest.
		//  - At each rebalance period (or when triggered), buy/sell currencies to make holding match the desired weightings.
		// Theory:
		//  - When a currency is above its assumed value we sell it (selling high)
		//  - When a currency is below its assumed value we buy it (buying low)
		//  Examples:
		//    Balance:  1000USD
		//  Portfolio:  BTC 60%, ETC 30%, LTC 10%.
		//     Values:  5000USD  300USD   50USD
		//   Holdings:  
		//       BTC = 1000*0.6/5000 = 0.12
		//       ETC = 1000*0.3/300 = 1
		//       LTC = 1000*0.1/50 = 2
		//             --- BTC tanks ---
		//     Values:  2000USD  300USD   50USD
		//    Balance:  240USD + 300USD + 100USD = 640USD (holdings * new values)
		//             --- Update holdings ---
		//   Holdings:
		//       BTC = 640*0.6/2000 = 0.192
		//       ETC = 640*0.3/300 = 0.64
		//       LTC = 640*0.1/50 = 1.28
		//             --- BTC recovers ---
		//     Values:  5000USD  300USD   50USD
		//    Balance:  960USD + 192USD + 64USD = 1216USD (holdings * new values)
		//   Holdings:  
		//       BTC = 1216*0.6/5000 = 0.146
		//       ETC = 1216*0.3/300 = 1.216
		//       LTC = 1216*0.1/50 = 2.432
		//             --- BTC spikes ---
		//     Values:  8000USD  300USD   50USD
		//    Balance:  1168USD + 365USD + 122USD = 1655USD (holdings * new values)
		//   Holdings:  
		//       BTC = 1655*0.6/8000 = 0.124
		//       ETC = 1655*0.3/300 = 1.655
		//       LTC = 1655*0.1/50 = 3.31
		//             --- BTC normalises ---
		//     Values:  5000USD  300USD   50USD
		//    Balance:  620USD + 497USD + 165USD = 1282USD (holdings * new values)
		//
		//   See excel worksheet

		public Portfolio(Model model, XElement settings_xml)
			:base("Portfolio", model, new SettingsData(settings_xml))
		{
			Folios = new BindingListEx<Folio>();
			UI = new PortfolioUI(this);
			Exchange = Model.GetExchange(Settings.Exchange);
		}
		protected override void Dispose(bool disposing)
		{
			UI = null;
			base.Dispose(disposing);
		}
		protected override void HandleSettingChanged(object sender, SettingChangedEventArgs args)
		{
			base.HandleSettingChanged(sender, args);
			switch (args.Key) {
			case nameof(SettingsData.Exchange):
				{
					Exchange = Model.GetExchange(Settings.Exchange);
					return;
				}
			case nameof(SettingsData.Portfolio):
				{
					// Synchronise the 'Folios' collection with the settings
					Folios.Sync(Settings.Portfolio.ToHashSet(x => new Folio(Exchange, x)));
					return;
				}
			}
		}

		/// <summary>Settings for this strategy</summary>
		public new SettingsData Settings
		{
			get { return (SettingsData)base.Settings; }
		}

		/// <summary>The UI for monitoring this bot</summary>
		public PortfolioUI UI
		{
			get { return m_ui; }
			private set
			{
				if (m_ui == value) return;
				Util.Dispose(ref m_ui);
				m_ui = value;
			}
		}
		private PortfolioUI m_ui;

		/// <summary>The exchange to trade on</summary>
		public Exchange Exchange
		{
			get { return m_exch; }
			private set
			{
				if (m_exch == value) return;
				if (m_exch != null)
				{
					Folios.Clear();
				}
				m_exch = value;
				if (m_exch != null)
				{
					// Repopulate the Folios collection
					Folios.AddRange(Settings.Portfolio.Select(x => new Folio(m_exch, x)));
				}
			}
		}
		private Exchange m_exch;

		/// <summary>The folios in this portfolio</summary>
		public BindingListEx<Folio> Folios { get; private set; }

		/// <summary>Add a currency to the portfolio</summary>
		public void AddCurrency(string sym = null)
		{
			// Prompt for the currency symbol
			if (sym == null)
			{
				using (var prompt = new PromptUI { Title = "Add Currency", PromptText = string.Empty })
				{
					if (prompt.ShowDialog(Model.UI) != DialogResult.OK) return;
					sym = ((string)prompt.Value).ToUpperInvariant();
				}
			}

			// Check the currency is not already in the portfolio
			if (Settings.Portfolio.Any(x => x.Currency == sym))
				return;

			// Add the currency to the port folio
			Settings.Portfolio = Settings.Portfolio.Concat(new SettingsData.Folio(sym, 0.0)).ToArray();
		}

		/// <summary>Remove a currency from the portfolio</summary>
		public void RemoveCurrency(string sym)
		{
			// Remove 'sym'
			Settings.Portfolio = Settings.Portfolio.Where(x => x.Currency != sym).ToArray();
		}

		/// <summary>Start the bot</summary>
		public override bool OnStart()
		{
			return base.OnStart();
		}

		/// <summary>Main loop step</summary>
		public override void Step()
		{
			base.Step();
		}

		/// <summary>Return items to add to the context menu for this bot</summary>
		public override void CMenuItems(ContextMenuStrip cmenu)
		{
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Monitor"));
				opt.Click += (s,a) =>
				{
					Model.AddToUI(UI);
					UI.DockControl.IsActiveContent = true;
				};
			}
		}

		/// <summary>Determine the amount of each portfolio currency we should have, and trade to rebalance</summary>
		private void RebalancePortfolio()
		{
			// Get the current balances of each of the folio currencies
			// and calculate the total value using the live prices.
			var total_value = Folios.Sum(x => x.Coin.ValueOf(x.Coin.Balance.Total * Settings.FundAllocation));

			// Determine what the new holdings should be given the current total value.
			var weight_total = Folios.Sum(x => x.Weight);
			var targets = Folios.Select(x => total_value * (decimal)(x.Weight / weight_total)).ToArray();

			// Get all the pairs on 'Exchange' where both currencies are in the portfolio

			// Iteratively find amounts to trade on each pair so that the holdings are the desired amount
			// Optimise for smaller trades

			// Place the trades, and monitor them
		}

		///<summary>One entry in the portfolio</summary>
		public class Folio
		{
			private readonly SettingsData.Folio m_folio;
			public Folio(Exchange exch, SettingsData.Folio folio)
			{
				Exchange = exch;
				m_folio = folio;
			}

			/// <summary>The currency that is part of the portfolio</summary>
			public string Currency
			{
				get { return m_folio.Currency; }
				set { m_folio.Currency = value; }
			}

			/// <summary>The weighting value of this currency</summary>
			public double Weight
			{
				get { return m_folio.Weight; }
				set { m_folio.Weight = value; }
			}

			/// <summary>The exchange that provides trading for the currency</summary>
			public Exchange Exchange { get; private set; }

			/// <summary>The coin on 'Exchange' associated with 'Currency'</summary>
			public Coin Coin
			{
				get { return Exchange.Coins[Currency]; }
			}

			#region Equals
			public bool Equals(Folio rhs)
			{
				return rhs != null && Exchange == rhs.Exchange && Currency == rhs.Currency;
			}
			public override bool Equals(object obj)
			{
				return Equals(obj as Folio);
			}
			public override int GetHashCode()
			{
				return new { Exchange, Currency }.GetHashCode();
			}
			#endregion
		}

		/// <summary>Data needed to save a fishing instance in the settings</summary>
		[TypeConverter(typeof(TyConv))]
		public class SettingsData :SettingsBase<SettingsData>
		{
			public SettingsData()
			{
				Exchange  = string.Empty;
				Portfolio = new Folio[0];
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			/// <summary>The name of the exchange to trade on</summary>
			public string Exchange
			{
				get { return get<string>(nameof(Exchange)); }
				set { set(nameof(Exchange), value); }
			}

			/// <summary>The currencies and their weightings</summary>
			public Folio[] Portfolio
			{
				get { return get<Folio[]>(nameof(Portfolio)); }
				set { set(nameof(Portfolio), value); }
			}

			/// <summary></summary>
			public override bool Valid(IBot bot)
			{
				var pf = (Portfolio)bot;

				// Valid if the exchange exists, and has pairs for trading each currency
				var exch = pf.Model.GetExchange(Exchange);
				if (exch == null)
					return false;

				// The currencies in the portfolio
				var currencies = Portfolio.Select(x => x.Currency).ToList();
				if (currencies.Count < 2)
					return false;
				
				// Must be at least one non-zero weight
				if (Portfolio.Sum(x => x.Weight) == 0.0)
					return false;

				// Make sure we can trade each currency with at least one other in the portfolio
				foreach (var folio in Portfolio)
				{
					// If there aren't any pairs with other currencies in the portfolio, then settings are invalid.
					if (!currencies.Except(folio.Currency).Any(x => exch.Pairs[folio.Currency, x] != null))
						return false;
				}

				return true;
			}

			/// <summary>A currency and weighting</summary>
			public class Folio
			{
				public Folio()
					:this(string.Empty, 0.0)
				{}
				public Folio(string sym, double weight)
				{
					Currency = sym;
					Weight = weight;
				}
				public Folio(XElement node)
				{
					Currency = node.Element(nameof(Currency)).As(Currency);
					Weight   = node.Element(nameof(Weight)).As(Weight);
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(nameof(Currency), Currency, false);
					node.Add2(nameof(Weight), Weight, false);
					return node;
				}

				/// <summary>The currency that is part of the portfolio</summary>
				public string Currency { get; set; }

				/// <summary>The weighting value of this currency</summary>
				public double Weight { get; set; }

				#region Equals
				public bool Equals(Folio rhs)
				{
					return rhs != null && Currency == rhs.Currency;
				}
				public override bool Equals(object obj)
				{
					return Equals(obj as Folio);
				}
				public override int GetHashCode()
				{
					return new { Currency }.GetHashCode();
				}
				#endregion
			}

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
	}
}
