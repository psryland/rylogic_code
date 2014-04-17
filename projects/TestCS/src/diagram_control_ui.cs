using System;
using System.Drawing;
using System.Windows.Forms;
using pr.gui;
using pr.maths;

namespace TestCS
{
	public class DiagramControlUI :Form
	{
		private DiagramControl m_diag;

		public DiagramControlUI()
		{
			InitializeComponent();

			ClientSize = new Size(400,500);
			var node1 = new DiagramControl.BoxNode{Text = "Hello"};
			m_diag.Nodes.Add(node1);
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
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GraphControlUI));
			this.m_diag = new pr.gui.DiagramControl();
			((System.ComponentModel.ISupportInitialize)(this.m_diag)).BeginInit();
			this.SuspendLayout();
			//
			// m_diag
			//
			this.m_diag.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_diag.Location = new System.Drawing.Point(0, 0);
			this.m_diag.Name = "m_diag";
			this.m_diag.Size = new System.Drawing.Size(552, 566);
			this.m_diag.TabIndex = 0;
			//
			// DiagramControlUI
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(552, 566);
			this.Controls.Add(this.m_diag);
			this.Name = "DiagramControl";
			this.Text = "DiagramControl";
			((System.ComponentModel.ISupportInitialize)(this.m_diag)).EndInit();
			this.ResumeLayout(false);
		}

		#endregion
	}
}
