using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Interop.Win32;

namespace Csex
{
	/// <summary>Search a directory tree for files that are duplicates of files in another directory tree</summary>
	public class FindDuplicateFiles :Cmd
	{
		private bool m_interactive;
		private string m_src_dir;
		private string m_dst_dir;
		private bool m_show_dups;
		private string m_mv_dir;
		private string m_lst;
		private string m_regex_ignore;
		private bool m_jpg_date_taken;

		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Finds duplicate files between two directory trees\n" +
				" Syntax: Csex -FindDuplicateFiles -src src_dir -dst dst_dir [-mv dups_directory] [-lst list_filepath] [-ignore regex_patn] [-jpg_date_taken]\n" +
				"  -interactive       : Use the GUI to manage duplicate files\n" +
				"  -src src_dir       : The directory tree containing potential duplicates\n" +
				"  -dst dst_dir       : The directory tree containing existing files\n" +
				"  -show              : Output the duplicates to stdout\n" +
				"  -mv                : The directory in which to move duplicates (optional)\n" +
				"  -lst list_filepath : Output a file listing the full paths of the duplicates found in 'src'" +
				"  -ignore regex_patn : A regex pattern to ignore files/folders" +
				"  -jpg_date_taken    : Use 'Date Taken' from jpg exif data for jpg files" +
				"");
		}

		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-find_duplicate_files":                           return true;
			case "-interactive":    m_interactive    = true;        return true;
			case "-src":            m_src_dir        = args[arg++]; return true;
			case "-dst":            m_dst_dir        = args[arg++]; return true;
			case "-show":           m_show_dups      = true;        return true;
			case "-mv":             m_mv_dir         = args[arg++]; return true;
			case "-lst":            m_lst            = args[arg++]; return true;
			case "-ignore":         m_regex_ignore   = args[arg++]; return true;
			case "-jpg_date_taken": m_jpg_date_taken = true;        return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			if (m_interactive) return true;
			return m_src_dir.HasValue() && m_dst_dir.HasValue();
		}

		/// <summary>Execute the command</summary>
		public override int Run()
		{
			if (!m_interactive)
				return RunCmdline();

			Win32.FreeConsole();
			using (var dlg = new FindDuplicateFilesUI())
				dlg.ShowDialog();

			return 0;
		}

		/// <summary>Search for duplicate files using the command line parameters</summary>
		private int RunCmdline()
		{
			try
			{
				// Build a map of the files in 'm_dst_dir'
				Console.WriteLine("Building a map of existing files");
				var existing = BuildMap(m_dst_dir, m_regex_ignore);

				// Get the files that might be duplicates
				Console.WriteLine("Building a map of potentially duplicate files");
				var newfiles = BuildMap(m_src_dir, null);

				// Ensure the duplicates directory exists
				if (m_mv_dir != null)
				{
					if (!Path_.DirExists(m_mv_dir))
						Directory.CreateDirectory(m_mv_dir);
				}

				// Create the file that contains the duplicates list
				var lst = (m_lst != null) ? new StreamWriter(m_lst) : null;

				Console.WriteLine("Finding Duplicates");
				foreach (var finfo in newfiles)
				{
					if (existing.TryGetValue(finfo.Key, out var original))
					{
						if (m_show_dups)
							Console.WriteLine($"{finfo.Value.FullName} is a duplicate of {original.FullName}");

						if (lst != null)
							lst.WriteLine(finfo.Value.FullName);

						if (m_mv_dir != null)
						{
							var dstfile = Path.Combine(m_mv_dir, finfo.Value.Name);
							if (File.Exists(dstfile)) File.Delete(dstfile);
							File.Move(finfo.Value.FullName, dstfile);
						}
					}
				}

				if (lst != null)
					lst.Dispose();

				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return -1;
			}
		}

		/// <summary>Build a map of the files in 'root'</summary>
		private Dictionary<string, FileSystemInfo> BuildMap(string root, string ignore_patn)
		{
			var dir = root.ToLowerInvariant();

			var map = new Dictionary<string, FileSystemInfo>();
			foreach (var finfo in Path_.EnumFileSystem(root, search_flags:SearchOption.AllDirectories))
			{
				var d = (Path.GetDirectoryName(finfo.FullName) ?? string.Empty).ToLowerInvariant();
				if (d != dir)
				{
					Console.WriteLine(d);
					dir = d;
				}

				// If the file matches the ignore pattern, skip it
				if (ignore_patn != null && Regex.IsMatch(finfo.FullName, ignore_patn, RegexOptions.IgnoreCase))
					continue;

				try
				{
					var k = MakeKey(finfo);
					if (map.TryGetValue(k, out var existing))
					{
						Console.WriteLine($"Existing duplicate found:\n  {finfo.FullName}\n  {existing.FullName}\n");
						continue;
					}
					map.Add(k, finfo);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to add {finfo.FullName} to the map");
					Console.WriteLine($"Reason: {ex.Message}");
				}
			}

			return map;
		}

		private string MakeKey(FileSystemInfo finfo)
		{
			var fname = finfo.Name.ToLowerInvariant();

			// Special case JPGs
			if (m_jpg_date_taken && Exif.IsJpgFile(finfo.FullName))
			{
				using (var fs = new FileStream(finfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					var exif = Exif.Read(fs, false);
					if (exif != null && exif.HasTag(Exif.Tag.DateTimeOriginal))
					{
						var dat = exif[Exif.Tag.DateTimeOriginal];
						var ts = dat.AsString;
						return ts+"-"+fname;
					}
				}
			}

			// Include the file size in the key
			if (finfo is FileInfo fi)
				return fi.Length +  "-" + fname;
			else
				return fname;
		}
	}
}
