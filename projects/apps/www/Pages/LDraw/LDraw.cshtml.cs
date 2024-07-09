using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace rylogic.co.nz.Pages.LDraw;

public class LDrawModel :PageModel
{
	public LDrawModel(IConfiguration config)
	{
		// Find the newest version msi file
		var file_store = config[ConfigTag.FileStore] ?? throw new Exception("FileStore path not set");
		var dir = Path.Combine(file_store, "ldraw");
		var latest_installer = Directory.GetFiles(dir, "LDrawInstaller*.msi").Max();

		// If the file exists, get the size
		if (latest_installer != null && Path.Exists(latest_installer))
		{
			var relative_path = Path.GetRelativePath(file_store, latest_installer).Replace('\\', '/');
			var size_in_bytes = new FileInfo(latest_installer).Length;

			DownloadUrl = $"{Paths.FileStore}/{relative_path}";
			DownloadSize = size_in_bytes switch
			{
				< 1024 => $"{size_in_bytes} bytes",
				< 1024 * 1024 => $"{size_in_bytes / 1024.0:N2} KB",
				_ => $"{size_in_bytes / 1024.0 / 1024.0:N2} MB",
			};
		}
		else
		{
			DownloadUrl = "no file";
			DownloadSize = "0";
		}
	}

	/// <summary>The file name</summary>
	public string DownloadFile => Path.GetFileName(DownloadUrl);
	
	/// <summary>The full URL for the download</summary>
	public string DownloadUrl { get; }

	/// <summary>The size of the download</summary>
	public string DownloadSize { get; }

	/// <summary></summary>
	public void OnGet()
	{
	}
}
