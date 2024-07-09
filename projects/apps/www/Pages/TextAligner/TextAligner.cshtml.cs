using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace rylogic.co.nz.Pages.TextAligner;

public class TextAlignerModel : PageModel
{
	public TextAlignerModel(IConfiguration config)
	{
		// Find the newest version apk file
		var file_store = config[ConfigTag.FileStore] ?? throw new Exception("FileStore path not set");
		var dir = Path.Combine(file_store, "textaligner");
		
		var latest_2022_vsix = Directory.GetFiles(dir, "Rylogic.TextAligner.2022.*.vsix").Max();
		var latest_2019_vsix = Directory.GetFiles(dir, "Rylogic.TextAligner.2019.*.vsix").Max();

		// If the file exists, get the size
		if (latest_2022_vsix != null && Path.Exists(latest_2022_vsix))
		{
			var relative_path = Path.GetRelativePath(file_store, latest_2022_vsix).Replace('\\', '/');
			DownloadVS2022Url = $"{Paths.FileStore}/{relative_path}";
			DownloadVS2022Size = new FileInfo(latest_2022_vsix).GetPrettySize();
		}
		else
		{
			DownloadVS2022Url = "no file";
			DownloadVS2022Size = "0";
		}

		// If the file exists, get the size
		if (latest_2019_vsix != null && Path.Exists(latest_2019_vsix))
		{
			var relative_path = Path.GetRelativePath(file_store, latest_2019_vsix).Replace('\\', '/');
			DownloadVS2019Url = $"{Paths.FileStore}/{relative_path}";
			DownloadVS2019Size = new FileInfo(latest_2019_vsix).GetPrettySize();
		}
		else
		{
			DownloadVS2019Url = "no file";
			DownloadVS2019Size = "0";
		}
	}

	/// <summary>The file name</summary>
	public string DownloadVS2022File => Path.GetFileName(DownloadVS2022Url);
	
	/// <summary>The full URL for the download</summary>
	public string DownloadVS2022Url { get; }

	/// <summary>The size of the download</summary>
	public string DownloadVS2022Size { get; }

	/// <summary>The file name</summary>
	public string DownloadVS2019File => Path.GetFileName(DownloadVS2019Url);
	
	/// <summary>The full URL for the download</summary>
	public string DownloadVS2019Url { get; }

	/// <summary>The size of the download</summary>
	public string DownloadVS2019Size { get; }

	/// <summary></summary>
	public void OnGet()
	{
	}
}
