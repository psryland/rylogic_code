using System;
using Rylogic.UnitTests;

namespace Rylogic
{
	public static class Program
	{
		/// <summary>The main entry point for the application.</summary>
		static void Main(string[] args)
		{
			Environment.ExitCode = UnitTest.RunLocalTests() ? 0 : 1;
		}

		public static void RunTestsOrThrow()
		{
			if (!UnitTest.RunLocalTests())
				throw new Exception("Unit tests failed");
		}
	}
}