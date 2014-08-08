using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.util;

namespace pr.extn
{
	public static class ToolStripExtensions
	{
		/// <summary>Exports location data for this tool strip container</summary>
		public static ToolStripLocations SaveLocations(this ToolStripContainer cont)
		{
			using (cont.MarkAsSaving())
				return new ToolStripLocations(cont);
		}

		/// <summary>Imports location data for this tool strip container</summary>
		public static void LoadLocations(this ToolStripContainer cont, ToolStripLocations data)
		{
			using (cont.MarkAsLoading())
				data.Apply(cont);
		}

		/// <summary>Returns the Top,Left,Right,Bottom panels</summary>
		public static IEnumerable<ToolStripPanel> Panels(this ToolStripContainer cont)
		{
			yield return cont.TopToolStripPanel;
			yield return cont.LeftToolStripPanel;
			yield return cont.RightToolStripPanel;
			yield return cont.BottomToolStripPanel;
		}

		/// <summary>Returns all contained ToolStrips within the Top,Left,Right,Bottom panels</summary>
		public static IEnumerable<ToolStrip> ToolStrips(this ToolStripContainer cont)
		{
			foreach (var panel in cont.Panels())
			foreach (var ts in panel.Controls.Cast<ToolStrip>())
				yield return ts;
		}

		/// <summary>Returns all contained toolbar items within the Top,Left,Right,Bottom panels</summary>
		public static IEnumerable<ToolStripItem> ToolStripItems(this ToolStripContainer cont)
		{
			foreach (var ts in cont.ToolStrips())
			foreach (var item in ts.Items.Cast<ToolStripItem>())
				yield return item;
		}

		/// <summary>Recursively search the collection removing any items with a Name property equal to 'key'</summary>
		public static void RemoveByKey(this ToolStripItemCollection cont, string[] keys, bool recursive)
		{
			for (int i = cont.Count; i-- != 0;)
			{
				var item = cont[i];
				if (keys.Contains(item.Name))
					cont.RemoveAt(i);
				else if (item is ToolStripMenuItem && recursive)
					item.As<ToolStripMenuItem>().DropDownItems.RemoveByKey(keys, recursive);
			}
		}
		public static void RemoveByKey(this ToolStripItemCollection cont, string key, bool recursive)
		{
			cont.RemoveByKey(new[]{key}, recursive);
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

		/// <summary>Set a status label with text plus optional colours, detail tooltip, callback click handler, display time</summary>
		public static void SetStatusMessage(this ToolStripStatusLabel status,
			string msg = null, string detail = null, string idle = null,
			bool bold = false, Color? fr_color = null, Color? bk_color = null,
			TimeSpan? display_time_ms = null, EventHandler on_click = null)
		{
			status.Visible = msg.HasValue();

			// Set the text
			status.Text = msg ?? string.Empty;

			// Set the tool tip to the detailed message
			status.ToolTipText = detail ?? string.Empty;

			// Set colours
			status.ForeColor = fr_color ?? SystemColors.ControlText;
			status.BackColor = bk_color ?? SystemColors.Control;

			// Choose the font to use
			var font_style = FontStyle.Regular;
			if (bold            ) font_style |= FontStyle.Bold;
			if (on_click != null) font_style |= FontStyle.Underline;
			if (status.Font.Style != font_style)
				status.Font = new Font(status.Font, font_style);

			// Ensure the status has tag data
			if (status.Tag == null)
			{
				var tag_data = new StatusTagData();
				status.Click += tag_data.HandleStatusClick;
				status.Tag = tag_data;
			}
			else if (!(status.Tag is StatusTagData))
				throw new Exception("Tag property already used for non-status data");

			var data = status.Tag.As<StatusTagData>();

			// If the status message has a timer, dispose it
			Util.Dispose(ref data.m_timer);
			if (display_time_ms != null)
			{
				// If the status has a display time, set a timer
				data.m_timer = new Timer{Enabled = true, Interval = (int)display_time_ms.Value.TotalMilliseconds};
				data.m_timer.Tick += (s,a) =>
					{
						// When the timer fires, if we're still associated with
						// the status message, null out the text and remove our self
						if (!ReferenceEquals(s, data.m_timer)) return;
						status.SetStatusMessage(idle);
					};
			}

			// If a click handler has been provided, subscribe
			data.m_on_click = on_click;
		}

		/// <summary>Data added to the 'Tag' of a status label when used by the SetStatus function</summary>
		private class StatusTagData
		{
			public Timer m_timer;
			public EventHandler m_on_click;
			public void HandleStatusClick(object sender, EventArgs args)
			{
				// Forward to the user handler
				m_on_click.Raise(sender, args);
			}
		}
		public static void SetStatusMessage(this ToolStripStatusLabel status, string text, bool bold, Color frcol, Color bkcol)
		{
			status.SetStatusMessage(text, null, null, bold, frcol, bkcol, TimeSpan.FromSeconds(2), null);
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
		}
		private string m_name;
		private ControlLocations m_top;
		private ControlLocations m_left;
		private ControlLocations m_right;
		private ControlLocations m_bottom;

		public ToolStripLocations()
		{
			m_name      = string.Empty;
			m_top       = new ControlLocations();
			m_left      = new ControlLocations();
			m_right     = new ControlLocations();
			m_bottom    = new ControlLocations();
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
		}
		public XElement ToXml(XElement node)
		{
			node.Add
			(
				m_name  .ToXml(Tag.Name    ,false),
				m_top   .ToXml(Tag.Top     ,false),
				m_left  .ToXml(Tag.Left    ,false),
				m_right .ToXml(Tag.Right   ,false),
				m_bottom.ToXml(Tag.Bottom  ,false)
			);
			return node;
		}
		public void Read(ToolStripContainer cont)
		{
			m_name      = cont.Name;
			m_top       = new ControlLocations(cont.TopToolStripPanel   );
			m_left      = new ControlLocations(cont.LeftToolStripPanel  );
			m_right     = new ControlLocations(cont.RightToolStripPanel );
			m_bottom    = new ControlLocations(cont.BottomToolStripPanel);
		}
		public void Apply(ToolStripContainer cont, bool layout_on_resume = true)
		{
			// If these locations are for a different container, don't apply.
			if (m_name != cont.Name)
			{
				System.Diagnostics.Debug.WriteLine("ToolStripContainer locations ignored due to name mismatch.\nToolStripContainer Name {0} != Layout Data Name {1}".Fmt(cont.Name, m_name));
				return;
			}

			// Apply the layout to each panel
			m_top   .Apply(cont.TopToolStripPanel   );
			m_left  .Apply(cont.LeftToolStripPanel  );
			m_right .Apply(cont.RightToolStripPanel );
			m_bottom.Apply(cont.BottomToolStripPanel);
		}
	}
}
