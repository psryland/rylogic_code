using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.common;
using pr.container;
using pr.extn;
using pr.util;
using Tradee;

namespace Tradee
{
	public partial class TradeeBotModel :IDisposable
	{

		public TradeeBotModel(Robot calgo, Settings settings)
		{
			CAlgo = calgo;
			Settings = settings;
			m_main_thread = Dispatcher.CurrentDispatcher;
			m_sym_cache = new Cache<string, Symbol> { ThreadSafe = true , Mode = CacheMode.StandardCache };

			// Connect to 'Tradee'
			Tradee = new TradeeProxy(DispatchMsg);

			// The collection of transmitters
			Transmitters = new BindingSource<Transmitter> { DataSource = new BindingListEx<Transmitter>() };
			Transmitters.AddRange(Settings.Transmitters.Select(trans => new Transmitter(this, trans.SymbolCode, trans)));
			Transmitters.Sort(BySymbolCode);

			// Initiate the connection by sending account data
			SendAccountStatus();
		}
		public virtual void Dispose()
		{
			// Don't save the removed transmitters
			Settings.AutoSaveOnChanges = false;

			// Shutdown all transmitters
			Util.DisposeAll(Transmitters);

			// Kill the connection to tradee
			Tradee = null;

			Util.Dispose(ref m_sym_cache);
			GC.SuppressFinalize(this);
		}

		/// <summary>The cAlgo connection</summary>
		public Robot CAlgo { get; private set; }

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The connection to the Tradee application</summary>
		private TradeeProxy Tradee
		{
			[DebuggerStepThrough] get { return m_tradee; }
			set
			{
				if (m_tradee == value) return;
				if (m_tradee != null)
				{
					m_tradee.ConnectionChanged -= HandleConnectionChanged;
					m_tradee.Posted -= HandleDataPosted;
					Util.Dispose(ref m_tradee);
				}
				m_tradee = value;
				if (m_tradee != null)
				{
					m_tradee.Posted += HandleDataPosted;
					m_tradee.ConnectionChanged += HandleConnectionChanged;
				}
			}
		}
		private TradeeProxy m_tradee;

		/// <summary>Worker threads for transmitting symbol data to Tradee</summary>
		public BindingSource<Transmitter> Transmitters
		{
			[DebuggerStepThrough] get { return m_bs_transmitters; }
			private set
			{
				if (m_bs_transmitters == value) return;
				if (m_bs_transmitters != null)
				{
					m_bs_transmitters.ListChanging -= HandleTransmitterListChanging;
				}
				m_bs_transmitters = value;
				if (m_bs_transmitters != null)
				{
					m_bs_transmitters.ListChanging += HandleTransmitterListChanging;
				}
			}
		}
		private BindingSource<Transmitter> m_bs_transmitters;

		/// <summary>Raised when data is sent to Tradee</summary>
		public event EventHandler DataPosted;

		/// <summary>Step each of the transmitters</summary>
		public void Step()
		{
			// Send account information
			SendAccountStatus();
			SendCurrentPositions();
			SendPendingPositions();

			// Send price data
			foreach (var trans in Transmitters.Where(x => x.Enabled))
			{
				try
				{
					trans.Step();
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}
			}

			// Remove dead transmitters
			Transmitters.RemoveIf(x => x.Invalid);
		}

		/// <summary>Return the transmitter for 'pair' if it exists, otherwise null</summary>
		public Transmitter FindTransmitter(string sym)
		{
			// If the pair is already in the list, set it as the current pair
			var idx = Transmitters.IndexOf(x => x.SymbolCode == sym);
			return idx != -1 ? Transmitters[idx] : null;
		}

		/// <summary>Add a new transmitter (unless it already exists)</summary>
		public Transmitter AddTransmitter(string sym)
		{
			// If the pair is already in the list, set it as the current pair
			var trans = FindTransmitter(sym);
			if (trans != null)
			{
				Transmitters.Current = trans;
				return trans;
			}

			// Look for settings for this pair
			var s_idx = Settings.Transmitters.IndexOf(x => x.SymbolCode == sym);
			if (s_idx == -1)
			{
				// If no settings exist, create some
				var list = Settings.Transmitters.ToList();
				list.Add(new TransmitterSettings(sym) { TimeFrames = Settings.DefaultTimeFrames });
				Settings.Transmitters = list.ToArray();
				s_idx = list.Count - 1;
			}

			trans = Transmitters.Add2(new Transmitter(this, sym, Settings.Transmitters[s_idx]));
			Transmitters.Sort(BySymbolCode);
			return trans;
		}

		/// <summary>Get the latest account information</summary>
		public IAccount GetAccount()
		{
			return CAlgo.Account;
		}

		/// <summary>Return the symbol for a given code</summary>
		public Symbol GetSymbol(string code)
		{
			return m_sym_cache.Get(code, c =>
			{
				Symbol res = null;
				using (var wait = new ManualResetEvent(false))
				{
					CAlgo.BeginInvokeOnMainThread(() =>
					{
						res = CAlgo.MarketData.GetSymbol(code);
						wait.Set();
					});
					wait.WaitOne();
					return res;
				}
			});
		}
		private Cache<string,Symbol> m_sym_cache;

		/// <summary>Return the series for a given symbol and time frame</summary>
		public MarketSeries GetSeries(Symbol sym, TimeFrame tf)
		{
			MarketSeries res = null;
			using (var wait = new ManualResetEvent(false))
			{
				CAlgo.BeginInvokeOnMainThread(() =>
				{
					res = CAlgo.MarketData.GetSeries(sym, tf);
					wait.Set();
				});
				wait.WaitOne();
				return res;
			}
		}

		/// <summary>Return the account trade history</summary>
		public History GetHistory()
		{
			History res = null;
			using (var wait = new ManualResetEvent(false))
			{
				CAlgo.BeginInvokeOnMainThread(() =>
				{
					res = CAlgo.History;
					wait.Set();
				});
				wait.WaitOne();
				return res;
			}
		}

		/// <summary>True if the pipe is connected</summary>
		public bool IsConnected
		{
			get { return Tradee.IsConnected; }
		}

		/// <summary>Sort Predicate for sorting the transmitter list</summary>
		public Cmp<Transmitter> BySymbolCode
		{
			get { return Cmp<Transmitter>.From((l,r) => l.SymbolCode.CompareTo(r.SymbolCode)); }
		}

		/// <summary>Raised when the pipe connection is established/broken</summary>
		public event EventHandler ConnectionChanged;
		private void HandleConnectionChanged(object sender, EventArgs e)
		{
			RunOnMainThread(() => ConnectionChanged.Raise(this));
		}

		/// <summary>Called when data is sent to Tradee</summary>
		private void HandleDataPosted(object sender, EventArgs e)
		{
			DataPosted.Raise(sender, e);
		}

		/// <summary>Handle changes to the list of transmitters</summary>
		private void HandleTransmitterListChanging(object sender, ListChgEventArgs<Transmitter> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.ItemRemoved:
				{
					// Remove the settings for this transmitter
					var list = Settings.Transmitters.ToList();
					list.Remove(e.Item.Settings);
					Settings.Transmitters = list.ToArray();

					// Dispose the transmitter
					e.Item.Dispose();
					break;
				}
			}
		}

		/// <summary>Marshal a function call to the main thread</summary>
		public void RunOnMainThread(Action action)
		{
			if (m_main_thread.Thread == Thread.CurrentThread)
				action();
			else
				m_main_thread.BeginInvoke(action);
		}
		public void RunOnMainThread(Action<object> action, object arg)
		{
			if (m_main_thread.Thread == Thread.CurrentThread)
				action(arg);
			else
				m_main_thread.BeginInvoke(action, arg);
		}
		private Dispatcher m_main_thread;

		/// <summary>Send a message to Tradee</summary>
		public bool Post<T>(T msg) where T:ITradeeMsg
		{
			return Tradee != null && Tradee.Post(msg);
		}

		/// <summary>Return a scope that measures time until disposed</summary>
		public Scope TimingScope(string msg)
		{
			var sw = new Stopwatch();
			return Scope.Create(
				() => sw.Start(),
				() =>
				{
					sw.Stop();
					Debug.WriteLine("{0}: {1}".Fmt(msg, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds).ToPrettyString()));
				});
		}
	}
}
