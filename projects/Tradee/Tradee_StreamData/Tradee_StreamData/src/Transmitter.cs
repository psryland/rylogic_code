using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.common;
using pr.extn;
using pr.util;
using Tradee;

namespace cAlgo
{
	/// <summary>Handles sending data for a single symbol</summary>
	public class Transmitter :IDisposable ,INotifyPropertyChanged
	{
		private ManualResetEvent m_quit;
		private Thread m_work;
		private object m_lock;

		public Transmitter(TradeeBotModel model, ETradePairs pair, TransmitterSettings settings)
		{
			try
			{
				Settings = settings;
				Model = model;
				Data = new ConcurrentDictionary<TimeFrame, MarketSeriesData>();
				Pair = pair;

				m_lock = new object();
				m_quit = new ManualResetEvent(false);
				m_work = new Thread(Run){ Name = "{0} Transmitter".Fmt(SymbolCode) };
				m_work.Start();
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public virtual void Dispose()
		{
			if (m_work != null && m_work.ThreadState != System.Threading.ThreadState.Unstarted)
			{
				m_quit.Set();
				if (!m_work.Join(1000))
					Debug.Write("Failed to shutdown Transmitter thread for {0}".Fmt(Pair));
			}
			Model = null;
		}

		/// <summary>Settings for this transmitter</summary>
		private TransmitterSettings Settings
		{
			get;
			set;
		}

		/// <summary>Tradee bot app logic</summary>
		private TradeeBotModel Model
		{
			get { return m_impl_model; }
			set
			{
				if (m_impl_model == value) return;
				m_impl_model = value;
			}
		}
		private TradeeBotModel m_impl_model;

		/// <summary>A map from time frame to data series</summary>
		public ConcurrentDictionary<TimeFrame, MarketSeriesData> Data
		{
			get;
			private set;
		}
		public class MarketSeriesData
		{
			public MarketSeriesData(MarketSeries series)
			{
				Series          = series;
				LastTransmit    = DateTime.MinValue;
				LatestPriceData = PriceData.Default;
				LatestCandle    = Candle.Default;
			}
			public MarketSeries Series { get; set; }
			public DateTime  LastTransmit { get; set; }
			public PriceData LatestPriceData { get; set; }
			public Candle    LatestCandle { get; set; }
		}

		/// <summary>The trading pair</summary>
		public ETradePairs Pair
		{
			get;
			private set;
		}

		/// <summary>The symbol code for the trading pair</summary>
		public string SymbolCode
		{
			get { return Pair.ToString(); }
		}

		/// <summary>The symbol data</summary>
		public Symbol Symbol
		{
			get { return Model.GetSymbol(SymbolCode); }
		}

		/// <summary>Get the list of time frames to transmit</summary>
		public TimeFrame[] TimeFrames
		{
			get
			{
				lock (m_lock)
					return Settings.TimeFrames.OrderBy(x => x).Select(x => x.ToCAlgoTimeframe()).ToArray();
			}
			set
			{
				lock (m_lock)
					Settings.TimeFrames = value.Select(x => x.ToTradeeTimeframe()).OrderBy(x => x).ToArray();
				Model.RunOnMainThread(() => RaisePropertyChanged(new PropertyChangedEventArgs(R<Transmitter>.Name(x => x.TimeFrames))));
			}
		}

		/// <summary>The time frames that are available</summary>
		public TimeFrame[] AvailableTimeFrames
		{
			get
			{
				var keys = Data.Keys.ToArray();
				keys.Sort(Cmp<TimeFrame>.From((l,r) => l < r));
				return keys;
			}
		}

		/// <summary>True while the transmitter is downloading time frame series data</summary>
		public bool RequestingSeriesData
		{
			get
			{
				lock (m_lock)
					return m_requesting_series_data;
			}
			set
			{
				lock (m_lock)
					m_requesting_series_data = value;
				Model.RunOnMainThread(() => RaisePropertyChanged(new PropertyChangedEventArgs(R<Transmitter>.Name(x => x.RequestingSeriesData))));
			}
		}
		private bool m_requesting_series_data;

		/// <summary>Property changed notification</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void RaisePropertyChanged(PropertyChangedEventArgs args)
		{
			if (PropertyChanged == null) return;
			PropertyChanged(this, args);
		}

		/// <summary>Transmit thread entry</summary>
		private void Run()
		{
			try
			{
				// Transmit loop
				var requested_time_frames = new TimeFrame[0];
				for (;!m_quit.WaitOne(500);)
				{
					// If the list of requested time frames has changed, get the new data series
					RequestingSeriesData = !requested_time_frames.SequenceEqual(TimeFrames);
					if (RequestingSeriesData)
					{
						// Update the requested list
						requested_time_frames = TimeFrames;

						// Add time frames that aren't in 'Data'
						var existing = Data.Keys.ToArray();
						var tf_to_get = requested_time_frames.Where(x => !existing.Contains(x)).ToArray();
						if (tf_to_get.Length != 0)
						{
							// Getting a market series takes ages..
							ThreadPool.QueueUserWorkItem(_ =>
							{
								try
								{
									var sym = Symbol;
									foreach (var tf in tf_to_get)
									{
										if (m_quit.WaitOne(0))
											return;

										// Get the series from the server
										var data = new MarketSeriesData(Model.GetSeries(sym, tf));
										Data.AddOrUpdate(tf, data, (k, v) => data);
									}
								}
								catch (Exception ex)
								{
									Trace.WriteLine(ex.Message);
								}
							});
						}
					}

					// Transmit data for each time frame
					var time_frames = Data.Keys.ToArray();
					foreach (var tf in time_frames)
					{
						// Get the market series for this time frame
						MarketSeriesData data;
						if (!requested_time_frames.Contains(tf))
							Data.TryRemove(tf, out data);
						else if (Data.TryGetValue(tf, out data) && Model.Tradee.IsConnected)
							SendMarketData(data);
					}
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
		}

		/// <summary>Send market data for all known symbols</summary>
		private void SendMarketData(MarketSeriesData data)
		{
			var sym = Model.GetSymbol(SymbolCode);
			var series = data.Series;

			// If this is the first send, send the historic data too
			if (data.LatestCandle == Candle.Default)
				SendHistoricMarketData(data.Series);

			// Post the symbol price data
			{
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
				if (!data.LatestPriceData.Equals(price_data))
				{
					if (Model.Tradee.Post(new InMsg.SymbolData(sym.Code, price_data)))
					{
						data.LatestPriceData = price_data;
						data.LastTransmit = DateTime.Now;
					}
				}
			}

			// Post the latest data Tradee
			if (series.Close.Count != 0)
			{
				var candle = new Candle(
					series.OpenTime.LastValue.Ticks,
					series.Open.LastValue,
					series.High.LastValue,
					series.Low.LastValue,
					series.Close.LastValue,
					series.TickVolume.LastValue);

				// Only transmit if different
				if (!data.LatestCandle.Equals(candle))
				{
					if (Model.Tradee.Post(new InMsg.CandleData(sym.Code, series.TimeFrame.ToTradeeTimeframe(), candle)))
					{
						data.LatestCandle = candle;
						data.LastTransmit = DateTime.Now;
					}
				}
			}
		}

		/// <summary>Send a range of data</summary>
		public void SendHistoricMarketData(TimeFrame tf, DateTime beg, DateTime end)
		{
			// Get the market series for this time frame
			MarketSeriesData data;
			if (Data.TryGetValue(tf, out data))
			{
				var i_oldest = data.Series.OpenTime.GetIndexByTime(beg);
				var i_newest = data.Series.OpenTime.GetIndexByTime(end);
				SendHistoricMarketData(data.Series, i_newest, i_oldest);
			}
		}

		/// <summary>Send a range of market data. The index range is indices backwards in time, i.e. 0 = latest, 100 = 100 candles ago</summary>
		private void SendHistoricMarketData(MarketSeries series, int i_newest, int i_oldest)
		{
			Debug.Assert(i_newest <= i_oldest);

			// Send historical data to Tradee
			var open_time = new List<long>  (series.OpenTime.Count);
			var open      = new List<double>(series.Open.Count);
			var high      = new List<double>(series.High.Count);
			var low       = new List<double>(series.Low.Count);
			var close     = new List<double>(series.Close.Count);
			var volume    = new List<double>(series.TickVolume.Count);
			for (int i = i_oldest; i-- != i_newest; )
			{
				open_time.Add(series.OpenTime  .Last(i).Ticks);
				open     .Add(series.Open      .Last(i));
				high     .Add(series.High      .Last(i));
				low      .Add(series.Low       .Last(i));
				close    .Add(series.Close     .Last(i));
				volume   .Add(series.TickVolume.Last(i));
			}
			if (close.Count != 0)
			{
				var data = new Candles(
					open_time.ToArray(),
					open.ToArray(),
					high.ToArray(),
					low.ToArray(),
					close.ToArray(),
					volume.ToArray());
				Model.Tradee.Post(new InMsg.CandleData(SymbolCode, series.TimeFrame.ToTradeeTimeframe(), data));
				Debug.WriteLine("Historic data for {0} send".Fmt(series.SymbolCode));
			}
		}
		private void SendHistoricMarketData(MarketSeries series)
		{
			SendHistoricMarketData(series, 0, series.OpenTime.Count);
		}

		/// <summary></summary>
		public override string ToString()
		{
			return "Transmitter: {0} TimeFrames={1}".Fmt(SymbolCode, Data.Count);
		}
	}
}
