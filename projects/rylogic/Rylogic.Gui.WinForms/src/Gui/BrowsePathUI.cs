using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	public class BrowsePathUI :Control
	{
		#region UI Elements
		private ComboBox m_cb_path;
		private Button m_btn_browse;
		#endregion

		public BrowsePathUI()
		{
			SetStyle(ControlStyles.ContainerControl, true);

			m_cb_path = new ComboBox
			{
				Name = "m_cb_path",
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				AutoCompleteMode = AutoCompleteMode.SuggestAppend,
				AutoCompleteSource = AutoCompleteSource.ListItems,
				PreserveSelectionThruFocusChange = true,
				Font = new Font(FontFamily.GenericSansSerif, 10f, FontStyle.Regular, GraphicsUnit.Point),
				Margin = Padding.Empty,
				Padding = Padding.Empty,
				TabIndex = 0,
			};
			m_btn_browse = new Button
			{
				Name = "m_btn_browse",
				Anchor = AnchorStyles.Right,
				BackgroundImage = Resources.folder,
				BackgroundImageLayout = ImageLayout.Zoom,
				UseVisualStyleBackColor = true,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
				TabIndex = 1,
			};
			using (this.SuspendLayout(true))
			{
				Controls.Add(m_cb_path);
				Controls.Add(m_btn_browse);
				Margin = new Padding(3);
				Padding = new Padding(1);
			}

			Type = EType.OpenFile;
			m_path = string.Empty;
			m_history = new string[0];
			m_file_filter = string.Empty;

			Title = "Choose a file";

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			using (this.SuspendLayout(false))
			{
				Util.Dispose(ref m_cb_path);
				Util.Dispose(ref m_btn_browse);
			}
			base.Dispose(disposing);
		}

		/// <summary>Set whether Path is a file or a folder</summary>
		public EType Type { get; set; }
		public enum EType
		{
			OpenFile,
			SaveFile,
			SelectDirectory,
		}

		/// <summary>The path currently displayed</summary>
		public string Path
		{
			get { return m_path; }
			set
			{
				if (m_path == value) return;
				m_path = m_cb_path.Text = value;
				AddPathToHistory();
				PathChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		private string m_path;

		/// <summary>The filter string to use when choosing filepaths</summary>
		public string FileFilter
		{
			get { return m_file_filter; }
			set
			{
				if (m_file_filter == value) return;
				m_file_filter = value;
			}
		}
		private string m_file_filter;

		/// <summary>The value to show in the combo box</summary>
		public string[] History
		{
			get { return m_history; }
			set
			{
				if (m_history == value) return;
				m_history = value ?? new string[0];
				m_cb_path.Items.Clear();
				m_cb_path.Items.AddRange(m_history);
			}
		}
		private string[] m_history;

		/// <summary>The title of the open file dialog</summary>
		public string Title { get; set; }

		/// <summary>Access to the path combo box</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboBox ComboBox
		{
			get { return m_cb_path; }
		}

		/// <summary>Access to the browse button</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Button Button
		{
			get { return m_btn_browse; }
		}

		/// <summary>Raised when the path changes</summary>
		public event EventHandler PathChanged;

		/// <summary>Set up the Control</summary>
		private void SetupUI()
		{
			// Path combo
			m_cb_path.ValueType = typeof(string);
			m_cb_path.ValidateText = t => Path_.IsValidFilepath(t, false);
			m_cb_path.ValueCommitted += (s,a) =>
			{
				Path = (string)m_cb_path.Value;
			};

			// Browse for a path
			m_btn_browse.Click += (s,a) =>
			{
				BrowsePath();
			};
		}

		/// <summary>Show a dialog to the user to browse for the path value</summary>
		public void BrowsePath()
		{
			switch (Type)
			{
			default: throw new Exception("Unknown path type");
			case EType.OpenFile:
				{
					using (var dlg = new OpenFileDialog { Title = Title, Filter = FileFilter })
					{
						dlg.FileName = Path;
						if (dlg.ShowDialog(this) != DialogResult.OK) break;
						Path = dlg.FileName;
					}
					break;
				}
			case EType.SaveFile:
				{
					using (var dlg = new SaveFileDialog { Title = Title, Filter = FileFilter })
					{
						dlg.FileName = Path;
						if (dlg.ShowDialog(this) != DialogResult.OK) break;
						Path = dlg.FileName;
					}
					break;
				}
			case EType.SelectDirectory:
				{
					var dlg = new Core.Windows.OpenFolderUI { Title = Title, SelectedPath = Path };
					if (dlg.ShowDialog(this) != DialogResult.OK) break;
					Path = dlg.SelectedPath;
					break;
				}
			}
		}

		/// <summary>Add the current path value to the history</summary>
		public virtual void AddPathToHistory(bool ignore_case = false, int max_history_length = 20)
		{
			if (string.IsNullOrEmpty(Path)) return;
			History = Util.AddToHistoryList(History, Path, ignore_case, max_history_length);
		}

		/// <summary>Layout the control</summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			using (this.SuspendLayout(false))
			{
				var r = ClientRectangle;
				var x = Math_.Min(r.Height, r.Width, 28);

				m_btn_browse.Size     = new Size(x,x);
				m_btn_browse.Location = new Point(r.Right - m_btn_browse.Width, (r.Height - m_btn_browse.Height)/2);

				m_cb_path.Size     = new Size(r.Width - m_btn_browse.Width - 3, m_cb_path.Height);
				m_cb_path.Location = new Point(r.Left, (r.Height - m_cb_path.Height)/2);
			}
			base.OnLayout(levent);
		}
	}
}
