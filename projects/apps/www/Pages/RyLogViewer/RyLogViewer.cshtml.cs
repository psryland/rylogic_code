using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace www.Pages.RyLogViewer
{
	public class RyLogViewerModel : PageModel
	{
		
		public string DownloadSize
		{ 
			get => "0 Mb";
		}

		public void OnGet()
		{
		}
	}
}
