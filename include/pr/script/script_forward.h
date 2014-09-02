//**********************************
// Script Reader
//  (c)opyright Rylogic Ltd 2007
//**********************************
#ifndef PR_SCRIPT_SCRIPT_FORWARD_H
#define PR_SCRIPT_SCRIPT_FORWARD_H
	
#include "pr/script/keywords.h"
	
namespace pr
{
	namespace script
	{
		struct Loc;
		struct Src;
		struct SeekSrc;
		struct PtrSrc;
		struct RangeSrc;
		struct BufferedSrc;
		struct FileSrc;
		struct FileSrc2;
		template <typename TBuf> struct Buffer;
		template <size_t Len> struct History;
		struct CaseSrc;
		struct SrcStack;
		struct PPMacro;
		struct IErrorHandler;
		struct IPPMacroDB;
		struct IIncludes;
		struct IgnoreIncludes;
		struct FileIncludes;
		struct StrIncludes;
		struct IEmbeddedCode;
		struct Preprocessor;
		struct Reader;
	}
}
	
#endif
	
