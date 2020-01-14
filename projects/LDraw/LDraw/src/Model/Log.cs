﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace LDraw
{
	public class Log
	{
		private static long m_index;
		private static object m_lock;
		private static Dispatcher m_dispatcher;
		private static DateTimeOffset m_tzero;
		static Log()
		{
			m_lock = new object();
			m_dispatcher = Dispatcher.CurrentDispatcher;
			Entries = new ObservableCollection<LogControl.LogEntry>();
			Highlighting = new ObservableCollection<LogControl.HLPattern>();
			m_tzero = DateTimeOffset.Now;
			m_tzero -= m_tzero.TimeOfDay;

			/// <summary>Single entry highlights</summary>
			//public static readonly HLPattern[] LogHighlighting = new[]
			//{
			//	new HLPattern(Color_.FromArgb(0xffb5ffd2), Color.Black, EPattern.Substring, "Fishing started"),
			//	new HLPattern(Color_.FromArgb(0xffb5ffd2), Color.Black, EPattern.Substring, "Fishing stopped"),
			//	new HLPattern(Color_.FromArgb(0xffe7a5ff), Color.Black, EPattern.Substring, "!Profit!"),
			//	new HLPattern(Color_.FromArgb(0xfffcffae), Color.Black, EPattern.Substring, "filled"),
			//	new HLPattern(Color_.FromArgb(0xffff7e39), Color.Black, EPattern.Substring, "ignored"),
			//};
		}

		/// <summary>Raised when new log entries are added</summary>
		public static event EventHandler? EntriesChanged;

		/// <summary>Log messages</summary>
		public static ObservableCollection<LogControl.LogEntry> Entries
		{
			get => m_entries;
			private set
			{
				if (m_entries == value) return;
				if (m_entries != null)
				{
					m_entries.CollectionChanged -= HandleOutputEntriesChanged;
				}
				m_entries = value ?? new ObservableCollection<LogControl.LogEntry>();
				if (m_entries != null)
				{
					m_entries.CollectionChanged += HandleOutputEntriesChanged;
				}

				// Handlers
				void HandleOutputEntriesChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					if (!m_update_signalled && (
						e.Action == NotifyCollectionChangedAction.Add ||
						e.Action == NotifyCollectionChangedAction.Remove ||
						e.Action == NotifyCollectionChangedAction.Reset))
					{
						m_update_signalled = true;
						m_dispatcher.BeginInvoke(new Action(() =>
						{
							EntriesChanged?.Invoke(null, EventArgs.Empty);
							m_update_signalled = false;
						}));
					}
				}
			}
		}
		private static ObservableCollection<LogControl.LogEntry> m_entries = null!;
		private static bool m_update_signalled;

		/// <summary>Highlighting patterns (in priority order)</summary>
		public static ObservableCollection<LogControl.HLPattern> Highlighting { get; }

		/// <summary>Return a timestamp for 'now'</summary>
		public static TimeSpan Timestamp => DateTimeOffset.Now - m_tzero;

		/// <summary>Reset the log messages</summary>
		public static void Clear(Func<LogControl.LogEntry, bool>? pred = null)
		{
			if (pred == null)
				Entries.Clear();
			else
				((IList<LogControl.LogEntry>)Entries).RemoveIf(pred);
		}

		/// <summary>Add an output message</summary>
		public static void Write(ELogLevel lvl, string message, string filepath, int line)
		{
			var msg = $"{lvl}|{filepath}({line})|{Timestamp.ToString("c")}|{message}";
			var entry = new LogControl.LogEntry(new Provider(), ++m_index, msg, false);
			System.Diagnostics.Debug.Assert(PatternRegex.IsMatch(entry.Text));
			lock (m_lock) Entries.Add(entry);
		}
		public static void Write(ELogLevel lvl, Exception ex, string message, string filepath, int line)
		{
			Write(lvl, $"{message} - {ex.Message}", filepath, line);
		}

		/// <summary>Output text pattern</summary>
		public static readonly Regex PatternRegex = new Regex(
			@"^(?<Level>.*?)\|(?<File>.*?)\((?<Line>.*)\)\|(?<Timestamp>.*?)\|(?<Message>.*)",
			//@"^(?<File>.*?)\((?<Line>.*)\):\s*(?<Tag>.*?)\|(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)",
			RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		/// <summary></summary>
		private class Provider :LogControl.ILogEntryPatternProvider
		{
			public Regex? LogEntryPattern => Log.PatternRegex;
			public IEnumerable<LogControl.HLPattern> Highlighting => Log.Highlighting;
		}
	}
}
