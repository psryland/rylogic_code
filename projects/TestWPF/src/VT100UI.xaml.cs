using System;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Rylogic.Common;

namespace TestWPF
{
	/// <summary></summary>
	public partial class VT100UI :Window
	{
		public VT100UI()
		{
			InitializeComponent();
			Settings = new VT100.Settings();
			Buffer = new VT100.Buffer(Settings);
			Buffer.Output(VT100.Buffer.TestTerminalText0);
			DataContext = this;

			TerminalServer = true;
			Closed += delegate { TerminalServer = false; };
		}

		/// <summary>Terminal settings</summary>
		public VT100.Settings Settings { get; }

		/// <summary>Character buffer</summary>
		public VT100.Buffer Buffer { get; }

		/// <summary>Fake terminal server</summary>
		private bool TerminalServer
		{
			get => m_timer != null;
			set
			{
				if (TerminalServer == value) return;
				if (m_timer != null)
				{
					m_timer.Stop();
				}
				m_timer = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (m_timer != null)
				{
					m_timer.Start();
				}

				// Handlers
				void HandleTick(object? sender, EventArgs e)
				{
					// This function emulates a terminal server responding to received byte data
					for (; ; )
					{
						var line = Buffer.UserInput.PeekLine;
						if (line.IsEmpty)
							break;

						var cmd = Encoding.UTF8.GetString(line).TrimEnd();
						switch (cmd)
						{
							case "help":
							{
								Buffer.Output(
									"Command Help\n"+
									"help - this text\n"+
									"wow - something impressive\n"+
									"\n> ");
								break;
							}
							case "wow":
							{
								Buffer.Output(
									"todo...\n"+
									"\n> ");
								break;
							}
						}
						Buffer.UserInput.Consume(line.Length);
					}
				}
			}
		}
		private DispatcherTimer? m_timer;
	}
}
