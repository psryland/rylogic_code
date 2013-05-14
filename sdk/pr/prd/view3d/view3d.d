module prd.view3d.view3d;

import dgui.all;
import std.stdio;
import std.conv;

public enum EView3DResult
{
	Success,
	Failed,
}

public class View3d :Control
{
	public this()
	{
	}

	// Initialise the control
	public void Initialise(HWND hwnd)
	{
		View3D_Initialise(hwnd, &ErrorCB, &SettingsChangedCB);
	}
}

extern (Windows)
{
	void ErrorCB(const (char)* msg)
	{
		writeln(to!(string)(msg));
		MessageBoxA(null, msg, "Error", MB_OK);
	}

	void SettingsChangedCB()
	{
	}

	private alias void function(const(char)* msg)  View3D_ReportErrorCB;
	private alias void function()                  View3D_SettingsChanged;
	//private alias extern (Windows) void function(std::size_t vcount,
	//                                              std::size_t icount,
	//                                              View3DVertex* verts,
	//                                              pr::uint16* indices,
	//                                              std::size_t& new_vcount,
	//                                              std::size_t& new_icount,
	//                                              EView3DPrim::Type& model_type,
	//                                              View3DMaterial& mat,
	//                                              void* ctx) View3D_EditObjectCB;

	EView3DResult View3D_Initialise(HWND hwnd, View3D_ReportErrorCB error_cb, View3D_SettingsChanged settings_changed_cb);
}