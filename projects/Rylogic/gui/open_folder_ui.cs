using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using pr.extn;
using pr.util;
using pr.win32;

namespace pr.gui
{
	/// <summary>Select a folder using the vista style dialog</summary>
	public sealed class OpenFolderUI : CommonDialog
	{
		private FolderBrowserDialog m_old_browse_folder_dlg;

		public OpenFolderUI()
		{
			if (!IsVistaFolderDialogSupported)
				m_old_browse_folder_dlg = new FolderBrowserDialog();
			else
				Reset();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_old_browse_folder_dlg);
			base.Dispose(disposing);
		}

		/// <summary>True if the vista style dialog will be shown, false if the crappy old XP style dialog will be shown</summary>
		public static bool IsVistaFolderDialogSupported
		{
			get { return Win32.IsWindowsVistaOrLater; }
		}

		/// <summary>The folder that is first displayed when the dialog opens. Default is desktop.</summary>
		public Environment.SpecialFolder RootFolder
		{
			get { return m_old_browse_folder_dlg != null ? m_old_browse_folder_dlg.RootFolder : m_initial_dir; }
			set
			{
				if (m_old_browse_folder_dlg != null)
					m_old_browse_folder_dlg.RootFolder = value;
				else
					m_initial_dir = value;
			}
		}
		private Environment.SpecialFolder m_initial_dir;

		/// <summary>Get/Set the path selected by the user and first selected in the dialog when opened.</summary>
		public string SelectedPath
		{
			get { return m_old_browse_folder_dlg != null ? m_old_browse_folder_dlg.SelectedPath : m_selected_path; }
			set
			{
				if (m_old_browse_folder_dlg != null)
					m_old_browse_folder_dlg.SelectedPath = value;
				else
					m_selected_path = value ?? string.Empty;
			}
		}
		private string m_selected_path;

		/// <summary>Get/Set the dialog title</summary>
		public string Title { get; set; }

		/// <summary>Get/Set the description text</summary>
		public string Description
		{
			get { return m_old_browse_folder_dlg != null ? m_old_browse_folder_dlg.Description : m_desc; }
			set
			{
				if (m_old_browse_folder_dlg != null)
					m_old_browse_folder_dlg.Description = value;
				else
					m_desc = value ?? String.Empty;
			}
		}
		private string m_desc;

		/// <summary>Get/Set whether the New Folder button is shown in the dialog</summary>
		public bool ShowNewFolderButton
		{
			get { return m_old_browse_folder_dlg != null ? m_old_browse_folder_dlg.ShowNewFolderButton : m_show_new_folder_btn; }
			set
			{
				if (m_old_browse_folder_dlg != null)
					m_old_browse_folder_dlg.ShowNewFolderButton = value;
				else
					m_show_new_folder_btn = value;
			}
		}
		private bool m_show_new_folder_btn;

		/// <summary>Show and run the dialog</summary>
		protected override bool RunDialog(IntPtr hwndOwner)
		{
			if (m_old_browse_folder_dlg != null)
				return m_old_browse_folder_dlg.ShowDialog(hwndOwner == IntPtr.Zero ? null : new HWnd(hwndOwner)) == DialogResult.OK;

			IFileDialog dialog = null;
			try
			{
				dialog = new NativeFileOpenDialog();
				dialog.SetOptions(Win32.FOS.FOS_PICKFOLDERS | Win32.FOS.FOS_FORCEFILESYSTEM | Win32.FOS.FOS_FILEMUSTEXIST);

				if (Title.HasValue())
					dialog.SetTitle(Title);
				if (Description.HasValue())
					dialog.As<IFileDialogCustomize>().AddText(0, m_desc);

				if (SelectedPath.HasValue())
				{
					var path = SelectedPath;
					var dir = Path.GetDirectoryName(path);
					if (dir == null || !Directory.Exists(dir))
					{
						dialog.SetFileName(path);
					}
					else
					{
						var fname = Path.GetFileName(m_selected_path);
						dialog.SetFolder(Win32.CreateItemFromParsingName(dir));
						dialog.SetFileName(fname);
					}
				}

				var result = dialog.Show(hwndOwner);
				if (result < 0)
				{
					if ((uint)result == (uint)Win32.HRESULT.ERROR_CANCELLED)
						return false;
					else
						throw Marshal.GetExceptionForHR(result);
				} 
			
				IShellItem item;
				dialog.GetResult(out item);
				item.GetDisplayName(Win32.SIGDN.SIGDN_FILESYSPATH, out m_selected_path);
				return true;
			}
			finally
			{
				if (dialog != null)
					Marshal.FinalReleaseComObject(dialog);
			}
		}

		/// <summary>Resets all properties to their default values.</summary>
		public override void Reset()
		{
			m_desc                = string.Empty;
			m_selected_path       = string.Empty;
			m_initial_dir         = Environment.SpecialFolder.Desktop;
			m_show_new_folder_btn = true;
		}
	}
}
