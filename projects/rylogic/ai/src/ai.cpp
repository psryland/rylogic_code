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
#include <cstring>
#include <span>
#include <atomic>
#include <deque>

#include <Windows.h>
#include <winhttp.h>

#include "pr/storage/json.h"
#include "pr/ai/ai.h"

#if PR_AI_LOCAL_INFERENCE
#include "llama.h"
#endif

namespace pr::ai
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
		std::string m_user_content;  // The user prompt (for adding to Recent memory)
		std::string m_user_role;     // The role of the user prompt
		CompletionCB m_cb;
		void* m_user_ctx;
		bool m_add_response_to_recent; // Whether to auto-add the prompt+response to agent's Recent memory

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
		std::string m_user_content;
		std::string m_user_role;
		int m_prompt_tokens;
		int m_completion_tokens;
		bool m_success;
		bool m_filtered;
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

	// Data for an individual agent/NPC
	struct AgentData
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
		{
		}

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

	// Data for the AI context (manages backend connection, request queue, agents, usage tracking)
	struct ContextData
	{
		ErrorHandler m_error_cb;
		EProvider m_provider;

		// WinHTTP handles (Azure only)
		HINTERNET m_session = nullptr;
		HINTERNET m_connection = nullptr;

		// llama.cpp handles (local only)
		#if PR_AI_LOCAL_INFERENCE
		llama_model* m_llama_model = nullptr;
		llama_context* m_llama_ctx = nullptr;
		llama_sampler* m_llama_sampler = nullptr;
		#endif

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

		// Rate limiting (Azure only)
		RateLimiter m_rate_limiter;

		// Cost cap (Azure only)
		double m_max_cost_usd = 0.0;

		// Usage tracking
		UsageStats m_usage;

		// All agents owned by this context
		std::vector<std::unique_ptr<AgentData>> m_agents;

		// Initialised flag (true if backend is ready)
		bool m_ready = false;

		ContextData(ContextConfig const& cfg, ErrorHandler error_cb)
			: m_error_cb(error_cb)
			, m_provider(cfg.m_provider)
			, m_api_version(cfg.m_api_version ? cfg.m_api_version : "2024-02-15-preview")
			, m_deployment(cfg.m_deployment ? cfg.m_deployment : "")
			, m_max_cost_usd(cfg.m_max_cost_usd)
		{
			if (m_provider == EProvider::AzureOpenAI)
				InitialiseAzure(cfg);
			#if PR_AI_LOCAL_INFERENCE
			else if (m_provider == EProvider::LlamaCpp)
				InitialiseLlama(cfg);
			#endif
		}

		void InitialiseAzure(ContextConfig const& cfg)
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

			m_ready = true;
		}

		#if PR_AI_LOCAL_INFERENCE
		void InitialiseLlama(ContextConfig const& cfg)
		{
			if (!cfg.m_model_path)
			{
				m_error_cb("Model path not provided for LlamaCpp provider.");
				return;
			}

			// Suppress llama.cpp's internal logging to keep the console clean
			llama_log_set([](ggml_log_level, char const*, void*) {}, nullptr);

			llama_backend_init();

			// Load the model
			auto model_params = llama_model_default_params();
			model_params.n_gpu_layers = cfg.m_gpu_layers;
			m_llama_model = llama_model_load_from_file(cfg.m_model_path, model_params);
			if (!m_llama_model)
			{
				m_error_cb(std::format("Failed to load model: {}", cfg.m_model_path));
				return;
			}

			// Create the inference context
			auto ctx_params = llama_context_default_params();
			ctx_params.n_ctx = cfg.m_context_length;
			ctx_params.n_batch = cfg.m_context_length;
			m_llama_ctx = llama_init_from_model(m_llama_model, ctx_params);
			if (!m_llama_ctx)
			{
				m_error_cb("Failed to create llama context");
				llama_model_free(m_llama_model);
				m_llama_model = nullptr;
				return;
			}

			// Create a sampler chain (temperature -> top-p -> dist)
			// Temperature will be overridden per-request based on agent settings
			auto sparams = llama_sampler_chain_default_params();
			m_llama_sampler = llama_sampler_chain_init(sparams);
			llama_sampler_chain_add(m_llama_sampler, llama_sampler_init_top_k(40));
			llama_sampler_chain_add(m_llama_sampler, llama_sampler_init_top_p(0.9f, 1));
			llama_sampler_chain_add(m_llama_sampler, llama_sampler_init_temp(0.7f));
			llama_sampler_chain_add(m_llama_sampler, llama_sampler_init_dist(LLAMA_DEFAULT_SEED));

			m_ready = true;
		}
		#endif

		~ContextData()
		{
			#if PR_AI_LOCAL_INFERENCE
			if (m_llama_sampler) llama_sampler_free(m_llama_sampler);
			if (m_llama_ctx) llama_free(m_llama_ctx);
			if (m_llama_model) llama_model_free(m_llama_model);
			#endif
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
				.m_user_content = content ? content : "",
				.m_user_role = role ? role : "user",
				.m_cb = cb,
				.m_user_ctx = user_ctx,
				.m_add_response_to_recent = add_to_recent,
			});
		}

		// Submit the next pending request
		void SubmitNextRequest()
		{
			PendingRequest req;
			{
				std::lock_guard lock(m_mutex);

				if (m_pending.empty()) return;
				if (m_in_flight.load() >= k_max_in_flight) return;

				// Check rate limits and cost cap (Azure only)
				if (m_provider == EProvider::AzureOpenAI)
				{
					m_rate_limiter.Prune();
					if (!m_rate_limiter.CanSend()) return;
					if (m_max_cost_usd > 0 && m_usage.m_estimated_cost_usd >= m_max_cost_usd) return;
					m_rate_limiter.RecordRequest();
				}

				req = std::move(const_cast<PendingRequest&>(m_pending.top()));
				m_pending.pop();
				m_in_flight.fetch_add(1);
			}

			// Perform the inference (synchronous, called from Update on the game thread)
			CompletedResponse completed;
			#if PR_AI_LOCAL_INFERENCE
			if (m_provider == EProvider::LlamaCpp)
				completed = PerformLocalInference(req);
			else
				#endif
				completed = PerformHttpRequest(req);

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

					// Cost tracking only applies to Azure
					if (m_provider == EProvider::AzureOpenAI)
					{
						m_usage.m_estimated_cost_usd =
							(m_usage.m_prompt_tokens * k_input_cost_per_million / 1'000'000.0) +
							(m_usage.m_completion_tokens * k_output_cost_per_million / 1'000'000.0);
					}
				}
				else
				{
					m_usage.m_failed_requests++;
					m_usage.m_total_requests++;
				}
			}
		}

		// Perform local inference via llama.cpp
		#if PR_AI_LOCAL_INFERENCE
		CompletedResponse PerformLocalInference(PendingRequest const& req)
		{
			CompletedResponse result = {};
			result.m_cb = req.m_cb;
			result.m_user_ctx = req.m_user_ctx;
			result.m_agent = req.m_agent;
			result.m_add_response_to_recent = req.m_add_response_to_recent;
			result.m_user_content = req.m_user_content;
			result.m_user_role = req.m_user_role;

			if (!m_llama_model || !m_llama_ctx)
			{
				result.m_error = "Local model not loaded";
				result.m_success = false;
				return result;
			}

			try
			{
				// Parse the JSON body to extract the messages array and parameters
				auto doc = json::Read(std::string_view(req.m_body));
				auto const& root = doc.to_object();
				auto const& messages = root["messages"].to_array();

				auto const* max_tok_val = root.find("max_tokens");
				auto max_tokens = max_tok_val ? static_cast<int>(max_tok_val->to<double>()) : 256;

				// Build the chat messages for llama_chat_apply_template
				std::vector<llama_chat_message> chat_msgs;
				for (auto const& m : messages)
				{
					auto const& obj = m.to_object();
					auto role = obj["role"].to<std::string>();
					auto content = obj["content"].to<std::string>();
					// Store strings separately to keep pointers valid
					chat_msgs.push_back({ nullptr, nullptr });
				}

				// We need to keep the strings alive while using the pointers
				std::vector<std::string> roles, contents;
				for (auto const& m : messages)
				{
					auto const& obj = m.to_object();
					roles.push_back(obj["role"].to<std::string>());
					contents.push_back(obj["content"].to<std::string>());
				}
				chat_msgs.clear();
				for (size_t i = 0; i < roles.size(); ++i)
					chat_msgs.push_back({ roles[i].c_str(), contents[i].c_str() });

				// Apply the chat template to produce a formatted prompt
				auto const* vocab = llama_model_get_vocab(m_llama_model);
				std::vector<char> prompt_buf(4096);
				auto prompt_len = llama_chat_apply_template(
					nullptr, // use model's built-in template
					chat_msgs.data(), chat_msgs.size(),
					true, // add assistant turn start
					prompt_buf.data(), static_cast<int32_t>(prompt_buf.size()));

				if (prompt_len < 0)
				{
					result.m_error = "Failed to apply chat template";
					result.m_success = false;
					return result;
				}

				// Resize and retry if buffer was too small
				if (prompt_len > static_cast<int32_t>(prompt_buf.size()))
				{
					prompt_buf.resize(prompt_len + 1);
					prompt_len = llama_chat_apply_template(
						nullptr, chat_msgs.data(), chat_msgs.size(),
						true, prompt_buf.data(), static_cast<int32_t>(prompt_buf.size()));
				}

				std::string prompt(prompt_buf.data(), prompt_len);

				// Tokenize the prompt
				auto n_prompt_max = static_cast<int32_t>(prompt.size()) + 128;
				std::vector<llama_token> tokens(n_prompt_max);
				auto n_tokens = llama_tokenize(vocab, prompt.c_str(), static_cast<int32_t>(prompt.size()),
					tokens.data(), n_prompt_max, true, true);
				if (n_tokens < 0)
				{
					result.m_error = "Tokenization failed";
					result.m_success = false;
					return result;
				}
				tokens.resize(n_tokens);
				result.m_prompt_tokens = n_tokens;

				// Clear the KV cache for a fresh generation
				llama_memory_t mem = llama_get_memory(m_llama_ctx);
				if (mem) llama_memory_clear(mem, false);

				// Decode the prompt
				auto batch = llama_batch_get_one(tokens.data(), n_tokens);
				if (llama_decode(m_llama_ctx, batch) != 0)
				{
					result.m_error = "Failed to decode prompt";
					result.m_success = false;
					return result;
				}

				// Reset the sampler for this request
				llama_sampler_reset(m_llama_sampler);

				// Generate tokens
				auto eos = llama_vocab_eos(vocab);
				auto eot = llama_vocab_eot(vocab);
				std::string response;
				int n_generated = 0;

				for (int i = 0; i < max_tokens; ++i)
				{
					auto token = llama_sampler_sample(m_llama_sampler, m_llama_ctx, -1);

					// Check for end of sequence, end of turn, or control tokens
					if (token == eos || token == eot || llama_vocab_is_eog(vocab, token) || llama_vocab_is_control(vocab, token))
						break;

					// Accept the token in the sampler
					llama_sampler_accept(m_llama_sampler, token);

					// Convert token to text (special=false to skip rendering template markers)
					char piece[256];
					auto piece_len = llama_token_to_piece(vocab, token, piece, sizeof(piece), 0, false);

					// Detect hidden special tokens: if special=false renders empty but special=true doesn't,
					// then this is a template marker (e.g. <|im_end|>) that the model didn't register as EOG.
					if (piece_len <= 0)
					{
						auto special_len = llama_token_to_piece(vocab, token, piece, sizeof(piece), 0, true);
						if (special_len > 0)
							break; // Template marker — stop generation
					}

					if (piece_len > 0)
						response.append(piece, piece_len);

					// Decode the new token
					auto next_batch = llama_batch_get_one(&token, 1);
					if (llama_decode(m_llama_ctx, next_batch) != 0)
						break;

					++n_generated;
				}

				// Strip any template markers that leaked through (including partial ones)
				for (auto const& marker : { "<|im_end|>", "<|im_start|>", "<|end|>", "<|assistant|>", "<|user|>" })
				{
					for (auto pos = response.find(marker); pos != std::string::npos; pos = response.find(marker))
						response.erase(pos, std::strlen(marker));
				}

				// Strip partial template markers (e.g., "<|im" at end of response)
				{
					auto partial = response.find("<|");
					if (partial != std::string::npos)
						response.erase(partial);
				}

				// Truncate at role-name tokens that leak through after template markers are stripped.
				// Models often generate <|im_end|><|im_start|>user\n... where the bare "user" or
				// "assistant" survives because it's a regular text token.
				for (auto const& role : { "\nuser\n", "\nassistant\n", "\nsystem\n" })
				{
					auto pos = response.find(role);
					if (pos != std::string::npos)
						response.erase(pos);
				}

				// Also check for role names at the very end of the response (no trailing newline)
				for (auto const& role : { "user", "assistant", "system" })
				{
					auto rlen = std::strlen(role);
					if (response.size() >= rlen && response.compare(response.size() - rlen, rlen, role) == 0)
					{
						// Only strip if preceded by a newline or start of string
						auto before = response.size() - rlen;
						if (before == 0 || response[before - 1] == '\n')
							response.erase(before);
					}
				}

				// Truncate at common meta-commentary patterns that instruction-tuned models emit
				for (auto const& pattern : { "\n**", "\n---", "\nNote:", "\n(Note", "\nFollow-up", "\nYou are now", "\nImagine you", "\n[Narrator]", "\nNow, let" })
				{
					auto pos = response.find(pattern);
					if (pos != std::string::npos)
						response.erase(pos);
				}

				// Also truncate at mid-paragraph prompt injection patterns
				for (auto const& pattern : { "You are now a character", "You are now a ", "Imagine you are", "[Narrator]", "The group has gathered to discuss" })
				{
					auto pos = response.find(pattern);
					if (pos != std::string::npos && pos > 0)
						response.erase(pos);
				}

				// Trim leading/trailing whitespace
				auto const is_ws = [](char c) { return c == ' ' || c == '\n' || c == '\r' || c == '\t'; };
				while (!response.empty() && is_ws(response.front())) response.erase(response.begin());
				while (!response.empty() && is_ws(response.back())) response.pop_back();

				result.m_response = std::move(response);
				result.m_completion_tokens = n_generated;
				result.m_success = true;
			}
			catch (std::exception const& ex)
			{
				result.m_error = std::format("Local inference error: {}", ex.what());
				result.m_success = false;
			}

			return result;
		}
		#endif

		// Perform a synchronous HTTP POST request to the Azure OpenAI endpoint
		CompletedResponse PerformHttpRequest(PendingRequest const& req)
		{
			CompletedResponse result = {};
			result.m_cb = req.m_cb;
			result.m_user_ctx = req.m_user_ctx;
			result.m_agent = req.m_agent;
			result.m_add_response_to_recent = req.m_add_response_to_recent;
			result.m_user_content = req.m_user_content;
			result.m_user_role = req.m_user_role;

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

			// Set timeouts: resolve=5s, connect=10s, send=30s, receive=60s
			WinHttpSetTimeouts(h_request, 5000, 10000, 30000, 60000);

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
				// Detect content filter errors (HTTP 400 with content_filter_result or ResponsibleAIPolicyViolation)
				if (status_code == 400 && (response_body.find("content_filter") != std::string::npos || response_body.find("ResponsibleAI") != std::string::npos))
				{
					result.m_filtered = true;
					result.m_error = "Content filtered by Azure moderation policy";
					result.m_success = false;
					return result;
				}

				// Rate limit errors get a cleaner message
				if (status_code == 429)
				{
					result.m_error = "Rate limited — too many requests";
					result.m_success = false;
					return result;
				}

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

				// Check if the response was truncated by content filtering
				auto const* finish_reason = first_choice.find("finish_reason");
				if (finish_reason && finish_reason->to<std::string>() == "content_filter")
				{
					result.m_filtered = true;
					result.m_error = "Response blocked by content filter";
					result.m_success = false;
					return result;
				}

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
}

extern "C"
{
	using namespace pr;
	using namespace pr::ai;

	__declspec(dllexport) ContextData* __stdcall AI_Initialise(ContextConfig const& cfg, ErrorHandler error_cb)
	{
		try
		{
			auto ctx = new ContextData(cfg, error_cb);
			if (!ctx->m_ready)
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
		// Enqueue the request. The user message is passed as the current prompt and
		// will be auto-added to Recent memory when the response arrives (via add_to_recent).
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
			// Auto-add the user prompt and response to the agent's Recent memory
			if (c.m_success && c.m_add_response_to_recent && c.m_agent)
			{
				c.m_agent->m_recent.push_back({ c.m_user_role, c.m_user_content });
				c.m_agent->m_recent.push_back({ "assistant", c.m_response });
			}

			if (c.m_cb)
			{
				ChatResult result;
				result.m_success = c.m_success;
				result.m_filtered = c.m_filtered;
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
