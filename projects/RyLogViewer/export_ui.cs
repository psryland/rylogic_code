using System.Windows.Forms;
using pr.common;

namespace RyLogViewer
{
	public partial class ExportUI :Form
	{
		public enum EContent
		{
			WholeFile,
			Selection,
			ByteRange
		}
		
		/// <summary>What to export</summary>
		public EContent Content { get; set; }

		/// <summary>If Content is byte range, export this range</summary>
		public Range ByteRange { get; set; }

		public ExportUI(Range file_byte_range)
		{
			InitializeComponent();
			ByteRange = file_byte_range;
			Content = EContent.WholeFile;
			
			// Radio buttons
			m_radio_whole_file.Checked = Content == EContent.WholeFile;
			m_radio_whole_file.CheckedChanged += (s,a)=>
				{
					Content = EContent.WholeFile;
					UpdateUI();
				};
			m_radio_selection.Checked = Content == EContent.Selection;
			m_radio_selection.CheckedChanged += (s,a)=>
				{
					Content = EContent.Selection;
					UpdateUI();
				};
			m_radio_range.Checked = Content == EContent.ByteRange;
			m_radio_range.CheckedChanged += (s,a)=>
				{
					Content = EContent.ByteRange;
					UpdateUI();
				};
			
			UpdateUI();
		}

		private void UpdateUI()
		{
			bool byte_range = Content == EContent.ByteRange;
			m_btn_range_to_start.Enabled = byte_range;
			m_btn_range_to_end  .Enabled = byte_range;
			m_spinner_range_min .Enabled = byte_range;
			m_spinner_range_max .Enabled = byte_range;
		}
	}
}
