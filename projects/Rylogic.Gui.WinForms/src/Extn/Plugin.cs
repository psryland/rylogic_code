using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;
using Rylogic.Plugin;

namespace Rylogic.Gui.WinForms
{
	public static class Plugins_
	{
		/// <summary>Scans a directory for assemblies that contain public types with the 'PluginAttribute' attribute that implement 'TInterface'.</summary>
		public static Plugins<TInterface> LoadWithUI<TInterface>(this Plugins<TInterface> pi, Control owner, string directory, object[] args, SearchOption search, string regex_filter = Plugins<TInterface>.DefaultRegexPattern, int delay = 500, string title = null, string desc = null, Icon icon = null) where TInterface : class
		{
			title = title ?? "Loading Plugins";
			desc = desc ?? $"Scanning for implementations of {typeof(TInterface).Name}";

			var dis = Dispatcher.CurrentDispatcher;
			var ui = new ProgressForm(title, desc, icon, ProgressBarStyle.Continuous, (s, a, cb) =>
			{
				pi.Load(directory, args, search, regex_filter, factory, progress);
				TInterface factory(Type ty, object[] arg2)
				{
					return (TInterface)dis.Invoke(() => Activator.CreateInstance(ty, args));
				}
				bool progress(string file, double frac)
				{
					cb(new ProgressForm.UserState { Description = $"{desc}\r\n{file}", FractionComplete = frac });
					if (s.CancelPending) throw new OperationCanceledException();
					return true;
				}
			});
			using (ui)
				ui.ShowDialog(owner, delay);

			// Report any plugins that failed to load
			if (pi.Failures.Count != 0)
			{
				var msg = new StringBuilder("The following plugins failed to load:\r\n");
				foreach (var x in pi.Failures)
				{
					msg.AppendLine($"{x.Name}");
					msg.AppendLine("Reason:");
					msg.AppendLine($"   {x.Error.Message}");
					msg.AppendLine();
				}

				MsgBox.Show(owner, msg.ToString(), "Plugin Load Failures", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

			return pi;
		}
	}
}
