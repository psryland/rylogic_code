//*****************************************************************************************
// Space Invaders
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************

#pragma once

#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <cassert>
#include "pr/gfx/onebit.h"

namespace pr
{
	class SpaceInvaders
	{
		// Notes:
		//  - Implement ISystem to for the environment
		//  - Call Run() periodically
		//  - Render the 'Display()' buffer as often as you want, probably not more than once per step though
		//  - All screen positions are stored in milli-pixels to allow for very fast step rates
		//  - All positions are 'centre' positions

	public:

		// Game sound identifiers
		enum class ESound
		{
			// The 'Get Ready' sound before the game starts. Duration: 2sec
			LevelStart,

			// The aliens getting one step closer
			AlienAdvance,

			// The player firing their weapon
			PlayerShoot,

			// The player firing their weapon
			AlienBombDrop,

			// An alien ship getting destroyed
			AlienDestroyed,

			// The player ship getting destroyed
			PlayerDestroyed,

			// An alien bomb hitting a bunker
			BunkerDamaged,

			// A bomb has been shot down
			BombDestroyed,

			// When the last alien is defeated
			LevelCompleted,

			// Game over sound
			GameOver,

			// Sound count
			NumberOf,
		};

		// User input data
		struct UserInputData
		{
			// The absolute value of the maximum joystick deflection (assumes symmetric joystick)
			constexpr static int AxisMaxAbs = +1000;

			int JoystickX;   // Value between [-1000, +1000]
			int JoystickY;   // Value between [-1000, +1000]
			bool FireButton; // Fire button state. Provider should handle transient button state
		};

		// System functions needed to run this game
		struct ISystem
		{
			virtual ~ISystem() {}

			// Play the indicated sound
			virtual void PlaySound(ESound) = 0;

			// Get the current user input data
			virtual UserInputData UserInput() = 0;
		};

		// Screen dimensions
		constexpr static int ScreenDimX = 128;
		constexpr static int ScreenDimY = 96;
		constexpr static int StartGameDelayMS = 1000; // The length of the pause between the start game sound and starting
		constexpr static int EndLevelDelayMS = 1000; // The length of the pause between the start game sound and starting

		// Player
		constexpr static int PlayerMaxSpeed = 1000; // The max speed of the player in pixels/second
		constexpr static int PlayerYPos = ScreenDimY - 6;
		constexpr static int BunkerYPos = ScreenDimY - 16;

		// Aliens
		constexpr static int AlienCols = 8;                      // The number of cols of aliens
		constexpr static int AlienRows = 5;                      // The number of rows of aliens
		constexpr static int AlienSizeX = 7;                     // The bounds that all alien types fit within
		constexpr static int AlienSizeY = 5;                     // The bounds that all alien types fit within
		constexpr static int AlienSpaceX = 5;                    // The number of pixels between cols of aliens
		constexpr static int AlienSpaceY = 2;                    // The number of pixels between rows of aliens
		constexpr static int AlienEdgeMargin = ScreenDimY / 10;  // Distance from the edge that the aliens turn around
		constexpr static int AlienInitialYPos = ScreenDimY / 10; // The initial vertical position of the highest alien
		constexpr static int AlienFinalYPos = ScreenDimY - 26;   // The high the aliens need to reach
		constexpr static int AlienInitialStepPeriodMS = 500;     // The time between each advance step at the start
		constexpr static int AlienFinalStepPeriodMS = 20;        // The minimum time between each advance step
		constexpr static int AlienAdvanceX = 2;                  // The number of pixels the aliens advance at the
		constexpr static int AlienAdvanceY = 5;                  // The number of pixels the aliens advance at the

		// Bunkers
		constexpr static int BunkerCount = 4;   // The number of bunkers

		// Bombs
		constexpr static int MaxBombs = 3;        // The maximum number of bombs on screen at once
		constexpr static int BombSpeed = 100;     // The speed of the falling bombs in pixels/second
		constexpr static int BombPeriodMS = 1000; // The maximum time between bomb drops (in msec)
		constexpr static int BombValue = 1;

		// Bullets
		constexpr static int MaxBullets = 1;    // The maximum number of bullets on screen at once
		constexpr static int BulletSpeed = 100; // The speed of the bullets in pixels/second

		// The types of aliens in each row
		enum class EAlienType
		{
			Private,
			Lieutenant,
			Captain,
			Major,
			General,
		};
		struct AlienConfigData { EAlienType Type; int Value; };
		constexpr static AlienConfigData AlienConfig[AlienRows] =
		{
			{ EAlienType::General    , 10 },
			{ EAlienType::Major      , 7  },
			{ EAlienType::Captain    , 5  },
			{ EAlienType::Lieutenant , 3  },
			{ EAlienType::Private    , 1  },
		};

		// Screen type
		using Screen = onebit::Bitmap<ScreenDimX, ScreenDimY, uint8_t>;

	private:

		using SpriteR = onebit::BitmapR<uint8_t>;
		template <int DimX, int DimY> using SpriteW = onebit::Bitmap<DimX, DimY, uint8_t>;
		struct Coord 
		{
			int x, y;
			Coord() :x(), y() {}
			Coord(int x_, int y_) :x(x_), y(y_) {}
			friend Coord operator +(Coord lhs, Coord rhs) { return Coord(lhs.x + rhs.x, lhs.y + rhs.y); }
			friend Coord operator -(Coord lhs, Coord rhs) { return Coord(lhs.x - rhs.x, lhs.y - rhs.y); }
			friend Coord operator *(Coord lhs, int rhs)   { return Coord(lhs.x * rhs  , lhs.y * rhs  ); }
			friend Coord operator /(Coord lhs, int rhs)   { return Coord(lhs.x / rhs  , lhs.y / rhs  ); }
		};

		constexpr static uint32_t const FNV_offset_basis32 = 2166136261U;
		constexpr static uint32_t const FNV_prime32 = 16777619U;

		// Conversion to/from milli pixels
		constexpr static int mpx(int pixels) { return pixels * 1000; }
		constexpr static int px(int millipx) { return millipx / 1000; }

		#pragma region Sprites
		// DotFactory Settings:
		//  RowMajor_1x8, LsbFirst, no flip/rotate
		static SpriteR sprite_ship()
		{
			static uint8_t const data[] = { 0xF0, 0x60, 0x70, 0xF8, 0xFF, 0xF8, 0x70, 0x60, 0xF0, };
			return SpriteR(data, 9, 8);
		}
		static SpriteR sprite_alien1()
		{
			static uint8_t const data[] = { 0x0E, 0x17, 0x1D, 0x07, 0x1D, 0x17, 0x0E, };
			return SpriteR(&data[0], 7, 5);
		}
		static SpriteR sprite_alien2()
		{
			static uint8_t const data[] = { 0x0E, 0x1B, 0x0F, 0x1B, 0x0F, 0x1B, 0x0E, };
			return SpriteR(&data[0], 7, 5);
		}
		static SpriteR sprite_alien3()
		{
			static uint8_t const data[] = { 0x17, 0x0E, 0x0A, 0x1F, 0x0A, 0x0E, 0x17, };
			return SpriteR(&data[0], 7, 5);
		}
		static SpriteR sprite_alien4()
		{
			static uint8_t const data[] = { 0x07, 0x0A, 0x1D, 0x07, 0x1D, 0x0A, 0x07, };
			return SpriteR(&data[0], 7, 5);
		}
		static SpriteR sprite_alien5()
		{
			static uint8_t const data[] = { 0x0A, 0x07, 0x1D, 0x07, 0x1D, 0x07, 0x0A, };
			return SpriteR(&data[0], 7, 5);
		}
		static SpriteR sprite_bunker()
		{
			static uint8_t const data[] = { 0xFC, 0xFE, 0xFE, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0xFE, 0xFE, 0xFC, };
			return SpriteR(&data[0], 11, 8);
		}
		static SpriteR sprite_bomb()
		{
			static uint8_t const data[] = { 0x0D, 0x1E, 0x0D, };
			return SpriteR(&data[0], 3, 5);
		}
		static SpriteR sprite_bullet()
		{
			static uint8_t const data[] = { 0x07, };
			return SpriteR(&data[0], 1, 3);
		}
		static SpriteR sprite_null()
		{
			return SpriteR(nullptr, 0, 0);
		}
		static SpriteR sprite_explode1()
		{
			static uint8_t const data[] = { 0x08, 0x41, 0x2A, 0x1C, 0x7F, 0x1C, 0x2A, 0x41, 0x08, };
			return SpriteR(&data[0], 9, 7);
		}
		static SpriteR sprite_explode2()
		{
			static uint8_t const data[] =
			{
				0x40, 0x08, 0x40, 0x0A, 0x50, 0x04, 0x19, 0x04, 0x50, 0x0A, 0x40, 0x08, 0x40,
				0x00, 0x02, 0x00, 0x0A, 0x01, 0x04, 0x13, 0x04, 0x01, 0x0A, 0x00, 0x02, 0x00,
			};
			return SpriteR(&data[0], 13, 13);
		}
		static SpriteR sprite_score()
		{
			static uint8_t const data[] =
			{
				0x12, 0x15, 0x15, 0x09, 0x00,
				0x0E, 0x11, 0x11, 0x11, 0x00,
				0x0E, 0x11, 0x11, 0x0E, 0x00,
				0x1F, 0x05, 0x0D, 0x12, 0x00,
				0x1F, 0x15, 0x15, 0x11, 0x00,
				0x0A,
			};
			return SpriteR(&data[0], 26, 5);
		}
		static SpriteR sprite_hiscore()
		{
			static uint8_t const data[] =
			{
				0x1F, 0x04,
				0x04, 0x1F,
				0x00, 0x11,
				0x1F, 0x11,
				0x00, 0x0A,
			};
			return SpriteR(&data[0], 10, 5);
		}
		static SpriteR sprite_digit(int n)
		{
			static uint8_t const data[] =
			{
				0x0E, 0x19, 0x13, 0x0E, 0x00,
				0x00, 0x12, 0x1F, 0x10, 0x00,
				0x12, 0x19, 0x15, 0x12, 0x00,
				0x11, 0x15, 0x15, 0x0A, 0x00,
				0x07, 0x04, 0x1F, 0x04, 0x00,
				0x13, 0x15, 0x15, 0x09, 0x00,
				0x0E, 0x15, 0x15, 0x09, 0x00,
				0x01, 0x11, 0x0D, 0x03, 0x00,
				0x0A, 0x15, 0x15, 0x0A, 0x00,
				0x02, 0x15, 0x15, 0x0E, 0x00,
			};
			return SpriteR(&data[n*5], 5, 5);
		}
		#pragma endregion

		enum class EEntityState
		{
			Dead = 0,
			Exploding2 = 1,
			Exploding1 = 2,
			Alive = 3,
		};

		// Interface for all single entity game objects
		struct Entity
		{
			virtual ~Entity() {}

			// Screen position (in milli-pixels)
			virtual Coord Position() const = 0;
			
			// The sprite to draw for this entity
			virtual SpriteR Sprite() const = 0;
		};
		struct Bomb :Entity
		{
			Coord m_pos;
			EEntityState m_state;

			Bomb()
				: m_pos()
				, m_state(EEntityState::Dead)
			{}
			Coord Position() const override
			{
				return m_pos;
			}
			SpriteR Sprite() const override
			{
				switch (m_state)
				{
				default:
				case EEntityState::Dead: return sprite_null();
				case EEntityState::Alive: return sprite_bomb();
				case EEntityState::Exploding1: return sprite_explode1();
				case EEntityState::Exploding2: return sprite_explode2();
				}
			}
		};
		struct Bullet :Entity
		{
			Coord m_pos;
			EEntityState m_state;

			Bullet()
				: m_pos()
				, m_state(EEntityState::Dead)
			{}
			Coord Position() const override
			{
				return m_pos;
			}
			SpriteR Sprite() const override
			{
				switch (m_state)
				{
				default:
				case EEntityState::Dead: return sprite_null();
				case EEntityState::Alive: return sprite_bullet();
				case EEntityState::Exploding1: return sprite_explode1();
				case EEntityState::Exploding2: return sprite_explode2();
				}
			}
		};
		struct Player :Entity
		{
			Coord m_pos;
			int m_xtarget_mpx; // The target x position
			EEntityState m_state;

			Player()
				: m_pos(mpx(ScreenDimX / 2), mpx(PlayerYPos))
				, m_xtarget_mpx(mpx(ScreenDimX / 2))
				, m_state(EEntityState::Alive)
			{}
			Coord Position() const override
			{
				return m_pos;
			}
			SpriteR Sprite() const override
			{
				switch (m_state)
				{
				default:
				case EEntityState::Dead: return sprite_null();
				case EEntityState::Alive: return sprite_ship();
				case EEntityState::Exploding1: return sprite_explode1();
				case EEntityState::Exploding2: return sprite_explode2();
				}
			}
		};
		struct Bunker :Entity
		{
			Coord m_pos;
			SpriteW<11,8> m_gfx;

			Bunker()
				: m_pos()
				, m_gfx(sprite_bunker())
			{}
			Coord Position() const override
			{
				return m_pos;
			}
			SpriteR Sprite() const override
			{
				return m_gfx;
			}
			SpriteW<11,8>& SpriteW()
			{
				return m_gfx;
			}
		};
		struct Aliens
		{
			constexpr static int Count = AlienRows * AlienCols;
			using StateWord = uint16_t;

			struct Alien :Entity
			{
				Aliens const* m_aliens;
				int m_row, m_col;

				Alien()
					: m_aliens()
					, m_row()
					, m_col()
				{}
				Alien(Aliens const& aliens, int r, int c)
					: m_aliens(&aliens)
					, m_row(r)
					, m_col(c)
				{}
				EEntityState State() const
				{
					return m_aliens->State(m_row, m_col);
				}
				Coord Position() const override
				{
					return m_aliens->Position(m_row, m_col);
				}
				SpriteR Sprite() const override
				{
					switch (m_aliens->State(m_row, m_col))
					{
					default:
					case EEntityState::Dead:       return sprite_null();
					case EEntityState::Exploding1: return sprite_explode1();
					case EEntityState::Exploding2: return sprite_explode2();
					case EEntityState::Alive:
						{
							switch (AlienConfig[m_row].Type)
							{
							case EAlienType::Private:    return sprite_alien1();
							case EAlienType::Lieutenant: return sprite_alien2();
							case EAlienType::Captain:    return sprite_alien3();
							case EAlienType::Major:      return sprite_alien4();
							case EAlienType::General:    return sprite_alien5();
							default: assert(false);      return sprite_null();
							}
						}
					}
				}
				int Value() const
				{
					return AlienConfig[m_row].Value;
				}
			};

			Coord m_pos;                  // The position of the upper/left corner for the block of aliens
			StateWord m_state[AlienCols]; // Bitmask of vertical columns of aliens states. LSB = highest because row0 is the highest. 2-bits per alien
			int m_last_step_ms;           // The clock value when the aliens last moved;
			int m_last_bomb_ms;           // Time since the last bomb was dropped
			int m_direction;              // The direction the aliens are moving in

			Aliens()
				: m_pos(mpx(AlienEdgeMargin), mpx(AlienInitialYPos))
				, m_state()
				, m_last_step_ms()
				, m_last_bomb_ms()
				, m_direction(+1)
			{
				static_assert(AlienRows <= static_cast<int>(sizeof(m_state[0]) * 4), "2-bits per alien means 8 rows max");
				static_assert(static_cast<int>(EEntityState::Alive) == 3, "2-bits per alien state");
				static_assert(static_cast<int>(EEntityState::Dead) == 0, "2-bits per alien state");

				// Set all aliens as alive
				auto mask = static_cast<StateWord>((1 << (2*AlienRows)) - 1);
				for (int c = 0; c != AlienCols; ++c)
					m_state[c] = mask;
			}
			
			// Return the alien at 'r,c'
			Alien AlienAt(int r, int c) const
			{
				return Alien(*this, r, c);
			}

			// Return the state of the alien at 'r,c'
			EEntityState State(int r, int c) const
			{
				assert(r >= 0 && r < AlienRows);
				assert(c >= 0 && c < AlienCols);
				return static_cast<EEntityState>((m_state[c] >> (r * 2)) & 0x3);
			}

			// Return the position of the alien at 'r,c'
			Coord Position(int r, int c) const
			{
				return Coord(
					m_pos.x + mpx(c * (AlienSizeX + AlienSpaceX)),
					m_pos.y + mpx(r * (AlienSizeY + AlienSpaceY)));
			}

			// True if all aliens are destroyed
			bool AllDead() const
			{
				int c;
				for (c = 0; c != AlienCols && m_state[c] == 0; ++c) {}
				return c == AlienCols;
			}

			// Advance by one step
			void Advance(bool drop_down_allowed, bool& dropped)
			{
				bool at_edge = false;
				if (m_direction > 0)
				{
					// Get the right most column that contains alive aliens
					int c = AlienCols;
					for (; c-- != 0 && m_state[c] == 0;) {}
					if (c == -1) return; // all dead
					at_edge = px(Position(0, c).x) >= ScreenDimX - AlienEdgeMargin;
				}
				else
				{
					// Get the left most column that contains alive aliens
					int c = -1;
					for (; ++c != AlienCols && m_state[c] == 0;) {}
					if (c == AlienCols) return; // all dead
					at_edge = px(Position(0, c).x) <= AlienEdgeMargin;
				}

				// Advance Y if at the edge
				if (at_edge)
				{
					if (drop_down_allowed)
					{
						m_pos.y += mpx(AlienAdvanceY);
						dropped = true;
					}
					m_direction = -m_direction;
				}
				else
				{
					m_pos.x += mpx(m_direction * AlienAdvanceX);
				}
			}

			// True if column 'c' contains an alive alien
			bool IsAliveColumn(int c) const
			{
				// This ((column & 0b1010) >> 1) & 0x0101 will only be non-zero if there is a 0b11 sequence in 'column'
				return (((m_state[c] & 0xAAAA) >> 1) & 0x5555) != 0;
			}

			// Return the number of columns containing alive aliens
			int AliveColumnsCount() const
			{
				auto count = 0;
				for (int c = 0; c != AlienCols; ++c)
					count += IsAliveColumn(c);

				return count;
			}

			// Return the n'th column index containing an alive alien
			int AliveColumn(int n) const
			{
				auto alive_count = AliveColumnsCount();
				assert(alive_count != 0); // All dead

				auto column = 0;
				for (n %= alive_count; ; --n, ++column)
				{
					for (; !IsAliveColumn(column); ++column) {}
					if (n == 0) break;
				}
				return column;
			}

			// Return the position of one of the lowest alive aliens
			Coord LowestPosition() const
			{
				int C = -1, R = -1;
				for (int c = 0; c != AlienCols; ++c)
				{
					for (int r = AlienRows; r-- != 0 && r > R; )
					{
						if (((m_state[c] >> (2 * r)) & 3) != 3) continue;
						R = r;
						C = c;
					}
				}
				return Position(R, C);
			}

			// Drop a bomb from the lowest alive alien in column 'col'
			Bomb DropBomb(int col) const
			{
				Bomb bomb;

				// Get the position of the lowest alive alien in 'col'
				int row = AlienRows;
				for (; row-- != 0 && ((m_state[col] >> (2*row)) & 3) != 3;) {}
				if (row == -1)
					return bomb; // All dead in this column

				bomb.m_state = EEntityState::Alive;
				bomb.m_pos.x = m_pos.x + mpx(col * (AlienSizeX + AlienSpaceX));
				bomb.m_pos.y = m_pos.y + mpx(row * (AlienSizeY + AlienSpaceY) + AlienSizeY / 2);
				return bomb;
			}

			// Step exploding aliens towards 'Dead'
			void UpdateStates()
			{
				for (int c = 0; c != AlienCols; ++c)
				{
					if (m_state[c] == 0) continue;
					for (int r = 0; r != AlienRows; ++r)
					{
						auto state = (m_state[c] >> (2 * r)) & 3;
						if (state == 3 || state == 0) continue;
						m_state[c] &= ~(0x3 << (2 * r));
						m_state[c] |= (state - 1) << (2 * r);
					}
				}
			}

			// Destroy 'alien'
			void Kill(Alien const& alien)
			{
				auto state = (m_state[alien.m_col] >> (2 * alien.m_row)) & 3;
				m_state[alien.m_col] &= ~(0x3 << (2 * alien.m_row));
				m_state[alien.m_col] |= static_cast<StateWord>((state - 1) << (2 * alien.m_row));
			}

			// Hit test 'obj' against the aliens
			bool HitTest(Entity const& obj, Alien* out)
			{
				auto const& s = obj.Sprite();
				auto pos = obj.Position();
				auto sw = s.m_dimx;
				auto sh = s.m_dimy;

				// 'obj's bounding box relative to the grid of aliens
				auto xmin = px(pos.x - m_pos.x) - sw/2;
				auto ymin = px(pos.y - m_pos.y) - sh/2;
				auto xmax = xmin + sw;
				auto ymax = ymin + sh;

				constexpr int CellW = (AlienSizeX + AlienSpaceX);
				constexpr int CellH = (AlienSizeY + AlienSpaceY);

				// The range of columns overlapped (inclusive)
				auto col_beg = (xmin / CellW) + (2 * (xmin % CellW) > AlienSizeX);
				auto col_end = (xmax / CellW) + (2 * (xmax % CellW) >= 2 * AlienSpaceX + AlienSizeX);
				if (col_end < 0 || col_beg >= AlienCols || col_beg > col_end) return false;

				// The range of rows overlapped (inclusive)
				auto row_beg = (ymin / CellH) + (2 * (ymin % CellH) > AlienSizeY);
				auto row_end = (ymax / CellH) + (2 * (ymax % CellH) >= 2 * AlienSpaceY + AlienSizeY);
				if (row_end < 0 || row_beg >= AlienRows || row_beg > row_end) return false;

				// Clamp to the valid range
				if (col_beg < 0) col_beg = 0;
				if (col_end >= AlienCols) col_end = AlienCols - 1;
				if (row_beg < 0) row_beg = 0;
				if (row_end >= AlienRows) row_end = AlienRows - 1;

				// Hit test against each potentially overlapping alien
				for (int r = row_end; r >= row_beg; --r)
				{
					for (int c = col_beg; c <= col_end; ++c)
					{
						auto const& alien = AlienAt(r, c);
						if (alien.State() != EEntityState::Alive)
							continue;

						if (CollisionTest(alien, obj))
						{
							if (out) *out = alien;
							return true;
						}
					}
				}
				return false;
			}
		};

	private:

		// Game state machine states
		enum class EState
		{
			// Reset data ready for a new game
			StartNewGame,

			// Reset data for the next level
			StartNewLevel,

			// Wait for intro sounds etc to finish before starting user interactive game play
			StartDelay,

			// Main 'playing' state for the game
			MainRun,

			// Enter this state as soon as collision is detected between the player and a bomb
			PlayerHit,

			// Enter this state as soon as the last alien is destroyed
			AliensDefeated,

			// Enter this state from 'AliensDefeated' after a delay
			LevelComplete,

			// Enter this state from 'PlayerHit' after a delay
			GameEnd,
		};

		// Game state
		ISystem* m_system;
		mutable Screen m_screen;
		Player m_player;
		Aliens m_aliens;
		Bunker m_bunkers[BunkerCount];
		Bomb m_bombs[MaxBombs];
		Bullet m_bullets[MaxBullets];
		UserInputData m_user_input;
		int m_hiscore;
		int m_score;
		int m_level;
		int m_fire_button_issue;
		int m_clock_ms;
		int m_timer_start_ms;
		uint32_t m_rng;
		EState const m_state;

	public:

		SpaceInvaders(ISystem* sys)
			: m_system(sys)
			, m_screen()
			, m_player()
			, m_aliens()
			, m_bunkers()
			, m_bombs()
			, m_bullets()
			, m_user_input()
			, m_hiscore()
			, m_score()
			, m_level()
			, m_clock_ms()
			, m_timer_start_ms()
			, m_rng(FNV_offset_basis32)
			, m_state(EState::StartNewGame)
		{}

		// Reset the game
		void Reset()
		{
			Init();
			ChangeState(EState::StartNewGame);
		}

		// Main loop step
		void Step(int elapsed_ms)
		{
			// Update the game clock and user input
			m_clock_ms += elapsed_ms;
			m_user_input = m_system->UserInput();
			m_hiscore = m_score > m_hiscore ? m_score : m_hiscore;

			// Update the random number generator from the user input
			m_rng = (m_rng << 8) | (m_rng >> 24);
			m_rng = (m_rng ^ static_cast<uint32_t>(m_user_input.JoystickX)) * FNV_prime32;

			// Step the game state machine
			switch (m_state)
			{
			case EState::StartNewGame:
				{
					m_score = 0;
					m_level = 0;
					ChangeState(EState::StartNewLevel);
					break;
				}
			case EState::StartNewLevel:
				{
					++m_level;
					Init();
					m_system->PlaySound(ESound::LevelStart);
					ChangeState(EState::StartDelay);
					break;
				}
			case EState::StartDelay:
				{
					if (m_clock_ms - m_timer_start_ms < StartGameDelayMS) break;
					m_aliens.m_last_step_ms = m_clock_ms;
					ChangeState(EState::MainRun);
					break;
				}
			case EState::MainRun:
			case EState::PlayerHit:
			case EState::AliensDefeated:
				{
					// Update the player
					UpdatePlayer(elapsed_ms);

					// Advance the aliens
					UpdateAliens();

					// Advance the bullet
					UpdateBullets(elapsed_ms);

					// Advance bombs
					UpdateBombs(elapsed_ms);

					// Leave this state after a delay
					if (m_state == EState::AliensDefeated && m_clock_ms - m_timer_start_ms > EndLevelDelayMS)
					{
						ChangeState(EState::LevelComplete);
					}
					if (m_state == EState::PlayerHit && m_clock_ms - m_timer_start_ms > EndLevelDelayMS)
					{
						m_system->PlaySound(ESound::GameOver);
						ChangeState(EState::GameEnd);
					}

					break;
				}
			case EState::LevelComplete:
				{
					ChangeState(EState::StartNewLevel);
					break;
				}
			case EState::GameEnd:
				{
					if (m_user_input.FireButton)
						ChangeState(EState::StartNewGame);
					break;
				}
			}
		}

		// Access the display buffer
		Screen const& Display() const
		{
			// Reset the display buffer
			m_screen.Clear();

			// Draw the score
			{
				int x = 1;
				auto const& s = sprite_score();
				m_screen.Draw(s, x, 1);
				x += s.m_dimx + 2;

				char score[16] = {};
				snprintf(&score[0], sizeof(score), "%d", m_score);
				for (char const* p = &score[0]; *p != '\0'; ++p)
				{
					auto const& d = sprite_digit(*p - '0');
					m_screen.Draw(d, x, 1);
					x += d.m_dimx;
				}
			}
			{
				int x = ScreenDimX / 2;
				auto const& s = sprite_hiscore();
				m_screen.Draw(s, x, 1);
				x += s.m_dimx + 2;

				char score[16] = {};
				snprintf(&score[0], sizeof(score), "%d", m_hiscore);
				for (char const* p = &score[0]; *p != '\0'; ++p)
				{
					auto const& d = sprite_digit(*p - '0');
					m_screen.Draw(d, x, 1);
					x += d.m_dimx;
				}
			}

			// Draw the player
			Draw(m_player);

			// Draw the aliens
			for (int r = 0; r != AlienRows; ++r)
			{
				for (int c = 0; c != AlienCols; ++c)
				{
					auto const& alien = m_aliens.AlienAt(r, c);
					Draw(alien);
				}
			}

			// Draw the bunkers
			for (int b = 0; b != BunkerCount; ++b)
			{
				auto const& bunker = m_bunkers[b];
				Draw(bunker);
			}

			// Draw any bombs
			for (int b = 0; b != MaxBombs; ++b)
			{
				auto const& bomb = m_bombs[b];
				if (bomb.m_state != EEntityState::Dead)
					Draw(bomb);
			}

			// Draw the bullet
			for (int b = 0; b != MaxBullets; ++b)
			{
				auto const& bullet = m_bullets[b];
				if (bullet.m_state != EEntityState::Dead)
					Draw(bullet);
			}

			return m_screen;
		}

	private:

		// Handle changing state machine state
		void ChangeState(EState new_state)
		{
			// Some states start a timer
			if (new_state == EState::StartDelay ||
				new_state == EState::PlayerHit || 
				new_state == EState::AliensDefeated)
				m_timer_start_ms = m_clock_ms;

			// Change the state in one place
			const_cast<EState&>(m_state) = new_state;
		}

		// Set up to start a new game
		void Init()
		{
			// Initialise the player 
			m_player = Player();

			// Initialise the aliens
			m_aliens = Aliens();

			// Initialise the bunkers
			for (int b = 0; b != BunkerCount; ++b)
			{
				auto& bunker = m_bunkers[b] = Bunker{};

				// Space the bunkers evenly across the width of the screen
				bunker.m_pos.x = mpx(ScreenDimX * (b + 1) / (BunkerCount + 1));
				bunker.m_pos.y = mpx(BunkerYPos);
			}

			// Reset all bombs/bullets
			for (int b = 0; b != MaxBombs; ++b)
				m_bombs[b].m_state = EEntityState::Dead;
			for (int b = 0; b != MaxBullets; ++b)
				m_bullets[b].m_state = EEntityState::Dead;
		}

		// Draw 'obj' into 'm_screen'
		void Draw(Entity const& obj) const
		{
			auto const& s = obj.Sprite();
			auto x = px(obj.Position().x) - s.m_dimx / 2;
			auto y = px(obj.Position().y) - s.m_dimy / 2;
			m_screen.Draw(s, x, y);
		}

		// Advance the player
		void UpdatePlayer(int elapsed_ms)
		{
			switch (m_player.m_state)
			{
			default:
			case EEntityState::Dead:
				break;
			case EEntityState::Alive:
				{
					// Find the allowed range for the player x position
					auto const& sprite = sprite_ship();
					int XMin = 5 + sprite.m_dimx / 2;
					int XMax = ScreenDimX - XMin;

					// Find the target x position based on the joystick
					auto xtarget = ScreenDimX * (m_user_input.JoystickX + UserInputData::AxisMaxAbs) / (2 * UserInputData::AxisMaxAbs);
					xtarget = xtarget > XMin ? xtarget : XMin;
					xtarget = xtarget < XMax ? xtarget : XMax;
					m_player.m_xtarget_mpx = mpx(xtarget);

					// Determine how far the player can move within 'elapsed_ms'
					auto max_dist_mpx = elapsed_ms * PlayerMaxSpeed; // Note: px/sec == milli_px/msec

					// Change the player position
					auto sign = m_player.m_xtarget_mpx >= m_player.m_pos.x ? +1 : -1;
					auto dist_mpx = abs(m_player.m_xtarget_mpx - m_player.m_pos.x);
					if (dist_mpx > max_dist_mpx) dist_mpx = max_dist_mpx;
					m_player.m_pos.x += sign * dist_mpx;

					// If the fire button is down, see if the player can shoot
					if (m_user_input.FireButton)
					{
						int b = 0;
						for (; b != MaxBullets && m_bullets[b].m_state != EEntityState::Dead; ++b) {}
						if (b == MaxBullets) // max bullet count reached
							return;

						// Create a bullet
						auto& bullet = m_bullets[b];
						bullet.m_pos = m_player.m_pos;
						bullet.m_state = EEntityState::Alive;
						m_system->PlaySound(ESound::PlayerShoot);
					}
					break;
				}
			case EEntityState::Exploding1:
			case EEntityState::Exploding2:
				m_player.m_state = static_cast<EEntityState>(static_cast<int>(m_player.m_state) - 1);
				break;
			}
		}

		// Advance the aliens
		void UpdateAliens()
		{
			// Advance alien positions
			for (;;)
			{
				// Lerp the step period from the Y position
				auto yrange = AlienFinalYPos - AlienInitialYPos;
				auto dperiod = AlienFinalStepPeriodMS - AlienInitialStepPeriodMS;
				auto dy = (px(m_aliens.m_pos.y) - AlienInitialYPos) + (m_level - 1);
				auto step_period_ms = AlienInitialStepPeriodMS + dperiod * dy / yrange;
				if (step_period_ms < AlienFinalStepPeriodMS)
					step_period_ms = AlienFinalStepPeriodMS;

				// Not time for a step yet?
				if (m_clock_ms - m_aliens.m_last_step_ms < step_period_ms) break;
				m_aliens.m_last_step_ms += step_period_ms;

				// Aliens only move while the player is alive
				bool dropped = false;
				if (m_player.m_state == EEntityState::Alive)
				{
					m_aliens.Advance(true, dropped);
					m_system->PlaySound(ESound::AlienAdvance);
				}

				// If the lowest alien reaches the final Y position, then game over
				if (dropped && px(m_aliens.LowestPosition().y) > AlienFinalYPos)
				{
					m_player.m_state = EEntityState::Exploding1;
					m_system->PlaySound(ESound::PlayerDestroyed);
					ChangeState(EState::PlayerHit);
					break;
				}
			}

			// Update alien states
			m_aliens.UpdateStates();

			// Drop a bomb randomly within the bomb period if the player is alive
			if (m_player.m_state == EEntityState::Alive &&
				RandEvent(100 * (m_clock_ms - m_aliens.m_last_bomb_ms) / BombPeriodMS) &&
				m_aliens.AliveColumnsCount() != 0)
			{
				int b = 0;
				for (; b != MaxBombs && m_bombs[b].m_state != EEntityState::Dead; ++b) {}
				if (b == MaxBombs) // max bomb count reached
					return;

				// Choose which alien to drop the bomb from
				auto col = m_aliens.AliveColumn(Rand(16));
				m_bombs[b] = m_aliens.DropBomb(col);
				m_aliens.m_last_bomb_ms = m_clock_ms;
				m_system->PlaySound(ESound::AlienBombDrop);
			}
		}

		// Advance bullets
		void UpdateBullets(int elapsed_ms)
		{
			// Advance the bullet positions
			for (int b = 0; b != MaxBullets; ++b)
			{
				auto& bullet = m_bullets[b];
				switch (bullet.m_state)
				{
				default: continue;
				case EEntityState::Alive:
					{
						// The distance travelled
						auto dist_mpx = elapsed_ms * BulletSpeed; // px/sec == mpx/msec
						bullet.m_pos.y -= dist_mpx;

						if (px(bullet.m_pos.y) < -bullet.Sprite().m_dimy / 2)
							bullet.m_state = EEntityState::Dead;

						break;
					}
				case EEntityState::Exploding1:
				case EEntityState::Exploding2:
					{
						bullet.m_state = static_cast<EEntityState>(static_cast<int>(bullet.m_state) - 1);
						break;
					}
				}
			}

			// Look for collisions
			for (int b = 0; b != MaxBullets; ++b)
			{
				auto& bullet = m_bullets[b];

				// Bullet vs. Alien
				if (bullet.m_state == EEntityState::Alive)
				{
					Aliens::Alien alien;
					if (m_aliens.HitTest(bullet, &alien))
					{
						m_score += alien.Value();
						m_aliens.Kill(alien);
						m_system->PlaySound(ESound::AlienDestroyed);
						bullet.m_state = EEntityState::Exploding1;
					}
				}

				// Bullet vs. Bomb
				if (bullet.m_state == EEntityState::Alive)
				{
					for (int m = 0; m != MaxBombs; ++m)
					{
						auto& bomb = m_bombs[m];
						if (bomb.m_state == EEntityState::Alive && CollisionTest(bomb, bullet))
						{
							m_score += BombValue;
							bomb.m_state = EEntityState::Exploding1;
							bullet.m_state = EEntityState::Exploding1;
							m_system->PlaySound(ESound::BombDestroyed);
							break;
						}
					}
				}

				// Bullet vs. Bunker
				if (bullet.m_state == EEntityState::Alive)
				{
					for (int k = 0; k != BunkerCount; ++k)
					{
						auto& bunker = m_bunkers[k];
						if (CollisionTest(bunker, bullet))
						{
							// Eat some bunker. Use 'vec.y+1' to increase the penetration of the bomb into the bunker
							bullet.m_state = EEntityState::Exploding1;
							m_system->PlaySound(ESound::BunkerDamaged);
							auto vec = RelativePosition(bunker, bullet);
							onebit::Combine(bunker.SpriteW(), bullet.Sprite(), vec.x, vec.y + 1, [](auto& lhs, auto const&, int b, int x, auto block)
							{
								lhs.Block(b, x) &= ~block;
								return false;
							});
							break;
						}
					}
				}
			}

			// If that was the last one
			if (m_aliens.AllDead())
			{
				m_system->PlaySound(ESound::LevelCompleted);
				ChangeState(EState::LevelComplete);
			}
		}

		// Advance bombs
		void UpdateBombs(int elapsed_ms)
		{
			// Advance the bomb positions
			for (int b = 0; b != MaxBombs; ++b)
			{
				auto& bomb = m_bombs[b];
				switch (bomb.m_state)
				{
				default: continue;
				case EEntityState::Alive:
					{
						// The distance travelled
						auto dist_mpx = elapsed_ms * BombSpeed; // px/src == mpx/msec
						bomb.m_pos.y += dist_mpx;

						if (px(bomb.m_pos.y) > ScreenDimY + bomb.Sprite().m_dimy / 2)
							bomb.m_state = EEntityState::Dead;
						break;
					}
				case EEntityState::Exploding1:
				case EEntityState::Exploding2:
					{
						bomb.m_state = static_cast<EEntityState>(static_cast<int>(bomb.m_state) - 1);
						break;
					}
				}
			}

			// Look for collisions
			for (int b = 0; b != MaxBombs; ++b)
			{
				auto& bomb = m_bombs[b];

				// Bomb vs. Player
				if (bomb.m_state == EEntityState::Alive)
				{
					if (CollisionTest(m_player, bomb))
					{
						bomb.m_state = EEntityState::Exploding1;
						m_player.m_state = EEntityState::Exploding1;
						m_system->PlaySound(ESound::PlayerDestroyed);
						ChangeState(EState::PlayerHit);
					}
				}

				// Bomb vs. Bunker
				if (bomb.m_state == EEntityState::Alive)
				{
					for (int k = 0; k != BunkerCount; ++k)
					{
						auto& bunker = m_bunkers[k];
						if (CollisionTest(bunker, bomb))
						{
							// Eat some bunker. Use 'vec.y+1' to increase the penetration of the bomb into the bunker
							bomb.m_state = EEntityState::Exploding1;
							m_system->PlaySound(ESound::BunkerDamaged);
							auto vec = RelativePosition(bunker, bomb);
							onebit::Combine(bunker.SpriteW(), bomb.Sprite(), vec.x, vec.y + 1, [](auto& lhs, auto const&, int b, int x, auto block)
							{
								lhs.Block(b, x) &= ~block;
								return false;
							});
							break;
						}
					}
				}
			}
		}

		// Get 'obj1' relative to 'obj0'
		static Coord RelativePosition(Entity const& obj0, Entity const& obj1)
		{
			auto const& s0 = obj0.Sprite();
			auto const& s1 = obj1.Sprite();
			auto dx = s0.m_dimx / 2 + px(obj1.Position().x - obj0.Position().x) - s1.m_dimx / 2;
			auto dy = s0.m_dimy / 2 + px(obj1.Position().y - obj0.Position().y) - s1.m_dimy / 2;
			return Coord(dx, dy);
		}

		// Collision test between two entities
		static bool CollisionTest(Entity const& obj0, Entity const& obj1)
		{
			auto const& s0 = obj0.Sprite();
			auto const& s1 = obj1.Sprite();
			auto vec = RelativePosition(obj0, obj1);
			return onebit::Combine(s0, s1, vec.x, vec.y, [](auto& lhs, auto&, int b, int x, auto block) { return (lhs.Block(b, x) & block) != 0; });
		}

		// Random number 
		int Rand(int max) const
		{
			return m_rng % max;
		}
		bool RandEvent(int percent_chance) const
		{
			auto n = static_cast<int>(m_rng & 0xFF);
			return n * 100 <= percent_chance * 0xFF;
		}

		// Dump the current display to a text file
		void DumpToFile() const
		{
			Display().DumpToFile("P:\\dump\\space_invaders.txt");
		}
	};
}
