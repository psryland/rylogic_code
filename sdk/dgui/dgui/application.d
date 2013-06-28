/*
	Copyright (c) 2011 - 2012 Trogu Antonio Davide

	This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

module dgui.application;

pragma(lib, "gdi32.lib");
pragma(lib, "comdlg32.lib");

private import dgui.core.winapi;
private import dgui.core.utils;
private import dgui.richtextbox;
private import dgui.form;
private import dgui.button;
private import dgui.label;
private import std.utf: toUTFz;
private import std.file;
private import std.conv;
public import dgui.resources;

private enum
{
	INFO = "Exception Information:",
	XP_MANIFEST_FILE = "dgui.xml.manifest",
	ERR_MSG = "An application exception has occured.\r\n1) Click \"Ignore\" to continue (The program can be unstable).\r\n2) Click \"Quit\" to exit.\r\n",
	XP_MANIFEST = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>` "\r\n"
					`<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">` "\r\n"
					  `<assemblyIdentity` "\r\n"
						`version="1.0.0.0"` "\r\n"
						`processorArchitecture="X86"` "\r\n"
						`name="client"` "\r\n"
						`type="win32"` "\r\n"
						`/>` "\r\n"
						`<description></description>` "\r\n"
						"\r\n"
						`<!-- Enable Windows XP and higher themes with common controls -->` "\r\n"
						`<dependency>` "\r\n"
							`<dependentAssembly>` "\r\n"
							  `<assemblyIdentity` "\r\n"
								`type="win32"` "\r\n"
								`name="Microsoft.Windows.Common-Controls"` "\r\n"
								`version="6.0.0.0"` "\r\n"
								`processorArchitecture="X86"` "\r\n"
								`publicKeyToken="6595b64144ccf1df"` "\r\n"
								`language="*"` "\r\n"
						 	    `/>` "\r\n"
							`</dependentAssembly>` "\r\n"
						 `</dependency>` "\r\n"
						  "\r\n"
						  `<!-- Disable Windows Vista UAC compatibility heuristics -->` "\r\n"
						  `<trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">` "\r\n"
							`<security>` "\r\n"
								`<requestedPrivileges>` "\r\n"
									`<requestedExecutionLevel level="asInvoker"/>` "\r\n"
								`</requestedPrivileges>` "\r\n"
							`</security>` "\r\n"
						  `</trustInfo> ` "\r\n"
						  "\r\n"
						  `<!-- Enable Windows Vista-style font scaling on Vista -->` "\r\n"
						  `<asmv3:application xmlns:asmv3="urn:schemas-microsoft-com:asm.v3">` "\r\n"
						  `<asmv3:windowsSettings xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">` "\r\n"
						  `<dpiAware>true</dpiAware>` "\r\n"
						  `</asmv3:windowsSettings>` "\r\n"
						  `</asmv3:application>` "\r\n"
					`</assembly>` "\r\n",
}
private alias extern(Windows) BOOL function(HANDLE hActCtx, ULONG_PTR* lpCookie) ActivateActCtxProc;
private alias extern(Windows) HANDLE function(ACTCTXW* pActCtx) CreateActCtxWProc;
private alias extern(Windows) bool function(INITCOMMONCONTROLSEX*) InitCommonControlsExProc;

/** 
   The _Application class manage the whole program, it can be used for load embedded resources, 
   close the program, get the current path and so on.
   Internally in initialize manifest (if available), DLLs, and it handle exceptions showing a window with exception information.
  */
class Application
{
	private static class ExceptionForm: Form
	{		
		public this(Throwable e)
		{
			this.text = "An Exception was thrown...";
			this.size = Size(400, 220);
			this.controlBox = false;
			this.startPosition = FormStartPosition.CENTER_PARENT;
			this.formBorderStyle = FormBorderStyle.FIXED_DIALOG;

			this._lblHead = new Label();
			this._lblHead.alignment = TextAlignment.MIDDLE | TextAlignment.LEFT;
			this._lblHead.foreColor = Color(0xB4, 0x00, 0x00);
			this._lblHead.dock = DockStyle.TOP;
			this._lblHead.height = 50;
			this._lblHead.text = ERR_MSG;
			this._lblHead.parent = this;
			
			this._lblInfo = new Label();
			this._lblInfo.alignment = TextAlignment.MIDDLE | TextAlignment.LEFT;
			this._lblInfo.dock = DockStyle.TOP;
			this._lblInfo.height = 20;
			this._lblInfo.text = INFO;
			this._lblInfo.parent = this;
			
			this._rtfText = new RichTextBox();
			this._rtfText.borderStyle = BorderStyle.FIXED_3D;
			this._rtfText.dock = DockStyle.TOP;
			this._rtfText.height = 90;
			this._rtfText.backColor = SystemColors.colorBtnFace;
			this._rtfText.scrollBars = true;
			this._rtfText.readOnly = true;
			this._rtfText.text = e.msg;
			this._rtfText.parent = this;

			this._btnQuit = new Button();
			this._btnQuit.bounds = Rect(310, 164, 80, 23);
			this._btnQuit.dialogResult = DialogResult.ABORT;
			this._btnQuit.text = "Quit";
			this._btnQuit.parent = this;

			this._btnIgnore = new Button();
			this._btnIgnore.bounds = Rect(225, 164, 80, 23);
			this._btnIgnore.dialogResult = DialogResult.IGNORE;
			this._btnIgnore.text = "Ignore";
			this._btnIgnore.parent = this;
		}

		private RichTextBox _rtfText;
		private Label _lblHead;
		private Label _lblInfo;
		private Button _btnIgnore;
		private Button _btnQuit;
	}

	/// Static constructor (it enable the manifest, if available)
	public static this()
	{
		Application.enableManifest(); //Enable Manifest (if available)
	}

	/** 
	      This method calls GetModuleHandle() API
	      
		Returns: 
			HINSTANCE of the program
	  */
	@property public static HINSTANCE instance()
	{
		return getHInstance();
	}
	
	/**
		Returns: 
			String value of the executable path ($(B including) the executable name)
	   */
	@property public static string executablePath()
	{
		return getExecutablePath();
	}

	/**
	   This method calls GetTempPath() API
		
		Returns: 
			String value of the system's TEMP directory
	   */
	@property public static string tempPath()
	{
		return dgui.core.utils.getTempPath();
	}

	/**
	   Returns: 
		String value of the executable path ($(B without) the executable name)
	   */
	@property public static string startupPath()
	{
		return getStartupPath();
	}

	/**
	   This property allows to load embedded _resources.
	   
		Returns:
			The Instance of reource object
		
		See_Also: 
			Resources Class
	 */
	@property public static Resources resources()
	{
		return Resources.instance;
	}

	/**
	   Internal method that enable XP Manifest (if available)
	 */
	private static void enableManifest()
	{
		HMODULE hKernel32 = getModuleHandle("kernel32.dll");

		if(hKernel32)
		{
			CreateActCtxWProc createActCtx = cast(CreateActCtxWProc)GetProcAddress(hKernel32, "CreateActCtxW");

			if(createActCtx) // Don't break Win2k compatibility
			{
				string temp = dgui.core.utils.getTempPath();
				ActivateActCtxProc activateActCtx = cast(ActivateActCtxProc)GetProcAddress(hKernel32, "ActivateActCtx");
				temp = std.path.buildPath(temp, XP_MANIFEST_FILE);
				std.file.write(temp, XP_MANIFEST);
	
				ACTCTXW actx;

				actx.cbSize = ACTCTXW.sizeof;
				actx.dwFlags = 0;
				actx.lpSource = toUTFz!(wchar*)(temp);

				HANDLE hActx = createActCtx(&actx);

				if(hActx != INVALID_HANDLE_VALUE)
				{
					ULONG_PTR cookie;
					activateActCtx(hActx, &cookie);
				}

				if(std.file.exists(temp))
				{
					std.file.remove(temp);
				}
			}
		}

		initCommonControls();
	}

	/**
	  Internal method that loads ComCtl32 DLL
	  */
	private static void initCommonControls()
	{
		INITCOMMONCONTROLSEX icc = void;

		icc.dwSize = INITCOMMONCONTROLSEX.sizeof;
		icc.dwICC = 0xFFFFFFFF;

		HMODULE hComCtl32 = loadLibrary("comctl32.dll");

		if(hComCtl32)
		{
			InitCommonControlsExProc iccex = cast(InitCommonControlsExProc)GetProcAddress(hComCtl32, "InitCommonControlsEx");

			if(iccex)
			{
				iccex(&icc);
			}
		}
	}

	/**
	  Start the program and handles handles Exception
	  Params:
		mainForm = The Application's main form
	  
	  Returns:
		Zero
	  */
	
	private static int doRun(Form mainForm)
	{
		try
		{
			mainForm.show();
		}
		catch(Throwable e)
		{
			switch(Application.showExceptionForm(e))
			{
				case DialogResult.ABORT:
					TerminateProcess(GetCurrentProcess(), -1);
					break;

				case DialogResult.IGNORE:
					Application.doRun(mainForm);
					break;

				default:
					break;
			}
		}
		
		return 0;
	}
	
	/**
	  Start the program and adds onClose() event at the MainForm
	  Params:
		mainForm = The Application's main form
	  
	  Returns:
		Zero
	  */
	public static int run(Form mainForm)
	{
		mainForm.close.attach(&onMainFormClose);
		return Application.doRun(mainForm);
	}

	/**
	  Close the program.
	  Params:
		exitCode = Exit code of the program (usually is 0)
	  */	
	public static void exit(int exitCode = 0)
	{
		ExitProcess(exitCode);
	}

	/**
	  When an exception was thrown, the _Application class call this method
	  showing the exception information, the user has the choice to continue the
	  application or terminate it.
	
	  Returns:
		A DialogResult enum that contains the button clicked by the user (IGNORE or EXIT)
	  */	
	package static DialogResult showExceptionForm(Throwable e)
	{
		ExceptionForm ef = new ExceptionForm(e);
		return ef.showDialog();
	}

	/**
	  Close _Application event attached (internally) at the main form
	  */
	private static void onMainFormClose(Control sender, EventArgs e)
	{
		Application.exit();
	}
}