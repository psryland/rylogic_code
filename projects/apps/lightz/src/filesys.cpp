#include "filesys.h"

namespace lightz
{
	FileSys::FileSys()
	 	: fs::LittleFSFS()
	{}
	
	void FileSys::Setup()
	{
		if (begin(false, "/root"))
			return;
	
		Serial.println("Failed to mount the file system");

		#if 0
		if (!format())
		{
			Serial.println("Failed to format the file system");
			throw std::runtime_error("Failed to mount the file system");
		}
		#endif

		// Try mount again
		if (!begin(false, "/root"))
		{
			Serial.println("Failed to mount the file system");
			throw std::runtime_error("Failed to mount the file system");
		}
	}
}
