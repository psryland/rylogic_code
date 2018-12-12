//DEBUG = build in debug mode (default)
//@ /nologo /std:c++17
//@ /I"p:/pr/include"
#include <vector>
#include <iostream>
#include <functional>
#include "pr/maths/maths.h"
#include "pr/maths/spatial.h"
#include "pr/physics2/shape/inertia.h"

using namespace pr;
using namespace pr::physics;

int main(int, char*[])
{
	char str[256];
	sprintf(str, "%lf", maths::float_nan);
	std::cout << "str:\n" << str << "\n";
	
	std::getchar();
	return 0;
}

