//***************************************************
// Copyright © Rylogic Ltd 2013
//***************************************************
using System;
using System.Drawing;
using System.Windows.Forms;

namespace pr.gui
{
	/// <summary>A form that overlays another form</summary>
	public sealed class Overlay :Form
	{
		/// <summary>The form being overlaid</summary>
		private Form m_attachee;

		public Overlay(Form attachee = null)
		{
			StartPosition = FormStartPosition.Manual;
			FormBorderStyle = FormBorderStyle.None;
			Dock = DockStyle.Fill;
			Opacity = 0.5;
			ShowInTaskbar = false;
			TransparencyKey = Color.Fuchsia;
			BackColor = Color.LightSkyBlue;

			TransparentBrush = new SolidBrush(TransparencyKey);
			Attachee = attachee;
		}
		protected override void Dispose(bool disposing)
		{
			Attachee = null;
			TransparentBrush.Dispose();
			base.Dispose(disposing);
		}

		/// <summary>The brush to use to draw transparent regions on the overlay</summary>
		public Brush TransparentBrush { get; private set; }

		/// <summary>Assign the form that this overlay overlays</summary>
		public Form Attachee
		{
			get { return m_attachee; }
			set
			{
				if (value == m_attachee) return;
				if (m_attachee != null)
				{
					Owner = null;
					m_attachee.Move -= UpdatePosition;
					m_attachee.Resize -= UpdatePosition;
					m_attachee.Paint -= InvalidateOnPaint;
					m_attachee = null;
				}
				if (value != null)
				{
					m_attachee = value;
					m_attachee.Move += UpdatePosition;
					m_attachee.Resize += UpdatePosition;
					m_attachee.Paint += InvalidateOnPaint;
					Owner = m_attachee;
					UpdatePosition();
				}
			}
		}

		/// <summary>Invalidate this form when the attachee redraws</summary>
		private void InvalidateOnPaint(object sender = null, PaintEventArgs e = null)
		{
			Invalidate();
		}

		/// <summary>Track the position and size of the attachee</summary>
		private void UpdatePosition(object sender = null, EventArgs e = null)
		{
			if (m_attachee == null)
				return;

			switch (m_attachee.WindowState)
			{
			default: throw new ArgumentOutOfRangeException();
			case FormWindowState.Normal:
			case FormWindowState.Maximized:
				Bounds = m_attachee.RectangleToScreen(m_attachee.ClientRectangle);
				Visible = true;
				break;
			case FormWindowState.Minimized:
				Visible = false;
				break;
			}
			BringToFront();
		}
	}

	//public class Overlay :Component
	//{
	//	private Form m_form;

	//	public Overlay(Form attachee = null)
	//	{
	//		InitializeComponent();
	//		Owner = attachee
	//	}
	//	public Overlay(IContainer container)
	//	{
	//		container.Add(this);
	//		InitializeComponent();
	//	}

	//	/// <summary>
	//	/// Raised when the content of the overlay is needed.
	//	/// This event will fire for the form and each control on the form.
	//	/// The graphical overlay component will have already transformed the graphics object
	//	/// to use the form's coordinate system, so no control-specific calculations are required.</summary>
	//	public event EventHandler<PaintEventArgs> Paint;

	//	/// <summary>The form for which the overlay overlays</summary>
	//	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	//	public Form Owner
	//	{
	//		get { return m_form; }
	//		set
	//		{
	//			// The owner form cannot be set to null.
	//			if (value == null)
	//				throw new ArgumentNullException();

	//			// The owner form can only be set once.
	//			if (m_form != null)
	//				throw new InvalidOperationException();

	//			// Save the form for future reference.
	//			m_form = value;

	//			// Handle the form's Resize event.
	//			m_form.Resize += (s,a) => m_form.Invalidate(true);

	//			// Handle the Paint event for each of the controls in the form's hierarchy.
	//			ConnectPaintEventHandlers(m_form);
	//		}
	//	}

	//	private void ConnectPaintEventHandlers(Control control)
	//	{
	//		// Connect the paint event handler for this control.
	//		// Remove the existing handler first (if one exists) and replace it.
	//		control.Paint -= ControlPaint;
	//		control.Paint += ControlPaint;

	//		// Connect the paint event handler for the new control.
	//		ControlEventHandler ControlAdded = (s,a) => ConnectPaintEventHandlers(a.Control);
	//		control.ControlAdded -= ControlAdded;
	//		control.ControlAdded += ControlAdded;

	//		// Recurse the hierarchy.
	//		foreach (Control child in control.Controls)
	//			ConnectPaintEventHandlers(child);
	//	}

	//	/// <summary>As each control on the form is repainted, this handler is called</summary>
	//	private void ControlPaint(object sender, PaintEventArgs e)
	//	{
	//		var control = sender as Control;
	//		if (control == null) return;
	//		Point location;

	//		// Determine the location of the control's client area relative to the form's client area.
	//		if (control == m_form)
	//		{
	//			// The form's client area is already form-relative.
	//			location = control.Location;
	//		}
	//		else
	//		{
	//			// The control may be in a hierarchy, so convert to screen coordinates and then back to form coordinates.
	//			location = m_form.PointToClient(control.Parent.PointToScreen(control.Location));

	//			// If the control has a border shift the location of the control's client area.
	//			location += new Size((control.Width - control.ClientSize.Width) / 2, (control.Height - control.ClientSize.Height) / 2);
	//		}

	//		// Translate the location so that we can use form-relative coordinates to draw on the control.
	//		if (control != m_form)
	//			e.Graphics.TranslateTransform(-location.X, -location.Y);

	//		// Fire a paint event.
	//		OnPaint(sender, e);
	//	}

	//		private void OnPaint(object sender, PaintEventArgs e)
	//		{
	//			// Fire a paint event.
	//			// The paint event will be handled in Form1.graphicalOverlay1_Paint().
	//			if (Paint != null)
	//				Paint(sender, e);
	//		}

	//	#region Component Designer generated code

	//	/// <summary>Required designer variable.</summary>
	//	private System.ComponentModel.IContainer components = null;

	//	/// <summary>Clean up any resources being used.</summary>
	//	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	//	protected override void Dispose(bool disposing)
	//	{
	//		if (disposing && (components != null))
	//		{
	//			components.Dispose();
	//		}
	//		base.Dispose(disposing);
	//	}

	//	/// <summary>
	//	/// Required method for Designer support - do not modify
	//	/// the contents of this method with the code editor.
	//	/// </summary>
	//	private void InitializeComponent()
	//	{
	//		components = new System.ComponentModel.Container();
	//	}

	//	#endregion
	//}
}
