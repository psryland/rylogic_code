namespace pr
{
	partial class AddSeries
	{
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_lbl_series_name = new System.Windows.Forms.Label();
			this.m_edit_series_name = new System.Windows.Forms.TextBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_graph = new System.Windows.Forms.Label();
			this.m_combo_graphs = new System.Windows.Forms.ComboBox();
			this.m_lbl_xaxis_data = new System.Windows.Forms.Label();
			this.m_combo_xaxis_column = new System.Windows.Forms.ComboBox();
			this.m_combo_yaxis_column = new System.Windows.Forms.ComboBox();
			this.m_lbl_yaxis_data = new System.Windows.Forms.Label();
			this.m_combo_plot_type = new System.Windows.Forms.ComboBox();
			this.m_lbl_plot_type = new System.Windows.Forms.Label();
			this.m_lbl_plot_colour = new System.Windows.Forms.Label();
			this.m_lbl_point_size = new System.Windows.Forms.Label();
			this.m_edit_point_size = new System.Windows.Forms.TextBox();
			this.m_edit_line_size = new System.Windows.Forms.TextBox();
			this.m_lbl_line_size = new System.Windows.Forms.Label();
			this.m_btn_plot_colour_black = new System.Windows.Forms.Button();
			this.m_btn_plot_colour_blue = new System.Windows.Forms.Button();
			this.m_btn_plot_colour_red = new System.Windows.Forms.Button();
			this.m_btn_plot_colour_green = new System.Windows.Forms.Button();
			this.m_btn_plot_colour_gray = new System.Windows.Forms.Button();
			this.m_btn_plot_colour_custom = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_lbl_series_name
			// 
			this.m_lbl_series_name.AutoSize = true;
			this.m_lbl_series_name.Location = new System.Drawing.Point(12, 9);
			this.m_lbl_series_name.Name = "m_lbl_series_name";
			this.m_lbl_series_name.Size = new System.Drawing.Size(70, 13);
			this.m_lbl_series_name.TabIndex = 0;
			this.m_lbl_series_name.Text = "Series Name:";
			// 
			// m_edit_series_name
			// 
			this.m_edit_series_name.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_series_name.Location = new System.Drawing.Point(88, 6);
			this.m_edit_series_name.Name = "m_edit_series_name";
			this.m_edit_series_name.Size = new System.Drawing.Size(203, 20);
			this.m_edit_series_name.TabIndex = 1;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(216, 227);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 8;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(135, 227);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 7;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_lbl_graph
			// 
			this.m_lbl_graph.AutoSize = true;
			this.m_lbl_graph.Location = new System.Drawing.Point(43, 89);
			this.m_lbl_graph.Name = "m_lbl_graph";
			this.m_lbl_graph.Size = new System.Drawing.Size(39, 13);
			this.m_lbl_graph.TabIndex = 4;
			this.m_lbl_graph.Text = "Graph:";
			// 
			// m_combo_graphs
			// 
			this.m_combo_graphs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_graphs.FormattingEnabled = true;
			this.m_combo_graphs.Location = new System.Drawing.Point(88, 86);
			this.m_combo_graphs.Name = "m_combo_graphs";
			this.m_combo_graphs.Size = new System.Drawing.Size(203, 21);
			this.m_combo_graphs.TabIndex = 4;
			// 
			// m_lbl_xaxis_data
			// 
			this.m_lbl_xaxis_data.AutoSize = true;
			this.m_lbl_xaxis_data.Location = new System.Drawing.Point(17, 35);
			this.m_lbl_xaxis_data.Name = "m_lbl_xaxis_data";
			this.m_lbl_xaxis_data.Size = new System.Drawing.Size(65, 13);
			this.m_lbl_xaxis_data.TabIndex = 6;
			this.m_lbl_xaxis_data.Text = "X Axis Data:";
			// 
			// m_combo_xaxis_column
			// 
			this.m_combo_xaxis_column.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_xaxis_column.FormattingEnabled = true;
			this.m_combo_xaxis_column.Location = new System.Drawing.Point(88, 32);
			this.m_combo_xaxis_column.Name = "m_combo_xaxis_column";
			this.m_combo_xaxis_column.Size = new System.Drawing.Size(203, 21);
			this.m_combo_xaxis_column.TabIndex = 2;
			// 
			// m_combo_yaxis_column
			// 
			this.m_combo_yaxis_column.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_yaxis_column.FormattingEnabled = true;
			this.m_combo_yaxis_column.Location = new System.Drawing.Point(88, 59);
			this.m_combo_yaxis_column.Name = "m_combo_yaxis_column";
			this.m_combo_yaxis_column.Size = new System.Drawing.Size(203, 21);
			this.m_combo_yaxis_column.TabIndex = 3;
			// 
			// m_lbl_yaxis_data
			// 
			this.m_lbl_yaxis_data.AutoSize = true;
			this.m_lbl_yaxis_data.Location = new System.Drawing.Point(17, 62);
			this.m_lbl_yaxis_data.Name = "m_lbl_yaxis_data";
			this.m_lbl_yaxis_data.Size = new System.Drawing.Size(65, 13);
			this.m_lbl_yaxis_data.TabIndex = 8;
			this.m_lbl_yaxis_data.Text = "Y Axis Data:";
			// 
			// m_combo_plot_type
			// 
			this.m_combo_plot_type.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_plot_type.FormattingEnabled = true;
			this.m_combo_plot_type.Location = new System.Drawing.Point(88, 113);
			this.m_combo_plot_type.Name = "m_combo_plot_type";
			this.m_combo_plot_type.Size = new System.Drawing.Size(203, 21);
			this.m_combo_plot_type.TabIndex = 5;
			// 
			// m_lbl_plot_type
			// 
			this.m_lbl_plot_type.AutoSize = true;
			this.m_lbl_plot_type.Location = new System.Drawing.Point(27, 116);
			this.m_lbl_plot_type.Name = "m_lbl_plot_type";
			this.m_lbl_plot_type.Size = new System.Drawing.Size(55, 13);
			this.m_lbl_plot_type.TabIndex = 11;
			this.m_lbl_plot_type.Text = "Plot Type:";
			// 
			// m_lbl_plot_colour
			// 
			this.m_lbl_plot_colour.AutoSize = true;
			this.m_lbl_plot_colour.Location = new System.Drawing.Point(21, 144);
			this.m_lbl_plot_colour.Name = "m_lbl_plot_colour";
			this.m_lbl_plot_colour.Size = new System.Drawing.Size(61, 13);
			this.m_lbl_plot_colour.TabIndex = 12;
			this.m_lbl_plot_colour.Text = "Plot Colour:";
			// 
			// m_lbl_point_size
			// 
			this.m_lbl_point_size.AutoSize = true;
			this.m_lbl_point_size.Location = new System.Drawing.Point(25, 170);
			this.m_lbl_point_size.Name = "m_lbl_point_size";
			this.m_lbl_point_size.Size = new System.Drawing.Size(57, 13);
			this.m_lbl_point_size.TabIndex = 13;
			this.m_lbl_point_size.Text = "Point Size:";
			// 
			// m_edit_point_size
			// 
			this.m_edit_point_size.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_point_size.Location = new System.Drawing.Point(88, 167);
			this.m_edit_point_size.Name = "m_edit_point_size";
			this.m_edit_point_size.Size = new System.Drawing.Size(203, 20);
			this.m_edit_point_size.TabIndex = 14;
			// 
			// m_edit_line_size
			// 
			this.m_edit_line_size.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_line_size.Location = new System.Drawing.Point(88, 193);
			this.m_edit_line_size.Name = "m_edit_line_size";
			this.m_edit_line_size.Size = new System.Drawing.Size(203, 20);
			this.m_edit_line_size.TabIndex = 16;
			// 
			// m_lbl_line_size
			// 
			this.m_lbl_line_size.AutoSize = true;
			this.m_lbl_line_size.Location = new System.Drawing.Point(29, 196);
			this.m_lbl_line_size.Name = "m_lbl_line_size";
			this.m_lbl_line_size.Size = new System.Drawing.Size(53, 13);
			this.m_lbl_line_size.TabIndex = 15;
			this.m_lbl_line_size.Text = "Line Size:";
			// 
			// m_btn_plot_colour_black
			// 
			this.m_btn_plot_colour_black.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_plot_colour_black.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.m_btn_plot_colour_black.Location = new System.Drawing.Point(88, 140);
			this.m_btn_plot_colour_black.Name = "m_btn_plot_colour_black";
			this.m_btn_plot_colour_black.Size = new System.Drawing.Size(27, 21);
			this.m_btn_plot_colour_black.TabIndex = 6;
			this.m_btn_plot_colour_black.UseCompatibleTextRendering = true;
			this.m_btn_plot_colour_black.UseVisualStyleBackColor = true;
			// 
			// m_btn_plot_colour_blue
			// 
			this.m_btn_plot_colour_blue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_plot_colour_blue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.m_btn_plot_colour_blue.Location = new System.Drawing.Point(123, 140);
			this.m_btn_plot_colour_blue.Name = "m_btn_plot_colour_blue";
			this.m_btn_plot_colour_blue.Size = new System.Drawing.Size(27, 21);
			this.m_btn_plot_colour_blue.TabIndex = 17;
			this.m_btn_plot_colour_blue.UseCompatibleTextRendering = true;
			this.m_btn_plot_colour_blue.UseVisualStyleBackColor = true;
			// 
			// m_btn_plot_colour_red
			// 
			this.m_btn_plot_colour_red.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_plot_colour_red.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.m_btn_plot_colour_red.Location = new System.Drawing.Point(158, 140);
			this.m_btn_plot_colour_red.Name = "m_btn_plot_colour_red";
			this.m_btn_plot_colour_red.Size = new System.Drawing.Size(27, 21);
			this.m_btn_plot_colour_red.TabIndex = 18;
			this.m_btn_plot_colour_red.UseCompatibleTextRendering = true;
			this.m_btn_plot_colour_red.UseVisualStyleBackColor = true;
			// 
			// m_btn_plot_colour_green
			// 
			this.m_btn_plot_colour_green.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_plot_colour_green.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.m_btn_plot_colour_green.Location = new System.Drawing.Point(193, 140);
			this.m_btn_plot_colour_green.Name = "m_btn_plot_colour_green";
			this.m_btn_plot_colour_green.Size = new System.Drawing.Size(27, 21);
			this.m_btn_plot_colour_green.TabIndex = 19;
			this.m_btn_plot_colour_green.UseCompatibleTextRendering = true;
			this.m_btn_plot_colour_green.UseVisualStyleBackColor = true;
			// 
			// m_btn_plot_colour_gray
			// 
			this.m_btn_plot_colour_gray.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_plot_colour_gray.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.m_btn_plot_colour_gray.Location = new System.Drawing.Point(228, 140);
			this.m_btn_plot_colour_gray.Name = "m_btn_plot_colour_gray";
			this.m_btn_plot_colour_gray.Size = new System.Drawing.Size(27, 21);
			this.m_btn_plot_colour_gray.TabIndex = 20;
			this.m_btn_plot_colour_gray.UseCompatibleTextRendering = true;
			this.m_btn_plot_colour_gray.UseVisualStyleBackColor = true;
			// 
			// m_btn_plot_colour_custom
			// 
			this.m_btn_plot_colour_custom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_plot_colour_custom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.m_btn_plot_colour_custom.Location = new System.Drawing.Point(263, 140);
			this.m_btn_plot_colour_custom.Name = "m_btn_plot_colour_custom";
			this.m_btn_plot_colour_custom.Size = new System.Drawing.Size(27, 21);
			this.m_btn_plot_colour_custom.TabIndex = 21;
			this.m_btn_plot_colour_custom.Text = "...";
			this.m_btn_plot_colour_custom.UseCompatibleTextRendering = true;
			this.m_btn_plot_colour_custom.UseVisualStyleBackColor = true;
			// 
			// AddSeries
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(303, 262);
			this.Controls.Add(this.m_btn_plot_colour_custom);
			this.Controls.Add(this.m_btn_plot_colour_gray);
			this.Controls.Add(this.m_btn_plot_colour_green);
			this.Controls.Add(this.m_btn_plot_colour_red);
			this.Controls.Add(this.m_btn_plot_colour_blue);
			this.Controls.Add(this.m_edit_line_size);
			this.Controls.Add(this.m_lbl_line_size);
			this.Controls.Add(this.m_edit_point_size);
			this.Controls.Add(this.m_lbl_point_size);
			this.Controls.Add(this.m_btn_plot_colour_black);
			this.Controls.Add(this.m_lbl_plot_colour);
			this.Controls.Add(this.m_lbl_plot_type);
			this.Controls.Add(this.m_combo_plot_type);
			this.Controls.Add(this.m_combo_yaxis_column);
			this.Controls.Add(this.m_lbl_yaxis_data);
			this.Controls.Add(this.m_combo_xaxis_column);
			this.Controls.Add(this.m_lbl_xaxis_data);
			this.Controls.Add(this.m_combo_graphs);
			this.Controls.Add(this.m_lbl_graph);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_edit_series_name);
			this.Controls.Add(this.m_lbl_series_name);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddSeries";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Add Series";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lbl_series_name;
		private System.Windows.Forms.TextBox m_edit_series_name;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Label m_lbl_graph;
		private System.Windows.Forms.ComboBox m_combo_graphs;
		private System.Windows.Forms.Label m_lbl_xaxis_data;
		private System.Windows.Forms.ComboBox m_combo_xaxis_column;
		private System.Windows.Forms.ComboBox m_combo_yaxis_column;
		private System.Windows.Forms.Label m_lbl_yaxis_data;
		private System.Windows.Forms.ComboBox m_combo_plot_type;
		private System.Windows.Forms.Label m_lbl_plot_type;
		private System.Windows.Forms.Label m_lbl_plot_colour;
		private System.Windows.Forms.Label m_lbl_point_size;
		private System.Windows.Forms.TextBox m_edit_point_size;
		private System.Windows.Forms.TextBox m_edit_line_size;
		private System.Windows.Forms.Label m_lbl_line_size;
		private System.Windows.Forms.Button m_btn_plot_colour_black;
		private System.Windows.Forms.Button m_btn_plot_colour_blue;
		private System.Windows.Forms.Button m_btn_plot_colour_red;
		private System.Windows.Forms.Button m_btn_plot_colour_green;
		private System.Windows.Forms.Button m_btn_plot_colour_gray;
		private System.Windows.Forms.Button m_btn_plot_colour_custom;
	}
}