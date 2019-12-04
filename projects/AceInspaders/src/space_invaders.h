//*****************************************************************************************
// Space Invaders
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************

#pragma once

#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <cassert>
#include "pr/app/gfx_1bit.h"

namespace pr
{
	class SpaceInvaders
	{
	public:

		// Game sound identifiers
		enum class ESound
		{
			// The 'Get Ready' sound before the game starts.
			// Duration: 2sec
			GameStart,

			// The aliens getting one step closer
			AlienAdvance,

			// The player firing their weapon
			PlayerShoot,

			// The player firing their weapon
			AlienBombDrop,

			// An alien ship getting destroyed
			AlienDestroyed,

			// An alien ship getting destroyed
			PlayerDestroyed,

			// An alien bomb hitting a bunker
			BunkerDamaged,
		};

		// System functions needed to run this game
		struct ISystem
		{
			~ISystem() {}

			// Reads the system clock
			virtual int ClockMS() = 0;

			// Play the indicated sound
			virtual void PlaySound(ESound) = 0;
		};

		// Screen dimensions
		constexpr static int ScreenDimX = 128;
		constexpr static int ScreenDimY = 96;
		using Screen = gfx_1bit::Screen<ScreenDimX, ScreenDimY, uint8_t>;
		using Sprite = gfx_1bit::Sprite<uint8_t>;
		using EditableSprite = gfx_1bit::EditableSprite<8, uint8_t>;

	private:

		#pragma region Sprites
		static gfx_1bit::Sprite<uint32_t> sprite_ship()
		{
			static const uint32_t data[] =
			{
				0x7FF80000U, //  ############                   
				0xFFFF0000U, // ################                
				0x7FF80000U, //  ############                   
				0x1FC00000U, //    #######                      
				0x1FE00000U, //    ########                     
				0x3FF00000U, //   ##########                    
				0x3FF80000U, //   ###########                   
				0x3FFC0000U, //   ############                  
				0x3FFE0000U, //   #############                 
				0x3FFF0000U, //   ##############                
				0x3FFF8000U, //   ###############               
				0x3FF7FE00U, //   ########## ##########         
				0x1FFBFFE0U, //    ########## #############     
				0x1FFDFFF0U, //    ########### #############    
				0x1FFDFFF0U, //    ########### #############    
				0x1FFBFFE0U, //    ########## #############     
				0x3FF7FE00U, //   ########## ##########         
				0x3FFF8000U, //   ###############               
				0x3FFF0000U, //   ##############                
				0x3FFE0000U, //   #############                 
				0x3FFC0000U, //   ############                  
				0x3FF80000U, //   ###########                   
				0x3FF00000U, //   ##########                    
				0x1FE00000U, //    ########                     
				0x1FC00000U, //    #######                      
				0x7FF80000U, //  ############                   
				0xFFFF0000U, // ################                
				0x7FF80000U, //  ############                   
			};
			static gfx_1bit::Sprite<uint32_t> sprite(data);
			return sprite;
		}
		static Sprite const& sprite_alien1()
		{
			static uint8_t const data[] =
			{
				0x8C, // #   ##  
				0x5E, //  # #### 
				0xBB, // # ### ##
				0x5F, //  # #####
				0x5F, //  # #####
				0xBB, // # ### ##
				0x5E, //  # #### 
				0x8C, // #   ##  
			};
			static gfx_1bit::Sprite<uint8_t> sprite(&data[0], 8, 4);
			return sprite;
		}
		static Sprite const& sprite_alien2()
		{
			static uint8_t const data[] =
			{
				0x8C, // #   ##  
				0x5E, //  # #### 
				0xBB, // # ### ##
				0x5F, //  # #####
				0x5F, //  # #####
				0xBB, // # ### ##
				0x5E, //  # #### 
				0x8C, // #   ##  
			};
			static gfx_1bit::Sprite<uint8_t> sprite(&data[0], 8, 4);
			return sprite;
		}
		static Sprite const& sprite_alien3()
		{
			static uint8_t const data[] =
			{
				0x8C, // #   ##  
				0x5E, //  # #### 
				0xBB, // # ### ##
				0x5F, //  # #####
				0x5F, //  # #####
				0xBB, // # ### ##
				0x5E, //  # #### 
				0x8C, // #   ##  
			};
			static gfx_1bit::Sprite<uint8_t> sprite(&data[0], 8, 4);
			return sprite;
		}
		static Sprite const& sprite_bunker()
		{
			static uint8_t const data[] =
			{
				0x8C, // #   ##  
				0x5E, //  # #### 
				0xBB, // # ### ##
				0x5F, //  # #####
				0x5F, //  # #####
				0xBB, // # ### ##
				0x5E, //  # #### 
				0x8C, // #   ##  
			};
			static gfx_1bit::Sprite<uint8_t> sprite(&data[0], 8, 4);
			return sprite;
		}
		#pragma endregion

		struct Player
		{
			int m_xpos;
			int m_score;

			Player()
				: m_xpos(ScreenDimX / 2)
				, m_score()
			{}
		};
		struct Alien
		{
			enum class EType
			{
				Pawn = 1,
				Officer = 3,
				General = 5,
				Commander = 10,
			};
			enum class EState
			{
				Alive = 0,
				Exploding = 1,
				Dead = 5,
			};

			EType m_type;
			EState m_state;

			explicit Alien(EType type = EType::Pawn)
				:m_type(type)
				,m_state(EState::Alive)
			{}
		};
		struct Bunker
		{
			// The baracade that the player can hide behind.
			EditableSprite m_gfx;
			
			Bunker()
				:m_gfx(sprite_bunker())
			{}
		};
		struct Behaviour
		{
			int m_speed; // How fast and the direction of movement
			int m_height; // The vertical position.
		};
		struct Bomb
		{
			// Notes:
			//  - Dropped from an alien.
			//  - only collides with shields or ship.
		};
		struct Bullet
		{
			// Notes:
			//  - A sprite fired from the player. Travels verically upward
			//  - Collides with shields and alien ships
		};

		// Attack force
		constexpr static int AlienRows = 5;
		constexpr static int AlienCols = 6;
		constexpr static Alien::EType AlienConfig[AlienRows] =
		{
			Alien::EType::General,
			Alien::EType::Officer,
			Alien::EType::Officer,
			Alien::EType::Pawn,
			Alien::EType::Pawn,
		};

		// Defenses
		constexpr static int BunkerCount = 4;

		// Game state machine states
		enum class EState
		{
			// Reset data ready for a new game
			StartNewGame,

			// Wait for intro sounds etc to finish before starting
			// user interactive game play
			StartDelay,

			// Main 'playing' state for the game
			MainRun,

			// Enter this state as soon as collision is detected between the player and a bomb
			PlayerHit,


		};

		// Game state
		ISystem* m_system;
		Screen m_screen;
		Player m_player;
		Alien m_aliens[AlienRows][AlienCols];
		Bunker m_bunkers[BunkerCount];
		Behaviour m_behaviour;
		int m_timer_start_ms;
		int m_last_step_ms;
		EState m_state;

	public:

		SpaceInvaders(ISystem* sys)
			: m_system(sys)
			, m_screen()
			, m_player()
			, m_aliens()
			, m_behaviour()
			, m_timer_start_ms()
			, m_last_step_ms()
			, m_state(EState::StartNewGame)
		{}

		// Main loop step
		void Run()
		{
			switch (m_state)
			{
			case EState::StartNewGame:
				{
					SetupGame();
					m_system->PlaySound(ESound::GameStart);
					m_timer_start_ms = m_system->ClockMS();
					m_state = EState::StartDelay;
					break;
				}
			case EState::StartDelay:
				{
					if (m_system->ClockMS() - m_timer_start_ms < 2000) break;
					m_state = EState::MainRun;
					break;
				}
			case EState::MainRun:
				{
					auto elapsed = m_system->ClockMS() - m_last_step_ms;
					(void)elapsed;
					break;
				}
			}
		}

	private:

		// Set up to start a new game
		void SetupGame()
		{
			// Initialise the player 
			m_player = Player{};

			// Initialise the aliens
			for (int y = 0; y != AlienRows; ++y)
				for (int x = 0; x != AlienCols; ++x)
					m_aliens[y][x] = Alien(AlienConfig[y]);

			// Initialise the bunkers
			for (int i = 0; i != BunkerCount; ++i)
				m_bunkers[i] = Bunker{};
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::app
{
	PRUnitTest(SpaceInvadersTests)
	{
		using namespace pr::gfx_1bit;

		Screen<128, 64, uint8_t> screen;
		screen.Clear();
		screen.DumpToFile();

		auto ship = SpaceInvaders::ship();
		screen.Draw(ship, 10, 10);

		auto alien1 = SpaceInvaders::alien1();
		screen.Draw(alien1, 4, 4);
		screen.Draw(alien1, 10, 4);
		screen.Draw(alien1, 20, 4);
		screen.Draw(alien1, 30, 4);

		screen.DumpToFile();
	}
}
#endif
