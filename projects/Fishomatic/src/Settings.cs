using System;
using System.ComponentModel;
using System.Windows;
using Rylogic.Common;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;

namespace Fishomatic
{
	public class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			TargetWindowName = "World of Warcraft";
			AlwaysOnTop = true;
			MoveCursor = true;
			CastKey = '7';
			MoveThreshold = 9;
			SearchArea = new Thickness(8);
			SmallSearchSize = new Size(50, 50);
			TargetColour = 0xFF962C1E;
			ColourTolerence = 50;
			ClickDelay = TimeSpan.FromMilliseconds(250);
			AfterCastWait = TimeSpan.FromSeconds(3);
			AfterCatchWait = TimeSpan.FromSeconds(3);
			MaxFishCycle = TimeSpan.FromSeconds(17);
			AbortTime = TimeSpan.FromSeconds(8);
			BaublesKey = '9';
			FishingPoleKey = '0';
			BaublesTime = TimeSpan.FromMinutes(11);
			BaublesApplyWait = TimeSpan.FromSeconds(6);

			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			: base(filepath)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary></summary>
		[Description("The window title to search for")]
		public string TargetWindowName
		{
			get => get<string>(nameof(TargetWindowName));
			set => set(nameof(TargetWindowName), value);
		}

		/// <summary></summary>
		[Description("Fishomatic window always on top")]
		public bool AlwaysOnTop
		{
			get => get<bool>(nameof(AlwaysOnTop));
			set => set(nameof(AlwaysOnTop), value);
		}

		/// <summary></summary>
		[Description("Move the cursor to the click position")]
		public bool MoveCursor
		{
			get => get<bool>(nameof(MoveCursor));
			set => set(nameof(MoveCursor), value);
		}

		/// <summary></summary>
		[Description("The key to press to cast")]
		public char CastKey
		{
			get => get<char>(nameof(CastKey));
			set => set(nameof(CastKey), value);
		}

		/// <summary></summary>
		[Description("How far the bobber moves before clicking to catch fish")]
		public int MoveThreshold
		{
			get => get<int>(nameof(MoveThreshold));
			set => set(nameof(MoveThreshold), value);
		}

		/// <summary></summary>
		[Description("The padding, relative to the target window, in which to search for the bobber")]
		public Thickness SearchArea
		{
			get => get<Thickness>(nameof(SearchArea));
			set => set(nameof(SearchArea), value);
		}

		/// <summary></summary>
		[Description("The size of the area to search while tracking the bobber")]
		public Size SmallSearchSize
		{
			get => get<Size>(nameof(SmallSearchSize));
			set => set(nameof(SmallSearchSize), value);
		}

		/// <summary></summary>
		[Description("The colour to search for")]
		public Colour32 TargetColour
		{
			get => get<Colour32>(nameof(TargetColour));
			set => set(nameof(TargetColour), value);
		}

		/// <summary></summary>
		[Description("Tolerance when searching for the target colour")]
		public int ColourTolerence
		{
			get => get<int>(nameof(ColourTolerence));
			set => set(nameof(ColourTolerence), value);
		}

		/// <summary></summary>
		[Description("Length of time to wait between detecting the bobber's moved and clicking (ms)")]
		public TimeSpan ClickDelay
		{
			get => get<TimeSpan>(nameof(ClickDelay));
			set => set(nameof(ClickDelay), value);
		}

		/// <summary></summary>
		[Description("Length of time to wait after casting before looking for the bobber (ms)")]
		public TimeSpan AfterCastWait
		{
			get => get<TimeSpan>(nameof(AfterCastWait));
			set => set(nameof(AfterCastWait), value);
		}

		/// <summary></summary>
		[Description("Length of time to wait after catch a fish before casting again")]
		public TimeSpan AfterCatchWait
		{
			get => get<TimeSpan>(nameof(AfterCatchWait));
			set => set(nameof(AfterCatchWait), value);
		}

		/// <summary></summary>
		[Description("The maximum length of time the fishing process can take")]
		public TimeSpan MaxFishCycle
		{
			get => get<TimeSpan>(nameof(MaxFishCycle));
			set => set(nameof(MaxFishCycle), value);
		}

		/// <summary></summary>
		[Description("The length of time to wait before deciding the bobber can't be found")]
		public TimeSpan AbortTime
		{
			get => get<TimeSpan>(nameof(AbortTime));
			set => set(nameof(AbortTime), value);
		}

		/// <summary></summary>
		[Description("The key to press to apply baubles to a fishing pole")]
		public char BaublesKey
		{
			get => get<char>(nameof(BaublesKey));
			set => set(nameof(BaublesKey), value);
		}

		/// <summary></summary>
		[Description("The key to press to select your fishing pole when applying baubles")]
		public char FishingPoleKey
		{
			get => get<char>(nameof(FishingPoleKey));
			set => set(nameof(FishingPoleKey), value);
		}

		/// <summary></summary>
		[Description("The time to wait (in minutes) between reapplication of baubles")]
		public TimeSpan BaublesTime
		{
			get => get<TimeSpan>(nameof(BaublesTime));
			set => set(nameof(BaublesTime), value);
		}

		/// <summary></summary>
		[Description("The time to wait while baubles are being applied")]
		public TimeSpan BaublesApplyWait
		{
			get => get<TimeSpan>(nameof(BaublesApplyWait));
			set => set(nameof(BaublesApplyWait), value);
		}

		/// <summary>A rect for the small search area, centred on 'position'</summary>
		public Rect SmallSearchArea(Point position)
		{
			return new Rect(
				position.X - SmallSearchSize.Width / 2,
				position.Y - SmallSearchSize.Height / 2,
				SmallSearchSize.Width,
				SmallSearchSize.Height);
		}

		/// <summary>A rect for the large search area, relative to 'window_rect'</summary>
		public Rect LargeSearchArea(Rect window_rect)
		{
			return window_rect.ShrinkBy(SearchArea);
		}
	}
}
