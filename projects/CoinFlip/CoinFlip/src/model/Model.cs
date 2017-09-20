using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public partial class Model :IDisposable, IShutdownAsync
	{
		// Notes:
		//  - The model builds the loops collection whenever new pairs are added.
		//  - Each exchange updates its data independently.
		//  - Each exchange owns the coins and pairs it provides.
		//  - The CrossExchange is a special exchange that links coins of the same currency
		//    between exchanges.
		//
		// The Model main loop (heartbeat) does the following:
		//    If the coins of interest have changed:
		//        For each exchange (in parallel):
		//            Update pairs, coins
		//        Copy pairs for COI to Model
		//        Update collection of Loops
		//    For each loop (in parallel):
		//        Test for profitability (pairs, coins, balances in the Model are read only)
		//    Execute most profitable loop (if any)
		//
		// Exchange main loop (heartbeat):
		//    Query server
		//    Collect data in staging buffers
		//    lock exchange
		//    Copy staged data to Exchange data
		//

		public Model(MainUI main_ui, Settings settings, User user)
		{
			UI = main_ui;
			Settings = settings;
			User = user;
			m_dispatcher = Dispatcher.CurrentDispatcher;
			m_main_loop_step = new AutoResetEvent(false);
			m_shutdown_token_source = new CancellationTokenSource();

			// Start the log
			Log = new Logger(string.Empty, new LogToFile(Misc.ResolveUserPath(Util.IsDebug ? "log_debug.txt" : "log.txt"), append:false));
			Log.TimeZero = Log.TimeZero - Log.TimeZero .TimeOfDay;
			Log.Write(ELogLevel.Debug, "<<< Started >>>");

			// Start the win log
			WinLog = new Logger(string.Empty, new LogToFile(Misc.ResolveUserPath("win_log.txt"), append:true));

			Coins         = new CoinDataTable(this);
			Exchanges     = new BindingSource<Exchange>     { DataSource = new BindingListEx<Exchange>() };
			Pairs         = new BindingSource<TradePair>    { DataSource = new BindingListEx<TradePair>() };
			Loops         = new BindingSource<Loop>         { DataSource = new BindingListEx<Loop>() };
			Fishing       = new BindingSource<Fishing>      { DataSource = new BindingListEx<Fishing>(Settings.Fishing.Select(x => new Fishing(this, x)).ToList()) };
			Balances      = new BindingSource<Balance>      { DataSource = null, AllowNoCurrent = true };
			Positions     = new BindingSource<Position>     { DataSource = null, AllowNoCurrent = true };
			History       = new BindingSource<PositionFill> { DataSource = null, AllowNoCurrent = true };
			MarketUpdates = new BlockingCollection<Action>();

			// Add exchanges
			string key, secret;
			if (LoadAPIKeys(user, nameof(Poloniex), out key, out secret))
				Exchanges.Add(new Poloniex(this, key, secret));
			if (LoadAPIKeys(user, nameof(Bittrex), out key, out secret))
				Exchanges.Add(new Bittrex(this, key, secret));
			if (LoadAPIKeys(user, nameof(Cryptopia), out key, out secret))
				Exchanges.Add(new Cryptopia(this, key, secret));
			Exchanges.Add(CrossExchange = new CrossExchange(this));
			//Exchanges.Add(new TestExchange(this));

			RunLoopFinder = false;
			AllowTrades = false;

			// Run the async main loop
			MainLoop(ShutdownToken);
		}
		public virtual void Dispose()
		{
			// Main loops needs to have shutdown before here
			if (!ShutdownToken.IsCancellationRequested)
				throw new Exception("Disposing before ShutdownAsync has completed");
			if (Running)
				throw new Exception("Disposing while process loops are still running");

			Loops = null;
			Fishing = null;
			Positions = null;
			History = null;
			Balances = null;
			Pairs = null;
			Exchanges = null;
			WinLog = null;
			Log = null;
		}

		/// <summary>Async shutdown</summary>
		public async Task ShutdownAsync()
		{
			// Signal shutdown
			m_shutdown_token_source.Cancel();

			// Shutdown fishing instances
			await Task.WhenAll(Fishing.Select(x => x.ShutdownAsync()));

			// Wait for async methods to exit
			await Task_.WaitWhile(() => Running);
		}

		/// <summary>A cancellation token for graceful shutdown</summary>
		public CancellationToken ShutdownToken
		{
			get { return m_shutdown_token_source.Token; }
		}
		private CancellationTokenSource m_shutdown_token_source;

		/// <summary>Application main loop
		private async void MainLoop(CancellationToken shutdown)
		{
			using (Scope.Create(() => ++m_main_loop_running, () => --m_main_loop_running))
			{
				// Infinite loop till shutdown
				for (;;)
				{
					try
					{
						await Task.Run(() => m_main_loop_step.WaitOne(Settings.MainLoopPeriod), shutdown);
						if (shutdown.IsCancellationRequested) break;

						// Update the trading pairs
						await UpdatePairsAsync(shutdown);

						// Process any pending market data updates
						IntegrateMarketUpdates();

						// Do trading tasks
						using (Scope.Create(() => MarketDataLocked = true, () => MarketDataLocked = false))
						{
							// Test the loops for profitability, and execute the best
							if (RunLoopFinder)
								await ExecuteProfitableLoops(shutdown);
						}

						// Simulate fake orders being filled
						if (!AllowTrades)
							await SimulateFakeOrders();
					}
					catch (Exception ex)
					{
						if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
						if (ex is OperationCanceledException) { break; }
						else Log.Write(ELogLevel.Error, ex, "Error during main loop.");
					}
				}
			}
		}
		private AutoResetEvent m_main_loop_step;
		private int m_main_loop_running;

		/// <summary>True if the Model can be closed gracefully</summary>
		public bool Running
		{
			get { return m_main_loop_running != 0; }
		}

		/// <summary>The main UI</summary>
		public MainUI UI { get; private set; }

		/// <summary>App settings</summary>
		public Settings Settings { get; private set; }

		/// <summary>The logged on user</summary>
		public User User { get; private set; }

		/// <summary>The current time (in UTC).</summary>
		public DateTimeOffset UtcNow
		{
			get { return DateTimeOffset.UtcNow; }
		}

		/// <summary>Application log</summary>
		public Logger Log
		{
			[DebuggerStepThrough] get { return m_log; }
			private set
			{
				if (m_log == value) return;
				Util.Dispose(ref m_log);
				m_log = value;
			}
		}
		private Logger m_log;

		/// <summary>Log for profitable trades</summary>
		public Logger WinLog
		{
			[DebuggerStepThrough] get { return m_win_log; }
			private set
			{
				if (m_win_log == value) return;
				Util.Dispose(ref m_win_log);
				m_win_log = value;
			}
		}
		private Logger m_win_log;

		/// <summary>True when pairs need updating</summary>
		public bool UpdatePairs
		{
			get { return m_update_pairs; }
			set
			{
				m_update_pairs = value;
				if (m_update_pairs)
				{
					++m_update_pairs_issue;
					RebuildLoops = true;
					m_main_loop_step.Set();
				}
			}
		}
		private bool m_update_pairs;
		private int m_update_pairs_issue;

		/// <summary>True when the set of loops needs regenerating</summary>
		public bool RebuildLoops
		{
			get { return m_rebuild_loops; }
			set
			{
				m_rebuild_loops = value;
				if (m_rebuild_loops)
				{
					++m_rebuild_loops_issue;
					m_main_loop_step.Set();
				}
			}
		}
		private bool m_rebuild_loops;
		private int m_rebuild_loops_issue;

		/// <summary>A global switch to control actually placing orders</summary>
		public bool AllowTrades
		{
			get { return m_allow_trades; }
			set
			{
				if (m_allow_trades == value) return;
				if (BackTesting) return;

				if (m_allow_trades)
				{
					AllowTradesChanging.Raise(this, new PrePostEventArgs(after:false));
				}
				m_allow_trades = value;
				ResetFakeCash();
				if (m_allow_trades)
				{
					AllowTradesChanging.Raise(this, new PrePostEventArgs(after:true));
				}
			}
		}
		public event EventHandler<PrePostEventArgs> AllowTradesChanging;
		private bool m_allow_trades;

		/// <summary>True while back testing</summary>
		public bool BackTesting
		{
			get { return m_back_testing; }
			set
			{
				if (m_back_testing == value) return;
				if (AllowTrades) return;
				m_back_testing = value;
			}
		}
		private bool m_back_testing;

		/// <summary>Start/Stop finding loops</summary>
		public bool RunLoopFinder
		{
			get { return m_run_loop_finder; }
			set
			{
				if (m_run_loop_finder == value) return;
				m_run_loop_finder = value;
				RunChanged.Raise(this);
			}
		}
		public event EventHandler RunChanged;
		private bool m_run_loop_finder;

		/// <summary>Return the sum of all balances, weighted by their values</summary>
		public decimal NettWorth
		{
			get
			{
				var worth = 0m;
				foreach (var exch in TradingExchanges)
				{
					foreach (var bal in exch.Balance.Values)
						worth += bal.Coin.Value(bal.Total);
				}
				return worth;
			}
		}

		/// <summary>Return the sum of all tokens</summary>
		public decimal TokenTotal
		{
			get
			{
				var sum = 0m;
				foreach (var exch in TradingExchanges)
				{
					foreach (var bal in exch.Balance.Values)
					{
						var amount = bal.Total;
						sum += amount;
					}
				}
				return sum;
			}
		}

		/// <summary>Return the sum across all exchanges of the total coin 'sym'</summary>
		public decimal SumOfTotal(string sym)
		{
			var sum = 0m;
			foreach (var exch in TradingExchanges)
			{
				var coin = exch.Coins[sym];
				sum += coin?.Balance.Total ?? 0;
			}
			return sum;
		}

		/// <summary>Return the sum across all exchanges of the available coin 'sym'</summary>
		public decimal SumOfAvailable(string sym)
		{
			var sum = 0m;
			foreach (var exch in TradingExchanges)
			{
				var coin = exch.Coins[sym];
				sum += coin?.Balance.Available ?? 0;
			}
			return sum;
		}

		/// <summary>"Real" exchanges</summary>
		public IEnumerable<Exchange> TradingExchanges
		{
			get { return Exchanges.Except(CrossExchange); }
		}

		/// <summary>The special case cross exchange</summary>
		public CrossExchange CrossExchange { get; private set; }

		/// <summary>Meta data for the known coins</summary>
		public CoinDataTable Coins { [DebuggerStepThrough] get; private set; }
		public class CoinDataTable :BindingSource<Settings.CoinData>
		{
			private readonly Model Model;
			public CoinDataTable(Model model)
			{
				Model = model;
				DataSource = new BindingListEx<Settings.CoinData>(Model.Settings.Coins.ToList());
				Model.UpdatePairs = true;
			}
			public Settings.CoinData this[string sym]
			{
				get
				{
					var idx = this.IndexOf(x => x.Symbol == sym);
					return idx >= 0 ? this[idx] : this.Add2(new Settings.CoinData(sym, 1m));
				}
			}
			protected override void OnListChanging(object sender, ListChgEventArgs<Settings.CoinData> args)
			{
				if (args.IsDataChanged)
				{
					// Record the coins in the settings
					Model.Settings.Coins = this.ToArray();

					// Flag that the COI have changed, we'll need to update pairs.
					Model.UpdatePairs = true;
				}
				base.OnListChanging(sender, args);
			}
		}

		/// <summary>The exchanges</summary>
		public BindingSource<Exchange> Exchanges
		{
			[DebuggerStepThrough] get { return m_exchanges; }
			private set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.ListChanging -= HandleExchangesListChanging;
					m_exchanges.PositionChanged -= HandleCurrentExchangeChanged;
					Util.DisposeAll(ref m_exchanges);
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.PositionChanged += HandleCurrentExchangeChanged;
					m_exchanges.ListChanging += HandleExchangesListChanging;
				}
			}
		}
		private BindingSource<Exchange> m_exchanges;
		private void HandleExchangesListChanging(object sender, ListChgEventArgs<Exchange> e)
		{
			if (e.ChangeType == ListChg.ItemAdded)
				Log.Write(ELogLevel.Info, "Exchange Added: {0}".Fmt(e.Item.Name));
			if (e.ChangeType == ListChg.ItemRemoved)
				Log.Write(ELogLevel.Info, "Exchange Removed: {0}".Fmt(e.Item.Name));

			UpdatePairs = true;
		
			// Update the bindings to the current exchange
			if (e.Item == Exchanges.Current)
				UpdateExchangeDetails();
		}
		private void HandleCurrentExchangeChanged(object sender = null, PositionChgEventArgs e = null)
		{
			UpdateExchangeDetails();
		}

		/// <summary>The trade pairs associated with the coins of interest. Note: the model does not own the pairs, the exchanges do</summary>
		public BindingSource<TradePair> Pairs
		{
			[DebuggerStepThrough] get { return m_pairs; }
			private set
			{
				if (m_pairs == value) return;
				if (m_pairs != null)
				{
					m_pairs.ListChanging -= HandlePairsListChanging;
				}
				m_pairs = value;
				if (m_pairs != null)
				{
					m_pairs.ListChanging += HandlePairsListChanging;
				}
			}
		}
		private BindingSource<TradePair> m_pairs;
		private void HandlePairsListChanging(object sender, ListChgEventArgs<TradePair> e)
		{
			// Ensure the list is unique
			Debug.Assert(e.ChangeType != ListChg.ItemAdded || Pairs.Count(x => x == e.Item) == 1);
		}

		/// <summary>The balances on the current exchange</summary>
		public BindingSource<Balance> Balances
		{
			get { return m_balances; }
			private set
			{
				if (m_balances == value) return;
				m_balances = value;
				if (m_balances != null)
				{
					m_balances.AllowSort = true;
				}
			}
		}
		private BindingSource<Balance> m_balances;

		/// <summary>The positions on the current exchange</summary>
		public BindingSource<Position> Positions
		{
			get { return m_positions; }
			private set
			{
				if (m_positions == value) return;
				m_positions = value;
				if (m_positions != null)
				{
					m_positions.AllowSort = true;
				}
			}
		}
		private BindingSource<Position> m_positions;

		/// <summary>The historic trades on the current exchange</summary>
		public BindingSource<PositionFill> History
		{
			get { return m_history; }
			private set
			{
				if (m_history == value) return;
				m_history = value;
				if (m_history != null)
				{
					m_history.AllowSort = true;
				}
			}
		}
		private BindingSource<PositionFill> m_history;

		/// <summary>Fishing instances</summary>
		public BindingSource<Fishing> Fishing
		{
			get { return m_fishing; }
			private set
			{
				if (m_fishing == value) return;
				if (m_fishing != null)
				{
					m_fishing.ListChanging -= HandleFishingListChanging;
					Util.DisposeAll(m_fishing);
				}
				m_fishing = value;
				if (m_fishing != null)
				{
					m_fishing.ListChanging += HandleFishingListChanging;
				}
			}
		}
		private BindingSource<Fishing> m_fishing;
		private void HandleFishingListChanging(object sender, ListChgEventArgs<Fishing> e)
		{
			if (e.IsDataChanged)
				Settings.Fishing = Fishing.Select(x => x.Settings).ToArray();
		}

		/// <summary>Trade pair loops</summary>
		public BindingSource<Loop> Loops
		{
			get { return m_loops; }
			private set
			{
				if (m_loops == value) return;
				m_loops = value;
			}
		}
		private BindingSource<Loop> m_loops;

		/// <summary>Pending market data updates awaiting integration at the right time</summary>
		public BlockingCollection<Action> MarketUpdates { get; private set; }

		/// <summary>Debugging flag for detecting misuse of the market data</summary>
		internal bool MarketDataLocked { get; private set; }

		/// <summary>Process any pending market data updates</summary>
		private void IntegrateMarketUpdates()
		{
			Debug.Assert(AssertMainThread());

			// Notify market data updating
			MarketDataChanging.Raise(this, new MarketDataChangingEventArgs(done:false));

			using (Positions.PreservePosition())
			using (History  .PreservePosition())
			using (Balances .PreservePosition())
			{
				Positions.Position = -1;
				History  .Position = -1;
				Balances .Position = -1;
				for (; MarketUpdates.TryTake(out var update);)
				{
					try
					{
						update();
					}
					catch (Exception ex)
					{
						Log.Write(ELogLevel.Error, ex, "Market data integration error");
					}
				}
			}

			// Notify market data updating
			MarketDataChanging.Raise(this, new MarketDataChangingEventArgs(done:true));
		}

		/// <summary>Raised when market data changes</summary>
		public event EventHandler<MarketDataChangingEventArgs> MarketDataChanging;

		/// <summary>Update the collections of pairs</summary>
		public async Task UpdatePairsAsync(CancellationToken shutdown, Exchange exchange = null)
		{
			var sw = new Stopwatch().Start2();
			for (; UpdatePairs;)
			{
				Log.Write(ELogLevel.Info, "Updating pairs ...");
				var update_pairs_issue = m_update_pairs_issue;
				if (shutdown.IsCancellationRequested) return;

				// Get the coins of interest
				var coi = Coins.Where(x => x.OfInterest).ToHashSet(x => x.Symbol);

				// Get each exchange to update it's available pairs/coins
				if (exchange == null)
					await Task.WhenAll(TradingExchanges.Where(x => x.Active).Select(x => x.UpdatePairs(coi)));
				else
					await exchange.UpdatePairs(coi);

				// Update the cross exchange when other exchanges change
				if (CrossExchange.Active)
					await CrossExchange.UpdatePairs(coi);

				// Apply any market data updates
				IntegrateMarketUpdates();

				// If the pairs have been invalidated in the meantime, give up
				if (update_pairs_issue != m_update_pairs_issue || shutdown.IsCancellationRequested)
					continue;

				// Update the Model's collection of pairs
				var pairs = new HashSet<TradePair>();
				foreach (var exch in Exchanges.Where(x => x.Active))
					pairs.AddRange(exch.Pairs.Values.Where(x => coi.Contains(x.Base) && coi.Contains(x.Quote)));

				Pairs.RemoveIf(x => !pairs.Contains(x));
				Pairs.ForEach(x => pairs.Remove(x));
				Pairs.AddRange(pairs);

				// Clear the dirty flag
				UpdatePairs = false;
				Log.Write(ELogLevel.Info, $"Trading pairs updated ... (Taking {sw.Elapsed.TotalSeconds} seconds)");
			}
		}

		/// <summary>Look for fake orders that would be filled by the current price levels</summary>
		private async Task SimulateFakeOrders()
		{
			foreach (var exch in Exchanges)
			{
				foreach (var pos in exch.Positions.Values.Where(x => x.Fake).ToArray())
				{
					if (pos.TradeType == ETradeType.B2Q && pos.Pair.QuoteToBase(pos.VolumeQuote).PriceQ2B > pos.Price * 1.00000000000001m)
						await pos.FillFakeOrder();
					if (pos.TradeType == ETradeType.Q2B && pos.Pair.BaseToQuote(pos.VolumeBase).PriceQ2B < pos.Price * 0.999999999999999m)
						await pos.FillFakeOrder();
				}
			}
		}

		/// <summary>Remove the fake cash from all exchange balances</summary>
		private void ResetFakeCash()
		{
			foreach (var bal in Exchanges.SelectMany(x => x.Balance.Values))
				bal.FakeCash.Clear();
		}

		/// <summary>True if we can get a live price for 'sym' from at least one exchange</summary>
		public bool LivePriceAvailable(string sym)
		{
			var available = false;
			foreach (var exch in TradingExchanges)
			{
				var coin = exch.Coins[sym];
				if (coin == null) continue;
				if (!coin.LivePriceAvailable) continue;
				available = true;
				break;
			}
			return available;
		}

		/// <summary>Find the maximum price for the given currency on the available exchanges</summary>
		public decimal MaxLiveValue(string sym)
		{
			var value = 0m._(sym);
			foreach (var exch in TradingExchanges)
			{
				var coin = exch.Coins[sym];
				if (coin == null) continue;
				value = Math.Max(value, coin.Value(1m._(sym)));
			}
			return value;
		}

		/// <summary>Execute 'action' in the GUI thread context</summary>
		public void RunOnGuiThread(Action action, bool block = false)
		{
			if (block)
				m_dispatcher.Invoke(action);
			else
				m_dispatcher.BeginInvoke(action);
		}
		private Dispatcher m_dispatcher;

		/// <summary>Update the data sources for the exchange specific data</summary>
		private void UpdateExchangeDetails()
		{
			var exch = Exchanges.Current;
			Balances.DataSource = exch?.Balance;
			Positions.DataSource = exch?.Positions;
			History.DataSource = exch?.History;
		}

		/// <summary>Return the key/secret for this exchange</summary>
		private bool LoadAPIKeys(User user, string exch, out string key, out string secret)
		{
			var crypto = new AesCryptoServiceProvider();

			// Get the key file name. This is an encrypted XML file
			var key_file = Misc.ResolveUserPath($"{user.Username}.keys");

			// If there is no keys file, or the keys file does not contain API keys for this exchange, prompt for them
			if (!Path_.FileExists(key_file))
				return CreateAPIKeys(user, exch, out key, out secret);

			// Read the file contents to memory and decrypt it
			string keys_xml;
			var decryptor = crypto.CreateDecryptor(user.Cred, InitVector(user, crypto.BlockSize));
			using (var fs = new FileStream(key_file, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
			using (var sr = new StreamReader(cs))
				keys_xml = sr.ReadToEnd();

			// Find the element for this exchange
			var root = XDocument.Parse(keys_xml, LoadOptions.None).Root;
			var exch_xml = root?.Element(exch);
			if (exch_xml == null)
				return CreateAPIKeys(user, exch, out key, out secret);

			// Read the key/secret
			key    = exch_xml.Element(XmlTag.APIKey).As<string>();
			secret = exch_xml.Element(XmlTag.APISecret).As<string>();
			return true;
		}

		/// <summary>Create or replace API keys for this exchange</summary>
		private bool CreateAPIKeys(User user, string exch, out string key, out string secret)
		{
			var crypto = new AesCryptoServiceProvider();
			key    = null;
			secret = null;

			// Prompt for the API keys
			var dlg = new APIKeysUI
			{
				Icon = UI.Icon,
				StartPosition = FormStartPosition.CenterScreen,
				Text = $"{exch} API Keys",
				Desc = $"Enter the API key and secret for your account on {exch}\r\n"+
						$"These will be stored in an encrypted file here:\r\n"+
						$"\"{Misc.ResolveUserPath($"{user.Username}.keys")}\""+
						$"\r\n"+
						$"Changing API Keys will require restarting",
			};
			using (dlg)
			{
				if (dlg.ShowDialog(UI) != DialogResult.OK)
					return false;

				key    = dlg.APIKey;
				secret = dlg.APISecret;

				// Get the key file name. This is an encrypted XML file
				var key_file = Misc.ResolveUserPath($"{user.Username}.keys");

				// If there is an existing keys file, decrypt and the XML content, otherwise create from new
				var root = (XElement)null;
				if (Path_.FileExists(key_file))
				{
					// Read the file contents to memory and decrypt it
					var decryptor = crypto.CreateDecryptor(user.Cred, InitVector(user, crypto.BlockSize));
					using (var fs = new FileStream(key_file, FileMode.Open, FileAccess.Read, FileShare.Read))
					using (var cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
					using (var sr = new StreamReader(cs))
						root = XDocument.Parse(sr.ReadToEnd(), LoadOptions.None).Root;
				}
				if (root == null)
					root = new XElement("root");

				// Add/replace the element for this exchange
				root.RemoveNodes(exch);
				var exch_xml = root.Add2(new XElement(exch));
				exch_xml.Add2(XmlTag.APIKey, key, false);
				exch_xml.Add2(XmlTag.APISecret, secret, false);
				var keys_xml = root.ToString(SaveOptions.None);

				// Write the keys file back to disk (encrypted)
				var encryptor = crypto.CreateEncryptor(user.Cred, InitVector(user, crypto.BlockSize));
				using (var fs = new FileStream(key_file, FileMode.Create, FileAccess.Write, FileShare.None))
				using (var cs = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
				using (var sw = new StreamWriter(cs))
					sw.Write(keys_xml);

				return true;
			}
		}

		/// <summary>Display and/or change the API keys for 'exch'</summary>
		public void ChangeAPIKeys(Exchange exch)
		{
			// Prompt for the user password again
			var user = Misc.LogIn(UI, Settings);
			if (user.Username != User.Username || !Array_.Equal(user.Cred, User.Cred))
			{
				MsgBox.Show(UI, "Invalid username or password", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			// Load the API keys
			if (LoadAPIKeys(User, exch.Name, out var key, out var secret))
			{
				var dlg = new APIKeysUI
				{
					Icon = UI.Icon,
					StartPosition = FormStartPosition.CenterScreen,
					Text = $"{exch} API Keys",
					Desc = $"Enter the API key and secret for your account on {exch}\r\n"+
							$"These will be stored in an encrypted file here:\r\n"+
							$"\"{Misc.ResolveUserPath($"{User.Username}.keys")}\""+
							$"\r\n"+
							$"Changing API Keys will require restarting",
				};
				using (dlg)
				{
					if (dlg.ShowDialog(UI) != DialogResult.OK)
						return;

					// Check if the Keys have changed
					if (dlg.APIKey != key || dlg.APISecret != secret)
					{
						var res = MsgBox.Show(UI,
							"A restart is required to use the new API keys.\r\n"+
							"\r\n"+
							"Restart now?",
							Application.ProductName,
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question);
						if (res == DialogResult.Yes)
							Application.Restart();
					}
				}
			}
		}

		/// <summary>Generate an initialisation vector for the encryption service provider</summary>
		private byte[] InitVector(User user, int size_in_bits)
		{
			var buf = new byte[size_in_bits / 8];
			for (int i = 0; i != buf.Length; ++i)
				buf[i] = user.Cred[(i * 13) % user.Cred.Length];
			return buf;
		}

		/// <summary>Assert that it is valid to read market data in the current thread</summary>
		public bool AssertMarketDataRead()
		{
			if (Thread.CurrentThread.ManagedThreadId == m_dispatcher.Thread.ManagedThreadId) return true;
			if (MarketDataLocked) return true;
			throw new Exception("Invalid access to market data");
		}

		/// <summary>Assert that it is valid to write market data in the current thread</summary>
		public bool AssertMarketDataWrite()
		{
			// Only the main thread can write to the market data
			if (Thread.CurrentThread.ManagedThreadId == m_dispatcher.Thread.ManagedThreadId) return true;
			throw new Exception("Invalid access to market data");
		}

		/// <summary>Assert that the current thread is the main thread</summary>
		public bool AssertMainThread()
		{
			if (Thread.CurrentThread.ManagedThreadId == m_dispatcher.Thread.ManagedThreadId) return true;
			throw new Exception("Cross-thread call detected");
		}

		/// <summary>Debugging test</summary>
		public async void Test()
		{
			try
			{
				var pair = Pairs.FirstOrDefault(x => x.Name == "BTC/USDT");
				using (var dlg = new MsgBox())
				using (var instr = new Instrument(this, pair, ETimeFrame.Min30, update_active:false))
					dlg.ShowDialog(UI);

				await Misc.CompletedTask;
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Test Failed");
			}
		}
	}

	#region EventArgs
	public class MarketDataChangingEventArgs :EventArgs
	{
		public MarketDataChangingEventArgs(bool done)
		{
			Done = done;
		}
		public bool Done { get; private set; }
	}
	#endregion
}