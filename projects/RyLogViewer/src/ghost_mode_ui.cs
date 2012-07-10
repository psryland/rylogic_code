using System.Windows.Forms;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	public partial class GhostModeUI :Form
	{
		private readonly Main m_main;
		private readonly ToolTip m_tt;
		private float m_alpha;

		/// <summary>Transparent to mouseclicks</summary>
		public bool ClickThru { get; set; }

		/// <summary>The level of transparency</summary>
		public float Alpha
		{
			get { return m_alpha; }
			set { m_alpha = Maths.Clamp(value, 0f, 1f); }
		}

		public GhostModeUI(Main main)
		{
			InitializeComponent();
			m_main = main;
			m_tt = new ToolTip();
			m_alpha = 1f;
			
			// Click through
			m_check_click_thru.ToolTip(m_tt, "Check to make the window invisible to user input.\r\nCancel this mode by clicking on the system tray icon");
			m_check_click_thru.Checked = ClickThru;
			m_check_click_thru.Click += (s,a)=>
				{
					ClickThru = m_check_click_thru.Checked;
				};

			// Transparency track
			m_track_opacity.ToolTip(m_tt, "The transparency of the main log view window");
			m_track_opacity.ValueChanged += (s,a)=>
				{
					Alpha = m_track_opacity.Value * 0.01f;
					m_main.Opacity = Alpha;
				};
			FormClosing += (s,a)=>
				{
					m_main.Opacity = 1f;
				};
		}
	}
}
