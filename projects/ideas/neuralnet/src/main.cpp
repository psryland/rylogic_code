//*********************************************
// Neural Net
//  Copyright (c) Rylogic Ltd 2015
//*********************************************
#include "pr/neuralnet/forward.h"
#include "pr/neuralnet/neuralnet.h"
using namespace pr::neuralnet;

int main(int argc, char* argv[])
{
	pr::neuralnet::Network hal({2, 3, 1});
	auto output = hal.Think({1,1});
	return 0;
}