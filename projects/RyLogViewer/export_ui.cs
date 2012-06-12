using System.Windows.Forms;
using pr.common;

namespace RyLogViewer
{
	public partial class ExportUI :Form
	{
		public enum EMode
		{
			WholeFile,
			Selection,
			ByteRange
		}
		
		public EMode ExportType
		{
			get;
			set;
		}

		public Range ByteRange
		{
			get;
			set;
		}

		public ExportUI(Range file_byte_range)
		{
			InitializeComponent();
			ByteRange = file_byte_range;
			ExportType = EMode.WholeFile;
			
			// Radio buttons
			m_radio_whole_file.Checked = ExportType == EMode.WholeFile;
			m_radio_whole_file.CheckedChanged += (s,a)=>
				{
					ExportType = EMode.WholeFile;
					UpdateUI();
				};
			m_radio_selection.Checked = ExportType == EMode.Selection;
			m_radio_selection.CheckedChanged += (s,a)=>
				{
					ExportType = EMode.Selection;
					UpdateUI();
				};
			m_radio_range.Checked = ExportType == EMode.ByteRange;
			m_radio_range.CheckedChanged += (s,a)=>
				{
					ExportType = EMode.ByteRange;
					UpdateUI();
				};
			
			UpdateUI();
		}

		private void UpdateUI()
		{
			bool byte_range = ExportType == EMode.ByteRange;
			m_btn_range_to_start.Enabled = byte_range;
			m_btn_range_to_end  .Enabled = byte_range;
			m_spinner_range_min .Enabled = byte_range;
			m_spinner_range_max .Enabled = byte_range;
		}
	}
}
