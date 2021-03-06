using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Rylogic.Extn;

namespace Rylogic.Utility
{
	/// <summary>Utility functions for finding problems in a .NET application</summary>
	public static class Sherlock
	{
		/// <summary>Reflects 'thing' for all event handlers and outputs a summary of the handlers signed up</summary>
		public static List<string> CheckEvents(object thing)
		{
			var type = thing.GetType();
			var sb = new StringBuilder();
			var output = new List<string>();
			var signed_up = new List<string>();

			// Events are field members of 'thing'
			var fields = type.AllFields(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).ToList();
			foreach (var mcd_info in fields.Where(x => x.FieldType.BaseType == typeof(MulticastDelegate)))
			{
				signed_up.Clear();

				// Get the instance of the multicast delegate from 'thing'
				if (mcd_info.GetValue(thing) is MulticastDelegate multicast_delegate)
				{
					// Get the multicast delegate's invocation list
					var invocation_list = multicast_delegate.GetType().GetMethod(nameof(MulticastDelegate.GetInvocationList));

					// Get the delegates subscribed to the event
					if (invocation_list?.Invoke(multicast_delegate, null) is Delegate[] delegates)
					{
						// Get the full names of the methods signed up, so we can look for potential duplicates
						var methods = delegates.Select(x => $"{x.Method.DeclaringType?.FullName ?? "<unk>"}.{x.Method.Name}").OrderBy(x => x);
						signed_up.AddRange(methods);
					}
				}

				// Add a row to the report
				sb.Append(type.Name,".",mcd_info.Name," - ",signed_up.Count," handlers").AppendLine();
				signed_up.ForEach(x => sb.Append("   ",x).AppendLine());
				output.Add(sb.ToString());
				sb.Clear();
			}
			return output;
		}

		/// <summary>Enumerates the handlers attached to the event 'owner.evt'</summary>
		public static IEnumerable<Delegate> EnumHandlers(object owner, string evt)
		{
			var type = owner.GetType();

			// Find the event on 'owner' with the name 'evt'
			// Have to use 'AllFields' because GetField doesn't find private members in base classes
			var fi_evt = type.AllFields(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).FirstOrDefault(x => x.Name == evt);
			if (fi_evt == null || fi_evt.FieldType.BaseType != typeof(MulticastDelegate))
				throw new Exception($"Object {type.Name} has no event called {evt}");

			// Get the instance of the multicast delegate from 'owner'
			if (fi_evt.GetValue(owner) is MulticastDelegate multicast_delegate)
			{
				// Get the multicast delegate's invocation list
				var invocation_list = multicast_delegate.GetType().GetMethod(nameof(MulticastDelegate.GetInvocationList));

				// Get the delegates subscribed to the event
				if (invocation_list?.Invoke(multicast_delegate, null) is Delegate[] delegates)
				{
					// Enum the attached handlers
					foreach (var del in delegates)
						yield return del;
				}
			}
			yield break;
		}

		/// <summary>Returns the count of the number of handlers attached to the event 'owner.evt'</summary>
		public static int CountHandlers(object owner, string evt)
		{
			return EnumHandlers(owner,evt).Count();
		}

		/// <summary>Counts the number of references to 'referenced' in the handlers for event 'owner.evt'</summary>
		public static int CountReferences(object owner, string evt, object referenced)
		{
			return EnumHandlers(owner,evt).Count(x => ReferenceEquals(x.Target, referenced));
		}

		/// <summary>Checks event 'owner.evt' for any handlers containing references to 'referenced'</summary>
		public static bool CheckForReferences(object owner, string evt, object referenced)
		{
			return CountReferences(owner, evt, referenced) != 0;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Utility;

	[TestFixture] public class TestSherlock
	{
		#pragma warning disable 67, 169, 649
		private class Thing
		{
			public Thing()
			{
				PrivateEvent += Handler;
				PrivateCustomEvent += Handler;
			}

			private event EventHandler? PrivateEvent;
			protected event EventHandler? ProtectedEvent;
			public event EventHandler? PublicEvent;
			private event EventHandler<CustEventArgs>? PrivateCustomEvent;
			public event EventHandler<CustEventArgs>? PublicCustomEvent;
			public class CustEventArgs :EventArgs {}

			public void Handler(object? sender, EventArgs e)
			{}
		}
		#pragma warning restore 67, 169, 649

		[Test] public void EventRefCount()
		{
			var thing = new Thing();
			thing.PublicEvent       += thing.Handler;
			thing.PublicCustomEvent += thing.Handler;
			thing.PublicEvent       += thing.Handler;
			thing.PublicCustomEvent += thing.Handler;
			thing.PublicEvent       += thing.Handler;
			thing.PublicEvent       += thing.Handler;
				
			var expected = new List<string>
			{
				"Thing.PrivateEvent - 1 handlers\r\n"+
				"   Rylogic.UnitTests.TestSherlock+Thing.Handler\r\n",

				"Thing.ProtectedEvent - 0 handlers\r\n",

				"Thing.PublicEvent - 4 handlers\r\n"+
				"   Rylogic.UnitTests.TestSherlock+Thing.Handler\r\n"+
				"   Rylogic.UnitTests.TestSherlock+Thing.Handler\r\n"+
				"   Rylogic.UnitTests.TestSherlock+Thing.Handler\r\n"+
				"   Rylogic.UnitTests.TestSherlock+Thing.Handler\r\n",

				"Thing.PrivateCustomEvent - 1 handlers\r\n"+
				"   Rylogic.UnitTests.TestSherlock+Thing.Handler\r\n",

				"Thing.PublicCustomEvent - 2 handlers\r\n"+
				"   Rylogic.UnitTests.TestSherlock+Thing.Handler\r\n"+
				"   Rylogic.UnitTests.TestSherlock+Thing.Handler\r\n",
			};
			var report = Sherlock.CheckEvents(thing);
			Assert.Equal(expected.Count, report.Count);
			for (int i = 0; i != expected.Count; ++i)
				Assert.Equal(expected[i], report[i]);
		}
		[Test] public void FindRefs()
		{
			var thing1 = new Thing();
			var thing2 = new Thing();

			thing1.PublicEvent += thing1.Handler;

			Assert.True (Sherlock.CheckForReferences(thing1, "PublicEvent"       , thing1));
			Assert.True (Sherlock.CheckForReferences(thing1, "PrivateEvent"      , thing1));
			Assert.True (Sherlock.CheckForReferences(thing1, "PrivateCustomEvent", thing1));

			Assert.False(Sherlock.CheckForReferences(thing1, "PublicEvent"       , thing2));
			Assert.False(Sherlock.CheckForReferences(thing1, "PrivateEvent"      , thing2));
			Assert.False(Sherlock.CheckForReferences(thing1, "PrivateCustomEvent", thing2));

			thing1.PublicEvent -= thing1.Handler;

			Assert.False(Sherlock.CheckForReferences(thing1, "PublicEvent"       , thing1));
			Assert.True (Sherlock.CheckForReferences(thing1, "PrivateEvent"      , thing1));
			Assert.True (Sherlock.CheckForReferences(thing1, "PrivateCustomEvent", thing1));
		}
	}
}
#endif
