using System;
using System.Windows;
using Rylogic.Gui.WPF;

namespace Rylogic.TextAligner
{
	public partial class PatternEditorUI :Window
	{
		// Notes:
		//  - This editor UI is mainly a wrapper for 'PatternEditor' with an added TextBox
		//    for the comment text. When the editor is closed, use 'Editor.Pattern' to update
		//    the AlignPattern in the group.

		public PatternEditorUI(UIElement owner, AlignPattern pattern)
		{
			InitializeComponent();
			Owner = Window.GetWindow(owner);
			Editor = Gui_.FindVisualChild<PatternEditor>((DependencyObject)Content) ?? throw new Exception("Expected a pattern editor child control");
			Editor.EditPattern(pattern, clone:false);

			Cancel = Command.Create(this, CancelInternal);
			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>The editor control</summary>
		public PatternEditor Editor
		{
			get => m_editor;
			set
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

				// Handler
				void HandleCommit(object sender, EventArgs e)
				{
					Accept.Execute();
				}
			}
		}
		private PatternEditor m_editor = null!;

		/// <summary>The pattern comment text</summary>
		public string Comment
		{
			get => ((AlignPattern)Editor.Pattern).Comment;
			set => ((AlignPattern)Editor.Pattern).Comment = value;
		}

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
