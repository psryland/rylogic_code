//#define TRAP_UNHANDLED_EXCEPTIONS
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.INet;
using Rylogic.Utility;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	static class Program
	{
		private static StartupOptions StartupOptions;

		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		static void Main(string[] args)
		{
			// Set up the unhandled exception handler
			var unhandled = (Exception)null;
			#if !DEBUG || TRAP_UNHANDLED_EXCEPTIONS
			try {
			#endif

				Environment.ExitCode = 0;
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Xml_.Config.SupportWinFormsTypes();

				// Start the main app
				Exception err = null;
				try { StartupOptions = new StartupOptions(args); }
				catch (Exception ex) { err = ex; }

				// If there was an error display the error message
				if (err != null)
				{
					MsgBox.Show(null,
						"There is an error in the startup options provided.\r\n"+
						$"Error Details:\r\n{err.Message}"
						, "Command Line Error"
						, MessageBoxButtons.OK
						, MessageBoxIcon.Error);
					HelpUI.ShowDialog(null, HelpUI.EContent.Html, Application.ProductName, Resources.command_line_ref);
					Environment.ExitCode = 1;
					return;
				}

				// If they just want help displayed...
				if (StartupOptions.ShowHelp)
				{
					HelpUI.ShowDialog(null, HelpUI.EContent.Html, Application.ProductName, Resources.command_line_ref);
					Environment.ExitCode = 0;
					return;
				}

				// If an export path is given, run as a command line tool doing an export
				if (StartupOptions.ExportPath != null)
				{
					RyLogViewer.Main.ExportToFile(StartupOptions);
					return;
				}

				// Otherwise show the app
				Application.Run(new Main(StartupOptions));

				// To catch any Disposes in the 'GC Finializer' thread
				GC.Collect();

			#if !DEBUG || TRAP_UNHANDLED_EXCEPTIONS
			} catch (Exception e) { unhandled = e; }
			#endif

			// Report unhandled exceptions
			if (unhandled != null)
				HandleTheUnhandled(unhandled);
		}

		/// <summary>Handle unhandled exceptions</summary>
		private static void HandleTheUnhandled(Exception ex)
		{
			var res = MsgBox.Show(null,
				$"{Application.ProductName} has shutdown with the following error.\r\n"+
				$"Error: {ex.GetType().Name}\r\n"+
				$"\r\n"+
				$"Deleting the applications settings file, '{StartupOptions?.SettingsPath ?? Path_.FileName(Settings.Default.Filepath)}', might prevent this problem.\r\n"+
				$"\r\n"+
				$"Generating a report and sending it to {Util.GetAssemblyAttribute<AssemblyCompanyAttribute>().Company} will aid in the resolution of this issue. "+
				$"The generated report is a plain text file that you can review before sending.\r\n"+
				$"\r\n"+
				$"Would you like to generate the report?\r\n"+
				$"\r\n"+
				$"Alternatively, please contact {Constants.SupportEmail} with information about this error so that it can be fixed.\r\n"+
				$"\r\n"+
				$"Apologies for any inconvenience caused.\r\n",
				"Unexpected Termination", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
			if (res == DialogResult.Yes)
			{
				var dg = new SaveFileDialog{Title = "Save Crash Report", FileName = Application.ProductName+"CrashReport", Filter = Util.FileDialogFilter("Crash Report Files","*.txt", "All files","*.*"), DefaultExt = "txt", CheckPathExists = true};
				if (dg.ShowDialog() == DialogResult.OK)
				{
					var settings = "Settings filepath unknown";
					if (StartupOptions != null && Path_.FileExists(StartupOptions.SettingsPath))
						settings = File.ReadAllText(StartupOptions.SettingsPath);

					var sb = new StringBuilder()
						.Append(Application.ProductName).Append(" - Crash Report - ").Append(DateTime.UtcNow).AppendLine()
						.AppendLine("---------------------------------------------------------------")
						.AppendLine("[Unhandled Exception Type]")
						.AppendLine(ex.MessageFull())
						.AppendLine()
						.AppendLine("[Settings File Contents]")
						.AppendLine(settings)
						.AppendLine()
						.AppendLine("[General]")
						.AppendLine($"Application Version: {Util.AssemblyVersion()}")
						.AppendLine(Environment.OSVersion.VersionString)
						.AppendLine(StartupOptions?.Dump() ?? string.Empty)
						.AppendLine()
						.AppendLine("[Additional Comments]")
						.AppendLine("Any additional information about what you were doing when this crash occurred would be extremely helpful and appreciated");
					File.WriteAllText(dg.FileName, sb.ToString());

					try
					{
						if (MsgBox.Show(null, "Preview the report before sending?", "Review Report", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
						Process.Start(dg.FileName);
					} catch {}

					try
					{
						// Try to create an email with the attachment ready to go
						var email = new Email();
						email.AddRecipient(Constants.SupportEmail, Email.MAPIRecipient.To);
						email.Subject = Application.ProductName + " crash report";
						email.Body = string.Format(
							"To {0},\r\n" +
							"\r\n" +
							"Attached is a crash report generated on {1}\r\n" +
							"A brief description of how the application was being used at the time follows:\r\n" +
							"\r\n\r\n\r\n\r\n" +
							"Regards,\r\n" +
							"A Helpful User"
							,Application.CompanyName
							,DateTime.Now);
						email.Attachments.Add(dg.FileName);
						email.Send();
					} catch {}
				}
			}
			Environment.ExitCode = 1;
		}
	}
}
