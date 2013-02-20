using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	public partial class AndroidLogcatUI :Form
	{
		private readonly Settings m_settings;
		private readonly ToolTip m_tt;

		/// <summary>The command line to execute</summary>
		public LaunchApp Launch;

		public AndroidLogcatUI(Settings settings)
		{
			InitializeComponent();
			m_settings = settings;
			m_tt = new ToolTip();
			
			m_edit_adb_fullpath.ToolTip(m_tt, "The full path to the android debug bridge executable ('adb.exe')");
			m_edit_adb_fullpath.Text = "<Please set the path to adb.exe>";
			m_edit_adb_fullpath.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					if (!File.Exists(m_edit_adb_fullpath.Text)) return;
					m_settings.AndroidLogcat.AdbFullPath = m_edit_adb_fullpath.Text;
					m_settings.Save();
				};
			
			AutoDetectAdbPath();
			
			//D:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
			//-s emulator-5554 logcat -v time
			//D:\Program Files (x86)\Android\android-sdk\platform-tools
			//D:\deleteme\logcat.txt
		}

		/// <summary>Search for the full path of adb.exe</summary>
		private void AutoDetectAdbPath()
		{
			// If the full path is saved in the settings, use that
			if (!string.IsNullOrEmpty(m_settings.AndroidLogcat.AdbFullPath))
			{
				m_edit_adb_fullpath.Text = m_settings.AndroidLogcat.AdbFullPath;
				return;
			}
			
			// Otherwise, begin a search for adb.exe
			ThreadPool.QueueUserWorkItem(() =>
				{
					string full_path = string.Empty;
					Func<string,bool> TryPath = p =>
						{
							if (string.IsNullOrEmpty(p)) return false;
							var dir  = new DirectoryInfo(p);
							var file = dir.EnumerateFiles("adb.exe", SearchOption.AllDirectories).FirstOrDefault();
							if (file == null) return false;
							full_path = file.FullName;
							return true;
						};
			
					m_edit_adb_fullpath.Text = 
						TryPath(Environment.GetEnvironmentVariable("ANDROID_HOME")) ||
						TryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)) ||
						TryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
						? full_path : string.Empty;
				});
		}
	}
}
