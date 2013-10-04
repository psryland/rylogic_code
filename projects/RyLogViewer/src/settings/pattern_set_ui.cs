using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	internal abstract partial class PatternSetUi :UserControl
	{
		[Serializable] protected class Set
		{
			public string Filepath      { get; set; }
			public string Name          { get { return Path.GetFileNameWithoutExtension(Filepath); } }
			public Set()                { Filepath = ""; }
			public Set(string filepath) { Filepath = filepath; }
			public Set(XElement node)
			{
				// ReSharper disable PossibleNullReferenceException
				Filepath = node.Element(XmlTag.Filepath).Value;
				// ReSharper restore PossibleNullReferenceException
			}
			public XElement ToXml(XElement node)
			{
				node.Add(new XElement(XmlTag.Filepath ,Filepath));
				return node;
			}
		}
		private class IgnoreIndexChange {}

		private const string PatternSetXmlTag = "patternset";
		private readonly Set m_dummy = new Set(@"x:\(Select Pattern Set).tmp");
		protected readonly List<Set> m_sets;
		private readonly ToolTip m_tt;
		private Settings m_settings;
		private ImageList m_image_list;
		private ComboBox m_combo_sets;
		private Button m_btn_save;
		private Button m_btn_load;

		/// <summary>Raised when the list of current patterns is changed</summary>
		public event EventHandler CurrentSetChanged;
		protected void RaiseCurrentSetChanged()
		{
			if (CurrentSetChanged != null)
				CurrentSetChanged(this, EventArgs.Empty);
		}

		protected PatternSetUi()
		{
			InitializeComponent();
			m_tt = new ToolTip();
			m_sets = new List<Set>();
			UpdateUI();

			// Combo
			m_combo_sets.ToolTip(m_tt, "Select a pattern set from this list to merge with or replace the existing patterns");
			m_combo_sets.DisplayMember = Reflect<Set>.MemberName(x=>x.Name);
			m_combo_sets.SelectedIndex = 0;
			m_combo_sets.SelectedIndexChanged += (s,a)=>
				{
					if (m_combo_sets.SelectedItem == null || m_combo_sets.SelectedItem == m_dummy) return;
					if (m_combo_sets.Tag is IgnoreIndexChange) { m_combo_sets.Tag = null; return; }
					PatternSetSelected((Set)m_combo_sets.SelectedItem);
				};

			// Load a pattern set from file
			m_btn_load.ToolTip(m_tt, "Load a pattern set from file");
			m_btn_load.Click += (s,a)=> LoadPatternSet();

			// Save the current list of patterns as a pattern set
			m_btn_save.ToolTip(m_tt, "Save the current list of patterns as a pattern set");
			m_btn_save.Click += (s,a)=> SavePatternSet();
		}

		protected void Init(Settings settings)
		{
			m_settings = settings;
			m_settings.SettingsSaving += OnSettingsSaving;
		}
		protected override void Dispose(bool disposing)
		{
			m_settings.SettingsSaving -= OnSettingsSaving;
			m_tt.Dispose();

			if (disposing && (components != null))
				components.Dispose();

			base.Dispose(disposing);
		}

		/// <summary>Called when settings are being saved, to save this pattern set</summary>
		protected abstract void OnSettingsSaving(object sender, SettingsBase<Settings>.SettingsSavingEventArgs args);

		/// <summary>Gets the version of the pattern set</summary>
		protected abstract string Version { get; }

		/// <summary>Return the pattern set filter</summary>
		protected abstract string PatternSetFilter { get; }

		/// <summary>Load a pattern set from file and add it to the set list</summary>
		private void LoadPatternSet()
		{
			// Ask for the file location
			OpenFileDialog fd = new OpenFileDialog{Filter = PatternSetFilter, CheckFileExists = true};
			if (fd.ShowDialog(this) != DialogResult.OK) return;
			AddToPatternSetList(new Set(fd.FileName), true);
		}

		/// <summary>Save the current set as a pattern set</summary>
		private void SavePatternSet()
		{
			// Ask for a name for the set
			SaveFileDialog fd = new SaveFileDialog{Filter = PatternSetFilter, CreatePrompt = false, OverwritePrompt = true};
			if (fd.ShowDialog(this) != DialogResult.OK) return;

			try
			{
				// Export the current patterns as xml
				XDocument doc = new XDocument(new XElement(XmlTag.Root));
				if (doc.Root == null) throw new ApplicationException("failed to add root xml element");
				doc.Root.Add(new XElement(XmlTag.Version, Version));

				CurrentSetToXml(doc.Root);

				// Write the current set to a file in the app path
				doc.Save(fd.FileName);

				// Add an entry to the pattern set collection
				AddToPatternSetList(new Set(fd.FileName), false);
			}
			catch (Exception ex)
			{
				MsgBox.Show(this, string.Format(Resources.CreatePatternSetFailedMsg, ex.Message), Resources.CreatePatternSetFailed, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Add 'set' to the list of pattern sets, and optional show the 'replace/merge' menu</summary>
		private void AddToPatternSetList(Set set, bool show_menu)
		{
			// Remove any existing sets with the same name (Note, dummy is only in m_combo.Items)
			m_sets.RemoveAll(ps => ps.Name == set.Name);
			m_sets.Insert(0, set);
			UpdateUI(set);

			if (show_menu)
				PatternSetSelected(set);
		}

		/// <summary>Pops up a context menu at the mouse location giving the options of what to do with a pattern set</summary>
		private void PatternSetSelected(Set set)
		{
			// Pop up a menu with options to merge/replace
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Items.Add("Replace existing patterns"  , null, (ss,aa) => { ClearPatterns(); AddPatternSet(set); });
			menu.Items.Add("Merge with existing patterns", null, (ss,aa) => AddPatternSet(set));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("<Remove from this list>", null, (ss,aa) => RemovePatternSet(set));
			menu.Show(m_combo_sets, m_combo_sets.Width - menu.PreferredSize.Width, m_combo_sets.Height);
		}

		/// <summary>Add the patterns in 'set' to the current list</summary>
		private void AddPatternSet(Set set)
		{
			// ReSharper disable PossibleNullReferenceException
			try
			{
				XDocument doc = XDocument.Load(set.Filepath);
				if (doc.Root == null) throw new InvalidDataException("root xml node not found");
				if (doc.Root.Element(XmlTag.Version) == null)
					doc.Root.Add(new XElement(XmlTag.Version, string.Empty));

				// Migrate old versions
				for (string version; (version = doc.Root.Element(XmlTag.Version).Value) != Version;)
					Upgrade(doc.Root, version);

				// Merge the patterns with the existing ones
				MergePatterns(doc.Root);
			}
			catch (Exception ex)
			{
				Misc.ShowMessage(this, string.Format("Could not load pattern set {0}.", set.Filepath), Resources.LoadPatternSetFailed, MessageBoxIcon.Error, ex);
			}
			// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>Remove a pattern set from the list</summary>
		private void RemovePatternSet(Set set)
		{
			m_sets.Remove(set);
			UpdateUI(m_dummy);
		}

		/// <summary>Update UI elements based on current state</summary>
		protected void UpdateUI() { UpdateUI((Set)m_combo_sets.SelectedItem); }
		private void UpdateUI(Set selected)
		{
			// Populate the combo
			m_combo_sets.Items.Clear();
			m_combo_sets.Items.Add(m_dummy);
			foreach (var set in m_sets)
				m_combo_sets.Items.Add(set);

			// Set the selected index in the combo
			var idx = m_combo_sets.Items.IndexOf(selected ?? m_dummy);
			m_combo_sets.Tag = new IgnoreIndexChange();
			m_combo_sets.SelectedIndex = idx;
			m_combo_sets.Tag = null;
		}

		/// <summary>Generate a list of pattern sets from xml</summary>
		protected void Import(string pattern_sets)
		{
			m_sets.Clear();
			XDocument doc = XDocument.Parse(pattern_sets);
			if (doc.Root != null)
			{
				foreach (var set in doc.Root.Elements(PatternSetXmlTag))
					try { m_sets.Add(new Set(set)); } catch {} // Ignore those that fail
			}
		}

		/// <summary>Serialise the pattern sets to xml</summary>
		protected string Export()
		{
			XDocument doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) return "";

			foreach (var set in m_sets)
				doc.Root.Add(set.ToXml(new XElement(PatternSetXmlTag)));

			return doc.ToString(SaveOptions.None);
		}

		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected abstract void CurrentSetToXml(XElement parent);

		/// <summary>Clear the current set of patterns</summary>
		protected abstract void ClearPatterns();

		/// <summary>Add the patterns in 'node' to the current list</summary>
		protected abstract void MergePatterns(XElement node);

		/// <summary>Called when loading a pattern set from an earlier version</summary>
		protected virtual void Upgrade(XElement root, string from_version)
		{
			throw new NotSupportedException("File version is {0}. Latest version is {1}. Upgrading from this version is not supported".Fmt(from_version, Version));
		}

		#region Component Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternSetUi));
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_combo_sets = new ComboBox();
			this.m_btn_save = new System.Windows.Forms.Button();
			this.m_btn_load = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_image_list
			//
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "edit_save.png");
			this.m_image_list.Images.SetKeyName(1, "folder_with_file.png");
			//
			// m_combo_sets
			//
			this.m_combo_sets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_sets.FormattingEnabled = true;
			this.m_combo_sets.Location = new System.Drawing.Point(3, 9);
			this.m_combo_sets.Name = "m_combo_sets";
			this.m_combo_sets.Size = new System.Drawing.Size(250, 21);
			this.m_combo_sets.TabIndex = 1;
			//
			// m_btn_save
			//
			this.m_btn_save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_save.AutoSize = true;
			this.m_btn_save.ImageIndex = 0;
			this.m_btn_save.ImageList = this.m_image_list;
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
			this.m_btn_load.ImageList = this.m_image_list;
			this.m_btn_load.Location = new System.Drawing.Point(259, 5);
			this.m_btn_load.Name = "m_btn_load";
			this.m_btn_load.Size = new System.Drawing.Size(39, 28);
			this.m_btn_load.TabIndex = 3;
			this.m_btn_load.UseVisualStyleBackColor = true;
			//
			// PatternSetUi
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btn_load);
			this.Controls.Add(this.m_btn_save);
			this.Controls.Add(this.m_combo_sets);
			this.MinimumSize = new System.Drawing.Size(274, 38);
			this.Name = "PatternSetUi";
			this.Size = new System.Drawing.Size(346, 40);
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion
	}

	/// <summary>Highlight specific instance of the pattern set control</summary>
	internal class PatternSetHL :PatternSetUi
	{
		private const string HLPatternSetExtn   = @"rylog_highlights";
		private const string HLPatternSetFilter = @"Highlight Pattern Set Files (*."+HLPatternSetExtn+")|*."+HLPatternSetExtn+"|All files (*.*)|*.*";

		/// <summary>A reference to the current set of highlight patterns</summary>
		public List<Highlight> CurrentSet { get; private set; }

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

		/// <summary>Called when settings are being saved, to save this pattern set</summary>
		protected override void OnSettingsSaving(object sender, SettingsBase<Settings>.SettingsSavingEventArgs args)
		{
			var settings = (Settings)sender;
			settings.HighlightPatternSets = Export();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return HLPatternSetFilter; }
		}

		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected override void CurrentSetToXml(XElement parent)
		{
			foreach (var p in CurrentSet)
				parent.Add(p.ToXml(new XElement(XmlTag.Highlight)));
		}

		/// <summary>Clear the current set of patterns</summary>
		protected override void ClearPatterns()
		{
			if (CurrentSet.Count == 0) return;
			CurrentSet.Clear();
			RaiseCurrentSetChanged();
		}

		/// <summary>Add the patterns in 'node' to the current list</summary>
		protected override void MergePatterns(XElement node)
		{
			bool some_added = false;
			foreach (XElement n in node.Elements(XmlTag.Highlight))
				try { some_added |= CurrentSet.AddIfUnique(new Highlight(n)); } catch {} // Ignore those that fail
			if (some_added) RaiseCurrentSetChanged();
		}

		/// <summary>Called when loading a pattern set from an earlier version</summary>
		protected override void Upgrade(XElement root, string from_version)
		{
			// ReSharper disable PossibleNullReferenceException
			switch (from_version)
			{
			default:
				base.Upgrade(root, from_version);
				break;
			case "":
				// Added version and 'whole file'
				foreach (var p in root.Elements(XmlTag.Highlight))
					p.Add(new XElement(XmlTag.WholeLine, false));

				root.Element(XmlTag.Version).Value = Version;
				break;
			}
			// ReSharper restore PossibleNullReferenceException
		}
	}

	/// <summary>Filter specific instance of the pattern set control</summary>
	internal class PatternSetFT :PatternSetUi
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
		protected override void OnSettingsSaving(object sender, SettingsBase<Settings>.SettingsSavingEventArgs args)
		{
			var settings = (Settings)sender;
			settings.FilterPatternSets = Export();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return FTPatternSetFilter; }
		}

		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected override void CurrentSetToXml(XElement parent)
		{
			foreach (var p in CurrentSet)
				parent.Add(p.ToXml(new XElement(XmlTag.Filter)));
		}

		/// <summary>Clear the current set of patterns</summary>
		protected override void ClearPatterns()
		{
			if (CurrentSet.Count == 0) return;
			CurrentSet.Clear();
			RaiseCurrentSetChanged();
		}

		/// <summary>Add the patterns in 'node' to the current list</summary>
		protected override void MergePatterns(XElement node)
		{
			bool some_added = false;
			foreach (XElement n in node.Elements(XmlTag.Filter))
				try { some_added |= CurrentSet.AddIfUnique(new Filter(n)); } catch {} // Ignore those that fail
			if (some_added) RaiseCurrentSetChanged();
		}

		/// <summary>Called when loading a pattern set from an earlier version</summary>
		protected override void Upgrade(XElement root, string from_version)
		{
			// ReSharper disable PossibleNullReferenceException
			switch (from_version)
			{
			default:
				base.Upgrade(root, from_version);
				break;
			case "":
				// Added version and 'whole file'
				foreach (var p in root.Elements(XmlTag.Filter))
					p.Add(new XElement(XmlTag.WholeLine, false));

				root.Element(XmlTag.Version).Value = Version;
				break;
			}
			// ReSharper restore PossibleNullReferenceException
		}
	}

	/// <summary>Transform specific instance of the pattern set control</summary>
	internal class PatternSetTX :PatternSetUi
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
		protected override void OnSettingsSaving(object sender, SettingsBase<Settings>.SettingsSavingEventArgs args)
		{
			var settings = (Settings)sender;
			settings.TransformPatternSets = Export();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return TXPatternSetFilter; }
		}

		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected override void CurrentSetToXml(XElement parent)
		{
			foreach (var p in CurrentSet)
				parent.Add(p.ToXml(new XElement(XmlTag.Transform)));
		}

		/// <summary>Clear the current set of patterns</summary>
		protected override void ClearPatterns()
		{
			if (CurrentSet.Count == 0) return;
			CurrentSet.Clear();
			RaiseCurrentSetChanged();
		}

		/// <summary>Add the patterns in 'node' to the current list</summary>
		protected override void MergePatterns(XElement node)
		{
			bool some_added = false;
			foreach (XElement n in node.Elements(XmlTag.Transform))
				try { some_added |= CurrentSet.AddIfUnique(new Transform(n)); } catch {} // Ignore those that fail
			if (some_added) RaiseCurrentSetChanged();
		}

		/// <summary>Called when loading a pattern set from an earlier version</summary>
		protected override void Upgrade(XElement root, string from_version)
		{
			// ReSharper disable PossibleNullReferenceException
			switch (from_version)
			{
			default:
				base.Upgrade(root, from_version);
				break;
			case "":
				// Added version and 'whole file'
				foreach (var p in root.Elements(XmlTag.Transform))
					p.Add(new XElement(XmlTag.WholeLine, false));

				root.Element(XmlTag.Version).Value = Version;
				break;
			}
			// ReSharper restore PossibleNullReferenceException
		}
	}

	/// <summary>Click Action specific instance of the pattern set control</summary>
	internal class PatternSetAC :PatternSetUi
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
		protected override void OnSettingsSaving(object sender, SettingsBase<Settings>.SettingsSavingEventArgs args)
		{
			var settings = (Settings)sender;
			settings.ActionPatternSets = Export();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return ACPatternSetFilter; }
		}

		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected override void CurrentSetToXml(XElement parent)
		{
			foreach (var p in CurrentSet)
				parent.Add(p.ToXml(new XElement(XmlTag.ClkAction)));
		}

		/// <summary>Clear the current set of patterns</summary>
		protected override void ClearPatterns()
		{
			if (CurrentSet.Count == 0) return;
			CurrentSet.Clear();
			RaiseCurrentSetChanged();
		}

		/// <summary>Add the patterns in 'node' to the current list</summary>
		protected override void MergePatterns(XElement node)
		{
			bool some_added = false;
			foreach (XElement n in node.Elements(XmlTag.ClkAction))
				try { some_added |= CurrentSet.AddIfUnique(new ClkAction(n)); } catch {} // Ignore those that fail
			if (some_added) RaiseCurrentSetChanged();
		}

		/// <summary>Called when loading a pattern set from an earlier version</summary>
		protected override void Upgrade(XElement root, string from_version)
		{
			// ReSharper disable PossibleNullReferenceException
			switch (from_version)
			{
			default:
				base.Upgrade(root, from_version);
				break;
			case "":
				// Added version and 'whole file'
				foreach (var p in root.Elements(XmlTag.ClkAction))
					p.Add(new XElement(XmlTag.WholeLine, false));

				root.Element(XmlTag.Version).Value = Version;
				break;
			}
			// ReSharper restore PossibleNullReferenceException
		}
	}
}
