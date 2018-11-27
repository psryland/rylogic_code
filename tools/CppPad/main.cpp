//DEBUG = build in debug mode (default)
//@ /nologo /std:c++17
//@ /I"p:/pr/include"
#include <vector>
#include <iostream>
#include <functional>
#include "pr/maths/maths.h"

using namespace pr;

int main(int, char*[])
{
	auto a2b = Random4x4(pr::g_rng(), v4Origin, 1.0f);
	auto a = CPM(a2b.pos) * a2b.rot;
	auto b = a2b.rot * -Invert(CPM(a2b.pos));

	// auto a = v4{1,2,3,1};
	// auto c0 = CPM(a);

	// auto c1 = CPM(a);
	// auto c2 = Invert(c0);
	// auto r = c0 * c1;
	// std::cout << "c0:\n" << c0 << "\n";
	// std::cout << "c1:\n " << c1 << "\n";
	// std::cout << "c2:\n " << c2 << "\n";
	// std::cout << "r:\n " << r << "\n";
	std::cout << "a:\n" << a << "\n";
	std::cout << "b:\n" << b << "\n";
	return 0;
}

