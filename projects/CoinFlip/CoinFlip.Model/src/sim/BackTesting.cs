using System;
using System.Diagnostics;
using System.Linq;
using pr.common;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	public partial class Model
	{
		/// <summary>True while back testing</summary>
		public bool BackTesting
		{
			get { return m_back_testing; }
			set
			{
				if (m_back_testing == value || AllowTrades) return;
				BackTestingChanging.Raise(this, new PrePostEventArgs(after:false));

				if (m_back_testing)
				{
					Log.Write(ELogLevel.Debug, "Back testing disabled");
				}

				// Integrate market updates before enabling/disabling back testing so that
				// updates from live data don't end up in back testing data or visa versa.
				IntegrateMarketUpdates();

				// Create a new Simulation instance for this back testing. Do this before
				// setting 'm_back_testing' to true so that the constructor can make copies
				// of the existing collections.
				Simulation = value ? new Simulation(this) : null;

				// Enable/Disable back testing
				m_back_testing = value;

				if (m_back_testing)
				{
					Log.Write(ELogLevel.Debug, "Back testing enabled");

					// Reset to the start time
					Simulation.SetStartTime(Settings.BackTesting.TimeFrame, Settings.BackTesting.Steps);
				}

				BackTestingChanging.Raise(this, new PrePostEventArgs(after:true));
			}
		}
		private bool m_back_testing;

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
				foreach (var pos in exch.Positions.Values.Where(x => x.Fake).ToArray())
				{
					if (pos.TradeType == ETradeType.B2Q && pos.Pair.QuoteToBase(pos.VolumeQuote).PriceQ2B > pos.Price * 1.00000000000001m)
						pos.FillFakeOrder();
					if (pos.TradeType == ETradeType.Q2B && pos.Pair.BaseToQuote(pos.VolumeBase).PriceQ2B < pos.Price * 0.999999999999999m)
						pos.FillFakeOrder();
				}
			}
		}

		/// <summary>Remove the fake cash from all exchange balances</summary>
		private void ResetFakeCash()
		{
			foreach (var bal in Exchanges.SelectMany(x => x.Balance.Values))
				bal.FakeCash.Clear();
		}
	}
}
