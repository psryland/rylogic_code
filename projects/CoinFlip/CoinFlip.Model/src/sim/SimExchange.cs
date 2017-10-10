using System;
using System.Collections.Generic;
using System.Linq;
using pr.common;
using pr.util;

namespace CoinFlip
{
	/// <summary>A mock exchange that replaces a real exchange during back testing</summary>
	public class SimExchange :IDisposable
	{
		private readonly Simulation m_sim; 
		private readonly Exchange m_exch;
		private Dictionary<TradePair, Instrument> m_src;

		public SimExchange(Simulation sim, Exchange exch)
		{
			try
			{
				m_sim = sim;
				m_exch = exch;
				m_src = new Dictionary<TradePair, Instrument>();

				// Copy construct the collections, except positions and history.
				// Positions an history will be populated by the simulation
				Coins = new CoinCollection(m_exch.Coins);
				Pairs = new PairCollection(m_exch.Pairs);
				Balance = new BalanceCollection(m_exch.Balance);
				Positions = new PositionsCollection(m_exch);
				History = new HistoryCollection(m_exch);

				//// Don't need source data for the cross exchange
				//if (m_exch == Model.CrossExchange)
				//	return;

				//// Create an instrument for each pair. Ideally we'd have source
				//// data from 'm_exch'. If not available however, use any exchange's data.
				//foreach (var pair in Pairs.Values)
				//{
				//	var time_frame = Model.Settings.BackTesting.TimeFrame;
				//	var db_filepath = Misc.CacheDBFilePath(pair.NameWithExchange);

				//	// Assume no data available
				//	m_src.Add(pair, null);

				//	// Find a trade pair that we have price data for
				//	if (Path_.FileExists(db_filepath))
				//	{
				//		// There is data from this exchange available
				//		m_src[pair] = new Instrument($"SimExch: {exch.Name}", Model.PriceData[pair, time_frame]);
				//	}
				//	else
				//	{
				//		// Look for a price data database from a different exchange
				//		var pair_name = pair.Name.Replace('/','_');
				//		var fd = Path_.EnumFileSystem(Path_.Directory(db_filepath), regex_filter:$@"{pair_name}\s*-\s*(?<exchange>.*)\.db").FirstOrDefault();
				//		if (fd == null)
				//			continue;

				//		// Find the corresponding pair on the exchange that the database file is for
				//		var exch_name = fd.RegexMatch.Groups["exchange"].Value;
				//		var src_exch = Model.TradingExchanges.FirstOrDefault(x => x.Name == exch_name);
				//		if (src_exch == null)
				//			continue;

				//		// Find the pair on the source exchange
				//		var src_pair = src_exch.Pairs[pair.Name];
				//		if (src_pair == null)
				//			continue;

				//		m_src[pair] = new Instrument($"SimExch: {exch.Name}", Model.PriceData[src_pair, time_frame]);
				//	}
				//}
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			Util.DisposeAll(m_src?.Values);
			m_src?.Clear();
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_sim.Model; }
		}

		/// <summary>Coins associated with this exchange</summary>
		public CoinCollection Coins { get; private set; }

		/// <summary>The pairs associated with this exchange</summary>
		public PairCollection Pairs { get; private set; }

		/// <summary>The balance of the given coin on this exchange</summary>
		public BalanceCollection Balance { get; private set; }

		/// <summary>Positions by order id</summary>
		public PositionsCollection Positions { get; private set; }

		/// <summary>Trade history on this exchange, keyed on order ID</summary>
		public HistoryCollection History { get; private set; }

		/// <summary>Step the exchange</summary>
		public void Step()
		{
			

		}
	}
}
