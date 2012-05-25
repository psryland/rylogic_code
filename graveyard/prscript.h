//************************************************************
//
//	PR::ScriptLoader loader/saver
//
//************************************************************
//
//	Example script file:
//		# Comments start with a '#'
//		# Keywords start with a '*'
//		# For example:
//		*keyword XX
//		{
//			*section_item
//			*another_item
//		}
//		*another_item XY
//
//	Loader Usage:
//		Load the file
//		Call "FindKeyword" to search for a keyword in the file
//		Call "GetKeyword" to get the next keyword in the file
//			On return, "m_pos" will point to the next item after the keyword
//		It's then up to the client code to scan to sections, or read values
//		Sections will be skipped unless FindSectionStart is used
//
//	Saver Usage:
//		Create/Reset the saver object
//		Call the Write methods to add keywords and data to the script
//		When done call Save
//

#ifndef PR_SCRIPT_OLD_H
#define PR_SCRIPT_OLD_H

#include <new>
#include "pr/common/stdvector.h"
#include "pr/common/fmt.h"
#include "pr/common/prexception.h"
#include "pr/maths/maths.h"
#include "pr/str/prstring.h"
#include "pr/filesys/filesys.h"
#include "pr/filesys/fileex.h"

namespace pr
{
	namespace script
	{
		enum EResult
		{
			EResult_Success = 0,
			EResult_Failed = 0x80000000,
			EResult_LoadSourceFailed,
			EResult_IncludeFilenameMissing,
			EResult_IncludeFileNotFound,
			EResult_FailedToReadIncludeFilename,
			EResult_LoadIncludeFailed,
			EResult_SectionNotFound,
			EResult_SectionStartNotFound,
			EResult_SectionEndNotFound,
			EResult_NotASection,
			EResult_ParseError = 0xC0000000,
			EResult_ExtractString,
			EResult_ExtractCString,
			EResult_ExtractIdentifier,
			EResult_ExtractByte,
			EResult_ExtractLong,
			EResult_ExtractULong,
			EResult_ExtractReal			
		};

		struct Exception : pr::Exception
		{
			Exception()                                                               {}
			explicit Exception(int value)             : pr::Exception(value)          {}
			Exception(int value, const char* message) : pr::Exception(value, message) {}
		};

		typedef std::vector<std::string> TPaths;

	}//namespace script
	inline std::string ToString(script::EResult result)
	{
		switch( result )
		{
		case script::EResult_Success:						return "Success";
		case script::EResult_Failed:						return "Failed";
		case script::EResult_LoadSourceFailed:				return "LoadSourceFailed";
		case script::EResult_IncludeFilenameMissing:		return "IncludeFilenameMissing";
		case script::EResult_IncludeFileNotFound:			return "IncludeFileNotFound";
		case script::EResult_FailedToReadIncludeFilename:	return "FailedToReadIncludeFilename";
		case script::EResult_LoadIncludeFailed:				return "LoadIncludeFailed";
		case script::EResult_SectionNotFound:				return "SectionNotFound";
		case script::EResult_SectionStartNotFound:			return "SectionStartNotFound";
		case script::EResult_SectionEndNotFound:			return "SectionEndNotFound";
		case script::EResult_NotASection:					return "NotASection";
		case script::EResult_ParseError:					return "ParseError";
		case script::EResult_ExtractString:					return "ParseError: ExtractString";
		case script::EResult_ExtractCString:				return "ParseError: ExtractCString";
		case script::EResult_ExtractIdentifier:				return "ParseError: ExtractIdentifier";
		case script::EResult_ExtractByte:					return "ParseError: ExtractByte";
		case script::EResult_ExtractLong:					return "ParseError: ExtractLong";
		case script::EResult_ExtractULong:					return "ParseError: ExtractULong";
		case script::EResult_ExtractReal:					return "ParseError: ExtractReal";
		default: PR_ASSERT_STR(PR_DBG_COMMON, false, "Unknown script error code"); return "";
		}
	}

	inline bool Failed   (script::EResult result)	{ return result  < 0; }
	inline bool Succeeded(script::EResult result)	{ return result >= 0; }
	inline void Verify   (script::EResult result)	{ (void)result; PR_ASSERT_STR(PR_DBG_COMMON, Succeeded(result), "Verify failure"); }

	// Handy error message
	//MessageBox(0, pr::Fmt(
	//	"Source script parser error: %s\n"
	//	"Near: '%.20s'"
	//	,pr::ToString(static_cast<pr::script::EResult>(e.m_value)).c_str()
	//	,loader.GetSourceStringAt()).c_str(), "Source Error", MB_ICONEXCLAMATION|MB_OK);
}

#include "pr/common/PRScriptLoader.h"
#include "pr/common/PRScriptSaver.h"

namespace pr
{
	typedef impl::ScriptLoader<void> ScriptLoader;	//	Loader - Used to load and read a script from disk
	typedef impl::ScriptSaver<void>  ScriptSaver;	//	Saver - Used to write a script to disk
}//namespace pr

#endif//PR_SCRIPT_OLD_H

