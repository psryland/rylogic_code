using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Rylogic.TextAligner
{
	public interface IPackage
	{
		/// <summary>Return the VS service of type 'TService'</summary>
		Task<object?> GetServiceAsync<TService>(CancellationToken cancellation_token);
		
		/// <summary>Return a dialog page</summary>
		T GetDialogPage<T>() where T : DialogPage;
	}
}
