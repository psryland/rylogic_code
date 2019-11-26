using System;
using System.Windows;

namespace Rylogic.Gui.WPF
{
	public partial class PatternEditorUI : Window
	{
		public PatternEditorUI()
		{
			InitializeComponent();
			Editor = Gui_.FindVisualChild<PatternEditor>((DependencyObject)Content) ?? throw new Exception("Expected a pattern editor child control");
			Cancel = Command.Create(this, CancelInternal);
			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>The contained pattern editor</summary>
		public PatternEditor Editor
		{
			get => m_editor;
			private set
			{
				if (m_editor == value) return;
				if (m_editor != null)
				{
					m_editor.Commit -= HandleCommit;
				}
				m_editor = value;
				if (m_editor != null)
				{
					m_editor.Commit += HandleCommit;
				}

				// Handle Commit
				void HandleCommit(object sender, EventArgs e)
				{
					if (StayOpen) return;
					Accept.Execute();
				}
			}
		}
		private PatternEditor m_editor = null!;

		/// <summary>Stay open when the commit button is clicked</summary>
		public bool StayOpen
		{
			get => (bool)GetValue(StayOpenProperty);
			set => SetValue(StayOpenProperty, value);
		}
		public static readonly DependencyProperty StayOpenProperty = Gui_.DPRegister<PatternEditorUI>(nameof(StayOpen));

		/// <summary>Close the window</summary>
		public Command Cancel { get; }
		private void CancelInternal()
		{
			if (this.IsModal())
				DialogResult = false;

			Close();
		}

		/// <summary>Close the window</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			if (this.IsModal())
				DialogResult = true;

			Close();
		}
	}
}
