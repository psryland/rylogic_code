using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace www.Pages;

public class IndexModel(ILogger<IndexModel> m_logger) :PageModel
{
	public void OnGet()
	{
		m_logger.LogInformation("Index page visited");
	}
}
