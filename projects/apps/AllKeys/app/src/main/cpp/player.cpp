#include "player.h"
#include "synth.h"

namespace allkeys
{
	Player::Player(Synth &synth)
		: m_player(new_fluid_player(synth))
	{}

	Player::Player(Player &&rhs) noexcept
		: m_player(rhs.m_player)
	{
		rhs.m_player = nullptr;
	}

	Player &Player::operator=(Player &&rhs) noexcept
	{
		std::swap(m_player, rhs.m_player);
		return *this;
	}

	Player::~Player()
	{
		if (m_player == nullptr) return;
		delete_fluid_player(m_player);
	}

	// Get the current playing status
	fluid_player_status Player::Status() const
	{
		return static_cast<fluid_player_status>(fluid_player_get_status(m_player));
	}

	// Player controls
	void Player::Play()
	{
		Check(fluid_player_play(m_player), "FluidPlayer play failed");
	}

	void Player::Pause()
	{
		Check(fluid_player_stop(m_player), "FluidPlayer stop failed");
	}

	void Player::Seek(int time_ms)
	{
		Check(fluid_player_seek(m_player, time_ms), "FluidPlayer seek failed");
	}

	void Player::Loop(bool enabled)
	{
		Check(fluid_player_set_loop(m_player, enabled), "FluidPlayer set loop failed");
	}

	// Get/Set the tempo of playback
	int Player::TempoBPM() const
	{
		return fluid_player_get_bpm(m_player);
	}
	void Player::Tempo(fluid_player_set_tempo_type tempo_type, double tempo)
	{
		Check(fluid_player_set_tempo(m_player, tempo_type, tempo), "FluidPlayer set tempo failed");
	}

	// Add midi data from memory
	void Player::Add(std::span<uint8_t const> midi_data)
	{
		Check(fluid_player_add_mem(m_player, midi_data.data(), midi_data.size()), "FluidPlayer Add midi data failed");
	}

	// Add midi data from file
	void Player::Add(char const* midi_file)
	{
		Check(fluid_player_add(m_player, midi_file), "FluidPlayer Add midi file failed");
	}
}