using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.attrib;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	internal class PatternSetUI :UserControl
	{
		// Notes:
		//  - A pattern set is a collection of highlight, filter, transform and action patterns.
		//  - A Pattern set is saved as an xml file at a user given location
		//  - The UI manages the list of pattern set files and supports loading/saving the 
		//    current patterns from a pattern set file

		private readonly string m_dummy = @"x:\(Select Pattern Set).dummy";
		private readonly string PatternSetFilter = Util.FileDialogFilter("Pattern Set Files", "*.pattern_set");
		private readonly string PatternSetVersion = "v1.0";

		public enum ESetType
		{
			[Assoc("name", "highlight")] [Assoc(XmlTag.Highlights)] Highlights,
			[Assoc("name", "filter"   )] [Assoc(XmlTag.Filters   )] Filters,
			[Assoc("name", "transform")] [Assoc(XmlTag.Transforms)] Transforms,
			[Assoc("name", "action"   )] [Assoc(XmlTag.ClkActions)] Actions,
		}

		#region UI Elements
		private ComboBox m_cb_sets;
		private Button m_btn_save;
		private Button m_btn_load;
		private ImageList m_il;
		private ToolTip m_tt;
		#endregion

		public PatternSetUI()
		{
			InitializeComponent();
			if (Util.IsInDesignMode)
				return;

			Sets = new BindingSource<string> { DataSource = new BindingListEx<string>() };
			Sets.Add(m_dummy);

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Sets = null;
			Settings = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Set up the control</summary>
		public void Init(ESetType type, Settings settings)
		{
			SetType = type;
			Settings = settings;
		}

		/// <summary>The </summary>
		public ESetType SetType
		{
			get;
			private set;
		}

		/// <summary>Pattern set settings</summary>
		private Settings Settings
		{
			[DebuggerStepThrough] get { return m_settings; }
			set
			{
				if (m_settings == value) return;
				if (m_settings != null)
				{
					m_settings.SettingsSaving -= OnSettingsSaving;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingsSaving += OnSettingsSaving;
				}
			}
		}
		private Settings m_settings;
		private void OnSettingsSaving(object sender, SettingsSavingEventArgs args)
		{
			Settings.PatternSets = Sets.Select(x => x.Filepath).ToArray();
		}

		/// <summary>The tab that this control is on</summary>
		public SettingsUI.ETab Tab
		{
			get;
			set;
		}

		/// <summary>The pattern set filepaths</summary>
		public BindingSource<string> Sets
		{
			get { return m_sets; }
			private set
			{
				if (m_sets == value) return;
				if (m_sets != null)
				{
					m_sets.CurrentChanged -= HandleSetSelected;
				}
				m_sets = value;
				if (m_sets != null)
				{
					m_sets.CurrentChanged += HandleSetSelected;
				}
			}
		}
		private BindingSource<string> m_sets;
		private void HandleSetSelected(object sender, EventArgs e)
		{
			if (Sets.Current == null || Sets.Current == m_dummy) return;
			PatternSetSelected(Sets.Current);
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Sets combo
			m_cb_sets.ToolTip(m_tt, "Select a pattern set from this list to merge with or replace the existing patterns");
			m_cb_sets.DataSource = Sets;
			m_cb_sets.Format += (s,a) =>
			{
				a.Value = Path_.FileTitle((string)a.ListItem);
			};

			// Load a pattern set from file
			m_btn_load.ToolTip(m_tt, "Load a pattern set from file");
			m_btn_load.Click += (s,a)=>
			{
				LoadPatternSet();
			};

			// Save the current list of patterns as a pattern set
			m_btn_save.ToolTip(m_tt, "Save the current list of patterns as a pattern set");
			m_btn_save.Click += (s,a)=>
			{
				SavePatternSet();
			};
		}

		/// <summary>Load a pattern set from file and add it to the set list</summary>
		private void LoadPatternSet()
		{
			// Ask for the file location
			using (var fd = new OpenFileDialog { Filter = PatternSetFilter, CheckFileExists = true })
			{
				if (fd.ShowDialog(this) != DialogResult.OK) return;
				AddToPatternSetList(fd.FileName, true);
			}
		}

		/// <summary>Save the current set as a pattern set</summary>
		private void SavePatternSet()
		{
			// Ask for a name for the set
			var fd = new SaveFileDialog
			{
				Filter = PatternSetFilter,
				CreatePrompt = false,
				OverwritePrompt = true,
			};
			using (fd)
			{
				if (fd.ShowDialog(this) != DialogResult.OK)
					return;

				try
				{
					// Export the current patterns as xml
					var doc = new XDocument(new XElement(XmlTag.Root));
					if (doc.Root == null) throw new ApplicationException("failed to add root xml element");
					doc.Root.Add(new XElement(XmlTag.Version, PatternSetVersion));

					doc.Root.Add2(XmlTag.Highlights , XmlTag.Highlight , Settings.HighlightPatterns , false);
					doc.Root.Add2(XmlTag.Filters    , XmlTag.Filter    , Settings.FilterPatterns    , false);
					doc.Root.Add2(XmlTag.Transforms , XmlTag.Transform , Settings.TransformPatterns , false);
					doc.Root.Add2(XmlTag.ClkActions , XmlTag.ClkAction , Settings.ActionPatterns    , false);

					// Write the current set to a file
					doc.Save(fd.FileName);

					// Add an entry to the pattern set collection
					AddToPatternSetList(fd.FileName, false);
				}
				catch (Exception ex)
				{
					MsgBox.Show(this, string.Format(Resources.CreatePatternSetFailedMsg, ex.Message), Resources.CreatePatternSetFailed, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		/// <summary>Add 'set' to the list of pattern sets, and optionally show the 'replace/merge' menu</summary>
		private void AddToPatternSetList(string set_filepath, bool show_menu)
		{
			// Remove any existing sets with the same name
			Sets.RemoveIf(ps => Path_.Compare(ps, set_filepath) == 0);
			Sets.Insert(1, set_filepath);

			// Show the merge or replace menu
			if (show_menu)
				PatternSetSelected(set_filepath);
		}

		/// <summary>Pops up a context menu at the mouse location giving the options of what to do with a pattern set</summary>
		private void PatternSetSelected(string set_filepath)
		{
			// Pop up a menu with options to merge/replace
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Replace all patterns"));
				opt.Click += (s,a) =>
				{
					LoadPatternSet(set_filepath, all:true, merge:false);
				};
			}
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Merge all patterns"));
				opt.Click += (s,a) =>
				{
					LoadPatternSet(set_filepath, all:true, merge:true);
				};
			}
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Replace {0} patterns".Fmt(SetType.Assoc<string>("name"))));
				opt.Click += (s,a) =>
				{
					LoadPatternSet(set_filepath, all:false, merge:false);
				};
			}
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Merge {0} patterns".Fmt(SetType.Assoc<string>("name"))));
				opt.Click += (s,a) =>
				{
					LoadPatternSet(set_filepath, all:false, merge:true);
				};
			}
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("<Remove from this list>"));
				opt.Click += (s,a) =>
				{
					RemovePatternSet(set_filepath);
				};
			}
			cmenu.Show(m_cb_sets, m_cb_sets.Width - cmenu.PreferredSize.Width, m_cb_sets.Height);
		}

		/// <summary>Add the patterns in 'set_filepath' to the current list</summary>
		private void LoadPatternSet(string set_filepath, bool all, bool merge)
		{
			try
			{
				var doc = XDocument.Load(set_filepath);
				if (doc.Root == null) throw new InvalidDataException("root xml node not found");
				if (doc.Root.Element(XmlTag.Version) == null)
					doc.Root.Add(new XElement(XmlTag.Version, string.Empty));

				// Migrate old versions
				for (string version; (version = doc.Root.Element(XmlTag.Version).Value) != PatternSetVersion;)
					Upgrade(doc.Root, version);

				var highlights = new List<Highlight>();
				var filters    = new List<Filter>();
				var transforms = new List<Transform>();
				var actions    = new List<ClkAction>();

				// If this is a merge, rather than a replace, add the existing patterns
				if (merge)
				{
					highlights.AddRange(Settings.HighlightPatterns);
					filters   .AddRange(Settings.FilterPatterns);
					transforms.AddRange(Settings.TransformPatterns);
					actions   .AddRange(Settings.ActionPatterns);
				}

				if (all || SetType == ESetType.Highlights)
					highlights.AddRange(doc.Root.Element(XmlTag.Highlights).Elements(XmlTag.Highlight).Select(x => x.As<Highlight>()));
				if (all || SetType == ESetType.Filters)
					filters.AddRange(doc.Root.Element(XmlTag.Filters).Elements(XmlTag.Filter).Select(x => x.As<Filter>()));
				if (all || SetType == ESetType.Transforms)
					transforms.AddRange(doc.Root.Element(XmlTag.Transforms).Elements(XmlTag.Transform).Select(x => x.As<Transform>()));
				if (all || SetType == ESetType.Actions)
					actions.AddRange(doc.Root.Element(XmlTag.ClkActions).Elements(XmlTag.ClkAction).Select(x => x.As<ClkAction>()));

				if (some_added) RaiseCurrentSetChanged();

			}
			catch (Exception ex)
			{
				Misc.ShowMessage(this, "Could not load pattern set {0}.".Fmt(set_filepath), Resources.LoadPatternSetFailed, MessageBoxIcon.Error, ex);
			}
		}


		/// <summary>Raised when the list of current patterns is changed</summary>
		public event EventHandler CurrentSetChanged;
		protected void RaiseCurrentSetChanged()
		{
			if (CurrentSetChanged != null)
				CurrentSetChanged(this, EventArgs.Empty);
		}


		/// <summary>Remove a pattern set from the list</summary>
		private void RemovePatternSet(Set set)
		{
			Sets.Remove(set);
			UpdateUI(m_dummy);
		}

		/// <summary>Generate a list of pattern sets from xml</summary>
		protected void Import(string pattern_sets)
		{
			Sets.Clear();
			var doc = XDocument.Parse(pattern_sets);
			if (doc.Root != null)
			{
				foreach (var set in doc.Root.Elements(PatternSetXmlTag))
					try { Sets.Add(new Set(set)); } catch {} // Ignore those that fail
			}
		}

		/// <summary>Serialise the pattern sets to xml</summary>
		protected string Export()
		{
			var doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) return "";

			foreach (var set in Sets)
				doc.Root.Add(set.ToXml(new XElement(PatternSetXmlTag)));

			return doc.ToString(SaveOptions.None);
		}
			private const string PatternSetXmlTag = "pattern_set";



		/// <summary>Called when loading a pattern set from an earlier version</summary>
		protected virtual void Upgrade(XElement root, string from_version)
		{
			throw new NotSupportedException("File version is {0}. Latest version is {1}. Upgrading from this version is not supported".Fmt(from_version, Version));
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternSetUI));
			this.m_il = new System.Windows.Forms.ImageList(this.components);
			this.m_cb_sets = new RyLogViewer.ComboBox();
			this.m_btn_save = new System.Windows.Forms.Button();
			this.m_btn_load = new System.Windows.Forms.Button();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// m_image_list
			// 
			this.m_il.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_il.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il.Images.SetKeyName(0, "edit_save.png");
			this.m_il.Images.SetKeyName(1, "folder_with_file.png");
			// 
			// m_combo_sets
			// 
			this.m_cb_sets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_sets.FormattingEnabled = true;
			this.m_cb_sets.Location = new System.Drawing.Point(3, 9);
			this.m_cb_sets.Name = "m_combo_sets";
			this.m_cb_sets.Size = new System.Drawing.Size(250, 21);
			this.m_cb_sets.TabIndex = 1;
			// 
			// m_btn_save
			// 
			this.m_btn_save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_save.AutoSize = true;
			this.m_btn_save.ImageIndex = 0;
			this.m_btn_save.ImageList = this.m_il;
			this.m_btn_save.Location = new System.Drawing.Point(304, 5);
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
			this.m_btn_load.ImageList = this.m_il;
			this.m_btn_load.Location = new System.Drawing.Point(259, 5);
			this.m_btn_load.Name = "m_btn_load";
			this.m_btn_load.Size = new System.Drawing.Size(39, 28);
			this.m_btn_load.TabIndex = 3;
			this.m_btn_load.UseVisualStyleBackColor = true;
			// 
			// PatternSetUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btn_load);
			this.Controls.Add(this.m_btn_save);
			this.Controls.Add(this.m_cb_sets);
			this.MinimumSize = new System.Drawing.Size(274, 38);
			this.Name = "PatternSetUI";
			this.Size = new System.Drawing.Size(346, 40);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}

	internal class Set
	{
		public Set()
			:this(string.Empty)
		{}
		public Set(string filepath)
		{
			Filepath = filepath;
		}
		public Set(XElement node)
		{
			Filepath = node.Element(XmlTag.Filepath).Value;
		}
		public XElement ToXml(XElement node)
		{
			node.Add(new XElement(XmlTag.Filepath ,Filepath));
			return node;
		}

		/// <summary>The Name of the set</summary>
		public string Name
		{
			get { return Path.GetFileNameWithoutExtension(Filepath); }
		}

		/// <summary>The set filepath</summary>
		public string Filepath { get; set; }
	}
}


#if false

	/// <summary>Highlight specific instance of the pattern set control</summary>
	internal class PatternSetHL :PatternSetUI
	{
		private const string HLPatternSetExtn   = @"rylog_highlights";
		private const string HLPatternSetFilter = @"Highlight Pattern Set Files (*."+HLPatternSetExtn+")|*."+HLPatternSetExtn+"|All files (*.*)|*.*";

		/// <summary>Initialise the control</summary>
		internal void Init(Settings settings, List<Highlight> highlights)
		{
			Init(settings);
			Import(settings.HighlightPatternSets);
			CurrentSet = highlights;
			UpdateUI();
		}

		/// <summary>Gets the version of the pattern set</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>A reference to the current set of highlight patterns</summary>
		public List<Highlight> CurrentSet { get; private set; }

		/// <summary>Called when settings are being saved, to save this pattern set</summary>
		protected override void OnSettingsSaving(object sender, SettingsSavingEventArgs args)
		{
			var settings = (Settings)sender;
			settings.HighlightPatternSets = Export();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return HLPatternSetFilter; }
		}




	}

	/// <summary>Filter specific instance of the pattern set control</summary>
	internal class PatternSetFT :PatternSetUI
	{
		protected const string FTPatternSetExtn   = @"rylog_filters";
		protected const string FTPatternSetFilter = @"Filter Pattern Set Files (*."+FTPatternSetExtn+")|*."+FTPatternSetExtn+"|All files (*.*)|*.*";

		/// <summary>A reference to the current set of filter patterns</summary>
		public List<Filter> CurrentSet { get; private set; }

		/// <summary>Initialise the control</summary>
		internal void Init(Settings settings, List<Filter> filters)
		{
			Init(settings);
			Import(settings.FilterPatternSets);
			CurrentSet = filters;
			UpdateUI();
		}

		/// <summary>Gets the version of the pattern set</summary>
		protected override string Version { get { return "v1.0"; } }

		/// <summary>Called when settings are being saved, to save this pattern set</summary>
		protected override void OnSettingsSaving(object sender, SettingsSavingEventArgs args)
		{
			var settings = (Settings)sender;
			settings.FilterPatternSets = Export();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return FTPatternSetFilter; }
		}




	}

	/// <summary>Transform specific instance of the pattern set control</summary>
	internal class PatternSetTX :PatternSetUI
	{
		protected const string TXPatternSetExtn   = @"rylog_transforms";
		protected const string TXPatternSetFilter = @"Transform Set Files (*."+TXPatternSetExtn+")|*."+TXPatternSetExtn+"|All files (*.*)|*.*";

		/// <summary>A reference to the current set of transforms</summary>
		public List<Transform> CurrentSet { get; private set; }

		/// <summary>Initialise the control</summary>
		internal void Init(Settings settings, List<Transform> transforms)
		{
			Init(settings);
			Import(settings.TransformPatternSets);
			CurrentSet = transforms;
			UpdateUI();
		}

		/// <summary>Gets the version of the pattern set</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>Called when settings are being saved, to save this pattern set</summary>
		protected override void OnSettingsSaving(object sender, SettingsSavingEventArgs args)
		{
			var settings = (Settings)sender;
			settings.TransformPatternSets = Export();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return TXPatternSetFilter; }
		}



	}

	/// <summary>Click Action specific instance of the pattern set control</summary>
	internal class PatternSetAC :PatternSetUI
	{
		protected const string ACPatternSetExtn   = @"rylog_actions";
		protected const string ACPatternSetFilter = @"Action Set Files (*."+ACPatternSetExtn+")|*."+ACPatternSetExtn+"|All files (*.*)|*.*";

		/// <summary>A reference to the current set of actions</summary>
		public List<ClkAction> CurrentSet { get; private set; }

		/// <summary>Initialise the control</summary>
		internal void Init(Settings settings, List<ClkAction> actions)
		{
			Init(settings);
			Import(settings.ActionPatternSets);
			CurrentSet = actions;
			UpdateUI();
		}

		/// <summary>Gets the version of the pattern set</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>Called when settings are being saved, to save this pattern set</summary>
		protected override void OnSettingsSaving(object sender, SettingsSavingEventArgs args)
		{
			var settings = (Settings)sender;
			settings.ActionPatternSets = Export();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return ACPatternSetFilter; }
		}




	}
#endif