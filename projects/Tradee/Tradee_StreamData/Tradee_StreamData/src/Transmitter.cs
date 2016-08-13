using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.extn;
using pr.util;

namespace Tradee
{
	/// <summary>Handles sending data for a single symbol</summary>
	public class Transmitter :IDisposable ,INotifyPropertyChanged
	{
		public Transmitter(TradeeBotModel model, ETradePairs pair, TransmitterSettings settings)
		{
			Settings             = settings;
			Model                = model;
			Data                 = new Dictionary<TimeFrame, MarketSeriesData>();
			HistoricDataRequests = new List<HistoricDataRequest>();
			Pair                 = pair;
			Enabled              = true;
			StatusMsg            = "Active";
		}
		public virtual void Dispose()
		{
			Model = null;
		}

		/// <summary>Settings for this transmitter</summary>
		public TransmitterSettings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Tradee bot app logic</summary>
		public TradeeBotModel Model
		{
			[DebuggerStepThrough] get { return m_impl_model; }
			private set
			{
				if (m_impl_model == value) return;
				m_impl_model = value;
			}
		}
		private TradeeBotModel m_impl_model;

		/// <summary>A map from time frame to data series</summary>
		public Dictionary<TimeFrame, MarketSeriesData> Data
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>A queue of requests for historic data to be sent</summary>
		public List<HistoricDataRequest> HistoricDataRequests { get; private set; }

		/// <summary>The trading pair</summary>
		public ETradePairs Pair
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The symbol code for the trading pair</summary>
		public string SymbolCode
		{
			[DebuggerStepThrough] get { return Pair.ToString(); }
		}

		/// <summary>The symbol data</summary>
		public Symbol Symbol
		{
			get { return m_symbol ?? (m_symbol = Model.GetSymbol(SymbolCode)); }
		}
		private Symbol m_symbol;

		/// <summary>Get the list of time frames to transmit</summary>
		public ETimeFrame[] TimeFrames
		{
			get { return Settings.TimeFrames; }
			set
			{
				if (TimeFrames.SequenceEqual(value)) return;
				Settings.TimeFrames = value.OrderBy(x => x).ToArray();
				OnPropertyChanged(new PropertyChangedEventArgs(R<Transmitter>.Name(x => x.TimeFrames)));
			}
		}

		/// <summary>The time frames that are available</summary>
		public TimeFrame[] AvailableTimeFrames
		{
			get { return Data.Keys.ToArray(); }
		}

		/// <summary>True while the transmitter is downloading time frame series data</summary>
		public bool RequestingSeriesData
		{
			[DebuggerStepThrough] get { return m_requesting_series_data; }
			set
			{
				if (m_requesting_series_data == value) return;
				m_requesting_series_data = value;
				OnPropertyChanged(new PropertyChangedEventArgs(R<Transmitter>.Name(x => x.RequestingSeriesData)));
			}
		}
		private bool m_requesting_series_data;

		/// <summary>Enable/Disable the transmitter</summary>
		public bool Enabled
		{
			[DebuggerStepThrough] get { return m_enabled; }
			set
			{
				if (m_enabled == value) return;
				m_enabled = value;
				StatusMsg = Enabled ? "Active" : "Disabled";
				OnPropertyChanged(new PropertyChangedEventArgs(R<Transmitter>.Name(x => x.Enabled)));
			}
		}
		private bool m_enabled;

		/// <summary>A description of what this transmitter is doing</summary>
		public string StatusMsg
		{
			[DebuggerStepThrough] get { return m_status; }
			private set
			{
				if (m_status == value) return;
				m_status = value;
				OnPropertyChanged(new PropertyChangedEventArgs(R<Transmitter>.Name(x => x.StatusMsg)));
			}
		}
		private string m_status;

		/// <summary>Ensure data is sent on the next Step(), even if unchanged</summary>
		public void ForcePost()
		{
			foreach (var data in Data)
			{
				data.Value.LastTransmittedPriceData = PriceData.Default;
				data.Value.LastTransmittedCandle = Candle.Default;
			}
		}

		/// <summary>Property changed notification</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(PropertyChangedEventArgs args)
		{
			if (PropertyChanged == null) return;
			PropertyChanged(this, args);
		}

		/// <summary>Poll instrument state</summary>
		public void Step()
		{
			// Get the time frames to send, and the time frames being sent
			var desired = TimeFrames.Select(x => x.ToCAlgoTimeframe()).ToArray();
			var available = AvailableTimeFrames;

			// Look for any missing time frames
			var to_get = desired.FirstOrDefault(x => !available.Contains(x));
			if (m_pending == null && to_get != null)
			{
				RequestingSeriesData = true;
				m_pending = to_get;

				// Get the missing time frame series.
				// This can take ages so do it in a background thread
				StatusMsg = "Acquiring {0},{1}".Fmt(SymbolCode, m_pending.ToTradeeTimeframe());

				var model = Model;
				ThreadPool.QueueUserWorkItem(x =>
				{
					var series = (MarketSeries)null;
					var error = (Exception)null;
					try
					{
						// Get the symbol
						var sym = model.GetSymbol(SymbolCode);
						if (sym == null)
							throw new Exception("Symbol {0} not available".Fmt(SymbolCode));

						// Get the data series
						series = model.GetSeries(sym, (TimeFrame)x);
					}
					catch (Exception ex) { error = ex; }
					try
					{
						// Add the received series to the collection
						model.RunOnMainThread(() =>
						{
							Data.Add(m_pending, new MarketSeriesData(series));
							m_pending = null;
							RequestingSeriesData = false;
							StatusMsg = error != null ? error.Message : "Active";
						});
					}
					catch { }
				}, m_pending);
			}

			// Transmit data for each time frame
			foreach (var tf in available)
			{
				// If not needed any more, remove it from 'Data'
				if (!desired.Contains(tf))
				{
					Data.Remove(tf);
					continue;
				}

				// Get the market series for this time frame
				MarketSeriesData data;
				if (Data.TryGetValue(tf, out data))
					SendMarketData(data);
			}

			// Service any historic data requests
			var done = new HashSet<HistoricDataRequest>();
			foreach (var req in HistoricDataRequests)
			{
				// Get the market series for the requested time frame
				MarketSeriesData data;
				if (!Data.TryGetValue(req.TimeFrame, out data))
					continue;

				try
				{
					// Get the index range for the request time range
					var count = data.Series.OpenTime.Count;
					var i_oldest = req.UseIndices ? req.OldestIndex : count - data.Series.OpenTime.GetIndexByTime(req.Beg.DateTime); // oldest (i.e. largest index)
					var i_newest = req.UseIndices ? req.NewestIndex : count - data.Series.OpenTime.GetIndexByTime(req.End.DateTime); // newest (i.e. smallest index)
					SendHistoricMarketData(data.Series, i_oldest, i_newest);
				}
				catch { } // ignore failures
				done.Add(req);
			}

			// Remove the requests we've serviced
			HistoricDataRequests.RemoveIf(x => done.Contains(x));
		}
		private TimeFrame m_pending;

		/// <summary>Send market data</summary>
		private void SendMarketData(MarketSeriesData data)
		{
			// Get the latest symbol (price data) information
			var sym = Symbol;

			// Post at least every X seconds, changed or not
			var post = DateTimeOffset.UtcNow - data.LastUpdateUTC > TimeSpan.FromSeconds(10);

			// Post the symbol price data
			var price_data = new PriceData(
				sym.Ask,
				sym.Bid,
				sym.LotSize,
				sym.PipSize,
				sym.PipValue,
				sym.VolumeMin,
				sym.VolumeStep,
				sym.VolumeMax);

			// Only transmit if different
			if (post || !price_data.Equals(data.LastTransmittedPriceData))
			{
				if (Model.Post(new InMsg.SymbolData(sym.Code, price_data)))
				{
					data.LastTransmittedPriceData = price_data;
					post = true;
				}
			}

			// Post the latest candle data
			if (data.Series.Close.Count != 0)
			{
				var candle = new Candle(
					data.Series.OpenTime.LastValue.Ticks,
					data.Series.Open.LastValue,
					data.Series.High.LastValue,
					data.Series.Low.LastValue,
					data.Series.Close.LastValue,
					data.Series.TickVolume.LastValue);

				// Only transmit if different
				if (post || !candle.Equals(data.LastTransmittedCandle))
				{
					if (Model.Post(new InMsg.CandleData(sym.Code, data.Series.TimeFrame.ToTradeeTimeframe(), candle)))
					{
						data.LastTransmittedCandle = candle;
						post = true;
					}
				}
			}

			// Record this update attempt
			data.LastUpdateUTC = DateTimeOffset.UtcNow;
			data.LastTransmitUTC = post ? DateTimeOffset.UtcNow : data.LastTransmitUTC;
		}

		/// <summary>Send a range of market data.</summary>
		private void SendHistoricMarketData(MarketSeries series, int i_oldest, int i_newest)
		{
			// Send historical data to Tradee
			var open_time = new List<long  >(series.OpenTime  .Count);
			var open      = new List<double>(series.Open      .Count);
			var high      = new List<double>(series.High      .Count);
			var low       = new List<double>(series.Low       .Count);
			var close     = new List<double>(series.Close     .Count);
			var volume    = new List<double>(series.TickVolume.Count);

			Debug.Assert(i_newest <= i_oldest);
			for (int i = i_oldest; i-- != i_newest; )
			{
				var t = series.OpenTime  .Last(i).Ticks;
				var o = series.Open      .Last(i);
				var h = series.High      .Last(i);
				var l = series.Low       .Last(i);
				var c = series.Close     .Last(i);
				var v = series.TickVolume.Last(i);

				open_time.Add(t);
				open     .Add(o);
				high     .Add(Math.Max(h, Math.Max(o,c)));
				low      .Add(Math.Min(l, Math.Min(o,c)));
				close    .Add(c);
				volume   .Add(v);
			}

			// Nothing available?
			if (close.Count == 0)
				return;

			// Create a candle batch
			var data = new Candles(
				open_time.ToArray(),
				open     .ToArray(),
				high     .ToArray(),
				low      .ToArray(),
				close    .ToArray(),
				volume   .ToArray());
			if (Model.Post(new InMsg.CandleData(SymbolCode, series.TimeFrame.ToTradeeTimeframe(), data)))
				Debug.WriteLine("Historic data for {0},{1} sent".Fmt(series.SymbolCode, series.TimeFrame.ToTradeeTimeframe()));
		}
		private void SendHistoricMarketData(MarketSeries series)
		{
			SendHistoricMarketData(series, series.OpenTime.Count, 0);
		}

		/// <summary></summary>
		public override string ToString()
		{
			return "Transmitter: {0} TimeFrames={1}".Fmt(SymbolCode, Data.Count);
		}
	}

	/// <summary>A range of historic data that's needed</summary>
	public class HistoricDataRequest
	{
		public HistoricDataRequest(TimeFrame tf, DateTimeOffset beg, DateTimeOffset end)
		{
			Debug.Assert(beg <= end, "Time range must be positive definite");
			TimeFrame = tf;
			Beg = beg;
			End = end;
			UseIndices = false;
		}
		public HistoricDataRequest(TimeFrame tf, int oldest, int newest)
		{
			Debug.Assert(oldest >= newest, "Candle data indices have 0 = newest, +ve = past");
			TimeFrame = tf;
			NewestIndex = newest;
			OldestIndex = oldest;
			UseIndices = true;
		}

		/// <summary>The time frame to get data for</summary>
		public TimeFrame TimeFrame;

		/// <summary>The time range into the candle data series</summary>
		public DateTimeOffset Beg;
		public DateTimeOffset End;

		/// <summary>The index into the candle data series</summary>
		public int NewestIndex;
		public int OldestIndex;

		/// <summary>True to use index values, false to use time range values</summary>
		public bool UseIndices;
	}
}
