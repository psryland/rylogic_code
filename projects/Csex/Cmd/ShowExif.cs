﻿using System;
using System.IO;
using pr.common;
using pr.extn;
using pr.gfx;

namespace Csex
{
	public class ShowExif :Cmd
	{
		private string m_jpg_filepath;

		public override void ShowHelp()
		{
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

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return m_jpg_filepath.HasValue();
		}

		public override int Run()
		{
			try
			{
				if (!Exif.IsJpgFile(m_jpg_filepath))
				{
					Console.WriteLine(" {0} - Not a valid Jpg file".Fmt(m_jpg_filepath));
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
				Console.WriteLine(" Error: {0}".Fmt(ex.Message));
				return -1;
			}
		}
	}
}