using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Common;
using Rylogic.Utility;

namespace Rylogic.Extn
{
	public static class Process_
	{
		// Notes:
		//   - To get stdout/stderr from a process do this:
		//       var proc = new Process();
		//       proc.StartInfo.FileName = <your_executable>;
		//       proc.StartInfo.Arguments = <arguments>;
		//       proc.StartInfo.CreateNoWindow = true;
		//       proc.StartInfo.UseShellExecute = false;
		//       proc.StartInfo.RedirectStandardOutput = true;
		//       proc.StartInfo.RedirectStandardError = true;
		//       proc.EnableRaisingEvents = true;
		//       proc.OutputDataReceived += (s, a) => Log(ELogLevel.Info, a.Data);
		//       proc.ErrorDataReceived += (s, a) => Log(ELogLevel.Error, a.Data);
		//       proc.Start();
		//       proc.BeginOutputReadLine();
		//       proc.BeginErrorReadLine();
		//       await proc.WaitForExitAsync(<cancel_token>);

		/// <summary>Run a background process with stdout/stderr redirected. Async waits for the process to exit</summary>
		public static async Task<Process> RunAsync(string executable, string arguments, DataReceivedEventHandler stdout, DataReceivedEventHandler stderr, CancellationToken cancel)
		{
			var proc = new Process();
			proc.StartInfo.FileName = executable;
			proc.StartInfo.Arguments = arguments;
			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;
			proc.EnableRaisingEvents = true;
			proc.OutputDataReceived += stdout;
			proc.ErrorDataReceived += stderr;
			proc.Start();

			// Wait for the process to exit
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			await proc.WaitForExitAsync(cancel);
			return proc;
		}

		/// <summary>Wait asynchronously for this process to exit.</summary>
		public static async Task WaitForExitAsync(this Process process, CancellationToken? cancel = null)
		{
			var tcs = new TaskCompletionSource<object>();
			void HandleExited(object? sender, EventArgs args) => tcs.TrySetResult(true);

			process.EnableRaisingEvents = true;
			using (Scope.Create(() => process.Exited += HandleExited, () => process.Exited -= HandleExited))
			{
				if (process.HasExited)
					return;

				cancel = cancel ?? CancellationToken.None;
				using (cancel.Value.Register(() => tcs.TrySetCanceled()))
					await tcs.Task;
			}
		}
	}
}
