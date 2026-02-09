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
			Model = null!;
		}

		/// <summary></summary>
		public Model Model
		{
			get;
			private set
			{
				if (field == value) return;
				if (field != null)
				{
					Exchanges = null!;
				}
				field = value;
				if (field != null)
				{
					Exchanges = new ListCollectionView(field.Exchanges);
				}
			}
		} = null!;

		/// <summary></summary>
		public string Address => $"{Exchange?.Name}-{Pair?.Name}-{TimeFrame}";

		/// <summary>The available exchanges</summary>
		public ICollectionView Exchanges
		{
			get;
			private set
			{
				if (field == value) return;

				// Preserve the exchange by name if possible
				var exch_name = Exchange?.Name;

				if (field != null)
				{
					field.CurrentChanged -= HandleCurrentChanged;
				}
				field = value;
				if (field != null)
				{
					field.Filter = exch => exch is not CrossExchange;
					var match = field.Cast<Exchange>().FirstOrDefault(x => x.Name == exch_name);
					if (match != null) field.MoveCurrentTo(match);

					field.CurrentChanged += HandleCurrentChanged;
					HandleCurrentChanged(null, EventArgs.Empty);
				}

				// Notify exchange collection changed
				NotifyPropertyChanged(nameof(Exchanges));

				// Handlers
				void HandleCurrentChanged(object? sender, EventArgs e)
				{
					var pairs = Exchange?.Pairs;
					Pairs = pairs != null ? new ListCollectionView(pairs) : new ListCollectionView(Array.Empty<TradePair>());
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exchange)));
				}
			}
		} = null!;

		/// <summary>The pairs available on the current exchange</summary>
		public ICollectionView Pairs
		{
			get;
			private set
			{
				if (field == value) return;

				// Preserve the pair by name if possible
				var pair_name = Pair?.Name;

				if (field != null)
				{
					// Shouldn't need to subscribe to ((PairCollection)field.SourceCollection).CollectionChanged
					// because ICollectionView already does that automatically
					field.CurrentChanged -= HandleCurrentChanged;
				}
				field = value;
				if (field != null)
				{
					field.SortDescriptions.Add(new SortDescription(nameof(TradePair.Base), ListSortDirection.Ascending));
					field.SortDescriptions.Add(new SortDescription(nameof(TradePair.Quote), ListSortDirection.Ascending));
					var match = field.Cast<TradePair>().FirstOrDefault(x => x.Name == pair_name);
					if (match != null) field.MoveCurrentTo(match);

					field.CurrentChanged += HandleCurrentChanged;
					HandleCurrentChanged(null, EventArgs.Empty);
				}

				// Notify pairs collection changed
				NotifyPropertyChanged(nameof(Pairs));

				// Handlers
				void HandleCurrentChanged(object? sender, EventArgs e)
				{
					var tfs = Pair?.CandleDataAvailable.ToList();
					TimeFrames = tfs != null ? new ListCollectionView(tfs) : new ListCollectionView(Array.Empty<ETimeFrame>());
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pair)));
				}
			}
		} = null!;

		/// <summary>The available time frames for the selected pair on the selected exchange</summary>
		public ICollectionView TimeFrames
		{
			get;
			private set
			{
				if (field == value) return;

				// Preserve the time frame
				var tf = TimeFrame;

				if (field != null)
				{
					field.CurrentChanged -= HandleCurrentChanged;
				}
				field = value;
				if (field != null)
				{
					var match = field.Cast<ETimeFrame>().FirstOrDefault(x => x == tf);
					if (match != ETimeFrame.None) field.MoveCurrentTo(match);

					field.CurrentChanged += HandleCurrentChanged;
					HandleCurrentChanged(null, EventArgs.Empty);
				}

				// Notify time frame collection changed
				NotifyPropertyChanged(nameof(TimeFrames));

				// Handlers
				void HandleCurrentChanged(object? sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeFrame)));
				}
			}
		} = null!;

		/// <summary>Current exchange</summary>
		public Exchange? Exchange
		{
			get => (Exchange?)Exchanges?.CurrentItem;
			set => Exchanges?.MoveCurrentTo(value);
		}

		/// <summary>The currently selected trade pair</summary>
		public TradePair? Pair
		{
			get => (TradePair?)Pairs?.CurrentItem;
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
		public Instrument? FindInstrument(string name)
		{
			if (Pair == null || TimeFrame == ETimeFrame.None) return null;
			return new Instrument(name, Model.PriceData[Pair, TimeFrame]);
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
