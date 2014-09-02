//******************************************************************
//
//	Nugget File functions
//
//******************************************************************
#ifndef PR_NUGGET_FILE_FUNCTIONS_H
#define PR_NUGGET_FILE_FUNCTIONS_H

#include "pr/storage/nugget_file/nuggetfileassertenable.h"
#include "pr/common/istream.h"
#include "pr/storage/nugget_file/types.h"
#include "pr/storage/nugget_file/interfaces.h"

namespace pr
{
	namespace nugget
	{
		namespace impl
		{
			//*****
			// Load a set of nuggets from some source data
			template <typename T>		
			EResult Load(const ISrc& src, std::size_t src_size, ECopyFlag copy_flag, INuggetReceiver& nuggets_out)
			{
				// Loop over nuggets in the source data
				for( std::size_t offset = 0; offset != src_size; )
				{
					NuggetImpl<T>* nugget = nuggets_out.NewNugget();
					if( !nugget ) return EResult_SuccessPartialLoad;

					EResult result = nugget->Initialise(src, offset, copy_flag);
					if( Failed(result) )	{ return result; }

					// Move the offset on to the next nugget
					offset += nugget->GetNuggetSizeInBytes();
					if( offset > src_size ) { return EResult_NuggetDataCorrupt; }
				}
				return EResult_Success;
			}

			//*****
			// Save a range of nuggets to some destination data
			template <typename NuggetIter>
			EResult Save(IDest& dst, NuggetIter first, NuggetIter last)
			{
				// Loop over nuggets saving them to 'dst'
				std::size_t offset = 0;
				for( first; first != last; ++first )
				{
					EResult result = first->Save(dst, offset);
					if( Failed(result) ) { return result; }
				}
				return EResult_Success;
			}

			//*****
			// Return the total size in bytes of a container of nuggets
			template <typename NuggetCIter>
			inline std::size_t SizeInBytes(NuggetCIter first, NuggetCIter last)
			{
				std::size_t size = 0;
				for( first; first != last; ++first )
				{
					size += first->GetNuggetSizeInBytes();
				}
				return size;
			}
		}//namespace impl
	}//namespace nugget
}//namespace pr

#endif//PR_NUGGET_FILE_FUNCTIONS_H
