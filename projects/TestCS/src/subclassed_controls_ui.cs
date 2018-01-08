using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;

namespace TestCS
{
	public class SubclassedControlsUI :Form
	{
		private ToolStrip m_ts;
		private Rylogic.Gui.ComboBox m_cb;
		private Rylogic.Gui.ListBox m_lb;
		private Rylogic.Gui.DateTimePicker m_dtp;
		private Rylogic.Gui.RichTextBox m_rtb;
		private Rylogic.Gui.TextProgressBar m_pb;
		private Button m_btn_test;
		private Timer m_timer;

		private BindingSource<Thing> m_bs;
		private BindingListEx<Thing> m_bl0;
		private BrowsePathUI m_browse_path;
		private ValueBox m_vb_value;
		private Label m_lbl_vb_value;
		private AnimCheckBox m_abtn_switch;
		private ImageList m_abtn_images;
		private PatternFilter m_pattern_filter;
		private BindingListEx<Thing> m_bl1;

		public SubclassedControlsUI()
		{
			InitializeComponent();

			m_bl0 = new BindingListEx<Thing>();
			m_bl1 = new BindingListEx<Thing>();
			m_bs = new BindingSource<Thing> { DataSource = m_bl0 };

			m_bl0.Add(new Thing { Name = "One" });
			m_bl0.Add(new Thing { Name = "Two" });
			m_bl0.Add(new Thing { Name = "Three" });
			m_bl0.Add(new Thing { Name = "Four" });

			m_bl1.Add(new Thing { Name = "Apple" });
			m_bl1.Add(new Thing { Name = "Banana" });
			m_bl1.Add(new Thing { Name = "Cucumber" });
	
			// Tool strip combo box
			var tscb = new Rylogic.Gui.ToolStripComboBox();
			tscb.ComboBox.DisplayProperty = nameof(Thing.Name);
			m_ts.Items.Add(tscb);

			// Tool strip date time picker
			var tsdtp = new Rylogic.Gui.ToolStripDateTimePicker();
			tsdtp.Format = DateTimePickerFormat.Custom;
			tsdtp.CustomFormat = "yyyy-MM-dd HH:mm:ss";
			tsdtp.DateTimePicker.Kind = DateTimeKind.Utc;
			tsdtp.DateTimePicker.ValueChanged += DateTimeValueChanged;
			m_ts.Items.Add(tsdtp);

			// Combo box
			m_cb.DisplayProperty = nameof(Thing.Name);
			m_cb.TextChanged += (s,a) =>
			{
				// The selected item becomes null when the text is changed by the user.
				// Without this test, changing the selection causes the previously selected
				// item to have it's text changed because TextChanged is raised before the
				// binding source position and 'SelectedIndex' are changed.
				if (m_cb.SelectedItem == null)
					m_bs.Current.Name = m_cb.Text;
			};

			// List Box
			m_lb.DisplayProperty = nameof(Thing.Name);

			// Date time picker
			m_dtp.Kind = DateTimeKind.Utc;
			m_dtp.MinDate = Rylogic.Gui.DateTimePicker.MinimumDateTime.As(DateTimeKind.Utc);
			m_dtp.MaxDate = Rylogic.Gui.DateTimePicker.MaximumDateTime.As(DateTimeKind.Utc);
			m_dtp.Value = DateTime.UtcNow;
			m_dtp.ValueChanged += DateTimeValueChanged;

			// Progress bar timer
			m_timer.Interval = 20;
			m_timer.Tick += (s,a) =>
			{
				if (m_pb.Value < m_pb.Maximum)
				{
					++m_pb.Value;
					m_pb.Text = $"{m_pb.Value}";
				}
				else
					m_timer.Enabled = false;
			};

			// Browse path
			m_browse_path.Path = "Some File";
			m_browse_path.History = new[] {"File1", "File2" };
			m_browse_path.PathChanged += (s,a) =>
			{
				m_browse_path.AddPathToHistory();
			};

			// Button to make stuff happen
			m_btn_test.Click += ChangeSource;
			m_btn_test.Click += (s,a) =>
			{
				m_pb.Value = m_pb.Minimum;
				m_timer.Enabled = true;
			};

			// Value Box
			var vb_value_flag = true;
			m_vb_value.Value = 6.28;
			m_vb_value.ValueChanged += (s,a) =>
			{
				m_lbl_vb_value.Text = m_vb_value.Value.ToString();
			};
			m_vb_value.ValueCommitted += (s,a) =>
			{
				vb_value_flag = !vb_value_flag;
				m_vb_value.BackColor = vb_value_flag ? Color.Red : Color.Blue;
			};

			// Init binding source
			ChangeSource();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Set up/Change the binding source data</summary>
		private void ChangeSource(object sender = null, EventArgs e = null)
		{
			// Clear and reset the data source without first chance exceptions
			m_cb.DataSource = null;
			m_lb.DataSource = null;
			((Rylogic.Gui.ToolStripComboBox)m_ts.Items[0]).ComboBox.DataSource = null;

			m_bs.DataSource = m_bs.DataSource == m_bl0 ? m_bl1 : m_bl0;

			m_cb.DataSource = m_bs;
			m_lb.DataSource = m_bs;
			((Rylogic.Gui.ToolStripComboBox)m_ts.Items[0]).ComboBox.DataSource = m_bs;
		}

		private void DateTimeValueChanged(object sender, EventArgs e)
		{
			var dtp =
				sender is ToolStripDateTimePicker tsdtp ? tsdtp.DateTimePicker :
				sender is Rylogic.Gui.DateTimePicker prdtp ? prdtp :
				throw new Exception("Not a date time picker control");

			if (dtp.Value.Kind != DateTimeKind.Utc)
				throw new Exception("Kind is wrong");

			m_dtp.Value = dtp.Value;
			((ToolStripDateTimePicker)m_ts.Items[1]).DateTimePicker.Value = dtp.Value;
		}

		/// <summary>Binding type</summary>
		public class Thing :INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			public string Name
			{
				get { return m_name; }
				set { m_name = value; PropertyChanged.Raise(this, new PropertyChangedEventArgs("Name")); }
			}
			private string m_name;

			public override string ToString() { return $"Thing: {Name}"; }
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubclassedControlsUI));
			Rylogic.Common.Pattern pattern1 = new Rylogic.Common.Pattern();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_test = new System.Windows.Forms.Button();
			this.m_timer = new System.Windows.Forms.Timer(this.components);
			this.m_lbl_vb_value = new System.Windows.Forms.Label();
			this.m_vb_value = new Rylogic.Gui.ValueBox();
			this.m_browse_path = new Rylogic.Gui.BrowsePathUI();
			this.m_pb = new Rylogic.Gui.TextProgressBar();
			this.m_rtb = new Rylogic.Gui.RichTextBox();
			this.m_lb = new Rylogic.Gui.ListBox();
			this.m_dtp = new Rylogic.Gui.DateTimePicker();
			this.m_cb = new Rylogic.Gui.ComboBox();
			this.m_abtn_switch = new Rylogic.Gui.AnimCheckBox();
			this.m_abtn_images = new System.Windows.Forms.ImageList(this.components);
			this.m_pattern_filter = new Rylogic.Gui.PatternFilter();
			this.SuspendLayout();
			// 
			// m_ts
			// 
			this.m_ts.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.m_ts.Location = new System.Drawing.Point(0, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(380, 25);
			this.m_ts.TabIndex = 4;
			this.m_ts.Text = "toolStrip1";
			// 
			// m_btn_test
			// 
			this.m_btn_test.Location = new System.Drawing.Point(12, 206);
			this.m_btn_test.Name = "m_btn_test";
			this.m_btn_test.Size = new System.Drawing.Size(75, 23);
			this.m_btn_test.TabIndex = 5;
			this.m_btn_test.Text = "Test";
			this.m_btn_test.UseVisualStyleBackColor = true;
			// 
			// m_lbl_vb_value
			// 
			this.m_lbl_vb_value.AutoSize = true;
			this.m_lbl_vb_value.Location = new System.Drawing.Point(245, 263);
			this.m_lbl_vb_value.Name = "m_lbl_vb_value";
			this.m_lbl_vb_value.Size = new System.Drawing.Size(33, 13);
			this.m_lbl_vb_value.TabIndex = 9;
			this.m_lbl_vb_value.Text = "value";
			// 
			// m_vb_value
			// 
			this.m_vb_value.BackColorInvalid = System.Drawing.Color.White;
			this.m_vb_value.BackColorValid = System.Drawing.Color.White;
			this.m_vb_value.CommitValueOnFocusLost = true;
			this.m_vb_value.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_vb_value.ForeColorValid = System.Drawing.Color.Black;
			this.m_vb_value.Location = new System.Drawing.Point(93, 260);
			this.m_vb_value.Name = "m_vb_value";
			this.m_vb_value.Size = new System.Drawing.Size(146, 20);
			this.m_vb_value.TabIndex = 8;
			this.m_vb_value.UseValidityColours = true;
			this.m_vb_value.Value = null;
			// 
			// m_browse_path
			// 
			this.m_browse_path.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_browse_path.FileFilter = "";
			this.m_browse_path.History = new string[0];
			this.m_browse_path.Location = new System.Drawing.Point(12, 28);
			this.m_browse_path.Name = "m_browse_path";
			this.m_browse_path.Padding = new System.Windows.Forms.Padding(1);
			this.m_browse_path.Path = "";
			this.m_browse_path.Size = new System.Drawing.Size(356, 32);
			this.m_browse_path.TabIndex = 7;
			this.m_browse_path.Title = "Choose a file";
			this.m_browse_path.Type = Rylogic.Gui.BrowsePathUI.EType.OpenFile;
			// 
			// m_pb
			// 
			this.m_pb.ForeColor = System.Drawing.Color.Black;
			this.m_pb.Location = new System.Drawing.Point(93, 206);
			this.m_pb.Name = "m_pb";
			this.m_pb.Size = new System.Drawing.Size(246, 48);
			this.m_pb.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.m_pb.TabIndex = 6;
			// 
			// m_rtb
			// 
			this.m_rtb.CaretLocation = new System.Drawing.Point(0, 0);
			this.m_rtb.CurrentLineIndex = 0;
			this.m_rtb.FirstVisibleLineIndex = 0;
			this.m_rtb.LineCount = 0;
			this.m_rtb.Location = new System.Drawing.Point(139, 92);
			this.m_rtb.Name = "m_rtb";
			this.m_rtb.Size = new System.Drawing.Size(200, 108);
			this.m_rtb.TabIndex = 3;
			this.m_rtb.Text = "";
			// 
			// m_lb
			// 
			this.m_lb.FormattingEnabled = true;
			this.m_lb.Location = new System.Drawing.Point(12, 92);
			this.m_lb.Name = "m_lb";
			this.m_lb.Size = new System.Drawing.Size(121, 108);
			this.m_lb.TabIndex = 2;
			// 
			// m_dtp
			// 
			this.m_dtp.Kind = System.DateTimeKind.Unspecified;
			this.m_dtp.Location = new System.Drawing.Point(139, 66);
			this.m_dtp.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
			this.m_dtp.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
			this.m_dtp.Name = "m_dtp";
			this.m_dtp.Size = new System.Drawing.Size(200, 20);
			this.m_dtp.TabIndex = 1;
			this.m_dtp.Value = new System.DateTime(2015, 5, 12, 11, 41, 16, 245);
			// 
			// m_cb
			// 
			this.m_cb.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb.BackColorValid = System.Drawing.Color.White;
			this.m_cb.CommitValueOnFocusLost = true;
			this.m_cb.DisplayProperty = null;
			this.m_cb.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb.FormattingEnabled = true;
			this.m_cb.Location = new System.Drawing.Point(12, 65);
			this.m_cb.Name = "m_cb";
			this.m_cb.PreserveSelectionThruFocusChange = false;
			this.m_cb.Size = new System.Drawing.Size(121, 21);
			this.m_cb.TabIndex = 0;
			this.m_cb.UseValidityColours = true;
			this.m_cb.Value = null;
			// 
			// m_abtn_switch
			// 
			this.m_abtn_switch.FrameRate = 5F;
			this.m_abtn_switch.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
			this.m_abtn_switch.ImageIndex = 0;
			this.m_abtn_switch.ImageList = this.m_abtn_images;
			this.m_abtn_switch.Location = new System.Drawing.Point(93, 286);
			this.m_abtn_switch.Name = "m_abtn_switch";
			this.m_abtn_switch.Size = new System.Drawing.Size(146, 64);
			this.m_abtn_switch.TabIndex = 10;
			this.m_abtn_switch.UseVisualStyleBackColor = true;
			// 
			// m_abtn_images
			// 
			this.m_abtn_images.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_abtn_images.ImageStream")));
			this.m_abtn_images.TransparentColor = System.Drawing.Color.Transparent;
			this.m_abtn_images.Images.SetKeyName(0, "slide_switch0.png");
			this.m_abtn_images.Images.SetKeyName(1, "slide_switch1.png");
			this.m_abtn_images.Images.SetKeyName(2, "slide_switch2.png");
			this.m_abtn_images.Images.SetKeyName(3, "slide_switch3.png");
			this.m_abtn_images.Images.SetKeyName(4, "slide_switch4.png");
			// 
			// m_pattern_filter
			// 
			this.m_pattern_filter.History = new Rylogic.Common.Pattern[0];
			this.m_pattern_filter.Location = new System.Drawing.Point(12, 356);
			this.m_pattern_filter.Name = "m_pattern_filter";
			pattern1.Active = true;
			pattern1.Expr = "";
			pattern1.IgnoreCase = false;
			pattern1.Invert = false;
			pattern1.PatnType = Rylogic.Common.EPattern.Substring;
			pattern1.WholeLine = false;
			this.m_pattern_filter.Pattern = pattern1;
			this.m_pattern_filter.Size = new System.Drawing.Size(356, 27);
			this.m_pattern_filter.TabIndex = 11;
			// 
			// SubclassedControlsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(380, 415);
			this.Controls.Add(this.m_pattern_filter);
			this.Controls.Add(this.m_abtn_switch);
			this.Controls.Add(this.m_lbl_vb_value);
			this.Controls.Add(this.m_vb_value);
			this.Controls.Add(this.m_browse_path);
			this.Controls.Add(this.m_pb);
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
