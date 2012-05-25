// LineDrawer plugin and helper functions
module prd.ldr.ldr;

import core.sys.windows.windows;

alias uint ContextId;
enum EStatusPri :uint { Normal, High };

struct LdrObject {};

// Linedrawer imports
extern (C)
{
	LdrObject* ldrRegisterObject       (ContextId ctx_id, const(char)* object_description);
	void       ldrUnRegisterObject     (ContextId ctx_id, LdrObject* object);
	void       ldrUnRegisterAllObjects (ContextId ctx_id);
	void       ldrRender               ();
	HWND       ldrMainWindowHandle     ();
	void       ldrError                (const(char)* err_msg);
	void       ldrStatus               (const(char)* msg, bool bold, EStatusPri priority, uint min_display_time_ms);
}
