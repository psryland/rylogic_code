using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace rylogic.co.nz.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
	private readonly ILogger<ErrorModel> m_logger;

	public ErrorModel(ILogger<ErrorModel> logger)
	{
		m_logger = logger;
	}

	/// <summary></summary>
	public string? RequestId { get; set; }

	/// <summary></summary>
	public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

	public void OnGet()
	{
		RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
	}
}

