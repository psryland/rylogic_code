#pragma once
#include "forward.h"

namespace allkeys
{
	class Player
	{
		fluid_player_t* m_player;

	public:
		explicit Player(Synth& synth);
		Player(Player&& rhs) noexcept;
		Player(Player const&) = delete;
		Player& operator=(Player&& rhs) noexcept;
		Player& operator=(Player const&) = delete;
		~Player();

		// Get the current playing status
		[[nodiscard]] fluid_player_status Status() const;

		// Player controls
		void Play();
		void Pause();
		void Seek(int time_ms);
		void Loop(bool enabled);

		// Get/Set the tempo of playback
		[[nodiscard]] int TempoBPM() const;
		void Tempo(fluid_player_set_tempo_type tempo_type, double tempo);

		// Add midi data from memory
		void Add(std::span<uint8_t const> midi_data);

		// Add midi data from file
		void Add(char const* midi_file);
	};
}
