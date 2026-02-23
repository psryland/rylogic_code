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
#include <filesystem>

#include <Windows.h>

#include "pr/ai/ai.h"

using namespace pr::ai;
namespace fs = std::filesystem;

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

// Parsed command line options
struct Options
{
	bool use_local = false;
	std::string model_path;
	int gpu_layers = -1; // -1 = all layers on GPU (when available)
};

// Trim leading and trailing whitespace (and optionally quotes)
void Trim(std::string& s, bool trim_quotes = false)
{
	auto is_trim_char = [trim_quotes](char c) {
		return c == ' ' || c == '\t' || c == '\n' || c == '\r' || (trim_quotes && c == '"');
	};
	while (!s.empty() && is_trim_char(s.front())) s.erase(s.begin());
	while (!s.empty() && is_trim_char(s.back())) s.pop_back();
}

// Read a line of input from the user
std::string ReadLine()
{
	std::string line;
	std::getline(std::cin, line);
	Trim(line);
	return line;
}

// Walk up from the exe directory looking for sdk/llama-cpp/models
std::string FindModelsDir()
{
	char buf[MAX_PATH] = {};
	GetModuleFileNameA(nullptr, buf, MAX_PATH);

	for (auto dir = fs::path(buf).parent_path(); !dir.empty() && dir != dir.root_path(); dir = dir.parent_path())
	{
		auto models = dir / "sdk" / "llama-cpp" / "models";
		if (fs::exists(models))
			return models.string();
	}
	return {};
}

// List .gguf files in a directory
std::vector<std::string> ListModels(std::string const& models_dir)
{
	std::vector<std::string> models;
	if (models_dir.empty() || !fs::exists(models_dir))
		return models;

	for (auto const& entry : fs::directory_iterator(models_dir))
	{
		if (entry.is_regular_file() && entry.path().extension() == ".gguf")
			models.push_back(entry.path().string());
	}
	std::sort(models.begin(), models.end());
	return models;
}

// Parse command-line arguments
Options ParseArgs(int argc, char* argv[])
{
	Options opts;
	for (int i = 1; i < argc; ++i)
	{
		auto arg = std::string(argv[i]);
		if (arg == "--local" && i + 1 < argc)
		{
			opts.model_path = argv[++i];
			opts.use_local = true;
		}
		else if (arg == "--azure")
		{
			opts.use_local = false;
		}
		else if (arg == "--gpu-layers" && i + 1 < argc)
		{
			opts.gpu_layers = std::atoi(argv[++i]);
		}
	}
	return opts;
}

// Prompt the user to choose between Azure and Local providers
bool PromptForProvider()
{
	std::cout << "Select AI provider:\n";
	std::cout << "  1. Azure OpenAI (cloud)\n";
	std::cout << "  2. Local model (llama.cpp)\n";
	std::cout << "> ";

	auto input = ReadLine();
	std::cout << "\n";
	return input == "2" || input == "local";
}

// Prompt the user to select a local model file
std::string PromptForModel()
{
	auto models_dir = FindModelsDir();
	auto available = ListModels(models_dir);

	if (available.empty())
	{
		std::cerr << "No .gguf model files found.\n";
		if (!models_dir.empty())
			std::cerr << "  Searched: " << models_dir << "\n";
		std::cerr << "  Download a model with: dotnet-script sdk/llama-cpp/_get_model.csx\n";
		std::cerr << "  Or specify: --local <path-to-model.gguf>\n";
		return {};
	}

	std::cout << "Available models:\n";
	for (size_t i = 0; i < available.size(); ++i)
	{
		auto size_mb = fs::file_size(available[i]) / (1024.0 * 1024.0);
		std::cout << std::format("  {}. {} ({:.0f} MB)\n", i + 1, fs::path(available[i]).filename().string(), size_mb);
	}

	// Auto-select if there's only one model
	if (available.size() == 1)
	{
		std::cout << std::format("> Using: {}\n\n", fs::path(available[0]).filename().string());
		return available[0];
	}

	std::cout << "> ";
	auto input = ReadLine();
	std::cout << "\n";

	auto idx = std::atoi(input.c_str());
	if (idx < 1 || idx > static_cast<int>(available.size()))
	{
		std::cerr << "Invalid selection.\n";
		return {};
	}
	return available[idx - 1];
}

// Prompt the user for a discussion topic
std::string PromptForTopic(std::mt19937& rng)
{
	std::cout << "Enter a topic for discussion (or press Enter for a random one):\n> ";
	auto input = ReadLine();
	std::cout << "\n";

	if (input.empty())
	{
		auto idx = std::uniform_int_distribution<int>(0, k_num_topics - 1)(rng);
		return k_topics[idx];
	}
	return input;
}

// Create the AI context based on options
Context CreateContext(Options const& opts)
{
	ContextConfig cfg;
	if (opts.use_local)
	{
		cfg.m_provider = EProvider::LlamaCpp;
		cfg.m_model_path = opts.model_path.c_str();
		cfg.m_gpu_layers = opts.gpu_layers;
		cfg.m_context_length = 4096;

		std::cout << "Using local model: " << fs::path(opts.model_path).filename().string() << "\n";
		std::cout << "GPU layers: " << (opts.gpu_layers < 0 ? "all" : std::to_string(opts.gpu_layers)) << "\n\n";
	}
	else
	{
		cfg.m_provider = EProvider::AzureOpenAI;
		cfg.m_endpoint = std::getenv("AZURE_OPENAI_ENDPOINT");
		cfg.m_deployment = std::getenv("AZURE_OPENAI_DEPLOYMENT");
		cfg.m_max_requests_per_minute = 30;

		if (!cfg.m_endpoint || !cfg.m_deployment)
		{
			std::cerr << "Error: Set AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_DEPLOYMENT environment variables.\n";
			std::cerr << "  e.g. AZURE_OPENAI_ENDPOINT=https://myresource.openai.azure.com\n";
			std::cerr << "       AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini\n";
			return {};
		}
	}

	return Context(cfg);
}

// Create agents with random personalities
std::vector<AgentState> CreateAgents(Context& ctx, int count, bool use_local, std::mt19937& rng)
{
	// Pick unique personalities
	std::vector<int> personality_indices(k_num_personalities);
	std::iota(personality_indices.begin(), personality_indices.end(), 0);
	std::shuffle(personality_indices.begin(), personality_indices.end(), rng);

	// Use shorter max tokens for local models (faster inference)
	auto max_tokens = use_local ? 80 : 150;

	std::vector<AgentState> agents(count);
	for (int i = 0; i < count; ++i)
	{
		auto personality_desc = k_personalities[personality_indices[i % k_num_personalities]];

		AgentConfig cfg;
		cfg.m_personality = personality_desc;
		cfg.m_temperature = 0.9f;
		cfg.m_max_response_tokens = max_tokens;
		cfg.m_priority = 3;

		agents[i].agent = ctx.CreateAgent(cfg);
		agents[i].personality = personality_desc;
		agents[i].colour_idx = i % k_num_colours;

		// Seed world knowledge
		agents[i].agent.MemoryAdd(EMemoryTier::Permanent, "system",
			"You are in a medieval fantasy settlement. Keep responses to 1-2 sentences. Stay in character.");
	}
	return agents;
}

// Have each agent choose a name via the LLM
void ChooseNames(Context& ctx, std::vector<AgentState>& agents)
{
	std::cout << "Agents choosing names...\n";

	for (auto& a : agents)
	{
		a.agent.Chat(
			"Choose a unique, memorable name for yourself that fits your personality. "
			"Respond with ONLY the name, nothing else.",
			[](void* user_ctx, ChatResult const& result)
			{
				auto& state = *static_cast<AgentState*>(user_ctx);
				if (result.m_success)
				{
					state.name = std::string(result.m_response, result.m_response_len);
					Trim(state.name, true);
				}
				else
				{
					state.name = "Unknown";
				}
				state.name_received = true;
			},
			&a);
	}

	// Wait for all names
	while (true)
	{
		ctx.Update();
		auto all_done = std::all_of(agents.begin(), agents.end(), [](auto& a) { return a.name_received; });
		if (all_done) break;
		std::this_thread::sleep_for(std::chrono::milliseconds(100));
	}

	// Display agents
	std::cout << "\nAgents:\n";
	for (auto const& a : agents)
		std::cout << std::format("  {}{:<12}{} ({})\n", k_colours[a.colour_idx], a.name, k_reset, a.personality);
	std::cout << "\n";
}

// Run the discussion rounds
void RunDiscussion(Context& ctx, std::vector<AgentState>& agents, std::string const& topic, int num_rounds, std::mt19937& rng)
{
	std::cout << std::format("Topic: \"{}\"\n\n", topic);

	// Add the topic to all agents' memory
	for (auto& a : agents)
	{
		auto topic_msg = std::format("The group is discussing: \"{}\"", topic);
		a.agent.MemoryAdd(EMemoryTier::Permanent, "system", topic_msg.c_str());
	}

	auto agent_count = static_cast<int>(agents.size());
	std::string discussion_log;

	for (int round = 0; round < num_rounds; ++round)
	{
		// Pick a random agent to speak
		auto speaker_idx = std::uniform_int_distribution<int>(0, agent_count - 1)(rng);
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
				else if (result.m_filtered)
					*sc.text = "*stays silent, lost in thought*";
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
}

// Print usage statistics
void PrintStats(Context& ctx)
{
	auto stats = ctx.GetUsageStats();
	std::cout << std::format(
		"\n=== Usage Stats ===\n"
		"Requests: {} | Tokens: {} in / {} out | Est. cost: ${:.4f}\n",
		stats.m_total_requests,
		stats.m_prompt_tokens,
		stats.m_completion_tokens,
		stats.m_estimated_cost_usd);
}

int main(int argc, char* argv[])
{
	// Enable ANSI escape sequences and UTF-8 output in the Windows console
	auto h_console = GetStdHandle(STD_OUTPUT_HANDLE);
	DWORD console_mode = 0;
	GetConsoleMode(h_console, &console_mode);
	SetConsoleMode(h_console, console_mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
	SetConsoleOutputCP(CP_UTF8);

	std::cout << "\n=== AI Discussion Test ===\n\n";

	// Parse CLI args, then prompt for anything not specified
	auto opts = ParseArgs(argc, argv);
	if (opts.model_path.empty() && !opts.use_local)
	{
		// No provider specified on command line — prompt interactively
		opts.use_local = PromptForProvider();
	}
	if (opts.use_local && opts.model_path.empty())
	{
		opts.model_path = PromptForModel();
		if (opts.model_path.empty())
			return 1;
	}

	std::mt19937 rng(std::random_device{}());
	auto topic = PromptForTopic(rng);

	// Use fewer agents for local inference (CPU is slow)
	auto agent_count = opts.use_local
		? std::uniform_int_distribution<int>(2, 3)(rng)
		: std::uniform_int_distribution<int>(3, 8)(rng);

	std::cout << "Creating " << agent_count << " agents...\n\n";

	// Create context and agents
	auto ctx = CreateContext(opts);
	if (!ctx)
	{
		std::cerr << "Error: Failed to create AI context.\n";
		return 1;
	}

	auto agents = CreateAgents(ctx, agent_count, opts.use_local, rng);
	ChooseNames(ctx, agents);

	// Run 3-5 rounds for local, 5-10 for cloud
	auto num_rounds = opts.use_local
		? std::uniform_int_distribution<int>(3, 5)(rng)
		: std::uniform_int_distribution<int>(5, 10)(rng);

	RunDiscussion(ctx, agents, topic, num_rounds, rng);
	PrintStats(ctx);

	return 0;
}
