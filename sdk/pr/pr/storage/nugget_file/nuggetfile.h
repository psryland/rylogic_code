//******************************************************************
//
//	Nugget File
//
//******************************************************************
// Usage:
//	Create nuggets from sratch, source data, raw data, etc...
//	Add them to a container of some sort
//	Save them to somewhere
//
//	Load nuggets from somewhere into an output iterator
//	Use them, modify them, re-save
//
// Note: The nugget file doesn't know anything about compression, if you
// want to compress the data in a nugget, do it before adding it
//
#ifndef PR_NUGGET_FILE_H
#define PR_NUGGET_FILE_H

#include "pr/common/assert.h"
#include "pr/common/PRTypes.h"
#include "pr/common/IStream.h"
#include "pr/storage/nugget_file/NuggetFileAssertEnable.h"
#include "pr/storage/nugget_file/Types.h"
#include "pr/storage/nugget_file/Interfaces.h"
#include "pr/storage/nugget_file/NuggetImpl.h"
#include "pr/storage/nugget_file/FunctionImpl.h"
#include "pr/storage/nugget_file/NuggetReceivers.h"

namespace pr
{
	namespace nugget
	{
		//*****
		// Load a set of nuggets from some source data. 'copy_flag' is typically
		// 'EFlag_Reference' unless you want to buffer the src data
		inline EResult Load(const ISrc& src, std::size_t src_size, ECopyFlag copy_flag, INuggetReceiver& nuggets_out)
		{
			return impl::Load<void>(src, src_size, copy_flag, nuggets_out);
		}
		
		//*****
		// Load nuggets from a file. 'copy_flag' is typically
		// 'EFlag_Reference' unless you want to buffer the file data
		inline EResult Load(const TCHAR* nugget_filename, ECopyFlag copy_flag, INuggetReceiver& nuggets_out)
		{
			Handle file = FileOpen(nugget_filename, EFileOpen::Reading);
			if (file == INVALID_HANDLE_VALUE) return EResult_FailedToOpenNuggetFile;
			FileI src(&file);
			return impl::Load<void>(src, src.GetDataSize(), copy_flag, nuggets_out);
		}

		//*****
		// Save a range of nuggets to some destination data
		template <typename NuggetIter>
		inline EResult Save(IDest& dst, NuggetIter first, NuggetIter last)
		{
			return impl::Save<NuggetIter>(dst, first, last);
		}

		//*****
		// Save nuggets to a file
		template <typename NuggetIter>
		inline EResult Save(const TCHAR* nugget_filename, NuggetIter first, NuggetIter last)
		{
			Handle file = FileOpen(nugget_filename, EFileOpen::Writing);
			if (file == INVALID_HANDLE_VALUE) return EResult_FailedToCreateNuggetFile;

			FileO dst(&file);
			return impl::Save<NuggetIter>(dst, first, last);
		}

		//*****
		// Return the total size in bytes of a container of nuggets
		template <typename NuggetCIter>
		inline std::size_t SizeInBytes(NuggetCIter first, NuggetCIter last)
		{
			return impl::SizeInBytes<NuggetCIter>(first, last);
		}

	}//namespace nugget
}//namespace pr

#define PR_MAKE_NUGGET_ID(c1, c2, c3, c4)	(((c1) << 0) | ((c2) << 8) | ((c3) << 16) | ((c4) << 24))

#endif//PR_NUGGET_FILE_H
