using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace TestCS
{
	public class ToolFormUI :pr.gui.ToolForm
	{
		public ToolFormUI(Control owner) :base(owner, EPin.TopRight)
		{
			InitializeComponent();
			AutoFade = true;

			m_combo_pin_to.Items.AddRange(Enum<EPin>.Values.Cast<object>().ToArray());
			m_combo_pin_to.SelectedIndexChanged += (s,a) =>
				{
					Pin = (EPin)m_combo_pin_to.SelectedItem;
				};

			PopulateChildCombo(owner.TopLevelControl);
			m_combo_child.DisplayMember = "Name";
			m_combo_child.SelectedIndexChanged += (s,a) =>
				{
					PinTarget = (Control)m_combo_child.SelectedItem;
				};

			m_track_autofade.Value = m_track_autofade.Maximum;
			m_track_autofade.ValueChanged += (s,a) =>
				{
					FadeRange = new RangeF(Maths.Frac(m_track_autofade.Minimum, m_track_autofade.Value, m_track_autofade.Maximum), 1f);
				};
		}

		private void PopulateChildCombo(Control parent)
		{
			foreach (var c in parent.Controls.Cast<Control>())
			{
				m_combo_child.Items.Add(c);
				PopulateChildCombo(c);
			}
		}

		private ComboBox m_combo_pin_to;
		private ComboBox m_combo_child;
		private Label label1;
		private Label label2;
		private TrackBar m_track_autofade;
		private Label label3;
		
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
			this.m_combo_pin_to = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_combo_child = new System.Windows.Forms.ComboBox();
			this.m_track_autofade = new System.Windows.Forms.TrackBar();
			this.label3 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_track_autofade)).BeginInit();
			this.SuspendLayout();
			// 
			// m_combo_pin_to
			// 
			this.m_combo_pin_to.FormattingEnabled = true;
			this.m_combo_pin_to.Location = new System.Drawing.Point(12, 24);
			this.m_combo_pin_to.Name = "m_combo_pin_to";
			this.m_combo_pin_to.Size = new System.Drawing.Size(132, 21);
			this.m_combo_pin_to.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(41, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Pin To:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 48);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(33, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Child:";
			// 
			// m_combo_child
			// 
			this.m_combo_child.FormattingEnabled = true;
			this.m_combo_child.Location = new System.Drawing.Point(12, 64);
			this.m_combo_child.Name = "m_combo_child";
			this.m_combo_child.Size = new System.Drawing.Size(132, 21);
			this.m_combo_child.TabIndex = 3;
			// 
			// m_track_autofade
			// 
			this.m_track_autofade.Location = new System.Drawing.Point(12, 104);
			this.m_track_autofade.Name = "m_track_autofade";
			this.m_track_autofade.Size = new System.Drawing.Size(132, 45);
			this.m_track_autofade.TabIndex = 4;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 88);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(56, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "AutoFade:";
			// 
			// ToolFormUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.m_track_autofade);
			this.Controls.Add(this.m_combo_child);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_combo_pin_to);
			this.Name = "ToolFormUI";
			this.Text = "toolform";
			((System.ComponentModel.ISupportInitialize)(this.m_track_autofade)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
