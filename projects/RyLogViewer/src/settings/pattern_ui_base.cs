using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace RyLogViewer
{
	public abstract class PatternUIBase<TPattern> :UserControl ,IPatternUI where TPattern:class, IPattern, new()
	{
		protected readonly ToolTip m_tt;
		protected TPattern m_original;
		protected TPattern m_pattern;

		protected PatternUIBase()
		{
			m_tt = new ToolTip();
			m_original = null;
			m_pattern = null;

			VisibleChanged += (s,a)=>
				{
					if (!Visible) return;
					UpdateUI();
				};
		}

		/// <summary>The pattern being edited by this UI</summary>
		[Browsable(false)] public TPattern Pattern { get { return m_pattern; } }
		IPattern IPatternUI.Pattern { get { return Pattern; } }

		/// <summary>The original pattern provided to the ui for editing</summary>
		[Browsable(false)] public TPattern Original { get { return m_original; } }
		IPattern IPatternUI.Original { get { return Original; } }

		/// <summary>True if the pattern currently contained is a new instance, vs editing an existing pattern</summary>
		public bool IsNew { get { return ReferenceEquals(m_original,null); } }

		/// <summary>Set a new pattern for the UI</summary>
		public void NewPattern(IPattern pattern)
		{
			m_original = null;
			m_pattern = (TPattern)pattern;
			UpdateUI();
		}

		/// <summary>Select a pattern into the UI for editting</summary>
		public void EditPattern(IPattern pattern)
		{
			m_original = (TPattern)pattern;
			m_pattern  = (TPattern)m_original.Clone();
			UpdateUI();
		}

		/// <summary>True when user activity has changed something in the ui</summary>
		public bool Touched { get; set; }

		/// <summary>True if the pattern contains unsaved changes</summary>
		public bool HasUnsavedChanges
		{
			get
			{
				return (IsNew && m_pattern.Expr.Length != 0 && Touched)
					|| (!IsNew && !Equals(m_original, m_pattern));
			}
		}

		/// <summary>True if the contained pattern is valid and therefore can be saved</summary>
		public bool CommitEnabled
		{
			get { return m_pattern != null && m_pattern.IsValid; }
		}

		/// <summary>Raised when the 'Commit' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Commit;
		protected void RaiseCommitEvent()
		{
			if (Commit == null) return;
			Commit(this, EventArgs.Empty);
		}

		/// <summary>Access to the test text field</summary>
		public abstract string TestText { get; set ;}

		/// <summary>Set focus to the primary input field</summary>
		public abstract void FocusInput();

		/// <summary>Prevents reentrant calls to UpdateUI. Yes this is the best way to do it /cry</summary>
		protected bool m_in_update_ui;
		protected void UpdateUI()
		{
			if (m_in_update_ui) return;
			try
			{
				m_in_update_ui = true;
				SuspendLayout();

				UpdateUIInternal();
			}
			finally
			{
				m_in_update_ui = false;
				ResumeLayout();
			}
		}

		/// <summary>Update the UI elements based on the current pattern</summary>
		protected abstract void UpdateUIInternal();
	}
}
