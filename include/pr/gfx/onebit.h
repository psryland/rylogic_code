//*****************************************************************************************
// Space Invaders
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************

#pragma once

#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <algorithm>
#include <cassert>

namespace pr::onebit
{
	// A 1-Bit readonly bitmap
	template <typename TWord = uint8_t>
	struct BitmapR
	{
		// Notes:
		//  - The required memory layout of the bitmap is:
		//    +--+--+--+--+--+
		//    |W1|W2|W3|W4|W5|
		//    +--+--+--+--+--+
		//    |W6|W7|W8|W9| ...
		//    +--+--+--+--+
		//    ...
		//    where a 'Word' represents a '1 x WordSize' column of pixels (called a 'Block')
		//  - The LSB of 'W1' is the top/left corner of the image.
		//  - A BitmapR is readonly, so does not need to contain its data. The 'R' stands for readonly, or reference,.. can't decide
		//  - A Bitmap contains a local buffer so it can be edited.
		//  - Drawing/Accessing out-of-bounds is silently ignored.
		//  - It's too complicated allowing mixed word size bitmaps. Only use bitmaps of the same word size together

		using Word = TWord;
		constexpr static int WordSize = sizeof(Word) * 8;

		Word const* m_data;
		int m_dimx, m_dimy;
		int m_stride;

		BitmapR(Word const* data, int dimx, int dimy, int stride = 0)
			: m_data(data)
			, m_dimx(dimx)
			, m_dimy(dimy)
			, m_stride(stride ? stride : dimx)
		{}

		// Access a block of pixel data
		Word Block(int b, int x) const
		{
			assert(b >= 0 && b < BlockIndex(m_dimy + WordSize - 1));
			assert(x >= 0 && x < m_dimx);
			return m_data[b * m_stride + x];
		}

		// Pixel state at (x,y)
		bool operator()(int x, int y) const
		{
			if (x < 0 || x >= m_dimx) return false;
			if (y < 0 || y >= m_dimy) return false;
			return Block(BlockIndex(y), x) & (1 << (y % WordSize));
		}

		// Convert a Y coordinate to a block index
		constexpr static int BlockIndex(int y)
		{
			// Handle negative values correctly. e.g. [-WordSize,0) == block -1, [0, WordSize) = block 0
			return y >= 0
				? y / WordSize
				: -~y / WordSize - 1;
		}

		// Write the bitmap to a file
		void DumpToFile(std::filesystem::path const& filepath) const
		{
			std::ofstream out(filepath);
			for (int y = 0; y != m_dimy; ++y)
			{
				for (int x = 0; x != m_dimx; ++x)
				{
					if ((*this)(x, y))
						out.write("#", 1);
					else
						out.write(".", 1);
				}
				out.write("\n", 1);
			}
		}
	};

	// A mutable 1-Bit bitmap
	template <int DimX, int DimY, typename TWord = uint8_t>
	struct Bitmap :BitmapR<TWord>
	{
		using BitmapR = BitmapR<TWord>;
		using Word = typename BitmapR::Word;
		constexpr static int WordSize = sizeof(Word) * 8;

		// The capacity of the bitmap
		constexpr static int DimX = DimX;
		constexpr static int DimY = DimY;
		static_assert(DimX > 0 && DimY > 0);

		// The height of the bitmap in blocks
		constexpr static int BlockHeight = (DimY + WordSize - 1) / WordSize;

		Word m_buf[DimX * BlockHeight];

		Bitmap() noexcept
			:BitmapR(&m_buf[0], DimX, DimY, DimX)
			,m_buf()
		{}
		Bitmap(Word const* data, int dimx, int dimy, int stride = 0) noexcept
			:Bitmap()
		{
			Init(data, dimx, dimy, stride);
		}
		Bitmap(Bitmap const& rhs) noexcept
			:Bitmap(rhs.m_data, rhs.m_dimx, rhs.m_dimy, rhs.m_stride)
		{}
		Bitmap(BitmapR const& rhs) noexcept
			:Bitmap(rhs.m_data, rhs.m_dimx, rhs.m_dimy, rhs.m_stride)
		{}
		Bitmap& operator =(Bitmap const& rhs) noexcept
		{
			if (this == &rhs) return *this;
			m_data = &m_buf[0];
			m_dimx = rhs.m_dimx;
			m_dimy = rhs.m_dimy;
			m_stride = rhs.m_stride;
			memcpy(&m_buf[0], &rhs.m_buf[0], sizeof(m_buf));
			return *this;
		}

		// Populate this bitmap with the given data
		void Init(Word const* data, int dimx, int dimy, int stride = 0)
		{
			stride = stride ? stride : dimx;
			assert(dimx <= stride);
			assert(dimx >= 0 && dimx <= DimX);
			assert(dimy >= 0 && dimy <= DimY);

			if (stride == DimX)
			{
				memcpy(&m_buf[0], &data[0], sizeof(m_buf));
			}
			else
			{
				for (int b = 0; b != BlockHeight; ++b)
					memcpy(&m_buf[b * DimX], &data[b * stride], dimx * sizeof(Word));
			}
			m_dimx = dimx;
			m_dimy = dimy;
			m_stride = stride;
		}

		// Write access to a block of image data
		Word Block(int b, int x) const
		{
			return BitmapR::Block(b, x);
		}
		Word& Block(int b, int x)
		{
			assert(b >= 0 && b < BlockHeight);
			assert(x >= 0 && x < m_dimx);
			return m_buf[b * DimX + x];
		}

		// Clear the image
		void Clear(int value = 0)
		{
			memset(&m_buf[0], value, sizeof(m_buf));
		}
		void Clear(int X, int Y, int W, int H, Word value = 0)
		{
			// Clip the clear rectange to the image
			if (X + W > DimX) { W = DimX - X; }
			if (Y + H > DimY) { H = DimY - Y; }
			if (X < 0) { W += X; X = 0; }
			if (Y < 0) { H += H; H = 0; }
			if (W <= 0 || H <= 0)
				return;

			// Fill the rectange with 'value'
			auto bbeg = BlockIndex(Y + 0);
			auto bend = BlockIndex(Y + H);
			for (int b = bbeg; b <= bend; ++b)
			{
				auto mask = value;
				if (b == bbeg) mask |= MaskLo((Y + 0) % WordSize);
				if (b == bend) mask |= MaskHi((Y + H) % WordSize);
				for (int x = X; x != X + W; ++x)
					Block(b, x) &= mask;
			}
		}

		// Draw an image into this image
		template <typename Image>
		void Draw(Image const& img, int X, int Y)
		{
			Combine(*this, img, X, Y, [](auto& lhs, auto const&, int b, int x, Word word)
			{
				// Combine bits with OR to print 'img' into this bitmap
				lhs.Block(b, x) |= word;
				return false;
			});
		}

		// Word mask helpers
		constexpr static Word MaskLo(int i)
		{
			// e.g.  0b00001111
			assert(i < WordSize);
			return static_cast<Word>((1ULL << i) - 1);
		}
		constexpr static Word MaskHi(int i)
		{
			// e.g.  0b11110000
			assert(i < WordSize);
			return static_cast<Word>(~0ULL << i);
		}
	};

	// Combine 'lhs' and 'rhs' at (X,Y) relative to 'lhs' using 'op'
	// Op should be callable with signature:
	//   bool op(ImageL& lhs, ImageR& rhs, int block_index, int column_index, Word rhs_block);
	//   'lhs' and 'rhs' are writable if Combine is called with non-const parameters.
	//   'block_index' and 'column_index' are the coordinates in 'lhs'
	//   'rhs_block' is the block from from 'rhs' that coincides with the coordinates in 'lhs'.
	// Returns true if 'op' returns true
	template <typename BitmapL, typename BitmapR, typename Op>
	bool Combine(BitmapL&& lhs, BitmapR&& rhs, int X, int Y, Op op)
	{
		using BmpL = std::decay_t<BitmapL>;
		using BmpR = std::decay_t<BitmapR>;
		using Word = typename BmpL::Word;
		
		static_assert(BmpL::WordSize == BmpR::WordSize, "Bitmaps must have the same Word size");
		constexpr auto BlockIndex = BmpL::BlockIndex;
		constexpr int WordSize = BmpL::WordSize;

		auto ofs0 = Y - BlockIndex(Y) * WordSize; // Offset from a block boundary
		auto ofs1 = ofs0 != 0 ? WordSize - ofs0 : 0; // Compliement of ofs0

		// Clip 'rhs' to the bounds of 'lhs'
		// (sx,sy) is the top/left coord of the clipped region in 'rhs'
		// (sw,sh) is the width/height of the clipped region in 'rhs'
		int sx = 0, sy = 0;
		int sw = rhs.m_dimx, sh = rhs.m_dimy;
		if (X + sw > lhs.m_dimx) { sw = lhs.m_dimx - X; }
		if (Y + sh > lhs.m_dimy) { sh = lhs.m_dimy - Y; }
		if (X < 0) { sx -= X; sw += X; X = 0; }
		if (Y < 0) { sy -= Y; sh += Y; Y = 0; }
		if (sw <= 0 || sh <= 0)
			return false;

		// The inclusive block range in 'lhs' spanned by 'rhs'
		auto bbeg = BlockIndex(Y + 0);
		auto bend = BlockIndex(Y + sh - 1);

		// The inclusive block range in 'rhs' spanned by 'lhs'
		auto Bbeg = BlockIndex(sy + 0);
		auto Bend = BlockIndex(sy + sh - 1);

		// Loop over the blocks that span 'rhs'
		for (int b = bbeg; b <= bend; ++b)
		{
			// Get the block indices in 'rhs' that span rows '(b+0)*WordSize' and '(b+1)*WordSize'
			auto B0 = BlockIndex((b + 0) * WordSize - Y + sy);
			auto B1 = BlockIndex((b + 1) * WordSize - Y + sy);

			// The horizontal range in 'rhs'
			for (int x = sx, xend = sx + sw; x != xend; ++x)
			{
				Word word = 0;

				if (B0 >= Bbeg && B0 <= Bend)
					word |= rhs.Block(B0, x) >> ofs1;
				if (B1 >= Bbeg && B1 <= Bend)
					word |= rhs.Block(B1, x) << ofs0;

				// Apply the combine operation
				if (op(lhs, rhs, b, X + (x - sx), word))
					return true;
			}
		}

		return false;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <cstdint>
namespace pr::onebit
{
	PRUnitTest(OneBitTests)
	{
		uint8_t const cross_data[] =
		{
			0x01, 0x02, //        #      # 
			0x04, 0x08, //      #      #   
			0x10, 0x20, //    #      #     
			0x40, 0x80, //  #      #       
			0x40, 0x20, //  #        #     
			0x10, 0x08, //    #        #   
			0x04, 0x02, //      #        # 
			0x01, 0x40, //        # #      
			0x20, 0x10, //   #        #    
			0x08, 0x04, //     #        #  
			0x02, 0x01, //       #        #
			0x00, 0x01, //                #
			0x02, 0x04, //       #      #  
			0x08, 0x10, //     #      #    
			0x20, 0x40, //   #      #      
		};
		BitmapR<uint8_t> cross(&cross_data[0], 15, 15);
		uint8_t const small_spaceship_data[] =
		{
			0xF0,
			0x60,
			0x70,
			0xF8,
			0xFF,
			0xF8,
			0x70,
			0x60,
			0xF0,
		};
		BitmapR<uint8_t> small_spaceship(&small_spaceship_data[0], 9, 8);
		uint8_t const alien_data[] =
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
		BitmapR<uint8_t> alien(&alien_data[0], 8, 4);
		uint8_t const big_spaceship_data[] =
		{
			0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0xE0,
			0xFE, 0xFF, 0xFF, 0xFE,
			0xE0, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00,
			0x80, 0xF0, 0x80, 0x00,
			0x00, 0x00, 0x80, 0xC0,
			0xE0, 0xF0, 0xF8, 0x7F,
			0xBF, 0xDF, 0xDF, 0xBF,
			0x7F, 0xF8, 0xF0, 0xE0,
			0xC0, 0x80, 0x00, 0x00,
			0x00, 0x80, 0xF0, 0x80,
			0xFF, 0xFF, 0xFF, 0xFC,
			0xFE, 0xFF, 0xFF, 0xFF,
			0xFF, 0xFF, 0xFF, 0xFF,
			0xFF, 0xFF, 0xFF, 0xFF,
			0xFF, 0xFF, 0xFF, 0xFF,
			0xFF, 0xFF, 0xFF, 0xFE,
			0xFC, 0xFF, 0xFF, 0xFF,
			0x07, 0x0F, 0x07, 0x01,
			0x01, 0x03, 0x03, 0x03,
			0x03, 0x03, 0x03, 0x03,
			0x01, 0x01, 0x01, 0x01,
			0x03, 0x03, 0x03, 0x03,
			0x03, 0x03, 0x03, 0x01,
			0x01, 0x07, 0x0F, 0x07,
		};
		BitmapR<uint8_t> big_spaceship(&big_spaceship_data[0], 28, 28);

		{// Clipping
			Bitmap<64, 64, uint8_t> screen;
			screen.Draw(cross, 1, +2);
			screen.Draw(cross, 17, -2);
			screen.Draw(cross, 33, -10);
			screen.Draw(cross, 1, 58);
			screen.Draw(cross, 17, 54);
			screen.Draw(cross, 33, 62);
			screen.Draw(small_spaceship, 50, 10);
			screen.Draw(big_spaceship, 30, 30);
			screen.Draw(alien, 20, 40);
			//screen.DumpToFile("P:\\dump\\sprite.txt");
		}
		{// Collision
			Bitmap<64, 64, uint8_t> screen;
			Bitmap<16, 16, uint8_t> s0(cross);
			Bitmap<16, 16, uint8_t> s1(cross);
			screen.Draw(s0, 10, 10);
			screen.Draw(s1, 20, 18);
			//screen.DumpToFile("P:\\dump\\sprite.txt");

			bool hit;

			hit = Combine(s0, s1, 10, 10, [](auto& lhs, auto&, int b, int x, auto word)
			{
				return (lhs.Block(b, x) & word) != 0;
			});
			PR_CHECK(hit, true);

			hit = Combine(s0, s1, 10, 8, [](auto& lhs, auto&, int b, int x, auto word)
			{
				return (lhs.Block(b, x) & word) != 0;
			});
			PR_CHECK(hit, false);
		}
	}
}
#endif
