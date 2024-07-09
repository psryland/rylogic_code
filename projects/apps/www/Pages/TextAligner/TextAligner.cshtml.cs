using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace www.Pages.TextAligner
{
	public class TextAlignerModel : PageModel
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
