using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using cAlgo.API;
using cAlgo.API.Indicators;
using pr.extn;

namespace Rylobot
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Rylobot :Robot
	{
		private RylobotUI m_ui;
		private Thread m_thread;
		private ManualResetEvent m_running;

		private Random m_rng;
		private Position m_position;
		private const string Label = "StrategyPotLuck";
		private MovingAverage m_ema;

		[Parameter("Pip Step", DefaultValue = 50)]
		public int PipStep { get; set; }

		protected override void OnStart()
		{
			base.OnStart();

			m_rng = new Random();

			// Look for any existing trades created by this strategy
			m_position = Positions.FirstOrDefault(x => x.Label == Label);

			m_ema = Indicators.MovingAverage(MarketSeries.Close, 55, MovingAverageType.Exponential);

			Positions.Closed += HandlePositionClosed;
		}
		protected override void OnStop()
		{
			Positions.Closed -= HandlePositionClosed;
			base.OnStop();
		}
		protected override void OnTick()
		{
			base.OnTick();

			// No current position? create one
			if (m_position == null)
				CreateOrder();
		}

		/// <summary>Called when a position closes</summary>
		private void HandlePositionClosed(PositionClosedEventArgs args)
		{
			if (m_position != null && m_position.Id == args.Position.Id)
			{
				m_position = null;
				CreateOrder();
			}
		}

		private void CreateOrder(Position last)
		{
			if (m_position != null)
				throw new Exception("Order already exists");

			var tt = TradeType.Buy;
			if (last != null)
			{
				tt = last.GrossProfit > 0 ? last.TradeType : last.TradeType.Opposite();
			}

			var grad = m_ema.Result.Gradient(m_ema.Result.Count-1);
			var thres = 0f;//grad > 0 ? -0.25f : +0.25f;
			var tt = m_rng.FloatC(0f, 1f) > thres ? TradeType.Buy : TradeType.Sell;

			var r = ExecuteMarketOrder(tt, Symbol, 1000, Label, 2*PipStep, PipStep);
			if (r.IsSuccessful)
			{
				m_position = r.Position;
			}
			else
			{
				Print("Create order failed: {0}".Fmt(r.Error));
			}
		}


		#if false
		protected override void OnStart()
		{
			try
			{
				m_running = new ManualResetEvent(false);

				// Create a thread and launch the Rylobot UI in it
				m_thread = new Thread(Main) { Name = "Rylobot" };
				m_thread.SetApartmentState(ApartmentState.STA);
				m_thread.Start();

				m_running.WaitOne(1000);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				m_thread = null;
				Stop();
			}
		}
		protected override void OnStop()
		{
			base.OnStop();
			try
			{
				if (m_ui != null && !m_ui.IsDisposed && m_ui.IsHandleCreated)
					m_ui.BeginInvoke(() => m_ui.Close());
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
		}
		protected override void OnTick()
		{
			base.OnTick();
			m_ui.Invoke(() => m_ui.Model.OnTick());
		}
		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);
			m_ui.Invoke(() => m_ui.Model.OnPositionClosed(position));
		}

		/// <summary>Thread entry point</summary>
		[STAThread] private void Main()
		{
			try
			{
				Trace.WriteLine("RylobotUI Created");

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				using (m_ui = new RylobotUI(this))
				{
					m_running.Set();
					Application.Run(m_ui);
				}

				Stop();
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
		}
		#endif
	}
}
