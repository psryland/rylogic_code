using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.audio;

namespace TestCS
{
	public class MidiUI :Form
	{
		private Label m_lbl_version;
		private VirtualMidi m_midi;
		private VirtualMidi.Callback m_cb;
		private pr.gui.LogUI m_log_ui;

		public MidiUI()
		{
			InitializeComponent();

			VirtualMidi.LogMode = VirtualMidi.ELogMode.All;
			var manufacturer = new Guid("aa4e075f-3504-4aab-9b06-9a4104a91cf0");
			var product = new Guid("bb4e075f-3504-4aab-9b06-9a4104a91cf0");

			bool CallbackMode = true;
			m_midi = CallbackMode
				? new VirtualMidi("Paul's Test Midi Device", m_cb = HandleMidiCommand, IntPtr.Zero, 65535, VirtualMidi.EFlags.ParseRX, manufacturer, product)
				: new VirtualMidi("Paul's Test Midi Device", 65535, VirtualMidi.EFlags.ParseRX, manufacturer, product);
			m_log_ui.AddMessage($"MIDI Port created");

			SetupUI();

			if (!CallbackMode)
				WorkerThreadActive = true;
		}
		protected override void Dispose(bool disposing)
		{
			WorkerThreadActive = false;
			m_midi.Shutdown();
			base.Dispose(disposing);
		}

		/// <summary></summary>
		private void SetupUI()
		{
			m_lbl_version.Text =
				$"Dll Version:    {VirtualMidi.VersionString}\r\n" +
				$"Driver Version: {VirtualMidi.DriverVersionString}\r\n";
		}

		/// <summary>Worker thread for processing midi commands</summary>
		public bool WorkerThreadActive
		{
			get { return m_thread != null; }
			set
			{
				if (WorkerThreadActive == value) return;
				if (WorkerThreadActive)
				{
					m_midi.Shutdown();
				}
				m_thread = value ? new Thread(new ThreadStart(WorkThreadFunction)) : null;
				if (WorkerThreadActive)
				{
					m_thread.Start();
				}
			}
		}
		private Thread m_thread;
		public void WorkThreadFunction()
		{
			try
			{
				for (;;)
				{
					var command = m_midi.GetCommand();
					m_log_ui.AddMessage(command.Description);
					m_midi.SendCommand(command);
				}
			}
			catch (Exception ex)
			{
				m_log_ui.AddMessage($"Thread aborting: {ex.Message}");
			}
		}

		/// <summary>Alternative to the worker thread method</summary>
		private void HandleMidiCommand(IntPtr handle, byte[] midi_data_bytes, uint length, IntPtr ctx)
		{
			try
			{
				var command = new Midi.Command(midi_data_bytes);
				m_log_ui.AddMessage(command.Description);
				m_midi.SendCommand(command);
			}
			catch (Exception ex)
			{
				m_log_ui.AddMessage($"Callback error: {ex.Message}");
			}
		}

		#region Designer
		private void InitializeComponent()
		{
			this.m_lbl_version = new System.Windows.Forms.Label();
			this.m_log_ui = new pr.gui.LogUI();
			this.SuspendLayout();
			// 
			// m_lbl_version
			// 
			this.m_lbl_version.AutoSize = true;
			this.m_lbl_version.Location = new System.Drawing.Point(12, 9);
			this.m_lbl_version.Name = "m_lbl_version";
			this.m_lbl_version.Size = new System.Drawing.Size(42, 13);
			this.m_lbl_version.TabIndex = 0;
			this.m_lbl_version.Text = "Version";
			// 
			// m_log_ui
			// 
			this.m_log_ui.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_log_ui.LineWrap = false;
			this.m_log_ui.Location = new System.Drawing.Point(12, 42);
			this.m_log_ui.Name = "m_log_ui";
			this.m_log_ui.PopOutOnNewMessages = true;
			this.m_log_ui.Size = new System.Drawing.Size(260, 207);
			this.m_log_ui.TabIndex = 1;
			this.m_log_ui.Title = "Log";
			// 
			// MidiUI
			// 
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.m_log_ui);
			this.Controls.Add(this.m_lbl_version);
			this.Name = "MidiUI";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
