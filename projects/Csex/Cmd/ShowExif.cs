using System;
using Rylogic.Extn;
using Rylogic.Gfx;

namespace Csex
{
	public class ShowExif :Cmd
	{
		private string m_jpg_filepath;

		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Display Exif data from a jpg file\n" +
				" Syntax: Csex -showexif jpg_file\n" +
				"  jpg_file : A jpg file to display exif data from\n" +
				"");
		}

		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-showexif": return true;
			}
		}

		public override bool CmdLineData(string data, string[] args, ref int arg)
		{
			m_jpg_filepath = data;
			return true;
		}

		/// <summary>Validate the current options</summary>
		public override Exception Validate()
		{
			return
				!m_jpg_filepath.HasValue() ? new Exception($"No jpg filepath given") :
				null;
		}

		public override int Run()
		{
			try
			{
				if (!Exif.IsJpgFile(m_jpg_filepath))
				{
					Console.WriteLine($" {m_jpg_filepath} - Not a valid JPG file");
					return 1;
				}

				var exif = Exif.Load(m_jpg_filepath);
				foreach (var tag in exif.Tags)
				{
					var field = exif[tag];
					Console.WriteLine(field.ToString());
				}
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine($" Error: {ex.Message}");
				return -1;
			}
		}
	}
}
