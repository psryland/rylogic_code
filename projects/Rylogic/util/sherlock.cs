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
			Type type = thing.GetType();
			List<string> output = new List<string>();
			var sb = new StringBuilder();
			var signed_up = new List<string>();

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
					var invocation_list = multicast_delegate.GetType().GetMethod(Reflect<MulticastDelegate>.MemberName(x => x.GetInvocationList()));

					// Get the delegates subscribed to the event
					var delegates = (Delegate[])invocation_list.Invoke(multicast_delegate,null);

					// Get the full names of the methods signed up, so we can look for potential duplicates
					var fullname_info = typeof(MethodBase).GetProperty("FullName", BindingFlags.NonPublic|BindingFlags.Instance);
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
	}
}
#endif
