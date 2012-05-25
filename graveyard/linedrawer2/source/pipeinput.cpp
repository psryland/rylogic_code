//*********************************************
// Pipe Input
//	(C)opyright Rylogic Limited 2007
//*********************************************

#include "Stdafx.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/Source/PipeInput.h"

std::string PipeInput::m_recv_data;
void PipeInput::OnRecv(void const* data, std::size_t, bool partial, void* user_data)
{
	if( !partial ) m_recv_data.clear();
	m_recv_data += static_cast<char const*>(data);
	static_cast<PipeInput*>(user_data)->Recv(m_recv_data);
}

// Constructor
PipeInput::PipeInput()
:m_pipe("LineDrawerListener", PipeInput::OnRecv, this)
{}

// Destructor
PipeInput::~PipeInput()
{}

// Start the listener
void PipeInput::Start()
{
	m_pipe.SpawnListenThread();
}

// Stop the listener
void PipeInput::Stop()
{
	m_pipe.TerminateListenThreads();
}

// Recieve data from the pipe
void PipeInput::Recv(std::string const& str)
{
	LineDrawer::Get().m_lua_input.DoString(str);
}
