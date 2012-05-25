//**********************************************************
//
//	A command line tool for creating a CRC of the input stream
//
//**********************************************************
#include <iostream>
#include "pr/common/Crc.h"

using namespace pr;

int main(int , char* [])
{
	char line[1024];
	memset(line, 0, sizeof(line));
	std::cin.getline(line, 1023);
	CRC crc = Crc(line, (unsigned int)strlen(line));
	printf("%8.8X\n", crc);
	return 0;
}