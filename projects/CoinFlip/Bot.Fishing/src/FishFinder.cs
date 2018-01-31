using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using CoinFlip;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;

namespace Bot.Fishing
{
	[Plugin(typeof(IBot), Unique = true)]
	public class FishFinder :IBot
	{
		/// <summary>A grid for displaying fisher instances</summary>
		private GridFishing m_grid_fishing;

		public FishFinder(Model model, XElement settings_xml)
			:base("Fish Finder", model, new SettingsData(settings_xml))
		{
			// Create the collection of fishers from the settings
			Fishers = new BindingSource<Fisher>{ DataSource = new BindingListEx<Fisher>(), PerItem = true };
			foreach (var fisher in Settings.Fishers)
				Fishers.Add(new Fisher(this, fisher));

			// Create a grid to display the fisher instances in
			m_grid_fishing = new GridFishing(this, "Fishing", nameof(m_grid_fishing)){ DataSource = Fishers };
			Model.AddToUI(m_grid_fishing, new[] { EDockSite.Right, EDockSite.Bottom });
		}
		protected override void Dispose(bool disposing)
		{
			Fishers = null;
			Util.Dispose(ref m_grid_fishing);
			base.Dispose(disposing);
		}

		/// <summary>Settings for this strategy</summary>
		public new SettingsData Settings
		{
			get { return (SettingsData)base.Settings; }
		}

		/// <summary>Enable/Disable this bot.</summary>
		protected override bool GetActiveInternal()
		{
			// This bot doesn't use the single main loop method
			// Each fisher instance has it's own main loop.
			return Fishers.Any(x => x.Active);
		}
		protected override void SetActiveInternal(bool enabled)
		{
			if (enabled)
			{
				// Start all fishing instances
				foreach (var fisher in Fishers)
					fisher.Active = true;
			}
			else
			{
				// Shutdown fishing instances
				foreach (var fisher in Fishers)
					fisher.Active = false;
			}
		}

		/// <summary>Fisher instances</summary>
		public BindingSource<Fisher> Fishers
		{
			get { return m_fishing; }
			private set
			{
				if (m_fishing == value) return;
				if (m_fishing != null)
				{
					if (m_grid_fishing != null) m_grid_fishing.DataSource = null;
					m_fishing.ListChanging -= HandleFishingListChanging;
					Util.DisposeAll(m_fishing);
				}
				m_fishing = value;
				if (m_fishing != null)
				{
					m_fishing.ListChanging += HandleFishingListChanging;
					if (m_grid_fishing != null) m_grid_fishing.DataSource = m_fishing;
				}

				// Handlers
				void HandleFishingListChanging(object sender, ListChgEventArgs<Fisher> e)
				{
					switch (e.ChangeType)
					{
					case ListChg.ItemAdded:
						{
							e.Item.PropertyChanged += HandleFisherPropertyChanged;
							break;
						}
					case ListChg.ItemPreRemove:
						{
							e.Item.PropertyChanged -= HandleFisherPropertyChanged;
							break;
						}
					}
					if (e.IsDataChanged)
						Settings.Fishers = Fishers.Select(x => x.Settings).ToArray();
				}
				void HandleFisherPropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					if (e.PropertyName == nameof(Fisher.Active))
						RaisePropertyChanged(nameof(FishFinder.Active));
				}
			}
		}
		private BindingSource<Fisher> m_fishing;

		/// <summary></summary>
		public class SettingsData :SettingsBase<SettingsData>
		{
			public SettingsData()
			{
				Fishers = new FishingData[0];
			}
			public SettingsData(SettingsData rhs)
				:base(rhs)
			{
				Fishers = rhs.Fishers.ToArray();
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			/// <summary>The fishing instances</summary>
			public FishingData[] Fishers
			{
				get { return get<FishingData[]>(nameof(Fishers)); }
				set { set(nameof(Fishers), value); }
			}
		}

		/// <summary>Data needed to save a fishing instance in the settings</summary>
		[DebuggerDisplay("{Pair} {Exch0} {Exch1}")]
		[TypeConverter(typeof(TyConv))]
		public class FishingData :SettingsXml<FishingData>
		{
			public FishingData()
			{
				Pair         = string.Empty;
				Exch0        = string.Empty;
				Exch1        = string.Empty;
				PriceOffset  = 0m;
				Direction    = ETradeDirection.None;
			}
			public FishingData(string pair, string exch0, string exch1, decimal price_offset, ETradeDirection direction)
			{
				Pair         = pair;
				Exch0        = exch0;
				Exch1        = exch1;
				PriceOffset  = price_offset;
				Direction    = direction;
			}
			public FishingData(FishingData rhs)
			{
				Pair         = rhs.Pair;
				Exch0        = rhs.Exch0;
				Exch1        = rhs.Exch1;
				PriceOffset  = rhs.PriceOffset;
				Direction    = rhs.Direction;
			}
			public FishingData(XElement node)
				:base(node)
			{}

			/// <summary>The name of the pair to trade</summary>
			public string Pair
			{
				get { return get<string>(nameof(Pair)); }
				set { set(nameof(Pair), value); }
			}

			/// <summary>The name of the reference exchange</summary>
			public string Exch0
			{
				get { return get<string>(nameof(Exch0)); }
				set { set(nameof(Exch0), value); }
			}

			/// <summary>The name of the target exchange</summary>
			public string Exch1
			{
				get { return get<string>(nameof(Exch1)); }
				set { set(nameof(Exch1), value); }
			}

			/// <summary>The price offset range (as a fraction of the reference price)</summary>
			public decimal PriceOffset
			{
				get { return get<decimal>(nameof(PriceOffset)); }
				set { set(nameof(PriceOffset), value); }
			}

			/// <summary>The directions to fish in</summary>
			public ETradeDirection Direction
			{
				get { return get<ETradeDirection>(nameof(Direction)); }
				set { set(nameof(Direction), value); }
			}

			/// <summary>An identifying name for this fishing instance</summary>
			public string Name
			{
				get { return $"{Pair.Replace("/",string.Empty)}-{Exch0}-{Exch1}"; }
			}

			/// <summary>True if this object contains valid data</summary>
			public bool Valid
			{
				get
				{
					return 
						Exch0 != Exch1 &&
						Pair.HasValue() &&
						PriceOffset > 0 &&
						Direction != ETradeDirection.None;
				}
			}

			/// <summary>Return the string description of why 'Valid' is false</summary>
			public string ReasonInvalid
			{
				get
				{
					return
						(Exch0 == Exch1                    ? "Exchanges are the same\r\n" : string.Empty)+
						(!Pair.HasValue()                  ? "No trading pair\r\n"        : string.Empty)+
						(PriceOffset <= 0                  ? "Price offset invalid\r\n"   : string.Empty)+
						(Direction == ETradeDirection.None ? "No trading direction\r\n"   : string.Empty);
				}
			}

			private class TyConv :GenericTypeConverter<FishingData> {}
		}
	}
}
#if false
  <setting key="Fishing" ty="CoinFlip.Settings+FishingData[]">
    <_>
      <Direction>Both</Direction>
      <Exch0>Bittrex</Exch0>
      <Exch1>Cryptopia</Exch1>
      <Pair>ETH/BTC</Pair>
      <PriceOffset>0.0075</PriceOffset>
    </_>
    <_>
      <Direction>Both</Direction>
      <Exch0>Poloniex</Exch0>
      <Exch1>Cryptopia</Exch1>
      <Pair>ETH/BTC</Pair>
      <PriceOffset>0.005</PriceOffset>
    </_>
    <_>
      <Direction>Both</Direction>
      <Exch0>Bittrex</Exch0>
      <Exch1>Poloniex</Exch1>
      <Pair>ETH/BTC</Pair>
      <PriceOffset>0.005</PriceOffset>
    </_>
    <_>
      <Direction>Both</Direction>
      <Exch0>Cryptopia</Exch0>
      <Exch1>Bittrex</Exch1>
      <Pair>ETH/BTC</Pair>
      <PriceOffset>0.005</PriceOffset>
    </_>
    <_>
      <Direction>Both</Direction>
      <Exch0>Poloniex</Exch0>
      <Exch1>Cryptopia</Exch1>
      <Pair>ETC/BTC</Pair>
      <PriceOffset>0.005</PriceOffset>
    </_>
  </setting>


#endif