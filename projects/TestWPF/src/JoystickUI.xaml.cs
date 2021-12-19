using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rylogic.Gui.WPF;

namespace TestWPF
{
	public partial class JoystickUI :Window, INotifyPropertyChanged
	{
		public JoystickUI()
		{
			InitializeComponent();
			DataContext = this;
			MovePosition = Command.Create(this, MovePositionInternal);
		}

		/// <summary></summary>
		public string Position => m_position.ToString();
		private Point m_position;

		/// <summary></summary>
		public ICommand MovePosition { get; }
		private void MovePositionInternal(object? value)
		{
			if (value is Vector dir)
				m_position += dir;
			NotifyPropertyChanged(nameof(Position));
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
