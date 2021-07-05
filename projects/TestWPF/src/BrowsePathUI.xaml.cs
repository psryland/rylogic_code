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
			PathHistory = CollectionViewSource.GetDefaultView(new ObservableCollection<string>{"C:\\Path1\\File1.txt", "D:\\Path2\\File2.txt"});
			Accept = Command.Create(this, Close);
			DataContext = this;
		}

		/// <summary></summary>
		public string Path
		{
			get => m_path;
			set
			{
				if (m_path == value) return;
				m_path = value;
				NotifyPropertyChanged(nameof(Path));
			}
		}
		private string m_path = string.Empty;

		/// <summary></summary>
		public ICollectionView PathHistory { get; }

		/// <summary></summary>
		public bool MustExist
		{
			get => m_must_exist;
			set
			{
				if (m_must_exist == value) return;
				m_must_exist = value;
				NotifyPropertyChanged(nameof(MustExist));
			}
		}
		private bool m_must_exist;

		/// <summary></summary>
		public bool RequireRoot
		{
			get => m_require_root;
			set
			{
				if (m_require_root == value) return;
				m_require_root = value;
				NotifyPropertyChanged(nameof(RequireRoot));
			}
		}
		private bool m_require_root;

		/// <summary>Close the dialog</summary>
		public Command Accept { get; }

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
