using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pr.attrib;
using pr.extn;
using Tradee;

namespace TestClient
{
	public class Transmitter :IDisposable
	{
		private ManualResetEvent m_send;
		private ManualResetEvent m_quit;
		private Thread m_thread;
		private object m_lock;
		private Random m_rng;
		private Candle m_candle;
		private double m_price;
		private long m_now;

		public Transmitter(string sym, MainUI model, ManualResetEvent send, ManualResetEvent quit)
		{
			SymbolCode = sym;
			TimeFrames = new [] { ETimeFrame.Min1, ETimeFrame.Hour1 };
			Model      = model;
			m_send     = send;
			m_quit     = quit;
			m_now      = new DateTime(2000,1,1,0,0,0).Ticks;
			m_rng      = new Random(0);
			m_candle   = new Candle(m_now, 0, 0, 0, 0, 0);
			m_price    = 1.0;

			m_lock = new object();
			m_thread = new Thread(Run){ Name = "{0} Transmitter".Fmt(SymbolCode) };
			m_thread.SetApartmentState(ApartmentState.STA);
			m_thread.Start();
		}
		public void Dispose()
		{
			if (m_thread == null)
				return;

			m_quit.Set();
			m_send.Set();
			if (m_thread.ThreadState != ThreadState.Unstarted)
				m_thread.Join();

			m_thread = null;
		}

		/// <summary>Fake symbol code</summary>
		public string SymbolCode { get; private set; }

		/// <summary>Time frames</summary>
		public ETimeFrame[] TimeFrames { get; private set; }

		/// <summary>Pipe connection</summary>
		public MainUI Model { get; private set; }

		/// <summary>Thread entry</summary>
		private void Run()
		{
			bool send = false;
			for (; !m_quit.WaitOne(100); )
			{
				m_price += m_rng.NextDouble(-0.01,+0.01);

				if (!m_send.WaitOne(0))
				{
					send = false;
					continue;
				}

				// Send historic data on first send
				if (!send)
				{
					SendHistoricData();
					send = true;
				}

				// Send per tick
				foreach (var tf in TimeFrames)
				{
					var one = Misc.TimeFrameToTicks(1.0, tf);
					var spread = m_rng.NextDouble(0.001, 0.01);

					// Fake price data
					if (++m_candle.Volume == 11)
					{
						m_now += one;

						m_candle.Timestamp = m_now;
						m_candle.Open      = m_price;
						m_candle.High      = m_price;
						m_candle.Low       = m_price;
						m_candle.Close     = m_price;
						m_candle.Volume    = 1;
					}
					m_candle.High    = Math.Max(m_candle.High, m_price);
					m_candle.Low     = Math.Min(m_candle.Low, m_price);
					m_candle.Close   = m_price;

					Model.Post(new InMsg.CandleData(SymbolCode, tf, m_candle));

					// Fake spread
					var sym_data = new PriceData(m_price + spread, m_price, 10000, 0.0001, 1.0, 1000, 1000, 100000000);
					var data = new InMsg.SymbolData(SymbolCode, sym_data);
					Model.Post(data);
				}
			}
		}

		/// <summary>Send fake historic data</summary>
		private void SendHistoricData()
		{
			const int Count = 20;
			foreach (var tf in TimeFrames)
			{
				var one = Misc.TimeFrameToTicks(1.0, tf);
				var candles = new Candles(
					Enumerable.Range(0, Count).Select(i => m_now - (Count - i) * one).ToArray(),
					Enumerable.Range(0, Count).Select(i => m_rng.NextDouble(0.5, 1.5)).ToArray(),
					Enumerable.Range(0, Count).Select(i => m_rng.NextDouble(1.5, 2.0)).ToArray(),
					Enumerable.Range(0, Count).Select(i => m_rng.NextDouble(0.0, 0.5)).ToArray(),
					Enumerable.Range(0, Count).Select(i => m_rng.NextDouble(0.5, 1.5)).ToArray(),
					Enumerable.Range(0, Count).Select(i => (double)(int)m_rng.NextDouble(1, 100)).ToArray());

				Model.Post(new InMsg.CandleData(SymbolCode, tf, candles));
			}
		}

		public override string ToString()
		{
			return "{0} - {1} - {2}".Fmt(SymbolCode, string.Join(" ", TimeFrames.Select(x => x.Desc())), m_price);
		}
	}
}
