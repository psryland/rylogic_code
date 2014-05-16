//**********************************************************
// TextFormatter
//  Copyright (c) Paul Ryland 2012
//**********************************************************

#pragma once
#ifndef PR_TEXT_FORMATTER_IFORMATTER_H
#define PR_TEXT_FORMATTER_IFORMATTER_H

#include <iostream>

// The interface for an object that applies formatting to a stream
// Formatters are applied as a series of isolated modules
// e.g.
//   file istream -> formatter -> formatter -> formatter -> file ostream
struct IFormatter
{
	// Show the command line syntax for how the formatter should be used
	virtual void Help() = 0;
};

#endif
