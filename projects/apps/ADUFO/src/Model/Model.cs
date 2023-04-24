using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADUFO;

public class Model :IDisposable
{
	public Model(Settings settings)
	{
		Settings = settings;
	}
	public void Dispose()
	{
	}

	/// <summary>Application settings</summary>
	private Settings Settings { get; }

	/// <summary>Access to ADO</summary>
	public AdoInterface Ado { get; set; } = null!;

	public void ConnectToADO()
	{
		// Connect to Azure DevOps

		// Download user stories from Azure DevOps

	}
}
