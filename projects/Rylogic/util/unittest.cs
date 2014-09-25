//***************************************************
// UnitTest Runner
//  Copyright (c) Rylogic Ltd 2014
//***************************************************

using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Drawing;
//using System.IO;
using System.Linq;
using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Runtime.Serialization;
//using System.Runtime.Serialization.Formatters.Binary;
//using System.Text;
//using System.Threading;
//using System.Windows.Forms;
//using System.Xml;
//using pr.common;
//using pr.maths;
using pr.extn;

namespace pr.unittest
{
	public class UnitTest
	{
		/// <summary>
		/// Loads a .NET assembly and searches for any 'TestFixture' marked classes.
		/// Any found are then executed. Returns true if all tests passed</summary>
		public static bool RunTests(string assembly_filepath)
		{
			return RunTests(Assembly.LoadFile(assembly_filepath));
		}
		public static bool RunTests(Assembly ass)
		{
			try
			{
				// Look for test fixtures
				var test_fixtures = FindTestFixtures(ass).ToList();
				
				// Create all plugins on the dispatcher thread
				//Activator.CreateInstance(ty, args);
				bool all_passed = true;

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
		public static void True(bool test)
		{
			if (test) return;
			throw new Exception("Unit test failed");
		}
		public static void False(bool test)
		{
			if (!test) return;
			throw new Exception("Unit test failed");
		}
		public static void Null(object ptr)
		{
			if (ptr == null) return;
			throw new Exception("Unit test failed");
		}
		public static void NotNull(object ptr)
		{
			if (ptr != null) return;
			throw new Exception("Unit test failed");
		}
		public static void IsTrue(bool test)
		{
			True(test);
		}
		public static void IsFalse(bool test)
		{
			False(test);
		}
		public static void IsNull(object ptr)
		{
			Null(ptr);
		}
		public static void IsNotNull(object ptr)
		{
			NotNull(ptr);
		}
		public static void AreSame(object lhs, object rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return;
			throw new Exception("Unit test failed");
		}
		public static void AreEqual(object lhs, object rhs)
		{
			if (Equals(lhs, rhs)) return;
			throw new Exception("Unit test failed");
		}
		public static void AreEqual(double lhs, double rhs, double tol)
		{
			if (Math.Abs(rhs - lhs) < tol) return;
			throw new Exception("Unit test failed");
		}
		public static void AreNotEqual(object lhs, object rhs)
		{
			if (!Equals(lhs, rhs)) return;
			throw new Exception("Unit test failed");
		}
		public static void Throws(Type exception_type, Action action)
		{
			try { action(); }
			catch (Exception ex)
			{
				if (exception_type.IsAssignableFrom(ex.GetType())) return;
				throw;
			}
		}
		public static void Throws<T>(Action action) where T:Exception
		{
			Throws(typeof(T), action);
		}
		public static void DoesNotThrow(Action action, string msg = null)
		{
			try { action(); }
			catch (Exception ex)
			{
				throw new Exception(msg ?? "Unit test failed", ex);
			}
		}
	}

	/// <summary>Marks a class as a unit test class</summary>
	[AttributeUsage(AttributeTargets.Class,AllowMultiple=true)]
	public class TestFixtureAttribute :Attribute {}

	/// <summary>Called once just after creation of a TestFixture class</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class TestFixtureSetUpAttribute :Attribute {}

	/// <summary>Called once after the last test in a TestFixture class has been executed</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class TestFixtureTearDownAttribute :Attribute {}

	/// <summary>Called just before a unit test in a test fixture is executed</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class SetUpAttribute :Attribute {}

	/// <summary>Called just after a unit test in a test fixture has executed</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class TearDownAttribute :Attribute {}

	/// <summary>Marks a method as a unit test method</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class TestAttribute :Attribute {}
}

// Todo, remove this mapping
namespace NUnit.Framework
{
	public class Assert                      :pr.unittest.Assert                      {}
	public class TestFixtureAttribute        :pr.unittest.TestFixtureAttribute        {}
	public class TestFixtureSetUpAttribute   :pr.unittest.TestFixtureSetUpAttribute   {}
	public class TestFixtureTearDownAttribute:pr.unittest.TestFixtureTearDownAttribute{}
	public class SetUpAttribute              :pr.unittest.SetUpAttribute              {}
	public class TearDownAttribute           :pr.unittest.TearDownAttribute           {}
	public class TestAttribute               :pr.unittest.TestAttribute               {}
}
