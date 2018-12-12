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
	auto mass = 5.0f;
	auto Ic = Inertia::Box(v4{0.1f, 0.5f, 0.1f,0}, mass);
	auto Ic¯ = Invert(Ic);

	auto F = 2.0f;

	// Apply a force at the CoM
	auto f0 = v8f{0, 0, 0, F, 0, 0};
	auto a0 = Ic¯ * f0;
	//PR_CHECK(FEql(a0, v8m{0, 0, 0, F/mass, 0, 0}), true);

	// Apply a force at the top
	auto f1 = Shift(f0, -v4{0,0.5f,0,0});//v8f{0,0,0.5f*F/Ic.m_diagonal.z,0,0});
	auto a1 = Ic¯.Ic¯3x3() * f1.lin;
	//PR_CHECK(FEql(a1, v8m{0, 0, 0.5f*F/Ic.m_diagonal.z, F/mass, 0, 0}), true);

	//auto a2b = Random4x4(pr::g_rng(), v4Origin, 1.0f);
	//auto a = CPM(a2b.pos) * a2b.rot;
	//auto b = a2b.rot * -Invert(CPM(a2b.pos));

	// auto a = v4{1,2,3,1};
	// auto c0 = CPM(a);

	// auto c1 = CPM(a);
	// auto c2 = Invert(c0);
	// auto r = c0 * c1;
	// std::cout << "c0:\n" << c0 << "\n";
	// std::cout << "c1:\n " << c1 << "\n";
	// std::cout << "c2:\n " << c2 << "\n";
	// std::cout << "r:\n " << r << "\n";
	//std::cout << "a:\n" << a << "\n";
	std::cout << "a1:\n" << a1 << "\n";
	return 0;
}

