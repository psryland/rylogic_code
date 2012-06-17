using System.Windows.Forms;

namespace RyLogViewer
{
	public partial class GhostModeUI :Form
	{
		private readonly Main m_main;

		/// <summary>Transparent to mouseclicks</summary>
		public bool ClickThru { get; set; }

		/// <summary>The level of transparency</summary>
		public float Alpha { get; set; }

		public GhostModeUI(Main main)
		{
			InitializeComponent();
			m_main = main;
			
			// Click through
			m_check_click_thru.Checked = ClickThru;
			m_check_click_thru.Click += (s,a)=>
				{
					ClickThru = m_check_click_thru.Checked;
				};

			// Transparency track
			m_track_opacity.ValueChanged += (s,a)=>
				{
					Alpha = m_track_opacity.Value * 0.01f;
					m_main.Opacity = Alpha;
				};
			FormClosing += (s,a)=>
				{
					m_main.Opacity = 1.0f;
				};
		}
	}
}
