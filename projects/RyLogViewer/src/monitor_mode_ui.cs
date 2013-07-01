using System;
using System.Windows.Forms;
using pr.extn;
using pr.maths;

namespace RyLogViewer
{
	public partial class MonitorModeUI :Form
	{
		private readonly Main m_main;
		private readonly ToolTip m_tt;
		private bool m_always_on_top;
		private bool m_click_through;
		private float m_alpha;

		/// <summary>Always above other windows</summary>
		public bool AlwaysOnTop
		{
			get { return m_always_on_top; }
			set { m_always_on_top = m_check_always_on_top.Checked = value; }
		}

		/// <summary>Transparent to mouse clicks</summary>
		public bool ClickThru
		{
			get { return m_click_through; }
			set { m_click_through = m_check_click_thru.Checked = value; }
		}

		/// <summary>The level of transparency</summary>
		public float Alpha
		{
			get { return m_alpha; }
			set
			{
				if (Math.Abs(m_alpha - value) < float.Epsilon) return;
				m_alpha = Maths.Clamp(value, 0f, 1f);
				m_track_opacity.Value = (int)Maths.Clamp(m_alpha * 100f, m_track_opacity.Minimum, m_track_opacity.Maximum);
			}
		}

		public MonitorModeUI(Main main)
		{
			InitializeComponent();
			m_main = main;
			m_tt = new ToolTip();

			// Always on top
			AlwaysOnTop = false;
			m_check_always_on_top.ToolTip(m_tt, "Check to make RyLogViewer always visible above other windows");
			m_check_always_on_top.CheckedChanged += (s,a) =>
				{
					AlwaysOnTop = m_check_always_on_top.Checked;
				};

			// Click through
			ClickThru = false;
			m_check_click_thru.ToolTip(m_tt, "Check to make the window invisible to user input.\r\nCancel this mode by clicking on the system tray icon");
			m_check_click_thru.Click += (s,a)=>
				{
					ClickThru = m_check_click_thru.Checked;
				};

			// Transparency track
			Alpha = 1f;
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
