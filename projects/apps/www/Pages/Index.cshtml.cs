using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace rylogic.co.nz.Pages;

public class IndexModel :PageModel
{
	private readonly ILogger<IndexModel> m_logger;
	public IndexModel(ILogger<IndexModel> logger)
	{ 
		m_logger = logger;
	}

	/// <summary></summary>
	public void OnGet()
	{
	}
}