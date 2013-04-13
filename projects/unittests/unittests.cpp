//***********************************************************************
// UnitTest
//  Copyright © Rylogic Ltd 2008
//***********************************************************************

#include <tchar.h>
#include "pr/common/unittests.h"

// Add includes containing unittests here
#include "pr/common/repeater.h"
#include "pr/macros/enum.h"
#include "pr/str/tostring.h"

int _tmain(int argc, _TCHAR* argv[])
{
	if (argc == 2 && _tcscmp(argv[1], _T("runtests")) == 0)
		pr::unittests::RunAllTests();

	return 0;
}
