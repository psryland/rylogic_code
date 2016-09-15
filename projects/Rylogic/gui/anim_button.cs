using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using pr.maths;
using pr.util;

namespace pr.gui
{
	/// <summary>A check box class for animated image buttons</summary>
	public class AnimCheckBox :CheckBox
	{
		// Notes:
		// To use this control, set the ImageList property to an image list and
		// the images to the sequences of animation frames (in order)

		private Timer m_timer;

		public AnimCheckBox()
		{
			InitializeComponent();

			FrameRate = 5f;
			//Hovered = false;
			//Pressed = false;
			m_timer.Tick += HandleTick;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>How fast the images switch (in frames per second)</summary>
		public float FrameRate
		{
			get { return 1000f / m_timer.Interval; }
			set { m_timer.Interval = (int)(1000f / Maths.Clamp(value, 0.01f, 60f)); }
		}

		/// <summary>The current frame of animation</summary>
		public new int ImageIndex
		{
			get { return base.ImageIndex; }
			set { base.ImageIndex = Maths.Clamp(value, 0, ImageList?.Images.Count ?? 0); }
		}

		/// <summary>Animation timer</summary>
		private void HandleTick(object sender, EventArgs e)
		{
			if (Checked && ImageIndex < ImageList.Images.Count)
				++ImageIndex;
			else if (!Checked && ImageIndex > 0)
				--ImageIndex;
			else
				m_timer.Enabled = false;
		}

		protected override void OnCheckedChanged(EventArgs e)
		{
			base.OnCheckedChanged(e);
			m_timer.Enabled = true;
		}

		///// <summary>True while the mouse is hovering over the button</summary>
		//[Browsable(false)]
		//public bool Hovered
		//{
		//	get { return m_hovered; }
		//	private set
		//	{
		//		if (m_hovered == value) return;
		//		m_hovered = value;
		//		UpdateImage();
		//	}
		//}
		//private bool m_hovered;

		///// <summary>True while the button is pressed</summary>
		//[Browsable(false)]
		//public bool Pressed
		//{
		//	get { return m_pressed; }
		//	private set
		//	{
		//		if (m_pressed == value) return;
		//		m_pressed = value;
		//		UpdateImage();
		//	}
		//}
		//private bool m_pressed;

		///// <summary>The image to display when the button is not hovered or pressed</summary>
		//[Category("Appearance")]
		//[Description("Image to show when the button is not in any other state.")]
		//public Image NormalImage
		//{
		//	get { return m_img_normal; }
		//	set
		//	{
		//		m_img_normal = value;
		//		if (!Hovered && !Pressed) Image = value;
		//	}
		//}
		//private Image m_img_normal;

		///// <summary>The image to display when the button is pressed</summary>
		//[Category("Appearance")]
		//[Description("Image to show when the button is pressed.")]
		//public Image DownImage
		//{
		//	get { return m_img_down; }
		//	set
		//	{
		//		m_img_down = value;
		//		if (Pressed) Image = value;
		//	}
		//}
		//private Image m_img_down;

		///// <summary>The image to display while the button is hovered</summary>
		//[Category("Appearance")]
		//[Description("Image to show when the button is hovered over.")]
		//public Image HoverImage
		//{
		//	get { return m_img_hovered; }
		//	set
		//	{
		//		m_img_hovered = value;
		//		if (Hovered) Image = value;
		//	}
		//}
		//private Image m_img_hovered;

		//// Mouse events
		//protected override void OnMouseDown(MouseEventArgs e)
		//{
		//	Pressed = true;
		//	base.OnMouseDown(e);
		//}
		//protected override void OnMouseUp(MouseEventArgs e)
		//{
		//	Pressed = false;
		//	base.OnMouseUp(e);
		//}
		//protected override void OnMouseEnter(EventArgs e)
		//{
		//	Hovered = true;
		//	base.OnMouseEnter(e);
		//}
		//protected override void OnMouseLeave(EventArgs e)
		//{
		//	Hovered = false;
		//	base.OnMouseLeave(e);
		//}


		/// <summary>Set the button image based on current state</summary>
		private void UpdateImage()
		{
			//Image = 
			//	Pressed ? DownImage ?? NormalImage :
			//	Hovered ? HoverImage ?? NormalImage :
			//	NormalImage;
		}


		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_timer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// m_timer
			// 
			this.m_timer.Interval = 10;
			this.ResumeLayout(false);

		}
		#endregion
	}
}
