module dllmain;

import core.dll_helper;
import std.c.windows.windows;
import std.stdio;
import std.conv;
import dfl.all;
import prd.ldr.ldr;

__gshared HINSTANCE g_hInst;

extern (Windows)
{
	BOOL DllMain(HINSTANCE hInstance, ULONG ulReason, LPVOID pvReserved)
	{
		switch (ulReason)
		{
		case DLL_PROCESS_ATTACH:
			g_hInst = hInstance;
			dll_process_attach( hInstance, true );
			break;

		case DLL_PROCESS_DETACH:
			dll_process_detach( hInstance, true );
			break;

		case DLL_THREAD_ATTACH:
			dll_thread_attach( true, true );
			break;

		case DLL_THREAD_DETACH:
			dll_thread_detach( true, true );
			break;
		}
		return true;
	}
}
extern (C)
{
	export void ldrInitialise(ContextId ctx_id, const(char)* args)
	{
		Application.enableVisualStyles();
		g_plugin = new LdrExamplePlugin(ctx_id, args);
	}
	export void ldrUnInitialise()
	{
		delete g_plugin;
	}
	export void ldrStep(double elapsed_s)
	{
		if (!g_plugin) return;
		g_plugin.Step(elapsed_s);
	}
}

// Global plugin object
LdrExamplePlugin g_plugin = null;

// Plugin class
class LdrExamplePlugin :Form
{
	ContextId  m_ctx_id;
	LdrObject* m_ldr;

	this(ContextId ctx_id, const(char)* args)
	{
		m_ctx_id = ctx_id;
		m_ldr = ldrRegisterObject(m_ctx_id, "*box ldrpi {1 *o2w{*randpos {0 0 0 1}}}");
		icon = Application.resources.getIcon(101);
		show();
	}
	~this()
	{
	}
	void Step(double elapsed_s)
	{
		ldrStatus(("hello from D " ~ to!(string)(elapsed_s)).ptr, false, EStatusPri.Normal, 1000);
	}
}

