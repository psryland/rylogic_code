//********************************
// FBX Model file format
//  Copyright (c) Rylogic Ltd 2018
//********************************
// Using *.fbx files requires the AutoDesk FBX SDK.
// To avoid making this a build dependency, this header will dynamically load 'fbx.dll' as needed.
#pragma once
#include <iostream>
#include <functional>
#include <filesystem>
#include "pr/win32/win32.h"

namespace pr::geometry::fbx
{
	// Options for parsing FBXfiles
	struct Options
	{
		Options()
		{}
	};

	// Access the dynamically loaded FBX dll for parsing fbx files
	struct FbxDll
	{
		// Notes:
		//  - Remember to open streams in binary mode!

		HMODULE m_module;

		FbxDll()
			: m_module(win32::LoadDll<struct FbxDll>("fbx.dll"))
			, Fbx_ReadStream((Fbx_ReadStreamFn)GetProcAddress(m_module, "Fbx_ReadStream"))
			, Fbx_DumpStream((Fbx_DumpStreamFn)GetProcAddress(m_module, "Fbx_DumpStream"))
			, Fbx_RoundTripTest((Fbx_RoundTripTestFn)GetProcAddress(m_module, "Fbx_RoundTripTest"))
		{}

		// Read an fbx model
		using Fbx_ReadStreamFn = void (*)(std::istream& src, Options const& opts, std::function<bool(int)> out);
		Fbx_ReadStreamFn Fbx_ReadStream;

		// Dump an fbx scene to a out stream
		using Fbx_DumpStreamFn = void (*)(std::istream& src, std::ostream& out);
		Fbx_DumpStreamFn Fbx_DumpStream;

			// Round trip test an fbx scene
		using Fbx_RoundTripTestFn = void (*)(std::istream& src, std::ostream& out);
		Fbx_RoundTripTestFn Fbx_RoundTripTest;

	};
}
