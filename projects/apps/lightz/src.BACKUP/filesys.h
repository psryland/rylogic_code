#pragma once
#include "forward.h"

namespace lightz
{
	struct FileSys
	{
		FileSys();
	};

	#if 0
	struct FileSys : fs::LittleFSFS
	{
		FileSys();
		void Setup();
	};

	// Singleton instance of the file system
	extern FileSys filesys;
	#endif
	
}
