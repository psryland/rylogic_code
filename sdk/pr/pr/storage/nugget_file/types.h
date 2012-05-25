//******************************************************************
//
//	Nugget File Types
//
//******************************************************************
#ifndef PR_NUGGET_FILE_TYPES_H
#define PR_NUGGET_FILE_TYPES_H

#include "pr/common/exception.h"
#include "pr/storage/nugget_file/nuggetfileassertenable.h"

namespace pr
{
	namespace nugget
	{
		// Result testing
		enum EResult
		{
			EResult_Success = 0,
			EResult_SuccessPartialLoad,
			EResult_Failed = 0x80000000,
			EResult_FailedToReadSourceData,
			EResult_NoData,
			EResult_NotNuggetData,
			EResult_IncorrectNuggetFileVersion,
			EResult_NuggetDataCorrupt,
			EResult_FailedToCreateTempFile,
			EResult_ReadFromTempFileFailed,
			EResult_ReadingExternalFileFailed,
			EResult_WriteToTempFileFailed,
			EResult_WriteToDestFailed,
			EResult_FailedToOpenNuggetFile,
			EResult_FailedToCreateNuggetFile
		};
		typedef pr::Exception<> Exception;

		enum ECopyFlag
		{
			ECopyFlag_Reference			= 0,	// Reference the provided data as it will stay in scope
			ECopyFlag_CopyToBuffer		= 1,	// Buffer the provided data
			ECopyFlag_CopyToTempFile	= 2,	// Buffer the provided data in a temporary file
		};

		// Forward declarations
		template <typename T> class NuggetImpl;
		typedef NuggetImpl<void> Nugget;
	}//namespace nugget
	
	inline bool Failed   (nugget::EResult result)	{ return result  < 0; }
	inline bool Succeeded(nugget::EResult result)	{ return result >= 0; }
	inline void Verify   (nugget::EResult result)	{ (void)result; PR_ASSERT(PR_DBG_NUGGET_FILE, Succeeded(result), "Verify failure"); }

}//namespace pr

#endif//PR_NUGGET_FILE_TYPES_H
