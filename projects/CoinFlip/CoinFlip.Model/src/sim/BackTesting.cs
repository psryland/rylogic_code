using System;
using System.Diagnostics;
using System.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public partial class Model
	{
		/// <summary>True while back testing</summary>
		public bool BackTesting
		{
			[DebuggerStepThrough] get { return m_impl_backtesting; }
			set
			{
				if (BackTesting == value) return;
				Debug.Assert(AssertMainThread());

				// Don't allow back testing to be enabled when 'AllowTrades' is enabled
				if (value && AllowTrades)
					return;

				// Notify about to change back testing mode
				BackTestingChanging.Raise(this, new PrePostEventArgs(after:false));

				// Stop all bots before enabling/disabling back testing
				foreach (var bot in Bots)
					bot.Active = false;

				// Integrate market updates before enabling/disabling back testing so that
				// updates from live data don't end up in back testing data or visa versa.
				// All exchanges should have their update threads deactivated by now.
				Debug.Assert(Exchanges.All(x => x.UpdateThreadActive == false));
				IntegrateMarketUpdates();

				// Enable/Disable back testing
				// Create a new Simulation instance for back testing. Do this before
				// setting 'BackTesting' to true so that the constructor can make copies
				// of the existing collections. Careful with UtcNow tho...
				if (value)
				{
					Simulation = new Simulation(this);
					m_impl_backtesting = true;
				}
				else
				{
					m_impl_backtesting = false;
					Simulation = null;
				}

				// Notify back testing changed
				Log.Write(ELogLevel.Debug, value ? "Back testing enabled" : "Back testing disabled");
				BackTestingChanging.Raise(this, new PrePostEventArgs(after:true));

				// Reset after back-testing starts
				if (value)
				{
					var ss = Settings.BackTesting;
					Simulation.SetStartTime(ss.TimeFrame, ss.Steps);
					Simulation.Reset();
				}
			}
		}
		private bool m_impl_backtesting;

		/// <summary>Raise before and after 'BackTesting' is changed</summary>
		public event EventHandler<PrePostEventArgs> BackTestingChanging;

		/// <summary>The back testing data container</summary>
		public Simulation Simulation
		{
			[DebuggerStepThrough] get { return m_simulation; }
			private set
			{
				if (m_simulation == value) return;
				if (m_simulation != null)
				{
					m_simulation.SimReset -= HandleSimReset;
					m_simulation.SimStep -= HandleSimStep;
					m_simulation.SimRunningChanged -= HandleSimRunningChanged;
					Util.Dispose(ref m_simulation);
				}
				m_simulation = value;
				if (m_simulation != null)
				{
					m_simulation.SimRunningChanged += HandleSimRunningChanged;
					m_simulation.SimStep += HandleSimStep;
					m_simulation.SimReset += HandleSimReset;
				}

				// Handlers
				void HandleSimRunningChanged(object sender, PrePostEventArgs e)
				{
					SimRunningChanged.Raise(sender, e);
				}
				void HandleSimStep(object sender, SimStepEventArgs e)
				{
					SimStep.Raise(sender, e);
				}
				void HandleSimReset(object sender, SimResetEventArgs e)
				{
					SimReset.Raise(sender, e);
				}
			}
		}
		private Simulation m_simulation;

		/// <summary>True while the simulation is running</summary>
		public bool SimRunning
		{
			get { return Simulation != null && Simulation.Running; }
		}

		/// <summary>Raised when the simulation is reset back to the start time</summary>
		public event EventHandler<SimResetEventArgs> SimReset;

		/// <summary>Raised when the simulation starts/stops</summary>
		public event EventHandler<PrePostEventArgs> SimRunningChanged;

		/// <summary>Raised when the simulation time changes</summary>
		public event EventHandler<SimStepEventArgs> SimStep;

		/// <summary>Look for fake orders that would be filled by the current price levels</summary>
		private void SimulateFakeOrders()
		{
			foreach (var exch in Exchanges)
			{
				foreach (var pos in exch.Orders.Values.Where(x => x.Fake).ToArray())
				{
					var trade = pos.Pair.MakeTrade(pos.FundId, pos.TradeType, pos.VolumeIn);
					if (Math.Sign(pos.PriceQ2B - trade.PriceQ2B) == pos.TradeType.Sign())
						pos.FillFakeOrder();
				}
			}
		}

		/// <summary>Remove the fake cash from all exchange balances</summary>
		private void ResetFakeCash()
		{
			//foreach (var bal in Exchanges.SelectMany(x => x.Balance.Values))
			//	bal.FakeCash.Clear();
		}
	}
}
