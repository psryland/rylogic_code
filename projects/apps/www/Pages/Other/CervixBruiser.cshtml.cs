using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace rylogic.co.nz.Pages.Other
{
	public class CervixBruiserModel : PageModel
	{
		public CervixBruiserModel(IConfiguration config)
		{
			var file_store = config[ConfigTag.FileStore] ?? throw new Exception("FileStore path not set");
			var dir = Path.Combine(file_store, "other");
			var file = Path.Combine(dir, "en_office_professional_plus_2021_x86_x64_dvd_c6dd6dc6.zip");

			var relative_path = Path.GetRelativePath(file_store, file).Replace('\\', '/');
			DownloadUrl = $"{Paths.FileStore}/{relative_path}";
			DownloadSize = new FileInfo(file).GetPrettySize();
		}

		/// <summary>The file name</summary>
		public string DownloadFile => Path.GetFileName(DownloadUrl);
	
		/// <summary>The full URL for the download</summary>
		public string DownloadUrl { get; }

		/// <summary>The size of the download</summary>
		public string DownloadSize { get; }

		/// <inheritdoc/>
		public void OnGet()
		{
		}
	}
}
