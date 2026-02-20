//********************************
// AI Agent System DLL
//  Copyright (c) Rylogic Ltd 2026
//********************************
// Implements the C-style API for the AI agent DLL.
// All backend details (WinHTTP, JSON) are contained within this DLL.

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
#include <atomic>
#include <deque>

#include <Windows.h>
#include <winhttp.h>

#pragma comment(lib, "winhttp.lib")

#include "pr/storage/json.h"
#include "pr/ai/ai.h"

using namespace pr;
using namespace pr::ai;

namespace
{
	// Cost per million tokens for GPT-4o-mini (Azure OpenAI)
	constexpr double k_input_cost_per_million = 0.15;
	constexpr double k_output_cost_per_million = 0.60;

	// Convert a narrow string to a wide string
	std::wstring ToWide(std::string_view s)
	{
		if (s.empty()) return {};
		auto len = MultiByteToWideChar(CP_UTF8, 0, s.data(), static_cast<int>(s.size()), nullptr, 0);
		std::wstring ws(len, 0);
		MultiByteToWideChar(CP_UTF8, 0, s.data(), static_cast<int>(s.size()), ws.data(), len);
		return ws;
	}

	// A message in the chat history
	struct Message
	{
		std::string m_role;
		std::string m_content;
	};

	// A pending request waiting to be submitted or in-flight
	struct PendingRequest
	{
		AgentData* m_agent;
		int m_priority;
		int64_t m_sequence;
		std::string m_body;          // JSON request body
		CompletionCB m_cb;
		void* m_user_ctx;
		bool m_add_response_to_recent; // Whether to auto-add the response to agent's Recent memory

		// Lower priority value = higher priority. On tie, lower sequence wins.
		bool operator >(PendingRequest const& rhs) const
		{
			if (m_priority != rhs.m_priority) return m_priority > rhs.m_priority;
			return m_sequence > rhs.m_sequence;
		}
	};

	// A completed response ready for callback dispatch
	struct CompletedResponse
	{
		CompletionCB m_cb;
		void* m_user_ctx;
		std::string m_response;
		std::string m_error;
		int m_prompt_tokens;
		int m_completion_tokens;
		bool m_success;
		AgentData* m_agent;
		bool m_add_response_to_recent;
	};

	// Simple token-bucket rate limiter
	struct RateLimiter
	{
		int m_max_per_minute = 60;
		std::deque<std::chrono::steady_clock::time_point> m_timestamps;

		bool CanSend() const
		{
			if (m_max_per_minute <= 0) return true;
			return static_cast<int>(m_timestamps.size()) < m_max_per_minute;
		}

		void RecordRequest()
		{
			auto now = std::chrono::steady_clock::now();
			m_timestamps.push_back(now);
		}

		void Prune()
		{
			auto cutoff = std::chrono::steady_clock::now() - std::chrono::minutes(1);
			while (!m_timestamps.empty() && m_timestamps.front() < cutoff)
				m_timestamps.pop_front();
		}
	};

	// Extract hostname from an endpoint URL like "https://myresource.openai.azure.com"
	std::wstring ExtractHostname(std::string_view endpoint)
	{
		auto pos = endpoint.find("://");
		if (pos != std::string_view::npos) endpoint.remove_prefix(pos + 3);

		// Remove trailing slash
		if (!endpoint.empty() && endpoint.back() == '/') endpoint.remove_suffix(1);

		return ToWide(endpoint);
	}

	// Build the URL path for a chat completion request
	std::wstring BuildRequestPath(std::string_view deployment, std::string_view api_version)
	{
		auto path = std::format("/openai/deployments/{}/chat/completions?api-version={}", deployment, api_version);
		return ToWide(path);
	}
}

// ========================================
// Opaque type definitions
// ========================================

struct pr::ai::AgentData
{
	ContextData& m_ctx;
	std::string m_name;
	std::string m_personality;
	std::string m_response_schema;
	float m_temperature = 0.7f;
	int m_max_response_tokens = 256;
	int m_priority = 5;

	// Three-tier memory
	std::vector<Message> m_permanent;
	std::vector<Message> m_summary;
	std::vector<Message> m_recent;

	// Cache for MemoryGet
	mutable std::string m_memory_cache;

	AgentData(ContextData& ctx, AgentConfig const& cfg)
		: m_ctx(ctx)
		, m_name(cfg.m_name ? cfg.m_name : "Agent")
		, m_personality(cfg.m_personality ? cfg.m_personality : "")
		, m_response_schema(cfg.m_response_schema ? cfg.m_response_schema : "")
		, m_temperature(cfg.m_temperature)
		, m_max_response_tokens(cfg.m_max_response_tokens)
		, m_priority(cfg.m_priority)
	{}

	// Access the memory tier by enum
	std::vector<Message>& Tier(EMemoryTier tier)
	{
		switch (tier)
		{
		case EMemoryTier::Permanent: return m_permanent;
		case EMemoryTier::Summary: return m_summary;
		case EMemoryTier::Recent: return m_recent;
		default: return m_recent;
		}
	}
	std::vector<Message> const& Tier(EMemoryTier tier) const
	{
		return const_cast<AgentData*>(this)->Tier(tier);
	}

	// Build the complete messages array for a chat completion request
	json::Array BuildMessages(char const* role, char const* content) const
	{
		json::Array messages;

		// System prompt from personality
		if (!m_personality.empty())
		{
			auto system_content = m_personality;
			if (!m_response_schema.empty())
			{
				system_content += "\n\nIMPORTANT: Always respond in valid JSON matching this schema: ";
				system_content += m_response_schema;
			}

			json::Object msg;
			msg["role"] = "system";
			msg["content"] = std::move(system_content);
			messages.push_back(json::Value(std::move(msg)));
		}

		// Permanent tier
		for (auto const& m : m_permanent)
		{
			json::Object msg;
			msg["role"] = m.m_role;
			msg["content"] = m.m_content;
			messages.push_back(json::Value(std::move(msg)));
		}

		// Summary tier
		for (auto const& m : m_summary)
		{
			json::Object msg;
			msg["role"] = m.m_role;
			msg["content"] = m.m_content;
			messages.push_back(json::Value(std::move(msg)));
		}

		// Recent tier
		for (auto const& m : m_recent)
		{
			json::Object msg;
			msg["role"] = m.m_role;
			msg["content"] = m.m_content;
			messages.push_back(json::Value(std::move(msg)));
		}

		// Current prompt
		if (role && content)
		{
			json::Object msg;
			msg["role"] = role;
			msg["content"] = content;
			messages.push_back(json::Value(std::move(msg)));
		}

		return messages;
	}
};

struct pr::ai::ContextData
{
	ErrorHandler m_error_cb;

	// WinHTTP handles
	HINTERNET m_session = nullptr;
	HINTERNET m_connection = nullptr;

	// Configuration
	std::string m_api_key;
	std::string m_deployment;
	std::string m_api_version;
	std::wstring m_request_path;

	// Request management
	std::mutex m_mutex;
	std::priority_queue<PendingRequest, std::vector<PendingRequest>, std::greater<PendingRequest>> m_pending;
	std::vector<CompletedResponse> m_completed;
	int64_t m_sequence = 0;
	std::atomic<int> m_in_flight = 0;
	static constexpr int k_max_in_flight = 5;

	// Rate limiting
	RateLimiter m_rate_limiter;

	// Cost cap
	double m_max_cost_usd = 0.0;

	// Usage tracking
	UsageStats m_usage;

	// All agents owned by this context
	std::vector<std::unique_ptr<AgentData>> m_agents;

	ContextData(ContextConfig const& cfg, ErrorHandler error_cb)
		: m_error_cb(error_cb)
		, m_api_version(cfg.m_api_version ? cfg.m_api_version : "2024-02-15-preview")
		, m_deployment(cfg.m_deployment ? cfg.m_deployment : "")
		, m_max_cost_usd(cfg.m_max_cost_usd)
	{
		// API key: from config or environment variable
		if (cfg.m_api_key)
			m_api_key = cfg.m_api_key;
		else if (auto env = std::getenv("AZURE_OPENAI_API_KEY"))
			m_api_key = env;

		if (m_api_key.empty())
		{
			m_error_cb("API key not provided. Set AZURE_OPENAI_API_KEY env var or pass in ContextConfig.");
			return;
		}
		if (!cfg.m_endpoint)
		{
			m_error_cb("Endpoint not provided in ContextConfig.");
			return;
		}

		m_rate_limiter.m_max_per_minute = cfg.m_max_requests_per_minute;
		m_request_path = BuildRequestPath(m_deployment, m_api_version);

		// Create WinHTTP session
		m_session = WinHttpOpen(L"pr::ai/1.0", WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
		if (!m_session)
		{
			m_error_cb(std::format("WinHttpOpen failed: {}", GetLastError()));
			return;
		}

		// Connect to the Azure endpoint
		auto hostname = ExtractHostname(cfg.m_endpoint);
		m_connection = WinHttpConnect(m_session, hostname.c_str(), INTERNET_DEFAULT_HTTPS_PORT, 0);
		if (!m_connection)
		{
			m_error_cb(std::format("WinHttpConnect failed: {}", GetLastError()));
			WinHttpCloseHandle(m_session);
			m_session = nullptr;
			return;
		}
	}

	~ContextData()
	{
		if (m_connection) WinHttpCloseHandle(m_connection);
		if (m_session) WinHttpCloseHandle(m_session);
	}

	// Enqueue a request for async processing
	void EnqueueRequest(AgentData& agent, char const* role, char const* content, CompletionCB cb, void* user_ctx, bool add_to_recent)
	{
		// Build the JSON request body
		auto messages = agent.BuildMessages(role, content);

		json::Object request;
		request["messages"] = json::Value(std::move(messages));
		request["temperature"] = static_cast<double>(agent.m_temperature);
		request["max_tokens"] = static_cast<int64_t>(agent.m_max_response_tokens);

		// Request JSON mode if schema is set
		if (!agent.m_response_schema.empty())
		{
			json::Object response_format;
			response_format["type"] = "json_object";
			request["response_format"] = json::Value(std::move(response_format));
		}

		auto body = json::Write(json::Value(std::move(request)), { .Indent = false });

		std::lock_guard lock(m_mutex);
		m_pending.push(PendingRequest{
			.m_agent = &agent,
			.m_priority = agent.m_priority,
			.m_sequence = m_sequence++,
			.m_body = std::move(body),
			.m_cb = cb,
			.m_user_ctx = user_ctx,
			.m_add_response_to_recent = add_to_recent,
		});
	}

	// Submit the next pending request via WinHTTP (synchronous HTTP on a thread)
	void SubmitNextRequest()
	{
		PendingRequest req;
		{
			std::lock_guard lock(m_mutex);

			// Check rate limits and cost cap
			m_rate_limiter.Prune();
			if (!m_rate_limiter.CanSend()) return;
			if (m_max_cost_usd > 0 && m_usage.m_estimated_cost_usd >= m_max_cost_usd) return;
			if (m_pending.empty()) return;
			if (m_in_flight.load() >= k_max_in_flight) return;

			req = std::move(const_cast<PendingRequest&>(m_pending.top()));
			m_pending.pop();
			m_rate_limiter.RecordRequest();
			m_in_flight.fetch_add(1);
		}

		// Perform synchronous HTTP request (called from Update on the game thread for simplicity)
		auto completed = PerformHttpRequest(req);

		{
			std::lock_guard lock(m_mutex);
			m_completed.push_back(std::move(completed));
			m_in_flight.fetch_sub(1);

			// Update usage stats
			auto& c = m_completed.back();
			if (c.m_success)
			{
				m_usage.m_prompt_tokens += c.m_prompt_tokens;
				m_usage.m_completion_tokens += c.m_completion_tokens;
				m_usage.m_total_requests++;
				m_usage.m_estimated_cost_usd =
					(m_usage.m_prompt_tokens * k_input_cost_per_million / 1'000'000.0) +
					(m_usage.m_completion_tokens * k_output_cost_per_million / 1'000'000.0);
			}
			else
			{
				m_usage.m_failed_requests++;
				m_usage.m_total_requests++;
			}
		}
	}

	// Perform a synchronous HTTP POST request to the Azure OpenAI endpoint
	CompletedResponse PerformHttpRequest(PendingRequest const& req)
	{
		CompletedResponse result = {};
		result.m_cb = req.m_cb;
		result.m_user_ctx = req.m_user_ctx;
		result.m_agent = req.m_agent;
		result.m_add_response_to_recent = req.m_add_response_to_recent;

		if (!m_connection)
		{
			result.m_error = "Not connected";
			result.m_success = false;
			return result;
		}

		// Open an HTTP request
		auto h_request = WinHttpOpenRequest(m_connection, L"POST", m_request_path.c_str(), nullptr, WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, WINHTTP_FLAG_SECURE);
		if (!h_request)
		{
			result.m_error = std::format("WinHttpOpenRequest failed: {}", GetLastError());
			result.m_success = false;
			return result;
		}

		// Set headers
		auto auth_header = std::format(L"api-key: {}", ToWide(m_api_key));
		WinHttpAddRequestHeaders(h_request, auth_header.c_str(), static_cast<DWORD>(auth_header.size()), WINHTTP_ADDREQ_FLAG_ADD);
		WinHttpAddRequestHeaders(h_request, L"Content-Type: application/json", static_cast<DWORD>(-1), WINHTTP_ADDREQ_FLAG_ADD);

		// Send the request
		auto body_data = req.m_body.data();
		auto body_size = static_cast<DWORD>(req.m_body.size());
		if (!WinHttpSendRequest(h_request, WINHTTP_NO_ADDITIONAL_HEADERS, 0, const_cast<char*>(body_data), body_size, body_size, 0))
		{
			result.m_error = std::format("WinHttpSendRequest failed: {}", GetLastError());
			result.m_success = false;
			WinHttpCloseHandle(h_request);
			return result;
		}

		// Receive the response
		if (!WinHttpReceiveResponse(h_request, nullptr))
		{
			result.m_error = std::format("WinHttpReceiveResponse failed: {}", GetLastError());
			result.m_success = false;
			WinHttpCloseHandle(h_request);
			return result;
		}

		// Check HTTP status code
		DWORD status_code = 0;
		DWORD status_size = sizeof(status_code);
		WinHttpQueryHeaders(h_request, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, WINHTTP_HEADER_NAME_BY_INDEX, &status_code, &status_size, WINHTTP_NO_HEADER_INDEX);

		// Read the response body
		std::string response_body;
		DWORD bytes_available = 0;
		while (WinHttpQueryDataAvailable(h_request, &bytes_available) && bytes_available > 0)
		{
			auto offset = response_body.size();
			response_body.resize(offset + bytes_available);
			DWORD bytes_read = 0;
			WinHttpReadData(h_request, response_body.data() + offset, bytes_available, &bytes_read);
			response_body.resize(offset + bytes_read);
		}
		WinHttpCloseHandle(h_request);

		if (status_code != 200)
		{
			result.m_error = std::format("HTTP {}: {}", status_code, response_body);
			result.m_success = false;
			return result;
		}

		// Parse the JSON response
		try
		{
			auto doc = json::Read(std::string_view(response_body));
			auto const& root = doc.to_object();

			// Extract the response text from choices[0].message.content
			auto const& choices = root["choices"].to_array();
			if (choices.empty())
			{
				result.m_error = "No choices in response";
				result.m_success = false;
				return result;
			}

			auto const& first_choice = choices[0].to_object();
			auto const& message = first_choice["message"].to_object();
			auto const* content = message.find("content");
			if (content)
				result.m_response = content->to<std::string>();

			// Extract token usage
			auto const* usage = root.find("usage");
			if (usage)
			{
				auto const& usage_obj = usage->to_object();
				auto const* pt = usage_obj.find("prompt_tokens");
				auto const* ct = usage_obj.find("completion_tokens");
				if (pt) result.m_prompt_tokens = static_cast<int>(pt->to<double>());
				if (ct) result.m_completion_tokens = static_cast<int>(ct->to<double>());
			}

			result.m_success = true;
		}
		catch (std::exception const& ex)
		{
			result.m_error = std::format("JSON parse error: {}", ex.what());
			result.m_success = false;
		}

		return result;
	}
};

// ========================================
// DLL Exports
// ========================================

extern "C"
{
	__declspec(dllexport) ContextData* __stdcall AI_Initialise(ContextConfig const& cfg, ErrorHandler error_cb)
	{
		try
		{
			auto ctx = new ContextData(cfg, error_cb);
			if (!ctx->m_session)
			{
				delete ctx;
				return nullptr;
			}
			return ctx;
		}
		catch (std::exception const& ex)
		{
			error_cb(ex.what());
			return nullptr;
		}
	}

	__declspec(dllexport) void __stdcall AI_Shutdown(ContextData* ctx)
	{
		delete ctx;
	}

	__declspec(dllexport) AgentData* __stdcall AI_CreateAgent(ContextData& ctx, AgentConfig const& cfg)
	{
		auto agent = std::make_unique<AgentData>(ctx, cfg);
		auto ptr = agent.get();
		ctx.m_agents.push_back(std::move(agent));
		return ptr;
	}

	__declspec(dllexport) void __stdcall AI_DestroyAgent(AgentData* agent)
	{
		if (!agent) return;
		auto& agents = agent->m_ctx.m_agents;
		agents.erase(
			std::remove_if(agents.begin(), agents.end(), [agent](auto& a) { return a.get() == agent; }),
			agents.end());
	}

	__declspec(dllexport) void __stdcall AI_SetPriority(AgentData& agent, int priority)
	{
		agent.m_priority = priority;
	}

	__declspec(dllexport) void __stdcall AI_Chat(AgentData& agent, char const* message, CompletionCB cb, void* user_ctx)
	{
		// Add the user message to recent memory before sending
		agent.m_recent.push_back({ "user", message });
		agent.m_ctx.EnqueueRequest(agent, "user", message, cb, user_ctx, true);
	}

	__declspec(dllexport) void __stdcall AI_Stimulate(AgentData& agent, char const* situation, CompletionCB cb, void* user_ctx)
	{
		// Wrap the situation as a narrator message
		auto prompt = std::format("[Narrator] {}\nReact briefly, in character.", situation);
		agent.m_ctx.EnqueueRequest(agent, "user", prompt.c_str(), cb, user_ctx, true);
	}

	__declspec(dllexport) void __stdcall AI_Think(AgentData& agent, CompletionCB cb, void* user_ctx)
	{
		auto prompt = "What are you thinking about right now? "
			"Express a goal, observation, or reaction in character. Be brief.";
		agent.m_ctx.EnqueueRequest(agent, "user", prompt, cb, user_ctx, false);
	}

	__declspec(dllexport) int __stdcall AI_Update(ContextData& ctx)
	{
		// Submit pending requests (up to the rate limit and in-flight cap)
		while (true)
		{
			bool has_pending;
			{
				std::lock_guard lock(ctx.m_mutex);
				ctx.m_rate_limiter.Prune();
				has_pending = !ctx.m_pending.empty()
					&& ctx.m_rate_limiter.CanSend()
					&& ctx.m_in_flight.load() < ContextData::k_max_in_flight;
			}
			if (!has_pending) break;
			ctx.SubmitNextRequest();
		}

		// Drain the completed queue and dispatch callbacks
		std::vector<CompletedResponse> to_dispatch;
		{
			std::lock_guard lock(ctx.m_mutex);
			std::swap(to_dispatch, ctx.m_completed);
		}

		for (auto& c : to_dispatch)
		{
			// Auto-add successful responses to the agent's Recent memory
			if (c.m_success && c.m_add_response_to_recent && c.m_agent)
			{
				c.m_agent->m_recent.push_back({ "assistant", c.m_response });
			}

			if (c.m_cb)
			{
				ChatResult result;
				result.m_success = c.m_success;
				result.m_response = c.m_success ? c.m_response.c_str() : nullptr;
				result.m_response_len = c.m_success ? c.m_response.size() : 0;
				result.m_error = c.m_success ? nullptr : c.m_error.c_str();
				result.m_prompt_tokens = c.m_prompt_tokens;
				result.m_completion_tokens = c.m_completion_tokens;
				c.m_cb(c.m_user_ctx, result);
			}
		}

		return static_cast<int>(to_dispatch.size());
	}

	__declspec(dllexport) void __stdcall AI_MemoryAdd(AgentData& agent, EMemoryTier tier, char const* role, char const* content)
	{
		agent.Tier(tier).push_back({ role ? role : "system", content ? content : "" });
	}

	__declspec(dllexport) void __stdcall AI_MemoryClear(AgentData& agent, EMemoryTier tier)
	{
		agent.Tier(tier).clear();
	}

	__declspec(dllexport) char const* __stdcall AI_MemoryGet(AgentData const& agent, EMemoryTier tier, size_t* out_len)
	{
		// Build a JSON array of the tier's messages
		json::Array arr;
		for (auto const& m : agent.Tier(tier))
		{
			json::Object msg;
			msg["role"] = m.m_role;
			msg["content"] = m.m_content;
			arr.push_back(json::Value(std::move(msg)));
		}
		agent.m_memory_cache = json::Write(json::Value(std::move(arr)), { .Indent = false });
		if (out_len) *out_len = agent.m_memory_cache.size();
		return agent.m_memory_cache.c_str();
	}

	__declspec(dllexport) void __stdcall AI_MemorySummarise(AgentData& agent, EMemoryTier src, EMemoryTier dst, CompletionCB cb, void* user_ctx)
	{
		// Build a prompt asking the LLM to summarise the source tier
		auto& source = agent.Tier(src);
		if (source.empty())
		{
			if (cb)
			{
				ChatResult result = {};
				result.m_success = true;
				result.m_response = "";
				result.m_response_len = 0;
				cb(user_ctx, result);
			}
			return;
		}

		// Build the content to summarise
		std::string content_to_summarise;
		for (auto const& m : source)
			content_to_summarise += std::format("{}: {}\n", m.m_role, m.m_content);

		auto summary_prompt = std::format(
			"Summarise the following conversation into a concise paragraph that preserves key facts, "
			"decisions, and relationship changes. Keep it under 200 words.\n\n{}", content_to_summarise);

		// We need a special callback that adds the summary to the dest tier and clears source
		struct SummariseCtx
		{
			AgentData* agent;
			EMemoryTier src;
			EMemoryTier dst;
			CompletionCB user_cb;
			void* user_ctx;
		};
		auto* sctx = new SummariseCtx{ &agent, src, dst, cb, user_ctx };

		agent.m_ctx.EnqueueRequest(agent, "user", summary_prompt.c_str(),
			[](void* ctx, ChatResult const& result)
			{
				auto* sctx = static_cast<SummariseCtx*>(ctx);
				if (result.m_success)
				{
					// Add the summary to the destination tier
					sctx->agent->Tier(sctx->dst).push_back({
						"system",
						std::format("[Summary of earlier interactions] {}", std::string_view(result.m_response, result.m_response_len))
					});

					// Clear the source tier
					sctx->agent->Tier(sctx->src).clear();
				}

				// Forward to the user's callback
				if (sctx->user_cb)
					sctx->user_cb(sctx->user_ctx, result);

				delete sctx;
			},
			sctx, false);
	}

	__declspec(dllexport) void __stdcall AI_GetUsageStats(ContextData const& ctx, UsageStats* out)
	{
		if (out) *out = ctx.m_usage;
	}

	__declspec(dllexport) void __stdcall AI_SetRateLimit(ContextData& ctx, int max_requests_per_minute)
	{
		std::lock_guard lock(ctx.m_mutex);
		ctx.m_rate_limiter.m_max_per_minute = max_requests_per_minute;
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
