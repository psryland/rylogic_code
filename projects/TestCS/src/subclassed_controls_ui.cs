using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;

namespace TestCS
{
	public class SubclassedControlsUI :Form
	{
		BindingSource<string> m_source;

		public SubclassedControlsUI()
		{
			m_source = new BindingSource<string>();
			InitializeComponent();

			var tscb = new pr.gui.ToolStripComboBox();
			m_ts.Items.Add(tscb);

			var tsdtp = new pr.gui.ToolStripDateTimePicker();
			tsdtp.Format = DateTimePickerFormat.Custom;
			tsdtp.CustomFormat = "yyyy-MM-dd HH:mm:ss";
			tsdtp.DateTimePicker.Kind = DateTimeKind.Utc;
			tsdtp.DateTimePicker.ValueChanged += DateTimeValueChanged;
			m_ts.Items.Add(tsdtp);

			ChangeSource();

			m_dtp.Kind = DateTimeKind.Utc;
			m_dtp.MinDate = pr.gui.DateTimePicker.MinimumDateTime.As(DateTimeKind.Utc);
			m_dtp.MaxDate = pr.gui.DateTimePicker.MaximumDateTime.As(DateTimeKind.Utc);
			m_dtp.Value = DateTime.UtcNow;
			m_dtp.ValueChanged += DateTimeValueChanged;

			m_btn_test.Click += ChangeSource;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		private void ChangeSource(object sender = null, EventArgs e = null)
		{
			var list = m_source.Count == 0 || m_source[0] == "One"
				? new List<string>(new[]{"Apple", "Banana", "Cucumber"})
				: new List<string>(new[]{"One", "Two", "Three"});
			m_source.DataSource = list;

			// Clear and reset the data source without first chance exceptions
			m_cb.DataSource = null;
			m_lb.DataSource = null;
			m_ts.Items[0].As<pr.gui.ToolStripComboBox>().ComboBox.DataSource = null;

			m_cb.DataSource = m_source;
			m_lb.DataSource = m_source;
			m_ts.Items[0].As<pr.gui.ToolStripComboBox>().ComboBox.DataSource = m_source;
		}

		private void DateTimeValueChanged(object sender, EventArgs e)
		{
			var dtp = sender is pr.gui.ToolStripDateTimePicker
				? sender.As<pr.gui.ToolStripDateTimePicker>().DateTimePicker
				: sender.As<pr.gui.DateTimePicker>();

			if (dtp.Value.Kind != DateTimeKind.Utc)
				throw new Exception("Kind is wrong");

			m_dtp.Value = dtp.Value;
			m_ts.Items[1].As<pr.gui.ToolStripDateTimePicker>().DateTimePicker.Value = dtp.Value;
		}

		private ToolStrip m_ts;
		private pr.gui.ComboBox m_cb;
		private pr.gui.ListBox m_lb;
		private pr.gui.DateTimePicker m_dtp;
		private pr.gui.RichTextBox m_rtb;
		private Button m_btn_test;

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_cb = new pr.gui.ComboBox();
			this.m_dtp = new pr.gui.DateTimePicker();
			this.m_lb = new pr.gui.ListBox();
			this.m_rtb = new pr.gui.RichTextBox();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_test = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_cb
			// 
			this.m_cb.FormattingEnabled = true;
			this.m_cb.Location = new System.Drawing.Point(12, 28);
			this.m_cb.Name = "m_cb";
			this.m_cb.Size = new System.Drawing.Size(121, 21);
			this.m_cb.TabIndex = 0;
			// 
			// m_dtp
			// 
			this.m_dtp.Kind = System.DateTimeKind.Unspecified;
			this.m_dtp.Location = new System.Drawing.Point(139, 29);
			this.m_dtp.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
			this.m_dtp.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
			this.m_dtp.Name = "m_dtp";
			this.m_dtp.Size = new System.Drawing.Size(200, 20);
			this.m_dtp.TabIndex = 1;
			this.m_dtp.Value = new System.DateTime(2015, 5, 12, 11, 41, 16, 245);
			// 
			// m_lb
			// 
			this.m_lb.FormattingEnabled = true;
			this.m_lb.Location = new System.Drawing.Point(12, 55);
			this.m_lb.Name = "m_lb";
			this.m_lb.Size = new System.Drawing.Size(121, 108);
			this.m_lb.TabIndex = 2;
			// 
			// m_rtb
			// 
			this.m_rtb.Location = new System.Drawing.Point(139, 55);
			this.m_rtb.Name = "m_rtb";
			this.m_rtb.Size = new System.Drawing.Size(200, 108);
			this.m_rtb.TabIndex = 3;
			this.m_rtb.Text = "";
			// 
			// m_ts
			// 
			this.m_ts.Location = new System.Drawing.Point(0, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(384, 25);
			this.m_ts.TabIndex = 4;
			this.m_ts.Text = "toolStrip1";
			// 
			// m_btn_test
			// 
			this.m_btn_test.Location = new System.Drawing.Point(12, 169);
			this.m_btn_test.Name = "m_btn_test";
			this.m_btn_test.Size = new System.Drawing.Size(75, 23);
			this.m_btn_test.TabIndex = 5;
			this.m_btn_test.Text = "Test";
			this.m_btn_test.UseVisualStyleBackColor = true;
			// 
			// SubclassedControlsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(384, 220);
			this.Controls.Add(this.m_btn_test);
			this.Controls.Add(this.m_ts);
			this.Controls.Add(this.m_rtb);
			this.Controls.Add(this.m_lb);
			this.Controls.Add(this.m_dtp);
			this.Controls.Add(this.m_cb);
			this.Name = "SubclassedControlsUI";
			this.Text = "Subclassed Controls";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
