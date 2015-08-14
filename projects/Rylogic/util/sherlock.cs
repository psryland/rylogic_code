using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using pr.extn;

namespace pr.util
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

			// Prop info for the 'FullName' property on MethodBase
			var fullname_info = typeof(MethodBase).GetProperty("FullName", BindingFlags.NonPublic|BindingFlags.Instance);

			// Events are field members of 'thing'
			var fields = type.AllFields(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).ToList();
			foreach (var mcd_info in fields.Where(x => x.FieldType.BaseType == typeof(MulticastDelegate)))
			{
				signed_up.Clear();

				// Get the instance of the multicast delegate from 'thing'
				var multicast_delegate = (MulticastDelegate)mcd_info.GetValue(thing);
				if (multicast_delegate != null)
				{
					// Get the mcd's invocation list
					var invocation_list = multicast_delegate.GetType().GetMethod(R<MulticastDelegate>.Name(x => x.GetInvocationList()));

					// Get the delegates subscribed to the event
					var delegates = (Delegate[])invocation_list.Invoke(multicast_delegate,null);

					// Get the full names of the methods signed up, so we can look for potential duplicates
					signed_up.AddRange(delegates.Select(x => (string)fullname_info.GetValue(x.Method, null)).OrderBy(x => x));
				}

				// Add a row to the report
				sb.Append(type.Name,".",mcd_info.Name," - ",signed_up.Count," handlers").AppendLine();
				signed_up.ForEach(x => sb.Append("   ",x).AppendLine());
				output.Add(sb.ToString());
				sb.Clear();
			}
			return output;
		}

		/// <summary>Checks event 'owner.evt' for any handlers containing references to 'referenced'</summary>
		public static bool CheckForReferences(object owner, string evt, object referenced)
		{
			var type = owner.GetType();

			// Find the event on 'owner' with the name 'evt'
			// Have to use 'AllFields' because GetField doesn't find private members in base classes
			var fi_evt = type.AllFields(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).FirstOrDefault(x => x.Name == evt);
			if (fi_evt == null || fi_evt.FieldType.BaseType != typeof(MulticastDelegate))
				throw new Exception("Object {0} has no event called {1}".Fmt(type.Name, evt));

			// Get the instance of the multicast delegate from 'owner'
			var multicast_delegate = (MulticastDelegate)fi_evt.GetValue(owner);
			if (multicast_delegate != null)
			{
				// Get the mcd's invocation list
				var invocation_list = multicast_delegate.GetType().GetMethod(R<MulticastDelegate>.Name(x => x.GetInvocationList()));

				// Get the delegates subscribed to the event
				var delegates = (Delegate[])invocation_list.Invoke(multicast_delegate,null);

				// Look for any delegates with references to 'referenced'
				if (delegates.Any(x => ReferenceEquals(x.Target, referenced)))
					return true;
			}
			return false;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using util;

	[TestFixture] public class TestSherlock
	{
		// ReSharper disable UnusedMember.Local, ClassNeverInstantiated.Local, EventNeverSubscribedTo.Local
		#pragma warning disable 67, 169, 649
		private class Thing
		{
			private event EventHandler PrivateEvent;
			protected event EventHandler ProtectedEvent;
			public event EventHandler PublicEvent;
			private event EventHandler<CustEventArgs> PrivateCustomEvent;
			public event EventHandler<CustEventArgs> PublicCustomEvent;
			public class CustEventArgs :EventArgs {}

			public Thing()
			{
				PrivateEvent       += Handler;
				PrivateCustomEvent += Handler;
			}
			public void Handler(object sender, EventArgs e)
			{}
		}
		#pragma warning restore 67, 169, 649
		// ReSharper restore UnusedMember.Local, ClassNeverInstantiated.Local, EventNeverSubscribedTo.Local

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
				"Thing.PrivateEvent - 1 handlers"+Environment.NewLine+
				"   pr.unittests.TestSherlock+Thing.Handler(System.Object, System.EventArgs)"+Environment.NewLine,
					
				"Thing.ProtectedEvent - 0 handlers"+Environment.NewLine,
					
				"Thing.PublicEvent - 4 handlers"+Environment.NewLine+
				"   pr.unittests.TestSherlock+Thing.Handler(System.Object, System.EventArgs)"+Environment.NewLine+
				"   pr.unittests.TestSherlock+Thing.Handler(System.Object, System.EventArgs)"+Environment.NewLine+
				"   pr.unittests.TestSherlock+Thing.Handler(System.Object, System.EventArgs)"+Environment.NewLine+
				"   pr.unittests.TestSherlock+Thing.Handler(System.Object, System.EventArgs)"+Environment.NewLine,
					
				"Thing.PrivateCustomEvent - 1 handlers"+Environment.NewLine+
				"   pr.unittests.TestSherlock+Thing.Handler(System.Object, System.EventArgs)"+Environment.NewLine,

				"Thing.PublicCustomEvent - 2 handlers"+Environment.NewLine+
				"   pr.unittests.TestSherlock+Thing.Handler(System.Object, System.EventArgs)"+Environment.NewLine+
				"   pr.unittests.TestSherlock+Thing.Handler(System.Object, System.EventArgs)"+Environment.NewLine,
			};
			var report = Sherlock.CheckEvents(thing);
			Assert.AreEqual(expected.Count, report.Count);
			for (int i = 0; i != expected.Count; ++i)
				Assert.AreEqual(expected[i], report[i]);
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
