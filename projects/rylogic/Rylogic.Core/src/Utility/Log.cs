//***************************************************
// Log Helper
//  Copyright (c) Rylogic Ltd 2011
//***************************************************
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
	/// <summary>Asynchronous logging instance</summary>
	public class Logger :IDisposable
	{
		// Notes:
		//  - This Logger isn't a singleton, it is completely instantiable. Applications
		//    that want to use a singleton can create reference-counted singleton instances
		//    of this object. (see boiler plate below)
		//  - The shared context is only shared between Logger instances that are
		//    created from an existing Logger instance. Each Logger constructor that
		//    creates a new 'SharedContext' is independent of other Loggers.

		/*	// Static Log Boiler Plate:
			public static class Log
			{
				static Log()
				{
					m_ref_count = new RefCount();
					m_ref_count.Referenced += delegate
					{
						m_logger = new Logger("VRex", new LogToFile(Filepath, LogToFile.EFlags.None));
						
						// Optional
						m_logger.Serialise = evt => $"{Log_.EntryDelimiter}{evt.Level}|{evt.Timestamp}|{evt.Message}|\n";
						m_logger.LogEntryPattern = new Regex($@"^(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*?)\|\n", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
					};
					m_ref_count.ZeroCount += delegate
					{
						Util.Dispose(ref m_logger!);
					};
				}
				private static RefCount m_ref_count;
				private static Logger m_logger = null!;
			
				/// <summary>Get a reference token for the Log</summary>
				public static IDisposable RefToken(object who) => m_ref_count.RefToken(who);
			
				/// <summary>Checked access to the 'm_logger' isntance</summary>
				private static Logger m_log => m_logger ?? throw new Exception("No reference to the logger exists. Applications should create one before using the log");
			
				/// <summary>Log filepath</summary>
				public static string Filepath => Util.ResolveUserDocumentsPath("Rylogic", "MyApp", "Logs", "log.txt");
			
				/// <summary>Log a message</summary>
				public static void Write(ELogLevel level, string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => m_log.Write(level, msg, file, line);
			
				/// <summary>Log an exception with message 'msg'</summary>
				public static void Write(ELogLevel level, Exception ex, string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => m_log.Write(level, ex, msg, file, line);
			}
		*/

		private Logger(string tag, SharedContext ctx, Logger? forward_log)
		{
			m_ctx = null!;
			Tag = tag;
			Context = ctx;
			ForwardLog = forward_log;
			Enabled = true;
		}
		public Logger(string tag, ILogWriter log_cb, int occurrences_batch_size = 0)
			: this(tag, new SharedContext(log_cb, occurrences_batch_size), null)
		{ }
		public Logger(string tag, ILogWriter log_cb, Logger fwd, int occurrences_batch_size = 0)
			: this(tag, new SharedContext(log_cb, occurrences_batch_size), fwd)
		{ }
		public Logger(string tag, Logger rhs)
			: this(tag, rhs.Context, rhs.ForwardLog)
		{ }
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			Context = null!;
		}

		/// <summary>The log entry tag for this instance</summary>
		public string Tag { get; set; }

		/// <summary>The character sequence that separates log entries</summary>
		public char EntryDelimiter
		{
			get => Context.EntryDelimiter;
			set => Context.EntryDelimiter = value;
		}

		/// <summary>True if each log entry is time stamped</summary>
		public bool AddTimestamp
		{
			get => Context.AddTimestamp;
			set => Context.AddTimestamp = value;
		}

		/// <summary>Get/Set the zero time for the log</summary>
		public DateTimeOffset TimeZero
		{
			get => Context.TimeZero;
			set => Context.TimeZero = value;
		}

		/// <summary>The method that converts a log entry into a string</summary>
		public Func<LogEvent, string> Serialise
		{
			get => Context.Serialise;
			set => Context.Serialise = value;
		}
		public string DefaultSerialise(LogEvent evt)
		{
			return Context.DefaultSerialise(evt);
		}

		/// <summary>Convenience location for storing a regex pattern corresponding to 'Serialise'</summary>
		public Regex? LogEntryPattern
		{
			get => Context.LogEntryPattern;
			set => Context.LogEntryPattern = value;
		}
		public Regex? DefaultPattern
		{
			get => Context.DefaultLogEntryPattern;
		}

		/// <summary>Access the shared context</summary>
		private SharedContext Context
		{
			get => m_ctx;
			set
			{
				if (m_ctx == value) return;
				if (m_ctx != null)
				{
					Util.Dispose(ref m_ref_token);
				}
				m_ctx = value;
				if (m_ctx != null)
				{
					m_ref_token = m_ctx.RefCount.RefToken(this);
				}
			}
		}
		private SharedContext m_ctx;
		private IDisposable? m_ref_token;

		/// <summary>A log instance to forward log entries to</summary>
		public Logger? ForwardLog { get; set; }

		/// <summary>The object that log data is written to</summary>
		public ILogWriter LogCB => Context.LogCB;

		/// <summary>On/Off switch for logging</summary>
		public bool Enabled { get; set; }

		/// <summary>
		/// Enable/Disable immediate mode.
		/// In immediate mode, log events are written to 'log_cb' instead of being queued for processing
		/// by the background thread. Useful when you want the log to be written in sync with debugging.</summary>
		public bool ImmediateWrite
		{
			get => Context.ImmediateWrite;
			set => Context.ImmediateWrite = value;
		}

		/// <summary>Log a message</summary>
		public void Write(ELogLevel level, string msg, string? file = null, int? line = null)
		{
			if (!Enabled) return;
			var evt = new LogEvent(level, Context.TimeZero, Tag, msg.TrimEnd('\n', '\r'), file, line);
			Write(evt);
			ForwardLog?.Write(evt);
		}

		/// <summary>Log an exception with message 'msg'</summary>
		public void Write(ELogLevel level, Exception ex, string msg, string? file = null, int? line = null)
		{
			if (!Enabled) return;
			var message = new StringBuilder(msg);
			if (ex is AggregateException ae)
			{
				message.Append(" - Exception: ").Append(ae.MessageFull());
			}
			else if (ex is TargetInvocationException ie && ie.InnerException != null)
			{
				message.Append(" - Exception: ").Append(ie.InnerException.Message);
			}
			else
			{
				message.Append(" - Exception: ").Append(ex.MessageFull());
			}
			message.TrimEnd('\n','\r');

			var evt = new LogEvent(level, Context.TimeZero, Tag, message.ToString(), file, line);
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
		public static string DefaultSerialise(StringBuilder sb, LogEvent evt, char entry_delimiter, bool timestamp, bool newlines)
		{
			var pre = string.Empty;
			if (entry_delimiter != '\0')        { sb.Clear(); sb.Append(entry_delimiter); }
			if (evt.File.HasValue())            { sb.Append(evt.File); pre = "|"; }
			if (evt.Line != null)               { sb.Append($"({evt.Line.Value})"); pre = "|"; }
			if (evt.Tag.HasValue())             { sb.Append($"{pre}{evt.Tag:8}"); pre = "|"; }
			if (evt.Level != ELogLevel.NoLevel) { sb.Append($"{pre}{evt.Level}"); pre = "|"; }
			if (timestamp)                      { sb.Append($"{pre}{evt.Timestamp:c}"); pre = "|"; }
			sb.Append(pre).Append(evt.Message);
			if (newlines && sb.Length != 0 && sb[sb.Length-1] != '\n') sb.Append('\n');
			return sb.ToString();
		}
		public static Regex DefaultLogEntryPattern()
		{
			// Notes:
			//  - This is just a simple pattern to make any log readable.
			//    You will probably not want to use this pattern.
			//  - The pattern doesn't need to include the entry delimiter.
			//    The pattern is only applied to each row read from the log file.
			//    The entry delimiter is used to determine a "row" and is therefore not part of each row.
			return new Regex($"^(?<Message>.*)\n", RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		}

		/// <summary>Logger context. A single Context is shared by many instances of a Logger</summary>
		private class SharedContext :IDisposable
		{
			public SharedContext(ILogWriter log_cb, int occurrences_batch_size)
			{
				SB = new StringBuilder();
				EntryDelimiter = Log_.EntryDelimiter;
				AddTimestamp = true;
				AddNewLines = true;
				Serialise = DefaultSerialise;
				LogEntryPattern = DefaultLogEntryPattern;

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

			/// <summary>True if each log entry has a newline at the end</summary>
			public bool AddNewLines { get; set; }

			/// <summary>References to this shared context</summary>
			public RefCount RefCount { get; }

			/// <summary>The time point when logging started</summary>
			public DateTimeOffset TimeZero { get; set; }

			/// <summary>Queue of log events to report</summary>
			public BlockingCollection<LogEvent> Queue { get; }

			/// <summary>A flag to indicate when the logger is idle</summary>
			public ManualResetEvent Idle { get; }

			/// <summary>A callback function used in immediate mode</summary>
			public ILogWriter LogCB { get; }

			/// <summary>The maximum number of identical events per batch</summary>
			public int OccurrencesBatchSize { get; }

			/// <summary>The method that converts a log entry into a string</summary>
			public Func<LogEvent, string> Serialise { get; set; }
			public string DefaultSerialise(LogEvent evt) => Logger.DefaultSerialise(SB, evt, EntryDelimiter, AddTimestamp, AddNewLines);
			public StringBuilder SB;

			/// <summary>Convenience location for storing a regex pattern corresponding to 'Serialise'</summary>
			public Regex? LogEntryPattern { get; set; }
			public Regex? DefaultLogEntryPattern => Logger.DefaultLogEntryPattern();

			/// <summary>
			/// Enable/Disable immediate mode.
			/// In immediate mode, log events are written to 'log_cb' instead of being queued for processing
			/// by the background thread. Useful when you want the log to be written in sync with debugging.</summary>
			public bool ImmediateWrite { get; set; }

			// The worker thread that forwards log events to the callback function
			private bool LogConsumerThreadActive
			{
				get => m_thread != null;
				set
				{
					if (LogConsumerThreadActive == value) return;
					if (m_thread != null)
					{
						Queue.Add(ShutdownSentinal);
						Queue.CompleteAdding();
						if (m_thread.IsAlive)
							m_thread.Join();
					}
					
					var stack_trace = Util.IsDebug ? new StackTrace(true).ToString() : string.Empty;
					m_thread = value ? new Thread(new ThreadStart(() => LogConsumerThread(this, LogCB, OccurrencesBatchSize, stack_trace))) : null;
					if (m_thread != null)
					{
						m_thread.Start();
					}

					// Entry point
					void LogConsumerThread(SharedContext ctx, ILogWriter log_cb, int occurrences_batch_size = 0, string? stack_trace = null)
					{
						try
						{
							// Note: If this thread is still running at shutdown, check 'stack_trace' to
							// see where it was created. All 'Logger' instances need to be disposed before
							// this shared context is disposed (which shuts down the thread).
							Thread.CurrentThread.Name = "Rylogic.Logger";
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
				}
			}
			private Thread? m_thread;

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

			/// <summary>Pull an event from the queue of log events</summary>
			private bool Dequeue(out LogEvent ev)
			{
				try
				{
					if (Queue.IsAddingCompleted)
					{
						ev = new LogEvent();
						return false;
					}
					if (!Queue.TryTake(out ev!))
					{
						// Notes:
						//  - If you get a block here on shutdown, it's probably because of leaked references to the
						//    log shared context. Look at 'RefCount' in the debugger. You may turn on 'REFS' and 'STACKTRACES'
						//    in the 'RefCount' class if you want. In each 'm_refs', look at the 'Tag' name.
						Idle.Set();
						ev = Queue.Take(); // Blocks
						Idle.Reset();
					}
					if (ReferenceEquals(ev, ShutdownSentinal))
					{
						ev = new LogEvent();
						return false;
					}
					return true;
				}
				catch (InvalidOperationException) // means take called on completed collection
				{
					ev = new LogEvent();
					return false;
				}
			}

			/// <summary>Wait for the log event queue to become empty</summary>
			public void WaitTillIdle()
			{
				Idle.WaitOne();
			}
			public bool WaitTillIdle(int timeout_ms)
			{
				return Idle.WaitOne(timeout_ms);
			}

			/// <summary>Magic value to signal log shutdown</summary>
			private static readonly LogEvent ShutdownSentinal = new();
		}

		/// <summary>An individual log event</summary>
		public class LogEvent
		{
			public LogEvent()
			{
				Level       = ELogLevel.Error;
				Timestamp   = TimeSpan.Zero;
				Tag         = string.Empty;
				Message     = string.Empty;
				File        = string.Empty;
				Line        = 0;
				Occurrences = 0;
			}
			public LogEvent(ELogLevel level, DateTimeOffset tzero, string ctx, string msg, string? file, int? line)
			{
				Level       = level;
				Timestamp   = DateTimeOffset.Now - tzero;
				Tag         = ctx;
				Message     = msg;
				File        = file;
				Line        = line;
				Occurrences = 1;
			}

			/// <summary>The importance of the log event</summary>
			public ELogLevel Level { get; }

			/// <summary>When the log event was recorded (relative to the start time of the context)</summary>
			public TimeSpan Timestamp { get; internal set; }

			/// <summary>The tag of the logger through which the event was added</summary>
			public string Tag { get; }

			/// <summary>The log message</summary>
			public string Message { get; }

			/// <summary>The associated file (if appropriate)</summary>
			public string? File { get; }

			/// <summary>The associated line number (if appropriate)</summary>
			public int? Line { get; }

			/// <summary>The number of occurrences of the same log event</summary>
			public int Occurrences { get; internal set; }

			/// <summary>Convert the log event to a string</summary>
			public override string ToString()
			{
				return DefaultSerialise(new StringBuilder(), this, '\0', true, true);
			}

			/// <summary>Compare log events for equality</summary>
			public static bool Same(LogEvent lhs, LogEvent rhs)
			{
				return
					lhs.Level == rhs.Level &&
					lhs.Tag == rhs.Tag &&
					lhs.File == rhs.File &&
					lhs.Line == rhs.Line &&
					lhs.Message == rhs.Message;
			}
		}
	}

	/// <summary>Log writer that writes to a file</summary>
	public class LogToFile :ILogWriter ,IDisposable
	{
		private readonly FileStream m_outf;
		public LogToFile(string filepath, EFlags flags)
		{
			Filepath = filepath;
			Flags = flags;

			Path_.CreateDirs(Path_.Directory(filepath));
			var append = Flags.HasFlag(EFlags.Append) && File.Exists(filepath);
			m_outf = new FileStream(Filepath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			m_outf.Dispose();
		}
	
		/// <summary>The full filepath of the log file</summary>
		public string Filepath { get; }

		/// <summary>The flags the log file was created with</summary>
		public EFlags Flags { get; }

		/// <summary>The ILogWriter Write method</summary>
		public void Write(string text)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			m_outf.Seek(0, SeekOrigin.End);
			m_outf.Write(bytes, 0, bytes.Length);
			m_outf.Flush();
		}

		[Flags]
		public enum EFlags
		{
			None = 0,
			Append = 1 << 0,
			DeleteOnClose = 1 << 1,
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

	/// <summary>Log writer that writes to the debug window</summary>
	public class LogToDebug :ILogWriter
	{
		/// <summary>Add a string to the log</summary>
		public void Write(string msg)
		{
			Debug.Write(msg);
		}
	}

	/// <summary>Swallow log messages</summary>
	public class LogToNull: ILogWriter
	{
		public void Write(string msg)
		{ }
	}

	/// <summary>Log extensions</summary>
	public static class Log_
	{
		// Notes:
		//  - The EntryDelimiter must be a single byte UTF8 character because the log control
		//    reads bytes in blocks from the log file, and doesn't support delimiters spanning
		//    blocks (for performance).
		public const char EntryDelimiter = '\u001b';
	}

	/// <summary>Log entry levels</summary>
	public enum ELogLevel
	{
		NoLevel,
		Debug,
		Info,
		Warn,
		Error,
		Fatal,
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
			using var log0 = new Logger("Thing1", l2s, 1) { AddTimestamp = false };
			using var log1 = new Logger("Thing2", log0);
			log0.Write(ELogLevel.Error, "Error message", "A:\\file.txt", 32);
			log1.Write(ELogLevel.Info, "Info message");
			log0.Write(ELogLevel.Warn, new Exception("Exception"), "Exception message");

			log0.Flush();

			// Overwrite the timestamp so its the same for all unit tests
			var lines = l2s.Str.ToString().Split(new[] { "\u001b" }, StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(3, lines.Length);
			Assert.True(lines[0] == "A:\\file.txt(32)|Thing1|Error|Error message\n");
			Assert.True(lines[1] == "Thing2|Info|Info message\n");
			Assert.True(lines[2] == "Thing1|Warn|Exception message - Exception: Exception\n");
		}
	}
}
#endif
