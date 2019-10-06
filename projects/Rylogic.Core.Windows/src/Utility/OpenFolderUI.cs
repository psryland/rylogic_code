using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Rylogic.Common;
using Rylogic.Interop.Win32;

namespace Rylogic.Core.Windows
{
	/// <summary>More useful directory browser</summary>
	public class OpenFolderUI: INotifyPropertyChanged
	{
		public OpenFolderUI()
		{
			m_title = "Browse for Directory";
			m_description = string.Empty;
			m_selected_path = string.Empty;
		}

		/// <summary>Dialog title</summary>
		public string Title
		{
			get => m_title;
			set
			{
				if (m_title == value) return;
				m_title = value;
				NotifyPropertyChanged(nameof(Title));
			}
		}
		private string m_title;

		/// <summary>Description text to display within the dialog</summary>
		public string Description
		{
			get { return m_description; }
			set
			{
				if (m_description == value) return;
				m_description = value;
				NotifyPropertyChanged(nameof(Description));
			}
		}
		private string m_description;

		/// <summary>Get/Set whether the user is prompted to create folders that don't exist</summary>
		public bool CreateNewFolder { get; set; }

		/// <summary>The selected directory path</summary>
		public string SelectedPath
		{
			get { return m_selected_path; }
			set
			{
				if (m_selected_path == value) return;
				m_selected_path = value;
				NotifyPropertyChanged(nameof(SelectedPath));
			}
		}
		private string m_selected_path;

		/// <summary>Show the dialog</summary>
		public bool ShowDialog(IntPtr hwnd_owner)
		{
			var dialog = (IFileDialog?)null;
			try
			{
				dialog = new NativeFileOpenDialog();
				dialog.SetOptions(Win32.FOS.FOS_PICKFOLDERS | Win32.FOS.FOS_FORCEFILESYSTEM | Win32.FOS.FOS_FILEMUSTEXIST | (CreateNewFolder ? Win32.FOS.FOS_CREATEPROMPT : 0));
				dialog.SetTitle(Title);
				((IFileDialogCustomize)dialog).AddText(0, Description);

				// Initialise the path in the dialog
				if (!string.IsNullOrEmpty(SelectedPath))
				{
					var path = SelectedPath;
					var dir = Path_.Directory(path);
					if (dir == null || !Path_.DirExists(dir))
					{
						dialog.SetFileName(path);
					}
					else
					{
						var fname = Path_.FileName(m_selected_path);
						dialog.SetFolder(Win32.CreateItemFromParsingName(dir));
						dialog.SetFileName(fname);
					}
				}

				// Show the dialog
				var result = dialog.Show(hwnd_owner);
				if (result < 0)
				{
					if (unchecked((uint)result) == (uint)Win32.HRESULT.ERROR_CANCELLED)
						return false;
					else if (Marshal.GetExceptionForHR(result) is Exception err)
						throw err;
					else
						throw new Exception($"OpenFolderUI returned an unknown result: {result}");
				}

				// Get the selected path
				dialog.GetResult(out var item);
				item.GetDisplayName(Win32.SIGDN.SIGDN_FILESYSPATH, out var selected_path);
				SelectedPath = selected_path;
				return true;
			}
			finally
			{
				if (dialog != null)
					Marshal.FinalReleaseComObject(dialog);
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
