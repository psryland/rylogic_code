using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace www.Pages.AllKeys
{
	public class AllKeysModel : PageModel
	{
		public string DownloadSize
		{
			get => "4 Mb";
		}

		public void OnGet()
		{
		}
	}
}
