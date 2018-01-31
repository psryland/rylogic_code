using System.ComponentModel;
using System.Windows.Forms;
using Rylogic.Utility;
using Rylogic.Gui;
using System.IO;
using System;
using Rylogic.Common;
using Rylogic.Extn;
using System.Diagnostics;

namespace CppPad
{
	public class EditorUI :UserControl ,IDockable
	{
		public EditorUI(string filepath, Model model)
		{
			InitializeComponent();

			Model = model;
			DockControl = new DockControl(this, "editor") { TabCMenu = CreateTabCMenu() };
			Edit = new ScintillaCtrl { AutoIndent = true };
			Filepath = filepath;

			// Initialise the text from the file
			Edit.Text = File.ReadAllText(filepath);
			SaveNeeded = false;
		}
		protected override void Dispose(bool disposing)
		{
			if (SaveNeeded)
				Close(MessageBoxButtons.YesNo);

			Edit = null;
			DockControl = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
			Model = null;
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_impl_model; }
			private set
			{
				if (m_impl_model == value) return;
				if (m_impl_model != null) { };
				m_impl_model = value;
			}
		}
		private Model m_impl_model;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				if (m_impl_dock_control != null)
				{
					Util.Dispose(ref m_impl_dock_control);
				}
				m_impl_dock_control = value;
				if (m_impl_dock_control != null)
				{
					
				}
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>The filepath of the file we're editing</summary>
		public string Filepath
		{
			get { return m_impl_filepath; }
			private set
			{
				if (m_impl_filepath == value) return;
				m_impl_filepath = value;
				UpdateUI();
			}
		}
		private string m_impl_filepath;

		/// <summary>True when this file has been modified</summary>
		public bool SaveNeeded
		{
			get { return m_save_needed; }
			set
			{
				if (m_save_needed == value) return;
				m_save_needed = value;
				UpdateUI();
			}
		}
		private bool m_save_needed;

		/// <summary>The Scintilla control</summary>
		public ScintillaCtrl Edit
		{
			get { return m_impl_edit; }
			private set
			{
				if (m_impl_edit == value) return;
				if (m_impl_edit != null)
				{
					m_impl_edit.TextChanged -= HandleTextChanged;

					Controls.Remove(m_impl_edit);
					Util.Dispose(ref m_impl_edit);
				}
				m_impl_edit = value;
				if (m_impl_edit != null)
				{
					m_impl_edit.Dock = DockStyle.Fill;
					Controls.Add(m_impl_edit);

//					m_impl_edit.Cmd(Sci.SCI_SETLEXER, 3/*Sci.SCLEX_CPP*/);
					m_impl_edit.TextChanged += HandleTextChanged;
				}
			}
		}
		private ScintillaCtrl m_impl_edit;

		/// <summary>
		/// Save the current editor content to 'filepath'.
		/// If 'filepath' is null, the user is prompted for a filepath.
		/// If 'filepath' is "", 'Filepath' is used.</summary>
		public void Save(string filepath)
		{
			if (filepath == null)
			{
				using (var dlg = new SaveFileDialog { Title = "Save a Source File", Filter = Model.CodeFileFilter })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}
				SaveNeeded = true;
			}
			else if (filepath == string.Empty || filepath == Filepath)
			{
				filepath = Filepath;
			}
			else
			{
				SaveNeeded = true;
			}

			if (SaveNeeded)
			{
				File.WriteAllText(filepath, Edit.Text);
				SaveNeeded = false;
				UpdateUI();
			}
		}

		/// <summary>Close this editor. Prompts to save if needed</summary>
		public void Close(MessageBoxButtons btns = MessageBoxButtons.YesNoCancel)
		{
			if (SaveNeeded)
			{
				var r = MsgBox.Show(this, "Save Changes?", "Closing...", btns, MessageBoxIcon.Question);
				if (r == DialogResult.Yes) Save(string.Empty);
				if (r == DialogResult.No) SaveNeeded = false;
				if (r == DialogResult.Cancel) return;
			}

			Dispose();
		}

		/// <summary>Text in the editor has been modified</summary>
		private void HandleTextChanged(object sender, EventArgs e)
		{
			SaveNeeded = true;
			UpdateUI();
		}

		/// <summary>Create a context menu for the tab</summary>
		private ContextMenuStrip CreateTabCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem($"Save {Path_.FileName(Filepath)}"));
				cmenu.Opening += (s,a) => opt.Enabled = SaveNeeded;
				opt.Click += (s,a) => Save(Filepath);
			} {
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Close") { ShortcutKeys = Keys.Control|Keys.F4 });
				opt.Click += (s,a) => Close();
			} {
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Close All But This"));
				opt.Click += (s,a) => Model.CloseAll(Filepath);
			}
			cmenu.Items.Add2(new ToolStripSeparator());
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Copy Full Path") { ShortcutKeys = Keys.Alt|Keys.C });
				opt.Click += (s,a) => Clipboard.SetText(Filepath);
			} {
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Open Containing Folder"));
				opt.Click += (s,a) => Process.Start("explorer", Path_.Directory(Filepath));
			}
			return cmenu;
		}

		/// <summary>Update the UI</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			DockControl.TabText = $"{Path_.FileName(Filepath)}{(SaveNeeded ? "*" : string.Empty)}";
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// EditorUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "EditorUI";
			this.Size = new System.Drawing.Size(390, 496);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
