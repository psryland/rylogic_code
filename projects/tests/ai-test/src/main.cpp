//********************************
// AI Agent Discussion Test
//  Copyright (c) Rylogic Ltd 2026
//********************************
// Console application that creates multiple AI agents with random personalities,
// selects a random topic, and simulates a discussion between the agents.
// Each agent gets a unique ANSI colour in the console output.

#include <iostream>
#include <string>
#include <vector>
#include <random>
#include <format>
#include <thread>
#include <chrono>
#include <atomic>
#include <numeric>
#include <cstdlib>

#include <Windows.h>

#include "pr/ai/ai.h"

using namespace pr::ai;

// ANSI colour codes for up to 8 agents
constexpr char const* k_colours[] = {
	"\033[91m", // Red
	"\033[92m", // Green
	"\033[93m", // Yellow
	"\033[94m", // Blue
	"\033[95m", // Magenta
	"\033[96m", // Cyan
	"\033[97m", // White
	"\033[33m", // Dark Yellow
};
constexpr char const* k_reset = "\033[0m";
constexpr int k_num_colours = static_cast<int>(std::size(k_colours));

// Personality archetypes
constexpr char const* k_personalities[] = {
	"an optimistic inventor who is always coming up with wild ideas",
	"a cynical philosopher who questions everything",
	"an excitable merchant who sees profit in every situation",
	"a stoic warrior who speaks plainly and values action over words",
	"a mischievous trickster who loves wordplay and clever solutions",
	"an elderly scholar who draws on ancient knowledge and speaks thoughtfully",
	"a nervous apprentice who is eager to prove themselves but often second-guesses",
	"a boisterous pirate who is loud, confident, and loves a good adventure",
	"a calm healer who cares about everyone's wellbeing and seeks peaceful solutions",
	"a suspicious spy who trusts no one and always looks for hidden motives",
};
constexpr int k_num_personalities = static_cast<int>(std::size(k_personalities));

// Discussion topics / problems
constexpr char const* k_topics[] = {
	"Should we build a bridge or a boat to cross the river?",
	"A mysterious stranger has arrived in town. What should we do?",
	"We've found a map to a sunken treasure. How do we proceed?",
	"The well has dried up. How do we find water?",
	"Two of our trading partners are at war. Which side do we support?",
	"A dragon has been spotted near the mountains. What is our plan?",
	"We need to choose a new leader for the settlement. Who should it be?",
	"A plague is spreading through the nearby village. How do we respond?",
	"We've discovered a hidden cave full of crystals. What do we do with them?",
	"Our ship has run aground on an uncharted island. What now?",
};
constexpr int k_num_topics = static_cast<int>(std::size(k_topics));

// Agent state during the discussion
struct AgentState
{
	Agent agent;
	std::string name;
	std::string personality;
	int colour_idx;
	bool name_received = false;
};

int main()
{
	// Enable ANSI escape sequences and UTF-8 output in the Windows console
	auto h_console = GetStdHandle(STD_OUTPUT_HANDLE);
	DWORD console_mode = 0;
	GetConsoleMode(h_console, &console_mode);
	SetConsoleMode(h_console, console_mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
	SetConsoleOutputCP(CP_UTF8);

	// Seed RNG
	std::mt19937 rng(std::random_device{}());

	// Determine agent count (3-8)
	int agent_count = std::uniform_int_distribution<int>(3, 8)(rng);

	// Pick unique personalities
	std::vector<int> personality_indices(k_num_personalities);
	std::iota(personality_indices.begin(), personality_indices.end(), 0);
	std::shuffle(personality_indices.begin(), personality_indices.end(), rng);

	// Pick a random topic
	int topic_idx = std::uniform_int_distribution<int>(0, k_num_topics - 1)(rng);
	auto topic = k_topics[topic_idx];

	std::cout << "\n=== AI Discussion Test ===\n\n";
	std::cout << "Creating " << agent_count << " agents...\n\n";

	// Create context (reads API key from AZURE_OPENAI_API_KEY env var)
	ContextConfig ctx_cfg;
	ctx_cfg.m_endpoint = std::getenv("AZURE_OPENAI_ENDPOINT");
	ctx_cfg.m_deployment = std::getenv("AZURE_OPENAI_DEPLOYMENT");
	ctx_cfg.m_max_requests_per_minute = 30;

	if (!ctx_cfg.m_endpoint || !ctx_cfg.m_deployment)
	{
		std::cerr << "Error: Set AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_DEPLOYMENT environment variables.\n";
		std::cerr << "  e.g. AZURE_OPENAI_ENDPOINT=https://myresource.openai.azure.com\n";
		std::cerr << "       AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini\n";
		return 1;
	}

	Context ctx(ctx_cfg);
	if (!ctx)
	{
		std::cerr << "Error: Failed to create AI context.\n";
		return 1;
	}

	// Create agents
	std::vector<AgentState> agents(agent_count);
	for (int i = 0; i < agent_count; ++i)
	{
		auto personality_desc = k_personalities[personality_indices[i % k_num_personalities]];

		AgentConfig cfg;
		cfg.m_personality = personality_desc;
		cfg.m_temperature = 0.9f;
		cfg.m_max_response_tokens = 150;
		cfg.m_priority = 3;

		agents[i].agent = ctx.CreateAgent(cfg);
		agents[i].personality = personality_desc;
		agents[i].colour_idx = i % k_num_colours;

		// Seed world knowledge
		agents[i].agent.MemoryAdd(EMemoryTier::Permanent, "system",
			"You are in a medieval fantasy settlement. Keep responses to 1-3 sentences. Stay in character.");
	}

	// Phase 1: Each agent chooses their own name
	std::cout << "Agents choosing names...\n";
	std::atomic<int> names_pending = agent_count;

	for (int i = 0; i < agent_count; ++i)
	{
		agents[i].agent.Chat(
			"Choose a unique, memorable name for yourself that fits your personality. "
			"Respond with ONLY the name, nothing else.",
			[](void* user_ctx, ChatResult const& result)
			{
				auto& state = *static_cast<AgentState*>(user_ctx);
				if (result.m_success)
				{
					state.name = std::string(result.m_response, result.m_response_len);

					// Trim whitespace and quotes
					while (!state.name.empty() && (state.name.front() == ' ' || state.name.front() == '"' || state.name.front() == '\n'))
						state.name.erase(state.name.begin());
					while (!state.name.empty() && (state.name.back() == ' ' || state.name.back() == '"' || state.name.back() == '\n'))
						state.name.pop_back();
				}
				else
				{
					state.name = "Unknown";
				}
				state.name_received = true;
			},
			&agents[i]);
	}

	// Wait for all names
	while (true)
	{
		ctx.Update();
		bool all_done = true;
		for (auto& a : agents)
		{
			if (!a.name_received)
			{
				all_done = false;
				break;
			}
		}
		if (all_done) break;
		std::this_thread::sleep_for(std::chrono::milliseconds(100));
	}

	// Display agents
	std::cout << "\nAgents:\n";
	for (auto const& a : agents)
	{
		std::cout << std::format("  {}{:<12}{} ({})\n", k_colours[a.colour_idx], a.name, k_reset, a.personality);
	}

	std::cout << std::format("\nTopic: \"{}\"\n\n", topic);

	// Add the topic to all agents' memory
	for (auto& a : agents)
	{
		auto topic_msg = std::format("The group is discussing: \"{}\"", topic);
		a.agent.MemoryAdd(EMemoryTier::Permanent, "system", topic_msg.c_str());
	}

	// Phase 2: Discussion rounds
	int num_rounds = std::uniform_int_distribution<int>(5, 10)(rng);
	std::string discussion_log;

	for (int round = 0; round < num_rounds; ++round)
	{
		// Pick a random agent to speak
		int speaker_idx = std::uniform_int_distribution<int>(0, agent_count - 1)(rng);
		auto& speaker = agents[speaker_idx];

		// Build the stimulus
		std::string stimulus;
		if (round == 0)
		{
			stimulus = std::format(
				"The group has gathered to discuss: \"{}\"\n"
				"You are the first to speak. Share your initial thoughts.", topic);
		}
		else
		{
			stimulus = std::format(
				"The discussion so far:\n{}\n"
				"It's your turn to respond. React to what others have said or add your own perspective.",
				discussion_log);
		}

		// Send stimulus and wait for response
		std::atomic<bool> response_received = false;
		std::string response_text;

		struct StimCtx { std::atomic<bool>* received; std::string* text; };
		StimCtx stim_ctx = { &response_received, &response_text };

		speaker.agent.Stimulate(stimulus.c_str(),
			[](void* user_ctx, ChatResult const& result)
			{
				auto& sc = *static_cast<StimCtx*>(user_ctx);
				if (result.m_success)
					*sc.text = std::string(result.m_response, result.m_response_len);
				else
					*sc.text = std::format("[Error: {}]", result.m_error ? result.m_error : "unknown");
				sc.received->store(true);
			},
			&stim_ctx);

		// Wait for this response
		while (!response_received.load())
		{
			ctx.Update();
			std::this_thread::sleep_for(std::chrono::milliseconds(100));
		}

		// Print the response
		std::cout << std::format("{}[{}]:{} {}\n\n",
			k_colours[speaker.colour_idx], speaker.name, k_reset, response_text);

		// Add to discussion log and all agents' recent memory
		auto log_entry = std::format("{}: {}", speaker.name, response_text);
		discussion_log += log_entry + "\n";

		for (int i = 0; i < agent_count; ++i)
		{
			if (i != speaker_idx)
				agents[i].agent.MemoryAdd(EMemoryTier::Recent, "user", log_entry.c_str());
		}
	}

	// Print usage stats
	auto stats = ctx.GetUsageStats();
	std::cout << std::format(
		"\n=== Usage Stats ===\n"
		"Requests: {} | Tokens: {} in / {} out | Est. cost: ${:.4f}\n",
		stats.m_total_requests,
		stats.m_prompt_tokens,
		stats.m_completion_tokens,
		stats.m_estimated_cost_usd);

	return 0;
}
