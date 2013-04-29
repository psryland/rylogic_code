//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#include "sol/main/stdafx.h"
#include "sol/objects/system.h"

using namespace pr;
using namespace sol;

System::System(int seed)
	:m_bodies()
{
	GenerateStars(seed);
}

// 
void System::GenerateStars(int seed)
{
}
