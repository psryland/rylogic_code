using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Animation;

#if DEBUG
namespace Rylogic.Gui.WPF
{
	// Code from http://www.wpfmentor.com/2009/01/how-to-debug-triggers-using-trigger.html
	// No license specified - this code is trimmed out from Release build anyway so it should be ok using it this way
	//
	// How To:
	//   Add the following attached property to any trigger and you will see when it is activated/deactivated
	//   in the output window:
	//         TriggerTracing.TriggerName="your debug name"
	//         TriggerTracing.TraceEnabled="True"
	//
	// Example:
	//   <Trigger my:TriggerTracing.TriggerName="BoldWhenMouseIsOver"  
	//            my:TriggerTracing.TraceEnabled="True"  
	//            Property="IsMouseOver"  
	//            Value="True">  
	//       <Setter Property = "FontWeight" Value="Bold"/>  
	//   </Trigger> 
	//
	// As this works on anything that inherits from TriggerBase, it will also work on <MultiTrigger>.

	/// <summary>
	/// Contains attached properties to activate Trigger Tracing on the specified Triggers.
	/// This file alone should be dropped into your app.</summary>
	public static class TriggerTracing
	{
		static TriggerTracing()
		{
			// Initialise WPF Animation tracing and add a TriggerTraceListener
			PresentationTraceSources.Refresh();
			PresentationTraceSources.AnimationSource.Listeners.Clear();
			PresentationTraceSources.AnimationSource.Listeners.Add(new TriggerTraceListener());
			PresentationTraceSources.AnimationSource.Switch.Level = SourceLevels.All;
		}

		public static readonly DependencyProperty TriggerNameProperty =
			DependencyProperty.RegisterAttached("TriggerName", typeof(string), typeof(TriggerTracing), new UIPropertyMetadata(string.Empty));
		public static readonly DependencyProperty TraceEnabledProperty =
			DependencyProperty.RegisterAttached("TraceEnabled", typeof(bool), typeof(TriggerTracing), new UIPropertyMetadata(false, OnTraceEnabledChanged));


		/// <summary>Gets the trigger name for the specified trigger. This will be used to identify the trigger in the debug output.</summary>
		public static string GetTriggerName(TriggerBase trigger)
		{
			return (string)trigger.GetValue(TriggerNameProperty);
		}

		/// <summary>Sets the trigger name for the specified trigger. This will be used to identify the trigger in the debug output.</summary>
		public static void SetTriggerName(TriggerBase trigger, string value)
		{
			trigger.SetValue(TriggerNameProperty, value);
		}

		/// <summary>Gets a value indication whether trace is enabled for the specified trigger.</summary>
		public static bool GetTraceEnabled(TriggerBase trigger)
		{
			return (bool)trigger.GetValue(TraceEnabledProperty);
		}

		/// <summary>Sets a value specifying whether trace is enabled for the specified trigger</summary>
		public static void SetTraceEnabled(TriggerBase trigger, bool value)
		{
			trigger.SetValue(TraceEnabledProperty, value);
		}

		/// <summary></summary>
		private static void OnTraceEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{

			if (!(d is TriggerBase triggerBase))
				return;

			if (!(e.NewValue is bool))
				return;

			if ((bool)e.NewValue)
			{
				// insert dummy story-boards which can later be traced using WPF animation tracing

				var storyboard = new TriggerTraceStoryboard(triggerBase, TriggerTraceStoryboardType.Enter);
				triggerBase.EnterActions.Insert(0, new BeginStoryboard() { Storyboard = storyboard });

				storyboard = new TriggerTraceStoryboard(triggerBase, TriggerTraceStoryboardType.Exit);
				triggerBase.ExitActions.Insert(0, new BeginStoryboard() { Storyboard = storyboard });
			}
			else
			{
				// remove the dummy storyboards

				foreach (TriggerActionCollection actionCollection in new[] { triggerBase.EnterActions, triggerBase.ExitActions })
				{
					foreach (TriggerAction triggerAction in actionCollection)
					{
						BeginStoryboard bsb = triggerAction as BeginStoryboard;

						if (bsb != null && bsb.Storyboard != null && bsb.Storyboard is TriggerTraceStoryboard)
						{
							actionCollection.Remove(bsb);
							break;
						}
					}
				}
			}
		}

		/// <summary></summary>
		private enum TriggerTraceStoryboardType
		{
			Enter,
			Exit
		}

		/// <summary>A dummy storyboard for tracing purposes</summary>
		private class TriggerTraceStoryboard : Storyboard
		{
			public TriggerTraceStoryboard(TriggerBase triggerBase, TriggerTraceStoryboardType storyboardType)
			{
				TriggerBase = triggerBase;
				StoryboardType = storyboardType;
			}
			public TriggerTraceStoryboardType StoryboardType { get; private set; }
			public TriggerBase TriggerBase { get; private set; }
		}

		/// <summary>A custom trace listener.</summary>
		private class TriggerTraceListener : TraceListener
		{
			public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
			{
				base.TraceEvent(eventCache, source, eventType, id, format, args);

				if (format.StartsWith("Storyboard has begun;"))
				{
					if (args[1] is TriggerTraceStoryboard storyboard)
					{
						// add a breakpoint here to see when your trigger has been
						// entered or exited

						// the element being acted upon
						object targetElement = args[5];

						// The name scope of the element being acted upon
						var namescope = (INameScope)args[7];

						var triggerBase = storyboard.TriggerBase;
						var triggerName = GetTriggerName(storyboard.TriggerBase);
						Debug.WriteLine(string.Format("Element: {0}, {1}: {2}: {3}",
							targetElement,
							triggerBase.GetType().Name,
							triggerName,
							storyboard.StoryboardType));
					}
				}
			}

			public override void Write(string message)
			{
			}

			public override void WriteLine(string message)
			{
			}
		}
	}
}
#endif