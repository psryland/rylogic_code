using System;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using RichTextBox = Rylogic.Gui.WinForms.RichTextBox;

namespace TestCS
{
	public class FormHintBalloon :Form
	{
		private readonly Random m_rng = new Random(0);
		private readonly HintBalloon m_hint_balloon = new HintBalloon
		{
			Target = new Point(53,87),
			FadeDuration = 1000,
		};
		private RichTextBox m_rtb;

		public FormHintBalloon()
		{
			InitializeComponent();
			m_rtb.TextChanged += (s,a) =>
				{
					m_hint_balloon.TextRtf = m_rtb.Rtf;
					m_hint_balloon.Show(this, new Point(m_rng.Next(Width),m_rng.Next(Height)));
				};
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{			this.m_rtb = new RichTextBox();
			this.SuspendLayout();
			//
			// m_rtb
			//
			this.m_rtb.Location = new System.Drawing.Point(12, 12);
			this.m_rtb.Name = "m_rtb";
			this.m_rtb.Size = new System.Drawing.Size(170, 130);
			this.m_rtb.TabIndex = 0;
			this.m_rtb.Text = "";
			//
			// FormHintBalloon
			//
			this.ClientSize = new System.Drawing.Size(194, 154);
			this.Controls.Add(this.m_rtb);
			this.Name = "FormHintBalloon";
			this.ResumeLayout(false);
		}

		#endregion
	}
}
