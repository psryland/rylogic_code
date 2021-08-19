using System.ComponentModel;
using System.Windows;

namespace TestWPF
{
	public partial class BitArrayUI :Window, INotifyPropertyChanged
	{
		public BitArrayUI()
		{
			InitializeComponent();
			Bitmask = 0xA5A55A5A;
			DataContext = this;
		}

		public ulong Bitmask
		{
			get => m_bitmask;
			set
			{
				if (m_bitmask == value) return;
				m_bitmask = value;
				NotifyPropertyChanged(nameof(Bitmask));
			}
		}
		private ulong m_bitmask;

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
