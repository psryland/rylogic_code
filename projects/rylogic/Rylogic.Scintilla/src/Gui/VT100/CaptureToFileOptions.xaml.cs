using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Rylogic.Common;

namespace Rylogic.Gui.WPF
{
	public partial class VT100CaptureToFileOptions : Window
	{
		public VT100CaptureToFileOptions(VT100.Settings settings)
		{
			InitializeComponent();
			Settings = settings;
			m_browse_path.HistoryString = settings.CaptureFilepathHistory;

			Accept = Command.Create(this, AcceptInternal);
			Cancel = Command.Create(this, CancelInternal);
			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			Settings.CaptureFilepathHistory = m_browse_path.HistoryString;
			base.OnClosed(e);
		}

		/// <summary>VT100 terminal settings</summary>
		private VT100.Settings Settings { get; }

		/// <summary>The selected filepath to save the captured data</summary>
		public string FileName { get; set; } = string.Empty;

		/// <summary>True if all data should be captured</summary>
		public bool BinaryCapture { get; set; }

		/// <summary>Show the dialog modally</summary>
		public bool? ShowDialog(DependencyObject dp)
		{
			Owner = GetWindow(dp);
			return ShowDialog();
		}

		/// <summary>Positive result</summary>
		public Command Accept { get; }
		private void AcceptInternal(object? x)
		{
			DialogResult = true;
			Close();
		}

		/// <summary>Negative result</summary>
		public Command Cancel { get; }
		private void CancelInternal(object? x)
		{
			DialogResult = false;
			Close();
		}
	}
}
