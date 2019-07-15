﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Rylogic.Common;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Rylogic.Gui.WPF
{
	public static class WPFUtil
	{
		/// <summary>Ensure Dispose is called during Application.Current.Exit</summary>
		public static void DisposeAtExit(this IDisposable dis)
		{
			Application.Current.Exit += CallDispose;
			void CallDispose(object sender, ExitEventArgs args)
			{
				Application.Current.Exit -= CallDispose;
				dis.Dispose();
			}
		}

		/// <summary>Blocks until the debugger is attached</summary>
		public static void WaitForDebugger()
		{
			if (Debugger.IsAttached) return;
			var dlg = new ProgressUI(null,
				"Application Paused",
				"Waiting for the debugger to attach.\n\nClick 'Cancel' to skip",
				SystemIcons.Information.ToBitmapSource(),
				CancellationToken.None,
				(u, o, p) =>
				{
					for (; !Debugger.IsAttached && !u.CancelPending; Thread.Sleep(100))
						p(new ProgressUI.UserState { });
				})
			{
				ProgressIsIndeterminate = true,
			};
			using (dlg)
				dlg.ShowDialog();
		}

		///// <summary>Helper to ignore the requirement for matched called to Cursor.Show()/Hide()</summary>
		//public static bool ShowCursor
		//{
		//	get { return m_cursor_visible; }
		//	set
		//	{
		//		if (ShowCursor == value) return;
		//		m_cursor_visible = value;
		//		if (value) Cursor.Show();
		//		else Cursor.Hide();
		//	}
		//}
		//private static bool m_cursor_visible = true;

		/// <summary>Move a screen-space rectangle so that it is within the virtual screen</summary>
		public static Rect OnScreen(Rect rect)
		{
			var scn = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
			if (rect.Right > scn.Right) rect.X = scn.Right - rect.Width;
			if (rect.Bottom > scn.Bottom) rect.Y = scn.Bottom - rect.Height;
			if (rect.Left < scn.Left) rect.X = scn.Left;
			if (rect.Top < scn.Top) rect.Y = scn.Top;
			return rect;
		}
		public static Point OnScreen(Point location, Size size)
		{
			return OnScreen(new Rect(location, size)).Location;
		}

		///// <summary>Convert this mouse button flag into an index of first button that is down</summary>
		//public static int ButtonIndex(MouseButtons button)
		//{
		//	if (Bit.AllSet(button, MouseButtons.Left)) return 1;
		//	if (Bit.AllSet(button, MouseButtons.Right)) return 2;
		//	if (Bit.AllSet(button, MouseButtons.Middle)) return 3;
		//	if (Bit.AllSet(button, MouseButtons.XButton1)) return 4;
		//	if (Bit.AllSet(button, MouseButtons.XButton2)) return 5;
		//	return 0;
		//}
		//public const int MouseButtonCount = 6;

		/// <summary>Convert to native win32 MK values</summary>
		public static EMouseBtns ToMouseBtns(this MouseEventArgs btns, ModifierKeys modifiers = ModifierKeys.None)
		{
			var res = (EMouseBtns)0;
			if (btns.LeftButton == MouseButtonState.Pressed) res |= EMouseBtns.Left;
			if (btns.RightButton == MouseButtonState.Pressed) res |= EMouseBtns.Right;
			if (btns.MiddleButton == MouseButtonState.Pressed) res |= EMouseBtns.Middle;
			if (btns.XButton1 == MouseButtonState.Pressed) res |= EMouseBtns.XButton1;
			if (btns.XButton2 == MouseButtonState.Pressed) res |= EMouseBtns.XButton2;
			if (modifiers.HasFlag(ModifierKeys.Shift)) res |= EMouseBtns.Shift;
			if (modifiers.HasFlag(ModifierKeys.Control)) res |= EMouseBtns.Ctrl;
			return res;
		}

		/// <summary>Convert to native win32 VK_ value</summary>
		public static EKeyCodes ToKeyCode(this Key key)
		{
			return (EKeyCodes)KeyInterop.VirtualKeyFromKey(key);
		}

		///// <summary>Returns true if 'point' is more than the drag size from 'ref_point'</summary>
		//public static bool Moved(Point point, Point ref_point)
		//{
		//	return Utility.Util.Moved(point, ref_point, SystemInformation.DragSize.Width, SystemInformation.DragSize.Height);
		//}

		///// <summary>Test for design mode. Note: Prefer the Control extension method over this. This is just for non-Control classes</summary>
		//public static bool InDesignMode
		//{
		//	get { return LicenseManager.UsageMode == LicenseUsageMode.Designtime; }
		//}

		///// <summary>
		///// Recursively checks the AutoScaleMode and AutoScaleDimensions of all container controls below 'root'.
		///// 'on_failure' is a callback that receives each failing control. Return true from 'on_failure' to end the recursion.
		///// Note: If you use want an assert check, use 'AssertAutoScaling'. Also, 'AutoScaleDimensions' gets changed by .net
		///// when run on non-96dpi systems</summary>
		//public static bool CheckAutoScaling(Control root, AutoScaleMode? mode_ = null, SizeF? dim_ = null, Func<Control, bool> on_failure = null)
		//{
		//	// Guidelines: http://stackoverflow.com/questions/22735174/how-to-write-winforms-code-that-auto-scales-to-system-font-and-dpi-settings
		//	var mode = mode_ ?? (root as ContainerControl)?.AutoScaleMode ?? AutoScaleMode.Font;
		//	var dim = dim_ ?? (root as ContainerControl)?.AutoScaleDimensions ?? new SizeF(6F, 13F);
		//	on_failure = on_failure ?? (c => false);

		//	var failed = false;
		//	if (root is ContainerControl cc)
		//	{
		//		if (cc.AutoScaleMode == AutoScaleMode.Inherit)
		//		{
		//			var parent = cc.Parents().OfType<ContainerControl>().FirstOrDefault(p => p.AutoScaleMode != AutoScaleMode.Inherit);
		//			if (parent == null || parent.AutoScaleMode != mode || parent.AutoScaleDimensions != dim)
		//			{
		//				failed = true;
		//				if (on_failure(root))
		//					return false;
		//			}
		//		}
		//		else if (cc.AutoScaleMode != mode)
		//		{
		//			failed = true;
		//			if (on_failure(root))
		//				return false;
		//		}
		//		// 'AutoScaleDimensions' gets changed on non-96dpi systems
		//		else if (cc.AutoScaleDimensions != dim)
		//		{
		//			failed = true;
		//			if (on_failure(root))
		//				return false;
		//		}
		//	}
		//	if (!failed)
		//	{
		//		foreach (var child in root.Controls.OfType<Control>())
		//		{
		//			if (!CheckAutoScaling(child, mode, dim, on_failure))
		//				return false;
		//		}
		//	}
		//	return true;
		//}

		///// <summary></summary>
		//[Conditional("DEBUG")]
		//public static void AssertAutoScaling(Control root, AutoScaleMode? mode_ = null, SizeF? dim_ = null)
		//{
		//	string failures = string.Empty;
		//	CheckAutoScaling(root, mode_, dim_, c =>
		//	{
		//		failures += $"{c.GetType().Name} {c.Name}\r\n";
		//		return false; // Find them all...
		//	});
		//	Debug.Assert(failures.Length == 0, $"Auto scaling properties not set correctly\r\n{failures}");
		//}

		///// <summary>Replace the AutoScaleMode and AutoScaleDimensions for all container controls below 'root'</summary>
		//public static void SetAutoScaling(Control root, AutoScaleMode? mode_ = null, SizeF? dim_ = null)
		//{
		//	var mode = mode_ ?? (root as ContainerControl)?.AutoScaleMode ?? AutoScaleMode.Font;
		//	var dim = dim_ ?? (root as ContainerControl)?.AutoScaleDimensions ?? new SizeF(6F, 13F);
		//	CheckAutoScaling(root, mode, dim, c =>
		//	{
		//		var cc = (ContainerControl)c;
		//		cc.AutoScaleMode = mode;
		//		cc.AutoScaleDimensions = dim;
		//		return false; // Do them all...
		//	});
		//}
	}
}
