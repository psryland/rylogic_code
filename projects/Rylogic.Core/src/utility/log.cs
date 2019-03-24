//***************************************************
// Log Helper
//  Copyright (c) Rylogic Ltd 2011
//***************************************************
// Usage:
//  At the start of your program call:
//   Log.Register(filepath, reset:false);
// Throughout call
//   Log.Info(this, "Message");
//   etc
//
// To output to a file, Register with a valid filename
// To output to the debug console, Register with the magic string "DebugConsole"
// To output to nowhere, Register with null or an empty string

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Rylogic.Common;
using Rylogic.Extn;

namespace Rylogic.Utility
{
	public enum ELogLevel
	{
		NoLevel,
		Debug,
		Info,
		Warn,
		Error,
	}
	public enum ELogOutputLevel
	{
		Debug,
		Info,
		Warn,
		Error,
		Exception,
		Silent,
	}
	public interface ILogWriter
	{
		/// <summary>Write a message to the log. Filtering already applied</summary>
		void Write(string msg);
	}

	/// <summary>Static log instance for basic diagnostic logging</summary>
	public static class Log
	{
		public const string ToDebugConsole = "DebugConsole";
		public const char EntryDelimiter = '\u001b';

		static Log()
		{
			Writer = new NullLogger();
			#if DEBUG
			Level = ELogOutputLevel.Debug;
			#else
			Level = ELogOutputLevel.Info;
			#endif
		}

		/// <summary>Set the log level. Messages below this level aren't logged</summary>
		public static ELogOutputLevel Level { get; set; }

		/// <summary>Where log output is sent</summary>
		public static ILogWriter Writer { get; set; }

		/// <summary>Regex filter pattern</summary>
		public static string FilterPattern { get; set; }

		/// <summary>Register a log file for the current process id. Pass null or empty string for filepath to log to the debug window</summary>
		public static void Register(string filepath, bool reset)
		{
			try
			{
				if (filepath == ToDebugConsole)
				{
					Writer = new DebugConsoleLogger();
					return;
				}
				if (Path_.IsValidFilepath(filepath, false))
				{
					Writer = new FileLogger(filepath, reset);
					return;
				}
			}
			catch {}
			Writer = new NullLogger();
			return;
		}

		/// <summary>Write debug trace statements into the log</summary>
		public static void Debug(object sender, string str)
		{
			Write(ELogOutputLevel.Debug, sender, str);
		}
		public static void Debug(object sender, Exception ex, string str)
		{
			Write(ELogOutputLevel.Debug, sender, str, ex);
		}

		/// <summary>Write info to the current log</summary>
		public static void Info(object sender, string str)
		{
			Write(ELogOutputLevel.Info, sender, str);
		}
		public static void Info(object sender, Exception ex, string str)
		{
			Write(ELogOutputLevel.Info, sender, str, ex);
		}

		/// <summary>Write info to the current log</summary>
		public static void Warn(object sender, string str)
		{
			Write(ELogOutputLevel.Warn, sender, str);
		}
		public static void Warn(object sender, Exception ex, string str)
		{
			Write(ELogOutputLevel.Warn, sender, str, ex);
		}

		/// <summary>Write info to the current log</summary>
		public static void Error(object sender, string str)
		{
			Write(ELogOutputLevel.Error, sender, str);
		}
		public static void Error(object sender, Exception ex, string str)
		{
			Write(ELogOutputLevel.Error, sender, str, ex);
		}

		/// <summary>Write info to the current log</summary>
		public static void Exception(object sender, Exception ex, string str)
		{
			Write(ELogOutputLevel.Exception, sender, str, ex);
		}

		/// <summary>Single method for filtering and formatting log messages</summary>
		private static void Write(ELogOutputLevel level, object sender, string str, Exception ex = null)
		{
			// Below the log level, ignore
			if (level < Level)
				return;

			// Convert 'sender' to a tag
			string tag;
			if (sender == null)
			{
				tag = "";
			}
			else if (sender is string)
			{
				tag = (string)sender;
			}
			else
			{
				Type sender_type = sender.GetType();
				tag = $"{sender_type.Namespace}.{sender_type.Name}";
			}

			// Filter out messages based on tag
			if (FilterPattern != null && !Regex.IsMatch(tag, FilterPattern))
				return;

			// Construct the log message and write it
			var msg = $"[{level}][{tag}] {str}{(ex != null ? Environment.NewLine + ex.MessageFull() : string.Empty)}"+Environment.NewLine;
			Writer.Write(msg);
		}

		#region Logger Implementations
		public class NullLogger :ILogWriter
		{
			public void Write(string msg)
			{ }
		}
		public class DebugConsoleLogger :ILogWriter
		{
			private readonly object m_lock;
			public DebugConsoleLogger()
			{
				m_lock = new object();
			}
		
			/// <summary>Add a string to the log</summary>
			public void Write(string msg)
			{
				lock (m_lock)
					System.Diagnostics.Debug.Write(msg);
			}
		}
		public class FileLogger :ILogWriter
		{
			private readonly object m_lock;
			public FileLogger(string filepath, bool reset)
			{
				System.Diagnostics.Debug.Assert(Path_.IsValidFilepath(filepath, false));
				m_lock = new object();

				// Ensure the directory exists (maybe this should go in an official logs folder?)
				var dir = Path_.Directory(filepath);
				if (!Path_.DirExists(dir))
					Directory.CreateDirectory(dir);

				// Ensure the file exists/is reset
				Filepath = filepath;
				using (File.Open(Filepath, reset ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) {}
			}

			/// <summary>The file being written</summary>
			public string Filepath { get; set; }

			/// <summary>Add a string to the log</summary>
			public void Write(string str)
			{
				lock (m_lock)
					using (var f = new StreamWriter(File.Open(Filepath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
						f.Write(str);
			}
		}
		#endregion
	}

	/// <summary>Asynchronous logging instance</summary>
	public class Logger :IDisposable
	{
		public Logger(string tag, ILogWriter log_cb, int occurrences_batch_size = 0)
		{
			Tag = tag;
			Context = new SharedContext(log_cb, occurrences_batch_size);
			ForwardLog = null;
			Enabled = true;
		}
		public Logger(string tag, ILogWriter log_cb, Logger fwd, int occurrences_batch_size = 0)
			:this(tag, log_cb, occurrences_batch_size)
		{
			ForwardLog = fwd;
		}
		public Logger(string tag, Logger rhs)
		{
			Tag = tag;
			Context = rhs.Context;
			ForwardLog = rhs.ForwardLog;
			Enabled = true;
		}
		public virtual void Dispose()
		{
			Context = null;
		}

		/// <summary>The log entry tag for this instance</summary>
		public string Tag { get; set; }

		/// <summary>The character sequence that separates log entries</summary>
		public char EntryDelimiter
		{
			get { return Context.EntryDelimiter; }
			set { Context.EntryDelimiter = value; }
		}

		/// <summary>True if each log entry is time stamped</summary>
		public bool AddTimestamp
		{
			get { return Context.AddTimestamp; }
			set { Context.AddTimestamp = value; }
		}

		/// <summary>Get/Set the zero time for the log</summary>
		public DateTimeOffset TimeZero
		{
			get { return Context.TimeZero; }
			set { Context.TimeZero = value; }
		}

		/// <summary>The method that converts a log entry into a string</summary>
		public Func<LogEvent, string> Serialise
		{
			get { return Context.Serialise; }
			set { Context.Serialise = value; }
		}
		public string DefaultSerialise(LogEvent evt)
		{
			return Context.DefaultSerialise(evt);
		}

		/// <summary>Access the shared context</summary>
		private SharedContext Context
		{
			[DebuggerStepThrough] get { return m_ctx; }
			set
			{
				if (m_ctx == value) return;
				if (m_ctx != null)
				{
					m_ctx.RefCount.ReleaseRef();
				}
				m_ctx = value;
				if (m_ctx != null)
				{
					m_ctx.RefCount.AddRef();
				}
			}
		}
		private SharedContext m_ctx;

		/// <summary>A log instance to forward log entries to</summary>
		public Logger ForwardLog { get; set; }

		/// <summary>The object that log data is written to</summary>
		public ILogWriter LogCB
		{
			[DebuggerStepThrough] get { return Context.LogCB; }
		}

		/// <summary>On/Off switch for logging</summary>
		public bool Enabled
		{
			get;
			set;
		}

		/// <summary>
		/// Enable/Disable immediate mode.
		/// In immediate mode, log events are written to 'log_cb' instead of being queued for processing
		/// by the background thread. Useful when you want the log to be written in sync with debugging.</summary>
		public bool ImmediateWrite
		{
			get { return Context.ImmediateWrite; }
			set { Context.ImmediateWrite = value; }
		}

		/// <summary>Log a message</summary>
		public void Write(ELogLevel level, string msg, string file = null, int? line = null)
		{
			if (!Enabled) return;
			var evt = new LogEvent(level, Context.TimeZero, Tag, msg, file, line);
			Write(evt);
			ForwardLog?.Write(evt);
		}

		/// <summary>Log an exception with message 'msg'</summary>
		public void Write(ELogLevel level, Exception ex, string msg, string file = null, int? line = null)
		{
			if (!Enabled) return;
			var message =
				ex is AggregateException        ae ? string.Concat(msg," - Exception: ", ae.MessageFull()) :
				ex is TargetInvocationException ie ? string.Concat(msg," - Exception: ", ie.InnerException.Message) :
				string.Concat(msg," - Exception: ", ex.MessageFull());
			var evt = new LogEvent(level, Context.TimeZero, Tag, message, file, line);
			Write(evt);
			ForwardLog?.Write(evt);
		}

		/// <summary>Write a log entry</summary>
		private void Write(LogEvent evt)
		{
			Context.Enqueue(evt);
		}

		/// <summary>Block the caller until the logger is idle</summary>
		public void Flush()
		{
			if (!Enabled) return;
			Context.WaitTillIdle();
		}

		/// <summary>Convert a log entry to a string</summary>
		public static string DefaultSerialise(StringBuilder sb, LogEvent evt, char entry_delimiter, bool timestamp)
		{
			var pre = string.Empty;
			if (entry_delimiter != '\0')        { sb.Clear(); sb.Append(entry_delimiter); }
			if (evt.File.HasValue())            { sb.Append(evt.File); pre = " "; }
			if (evt.Line != null)               { sb.Append($"({evt.Line.Value}):"); pre = " "; }
			if (evt.Tag.HasValue())             { sb.Append($"{pre}{evt.Tag:8}"); pre = "|"; }
			if (evt.Level != ELogLevel.NoLevel) { sb.Append($"{pre}{evt.Level}"); pre = "|"; }
			if (timestamp)                      { sb.Append($"{pre}{evt.Timestamp.ToString("c")}"); pre = "|"; }
			sb.Append(pre).Append(evt.Message);
			return sb.ToString();
		}

		/// <summary>Logger context. A single Context is shared by many instances of a Logger</summary>
		private class SharedContext :IDisposable
		{
			public SharedContext(ILogWriter log_cb, int occurrences_batch_size)
			{
				SB = new StringBuilder();
				EntryDelimiter = Log.EntryDelimiter;
				AddTimestamp = true;
				Serialise = DefaultSerialise;

				// Clean up when there are no more loggers referencing this context
				RefCount = new RefCount(0);
				RefCount.ZeroCount += (s,a) => Dispose();

				Queue = new BlockingCollection<LogEvent>();
				Idle = new ManualResetEvent(true);
				LogCB = log_cb;
				OccurrencesBatchSize = occurrences_batch_size;

				TimeZero = DateTimeOffset.Now;
				LogConsumerThreadActive = true;
			}
			public void Dispose()
			{
				LogConsumerThreadActive = false;
			}

			/// <summary>The character sequence that separates log entries</summary>
			public char EntryDelimiter { get; set; }

			/// <summary>True if each log entry is time stamped</summary>
			public bool AddTimestamp { get; set; }

			/// <summary>References to this shared context</summary>
			public RefCount RefCount { get; private set; }

			/// <summary>The time point when logging started</summary>
			public DateTimeOffset TimeZero { get; set; }

			/// <summary>Queue of log events to report</summary>
			public BlockingCollection<LogEvent> Queue { get; private set; }

			/// <summary>A flag to indicate when the logger is idle</summary>
			public ManualResetEvent Idle { get; private set; }

			/// <summary>A callback function used in immediate mode</summary>
			public ILogWriter LogCB { get; private set; }

			/// <summary>The maximum number of identical events per batch</summary>
			public int OccurrencesBatchSize { get; private set; }

			/// <summary>The method that converts a log entry into a string</summary>
			public Func<LogEvent, string> Serialise { get; set; }
			public string DefaultSerialise(LogEvent evt)
			{
				return Logger.DefaultSerialise(SB, evt, EntryDelimiter, AddTimestamp);
			}
			public StringBuilder SB;

			/// <summary>
			/// Enable/Disable immediate mode.
			/// In immediate mode, log events are written to 'log_cb' instead of being queued for processing
			/// by the background thread. Useful when you want the log to be written in sync with debugging.</summary>
			public bool ImmediateWrite { get; set; }

			// The worker thread that forwards log events to the callback function
			private bool LogConsumerThreadActive
			{
				get { return m_thread != null; }
				set
				{
					if (LogConsumerThreadActive == value) return;
					if (m_thread != null)
					{
						Queue.CompleteAdding();
						if (m_thread.IsAlive)
							m_thread.Join();
					}
					m_thread = value ? new Thread(new ThreadStart(() => LogConsumerThread(this, LogCB, OccurrencesBatchSize))) : null;
					if (m_thread != null)
					{
						m_thread.Start();
					}
				}
			}
			private void LogConsumerThread(SharedContext ctx, ILogWriter log_cb, int occurrences_batch_size = 0)
			{
				try
				{
					Thread.CurrentThread.Name = "pr::Logger";

					for (var last = new LogEvent(); Dequeue(out var ev);)
					{
						// Same event as last time? add it to the batch
						bool is_same = false;
						if (last.Occurrences < occurrences_batch_size && (is_same = LogEvent.Same(ev, last)) == true)
						{
							++last.Occurrences;
							last.Timestamp = ev.Timestamp;
							continue;
						}

						// Have events been batched? Report them now
						if (last.Occurrences != 0)
						{
							log_cb.Write(Serialise(last));
							last.Occurrences = 0;
						}

						// Start of the next batch (and batching is enabled)? Add it to the batch
						if (occurrences_batch_size != 0 && is_same)
						{
							last.Occurrences = 1;
							last.Timestamp = ev.Timestamp;
						}
						else
						{
							log_cb.Write(Serialise(ev));
							last = ev;
							last.Occurrences = 0;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Assert(false, $"Unknown exception in log thread: {ex.Message}");
				}
			}
			private Thread m_thread;

			/// <summary>Queue a log event for writing to the log callback function</summary>
			public void Enqueue(LogEvent ev)
			{
				if (ImmediateWrite)
				{
					LogCB.Write(Serialise(ev));
				}
				else
				{
					Idle.Reset();
					Queue.Add(ev);
				}
			}

			// Pull an event from the queue of log events
			private bool Dequeue(out LogEvent ev)
			{
				try
				{
					if (!Queue.TryTake(out ev))
					{
						Idle.Set();
						ev = Queue.Take(); // Blocks
						Idle.Reset();
					}
					return true;
				}
				catch (InvalidOperationException) // means take called on completed collection
				{
					ev = null;
					return false;
				}
			}

			// Wait for the log event queue to become empty
			public void WaitTillIdle()
			{
				Idle.WaitOne();
			}
			public bool WaitTillIdle(int timeout_ms)
			{
				return Idle.WaitOne(timeout_ms);
			}
		}

		/// <summary>An individual log event</summary>
		public class LogEvent
		{
			public LogEvent()
			{
				Level       = ELogLevel.Error;
				Timestamp   = TimeSpan.Zero;
				Tag     = string.Empty;
				Message     = string.Empty;
				File        = string.Empty;
				Line        = 0;
				Occurrences = 0;
			}
			public LogEvent(ELogLevel level, DateTimeOffset tzero, string ctx, string msg, string file, int? line)
			{
				Level       = level;
				Timestamp   = DateTimeOffset.Now - tzero;
				Tag     = ctx;
				Message     = msg;
				File        = file;
				Line        = line;
				Occurrences = 1;
			}

			/// <summary>The importance of the log event</summary>
			public ELogLevel Level { get; private set; }

			/// <summary>When the log event was recorded (relative to the start time of the context)</summary>
			public TimeSpan Timestamp { get; internal set; }

			/// <summary>The tag of the logger through which the event was added</summary>
			public string Tag { get; private set; }

			/// <summary>The log message</summary>
			public string Message { get; private set; }

			/// <summary>The associated file (if appropriate)</summary>
			public string File { get; private set; }

			/// <summary>The associated line number (if appropriate)</summary>
			public int? Line { get; private set; }

			/// <summary>The number of occurrences of the same log event</summary>
			public int Occurrences { get; internal set; }

			/// <summary>Convert the log event to a string</summary>
			public override string ToString()
			{
				return DefaultSerialise(new StringBuilder(), this, '\0', true);
			}

			/// <summary>Compare log events for equality</summary>
			public static bool Same(LogEvent lhs, LogEvent rhs)
			{
				return
					lhs.Level   == rhs.Level   &&
					lhs.Tag == rhs.Tag &&
					lhs.File    == rhs.File    &&
					lhs.Line    == rhs.Line    &&
					lhs.Message == rhs.Message;
			}
		}
	}

	/// <summary>Log writer that writes to a file</summary>
	public class LogToFile :ILogWriter ,IDisposable
	{
		private FileStream m_outf;

		public LogToFile(string filepath, bool append)
		{
			Filepath = filepath;
			append &= File.Exists(filepath);
			Path_.CreateDirs(Path_.Directory(filepath));
			m_outf = new FileStream(Filepath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		}
		public virtual void Dispose()
		{
			Util.Dispose(ref m_outf);
		}
		public string Filepath { get; }
		public void Write(string text)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			m_outf.Seek(0, SeekOrigin.End);
			m_outf.Write(bytes, 0, bytes.Length);
			m_outf.Flush();
		}
	}

	/// <summary>Log writer that writes to a string</summary>
	public class LogToString :ILogWriter
	{
		public LogToString()
		{
			Str = new StringBuilder();
		}
		public StringBuilder Str
		{
			get;
			private set;
		}
		public void Write(string text)
		{
			Str.Append(text);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Utility;

	[TestFixture]
	public class TestLogger
	{
		[Test]
		public void Logger()
		{
			var l2s = new LogToString();
			using (var log0 = new Logger("Thing1", l2s, 1) { AddTimestamp = false })
			using (var log1 = new Logger("Thing2", log0))
			{
				log0.Write(ELogLevel.Error, "Error message", "A:\\file.txt", 32);
				log1.Write(ELogLevel.Info, "Info message");
				log0.Write(ELogLevel.Warn, new Exception("Exception"), "Exception message");

				log0.Flush();

				// Overwrite the timestamp so its the same for all unit tests
				var lines = l2s.Str.ToString().Split(new[]{"\u001b"}, StringSplitOptions.RemoveEmptyEntries);
				Assert.Equal(3, lines.Length);
				Assert.True(lines[0] == "A:\\file.txt(32): Thing1|Error|Error message");
				Assert.True(lines[1] == "Thing2|Info|Info message");
				Assert.True(lines[2] == "Thing1|Warn|Exception message - Exception: Exception");
			}
		}
	}
}
#endif