using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class Model :IDisposable
	{
		static Model()
		{
			Log = new Logger("CF", new LogToFile(Misc.ResolveUserPath("Logs\\log.txt"), append: false));
			Log.TimeZero = Log.TimeZero - Log.TimeZero.TimeOfDay;
			Log.Write(ELogLevel.Debug, "<<< Started >>>");

			MarketUpdates = new BlockingCollection<Action>();
		}
		public Model()
		{
			try
			{
				Shutdown = new CancellationTokenSource();
				Exchanges = new ExchangeContainer();

				// Run these steps after construction is complete
				Misc.RunOnMainThread(() =>
				{
					//// Create the funds given in the settings
					//CreateFundsFromSettings();

					//// Create Bots listed in the settings
					//CreateBotsFromSettings();

					// Enable settings auto save after everything is up and running
					SettingsData.Settings.AutoSaveOnChanges = true;
				});
			}
			catch
			{
				Shutdown?.Cancel();
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			User = null;
			Shutdown = null;
			Util.Dispose(Log);
		}

		/// <summary>Logging</summary>
		public static Logger Log { get; }

		/// <summary>The current time. This might be in the past during a simulation</summary>
		public static DateTimeOffset UtcNow { get; private set; }

		/// <summary>True if live trading</summary>
		public static bool AllowTrades { get; private set; }

		/// <summary>True if back testing is enabled</summary>
		public static bool BackTesting { get; private set; }

		/// <summary>The logged on user</summary>
		public User User
		{
			get { return m_user; }
			set
			{
				if (m_user == value) return;
				if (m_user != null)
				{
					// Drop connections to exchanges
					Util.DisposeAll(Exchanges);
					Exchanges.Clear();
				}
				m_user = value;
				if (m_user != null)
				{
					// Ensure the keys file exists
					if (!Path_.FileExists(m_user.KeysFilepath))
						m_user.NewKeys();

					// On a new user, reconnect to all of the exchanges
					string apikey, secret;
					if (m_user.GetKeys(nameof(Poloniex), out apikey, out secret) == User.EResult.Success)
						Exchanges.Add(new Poloniex(apikey, secret, Shutdown.Token));
					//if (LoadAPIKeys(user, nameof(Bitfinex), out key, out secret)) Exchanges.Add(new Bitfinex(this, key, secret));
					//if (LoadAPIKeys(user, nameof(Bittrex), out key, out secret)) Exchanges.Add(new Bittrex(this, key, secret));
					//if (LoadAPIKeys(user, nameof(Cryptopia), out key, out secret)) Exchanges.Add(new Cryptopia(this, key, secret));
					Exchanges.Insert2(0, new CrossExchange(Exchanges, Shutdown.Token));

					// Save the last user name
					SettingsData.Settings.LastUser = m_user.Name;
				}
			}
		}
		private User m_user;

		/// <summary>A cancellation token for graceful shutdown</summary>
		public CancellationTokenSource Shutdown
		{
			[DebuggerStepThrough]
			get { return m_shutdown; }
			private set
			{
				if (m_shutdown == value) return;
				if (m_shutdown != null && !m_shutdown.IsCancellationRequested)
					throw new Exception("Shouldn't dispose a cancellation token before cancelling it");

				Util.Dispose(ref m_shutdown);
				m_shutdown = value;
			}
		}
		private CancellationTokenSource m_shutdown;

		/// <summary>The supported exchanges</summary>
		public ExchangeContainer Exchanges { get; }

		/// <summary>The special case cross exchange</summary>
		public CrossExchange CrossExchange => Exchanges.OfType<CrossExchange>().FirstOrDefault();

		/// <summary>Pending market data updates awaiting integration at the right time</summary>
		public static BlockingCollection<Action> MarketUpdates { get; }
	}
}
