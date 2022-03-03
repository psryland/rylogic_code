//*********************************************
// Lua Wrapper
//  Copyright (c) Rylogic Ltd 2007
//*********************************************
// This file contains a helper object for creating a lua state and
// a collection of useful lua functions that operate on a lua state.
// Usage:
//   Create an instance of 'pr::Lua' in your program
//   Call lua functions like this:
//      pr::Lua lua;
//      std::string str = lua::ToString(lua.m_state, 0); // Return the item on the stack as a string
//      lua::RegisterFunction(lua.m_state, MyFunction);

#pragma once

extern "C"
{
	#include "lua/include/lua.h"
	#include "lua/include/lualib.h"
	#include "lua/include/lauxlib.h"
}
#include <string>
#include <algorithm>
#include <varargs.h>
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/str/string_util.h"

#ifndef PR_DBG_LUA
#define PR_DBG_LUA PR_DBG
#endif

#if PR_DBG_LUA
inline lua_State*& dbg_lua()
{
	static lua_State* s_dbg_lua = 0;
	return s_dbg_lua;
}
#endif

namespace pr::lua
{
	enum class EResult :long long
	{
		Success = 0,
		Failed = 0x80000000,
		FailedToInitialise,
		Incomplete,
		Exit,
		SyntaxError,
		MemoryError,
	};

	// Lua mapping function. Should return the number of results returned by the function
	typedef int (*MappingFunction)(lua_State*);

	// Return the lua version string
	inline char const* VersionString()
	{
		return LUA_RELEASE "  " LUA_COPYRIGHT;
	}

	// Return a string representation of an item on the stack without changing the item
	inline std::string ToString(lua_State* lua_state, int index)
	{
		switch (lua_type(lua_state, index))
		{
		case LUA_TNONE:    return "None";
		case LUA_TNIL:     return "nil";
		case LUA_TBOOLEAN: return lua_toboolean(lua_state, index) ? "true" : "false";
		case LUA_TNUMBER:  return Fmt("%g", lua_tonumber(lua_state, index));
		case LUA_TSTRING:  return lua_tostring(lua_state, index);
		case LUA_TTABLE:
		case LUA_TFUNCTION:
		case LUA_TUSERDATA:
		case LUA_TTHREAD:
		case LUA_TLIGHTUSERDATA:
		default: return Fmt("%s[%p]", luaL_typename(lua_state, index), lua_topointer(lua_state, index));
		}
	}

	// Output the item on the top of the stack using 'luamsg'
	// Pop's the string from the stack
	inline int LuaMessage(lua_State* lua_state)
	{
		lua_getglobal(lua_state, "luamsg");
		lua_insert(lua_state, lua_gettop(lua_state) - 1);
		lua_pcall(lua_state, 1, 0, 0);
		return 0;
	}

	// Output the item on the top of the stack using 'print'
	// Pop's the string from the stack
	inline int LuaPrint(lua_State* lua_state)
	{
		lua_getglobal(lua_state, "print");
		lua_insert(lua_state, lua_gettop(lua_state) - 1);
		lua_pcall(lua_state, 1, 0, 0);
		return 0;
	}

	// Directed output functions
	// These functions should not be called directly by C code. They are here
	// to allow the lua functions 'print' and 'luamsg' to be wired to something.
	// To output something and have it directed to the output that the client wants
	// call "LuaMessage()" or "LuaPrint()". LuaMessage() is for output originating
	// from lua (e.g. error messages). "LuaPrint()" is for program output
	inline int DebugPrint(lua_State* lua_state)
	{
		PR_INFO(PR_DBG_LUA, str::EnsureNewline(ToString(lua_state, 1)).c_str()); (void)lua_state;
		return 0;
	}
	inline int ConsolePrint(lua_State* lua_state)
	{
		printf("%s", ToString(lua_state, 1).c_str());
		return 0;
	}

	// Dump the lua stack into a string
	inline std::string DumpStack(lua_State* lua_state)
	{
		std::string str = "Lua Stack Dump:\n";
		for (int i = lua_gettop(lua_state), bot = 0; i > bot; --i)
			str += Fmt("%d) %s\n", i, ToString(lua_state, i).c_str());
		return str;
	}
	inline int LuaDumpStack(lua_State* lua_state)
	{
		lua_pushstring(lua_state, DumpStack(lua_state).c_str());
		return LuaMessage(lua_state);
	}

	// Dump the contents of a table at position 'table_index' on the stack into a string
	// If the item on the stack is a table, dump that, otherwise dump the global table.
	inline std::string DumpTable(lua_State* lua_state, int table_index)
	{
		bool remove_table = false;
		if (table_index == 0 || !lua_istable(lua_state, table_index))
		{
			lua_getglobal(lua_state, "_G");
			table_index = -1;
			remove_table = true;
		}

		// Convert the table index into an absolute index
		if (table_index < 0)
			table_index = lua_gettop(lua_state) + 1 + table_index;

		std::string str;
		lua_pushnil(lua_state);                   // first key
		while (lua_next(lua_state, table_index))  // 'key' is at index -2 and `value' at index -1
		{
			str += Fmt("%20s - %s\n", ToString(lua_state, -2).c_str(), ToString(lua_state, -1).c_str());
			lua_pop(lua_state, 1);                // removes `value'; keeps `key' for next iteration
		}

		if (remove_table)
			lua_pop(lua_state, 1);                // remove the table

		return str;
	}
	inline int LuaDumpTable(lua_State* lua_state)
	{
		lua_pushstring(lua_state, DumpTable(lua_state, -1).c_str());
		return LuaMessage(lua_state);
	}

	// Use this for forwarding global functions to member functions
	// i.e. In the object call: lua::AddUserPointer(lua, "MyClass", this);
	// In your registered global functions you can then do
	//	int MyRegisteredFunction(lua_State* lua_state)
	//	{
	//		return lua::GetUserPointer<MyClass>(lua, "MyClass")->MyRegisteredFunction(lua_state);
	//	}
	inline void AddUserPointer(lua_State* lua_state, char const* name, void* user)
	{
		lua_pushlightuserdata(lua_state, user);
		lua_setglobal(lua_state, name);
	}

	// Return a pointer that corresponds to 'name'
	template <typename T> inline T* GetUserPointer(lua_State* lua_state, char const* name)
	{
		lua_getglobal(lua_state, name);
		if (!lua_islightuserdata(lua_state, -1)) return 0;
		T* ptr = (T*)(lua_topointer(lua_state, -1));
		lua_pop(lua_state, 1);
		return ptr;
	}

	// Execute lua script
	inline bool DoString(lua_State* lua_state, char const* string)
	{
		int res = luaL_dostring(lua_state, string);
		if (res == 0) return true;
		LuaMessage(lua_state);
		return false;
	}
	inline bool DoFile(lua_State* lua_state, char const* filename)
	{
		int res = luaL_dofile(lua_state, filename);
		if (res == 0) return true;
		LuaMessage(lua_state);
		return false;
	}

	namespace impl
	{
		// Pushes onto the stack the tables specified by 'address'
		// 'address' should have the form "[table.sub_table.another_table.]function_or_variable"
		// When found, the stack will contain [table, sub_table, another_table]
		// Use 'lua_gettop(lua_state) == 0' to test if the stack is empty
		// If 'create' is true, creates tables according to 'address'
		// if false, then GetTable will return false if the wanted address did not exist
		// On return, 'remainder' contains the characters after the last "."
		inline bool GetTable(lua_State* lua_state, char const* address, std::string& remainder, bool create)
		{
			bool first_table = true;

			remainder = address;
			for (std::size_t pos = remainder.find_first_of("."); pos != std::string::npos; pos = remainder.find_first_of("."))
			{
				std::string table_name;
				table_name.assign(remainder, 0, pos);
				remainder = remainder.substr(pos + 1);

				// Attempt to put the table with the name 'table_name' into the stack
				if (first_table)
				{
					// Look for the table in the global variables
					lua_getglobal(lua_state, table_name.c_str());
				}
				else
				{
					// Otherwise, look for the table in the table that is currently top of the stack
					lua_pushstring(lua_state, table_name.c_str());
					lua_gettable(lua_state, -1);
				}

				// If 'table_name' doesn't exist then add a table with this name
				if (lua_isnil(lua_state, -1))
				{
					lua_pop(lua_state, 1);		// Pop the nil
					if (!create)
						return false;

					if (first_table) // Nothing added to the stack yet
					{
						// Add to global variables
						lua_newtable(lua_state);
						lua_setglobal(lua_state, table_name.c_str());
						lua_getglobal(lua_state, table_name.c_str());
					}
					else
					{
						// Otherwise, add to the table that is currently top of the stack
						lua_pushstring(lua_state, table_name.c_str());
						lua_newtable(lua_state);
						lua_settable(lua_state, -3);
						lua_pushstring(lua_state, table_name.c_str());
						lua_gettable(lua_state, -2);
					}
				}
				first_table = false;
			}
			return true;
		}
	}

	// Register a global function with a lua-side name
	// Lua scripts can then call 'function_name()' and the global function will be called
	// This function can be used to setup tables of functions
	// if 'function_name' contains '.' characters these refer to new tables
	// e.g.
	//	Register(lua, "pr.maths.Sin", pr::Sin);	// Creates a table called 'pr', then another
	//	table called 'maths', then adds function called 'Sin'
	inline void Register(lua_State* lua_state, char const* function_name, MappingFunction mapping_function)
	{
		// Record the current top of the stack
		int base = lua_gettop(lua_state);

		// Add tables where necessary
		std::string fname;
		impl::GetTable(lua_state, function_name, fname, true);

		// If no tables have been added to the stack, add the function to the global name space
		if (lua_gettop(lua_state) == base)
		{
			lua_register(lua_state, fname.c_str(), mapping_function);
		}
		// Otherwise, add the function to the table currently on the top of the stack
		else
		{
			lua_pushstring(lua_state, fname.c_str());		// Push the name of the function
			lua_pushcfunction(lua_state, mapping_function);	// Push the function pointer
			lua_settable(lua_state, -3);					// Assign the function to the table
		}

		// Restore the stack to the height on entry
		lua_settop(lua_state, base);
	}

	// Call a lua function
	// 'function' - is the name of the function in the form: [table.sub_table.]func
	// 'signiture' - is a format string: "iii>oo" where 'i' and 'o' are:
	//	 'b' - bool
	//	 'i' - int
	//	 'd' - double
	//	 's' - string
	//	 'v' - void*
	// Returns true if the function was successfully called.
	// If false is returned than an error message is returned on the stack.
	inline bool Call(lua_State* lua_state, char const* function, char const* signiture, va_list arg_list, bool output_err_msgs)
	{
		int base = lua_gettop(lua_state);

		// Locate the table containing the function
		std::string fname;
		if (!impl::GetTable(lua_state, function, fname, false))
		{
			lua_settop(lua_state, base);
			lua_pushfstring(lua_state, "Lua Error: Attempt to call unknown function '%s'\n", function);
			if (output_err_msgs) LuaMessage(lua_state);
			return false;
		}

		// Get the function onto the stack
		if (lua_gettop(lua_state) == base) { lua_getglobal(lua_state, fname.c_str()); }
		else { lua_pushstring(lua_state, fname.c_str()); lua_gettable(lua_state, -2); }
		lua_insert(lua_state, base + 1);
		lua_settop(lua_state, base + 1);
		if (!lua_isfunction(lua_state, -1))
		{
			lua_settop(lua_state, base);
			lua_pushfstring(lua_state, "Lua Error: Attempt to call unknown function '%s'\n", function);
			if (output_err_msgs) LuaMessage(lua_state);
			return false;
		}

		// Add input arguments to the lua stack
		int num_args;
		for (num_args = 0; *signiture && *signiture != '>'; ++signiture, ++num_args)
		{
			switch (*signiture)
			{
			default:  PR_ASSERT(PR_DBG_LUA, false, "Invalid signiture character"); break;
			case 'b': lua_pushboolean(lua_state, va_arg(arg_list, bool));   break;
			case 'i': lua_pushnumber(lua_state, va_arg(arg_list, int));    break;
			case 'd': lua_pushnumber(lua_state, va_arg(arg_list, double)); break;
			case 's': lua_pushstring(lua_state, va_arg(arg_list, char*));  break;
			case 'v': lua_pushlightuserdata(lua_state, va_arg(arg_list, void*));  break;
			}
			luaL_checkstack(lua_state, 1, "Stack full");
		}
		if (*signiture == '>') ++signiture;

		// Do the call 
		int num_results = static_cast<int>(strlen(signiture));
		if (lua_pcall(lua_state, num_args, num_results, 0))
		{
			char const* err_msg = FmtS("Lua Error: During call to function '%s' : '%s'\n", function, lua_tostring(lua_state, -1));
			lua_settop(lua_state, base);
			lua_pushstring(lua_state, err_msg);
			if (output_err_msgs) LuaMessage(lua_state);
			return false;
		}

		// Get the results from the function call
		// 'result_index' is the index on the lua stack of the first result
		for (int result_index = -num_results; *signiture; ++signiture, ++result_index)
		{
			bool results_valid = true;
			switch (*signiture)
			{
			case 'b':
				if (!lua_isboolean(lua_state, result_index)) results_valid = false;
				else *va_arg(arg_list, bool*) = lua_toboolean(lua_state, result_index) != 0;
				break;
			case 'i':
				if (!lua_isnumber(lua_state, result_index)) results_valid = false;
				else *va_arg(arg_list, int*) = static_cast<int>(lua_tonumber(lua_state, result_index));
				break;
			case 'd':
				if (!lua_isnumber(lua_state, result_index)) results_valid = false;
				else *va_arg(arg_list, double*) = lua_tonumber(lua_state, result_index);
				break;
			case 's':
				if (!lua_isstring(lua_state, result_index)) results_valid = false;
				else *va_arg(arg_list, const char**) = lua_tostring(lua_state, result_index);
				break;
			case 'v':
				if (!lua_islightuserdata(lua_state, result_index)) results_valid = false;
				else *va_arg(arg_list, void**) = static_cast<void*>(lua_touserdata(lua_state, result_index));
				break;
			default:
				PR_ASSERT(PR_DBG_LUA, false, "Invalid signiture character");
				results_valid = false;
				break;
			}
			if (!results_valid)
			{
				char const* err_msg = FmtS("Lua Error: A call to function '%s' did not return valid results (result %d invalid or missing)\n", function, num_results + result_index + 1);
				lua_settop(lua_state, base);
				lua_pushstring(lua_state, err_msg);
				if (output_err_msgs) LuaMessage(lua_state);
				return false;
			}
		}
		return true;
	}

	// Output a trace back of the call stack in the lua console
	inline int TracebackCallStack(lua_State* lua_state)
	{
		// Find the debug module
		lua_getfield(lua_state, LUA_GLOBALSINDEX, "debug");
		if (!lua_istable(lua_state, -1)) { lua_pop(lua_state, 1); return 1; }

		// Find the traceback function within the debug module
		lua_getfield(lua_state, -1, "traceback");
		if (!lua_isfunction(lua_state, -1)) { lua_pop(lua_state, 2); return 1; }

		lua_pushvalue(lua_state, 1);    // pass error message
		lua_pushinteger(lua_state, 2);  // skip this function and traceback
		lua_call(lua_state, 2, 1);      // call debug.traceback
		return 1;
	}

	// This function wraps a call to lua_pcall. It assumes there is a compiled
	// lua chunk on the stack. It inserts a traceback call under the chunk which
	// is called if an error occurs.
	// Returns true if the call was made successfully
	inline bool CallLuaChunk(lua_State* lua_state, int num_args, bool clear)
	{
		int base = lua_gettop(lua_state) - num_args;            // function index
		lua_pushcfunction(lua_state, TracebackCallStack); // push traceback function
		lua_insert(lua_state, base);                            // put it under chunk and args

		// Make the call
		int status = lua_pcall(lua_state, num_args, (clear ? 0 : LUA_MULTRET), base);

		lua_remove(lua_state, base);                            // Remove traceback function

		// Force a complete garbage collection if there was an error
		if (status != 0) { lua_gc(lua_state, LUA_GCCOLLECT, 0); }
		return status == 0;
	}

	// Push the contents of 'input' as a lua chunk onto the stack
	// Returns:
	//	EResult::Success - valid lua code in on the stack
	//	EResult::Incomplete - Stack is empty, input was incomplete
	//	EResult::SyntaxError - lua code was invalid, stack contains an error message
	//	EResult::MemoryError - lua returned a memory error, stack contains an error message
	//	EResult::Failed - lua returned a result that the documentation doesn't mention
	template <typename String>
	EResult PushLuaChunk(lua_State* lua_state, String const& input, String& syntax_error_msg)
	{
		static_assert(sizeof(String::value_type) == sizeof(char), "Lua only supports ascii");

		// Load the string into the lua stack as a function
		// I.e. inserts the user string between "function foo() user_string end"
		int result = luaL_loadbuffer(lua_state, input.c_str(), input.size(), "");
		switch (result)
		{
		case 0:
			return EResult::Success;

			// If the compiled code is incomplete we get an error message saying
			// "[string ""]:1: unexpected symbol near '<eof>'". Look
			// for the '<eof>' flag at the end of the message
		case LUA_ERRSYNTAX:
			{
				std::size_t lmsg;
				const char* msg = lua_tolstring(lua_state, -1, &lmsg);
				const char* tp = msg + lmsg - (sizeof(LUA_QL("<eof>")) - 1);
				if (strstr(msg, LUA_QL("<eof>")) == tp)
				{
					lua_pop(lua_state, 1); // Pop off the stack and wait for more input
					return EResult::Incomplete;
				}
				syntax_error_msg = msg;
			}return EResult::SyntaxError;

			// Memory allocation error
		case LUA_ERRMEM:
			lua_pushfstring(lua_state, "Lua memory error: %s", lua_tostring(lua_state, -1));
			return EResult::MemoryError;

		default: PR_ASSERT(PR_DBG_LUA, false, "");
			return EResult::Failed;
		}
	}

	// Step console.
	// This function is used to execute lua in the form of a console
	// (e.g. line by line)
	// If 'Incomplete' is returned, the caller should add more data to 'input'
	// E.g.
	//		lua::EResult result;
	//		std::string  input;
	//		do
	//		{
	//			input += ReadConsole();
	//			result = lua::StepConsole(lua, input);
	//		}
	//		while( result == EResult::Incomplete );
	//		if     ( result == EResult::Exit    ) { break; }
	//		else if( result != EResult::Success ) { lua::LuaPrint(lua); } // Print the error message
	inline EResult StepConsole(lua_State* lua_state, std::string input, std::string& syntax_error_msg)
	{
		// If the input says exit, return
		if (str::EqualN(input, "exit", 4)) return EResult::Exit;

		// If the first character is '=', replace it with "return "
		if (!input.empty() && input[0] == '=') input = "return " + input.substr(1);

		int base = lua_gettop(lua_state);

		// Push the input as a lua chunk onto the stack
		auto result = PushLuaChunk(lua_state, input, syntax_error_msg);
		switch (result)
		{
		default: LuaMessage(lua_state); break;
		case EResult::Incomplete: break;
		case EResult::Success:

			CallLuaChunk(lua_state, 0, false);

			// If there's something still on the stack, output it
			if (lua_gettop(lua_state) != base && !lua_isnil(lua_state, -1))
			{
				std::string str = lua_tostring(lua_state, -1);
				lua_pop(lua_state, 1);
				lua_pushstring(lua_state, str::EnsureNewline(str).c_str());
				LuaPrint(lua_state);
			}
			break;
		}
		return result;
	}

	// The lua wrapper object *************************************
	struct Lua
	{
		lua_State* m_state;
		bool       m_owned;

		// Create the lua state
		Lua()
			:m_state(luaL_newstate())
			, m_owned(true)
		{
			Setup();
			SetOutputFuncs(ConsolePrint, ConsolePrint, LuaDumpStack, LuaDumpTable);
		}
		explicit Lua(lua_State* lua_state)
			:m_state(lua_state)
			, m_owned(false)
		{
			// Attach to a lua state
			Setup();
		}
		Lua(Lua&& rhs) noexcept
			:m_state(rhs.m_state)
			, m_owned(rhs.m_owned)
		{
			rhs.m_state = nullptr;
			rhs.m_owned = false;
		}
		Lua(Lua const&) = delete;
		~Lua()
		{
			if (m_owned && m_state)
				lua_close(m_state);
		}

		// Assignment
		Lua& operator = (Lua&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			std::swap(m_state, rhs.m_state);
			std::swap(m_owned, rhs.m_owned);
			return *this;
		}
		Lua& operator = (Lua const&) = delete;

		// Allow this object to be used in-place of lua_State*'s
		operator lua_State* ()
		{
			return m_state;
		}
		operator lua_State const* () const
		{
			return m_state;
		}

		// Map standard functions
		void Setup()
		{
			if (!m_state) throw EResult::FailedToInitialise;
			luaL_openlibs(m_state);
			PR_EXPAND(PR_DBG_LUA, dbg_lua() = m_state);
		}

		// Set some default mapping functions. By default assume this is a console app. '0' means don't change.
		void SetOutputFuncs(MappingFunction print_cb, MappingFunction luamsg_cb, MappingFunction dumpstack_cb, MappingFunction dumptable_cb)
		{
			if (luamsg_cb)    lua_atpanic(m_state, luamsg_cb);
			if (luamsg_cb)    lua_register(m_state, "luamsg", luamsg_cb);	// 'luamsg' is used for output originating from lua
			if (print_cb)     lua_register(m_state, "print", print_cb);	// 'print' is used for output originating from a lua script
			if (dumpstack_cb) lua_register(m_state, "dumpstack", dumpstack_cb);
			if (dumptable_cb) lua_register(m_state, "dumptable", dumptable_cb);
		}
	};

	inline int  LuaMessage(Lua& lua)
	{
		return LuaMessage(lua.m_state);
	}
	inline int  LuaPrint(Lua& lua)
	{
		return LuaPrint(lua.m_state);
	}
	inline std::string DumpStack(Lua& lua)
	{
		return DumpStack(lua.m_state);
	}
	inline std::string DumpTable(Lua& lua, int table_index)
	{
		return DumpTable(lua.m_state, table_index);
	}
	inline void AddUserPointer(Lua& lua, char const* name, void* user)
	{
		return AddUserPointer(lua.m_state, name, user);
	}
	template <typename T> T* GetUserPointer(Lua& lua, char const* name)
	{
		return GetUserPointer<T>(lua.m_state, name);
	}
	inline bool DoString(Lua& lua, char const* string)
	{
		return DoString(lua.m_state, string);
	}
	inline bool DoFile(Lua& lua, char const* filename)
	{
		return DoFile(lua.m_state, filename);
	}
	inline void Register(Lua& lua, char const* function_name, MappingFunction mapping_function)
	{
		return Register(lua.m_state, function_name, mapping_function);
	}
	inline bool Call(Lua& lua, char const* function, char const* signiture, ...)
	{
		va_list arg_list; va_start(arg_list, signiture); bool result = Call(lua.m_state, function, signiture, arg_list, true); va_end(arg_list); return result;
	}
	inline bool CallQ(Lua& lua, char const* function, char const* signiture, ...)
	{
		va_list arg_list; va_start(arg_list, signiture); bool result = Call(lua.m_state, function, signiture, arg_list, false); va_end(arg_list); return result;
	}
	inline int  TracebackCallStack(Lua& lua)
	{
		return TracebackCallStack(lua.m_state);
	}
	inline EResult StepConsole(Lua& lua, std::string const& input, std::string& err_msg)
	{
		return StepConsole(lua.m_state, input, err_msg);
	}
}
	
// Functions that can be called from the immediate window
#if PR_DBG_LUA
inline void DumpLuaStack()
{
	if( !dbg_lua() ) return;
	std::string str = pr::lua::DumpStack(dbg_lua());
	PR_INFO(PR_DBG_LUA, str.c_str());
}
inline void DumpLuaTable(int i)
{
	if( !dbg_lua() ) return;
	std::string str = pr::lua::DumpTable(dbg_lua(), i);
	PR_INFO(PR_DBG_LUA, str.c_str());
}
#endif
