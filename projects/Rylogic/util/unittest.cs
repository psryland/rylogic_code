//***************************************************
// UnitTest Runner
//  Copyright (c) Rylogic Ltd 2014
//***************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using pr.extn;
using pr.stream;

namespace pr.unittests
{
	public class UnitTest
	{
		/// <summary>Run the unit tests in this assembly</summary>
		public static bool RunLocalTests()
		{
			return RunTests(Assembly.GetExecutingAssembly(), System.Console.OpenStandardOutput());
		}

		/// <summary>
		/// Loads a .NET assembly and searches for any 'TestFixture' marked classes.
		/// Any found are then executed. Returns true if all tests passed.
		/// Diagnostic output is written to 'output'</summary>
		public static bool RunTests(string assembly_filepath, Stream outstream)
		{
			return RunTests(Assembly.LoadFile(assembly_filepath), outstream);
		}
		public static bool RunTests(Assembly ass, Stream outstream)
		{
			try
			{
				bool all_passed = true;
				using (var outp = new StreamWriter(new UncloseableStream(outstream)))
				{
					// Look for test fixtures
					var test_fixtures = FindTestFixtures(ass).ToList();
					foreach (var fixture in test_fixtures)
					{
						object inst = null;
						try { inst = Activator.CreateInstance(fixture); }
						catch (Exception ex)
						{
							outp.WriteLine("{0} - Failed to create an instance of test fixture\n{1}".Fmt(fixture.Name, ex.Message));
							outp.Flush();
							all_passed = false;
							continue;
						}


						// Call the fixture setup method
						var setup = fixture.FindMethodsWithAttribute<TestFixtureSetUpAttribute>().FirstOrDefault();
						if (setup != null) try
						{
							setup.Invoke(inst, null);
						}
						catch (Exception ex)
						{
							outp.WriteLine("{0} - Test fixture setup function threw\n{1}".Fmt(fixture.Name, ex.Message));
							outp.Flush();
							all_passed = false;
						}

						// Find the setup/cleanup method to call before each unit test
						var test_setup = fixture.FindMethodsWithAttribute<SetUpAttribute>().FirstOrDefault();
						var test_clean = fixture.FindMethodsWithAttribute<TearDownAttribute>().FirstOrDefault();

						// Find the unit tests
						foreach (var test in fixture.FindMethodsWithAttribute<TestAttribute>())
						{
							try
							{
								// Call setup for the test
								if (test_setup != null)
									test_setup.Invoke(inst, null);

								// Run the test
								test.Invoke(inst, null);

								// Call cleanup for the test
								if (test_clean != null)
									test_clean.Invoke(inst, null);
							}
							catch (Exception ex)
							{
								if (ex is TargetInvocationException) ex = ex.InnerException;
								outp.WriteLine("\r\nTest {0} Failed\r\n{1}".Fmt(test.Name, ex.MessageFull()));
								outp.Flush();
								all_passed = false;
							}
						}

						// Call the fixture cleanup method
						var cleanup = fixture.FindMethodsWithAttribute<TestFixtureTearDownAttribute>().FirstOrDefault();
						if (cleanup != null) try
						{
							cleanup.Invoke(inst, null);
						}
						catch (Exception ex)
						{
							outp.WriteLine("{0} - Test fixture cleanup function threw\n{1}".Fmt(fixture.Name, ex.Message));
							outp.Flush();
							all_passed = false;
						}
					}
				}
				return all_passed;
			}
			catch (Exception)
			{
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

	public class Assert
	{
		/// <summary>Return a file and line link</summary>
		private static string VSLink
		{
			get
			{
				var st = new System.Diagnostics.StackTrace(true);
				var sf = st.GetFrame(2);
				var file = sf.GetFileName();
				var line = sf.GetFileLineNumber();
				return "{0}({1}) : ".Fmt(file, line);
			}
		}

		public static void True(bool test)
		{
			if (test) return;
			throw new Exception(VSLink + "result is not 'True'");
		}
		public static void False(bool test)
		{
			if (!test) return;
			throw new Exception(VSLink + "result is not 'False'");
		}
		public static void Null(object ptr)
		{
			if (ptr == null) return;
			throw new Exception(VSLink + "reference is not Null");
		}
		public static void NotNull(object ptr)
		{
			if (ptr != null) return;
			throw new Exception(VSLink + "reference is Null");
		}
		public static void AreSame(object lhs, object rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return;
			throw new Exception(VSLink + "references are not equal");
		}
		public static void AreEqual(object lhs, object rhs)
		{
			if (Equals(lhs, rhs)) return;
			throw new Exception(VSLink + "values are not equal\r\n  lhs: {0}\r\n  rhs: {1}".Fmt(lhs.ToString(), rhs.ToString()));
		}
		public static void AreEqual(double lhs, double rhs, double tol)
		{
			if (Math.Abs(rhs - lhs) < tol) return;
			throw new Exception(VSLink + "values are not equal\r\n  lhs: {0}\r\n  rhs: {1}\r\n  tol: {2}".Fmt(lhs, rhs, tol));
		}
		public static void AreNotEqual(object lhs, object rhs)
		{
			if (!Equals(lhs, rhs)) return;
			throw new Exception(VSLink + "values are equal");
		}
		public static void Throws(Type exception_type, Action action)
		{
			try
			{
				action();
				throw new Exception(VSLink + "no exception was thrown");
			}
			catch (Exception ex)
			{
				if (exception_type.IsAssignableFrom(ex.GetType())) return;
				throw new Exception(VSLink + "exception of the wrong type thrown", ex);
			}
		}
		public static void Throws<T>(Action action) where T:Exception
		{
			Throws(typeof(T), action);
		}
		public static void DoesNotThrow(Action action, string msg = null)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				throw new Exception(VSLink + (msg ?? "exception was thrown"), ex);
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

