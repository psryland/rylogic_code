#pragma once

// Helper for debugging expansion functions.
// Stops the debugger expanding types while in expansion functions
struct ReentryGuard
{
	#ifdef _DEBUG
	std::atomic_flag& guard() { static std::atomic_flag m_guard; return m_guard; }
	ReentryGuard()            { if (guard().test_and_set()) throw std::exception(); } // Throws if already set
	~ReentryGuard()           { guard().clear(); }
	#else
	ReentryGuard() {} // prevents unused local variable warning
	#endif
};