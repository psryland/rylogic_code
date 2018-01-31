using System;
using System.Windows.Forms;
using Rylogic.Extn;

namespace Rylogic.VSExtension
{
	internal partial class EditPatternDlg :Form
	{
		/// <summary>The pattern being edited</summary>
		public AlignPattern Pattern { get { return (AlignPattern)m_pattern_ui.Original; } }

		/// <summary>Raised when the 'Commit' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Commit
		{
			add { m_pattern_ui.Commit += value; }
			remove { m_pattern_ui.Commit -= value; }
		}

		public EditPatternDlg(AlignPattern pat)
		{
			InitializeComponent();
			if (pat != null)
			{
				m_pattern_ui.EditPattern(pat);
			}
			else
			{
				m_pattern_ui.NewPattern(new AlignPattern());
			}

			pat = (AlignPattern)m_pattern_ui.Pattern;

			m_edit_comment.Text = pat.Comment.HasValue() ? pat.Comment : "Enter a reminder comment here";
			m_edit_comment.TextChanged += (s,a) =>
				{
					pat.Comment = m_edit_comment.Text;
				};

			Shown += (s,a) =>
				{
					CenterToParent();
				};
		}
	}
}
