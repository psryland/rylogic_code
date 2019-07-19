using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Rylogic.Common;
using Rylogic.Extn;

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

		/// <summary>The available exchanges</summary>
		public ICollectionView Exchanges
		{
			get { return m_exchanges; }
			private set
			{
				if (m_exchanges == value) return;

				// Preserve the exchange by name if possible
				var exch_name = Exchange?.Name;

				if (m_exchanges != null)
				{
					m_exchanges.CurrentChanged -= HandleCurrentChanged;
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.Filter = exch => !(exch is CrossExchange);
					var match = m_exchanges.Cast<Exchange>().FirstOrDefault(x => x.Name == exch_name);
					if (match != null) m_exchanges.MoveCurrentTo(match);

					m_exchanges.CurrentChanged += HandleCurrentChanged;
					HandleCurrentChanged(null, null);
				}

				// Notify exchange collection changed
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exchanges)));

				// Handlers
				void HandleCurrentChanged(object sender, EventArgs e)
				{
					var pairs = Exchange?.Pairs;
					Pairs = pairs != null ? new ListCollectionView(pairs) : null;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exchange)));
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

				// Preserve the pair by name if possible
				var pair_name = Pair?.Name;

				if (m_pairs != null)
				{
					// Shouldn't need to subscribe to ((PairCollection)m_pairs.SourceCollection).CollectionChanged
					// because ICollectionView already does that automatically
					m_pairs.CurrentChanged -= HandleCurrentChanged;
				}
				m_pairs = value;
				if (m_pairs != null)
				{
					m_pairs.SortDescriptions.Add(new SortDescription(nameof(TradePair.Base), ListSortDirection.Ascending));
					m_pairs.SortDescriptions.Add(new SortDescription(nameof(TradePair.Quote), ListSortDirection.Ascending));
					var match = m_pairs.Cast<TradePair>().FirstOrDefault(x => x.Name == pair_name);
					if (match != null) m_pairs.MoveCurrentTo(match);

					m_pairs.CurrentChanged += HandleCurrentChanged;
					HandleCurrentChanged(null, null);
				}

				// Notify pairs collection changed
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pairs)));

				// Handlers
				void HandleCurrentChanged(object sender, EventArgs e)
				{
					var tfs = Pair?.CandleDataAvailable.ToList();
					TimeFrames = tfs != null ? new ListCollectionView(tfs) : null;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pair)));
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

				// Preserve the time frame
				var tf = TimeFrame;

				if (m_time_frames != null)
				{
					m_time_frames.CurrentChanged -= HandleCurrentChanged;
				}
				m_time_frames = value;
				if (m_time_frames != null)
				{
					var match = m_time_frames.Cast<ETimeFrame>().FirstOrDefault(x => x == tf);
					if (match != ETimeFrame.None) m_time_frames.MoveCurrentTo(match);

					m_time_frames.CurrentChanged += HandleCurrentChanged;
					HandleCurrentChanged(null, null);
				}

				// Notify time frame collection changed
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeFrames)));

				// Handlers
				void HandleCurrentChanged(object sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeFrame)));
				}
			}
		}
		private ICollectionView m_time_frames;

		/// <summary>Current exchange</summary>
		public Exchange Exchange
		{
			get => (Exchange)Exchanges?.CurrentItem;
			set => Exchanges?.MoveCurrentTo(value);
		}

		/// <summary>The currently selected trade pair</summary>
		public TradePair Pair
		{
			get => (TradePair)Pairs?.CurrentItem;
			set
			{
				if (value != null) Exchange = value.Exchange;
				Pairs?.MoveCurrentTo(value);
			}
		}

		/// <summary>The currently selected time frame</summary>
		public ETimeFrame TimeFrame
		{
			get => (ETimeFrame?)TimeFrames?.CurrentItem ?? ETimeFrame.None;
			set => TimeFrames?.MoveCurrentTo(value);
		}

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
