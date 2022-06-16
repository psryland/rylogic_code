using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Rylogic.Common;
using Rylogic.Utility;

namespace TimeTracker
{
	public class Settings : SettingsBase<Settings>
	{
		public Settings()
		{
			AlwaysOnTop = true;
			RemindersEnabled = true;
			ScreenPosition = new Point(50, 50);
			ReminderTime = TimeSpan.FromHours(1);
			ResetEachDay = true;
			AutoRemoveOldTasks = false;
			MaxAge = TimeSpan.FromHours(24);
			TaskNames = new HashSet<string>(new EqualNoCase())
			{
				"Procrastinating",
			};
		}
		public Settings(string filepath)
			: base(filepath)
		{
			AutoSaveOnChanges = true;
		}

		/// <inheritdoc/>
		public override string Version => "v1.0";

		/// <summary></summary>
		public static new string Filepath => Util.ResolveUserDocumentsPath("Rylogic", "TimeTracker", "settings.xml");

		/// <summary>Keep above all other windows</summary>
		public bool AlwaysOnTop
		{
			get => get<bool>(nameof(AlwaysOnTop));
			set => set(nameof(AlwaysOnTop), value);
		}

		/// <summary>True when reminders are enabled</summary>
		public bool RemindersEnabled
		{
			get => get<bool>(nameof(RemindersEnabled));
			set => set(nameof(RemindersEnabled), value);
		}

		/// <summary>The position of the window on screen</summary>
		public Point ScreenPosition
		{
			get => get<Point>(nameof(ScreenPosition));
			set => set(nameof(ScreenPosition), value);
		}

		/// <summary>How often to remind the user about time keeping</summary>
		public TimeSpan ReminderTime
		{
			get => get<TimeSpan>(nameof(ReminderTime));
			set => set(nameof(ReminderTime), value);
		}

		/// <summary>Remove time periods older than this</summary>
		public TimeSpan MaxAge
		{
			get => get<TimeSpan>(nameof(MaxAge));
			set => set(nameof(MaxAge), value);
		}

		/// <summary>Remove tasks older than 'MaxAge'</summary>
		public bool AutoRemoveOldTasks
		{
			get => get<bool>(nameof(AutoRemoveOldTasks));
			set => set(nameof(AutoRemoveOldTasks), value);
		}

		/// <summary>Remove tasks not from the current day</summary>
		public bool ResetEachDay
		{
			get => get<bool>(nameof(ResetEachDay));
			set => set(nameof(ResetEachDay), value);
		}

		/// <summary>Task names</summary>
		public HashSet<string> TaskNames
		{
			get => get<HashSet<string>>(nameof(TaskNames));
			set => set(nameof(TaskNames), value);
		}

		/// <summary>Equal strings</summary>
		private class EqualNoCase : IEqualityComparer<string>
		{
			public bool Equals(string? x, string? y) => Misc.SameTaskName(x, y);
			public int GetHashCode([DisallowNull] string obj) => obj.GetHashCode();
		}
	}
}
