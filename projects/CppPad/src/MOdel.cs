using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using pr.common;
using pr.container;
using pr.extn;
using pr.util;

namespace CppPad
{
	/// <summary>App logic</summary>
	public class Model :IDisposable
	{
		public const string CodeFilePattern   = @"(^.*\.cpp$)|(^.*\.c$)|(^.*\.h$)";
		public const string SourceFilePattern = @"(^.*\.cpp$)|(^.*\.c$)";
		public const string HeaderFilePattern = @"(^.*\.h$)";

		public readonly static string CodeFileFilter   = Util.FileDialogFilter("C++ Source Files", "*.cpp", "C Source Files", "*.c", "Header Files", "*.h");
		public readonly static string SourceFileFilter = Util.FileDialogFilter("C++ Source Files", "*.cpp", "C Source Files", "*.c");
		public readonly static string HeaderFileFilter = Util.FileDialogFilter("Header Files", "*.h");

		public Model(Settings settings, MainUI owner)
		{
			Settings    = settings;
			Owner       = owner;
			Editors     = new BindingSource<EditorUI> { DataSource = new BindingListEx<EditorUI>() };
			BuildOutput = new OutputUI("Build");
			ProgOutput  = new OutputUI("Output");
		}
		public virtual void Dispose()
		{
		}

		/// <summary>App settings</summary>
		public Settings Settings { get; private set; }

		/// <summary>The form that owns this model</summary>
		public MainUI Owner { get; private set; }

		/// <summary>The code editor</summary>
		public BindingSource<EditorUI> Editors { get; set; }

		/// <summary>The build output UI</summary>
		public OutputUI BuildOutput { get; set; }

		/// <summary>The program output UI</summary>
		public OutputUI ProgOutput  { get; set; }

		/// <summary>The root directory containing the current source files</summary>
		public string ProjectDirectory
		{
			get { return m_impl_projdir; }
			set
			{
				if (m_impl_projdir == value) return;
				m_impl_projdir = value;
				Settings.LastProject = value;
			}
		}
		private string m_impl_projdir;

		/// <summary>Save all open editors</summary>
		public void SaveAll()
		{
			foreach (var editor in Editors)
				editor.Save(string.Empty);
		}

		/// <summary>Close all open editors, excluding 'but_this'</summary>
		public void CloseAll(string but_this = null)
		{
			foreach (var edit in Editors.ToArray())
			{
				if (but_this != null && PathEx.Compare(edit.Filepath, but_this) == 0) continue;
				edit.Dispose();
			}
		}

		/// <summary>Compile the project and run the result</summary>
		public void BuildAndRun()
		{
			// Save all open files
			if (Settings.SaveAllBeforeCompile)
			{
				foreach (var editor in Editors)
					editor.Save(string.Empty);
			}

			// Increment the build issue to cancel any previous builds
			var build_issue = ++m_build_issue;

			// Create a copy of the app settings to use for this compile
			var settings = new Settings(Settings, read_only:true);

			// Copy the project directory
			var proj_dir = ProjectDirectory;

			// Reset the build and output windows
			BuildOutput.Edit.ResetText();
			ProgOutput.Edit.ResetText();

			// Start an worker thread to do the build
			ThreadPool.QueueUserWorkItem(x =>
			{
				try
				{
					// Build the project. If successful, run the resulting program
					if (BuildProject(proj_dir, build_issue, settings))
						RunProject(proj_dir, build_issue, settings);
				}
				catch (Exception ex)
				{
					// Show exceptions as status messages
					Owner.BeginInvoke(() => Owner.Status.SetStatusMessage(msg:ex.Message, bold:true, fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5)));
				}
			});
		}
		private int m_build_issue;

		/// <summary>Compile the current project</summary>
		private bool BuildProject(string proj_dir, int build_issue, Settings settings) // Worker thread context
		{
			// Create the compiler to use
			var compiler =
				settings.CompilerType == Settings.ECompiler.MSVC ? new CompilerMSVC(settings.MSVC) :
				(Compiler)null;
			if (compiler == null)
				throw new Exception("Unknown compiler type");

			// Clean up the compiler on exit
			using (compiler)
			{
				// Read the source files from the project directory
				var files = PathEx.EnumFileSystem(ProjectDirectory, SearchOption.TopDirectoryOnly, SourceFilePattern, RegexOptions.IgnoreCase)
					.Select(x => x.FullPath).ToArray();

				// Nothing to build, done...
				if (files.Length == 0)
					return false;

				// Ensure a directory for the object files exists
				var obj_dir = PathEx.CombinePath(proj_dir, "obj");
				if (!PathEx.DirExists(obj_dir))
					Directory.CreateDirectory(obj_dir);

				// Create the compiler's build process
				using (var proc = compiler.CreateBuildProcess(files, obj_dir))
				{
					if (!proc.Start())
						return false;

					// Read from standard out for the build process
					var output_length = 0;
					var dec = Encoding.UTF8.GetDecoder();
					for (var buffer = new byte[1024];;)
					{
						// Read standard out
						var read = proc.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length);
						for (;!read.Wait(100) && build_issue == m_build_issue;) {}
						if (build_issue != m_build_issue) return false;

						// Read until the stream runs dry
						if (read.Result != 0)
						{
							// 'dec' has internal state and handles UTF characters that span reads.
							var text = new char[buffer.Length];
							var len = dec.GetChars(buffer, 0, read.Result, text, 0);
							output_length += len;

							// Set the text in the output window
							Owner.BeginInvoke(() =>
							{
								if (build_issue != m_build_issue) return;

								// ToDo, append text, preserve selection
								BuildOutput.Edit.Text += new string(text, 0, len);
							});
						}
						else if (proc.HasExited)
						{
							// Stop reading once all data is read and the process has exited
							break;
						}
					}

					// Get the result of the compile
					Debug.Assert(proc.HasExited);
					var exit_code = proc.ExitCode;
					return exit_code == 0;
				}
			}
		}

		/// <summary>Run the current project</summary>
		private void RunProject(string proj_dir, int build_issue, Settings settings) // Worker thread context
		{
			// On build complete,
			//   If failed, end the worker thread
			//   If success, start the compiled program in a new process, sending output to the 'output' window (and input from)
			//   Interrupt the 'exe' if the build issue changes
		}

		/// <summary>Called once the build is complete</summary>
		private void HandleBuildComplete()
		{
			// Note: called in a background thread

		}
	}
}
