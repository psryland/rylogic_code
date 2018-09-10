using System;
using System.Windows.Forms;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
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
		protected override void OnCheckedChanged(EventArgs e)
		{
			base.OnCheckedChanged(e);
			m_timer.Enabled = true;
		}

		/// <summary>How fast the images switch (in frames per second)</summary>
		public float FrameRate
		{
			get { return 1000f / m_timer.Interval; }
			set { m_timer.Interval = (int)(1000f / Math_.Clamp(value, 0.01f, 60f)); }
		}

		/// <summary>The current frame of animation</summary>
		public new int ImageIndex
		{
			get { return base.ImageIndex; }
			set { base.ImageIndex = Math_.Clamp(value, 0, ImageList?.Images.Count ?? 0); }
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
