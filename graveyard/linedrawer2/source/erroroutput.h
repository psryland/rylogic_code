//*************************************************************************
//
// Error Output
//
//*************************************************************************
#ifndef ERROROUTPUT_H
#define ERROROUTPUT_H

#include "pr/common/PRString.h"
#include "LineDrawer/Source/Forward.h"

class ErrorOutput
{
public:
	ErrorOutput();
	~ErrorOutput(){}
	void ResetLogFile(const char* filename);
	void Error(const char* msg);
	void Warn(const char* msg);
	void Info(const char* msg);

private:
	void Out(const char* str, const char* msg);

private:
	LineDrawer*	m_linedrawer;
};

#endif//DATALIST_H
