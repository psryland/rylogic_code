//********************************
// AI Agent System
//  Copyright (c) Rylogic Ltd 2026
//********************************
// Notes:
//  - Provides AI-powered agents (NPCs) with personalities and conversational ability.
//  - Uses Azure OpenAI REST API via WinHTTP internally; all backend details are hidden.
//  - To avoid making this a build dependency, this header dynamically loads 'ai.dll' as needed.
//  - All network calls are asynchronous — call Context::Update() each frame to dispatch completed responses.
//  - Memory is managed in three tiers (Permanent, Summary, Recent) — the game decides what goes in each.
#pragma once
#include <cstdint>
#include <string>
#include <string_view>
#include <cassert>
#include "pr/win32/win32.h"

namespace pr::ai
{
	// Opaque types (defined within the DLL)
	struct ContextData;
	struct AgentData;

	// LLM backend provider
	enum class EProvider
	{
		AzureOpenAI,
		// OpenAI,      // Future
		// LlamaCpp,    // Future: local inference
	};

	// Memory tiers for agent context management.
	// When building LLM messages, tiers are concatenated: Permanent -> Summary -> Recent -> current prompt.
	// The game decides what goes in each tier and when to summarise.
	enum class EMemoryTier
	{
		// Long-lived facts that never expire (personality traits, career, key relationships).
		// Added by the game, never auto-pruned.
		Permanent,

		// Compressed summaries of older interactions. Game calls MemorySummarise() to compress
		// Recent messages into this tier (costs one LLM call).
		Summary,

		// Recent verbatim messages (user/assistant pairs). The most recent interactions.
		// Game can set a max count or manage manually.
		Recent,
	};

	// Error handling callback (same pattern as fbx/imgui)
	struct ErrorHandler
	{
		using FuncCB = void(*)(void*, char const* msg, size_t len);

		void* m_ctx;
		FuncCB m_cb;

		void operator()(std::string_view message) const
		{
			if (m_cb) m_cb(m_ctx, message.data(), message.size());
			else throw std::runtime_error(std::string(message));
		}
	};

	// Configuration for the AI context (backend connection)
	struct ContextConfig
	{
		EProvider m_provider = EProvider::AzureOpenAI;

		// Azure OpenAI connection settings
		char const* m_api_key = nullptr;       // API key (or null to read from env var AZURE_OPENAI_API_KEY)
		char const* m_endpoint = nullptr;      // e.g. "https://myresource.openai.azure.com"
		char const* m_deployment = nullptr;    // e.g. "gpt-4o-mini"
		char const* m_api_version = "2024-02-15-preview";

		// Throttling
		int m_max_requests_per_minute = 60;    // Rate limit (0 = unlimited)
		double m_max_cost_usd = 0.0;           // Cost cap in USD (0 = unlimited)
	};

	// Configuration for an individual agent/NPC
	struct AgentConfig
	{
		char const* m_name = "Agent";          // Display name for the agent

		// Personality (becomes the system prompt)
		char const* m_personality = nullptr;   // e.g. "You are a grizzled old sailor named Barnaby..."

		// Behavioural parameters
		float m_temperature = 0.7f;            // Creativity: 0.0 = deterministic, 1.0 = creative
		int m_max_response_tokens = 256;       // Max tokens per response (controls cost + response length)
		int m_priority = 5;                    // Request priority (1 = highest, 10 = lowest)

		// Structured output schema (optional). When set, the DLL requests JSON mode from the LLM
		// and injects this schema into the system prompt. Set to nullptr for free-form text.
		// e.g. R"({"goal": "string", "action": "string", "urgency": "int 1-10"})"
		char const* m_response_schema = nullptr;
	};

	// Token usage statistics
	struct UsageStats
	{
		int64_t m_prompt_tokens = 0;           // Total input tokens consumed
		int64_t m_completion_tokens = 0;       // Total output tokens consumed
		int64_t m_total_requests = 0;          // Total API calls made
		int64_t m_failed_requests = 0;         // Total failed API calls
		double m_estimated_cost_usd = 0.0;     // Estimated total cost
	};

	// Result of an async AI operation
	struct ChatResult
	{
		bool m_success;                        // True if the request completed successfully
		char const* m_response;                // The response text (null on failure)
		size_t m_response_len;                 // Length of the response text
		char const* m_error;                   // Error message (null on success)
		int m_prompt_tokens;                   // Tokens used for the prompt
		int m_completion_tokens;               // Tokens used for the response
	};

	// Async completion callback
	using CompletionCB = void(*)(void* user_ctx, ChatResult const& result);

	// ========================================
	// DLL function table (X-macro pattern)
	// ========================================
	#define PR_AI_API(x)\
	x(, Initialise        , ContextData* (__stdcall*)(ContextConfig const& cfg, ErrorHandler error_cb))\
	x(, Shutdown           , void (__stdcall*)(ContextData* ctx))\
	x(, CreateAgent        , AgentData* (__stdcall*)(ContextData& ctx, AgentConfig const& cfg))\
	x(, DestroyAgent       , void (__stdcall*)(AgentData* agent))\
	x(, SetPriority        , void (__stdcall*)(AgentData& agent, int priority))\
	x(, Chat               , void (__stdcall*)(AgentData& agent, char const* message, CompletionCB cb, void* user_ctx))\
	x(, Stimulate          , void (__stdcall*)(AgentData& agent, char const* situation, CompletionCB cb, void* user_ctx))\
	x(, Think              , void (__stdcall*)(AgentData& agent, CompletionCB cb, void* user_ctx))\
	x(, Update             , int  (__stdcall*)(ContextData& ctx))\
	x(, MemoryAdd          , void (__stdcall*)(AgentData& agent, EMemoryTier tier, char const* role, char const* content))\
	x(, MemoryClear        , void (__stdcall*)(AgentData& agent, EMemoryTier tier))\
	x(, MemoryGet          , char const* (__stdcall*)(AgentData const& agent, EMemoryTier tier, size_t* out_len))\
	x(, MemorySummarise    , void (__stdcall*)(AgentData& agent, EMemoryTier src, EMemoryTier dst, CompletionCB cb, void* user_ctx))\
	x(, GetUsageStats      , void (__stdcall*)(ContextData const& ctx, UsageStats* out))\
	x(, SetRateLimit       , void (__stdcall*)(ContextData& ctx, int max_requests_per_minute))

	// Dynamically loaded AI DLL
	class AIDll
	{
		friend struct Context;
		friend struct Agent;

		HMODULE m_module;

		#define PR_AI_FUNCTION_MEMBERS(prefix, name, function_type) using prefix##name##Fn = function_type; prefix##name##Fn prefix##name = {};
		PR_AI_API(PR_AI_FUNCTION_MEMBERS)
		#undef PR_AI_FUNCTION_MEMBERS

		AIDll()
			: m_module(win32::LoadDll<struct AIDllTag>("ai.dll"))
		{
			#pragma warning(push)
			#pragma warning(disable: 4191)
			#define PR_AI_GET_PROC_ADDRESS(prefix, name, function_type) prefix##name = reinterpret_cast<prefix##name##Fn>(GetProcAddress(m_module, "AI_" #prefix #name));
			PR_AI_API(PR_AI_GET_PROC_ADDRESS)
			#undef PR_AI_GET_PROC_ADDRESS
			#pragma warning(pop)
		}

		static AIDll& get() { static AIDll s_this; return s_this; }
		static void StaticChecks();
	};

	// ========================================
	// RAII Agent wrapper
	// ========================================
	struct Agent
	{
		AgentData* m_data;

		Agent()
			: m_data()
		{}
		Agent(Agent&& rhs) noexcept
			: m_data()
		{
			std::swap(m_data, rhs.m_data);
		}
		Agent(Agent const&) = delete;
		Agent& operator=(Agent&& rhs) noexcept
		{
			if (this != &rhs) std::swap(m_data, rhs.m_data);
			return *this;
		}
		Agent& operator=(Agent const&) = delete;
		~Agent()
		{
			if (m_data)
				AIDll::get().DestroyAgent(m_data);
		}

		explicit operator bool() const
		{
			return m_data != nullptr;
		}

		// Send a player message to this agent. Response delivered via callback.
		void Chat(char const* message, CompletionCB cb, void* user_ctx = nullptr)
		{
			assert(m_data != nullptr && "Agent not initialised");
			AIDll::get().Chat(*m_data, message, cb, user_ctx);
		}

		// Inject a situational stimulus (game event, environmental observation).
		// The agent reacts in character. Response delivered via callback.
		void Stimulate(char const* situation, CompletionCB cb, void* user_ctx = nullptr)
		{
			assert(m_data != nullptr && "Agent not initialised");
			AIDll::get().Stimulate(*m_data, situation, cb, user_ctx);
		}

		// Ask the agent to generate its own thought/goal. Response delivered via callback.
		void Think(CompletionCB cb, void* user_ctx = nullptr)
		{
			assert(m_data != nullptr && "Agent not initialised");
			AIDll::get().Think(*m_data, cb, user_ctx);
		}

		// Get the contents of a memory tier as a JSON string
		std::string_view Memory(EMemoryTier tier) const
		{
			assert(m_data != nullptr && "Agent not initialised");
			size_t len = 0;
			auto ptr = AIDll::get().MemoryGet(*m_data, tier, &len);
			return { ptr, len };
		}

		// Add a fact/message to a specific memory tier
		void MemoryAdd(EMemoryTier tier, char const* role, char const* content)
		{
			assert(m_data != nullptr && "Agent not initialised");
			AIDll::get().MemoryAdd(*m_data, tier, role, content);
		}

		// Clear all messages from a specific memory tier
		void MemoryClear(EMemoryTier tier)
		{
			assert(m_data != nullptr && "Agent not initialised");
			AIDll::get().MemoryClear(*m_data, tier);
		}

		// Compress messages from one tier into a summary in another tier (async, costs 1 LLM call)
		void MemorySummarise(EMemoryTier source, EMemoryTier dest, CompletionCB cb, void* user_ctx = nullptr)
		{
			assert(m_data != nullptr && "Agent not initialised");
			AIDll::get().MemorySummarise(*m_data, source, dest, cb, user_ctx);
		}

		// Update request priority (1 = highest, 10 = lowest)
		void SetPriority(int priority)
		{
			assert(m_data != nullptr && "Agent not initialised");
			AIDll::get().SetPriority(*m_data, priority);
		}
	};

	// ========================================
	// RAII Context wrapper (main entry point)
	// ========================================
	struct Context
	{
		ContextData* m_data;

		Context()
			: m_data()
		{}
		Context(ContextConfig const& cfg, ErrorHandler error_cb = {})
			: m_data(AIDll::get().Initialise(cfg, error_cb))
		{}
		Context(Context&& rhs) noexcept
			: m_data()
		{
			std::swap(m_data, rhs.m_data);
		}
		Context(Context const&) = delete;
		Context& operator=(Context&& rhs) noexcept
		{
			if (this != &rhs) std::swap(m_data, rhs.m_data);
			return *this;
		}
		Context& operator=(Context const&) = delete;
		~Context()
		{
			if (m_data)
				AIDll::get().Shutdown(m_data);
		}

		explicit operator bool() const
		{
			return m_data != nullptr;
		}

		// Create an agent within this context
		Agent CreateAgent(AgentConfig const& cfg)
		{
			assert(m_data != nullptr && "Context not initialised");
			Agent agent;
			agent.m_data = AIDll::get().CreateAgent(*m_data, cfg);
			return agent;
		}

		// Poll for completed async responses. Call once per frame from your game loop.
		// Returns the number of callbacks dispatched.
		int Update()
		{
			assert(m_data != nullptr && "Context not initialised");
			return AIDll::get().Update(*m_data);
		}

		// Get cumulative usage statistics
		UsageStats GetUsageStats() const
		{
			assert(m_data != nullptr && "Context not initialised");
			UsageStats stats;
			AIDll::get().GetUsageStats(*m_data, &stats);
			return stats;
		}

		// Update the rate limit
		void SetRateLimit(int max_requests_per_minute)
		{
			assert(m_data != nullptr && "Context not initialised");
			AIDll::get().SetRateLimit(*m_data, max_requests_per_minute);
		}
	};
}
