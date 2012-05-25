//*********************************************
// Lua Input
//	(C)opyright Rylogic Limited 2007
//*********************************************

#ifndef LDR_LUA_INPUT_H
#define LDR_LUA_INPUT_H

#include "pr/lua/Lua.h"
#include "LineDrawer/Source/Forward.h"

namespace pr { class LuaConsole; }
class LuaInput
{
public:
	LuaInput(LineDrawer& linedrawer);
	~LuaInput();

	static LuaInput& Get()	{ return *m_this; }
	void DoFile(std::string const& lua_file);
	void DoString(std::string const& lua_string);
	
	void CreateGUI();
	void ShowConsole(bool yes);

	// Lua C functions
	int ldrClear		(lua_State* lua_state);
	int ldrScript		(lua_State* lua_state);
	int ldrDelete		(lua_State* lua_state);
	int ldrGetNumObjects(lua_State* lua_state);
	int ldrSetObjectColour();
	int ldrSetObjectTransform();
	int ldrSetObjectPosition();
	int ldrView();
	int ldrViewAll();
	int ldrRender();

private:
	static LuaInput*    m_this;
	pr::lua::Lua        m_lua;
	pr::LuaConsole*		m_console;
	LineDrawer*			m_linedrawer;
	DataManager*		m_data_manager;
};

#endif//LDR_LUA_INPUT_H
