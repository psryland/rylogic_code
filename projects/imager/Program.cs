using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using pr.util;

namespace imager
{
	static class Program
	{
		/// <summary>The main entry point for the application.</summary>
		[STAThread] static void Main(string[] args)
		{
			#if DEBUG 
			MessageBox.Show("Paws'd", "Imager");
			#endif

			Log.Open("imager.log", FileMode.Create);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			try
			{
				Log.Write("args: "); foreach (string s in args) {Log.Write(s+" ");} Log.Write("\n");

				// Parse screen saver arguments
				if (args.Length >= 1)
				{
					switch (args[0].ToLower().Substring(0,2))
					{
					default:
						Application.Run(new Imager(Imager.EMode.Normal, args, IntPtr.Zero));
						break;
					
					// Show the screen saver
					case "/s":
						Application.Run(new Imager(Imager.EMode.ScreenSaver, null, IntPtr.Zero));
						return;

					// Preview the screen saver
					case "/p":
						if (args.Length < 2) return; // the next arg is the handle to the preview window
						Application.Run(new Imager(Imager.EMode.Preview, null, new IntPtr(long.Parse(args[1]))));
						return;

					// Configure the screen saver
					case "/c":
						Settings settings = Settings.Load();
						if (new Config(settings).ShowDialog() == DialogResult.OK)
							settings.Save();
						return;
					}
				}
				else
				{
					// Otherwise run as a normal app
					Application.Run(new Imager(Imager.EMode.Normal, null, IntPtr.Zero));
				}
			}
			catch (Exception ex)
			{
				Log.Write("exception: "+ex+"\n");

				// Details of the error should have been display in an earlier message box
				#if DEBUG
				MessageBox.Show("An error has occurred that means Imager cannot run.\nError: "+ex.Message+"\nDetail:\n"+ex, "Imager Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				#else
				MessageBox.Show("An error has occurred that means Imager cannot run.\nError: "+ex.Message, "Imager Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				#endif
				return;
			}
			finally
			{
				Log.Write("shutdown\n");
			}
		}

		/// <summary>General error reporting callback</summary>
		public static void OnError(string msg)
		{
			MessageBox.Show(msg, "Imager Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}
