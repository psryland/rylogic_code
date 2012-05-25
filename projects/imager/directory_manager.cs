using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using pr.gfx;
using pr.util;

namespace imager
{
	public partial class DirectoryManager :Form
	{
		private readonly BindingList<MediaPath> m_paths;
		private readonly Settings m_settings;
		private readonly bool m_ss_dirs;
		private Point m_mouse_down_loc;
		private Bitmap m_drag_bm;
		private readonly ToolTip m_tt;
		
		/// <summary>notification of when the paths list is changed</summary>
		public event ListChangedEventHandler ListChanged;

		public DirectoryManager(Settings settings, bool ss_dirs)
		{
			InitializeComponent();
			m_settings = settings;
			m_ss_dirs = ss_dirs;
			m_tt = new ToolTip();

			m_paths = new BindingList<MediaPath>();
			m_paths.ListChanged += (s,e)=>{ if (ListChanged != null) ListChanged(s,e); };

			m_list.AllowDrop = true;
			m_list.AutoGenerateColumns = false;
			m_list.Columns.Add(new DataGridViewTextBoxColumn {HeaderText="Path"         ,DataPropertyName="Pathname"    ,FillWeight = 10f ,ReadOnly = true ,ToolTipText = "Directories to search when building the list of media files"});
			m_list.Columns.Add(new DataGridViewCheckBoxColumn{HeaderText="Sub Folders"  ,DataPropertyName="SubFolders"  ,FillWeight =  3f                  ,ToolTipText = "Include sub folders within this directory when searching for media files"});
			m_list.DataError += (s,e)=>{ e.Cancel = true; };
			m_list.DragEnter += (s,e)=>{ List_DropEnter(e); };
			m_list.DragOver  += (s,e)=>{ List_DragOver(e); };
			m_list.DragDrop  += (s,e)=>{ List_DropFiles(e); };
			m_list.MouseDown += (s,e)=>{ List_MouseDown(e); };
			m_list.MouseUp   += (s,e)=>{ m_list.MouseMove -= List_MouseMove; };
			m_list.DataSource = m_paths;
			m_list.DblBuffer();
			
			m_tt.SetToolTip(m_edit_path, "Enter a path to include when searching for media files");
			m_edit_path.TextChanged += (s,e)=>
				{
					m_btn_add.Enabled = m_edit_path.Text.Length != 0;
				};
			
			m_tt.SetToolTip(m_edit_path, "Browse for a path to include when searching for media files");
			m_btn_browse.Click += (s,e)=>
				{
					FolderBrowserDialog fd = new FolderBrowserDialog{Description = "Select a directory containing image, video, or audio files", SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)};
					if (fd.ShowDialog(this) != DialogResult.OK) return;
					m_edit_path.Text = fd.SelectedPath;
				};
			
			m_tt.SetToolTip(m_btn_add, "Add a path to the list above");
			m_btn_add.Enabled = false;
			m_btn_add.Click += (s,e)=>
				{
					AddPath(m_edit_path.Text);
				};
			
			m_tt.SetToolTip(m_btn_cancel, "Cancel any changes to the directories used to build the media list");
			m_btn_cancel.Click += (s,e)=>
				{
					if (Owner != null) Owner.BringToFront();
					Hide();
				};
			
			m_tt.SetToolTip(m_btn_ok, "Close this window and start building the media list using the directories listed above");
			m_btn_ok.Click += (s,e)=>
				{
					if (m_ss_dirs) m_settings.SSMediaPaths = new List<MediaPath>(m_paths);
					else           m_settings.MediaPaths   = new List<MediaPath>(m_paths);
					m_settings.Save();
					if (Owner != null) Owner.BringToFront();
					Hide();
				};
			
			FormClosing += (s,e)=>
				{
					e.Cancel = e.CloseReason == CloseReason.UserClosing;
					if (!e.Cancel) return;
					if (Owner != null) Owner.BringToFront();
					Hide();
				};

			Text = m_ss_dirs ? "Screen Saver Search Directories" : "Media List Search Directories";
			DoubleBuffered = true;
		}

		/// <summary>Show the window</summary>
		public new void Show(IWin32Window owner)
		{
			// Copy the paths to our local list
			m_paths.RaiseListChangedEvents = false;
			m_paths.Clear();
			if (m_ss_dirs) foreach (MediaPath p in m_settings.SSMediaPaths) m_paths.Add((MediaPath)p.Clone());
			else           foreach (MediaPath p in m_settings.MediaPaths  ) m_paths.Add((MediaPath)p.Clone());
			m_paths.RaiseListChangedEvents = true;
			m_paths.ResetBindings();
			base.Show(owner);
		}
		
		/// <summary>Add a path to the directory list</summary>
		private void AddPath(string path)
		{
			if (!Directory.Exists(path))
			{
				MessageBox.Show(this, "'" + path + "' does not exist", "Path Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			
			m_list.ClearSelection();
			
			// Check for duplicates
			foreach (DataGridViewRow row in m_list.Rows)
			{
				if (((MediaPath)row.DataBoundItem).Pathname != path) continue;
				row.Selected = true;
				return;
			}
			
			// Add the path
			m_paths.Add(new MediaPath(path));
		}
		
		/// <summary>Handle mouse events on the list</summary>
		private void List_MouseDown(MouseEventArgs args)
		{
			m_mouse_down_loc = args.Location;
			m_list.MouseMove -= List_MouseMove;
			m_list.MouseMove += List_MouseMove;
		}

		/// <summary>Handler for mouse move events over a list cell</summary>
		private void List_MouseMove(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Left && Util.Moved(args.Location, m_mouse_down_loc))
			{
				DataGridView.HitTestInfo info = m_list.HitTest(m_mouse_down_loc.X, m_mouse_down_loc.Y);
				if (info.ColumnIndex == -1 && (uint)info.RowIndex < m_paths.Count)
				{
					ImageAttributes ia = new ImageAttributes();
					ia.SetColorMatrix(new ColorMatrix{Matrix00=1f, Matrix11=1f, Matrix22=1f, Matrix44=0.2f});
					m_drag_bm = new Bitmap(m_list.Width, m_list.Height) {Tag = ia};
					m_list.DrawToBitmap(m_drag_bm, new Rectangle(Point.Empty, m_list.Size));
					m_list.DoDragDrop(m_paths[info.RowIndex], DragDropEffects.Move);
				}
			}
		}

		/// <summary>Handler for deciding what to do with drop data</summary>
		private static void List_DropEnter(DragEventArgs args)
		{
			if (args.Data.GetDataPresent(DataFormats.FileDrop) ||
				args.Data.GetDataPresent(DataFormats.Text))
			{
				args.Effect = DragDropEffects.Copy;
			}
			else if (args.Data.GetDataPresent(typeof(MediaPath)))
			{
				args.Effect = DragDropEffects.Move;
			}
			else
			{
				args.Effect = DragDropEffects.None;
			}
		}

		/// <summary>Handler that draws the currently dragging row</summary>
		private void List_DragOver(DragEventArgs args)
		{
			if (args.Effect == DragDropEffects.Move && m_drag_bm != null)
			{
				m_list.SuspendLayout();
				Graphics gfx = m_list.CreateGraphics();
				Point pt = m_list.PointToClient(new Point(args.X, args.Y));
				DataGridView.HitTestInfo info = m_list.HitTest(m_mouse_down_loc.X, m_mouse_down_loc.Y);
				Rectangle src_rect = new Rectangle(0, info.RowY, m_list.Width, m_list.RowTemplate.Height);
				gfx.CompositingMode = CompositingMode.SourceOver;
				gfx.DrawImage(m_drag_bm, Point.Empty);
				gfx.DrawImage(m_drag_bm, pt.X, pt.Y, src_rect, GraphicsUnit.Pixel, (ImageAttributes)m_drag_bm.Tag);
				m_list.ResumeLayout();
			}
		}

		/// <summary>Handler for dropped files/directories</summary>
		private void List_DropFiles(DragEventArgs args)
		{
			if (args.Effect == DragDropEffects.Copy)
			{
				string[] paths;
				if (args.Data.GetDataPresent(DataFormats.Text))
				{
					paths = ((string)args.Data.GetData(DataFormats.Text)).Split('\n',',','\t');
				}
				else if (args.Data.GetDataPresent(DataFormats.FileDrop))
				{
					paths = (string[])args.Data.GetData(DataFormats.FileDrop);
				}
				else return;
				foreach (string path in paths)
					AddPath(path);
				m_list.Invalidate();
			}
			else if (args.Effect == DragDropEffects.Move)
			{
				MediaPath path = args.Data.GetData(typeof(MediaPath)) as MediaPath;
				if (path == null) return;

				Point pt = m_list.PointToClient(new Point(args.X, args.Y));
				DataGridView.HitTestInfo info = m_list.HitTest(pt.X, pt.Y);
				
				int idx0 = m_paths.IndexOf(path);
				int idx1 = (info.RowIndex < 0) ? m_list.RowCount - 1 : info.RowIndex;
				if (idx0 != idx1)
				{
					if (idx0 <  idx1) --idx1;
					if (pt.Y - info.RowY > m_list.RowTemplate.Height/2) ++idx1;

					m_paths.RemoveAt(idx0);
					m_paths.Insert(idx1, path);

					m_list.ClearSelection();
					m_list.Rows[idx1].Selected = true;
				}
				m_list.Invalidate();
			}
		}
	}
}
