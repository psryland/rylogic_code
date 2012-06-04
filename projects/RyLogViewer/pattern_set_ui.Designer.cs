namespace RyLogViewer
{
	internal partial class PatternSetUi
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternSetUi));
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_combo_sets = new System.Windows.Forms.ComboBox();
			this.m_btn_save = new System.Windows.Forms.Button();
			this.m_btn_load = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "bookmark_add.png");
			this.m_image_list.Images.SetKeyName(1, "my_documents.png");
			this.m_image_list.Images.SetKeyName(2, "fileclose.png");
			// 
			// m_combo_sets
			// 
			this.m_combo_sets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_sets.FormattingEnabled = true;
			this.m_combo_sets.Location = new System.Drawing.Point(3, 9);
			this.m_combo_sets.Name = "m_combo_sets";
			this.m_combo_sets.Size = new System.Drawing.Size(248, 21);
			this.m_combo_sets.TabIndex = 1;
			// 
			// m_btn_save
			// 
			this.m_btn_save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_save.AutoSize = true;
			this.m_btn_save.ImageIndex = 0;
			this.m_btn_save.ImageList = this.m_image_list;
			this.m_btn_save.Location = new System.Drawing.Point(257, 5);
			this.m_btn_save.Name = "m_btn_save";
			this.m_btn_save.Size = new System.Drawing.Size(39, 28);
			this.m_btn_save.TabIndex = 2;
			this.m_btn_save.UseVisualStyleBackColor = true;
			// 
			// m_btn_load
			// 
			this.m_btn_load.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_load.AutoSize = true;
			this.m_btn_load.ImageIndex = 1;
			this.m_btn_load.ImageList = this.m_image_list;
			this.m_btn_load.Location = new System.Drawing.Point(302, 5);
			this.m_btn_load.Name = "m_btn_load";
			this.m_btn_load.Size = new System.Drawing.Size(39, 28);
			this.m_btn_load.TabIndex = 3;
			this.m_btn_load.UseVisualStyleBackColor = true;
			// 
			// PatternSetUi
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.m_btn_load);
			this.Controls.Add(this.m_btn_save);
			this.Controls.Add(this.m_combo_sets);
			this.MinimumSize = new System.Drawing.Size(274, 38);
			this.Name = "PatternSetUi";
			this.Size = new System.Drawing.Size(344, 38);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ImageList m_image_list;
		private System.Windows.Forms.ComboBox m_combo_sets;
		private System.Windows.Forms.Button m_btn_save;
		private System.Windows.Forms.Button m_btn_load;
	}
}
