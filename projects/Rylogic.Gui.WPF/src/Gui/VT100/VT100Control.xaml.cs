using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rylogic.Common;

namespace Rylogic.Gui.WPF
{
	public partial class VT100Control :UserControl, IDisposable, INotifyPropertyChanged
	{
		public VT100Control()
		{
			InitializeComponent();
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			Buffer = null;
		}

		/// <summary>The underlying vt100 virtual display buffer</summary>
		public VT100.Buffer? Buffer
		{
			get => m_buffer;
			set
			{
				if (m_buffer == value) return;
				m_buffer = value;
				NotifyPropertyChanged(nameof(Buffer));
			}
		}
		private VT100.Buffer? m_buffer;

		/// <summary></summary>
		public VT100.Settings? Settings => Buffer?.Settings;

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
