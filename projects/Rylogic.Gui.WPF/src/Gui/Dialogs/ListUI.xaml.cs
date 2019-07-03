using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Rylogic.Gui.WPF
{
	public partial class ListUI : Window, INotifyPropertyChanged
	{
		static ListUI()
		{
			PromptProperty = Gui_.DPRegister<ListUI>(nameof(Prompt));
			AllowCancelProperty = Gui_.DPRegister<ListUI>(nameof(AllowCancel));
		}
		public ListUI(Window owner = null)
		{
			InitializeComponent();
			Owner = owner;
			Icon = Owner?.Icon;
			AllowCancel = false;
			Items = new ObservableCollection<object>();
			DataContext = this;

			Accept = Command.Create(this, () =>
			{
				if (!IsValid) return;
				DialogResult = true;
				Close();
			});
			m_list.SelectionChanged += (s, a) =>
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
			};
			m_list.PreviewMouseDoubleClick += (s, a) =>
			{
				Accept.Execute();
			};
		}

		/// <summary>Accept the current selection</summary>
		public Command Accept { get; }

		/// <summary>The selection mode for the list</summary>
		public SelectionMode SelectionMode
		{
			get => m_list.SelectionMode;
			set => m_list.SelectionMode = value;
		}

		/// <summary>Get/Set the property to display</summary>
		public string DisplayMember
		{
			get => m_list.DisplayMemberPath;
			set => m_list.DisplayMemberPath = value;
		}

		/// <summary>The prompt text</summary>
		public string Prompt
		{
			get { return (string)GetValue(PromptProperty); }
			set { SetValue(PromptProperty, value); }
		}
		public static readonly DependencyProperty PromptProperty;

		/// <summary>True if the cancel button is displayed</summary>
		public bool AllowCancel
		{
			get { return (bool)GetValue(AllowCancelProperty); }
			set { SetValue(AllowCancelProperty, value); }
		}
		public static readonly DependencyProperty AllowCancelProperty;

		/// <summary>Items displayed in the list</summary>
		public ObservableCollection<object> Items { get; }

		/// <summary>The item selected in the list</summary>
		public object SelectedItem => m_list.SelectedItem;

		/// <summary>The items selected in the list</summary>
		public IList SelectedItems => m_list.SelectedItems;

		/// <summary>True if the user input is valid</summary>
		public bool IsValid
		{
			get
			{
				switch (SelectionMode)
				{
				default: throw new Exception("Unknown selection mode");
				case SelectionMode.Single:
					return m_list.SelectedItem != null;
				case SelectionMode.Multiple:
				case SelectionMode.Extended:
					return m_list.SelectedItems.Count > 0;
				}
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
