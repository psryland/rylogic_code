//**********************************************************************************
// Resource
//  Copyright © Rylogic Ltd 2009
//**********************************************************************************
// Access data embedded in an exe file
//
// To add resources to an exe, add a '.rc' file to the project (create a text file with
//	.rc extn, and 'Add existing'. Or use the File->New->Resource->Resource File to have
//	IDE resource editor work)
//
// Add entries in the resource file like this:
//  // name         type       legacy-dontcare   filename
//  IDR_EXAMPLE0    TEXT       DISCARDABLE       "test.cpp"
//  IDR_EXAMPLE1    BINARY     DISCARDABLE       "test.cpp"
//
// Call this helper function like this:
//  pr::Resource<char> res = pr::resource::Read<char>("IDR_EXAMPLE0", "TEXT");
//  res.m_data;
//  res.m_size;
//
// Note: you only need a "resource.h" file containing #define's if you want to use
// 'IDR_EXAMPLE0' as an int. In this case you need to call:
//     pr::resource::Read<char>(MAKEINTRESOURCE(IDR_EXAMPLE0), "TEXT");
//

#ifndef PR_RESOURCE_H
#define PR_RESOURCE_H

#include <windows.h>

namespace pr
{
	template <typename Type>
	struct Resource
	{
		Type const* m_data; // Pointer to the resource
		std::size_t m_size; // Size of the resource in bytes
	};

	namespace resource
	{
		// Return const access to an embedded resource
		// module = 0 means "this exe"
		// If you're resource is in a dll, you need to use the HMODULE passed to the DllMain function
		// Note: you can use pr::GetCurrentModule() for 'module' in windowsfunctions.h
		template <typename Type> Resource<Type> Read(TCHAR const* name, TCHAR const* type, HMODULE module = 0)
		{
			Resource<Type> res;
			HRSRC handle = FindResource(module, name, type);
			if (!handle)
			{
				DWORD last_error = GetLastError();
				throw std::exception("resource not found",last_error);
			}
			
			res.m_size = SizeofResource(module, handle);
			HGLOBAL mem = LoadResource(module, handle);
			if (!mem)
			{
				DWORD last_error = GetLastError();
				throw std::exception("failed to load resource",last_error);
			}
			
			// Object a pointer to the resouce.
			// Valid until the module is unloaded therefore don't need to worry about unlocking (according to MSDN)
			res.m_data = static_cast<Type const*>(LockResource(mem));
			return res;
		}
	}
}

#endif

