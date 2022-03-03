//******************************************
// Hooks
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"

enum EHookType
{
	EHookType_DeleteObjects,
	EHookType_NumberOf
};

struct HookState
{
	HookState()					{ m_stack.push_back(true); }
	bool State() const			{ return m_stack.back(); }
	void Push(bool enabled)		{ m_stack.push_back(enabled); }
	void Pop()					{ PR_ASSERT(PR_DBG_COMMON, m_stack.size() >= 1); m_stack.pop_back(); }
	std::vector<bool> m_stack;
};
