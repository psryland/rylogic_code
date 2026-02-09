using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Rylogic.Gui.WPF;

namespace TestWPF
{
	public partial class BrowsePathUI :Window, INotifyPropertyChanged
	{
		public BrowsePathUI()
		{
			InitializeComponent();
			Path = string.Empty;
			MustExist = false;
			RequireRoot = false;
			PathHistory = new ObservableCollection<string>{"C:\\Path1\\File1.txt", "D:\\Path2\\File2.txt"};
			Accept = Command.Create(this, Close);
			DataContext = this;
		}

		/// <summary></summary>
		public string Path
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(Path));
			}
		} = string.Empty;

		/// <summary></summary>
		public ObservableCollection<string> PathHistory { get; }

		/// <summary></summary>
		public bool MustExist
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(MustExist));
			}
		}

		/// <summary></summary>
		public bool RequireRoot
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(RequireRoot));
			}
		}

		/// <summary>Close the dialog</summary>
		public Command Accept { get; }

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
