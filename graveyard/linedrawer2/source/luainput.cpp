//*********************************************
// Lua Input
//	(C)opyright Rylogic Limited 2007
//*********************************************
#include "Stdafx.h"
#include "pr/GUI/LuaConsole.h"
#include "LineDrawer/Source/LuaInput.h"
#include "LineDrawer/Source/LineDrawer.h"

namespace pr
{
	namespace lua
	{
		int ldrClear		(lua_State* lua_state)		{ return LuaInput::Get().ldrClear (lua_state); }
		int ldrScript		(lua_State* lua_state)		{ return LuaInput::Get().ldrScript(lua_state); }
		int ldrDelete		(lua_State* lua_state)		{ return LuaInput::Get().ldrDelete(lua_state); }
		int ldrGetNumObjects(lua_State* lua_state)		{ return LuaInput::Get().ldrGetNumObjects(lua_state); }
	}//namespace lua
}//namespace pr

LuaInput* LuaInput::m_this = 0;

// Constructor
LuaInput::LuaInput(LineDrawer& linedrawer)
:m_console(0)
,m_linedrawer(&linedrawer)
,m_data_manager(&linedrawer.m_data_manager)
{
	m_this = this;
	m_lua.SetOutputFuncs(pr::lua::DebugPrint, pr::lua::DebugPrint, 0, 0);

	// Register functions
	lua::Register(m_lua, "ldr.Clear"			, lua::ldrClear);
	lua::Register(m_lua, "ldr.Script"			, lua::ldrScript);
	lua::Register(m_lua, "ldr.Delete"			, lua::ldrDelete);
	lua::Register(m_lua, "ldr.GetNumObjects"	, lua::ldrGetNumObjects);
}

// Destructor
LuaInput::~LuaInput()
{
	m_this = 0;
	delete m_console;
}

// Execute a lua file
void LuaInput::DoFile(std::string const& lua_file)
{
	lua::DoFile(m_lua, lua_file.c_str());
}

// Execute a lua string
void LuaInput::DoString(std::string const& lua_string)
{
	lua::DoString(m_lua, lua_string.c_str());
}

// Create the lua console 
void LuaInput::CreateGUI()
{
	m_console = new pr::LuaConsole(m_lua, (CWnd*)LineDrawer::Get().m_line_drawer_GUI);
	m_console->Create((CWnd*)LineDrawer::Get().m_line_drawer_GUI);
}

// Display the lua console
void LuaInput::ShowConsole(bool yes)
{
	m_console->ShowWindow(yes ? SW_SHOW : SW_HIDE);
}

// Clear all data from line drawer
// input: none
// output: none
// error: none
int LuaInput::ldrClear(lua_State*)
{
	m_data_manager->Clear();
	return 1;
}

// Send a line drawer string from lua to line drawer
// input:  One line drawer string
// output: Object handles in order of creation in the script
// error:  Returns an error string in the state
int LuaInput::ldrScript(lua_State* lua_state)
{
	// Check the parameter is a string
	if( !lua_isstring(lua_state, 1) )
	{
		lua_pop(lua_state, 1);
		lua_pushstring(lua_state, "Incorrect parameter type, should be a linedrawer string");
		return 0;
	}

	// Get the source string
	std::string ldr_str = lua_tostring(lua_state, 1);
	lua_pop(lua_state, 1);

	// Parse the string
	StringParser string_parser(m_linedrawer);
	if( !string_parser.Parse(ldr_str.c_str(), ldr_str.size()) )
	{
		lua_pushstring(lua_state, "Parse error in linedrawer string");
		return 0;
	}

	// Add the objects
	for( uint i = 0, i_end = string_parser.GetNumObjects(); i != i_end; ++i )
	{
		LdrObject* object = string_parser.GetObject(i);
		m_data_manager->AddObject(object);
		lua_pushlightuserdata(lua_state, object);
	}
	m_linedrawer->Refresh();
	return 0;
}

// Delete a particular object
// input: ObjectHandle
// output: none
int LuaInput::ldrDelete(lua_State* lua_state)
{
	LdrObject* object = static_cast<LdrObject*>(lua_touserdata(lua_state, 1));
	m_data_manager->DeleteObject(object);
	lua_pop(lua_state, 1);
	return 0;
}

// Return the number of top level line drawer objects
// input: none
// output: integer
int LuaInput::ldrGetNumObjects(lua_State* lua_state)
{
	lua_pushinteger(lua_state, m_data_manager->GetNumObjects());
	return 0;
}

int LuaInput::ldrSetObjectColour()
{
	return 0;
}

int LuaInput::ldrSetObjectTransform()
{
	return 0;
}

int LuaInput::ldrSetObjectPosition()
{
	return 0;
}

int LuaInput::ldrView()
{
	return 0;
}

int LuaInput::ldrViewAll()
{
	return 0;
}

int LuaInput::ldrRender()
{
	return 0;
}
