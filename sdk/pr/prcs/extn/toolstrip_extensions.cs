using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

namespace pr.extn
{
	public static class ToolStripExtensions
	{
		/// <summary>Exports location data for this tool strip container</summary>
		public static ToolStripLocations SaveLocations(this ToolStripContainer cont)
		{
			return new ToolStripLocations(cont);
		}

		/// <summary>Imports location data for this tool strip container</summary>
		public static void LoadLocations(this ToolStripContainer cont, ToolStripLocations data)
		{
			data.Apply(cont);
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public static void SetText(this ToolStripComboBox cb, string text)
		{
			var idx = cb.SelectionStart;
			cb.SelectedText = text;
			cb.SelectionStart = idx + text.Length;
		}

		/// <summary>Set the tooltip for this tool strip item</summary>
		public static void ToolTip(this ToolStripItem ctrl, ToolTip tt, string caption)
		{
			// Don't need 'tt', this method is just for consistency with the other overload
			ctrl.ToolTipText = caption;
		}

		/// <summary>Display the hint balloon.</summary>
		public static void ShowHintBalloon(this ToolStripItem item, ToolTip tt, string msg, int duration = 5000)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return;
			var pt = item.Bounds.Centre();

			tt.SetToolTip(parent, msg);
			tt.Show(msg, parent, pt, duration);
			tt.BeginInvokeDelayed(duration, () => tt.SetToolTip(parent, null));
		}

		/// <summary>Returns the location of this item in screen space</summary>
		public static Point ScreenLocation(this ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return item.Bounds.Location;
			return parent.PointToScreen(item.Bounds.Location);
		}

		/// <summary>Returns the bounds of this item in form space</summary>
		public static Rectangle ParentFormRectangle(this ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			var top    = parent != null ? parent.TopLevelControl : null;
			var srect  = parent == null ? item.Bounds : parent.RectangleToScreen(item.Bounds);
			return top != null ? top.RectangleToClient(srect) : srect;
		}

		/// <summary>Returns the bounds of this item in screen space</summary>
		public static Rectangle ScreenRectangle(this ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return item.Bounds;
			return parent.RectangleToScreen(item.Bounds);
		}

		/// <summary>Create a message that displays for a period then disappears. Use null or "" to hide the status</summary>
		public static void SetStatusMessage(this ToolStripStatusLabel status, string text, string idle = null, bool bold = false, Color? frcol = null, Color? bkcol = null, TimeSpan? display_time_ms = null)
		{
			status.Text = text ?? string.Empty;
			status.Visible = text.HasValue();
			status.ForeColor = frcol ?? SystemColors.ControlText;
			status.BackColor = bkcol ?? SystemColors.Control;
			if (status.Font.Bold != bold)
				status.Font = new Font(status.Font, bold ? FontStyle.Bold : FontStyle.Regular);

			// If the status message has a timer already, dispose it
			var timer = status.Tag as Timer;
			if (timer != null)
			{
				timer.Dispose();
				status.Tag = null;
			}

			if (!text.HasValue() || display_time_ms == null)
				return;

			// Attach a new timer to the status message
			status.Tag = timer = new Timer{Enabled = true, Interval = (int)display_time_ms.Value.TotalMilliseconds};
			timer.Tick += (s,a)=>
				{
					// When the timer fires, if we're still associated with
					// the status message, null out the text and remove our self
					if (s != status.Tag) return;
					SetStatusMessage(status, idle);
				};
		}
		public static void SetStatusMessage(this ToolStripStatusLabel status, string text, bool bold, Color frcol, Color bkcol)
		{
			status.SetStatusMessage(text, null, bold, frcol, bkcol, TimeSpan.FromSeconds(2));
		}
	}

	/// <summary>Used to persist control locations and sizes in xml</summary>
	public class ToolStripLocations
	{
		private static class Tag
		{
			public const string Name     = "name";
			public const string Top      = "top";
			public const string Left     = "left";
			public const string Right    = "right";
			public const string Bottom   = "bottom";
			public const string TopVis   = "top_visible";
			public const string LeftVis  = "left_visible";
			public const string RightVis = "right_visible";
			public const string BotVis   = "bottom_visible";
		}
		private string m_name;
		private ControlLocations m_top;
		private ControlLocations m_left;
		private ControlLocations m_right;
		private ControlLocations m_bottom;
		private bool m_top_vis;
		private bool m_left_vis;
		private bool m_right_vis;
		private bool m_bot_vis;

		public ToolStripLocations()
		{
			m_name      = string.Empty;
			m_top       = new ControlLocations();
			m_left      = new ControlLocations();
			m_right     = new ControlLocations();
			m_bottom    = new ControlLocations();
			m_top_vis   = true;
			m_left_vis  = true;
			m_right_vis = true;
			m_bot_vis   = true;
		}
		public ToolStripLocations(ToolStripContainer cont)
		{
			Read(cont);
		}
		public ToolStripLocations(XElement node)
		{
			m_name      = node.Element(Tag.Name    ).As<string>();
			m_top       = node.Element(Tag.Top     ).As<ControlLocations>();
			m_left      = node.Element(Tag.Left    ).As<ControlLocations>();
			m_right     = node.Element(Tag.Right   ).As<ControlLocations>();
			m_bottom    = node.Element(Tag.Bottom  ).As<ControlLocations>();
			m_top_vis   = node.Element(Tag.TopVis  ).As<bool>();
			m_left_vis  = node.Element(Tag.LeftVis ).As<bool>();
			m_right_vis = node.Element(Tag.RightVis).As<bool>();
			m_bot_vis   = node.Element(Tag.BotVis  ).As<bool>();
		}
		public XElement ToXml(XElement node)
		{
			node.Add(
				m_name .    ToXml(Tag.Name    ,false),
				m_top      .ToXml(Tag.Top     ,false),
				m_left     .ToXml(Tag.Left    ,false),
				m_right    .ToXml(Tag.Right   ,false),
				m_bottom   .ToXml(Tag.Bottom  ,false),
				m_top_vis  .ToXml(Tag.TopVis  ,false),
				m_left_vis .ToXml(Tag.LeftVis ,false),
				m_right_vis.ToXml(Tag.RightVis,false),
				m_bot_vis  .ToXml(Tag.BotVis  ,false));
			return node;
		}
		public void Read(ToolStripContainer cont)
		{
			m_name      = cont.Name;
			m_top       = new ControlLocations(cont.TopToolStripPanel   );
			m_left      = new ControlLocations(cont.LeftToolStripPanel  );
			m_right     = new ControlLocations(cont.RightToolStripPanel );
			m_bottom    = new ControlLocations(cont.BottomToolStripPanel);
			m_top_vis   = cont.TopToolStripPanelVisible;
			m_left_vis  = cont.LeftToolStripPanelVisible;
			m_right_vis = cont.RightToolStripPanelVisible;
			m_bot_vis   = cont.BottomToolStripPanelVisible;
		}
		public void Apply(ToolStripContainer cont)
		{
			if (m_name != cont.Name) return;
			cont.TopToolStripPanelVisible    = m_top_vis;
			cont.LeftToolStripPanelVisible   = m_left_vis;
			cont.RightToolStripPanelVisible  = m_right_vis;
			cont.BottomToolStripPanelVisible = m_bot_vis;
			m_top   .Apply(cont.TopToolStripPanel   );
			m_left  .Apply(cont.LeftToolStripPanel  );
			m_right .Apply(cont.RightToolStripPanel );
			m_bottom.Apply(cont.BottomToolStripPanel);
		}
	}
}
