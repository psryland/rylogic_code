using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace rylogic.co.nz.Pages.RyLogViewer;

public class RyLogViewerModel : PageModel
{
	public RyLogViewerModel(IConfiguration config)
	{
		// Find the newest version apk file
		var file_store = config[ConfigTag.FileStore] ?? throw new Exception("FileStore path not set");
		var dir = Path.Combine(file_store, "rylogviewer");
		var latest_msi = Directory.GetFiles(dir, "RyLogViewerInstaller_*.msi").Max();

		// If the file exists, get the size
		if (latest_msi != null && Path.Exists(latest_msi))
		{
			var relative_path = Path.GetRelativePath(file_store, latest_msi).Replace('\\', '/');
			DownloadUrl = $"{Paths.FileStore}/{relative_path}";
			DownloadSize = new FileInfo(latest_msi).GetPrettySize();
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
