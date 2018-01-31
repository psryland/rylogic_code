//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Maths;

namespace Rylogic.Utility
{
	/// <summary>Utility function container</summary>
	public static class Util2
	{
		[DebuggerStepThrough] public static void DisposeAll<T>(ref BindingSource<T> doomed) where T:class, IDisposable
		{
			var doomed_ = (IList<T>)doomed;
			doomed = null;
			Util.DisposeAll<IList<T>,T>(ref doomed_);
		}
		[DebuggerStepThrough] public static BindingSource<T> DisposeAll<T>(this BindingSource<T> doomed) where T:class, IDisposable
		{
			return (BindingSource<T>)Util.DisposeAll<IList<T>, T>(doomed);
		}
		[DebuggerStepThrough] public static BindingListEx<T> DisposeAll<T>(this BindingListEx<T> doomed) where T:class, IDisposable
		{
			return (BindingListEx<T>)Util.DisposeAll<IList<T>, T>(doomed);
		}

		/// <summary>Blocks until the debugger is attached</summary>
		public static void WaitForDebugger()
		{
			if (Debugger.IsAttached) return;
			var t = new Thread(new ThreadStart(() =>
			{
				try
				{
					var mb = new MsgBox("Waiting for Debugger...", "Debugging", MessageBoxButtons.OK, MessageBoxIcon.Information) { PositiveBtnText = "Continue" };
					using (mb)
					{
						mb.Show(null);
						for (;mb.DialogResult == DialogResult.None && !Debugger.IsAttached;)
							Application.DoEvents();
					}
				}
				catch {}
			}));
			t.Start();
			t.Join();
		}

		/// <summary>
		/// Recursively checks the AutoScaleMode and AutoScaleDimensions of all container controls below 'root'.
		/// 'on_failure' is a callback that receives each failing control. Return true from 'on_failure' to end the recursion.
		/// Note: If you use want an assert check, use 'AssertAutoScaling'. Also, 'AutoScaleDimensions' gets changed by .net
		/// when run on non-96dpi systems</summary>
		public static bool CheckAutoScaling(Control root, AutoScaleMode? mode_ = null, SizeF? dim_ = null, Func<Control,bool> on_failure = null)
		{
			// Guidelines: http://stackoverflow.com/questions/22735174/how-to-write-winforms-code-that-auto-scales-to-system-font-and-dpi-settings
			var mode = mode_ ?? (root as ContainerControl)?.AutoScaleMode       ?? AutoScaleMode.Font;
			var dim  = dim_  ?? (root as ContainerControl)?.AutoScaleDimensions ?? new SizeF(6F, 13F);
			on_failure = on_failure ?? (c => false);

			var failed = false;
			if (root is ContainerControl cc)
			{
				if (cc.AutoScaleMode == AutoScaleMode.Inherit)
				{
					var parent = cc.Parents().OfType<ContainerControl>().FirstOrDefault(p => p.AutoScaleMode != AutoScaleMode.Inherit);
					if (parent == null || parent.AutoScaleMode != mode || parent.AutoScaleDimensions != dim)
					{
						failed = true;
						if (on_failure(root))
							return false;
					}
				}
				else if (cc.AutoScaleMode != mode)
				{
					failed = true;
					if (on_failure(root))
						return false;
				}
				// 'AutoScaleDimensions' gets changed on non-96dpi systems
				else if (cc.AutoScaleDimensions != dim)
				{
					failed = true;
					if (on_failure(root))
						return false;
				}
			}
			if (!failed)
			{
				foreach (var child in root.Controls.OfType<Control>())
				{
					if (!CheckAutoScaling(child, mode, dim, on_failure))
						return false;
				}
			}
			return true;
		}
		[Conditional("DEBUG")] public static void AssertAutoScaling(Control root, AutoScaleMode? mode_ = null, SizeF? dim_ = null)
		{
			string failures = string.Empty;
			CheckAutoScaling(root, mode_, dim_, c =>
			{
				failures += $"{c.GetType().Name} {c.Name}\r\n";
				return false; // Find them all...
			});
			Debug.Assert(failures.Length == 0, $"Auto scaling properties not set correctly\r\n{failures}");
		}

		/// <summary>Replace the AutoScaleMode and AutoScaleDimensions for all container controls below 'root'</summary>
		public static void SetAutoScaling(Control root, AutoScaleMode? mode_ = null, SizeF? dim_ = null)
		{
			var mode = mode_ ?? (root as ContainerControl)?.AutoScaleMode       ?? AutoScaleMode.Font;
			var dim  = dim_  ?? (root as ContainerControl)?.AutoScaleDimensions ?? new SizeF(6F, 13F);
			CheckAutoScaling(root, mode, dim, c =>
			{
				var cc = (ContainerControl)c;
				cc.AutoScaleMode = mode;
				cc.AutoScaleDimensions = dim;
				return false; // Do them all...
			});
		}

		/// <summary>The directory that this application executable is in</summary>
		public static string AppDirectory
		{
			get { return Path_.Directory(Application.ExecutablePath); }
		}

		/// <summary>Returns the full path to a file or directory relative to the app executable</summary>
		public static string ResolveAppPath(string relative_path = "")
		{
			return Path_.CombinePath(AppDirectory, relative_path);
		}

		/// <summary>Helper to ignore the requirement for matched called to Cursor.Show()/Hide()</summary>
		public static bool ShowCursor
		{
			get { return m_cursor_visible; }
			set
			{
				if (ShowCursor == value) return;
				m_cursor_visible = value;
				if (value) Cursor.Show(); else Cursor.Hide();
			}
		}
		private static bool m_cursor_visible = true;

		/// <summary>Returns true if 'point' is more than the drag size from 'ref_point'</summary>
		public static bool Moved(Point point, Point ref_point)
		{
			return Moved(point, ref_point, SystemInformation.DragSize.Width, SystemInformation.DragSize.Height);
		}

		/// <summary>Returns true if 'point' is more than 'dx' or 'dy' from 'ref_point'</summary>
		public static bool Moved(Point point, Point ref_point, int dx, int dy)
		{
			return Math.Abs(point.X - ref_point.X) > dx || Math.Abs(point.Y - ref_point.Y) > dy;
		}

		/// <summary>
		/// A helper for copying lib files to the current output directory
		/// 'src_filepath' is the source lib name with {platform} and {config} optional substitution tags
		/// 'dst_filepath' is the output lib name with {platform} and {config} optional substitution tags
		/// If 'dst_filepath' already exists then it is overridden if 'overwrite' is true, otherwise 'DestExists' is returned
		/// 'src_filepath' can be a relative path, if so, the search order is: local directory, Q:\sdk\pr\lib
		/// </summary>
		[Obsolete("Use python instead")] public static ELibCopyResult LibCopy(string src_filepath, string dst_filepath, bool overwrite)
		{
			// Do text substitutions
			src_filepath = src_filepath.Replace("{platform}", Environment.Is64BitProcess ? "x64" : "x86");
			dst_filepath = dst_filepath.Replace("{platform}", Environment.Is64BitProcess ? "x64" : "x86");

			const string config = Util.IsDebug ? "debug" : "release";
			src_filepath = src_filepath.Replace("{config}", config);
			dst_filepath = dst_filepath.Replace("{config}", config);

			// Check if 'dst_filepath' already exists
			dst_filepath = Path.GetFullPath(dst_filepath);
			if (!overwrite && Path_.FileExists(dst_filepath))
			{
				Log.Info(null, $"LibCopy: Not copying {src_filepath} as {dst_filepath} already exists");
				return ELibCopyResult.DestExists;
			}

			// Get the full path for 'src_filepath'
			for (;!Path.IsPathRooted(src_filepath);)
			{
				// If 'src_filepath' exists in the local directory
				var full = Path.GetFullPath(src_filepath);
				if (File.Exists(full))
				{
					src_filepath = full;
					break;
				}

				// Get the pr libs directory
				full = Path.Combine(@"P:\pr\sdk\lib", src_filepath);
				if (File.Exists(full))
				{
					src_filepath = full;
					break;
				}

				// Can't find 'src_filepath'
				Log.Info(null, $"LibCopy: Not copying {src_filepath}, file not found");
				return ELibCopyResult.SrcNotFound;
			}

			// Copy the file
			Log.Info(null, $"LibCopy: {src_filepath} -> {dst_filepath}");
			File.Copy(src_filepath, dst_filepath, true);
			return ELibCopyResult.Success;
		}
		public enum ELibCopyResult { Success, DestExists, SrcNotFound }
	
		/// <summary>
		/// Helper for making a file dialog file type filter string.
		/// Strings starting with *. are assumed to be extensions
		/// Use: FileDialogFilter("Text Files","*.txt","Bitmaps","*.bmp","*.png"); </summary>
		public static string FileDialogFilter(params string[] desc)
		{
			if (desc.Length == 0) return string.Empty;
			if (desc[0].StartsWith("*.")) throw new Exception("First string should be a file type description");
			
			var sb = new StringBuilder();
			var extn = new List<string>();
			for (int i = 0; i != desc.Length;)
			{
				if (sb.Length != 0) sb.Append("|");
				sb.Append(desc[i]);
				
				extn.Clear();
				for (++i; i != desc.Length && desc[i].StartsWith("*."); ++i)
					extn.Add(desc[i]);
				
				if (extn.Count == 0) throw new Exception("Expected file extension patterns to follow description");
				
				sb.Append(" (").Append(string.Join(",", extn)).Append(")");
				sb.Append("|").Append(string.Join(";", extn));
			}

			return sb.ToString();
		}

		/// <summary>
		/// Attempts to locate an application installation directory on the local machine.
		/// Returns the folder location if found, or null.
		/// 'look_in' are optional extra directories to check</summary>
		public static string LocateDir(string relative_path, params string[] look_in)
		{
			if (look_in.Length == 0)
				look_in = new []
				{
					Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
					Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
					@"C:\Program Files",
					@"C:\Program Files (x86)",
					@"D:\Program Files",
					@"D:\Program Files (x86)",
				};
				
			return look_in
				.Select(x => Path_.CombinePath(x, relative_path))
				.FirstOrDefault(Path_.DirExists);
		}

		/// <summary>Move a screen-space rectangle so that it is within the virtual screen</summary>
		public static Rectangle OnScreen(Rectangle rect)
		{
			var r = new RectangleRef(rect);
			var scn = SystemInformation.VirtualScreen;
			if (r.Right  > scn.Right ) r.X = scn.Right - rect.Width;
			if (r.Bottom > scn.Bottom) r.Y = scn.Bottom - rect.Height;
			if (r.Left   < scn.Left  ) r.X = scn.Left;
			if (r.Top    < scn.Top   ) r.Y = scn.Top;
			return r;
		}
		public static Point OnScreen(Point location, Size size)
		{
			return OnScreen(new Rectangle(location, size)).Location;
		}

		/// <summary>Convert this mouse button flag into an index of first button that is down</summary>
		public static int ButtonIndex(this MouseButtons button)
		{
			if (Bit.AllSet(button, MouseButtons.Left    )) return 1;
			if (Bit.AllSet(button, MouseButtons.Right   )) return 2;
			if (Bit.AllSet(button, MouseButtons.Middle  )) return 3;
			if (Bit.AllSet(button, MouseButtons.XButton1)) return 4;
			if (Bit.AllSet(button, MouseButtons.XButton2)) return 5;
			return 0;
		}
		public const int MouseButtonCount = 6;

		/// <summary>Test for design mode. Note: Prefer the Control extension method over this. This is just for non-Control classes</summary>
		public static bool InDesignMode
		{
			get { return LicenseManager.UsageMode == LicenseUsageMode.Designtime; }
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	[TestFixture] public class TestUtils
	{
		[Test] public void ToDo()
		{
		}
	}
}
#endif
