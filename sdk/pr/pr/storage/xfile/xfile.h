//************************************************************************
// XFile Load/Save
//  Copyright © Rylogic Ltd 2010
//************************************************************************

#ifndef PR_XFILE_H
#define PR_XFILE_H
#pragma once

// Required preprocessor defines:
//	;NOMINMAX
// Required libs
//	d3d9.lib d3dx9.lib d3dxof.lib dxguid.lib dxerr9.lib rpcrt4.lib

#include "pr/common/min_max_fix.h"
#include <set>
#include <d3d9.h>
#include <rmxfguid.h>
#include <d3dx9xof.h>
#include "pr/common/d3dptr.h"
#include "pr/common/exception.h"
#include "pr/geometry/geometry.h"

extern unsigned char* D3DTemplates;
extern unsigned int   D3DTemplateBytes;

#ifndef PR_DBG_XFILE
#define PR_DBG_XFILE  PR_DBG_GEOM
#endif//PR_DBG_XFILE

namespace pr
{
	namespace xfile
	{
		enum EResult
		{
			EResult_Success	= 0,
			EResult_Failed  = 0x80000000,
			EResult_EnumerateFileFailed,
			EResult_FailedToCreateSaveObject,
			EResult_GetChildFailed,
			EResult_FailedToCreateSaveData,
			EResult_LockDataFailed,
			EResult_AddDataFailed,
			EResult_DataSizeInvalid,
			EResult_SaveFailed,
		};
		typedef pr::Exception<EResult> Exception;
		
		namespace EConvert
		{
			enum Type
			{
				Bin           = D3DXF_FILEFORMAT_BINARY,
				Txt           = D3DXF_FILEFORMAT_TEXT,
				CompressedBin = D3DXF_FILEFORMAT_COMPRESSED | D3DXF_FILEFORMAT_BINARY
			};
		}

		struct GUIDPred
		{
			bool operator () (const GUID& lhs, const GUID& rhs) const { return memcmp(&lhs, &rhs, sizeof(GUID)) < 0; }
		};
		typedef std::set<GUID, GUIDPred> TGUIDSet;

		// Load an x file
		       EResult Load(char const*xfilename, Geometry& geometry, TGUIDSet const* partial_load_set, void const* custom_templates, unsigned int custom_templates_size);
		inline EResult Load(char const*xfilename, Geometry& geometry, TGUIDSet const* partial_load_set) { return Load(xfilename, geometry, partial_load_set, 0, 0); }
		inline EResult Load(char const*xfilename, Geometry& geometry)                                   { return Load(xfilename, geometry, 0); }

		// Save an x file
		       EResult Save(Geometry const& geometry, char const* xfilename, TGUIDSet const* partial_save_set, void const* custom_templates, unsigned int custom_templates_size);
		inline EResult Save(Geometry const& geometry, char const* xfilename, TGUIDSet const* partial_save_set) { return Save(geometry, xfilename, partial_save_set, 0, 0); }
		inline EResult Save(Geometry const& geometry, char const* xfilename)                                   { return Save(geometry, xfilename, 0, 0, 0); }
		inline EResult Save(Geometry const& geometry)                                                          { return Save(geometry, 0, 0, 0, 0); }

		// Convert an x file
		EResult Convert(char const* xfilename_in, char const* xfilename_out, EConvert::Type convert_type);

		namespace impl
		{
			std::string GetName        (D3DPtr<ID3DXFileData> data);
			GUID        GetGUID        (D3DPtr<ID3DXFileData> data);
			std::size_t GetNumChildren (D3DPtr<ID3DXFileData> data);
		}
	}

	// Result testing
	inline std::string GetErrorString(xfile::EResult result)	{ (void)result; return ""; }
	inline bool        Failed        (xfile::EResult result)	{ return result  < 0; }
	inline bool        Succeeded     (xfile::EResult result)	{ return result >= 0; }
	inline void        Verify        (xfile::EResult result)	{ (void)result; PR_ASSERT(PR_DBG_XFILE, Succeeded(result), "Verify failure"); }
}

#endif
