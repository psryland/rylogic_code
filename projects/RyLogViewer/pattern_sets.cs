using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	public class PatternSets<T> :UserControl where T:Pattern
	{
		private class Set
		{
			public string Name;
			public string Filepath;
			public Set(string filepath) { Filepath = filepath; Name = Path.GetFileName(filepath); }
		}

		private Label m_lbl_pattern_set;
		private Button m_btn_load_set;
		private Button m_btn_save_set;
		private Button m_btn_del;
		private ImageList m_image_list;
		private ComboBox m_combo_recent_sets;
		
		// The designer requires a parameterless constructor
		public PatternSets()
		{
			InitializeComponent();
		}

		/// <summary>Setup the control</summary>
		internal void Init(List<T> current_set, Settings settings)
		{
			// Convert the XML list of pattern sets to a binding list
			XDocument doc = XDocument.Parse(settings.HighlightPatternSets);
			BindingList<Set> sets = new BindingList<Set>();
			if (doc.Root != null)
			{
				foreach (var set in doc.Root.Elements("pattern_set"))
					sets.Add(new Set(set.Value));
			}
			
			// Bind the sets list to the combo box
			m_combo_recent_sets.DataSource = new BindingList<Set>();
			m_combo_recent_sets.DisplayMember = "Name";

			m_btn_load_set.Click += (s,a)=>
				{
					OpenFileDialog fd = new OpenFileDialog{Filter = Resources.PatternSetFilter, Multiselect = false};
					if (fd.ShowDialog(this) != DialogResult.OK) return;
					foreach (var set in sets) if (string.Compare(set.Filepath, fd.FileName, true) == 0) return; // Already in the list
					new Set(fd.FileName);
					
						// Clear the current set and repopulate from the file
				};
			m_btn_save_set.Click += (s,a)=>
				{
				};
			m_combo_recent_sets.SelectedIndexChanged += (s,a)=>
				{
				};
		}
		
		#region Component Designer generated code
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternSets));
			this.m_lbl_pattern_set = new System.Windows.Forms.Label();
			this.m_btn_load_set = new System.Windows.Forms.Button();
			this.m_btn_save_set = new System.Windows.Forms.Button();
			this.m_combo_recent_sets = new System.Windows.Forms.ComboBox();
			this.m_btn_del = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			// 
			// m_lbl_pattern_set
			// 
			this.m_lbl_pattern_set.AutoSize = true;
			this.m_lbl_pattern_set.Location = new System.Drawing.Point(3, 8);
			this.m_lbl_pattern_set.Name = "m_lbl_pattern_set";
			this.m_lbl_pattern_set.Size = new System.Drawing.Size(68, 13);
			this.m_lbl_pattern_set.TabIndex = 5;
			this.m_lbl_pattern_set.Text = "Pattern Sets:";
			this.m_lbl_pattern_set.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_btn_load_set
			// 
			this.m_btn_load_set.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_load_set.Location = new System.Drawing.Point(274, 3);
			this.m_btn_load_set.Name = "m_btn_load_set";
			this.m_btn_load_set.Size = new System.Drawing.Size(45, 23);
			this.m_btn_load_set.TabIndex = 4;
			this.m_btn_load_set.Text = "Load";
			this.m_btn_load_set.UseVisualStyleBackColor = true;
			// 
			// m_btn_save_set
			// 
			this.m_btn_save_set.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_save_set.Location = new System.Drawing.Point(325, 3);
			this.m_btn_save_set.Name = "m_btn_save_set";
			this.m_btn_save_set.Size = new System.Drawing.Size(45, 23);
			this.m_btn_save_set.TabIndex = 3;
			this.m_btn_save_set.Text = "Save";
			this.m_btn_save_set.UseVisualStyleBackColor = true;
			// 
			// m_combo_recent_sets
			// 
			this.m_combo_recent_sets.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_recent_sets.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.m_combo_recent_sets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.m_combo_recent_sets.FormattingEnabled = true;
			this.m_combo_recent_sets.Location = new System.Drawing.Point(72, 5);
			this.m_combo_recent_sets.Name = "m_combo_recent_sets";
			this.m_combo_recent_sets.Size = new System.Drawing.Size(148, 21);
			this.m_combo_recent_sets.TabIndex = 6;
			// 
			// m_btn_del
			// 
			this.m_btn_del.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_del.Location = new System.Drawing.Point(226, 3);
			this.m_btn_del.Name = "m_btn_del";
			this.m_btn_del.Size = new System.Drawing.Size(45, 23);
			this.m_btn_del.TabIndex = 7;
			this.m_btn_del.Text = "Remove";
			this.m_btn_del.UseVisualStyleBackColor = true;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "edit_add.png");
			// 
			// PatternSets
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.m_btn_del);
			this.Controls.Add(this.m_combo_recent_sets);
			this.Controls.Add(this.m_lbl_pattern_set);
			this.Controls.Add(this.m_btn_load_set);
			this.Controls.Add(this.m_btn_save_set);
			this.Name = "PatternSets";
			this.Size = new System.Drawing.Size(373, 30);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
	
	// Specific instances so they're available in the designer
	public class PatternSetHL :PatternSets<Highlight> {}
	public class PatternSetFT :PatternSets<Filter> {}
}
