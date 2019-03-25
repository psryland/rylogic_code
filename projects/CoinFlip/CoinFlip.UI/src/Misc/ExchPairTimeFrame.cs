using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class ExchPairTimeFrame : IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//   - A binding helper class for managing the available exchanges,
		//     pairs on those exchanges, and time frames available for those pairs

		public ExchPairTimeFrame(Model model)
		{
			Model = model;
		}
		public void Dispose()
		{
			Model = null;
		}

		/// <summary></summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					Exchanges = null;
				}
				m_model = value;
				if (m_model != null)
				{
					Exchanges = new ListCollectionView(m_model.Exchanges);
				}
			}
		}
		private Model m_model;

		/// <summary></summary>
		public string Address => $"{Exchange?.Name}-{Pair?.Name}-{TimeFrame}";

		/// <summary>The currently selected Exchange</summary>
		public Exchange Exchange
		{
			get => (Exchange)Exchanges?.CurrentItem;
			set => Exchanges.MoveCurrentTo(value);
		}

		/// <summary>The currently selected trade pair</summary>
		public TradePair Pair
		{
			get => (TradePair)Pairs?.CurrentItem;
			set => Pairs.MoveCurrentTo(value);
		}

		/// <summary>The currently selected time frame</summary>
		public ETimeFrame TimeFrame
		{
			get => TimeFrames?.CurrentItem is ETimeFrame tf ? tf : ETimeFrame.None;
			set => TimeFrames.MoveCurrentTo(value);
		}

		/// <summary>The available exchanges</summary>
		public ICollectionView Exchanges
		{
			get { return m_exchanges; }
			private set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.CurrentChanged -= HandleCurrentChanged;
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.CurrentChanged += HandleCurrentChanged;
					m_exchanges.Filter = exch => !(exch is CrossExchange);
					m_exchanges.MoveCurrentToFirst();
				}

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exchanges)));

				// Handlers
				void HandleCurrentChanged(object sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exchange)));

					// Try to preserve the current pair and time frame
					var pair = Pair?.Name;

					// Set the new list of pairs for the new exchange
					var exch = (Exchange)m_exchanges.CurrentItem;
					var pairs = new ObservableCollection<TradePair>(exch?.Pairs ?? (IList<TradePair>)new TradePair[0]);
					Pairs = new ListCollectionView(pairs);

					// Try to select the same pair
					var same = pair != null ? pairs.FirstOrDefault(x => x.Name == pair) : null;
					if (same != null) Pairs.MoveCurrentTo(same);
				}
			}
		}
		private ICollectionView m_exchanges;

		/// <summary>The pairs available on the current exchange</summary>
		public ICollectionView Pairs
		{
			get { return m_pairs; }
			private set
			{
				if (m_pairs == value) return;
				if (m_pairs != null)
				{
					m_pairs.CurrentChanged -= HandleCurrentChanged;
				}
				m_pairs = value;
				if (m_pairs != null)
				{
					m_pairs.CurrentChanged += HandleCurrentChanged;
					m_pairs.SortDescriptions.Add(new SortDescription(nameof(TradePair.Base), ListSortDirection.Ascending));
					m_pairs.SortDescriptions.Add(new SortDescription(nameof(TradePair.Quote), ListSortDirection.Ascending));
					m_pairs.MoveCurrentToFirst();
				}

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pairs)));

				// Handlers
				void HandleCurrentChanged(object sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pair)));

					// Try to preserve the time frame
					var tf = TimeFrame;

					var pair = (TradePair)m_pairs.CurrentItem;
					var tfs = new ObservableCollection<ETimeFrame>(pair?.CandleDataAvailable ?? new ETimeFrame[0]);
					TimeFrames = new ListCollectionView(tfs);

					// Try to select the same timeframe
					var same = tf != ETimeFrame.None ? tfs.FirstOrDefault(x => x == tf) : ETimeFrame.None;
					if (same != ETimeFrame.None) TimeFrames.MoveCurrentTo(same);
}
			}
		}
		private ICollectionView m_pairs;

		/// <summary>The available time frames for the selected pair on the selected exchange</summary>
		public ICollectionView TimeFrames
		{
			get { return m_time_frames; }
			private set
			{
				if (m_time_frames == value) return;
				if (m_time_frames != null)
				{
					m_time_frames.CurrentChanged -= HandleCurrentChanged;
				}
				m_time_frames = value;
				if (m_time_frames != null)
				{
					m_time_frames.CurrentChanged += HandleCurrentChanged;
					m_time_frames.MoveCurrentToFirst();
				}

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeFrames)));

				void HandleCurrentChanged(object sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeFrame)));
				}
			}
		}
		private ICollectionView m_time_frames;

		/// <summary>Get the instrument for the selected options</summary>
		public Instrument GetInstrument(string name)
		{
			if (Pair == null || TimeFrame == ETimeFrame.None) return null;
			return new Instrument(name, Model.PriceData[Pair, TimeFrame]);
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
