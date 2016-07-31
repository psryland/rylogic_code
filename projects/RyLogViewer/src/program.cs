using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.extn;
using pr.gui;
using pr.inet;
using pr.util;

namespace RyLogViewer
{
	static class program
	{
		private static StartupOptions StartupOptions;

		/// <summary>The main entry point for the application.</summary>
		[STAThread] [SecurityPermission(SecurityAction.Demand,Flags=SecurityPermissionFlag.ControlAppDomain)] // for the unhandled exception handler
		static void Main(string[] args)
		{
			// Setup the unhandled exception handler
			try { AppDomain.CurrentDomain.UnhandledException += HandleTheUnhandled; }
			catch (Exception ex) { Log.Exception(null, ex, "Failed to set unhandled exception handler"); }

			Environment.ExitCode = 0;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Start the main app
			Exception err = null;
			try { StartupOptions = new StartupOptions(args); }
			catch (Exception ex) { err = ex; }

			// If there was an error display the error message
			if (err != null)
			{
				MsgBox.Show(null,
					"There is an error in the startup options provided.\r\n"+
					"Error Details:\r\n{0}".Fmt(err.Message)
					,"Command Line Error"
					,MessageBoxButtons.OK
					,MessageBoxIcon.Error);
				HelpUI.ShowDialog(null, HelpUI.EContent.Html, Resources.AppTitle, Resources.command_line_ref);
				Environment.ExitCode = 1;
				return;
			}

			// If they just want help displayed...
			if (StartupOptions.ShowHelp)
			{
				HelpUI.ShowDialog(null, HelpUI.EContent.Html, Resources.AppTitle,  Resources.command_line_ref);
				Environment.ExitCode = 1;
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
		}

		/// <summary>Handle unhandled exceptions</summary>
		private static void HandleTheUnhandled(object sender, UnhandledExceptionEventArgs args)
		{
			var res = MsgBox.Show(null, string.Format(
				"{0} has shutdown with the following error.\r\n" +
				"Error: {1}\r\n" +
				"\r\n" +
				"Deleting the applications settings file, '{2}', might prevent this problem.\r\n" +
				"\r\n" +
				"Generating a report and sending it to {3} will aid in the resolution of this issue. The generated report is a plain text file that you can review before sending.\r\n" +
				"\r\n" +
				"Would you like to generate the report?\r\n" +
				"\r\n" +
				"Alternatively, please contact  {4}  with information about this error so that it can be fixed.\r\n" +
				"\r\n" +
				"Apologies for any inconvenience caused.\r\n"
				,Application.ProductName
				,args.ExceptionObject.GetType().Name
				,StartupOptions != null ? StartupOptions.SettingsPath : Path.GetFileName(Settings.Default.Filepath)
				,Util.GetAssemblyAttribute<AssemblyCompanyAttribute>().Company
				,Constants.SupportEmail)
				,"Unexpected Termination"
				,MessageBoxButtons.YesNo
				,MessageBoxIcon.Error);
			if (res == DialogResult.Yes)
			{
				var dg = new SaveFileDialog{Title = "Save Crash Report", FileName = Application.ProductName+"CrashReport", Filter = "Crash Report Files (*.txt)|*.txt|All files (*.*)|*.*", DefaultExt = "txt", CheckPathExists = true};
				if (dg.ShowDialog() == DialogResult.OK)
				{
					string settings = "Settings filepath unknown";
					if (StartupOptions != null && Path_.FileExists(StartupOptions.SettingsPath))
						settings = File.ReadAllText(StartupOptions.SettingsPath);

					var sb = new StringBuilder()
						.Append(Application.ProductName).Append(" - Crash Report - ").Append(DateTime.UtcNow).AppendLine()
						.AppendLine("---------------------------------------------------------------")
						.AppendLine("[Unhandled Exception Type]")
						.AppendLine(args.ExceptionObject.Dump())
						.AppendLine()
						.AppendLine("[Settings File Contents]")
						.AppendLine(settings)
						.AppendLine()
						.AppendLine("[General]")
						.AppendLine("Application Version: {0}".Fmt(Util.AssemblyVersion()))
						.AppendLine(Environment.OSVersion.VersionString);
					if (StartupOptions != null) sb
						.AppendLine(StartupOptions.Dump());
					sb
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
			Application.Exit();
		}
	}
}
