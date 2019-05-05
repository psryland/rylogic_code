using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace EDTradeAdvisor
{
	public class EDJournalMonitor : IDisposable
	{
		// Notes:
		//  - Monitors the journal files in the ED user data directory
		private const string JournalRegex = @"Journal\..*\.log";

		public EDJournalMonitor()
		{
			Location = new CurrentLocation();
		}
		public void Dispose()
		{
			Run = false;
		}

		/// <summary>Start/Stop the journal monitoring</summary>
		public bool Run
		{
			get { return m_watcher != null; }
			set
			{
				if (Run == value) return;
				if (Run)
				{
					m_watcher.Created -= HandleCreated;
					m_watcher.Changed -= HandleChanged;
					m_watcher.Deleted -= HandleDeleted;
					Util.Dispose(ref m_watcher);
				}
				m_watcher = value ? new FileSystemWatcher(Settings.Instance.JournalFilesDir) { EnableRaisingEvents = false } : null;
				if (Run)
				{
					m_watcher.Deleted += HandleDeleted;
					m_watcher.Changed += HandleChanged;
					m_watcher.Created += HandleCreated;

					// Find the latest journal file in the directory
					var latest_path = Path_.EnumFileSystem(Settings.Instance.JournalFilesDir, SearchOption.TopDirectoryOnly, JournalRegex).MaxByOrDefault(x => x.Name);
					if (latest_path != null)
					{
						JournalFilename = latest_path.FullName;
						JournalOffset = Parse(JournalFilename, 0L);
					}

					m_watcher.EnableRaisingEvents = true;
				}

				// Handlers
				void HandleCreated(object sender, FileSystemEventArgs e)
				{
					if (Regex.IsMatch(e.Name, JournalRegex))
					{
						// When a new journal file is created, parse the remainder of the current one then load the new one.
						if (JournalFilename != null && string.CompareOrdinal(e.Name, JournalFilename) <= 0)
							return;

						// Parse the remainder of the current journal (if there is one)
						if (JournalFilename != null)
							JournalOffset = Parse(JournalFilename, JournalOffset);

						// Read the new journal
						JournalFilename = e.FullPath;
						JournalOffset = Parse(JournalFilename, 0L);
					}
					if (e.Name == "Market.json")
					{
						MarketUpdate?.Invoke(this, new MarketUpdateEventArgs(e.FullPath));
					}
				}
				void HandleChanged(object sender, FileSystemEventArgs e)
				{
					// When the latest journal changes, parse the additional lines
					if (JournalFilename != null && string.CompareOrdinal(e.Name, JournalFilename) == 0)
					{
						JournalOffset = Parse(JournalFilename, JournalOffset);
					}
					if (e.Name == "Market.json")
					{
						MarketUpdate?.Invoke(this, new MarketUpdateEventArgs(e.FullPath));
					}
				}
				void HandleDeleted(object sender, FileSystemEventArgs e)
				{
					// If the latest journal gets deleted, remove our reference to it
					if (JournalFilename != null && string.CompareOrdinal(e.Name, JournalFilename) == 0)
					{
						JournalFilename = null;
						JournalOffset = 0L;
					}
				}
			}
		}
		private FileSystemWatcher m_watcher;

		/// <summary>The current player location</summary>
		public CurrentLocation Location { get; }
		public event EventHandler LocationChanged;

		/// <summary>The player's ship cargo space</summary>
		public int CargoCapacity { get; private set; }
		public event EventHandler CargoCapacityChanged;

		/// <summary>The max jump range (unladen)</summary>
		public double MaxJumpRange { get; private set; }
		public event EventHandler MaxJumpRangeChanged;

		/// <summary>Raised when the Market.json file is created or changed</summary>
		public event EventHandler<MarketUpdateEventArgs> MarketUpdate;

		/// <summary>The latest journal name, currently being watched</summary>
		private string JournalFilename
		{
			get { return m_journal_filename; }
			set
			{
				if (m_journal_filename == value) return;
				m_journal_filename = value;
				Advisor.Log.Write(ELogLevel.Info, $"Reading ED Journal file: {value}");
			}
		}
		private string m_journal_filename;

		/// <summary>The offset into the journal that has been successfully parsed</summary>
		private long JournalOffset { get; set; }

		/// <summary>Parse events in a journal file, reading from 'fileofs'</summary>
		private long Parse(string filepath, long fileofs)
		{
			using (var file = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) { Position = fileofs })
			using (var sr = new StreamReader(file))
			{
				// Seek backward to the start of the line
				for (; sr.BaseStream.Position != 0 && sr.Peek() != '\n'; --sr.BaseStream.Position) { }
				if (sr.BaseStream.Position != 0)
					for (var ch = sr.Read(); ch == '\n' || ch == '\r'; ch = sr.Read()) { }

				// Parse events from the journal
				for (; !sr.EndOfStream;)
				{
					try
					{
						// Read a full line from the journal
						var line = sr.ReadLine();
						var jobj = JObject.Parse(line);
						fileofs = sr.Position();

						// Parse the line as Json
						var evt = jobj["event"].Value<string>();
						switch (evt)
						{
						case "Location":
							ParseLocation(jobj);
							break;
						case "SupercruiseEntry":
							ParseSupercruiseEntry(jobj);
							break;
						case "SuperCruiseExit":
							ParseSuperCruiseExit(jobj);
							break;
						case "Docked":
							ParseDocked(jobj);
							break;
						case "Undocked":
							ParseUndocked(jobj);
							break;
						case "Loadout":
							ParseLoadout(jobj);
							break;
						}
					}
					catch (Exception ex)
					{
						// Assume reader exceptions mean partial line reads
						if (!(ex is JsonReaderException)) Advisor.Log.Write(ELogLevel.Error, ex, $"Error reading journal file: {JournalFilename}");
						break;
					}
				}
				return fileofs;
			}
		}

		/// <summary>Parse a journal location event</summary>
		private void ParseLocation(JObject jobj)
		{
			Location.StarSystem = jobj["StarSystem"].Value<string>();
			Location.Station = jobj["StationName"].Value<string>();
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Parse a journal docked event</summary>
		private void ParseDocked(JObject jobj)
		{
			Location.StarSystem = jobj["StarSystem"].Value<string>();
			Location.Station = jobj["StationName"].Value<string>();
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary></summary>
		private void ParseUndocked(JObject jobj)
		{
			Location.Station = null;
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Parse a journal super cruise entry event</summary>
		private void ParseSupercruiseEntry(JObject jobj)
		{
			Location.StarSystem = null;
			Location.Station = null;
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Parse a journal super cruise exit event</summary>
		private void ParseSuperCruiseExit(JObject jobj)
		{
			Location.StarSystem = jobj["StarSystem"].Value<string>();
			Location.Station = null;
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Parse a journal load out event</summary>
		private void ParseLoadout(JObject jobj)
		{
			CargoCapacity = jobj["CargoCapacity"].Value<int>();
			CargoCapacityChanged?.Invoke(this, EventArgs.Empty);

			MaxJumpRange = jobj["MaxJumpRange"].Value<double>();
			MaxJumpRangeChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Current player location</summary>
		public class CurrentLocation
		{
			public CurrentLocation(string system = null, string station = null)
			{
				StarSystem = system;
				Station = station;
			}

			/// <summary>The system that the player is currently in</summary>
			public string StarSystem { get; set; }

			/// <summary>The station that the player is currently in</summary>
			public string Station { get; set; }
		}
	}

	#region EventArgs
	public class MarketUpdateEventArgs
	{
		public MarketUpdateEventArgs(string market_data_filepath)
		{
			MarketDataFilepath = market_data_filepath;
		}

		public string MarketDataFilepath { get; }
	}
	#endregion
}
