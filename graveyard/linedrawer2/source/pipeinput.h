//*********************************************
// Pipe Input
//	(C)opyright Rylogic Limited 2007
//*********************************************

#ifndef LDR_PIPE_INPUT_H
#define LDR_PIPE_INPUT_H

#include "pr/common/Pipe.h"

class PipeInput
{
public:
	PipeInput();
	~PipeInput();

	void Start();
	void Stop();

private:
	static void OnRecv(void const* data, std::size_t, bool partial, void* user_data);
	void Recv(std::string const& str);

private:
	pr::Pipe<>			m_pipe;
	static std::string	m_recv_data;
};

#endif//LDR_PIPE_INPUT_H