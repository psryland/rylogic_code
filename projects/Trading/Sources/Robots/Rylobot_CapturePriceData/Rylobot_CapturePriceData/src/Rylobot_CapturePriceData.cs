using System.IO;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Rylobot_CapturePriceData : Robot
	{
		/// <summary>File of price tick data</summary>
		private StreamWriter m_pd_out;

		/// <summary>File of candle tick data</summary>
		private StreamWriter m_cd_out;

		/// <summary>Incremented whenever OnTick is called</summary>
		private int m_tick_number;

		[Parameter("Output Directory", DefaultValue = "P:\\dump\\trading\\capture")]
		public string OutDir { get; set; }

		protected override void OnStart()
		{
			var dir = Path.Combine(OutDir, string.Format("{0} - {1}", Symbol.Code, TimeFrame.ToString()));
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			// Create a file for candle tick data
			m_cd_out = new StreamWriter(new FileStream(Path.Combine(dir,"candle_data.csv"), FileMode.Create, FileAccess.Write, FileShare.Read));
			m_cd_out.WriteLine("TickNumber,CandleIndex,OpenTime,Open,High,Low,Close,Median,TickVolume");
					
			// Log the initial market series data
			for (int i = 0; i != MarketSeries.OpenTime.Count; ++i)
				WriteCandleTick(i);

			// Create a file for the price tick data
			m_pd_out = new StreamWriter(new FileStream(Path.Combine(dir,"price_data.csv"), FileMode.Create, FileAccess.Write, FileShare.Read));
			m_pd_out.WriteLine("TickNumber,Timestamp,Ask,Bid");

			// Write the initial price
			m_pd_out.WriteLine(string.Format("{0},{1},{2},{3}", m_tick_number, Server.Time.Ticks, Symbol.Ask, Symbol.Bid));

			// Create a file with symbol info
			using (var sym = new StreamWriter(new FileStream(Path.Combine(dir,"symbol.csv"), FileMode.Create, FileAccess.Write, FileShare.Read)))
			{
				sym.WriteLine(string.Format("Code,{0}"           , Symbol.Code           ));
				sym.WriteLine(string.Format("Digits,{0}"         , Symbol.Digits         ));
				sym.WriteLine(string.Format("LotSize,{0}"        , Symbol.LotSize        ));
				sym.WriteLine(string.Format("PipSize,{0}"        , Symbol.PipSize        ));
				sym.WriteLine(string.Format("PipValue,{0}"       , Symbol.PipValue       ));
				sym.WriteLine(string.Format("PreciseLeverage,{0}", Symbol.PreciseLeverage));
				sym.WriteLine(string.Format("TickSize,{0}"       , Symbol.TickSize       ));
				sym.WriteLine(string.Format("TickValue,{0}"      , Symbol.TickValue      ));
				sym.WriteLine(string.Format("VolumeMax,{0}"      , Symbol.VolumeMax      ));
				sym.WriteLine(string.Format("VolumeMin,{0}"      , Symbol.VolumeMin      ));
				sym.WriteLine(string.Format("VolumeStep,{0}"     , Symbol.VolumeStep     ));
			}
		}
		protected override void OnStop()
		{
			// Close files
			m_pd_out.Dispose();
			m_cd_out.Dispose();
		}
		protected override void OnTick()
		{
			++m_tick_number;

			WriteCandleTick(MarketSeries.OpenTime.Count - 1);
			WritePriceTick();
		}

		/// <summary>Write a line to the candle data file</summary>
		private void WriteCandleTick(int candle_index)
		{
			m_cd_out.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}"
				,m_tick_number
				,candle_index
				,MarketSeries.OpenTime  [candle_index].Ticks
				,MarketSeries.Open      [candle_index]
				,MarketSeries.High      [candle_index]
				,MarketSeries.Low       [candle_index]
				,MarketSeries.Close     [candle_index]
				,MarketSeries.Median    [candle_index]
				,MarketSeries.TickVolume[candle_index]));
		}

		/// <summary>Write a line to the price tick data file</summary>
		private void WritePriceTick()
		{
			m_pd_out.WriteLine(string.Format("{0},{1},{2},{3}"
				,m_tick_number
				,Server.Time.Ticks
				,Symbol.Ask
				,Symbol.Bid));
		}
	}
}
