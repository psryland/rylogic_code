using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Common;

namespace TestConsoleCS
{
	class Program
	{
		static void Main(string[] args)
		{
			var cancel = new CancellationTokenSource();
			var t0 = MainAsync();
			var t1 = ConsoleNoise();
			Task.WaitAll(t0, t1);

			async Task MainAsync()
			{
				var Console = new ConsoleEx
				{
					IndentString = "  ",
					AutoComplete = new ConsoleEx.AutoCompleteList(ConsoleEx.ECompletionMode.Word, new List<string>
					{
						"Apple", "Approval", "A-Hole",
						"Boris", "Bottom", "Butt",
						"Cheese", "Cheers", "Chuck",
					}),
				};

				Console.IndentLevel = 0;
				Console.Write("Hello");
				Console.Write(" World");
				Console.WriteLine();
				Console.IndentLevel = 3;
				Console.WriteLine("Indent 3");
				Console.IndentLevel = 1;
				Console.Write("Indent 1\n");
				Console.WriteLine("Done");

				for (; !cancel.IsCancellationRequested; )
				{
					Console.Prompt(">");
					var cmd_line = await Console.ReadAsync();
					if (cmd_line.ToLower() == "exit")
						break;
				}
				cancel.Cancel();
			}
			async Task ConsoleNoise()
			{
				for (int i = 0; i != 1000 && !cancel.IsCancellationRequested; ++i, await Task.Delay(3000))
					Console.WriteLine($"Log Output: {i}");
			}
		}
	}
}
