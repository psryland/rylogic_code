using System;
using System.IO;
using System.Windows;

namespace Rylogic.Plugin
{
	public static class Plugins_
	{
		/// <summary>Load plugins showing a UI if it takes too long</summary>
		public static Plugins<TInterface> LoadWithUI<TInterface>(this Plugins<TInterface> plugins, Window owner, string directory, object[]? args, SearchOption search, string? regex_filter = Plugins<TInterface>.DefaultRegexPattern, int delay = 500, string? title = null, string? desc = null)
			where TInterface:class
		{
			// Need to implement ProgressUI in WPF
			throw new NotImplementedException();
			//if (title == null) title = "Loading Plugins";
			//if (desc == null) desc = $"Scanning for implementations of {typeof(TInterface).Name}";

			//var dis = Dispatcher.CurrentDispatcher;

			//var plugins = (Plugins<TInterface>)null;
			//var progress = new ProgressForm(title, desc, icon, ProgressBarStyle.Continuous, (s,a,cb) =>
			//{
			//	plugins = Load(directory, args, search, regex_filter, dis, (file,frac) =>
			//	{
			//		cb(new ProgressForm.UserState{Description = $"{desc}\r\n{file}", FractionComplete = frac});
			//		if (s.CancelPending) throw new OperationCanceledException();
			//	});
			//});
			//using (progress)
			//	progress.ShowDialog(parent, delay);

			//// Report any plugins that failed to load
			//if (plugins.Failures.Count != 0)
			//{
			//	var msg = new StringBuilder("The following plugins failed to load:\r\n");
			//	foreach (var x in plugins.Failures)
			//	{
			//		msg.AppendLine($"{x.Item1}");
			//		msg.AppendLine("Reason:");
			//		msg.AppendLine($"   {x.Item2.Message}");
			//		msg.AppendLine();
			//	}

			//	MsgBox.Show(parent, msg.ToString(), "Plugin Load Failures", MessageBoxButtons.OK, MessageBoxIcon.Information);
			//}
			//return plugins;
		}
	}
}
