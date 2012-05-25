//***********************************************
// Test Common
//***********************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using PR;

namespace PR.Common
{
	class Program
	{
		delegate int NativeMessageBox(int hwnd, string text, string caption, uint type);

		static void Main(string[] args)
		{
			using (DllWrapper user32dll = new DllWrapper("user32.dll"))
			{
				NativeMessageBox msg_box = (NativeMessageBox)user32dll.GetDelegate("MessageBoxA", typeof(NativeMessageBox));
				msg_box(0, "Hello World", "Test Native DLL", 0);
			}
		}
	}
}

