//********************************
// AI Agent System DLL
//  Copyright (c) Rylogic Ltd 2026
//********************************
// Implements the C-style API for the AI agent DLL.
// All backend details (WinHTTP, JSON-RPC) are contained within this DLL.
// This file does NOT include any backend-specific headers in the public API.

#include <string>
#include <string_view>
#include <vector>
#include <memory>
#include <mutex>
#include <queue>
#include <chrono>
#include <functional>
#include <algorithm>
#include <format>
#include <cstdlib>
#include <span>

#include <Windows.h>
#include <winhttp.h>

#pragma comment(lib, "winhttp.lib")

#include "pr/storage/json.h"
#include "pr/ai/ai.h"

using namespace pr;
using namespace pr::ai;

// TODO: Implementation follows in subsequent todos

extern "C"
{
	__declspec(dllexport) ContextData* __stdcall AI_Initialise(ContextConfig const& cfg, ErrorHandler error_cb)
	{
		(void)cfg, (void)error_cb;
		return nullptr; // TODO
	}
	__declspec(dllexport) void __stdcall AI_Shutdown(ContextData* ctx)
	{
		(void)ctx; // TODO
	}
	__declspec(dllexport) AgentData* __stdcall AI_CreateAgent(ContextData& ctx, AgentConfig const& cfg)
	{
		(void)ctx, (void)cfg;
		return nullptr; // TODO
	}
	__declspec(dllexport) void __stdcall AI_DestroyAgent(AgentData* agent)
	{
		(void)agent; // TODO
	}
	__declspec(dllexport) void __stdcall AI_SetPriority(AgentData& agent, int priority)
	{
		(void)agent, (void)priority; // TODO
	}
	__declspec(dllexport) void __stdcall AI_Chat(AgentData& agent, char const* message, CompletionCB cb, void* user_ctx)
	{
		(void)agent, (void)message, (void)cb, (void)user_ctx; // TODO
	}
	__declspec(dllexport) void __stdcall AI_Stimulate(AgentData& agent, char const* situation, CompletionCB cb, void* user_ctx)
	{
		(void)agent, (void)situation, (void)cb, (void)user_ctx; // TODO
	}
	__declspec(dllexport) void __stdcall AI_Think(AgentData& agent, CompletionCB cb, void* user_ctx)
	{
		(void)agent, (void)cb, (void)user_ctx; // TODO
	}
	__declspec(dllexport) int __stdcall AI_Update(ContextData& ctx)
	{
		(void)ctx;
		return 0; // TODO
	}
	__declspec(dllexport) void __stdcall AI_MemoryAdd(AgentData& agent, EMemoryTier tier, char const* role, char const* content)
	{
		(void)agent, (void)tier, (void)role, (void)content; // TODO
	}
	__declspec(dllexport) void __stdcall AI_MemoryClear(AgentData& agent, EMemoryTier tier)
	{
		(void)agent, (void)tier; // TODO
	}
	__declspec(dllexport) char const* __stdcall AI_MemoryGet(AgentData const& agent, EMemoryTier tier, size_t* out_len)
	{
		(void)agent, (void)tier;
		if (out_len) *out_len = 0;
		return ""; // TODO
	}
	__declspec(dllexport) void __stdcall AI_MemorySummarise(AgentData& agent, EMemoryTier src, EMemoryTier dst, CompletionCB cb, void* user_ctx)
	{
		(void)agent, (void)src, (void)dst, (void)cb, (void)user_ctx; // TODO
	}
	__declspec(dllexport) void __stdcall AI_GetUsageStats(ContextData const& ctx, UsageStats* out)
	{
		(void)ctx;
		if (out) *out = {};
	}
	__declspec(dllexport) void __stdcall AI_SetRateLimit(ContextData& ctx, int max_requests_per_minute)
	{
		(void)ctx, (void)max_requests_per_minute; // TODO
	}

	// Static checks to ensure the DLL function signatures match the header's function pointer types
	void AIDll::StaticChecks()
	{
		#define PR_AI_API_CHECK(prefix, name, function_type)\
		static_assert(std::is_same_v<AIDll::prefix##name##Fn, decltype(&AI_##prefix##name)>, "Function signature mismatch: AI_" #prefix #name);
		PR_AI_API(PR_AI_API_CHECK);
		#undef PR_AI_API_CHECK
	}
}
