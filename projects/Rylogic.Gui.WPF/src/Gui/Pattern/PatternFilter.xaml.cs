using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Rylogic.Common;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class PatternFilter : UserControl, INotifyPropertyChanged
	{
		// Notes:
		//  - 'Pattern' is a Rylogic.Core type, so does not have any WPF specific stuff.

		public PatternFilter()
		{
			InitializeComponent();

			Pattern = new Pattern(EPattern.Substring, string.Empty);
			History = new ListCollectionView(new ObservableCollection<Pattern>());
			ShowHistoryList = WPF.Command.Create(this, ShowHistoryListInternal);
			CloseHistoryList = WPF.Command.Create(this, CloseHistoryListInternal);
			SelectFromHistory = WPF.Command.Create(this, SelectFromHistoryInternal);
			CommitPattern = WPF.Command.Create(this, CommitPatternInternal);
			EditPattern = WPF.Command.Create(this, EditPatternInternal);
			m_root.DataContext = this;
		}

		/// <summary>The current pattern</summary>
		public Pattern Pattern
		{
			get => m_pattern;
			set
			{
				if (m_pattern == value) return;
				m_pattern = value ?? new Pattern(EPattern.Substring, string.Empty);
				NotifyPropertyChanged(nameof(Pattern));
			}
		}
		private Pattern m_pattern = null!;

		/// <summary>The history of filters</summary>
		public ICollectionView History { get;  }
		private ObservableCollection<Pattern> HistoryList => (ObservableCollection<Pattern>)History.SourceCollection;

		/// <summary>The command to execute when the pattern is commited</summary>
		public ICommand	Command
		{
			get => (ICommand)GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}
		public static readonly DependencyProperty CommandProperty = Gui_.DPRegister<PatternFilter>(nameof(Command), flags:FrameworkPropertyMetadataOptions.None);

		/// <summary>Parameter to pass to 'Command'</summary>
		public object CommandParameter
		{
			get => GetValue(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
		}
		public static readonly DependencyProperty CommandParameterProperty = Gui_.DPRegister<PatternFilter>(nameof(CommandParameter));

		/// <summary>Pop open the history list</summary>
		public Command ShowHistoryList { get; }
		private void ShowHistoryListInternal()
		{
			PART_HistoryPopup.IsOpen = true;
			PART_HistoryList.Focus();
		}

		/// <summary>Pop open the history list</summary>
		public Command CloseHistoryList { get; }
		private void CloseHistoryListInternal()
		{
			PART_HistoryPopup.IsOpen = false;
			PART_TextBox.Focus();
		}

		/// <summary>Make the current history item the 'Pattern'</summary>
		public Command SelectFromHistory { get; }
		private void SelectFromHistoryInternal()
		{
			if (History.CurrentItem is Pattern pattern)
			{
				Pattern = new Pattern(pattern);
				CloseHistoryList.Execute();
			}
		}

		/// <summary>Apply the current pattern</summary>
		public Command CommitPattern { get; }
		private void CommitPatternInternal()
		{
			// Save the pattern to the history
			if (!string.IsNullOrEmpty(Pattern.Expr))
				Util.AddToHistoryList(HistoryList, new Pattern(Pattern), 10);

			// Call the 'Command'
			if (Command != null && Command.CanExecute(null))
				Command.Execute(CommandParameter ?? Pattern);
		}

		/// <summary>Display the pattern editor</summary>
		public Command EditPattern { get; }
		private void EditPatternInternal()
		{
			var dlg = new PatternEditorUI { Owner = Window.GetWindow(this) };
			dlg.Editor.EditPattern(Pattern);
			if (dlg.ShowDialog() == true)
			{
				Pattern = (Pattern)dlg.Editor.Pattern;
				CommitPattern.Execute();
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
