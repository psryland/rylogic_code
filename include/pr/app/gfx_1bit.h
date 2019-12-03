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

namespace pr::gfx_1bit
{
	// A 1-Bit sprite
	template <typename Word = uint32_t>
	struct Sprite
	{
		// Notes:
		//  - A sprite has a maximum high equal to the word size
		//  - The word size does not have to match the screen word size
		constexpr static int MaxHeight = sizeof(Word) * 8;

		int m_dimx, m_dimy;
		Word const* m_data;

		Sprite(Word const* data, int dimx, int dimy)
			:m_dimx(dimx)
			, m_dimy(dimy)
			, m_data(data)
		{
			assert(dimy <= MaxHeight);
		}
		template <int Sz> Sprite(Word const (&data)[Sz])
			:Sprite(&data[0], Sz, MaxHeight)
		{}

		// Checked access to 'm_data'
		Word buf(int x) const
		{
			assert(x >= 0 && x < m_dimx);
			return m_data[x];
		}
		Word& buf(int x)
		{
			assert(x >= 0 && x < m_dimx);
			return m_data[x];
		}

		// Pixel state at (x,y)
		bool operator()(int x, int y) const
		{
			if (x < 0 || x >= m_dimx) return false;
			if (y < 0 || y >= m_dimy) return false;
			return buf(x) & (1 << y);
		}
	};

	// An editable 1-Bit sprite
	template <int MaxWidth, typename Word = uint32_t>
	struct EditableSprite :Sprite<Word>
	{
		// Notes:
		//  - An editable sprite makes a local copy of the sprite data so it can be edited.
		constexpr static int MaxWidth = MaxWidth;

		Word m_buf[MaxWidth];

		EditableSprite(Word const* data, int dimx, int dimy)
			:Sprite<Word>(&m_buf[0], dimx, dimy)
		{
			assert(dimx <= MaxWidth);
			assert(dimy <= MaxHeight);
			memcpy(&m_buf[0], data, m_dimx * sizeof(Word));
		}
		EditableSprite(Sprite<Word> const& sprite)
			:Sprite<Word>(sprite.m_data, sprite.m_dimx, sprite.m_dimy)
		{}
	};

	// A 1-bit screen buffer
	template <int XDim, int YDim, typename Word = uint32_t>
	struct Screen
	{
		// Notes:
		//  - 1-Bit graphics in-memory screen buffer.
		//  - The LSB of m_buf[0][0] is the upper left corner. X increases to the right, Y increases going down.
		//  - Drawing out-of-bounds is silently ignored
		constexpr static int Page = sizeof(Word) * 8;
		constexpr static int XDim = XDim;
		constexpr static int YDim = YDim;
		static_assert((YDim % Page) == 0);

		// Screen buffer (1 bit) XDim x YDim
		Word m_buf[YDim / Page][XDim];

		Screen()
			:m_buf()
		{}

		// Checked access to 'm_buf'
		Word buf(int page, int x) const
		{
			assert(page >= 0 && page < YDim / Page);
			assert(x >= 0 && x < XDim);
			return m_buf[page][x];
		}
		Word& buf(int page, int x)
		{
			assert(page >= 0 && page < YDim / Page);
			assert(x >= 0 && x < XDim);
			return m_buf[page][x];
		}

		// Pixel state at (x,y)
		bool operator()(int x, int y) const
		{
			if (x < 0 || x >= XDim) return false;
			if (y < 0 || y >= YDim) return false;
			return buf(y / Page, x) & (1 << (y % Page));
		}

		// Clear the screen
		void Clear(int value = 0)
		{
			memset(&m_buf[0], value, sizeof(m_buf));
		}
		void Clear(int X, int Y, int W, int H)
		{
			// Clip the clear rectange to the screen
			if (X + W > XDim) { W = XDim - X; }
			if (Y + H > YDim) { H = YDim - Y; }
			if (X < 0) { W += X; X = 0; }
			if (Y < 0) { H += H; H = 0; }
			if (W <= 0 || H <= 0) return;

			// Fill the rectange with 'zero'
			auto pbeg = (Y) / Page;
			auto pend = (Y + H) / Page;
			for (int p = pbeg; p <= pend; ++p)
			{
				Word mask = 0;
				if (p == pbeg) mask |= MaskLo(Y % Page);
				if (p == pend) mask |= MaskHi((Y + H) % Page);
				for (int x = X; x != X + W; ++x)
					buf(p, x) &= mask;
			}
		}

		// Draw a sprite on-screen
		template <typename Sprite>
		void Draw(Sprite const& sprite, int X, int Y)
		{
			// Clip the sprite to the screen
			int sx = 0, sy = 0;
			int sw = sprite.m_dimx, sh = sprite.m_dimy;
			if (X + sw > XDim) { sw = XDim - X; }
			if (Y + sh > YDim) { sh = YDim - Y; }
			if (X < 0) { sx -= X; sw += X; X = 0; }
			if (Y < 0) { sy -= Y; sh += Y; Y = 0; }
			if (sw <= 0 || sh <= 0)
				return;

			// Blit the sprite into the screen buffer.
			auto p = Y / Page;
			for (int i = X, x = sx; x != sx + sw; ++x, ++i)
				buf(p, i) |= static_cast<Word>((sprite.buf(x) >> sy) << (Y % Page));
			for (++p; p * Page <= Y + sh; ++p)
				for (int i = X, x = sx; x != sx + sw; ++x, ++i)
					buf(p, i) |= static_cast<Word>((sprite.buf(x) >> sy) >> (p * Page - Y));
		}

		// Write the screen to file
		void DumpToFile()
		{
			std::ofstream out("P://dump//space_invaders.txt");
			for (int y = 0; y != YDim; ++y)
			{
				for (int x = 0; x != XDim; ++x)
					out.write((*this)(x, y) ? "X" : ".", 1);

				out.write("\n", 1);
			}
		}

		// Word mask helpers
		static constexpr Word MaskLo(int i)
		{
			// e.g.  0b00001111
			assert(i < Page);
			return static_cast<Word>((1ULL << i) - 1);
		}
		static constexpr Word MaskHi(int i)
		{
			// e.g.  0b11110000
			assert(i < Page);
			return static_cast<Word>(~0ULL << i);
		}
	};

	// Pixel resolution collision detection
	template <typename Sprite1, typename Sprite2>
	bool CollisionTest(Sprite1 const& lhs, int x0, int y0, Sprite2 const& rhs, int x1, int y1)
	{
		return false;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::gfx_1bit
{
	PRUnitTest(SpaceInvadersTests)
	{
		static const uint32_t data_ship[] =
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
		static Sprite<uint32_t> ship(data_ship);

		static uint8_t const data_alien[] =
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
		static Sprite<uint8_t> alien(&data_alien[0], 8, 4);

		Screen<128, 64, uint8_t> screen;
		screen.Clear();
		screen.DumpToFile();

		screen.Draw(ship, 10, 10);

		screen.Draw(alien, 10, 4);
		screen.Draw(alien, 20, 4);
		screen.Draw(alien, 30, 4);
		screen.Draw(alien, 40, 4);

		screen.DumpToFile();
	}
}
#endif
