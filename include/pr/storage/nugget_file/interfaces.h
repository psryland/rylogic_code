//******************************************************************
//
//	Nugget interfaces
//
//******************************************************************
#ifndef PR_NUGGET_FILE_INTERFACES_H
#define PR_NUGGET_FILE_INTERFACES_H

#include "pr/common/PRTypes.h"
#include "pr/storage/nugget_file/Types.h"

namespace pr
{
	namespace nugget
	{
		// An interface for receiving nuggets. This interface is used by the load methods
		// when loading nuggets from some 'ISrc'.
		struct INuggetReceiver
		{
			// Return null to stop receiving nuggets
			virtual Nugget* NewNugget() = 0;
		};

	}//namespace nugget
}//namespace pr

#endif//PR_NUGGET_FILE_INTERFACES_H
