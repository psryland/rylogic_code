//***************************************************
// UnitTest Runner
//  Copyright (c) Rylogic Ltd 2014
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.UnitTests
{
	public class UnitTest
	{
		#if PR_UNITTESTS
		public const bool TestsEnabled = true;
		#else
		public const bool TestsEnabled = false;
		#endif

		/// <summary>
		/// Loads a .NET assembly and searches for any 'TestFixture' marked classes.
		/// Any found are then executed. Returns true if all tests passed.
		/// Diagnostic output is written to 'output'</summary>
		public static bool RunTests(string assembly_filepath, Stream? outstream = null)
		{
			return RunTests(Assembly.LoadFile(assembly_filepath), outstream);
		}
		public static bool RunTests(Assembly ass, Stream? outstream = null)
		{
			try
			{
				if (ass == null)
					throw new ArgumentNullException("ass", "Assembly was null");
				if (outstream == null)
					outstream = Console.OpenStandardOutput();

				int passed = 0;
				int failed = 0;
				using (var outp = new StreamWriter(outstream, Encoding.ASCII, 4096, true))
				{
					outp.WriteLine($"Unit Testing:  {ass.GetName().Name}.dll  Framework={ass.FindAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? ass.ImageRuntimeVersion}  Platform={(Environment.Is64BitProcess ? "x64" : "x86")}  Config={(Util.IsDebug ? "Debug" : "Release")}  Version={ass.GetName().Version}");
					const string file_pattern = @" in " + Regex_.FullPathPattern + @":line\s(?<line>\d+)";

					// Look for test fixtures
					var test_fixtures = FindTestFixtures(ass).ToList();
					foreach (var fixture in test_fixtures)
					{
						// Create an instance of the unit test
						object inst;
						try { inst = Activator.CreateInstance(fixture) ?? throw new Exception($"Failed to create unit test {fixture.Name}"); }
						catch (Exception ex)
						{
							while (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException;
							outp.WriteLine(
								$"Unit Testing:  {fixture.Name} - Failed to create an instance of test fixture\n" +
								$"{ex.MessageFull()}\n" +
								$"{(ex.StackTrace is string st ? Util.FormatForOutputWindow(st, file_pattern) : string.Empty)}");
							outp.Flush();
							++failed;
							continue;
						}

						// Call the fixture set up method
						var setup = fixture.FindMethodsWithAttribute<TestFixtureSetUpAttribute>().FirstOrDefault();
						if (setup != null) try
						{
							setup.Invoke(inst, null);
						}
						catch (Exception ex)
						{
							while (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException;
							outp.WriteLine(
								$"Unit Testing:  {fixture.Name} - Test fixture set up function threw\n" +
								$"{ex.MessageFull()}\n" +
								$"{(ex.StackTrace is string st ? Util.FormatForOutputWindow(st, file_pattern) : string.Empty)}");
							outp.Flush();
							++failed;
							continue;
						}

						// Find the set up/clean up method to call before each unit test
						var test_setup = fixture.FindMethodsWithAttribute<SetUpAttribute>().FirstOrDefault();
						var test_clean = fixture.FindMethodsWithAttribute<TearDownAttribute>().FirstOrDefault();

						// Find the unit tests
						int pass_count = 0;
						foreach (var test in fixture.FindMethodsWithAttribute<TestAttribute>())
						{
							try
							{
								// Call set up for the test
								if (test_setup != null)
									test_setup.Invoke(inst, null);

								// Run the test
								test.Invoke(inst, null);

								// Call clean up for the test
								if (test_clean != null)
									test_clean.Invoke(inst, null);

								++pass_count;
							}
							catch (Exception ex)
							{
								while (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException;
								outp.WriteLine();
								outp.WriteLine($"Unit Testing:  Test {test.Name} Failed");
								outp.WriteLine($"{ex.MessageFull()}");
								outp.WriteLine($"{(ex.StackTrace is string st ? Util.FormatForOutputWindow(st, file_pattern) : string.Empty)}");
								outp.Flush();
								++failed;
							}
						}

						// Call the fixture clean up method
						var cleanup = fixture.FindMethodsWithAttribute<TestFixtureTearDownAttribute>().FirstOrDefault();
						if (cleanup != null)
						{
							try
							{
								cleanup.Invoke(inst, null);
							}
							catch (Exception ex)
							{
								while (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException;
								outp.WriteLine($"Unit Testing:  {fixture.Name} - Test fixture clean up function threw");
								outp.WriteLine($"{ex.MessageFull()}");
								outp.WriteLine($"{(ex.StackTrace is string st ? Util.FormatForOutputWindow(st, file_pattern) : string.Empty)}");
								outp.Flush();
								++failed;
								continue;
							}
						}
						passed += pass_count;
					}

					if (test_fixtures.Count == 0)
					{
						outp.WriteLine($"Unit Testing:  No unit tests found in assembly {ass.FullName}");
						outp.Flush();
					}
					else if (failed == 0 && passed > 0)
					{
						outp.WriteLine($"Unit Testing:  *** All {passed} unit tests passed ***");
						outp.Flush();
					}
					return failed == 0;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unit Testing:  Unhandled exception running unit tests: {ex.Message}");
				return false;
			}
		}

		/// <summary>Returns the test fixture types in 'ass'</summary>
		private static IEnumerable<Type> FindTestFixtures(Assembly ass)
		{
			foreach (var type in ass.GetExportedTypes())
			{
				var attr = type.FindAttribute<TestFixtureAttribute>(false);
				if (attr == null)
					continue;

				yield return type;
			}
		}
	}

	/// <summary>Exceptions thrown by unit test failures</summary>
	public class UnitTestException :System.Exception
	{
		public UnitTestException() :base() {}
		public UnitTestException(string message) :base(message) {}
		public UnitTestException(string message, Exception innerException) :base(message, innerException) {}
		protected UnitTestException(SerializationInfo info, StreamingContext context) :base(info, context) {}
	}

	public class Assert
	{
		/// <summary>Return a file and line link</summary>
		private static string VSLink
		{
			get
			{
				var st = new System.Diagnostics.StackTrace(true);
				var sf = st.GetFrame(2);
				var file = sf?.GetFileName() ?? string.Empty;
				var line = sf?.GetFileLineNumber() ?? 0;
				return $"{file}({line}) : ";
			}
		}

		public static void True(bool test)
		{
			if (test) return;
			throw new UnitTestException(VSLink + "result is not 'True'");
		}
		public static void False(bool test)
		{
			if (!test) return;
			throw new UnitTestException(VSLink + "result is not 'False'");
		}
		public static void Null(object? ptr)
		{
			if (ptr == null) return;
			throw new UnitTestException(VSLink + "reference is not Null");
		}
		public static void NotNull(object? ptr)
		{
			if (ptr != null) return;
			throw new UnitTestException(VSLink + "reference is Null");
		}

		/// <summary>Tests reference equality</summary>
		public static void AreSame(object? lhs, object? rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return;
			throw new UnitTestException(VSLink + "references are not equal");
		}

		/// <summary>Tests value equality</summary>
		public static void Equal(object? expected, object? result)
		{
			if (Equals(expected, result)) return;
			throw new UnitTestException(VSLink + $"values are not equal\r\n  expected: {(expected ?? "null").ToString()}\r\n  result: {(result ?? "null").ToString()}");
		}

		/// <summary>Tests value equality</summary>
		public static void Equal<T>(IEnumerable<T>? expected, IEnumerable<T>? result)
		{
			if (ReferenceEquals(expected, result)) return;
			if (expected.SequenceEqual(result)) return;
			throw new UnitTestException(VSLink + $"sequences are not equal\r\n  expected: {(expected != null ? string.Join(",", expected) : "null")}\r\n  result: {(result != null ? string.Join(",", result) : "null")}");
		}

		/// <summary>Tests value equality</summary>
		public static void Equal(double expected, double result, double tol)
		{
			if (Math.Abs(result - expected) < tol) return;
			throw new UnitTestException(VSLink + $"values are not equal\r\n  expected: {expected}\r\n  result: {result}\r\n  tol: {tol}");
		}
		public static void Equal(decimal expected, decimal result, decimal tol)
		{
			if (Math.Abs(result - expected) < tol) return;
			throw new UnitTestException(VSLink + $"values are not equal\r\n  expected: {expected}\r\n  result: {result}\r\n  tol: {tol}");
		}

		/// <summary>Tests value inequality</summary>
		public static void NotEqual(object? expected, object? result)
		{
			if (!Equals(expected, result)) return;
			throw new UnitTestException(VSLink + "values are equal");
		}

		public static void Throws(Type exception_type, Action action)
		{
			try
			{
				action();
				throw new UnitTestException(VSLink + "no exception was thrown");
			}
			catch (Exception ex)
			{
				if (exception_type.IsAssignableFrom(ex.GetType())) return;
				throw new UnitTestException(VSLink + "exception of the wrong type thrown", ex);
			}
		}
		public static void Throws<T>(Action action) where T:Exception
		{
			Throws(typeof(T), action);
		}
		public static void DoesNotThrow(Action action, string? msg = null)
		{
			try { action(); }
			catch (Exception ex)
			{
				throw new UnitTestException(VSLink + (msg ?? "exception was thrown"), ex);
			}
		}
	}

	public abstract class AttributeBase :Attribute
	{}

	/// <summary>Marks a class as a unit test class</summary>
	[AttributeUsage(AttributeTargets.Class,AllowMultiple=true)]
	public class TestFixtureAttribute :AttributeBase {}

	/// <summary>Called once just after creation of a TestFixture class</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class TestFixtureSetUpAttribute :AttributeBase {}

	/// <summary>Called once after the last test in a TestFixture class has been executed</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class TestFixtureTearDownAttribute :AttributeBase {}

	/// <summary>Called just before a unit test in a test fixture is executed</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class SetUpAttribute :AttributeBase {}

	/// <summary>Called just after a unit test in a test fixture has executed</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class TearDownAttribute :AttributeBase {}

	/// <summary>Marks a method as a unit test method</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class TestAttribute :AttributeBase {}
}

