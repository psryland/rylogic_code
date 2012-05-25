//*****************************************
//*****************************************
#include "test.h"
#include "pr/storage/xfile/xfile.h"
#include "pr/geometry/geometry.h"

namespace TestXFile
{
	using namespace pr;

	void Run()
	{
		Geometry geometry;
		if( Failed(xfile::Load("Z:/D4-Resources/Levels/NewYork/Models/Regions/01_Englewood/common/HOUSE01.X", geometry)) )
		{
			printf("Load Failed\n");
		}
		else
		{
			printf("Load Succeeded\n");
		}

		if( Failed(xfile::Save(geometry, "C:/deleteme/deleteme.x")) )
		{
			printf("Save Failed\n");
		}
		else
		{
			printf("Save Succeeded\n");
		}
		_getch();
	}
}//namespace TestXFile
