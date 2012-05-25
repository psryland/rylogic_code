//**********************************************************
//
//	A command line tool for creating GUID's
//
//**********************************************************
#include <iostream>
#include "pr/common/GUID.h"
#include "pr/macros/link.h"

int main(int , char* [])
{
	std::cout << pr::GUIDToString(pr::GenerateGUID());
	return 0;
}