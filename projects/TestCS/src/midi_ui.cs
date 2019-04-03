using System;
using System.Threading;
using System.Windows.Forms;
using Rylogic.Audio;
using Rylogic.Core.Windows;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Util = Rylogic.Utility.Util;

namespace TestCS
{
	public class MidiUI :Form
	{
		private Audio m_audio;
		private VirtualMidi m_midi;
		private VirtualMidi.Callback m_cb;

		#region UI Elements
		private Label m_lbl_version;
		private Button m_btn_play;
		private TextBox m_tb_wav_filepath;
		private Button m_btn_make_instrument;
		private Rylogic.Gui.WinForms.LogUI m_log_ui;
		#endregion

		public MidiUI()
		{
			InitializeComponent();

			// Create an Audio instance
			m_audio = new Audio();

			// Create a virtual MIDI device
			bool CallbackMode = true;
			VirtualMidi.LogMode = VirtualMidi.ELogMode.All;
			var manufacturer = new Guid("aa4e075f-3504-4aab-9b06-9a4104a91cf0");
			var product = new Guid("bb4e075f-3504-4aab-9b06-9a4104a91cf0");
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
			Util.Dispose(m_audio);
			base.Dispose(disposing);
		}

		/// <summary></summary>
		private void SetupUI()
		{
			m_lbl_version.Text =
				$"Dll Version:    {VirtualMidi.VersionString}\r\n" +
				$"Driver Version: {VirtualMidi.DriverVersionString}\r\n";

			m_btn_make_instrument.Click += (s,a) =>
			{
				var dlg = new OpenFolderUI { Title = "Select Root instrument folder" };
				if (dlg.ShowDialog(this) != DialogResult.OK) return;
				m_audio.WaveBankCreateMidiInstrument("TEST", dlg.SelectedPath, "D:\\dump\\test.xwb", "D:\\dump\\test.h");
			};

			m_btn_play.Click += (s,a) =>
			{
				PlayWave();
			};
		}

		/// <summary></summary>
		private void PlayWave()
		{
			if (!m_tb_wav_filepath.Text.HasValue())
			{
				using (var dlg = new OpenFileDialog { Title = "Open WAV file", Filter = Util.FileDialogFilter("Wave Files", "*.wav") })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					m_tb_wav_filepath.Text = dlg.FileName;
				}
			}

			if (m_tb_wav_filepath.Text.HasValue())
				m_audio.PlayFile(m_tb_wav_filepath.Text);
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
		private Thread m_thread;

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
			this.m_log_ui = new Rylogic.Gui.WinForms.LogUI();
			this.m_btn_play = new System.Windows.Forms.Button();
			this.m_tb_wav_filepath = new System.Windows.Forms.TextBox();
			this.m_btn_make_instrument = new System.Windows.Forms.Button();
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
			this.m_log_ui.Location = new System.Drawing.Point(2, 110);
			this.m_log_ui.Name = "m_log_ui";
			this.m_log_ui.PopOutOnNewMessages = true;
			this.m_log_ui.Size = new System.Drawing.Size(335, 263);
			this.m_log_ui.TabIndex = 1;
			this.m_log_ui.Title = "Log";
			// 
			// m_btn_play
			// 
			this.m_btn_play.Location = new System.Drawing.Point(262, 53);
			this.m_btn_play.Name = "m_btn_play";
			this.m_btn_play.Size = new System.Drawing.Size(64, 23);
			this.m_btn_play.TabIndex = 2;
			this.m_btn_play.Text = "Play";
			this.m_btn_play.UseVisualStyleBackColor = true;
			// 
			// m_tb_wav_filepath
			// 
			this.m_tb_wav_filepath.Location = new System.Drawing.Point(12, 55);
			this.m_tb_wav_filepath.Name = "m_tb_wav_filepath";
			this.m_tb_wav_filepath.Size = new System.Drawing.Size(241, 20);
			this.m_tb_wav_filepath.TabIndex = 3;
			// 
			// m_btn_make_instrument
			// 
			this.m_btn_make_instrument.Location = new System.Drawing.Point(12, 81);
			this.m_btn_make_instrument.Name = "m_btn_make_instrument";
			this.m_btn_make_instrument.Size = new System.Drawing.Size(118, 23);
			this.m_btn_make_instrument.TabIndex = 4;
			this.m_btn_make_instrument.Text = "Make Instrument";
			this.m_btn_make_instrument.UseVisualStyleBackColor = true;
			// 
			// MidiUI
			// 
			this.ClientSize = new System.Drawing.Size(338, 374);
			this.Controls.Add(this.m_btn_make_instrument);
			this.Controls.Add(this.m_tb_wav_filepath);
			this.Controls.Add(this.m_btn_play);
			this.Controls.Add(this.m_log_ui);
			this.Controls.Add(this.m_lbl_version);
			this.Name = "MidiUI";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
