using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class PatternFilter : UserControl, INotifyPropertyChanged
	{
		public PatternFilter()
		{
			InitializeComponent();

			Pattern = new Pattern(EPattern.Substring, string.Empty);
			History = new BindingListEx<Pattern>();
			EditPattern = Command.Create(this, () =>
			{
				var dlg = new PatternEditorUI { Owner = Window.GetWindow(this) };
				dlg.Editor.EditPattern(Pattern);
				if (dlg.ShowDialog() == true)
					Pattern = (Pattern)dlg.Editor.Pattern;
			});
			
			DataContext = this;
		}

		/// <summary>The current pattern</summary>
		public Pattern Pattern
		{
			get { return m_pattern; }
			set
			{
				if (m_pattern != value)
				{
					m_pattern = value ?? new Pattern(EPattern.Substring, string.Empty);

					// Save the pattern to the history
					if (!string.IsNullOrEmpty(m_pattern.Expr))
						Util.AddToHistoryList(History, new Pattern(m_pattern), false, 10);
				}

				// Notify of a new/changed pattern
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pattern)));
			}
		}
		private Pattern m_pattern;

		/// <summary>The history of filters</summary>
		public BindingListEx<Pattern> History { get;  }

		/// <summary>Display the pattern editor</summary>
		public Command EditPattern { get; }

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
