//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#include "pr/maths/maths.h"

namespace pr
{
	// Compress a normalised 3-vector into a v2 (almost) losslessly
	inline v2 CompressNorm3(v4 const& norm)
	{
		// Drop the largest component
		auto i = MaxElementIndex3(Abs(norm));
		
		// Save the sign of the dropped component
		auto sign = norm[i] < 0.0f;
		auto w = norm.w != 0.0f;

		v2 result;
		result.x = norm[(i + 1) % 3];
		result.y = norm[(i + 2) % 3];

		// Encode the sign and 'w' value in the LSB
		reinterpret_cast<uint32_t&>(result.x) = SetBits(reinterpret_cast<uint32_t const&>(result.x), 0x1, sign);
		reinterpret_cast<uint32_t&>(result.y) = SetBits(reinterpret_cast<uint32_t const&>(result.y), 0x1, w);

		return result;
	}
	inline v4 DecompressNorm3(v2 const& packed_norm)
	{
		auto sign = AllSet(reinterpret_cast<uint32_t const&>(packed_norm.x), 0x1);
		auto w = AllSet(reinterpret_cast<uint32_t const&>(packed_norm.y), 0x1);
		

		switch (bits)
		{
		case 0:
			{
				auto x = packed_norm;
				auto y = 1.0f;

			}
		case 1:
		case 2:
		default: throw std::runtime_error("Invalid encoded normal")
		}
	}

	// Returns a direction in 5 bits. (Actually a number < 27)
	// Note: this can be converted into 4 bits if sign information isn't needed
	// using: if( idx > 13 ) idx = 26 - idx; Doing so, does not effect the DecompressNormal() function
	struct Norm5bit
	{
		static uint Compress(v4_cref<> norm)
		{
			const float cos_67p5 = 0.382683f;
			uint x = (norm.x > cos_67p5) - (norm.x < -cos_67p5) + 1;
			uint y = (norm.y > cos_67p5) - (norm.y < -cos_67p5) + 1;
			uint z = (norm.z > cos_67p5) - (norm.z < -cos_67p5) + 1;
			return x + y*3 + z*9;
		}
		static v4 Decompress(uint idx, bool renorm = true)
		{
			int x = (idx % 3);
			int y = (idx % 9) / 3;
			int z = (idx / 9);
			return renorm
				? v4::Normal3(x - 1.0f, y - 1.0f, z - 1.0f, 0.0f)
				: v4{x - 1.0f, y - 1.0f, z - 1.0f, 0};
		}
	};

	// Compress a v2 into a maximum number of bits
	template <int MaxBits, typename ReturnType> inline ReturnType PackNormV2(v2_cref<> vec)
	{
		assert(Abs(vec.x) <= 1.0f && Abs(vec.y) <= 1.0f && "Only supports vectors with components in the range -1 to 1");
		auto Bits  = MaxBits / 2;
		auto Scale = (static_cast<ReturnType>(1) << Bits) / 2 - 1;
		auto Mask  = (static_cast<ReturnType>(1) << Bits) - 1;

		auto x = static_cast<ReturnType>((vec.x + 1.0f) * Scale) & Mask;
		auto y = static_cast<ReturnType>((vec.y + 1.0f) * Scale) & Mask;
		return (x << Bits) | y;
	}
	template <int MaxBits, typename PackedType> inline v2 UnpackNormV2(PackedType packed_vec)
	{
		auto Bits  = MaxBits / 2;
		auto Scale = (static_cast<PackedType>(1) << Bits) / 2 - 1;
		auto Mask  = (static_cast<PackedType>(1) << Bits) - 1;

		v2 vec =
		{
			((packed_vec >> Bits) & Mask) / static_cast<float>(Scale) - 1.0f,
			((packed_vec >>    0) & Mask) / static_cast<float>(Scale) - 1.0f,
		};
		return vec;
	}

	// Compress a v4 into a maximum number of bits
	template <int MaxBits, typename ReturnType> inline ReturnType PackNormV4(const v4& vec)
	{
		assert(Abs(vec.x) <= 1.0f && Abs(vec.y) <= 1.0f && Abs(vec.z) <= 1.0f && (Abs(vec.w) == 0.0f || Abs(vec.w) == 1.0f) && "Only supports vectors with components in the range -1 to 1, and w as 0 or 1");

		// Use 1 bit for w and divide the remaining bits by 3
		const ReturnType Bits  = (MaxBits - 1) / 3;
		const ReturnType Scale = (static_cast<ReturnType>(1) << Bits) / 2 - 1;
		const ReturnType Mask  = (static_cast<ReturnType>(1) << Bits) - 1;

		ReturnType x = static_cast<ReturnType>((vec.x + 1.0f) * Scale) & Mask;
		ReturnType y = static_cast<ReturnType>((vec.y + 1.0f) * Scale) & Mask;
		ReturnType z = static_cast<ReturnType>((vec.z + 1.0f) * Scale) & Mask;
		return (x << (2 * Bits + 1)) |
			   (y << (Bits + 1)) |
			   (z << (1)) |
			   (vec.w ? 1 : 0);
	}
	template <int MaxBits, typename PackedType> inline v4 UnpackNormV4(PackedType packed_vec)
	{
		// Use 1 bit for w and divide the remaining bits by 3
		const PackedType Bits  = (MaxBits - 1) / 3;
		const PackedType Scale = (static_cast<PackedType>(1) << Bits) / 2 - 1;
		const PackedType Mask  = (static_cast<PackedType>(1) << Bits) - 1;

		v4 vec =
		{
			((packed_vec >> (2 * Bits + 1)) & Mask) / static_cast<float>(Scale) - 1.0f,
			((packed_vec >> (Bits + 1)) & Mask) / static_cast<float>(Scale) - 1.0f,
			((packed_vec >> (1)) & Mask) / static_cast<float>(Scale) - 1.0f,
			(packed_vec & 0x1) ? (1.0f) : (0.0f)
		};
		return vec;
	}

	namespace impl
	{
		#define CompressQuat32_Mask1   0x3FF       // = (1 << 10) - 1
		#define CompressQuat32_Mask2   0x1FF       // = (1 <<  9) - 1
		#define CompressQuat32_Ofs1    0x1FF
		#define CompressQuat32_Ofs2    0xFF
		#define CompressQuat32_FScale1 723.3710f   // = Scale1 / (2.0 * 0.707106)
		#define CompressQuat32_FScale2 442.5391f   // = Scale2 / (2.0 * 0.577350)

		// Compress a normalised quaternion into 32bits.
		// The algorithm and reasoning is as follows:
		// - We can drop one component because it can be recreated using the knowledge that the quat is normalised.
		//   Dropping the largest component gives the best accuracy at the cost of having to remember which component
		//   was dropped.
		// - Dropping the largest component means the magnitude of the next largest component can be no greater than
		//   1/root(2) (0.707106), the third largest no greater than 1/root(3) (0.577350), and 4th component no greater
		//   than 1/root(4) (0.5).
		// - Proportioning the bits used to store each component with these ratios gives the greatest accuracy.
		// - Since the 3rd and 4th components have roughly the same proportion, use an equal number of bits for these and store
		//   them in order so that the component position does not need to be stored.
		// - Reserve 4 bits for the index of the component that was dropped and the next largest component.
		// - Store the remaining components in 28bits using the ratio: 0.707106 : 0.577350 : 0.577350 -> 10 bits : 9 bits : 9 bits
		// - The final compressed format is:
		//      2bits: index of the dropped component
		//      2bits: index of the second largest component
		//      10bits: the compressed value of the second largest component
		//      9bits: the compressed value of the first component that isn't the dropped component or the second largest
		//      9bits: the compressed value of the second component that isn't the dropped component or the second largest
		// Approximate angular error ~0.27 degrees
		template <typename T> uint32 CompressQuat32(quat const& orientation)
		{
			quat ori = orientation;

			// Choose the largest component
			int largest1 = 0;
			if (Abs(ori[largest1]) < Abs(ori[1])) largest1 = 1;
			if (Abs(ori[largest1]) < Abs(ori[2])) largest1 = 2;
			if (Abs(ori[largest1]) < Abs(ori[3])) largest1 = 3;
			if (ori[largest1] < 0.0f) ori = -ori;    // Ensure the one we drop is positive
			ori[largest1] = 0.0f;

			// Choose the next largest component
			float flargest2;
			int largest2 = 0;
			if (Abs(ori[largest2]) < Abs(ori[1])) largest2 = 1;
			if (Abs(ori[largest2]) < Abs(ori[2])) largest2 = 2;
			if (Abs(ori[largest2]) < Abs(ori[3])) largest2 = 3;
			flargest2 = ori[largest2];
			ori[largest2] = 0.0f;

			// Compress the remaining three components
			int the_big_one = static_cast<int>(flargest2 * CompressQuat32_FScale1) + CompressQuat32_Ofs1;
			assert((the_big_one & CompressQuat32_Mask1) == the_big_one);
			uint32 compressed_quat = (largest1    << 30) |   // index of the largest
									 (largest2    << 28) |   // index of the second largest
									 (the_big_one << 18);    // The compressed value of the second largest
			int shift = 9;
			for (int i = 0; i != 4; ++i)
			{
				if (i == largest1 || i == largest2) continue;

				int compressed_value = static_cast<int>((ori[i] * CompressQuat32_FScale2) + CompressQuat32_Ofs2);
				assert((compressed_value & CompressQuat32_Mask2) == compressed_value);
				compressed_quat |= compressed_value << shift;
				shift = 0;
			}
			return compressed_quat;
		}
		template <typename T> quat DecompressQuat32(uint32 compressed_orientation)
		{
			int largest1 = (compressed_orientation >> 30) & 0x3;
			int largest2 = (compressed_orientation >> 28) & 0x3;

			quat orientation;
			orientation[largest2] = (int((compressed_orientation >> 18) & CompressQuat32_Mask1) - CompressQuat32_Ofs1) / CompressQuat32_FScale1;

			int shift = 9;
			float sq_sum = orientation[largest2] * orientation[largest2];
			for (int i = 0; i != 4; ++i)
			{
				if (i == largest1 || i == largest2) continue;
				orientation[i] = ((int(compressed_orientation >> shift) & CompressQuat32_Mask2) - CompressQuat32_Ofs2) / CompressQuat32_FScale2;
				shift = 0;

				sq_sum += orientation[i] * orientation[i];
			}
			orientation[largest1] = Sqrt(1.0f - sq_sum);
			return orientation;
		}
		#undef CompressQuat32_Mask1
		#undef CompressQuat32_Mask2
		#undef CompressQuat32_Ofs1
		#undef CompressQuat32_Ofs2
		#undef CompressQuat32_FScale1
		#undef CompressQuat32_FScale2
	}

	inline uint32 CompressQuat32(quat const& orientation)         { return impl::CompressQuat32<void>(orientation); }
	inline quat   DecompressQuat32(uint32 compressed_orientation) { return impl::DecompressQuat32<void>(compressed_orientation); }

	// Pack a normalised 32bit float into 4 floats assuming each float is stored using 8bit precision
	// This is mainly used for packing a normalised float value into a Colour32
	inline v4 EncodeNormalisedF32toV4(float value)
	{
		assert(value >= 0 && value <= 1 && "Only supports floats in the range [0,1]");
		v4 const shifts = v4(1.677721e+7f, 6.553599e+4f, 2.559999e+2f, 9.999999e-1f); // 256^3, 256^2, 256, 1
		v4 packed = Frac(value * shifts);
		packed.y -= packed.x / 256.0f;
		packed.z -= packed.y / 256.0f;
		packed.w -= packed.z / 256.0f;
		return packed;
	}
	inline float DecodeNormalisedF32(v4 const& value)
	{
		v4 const shifts = v4(5.960464e-8f, 1.525879e-5f, 3.90625e-3f, 1.0f); // 1/256^3, 1/256^2, 1/256, 1
		return Dot4(value, shifts);
	}
	inline v2 EncodeNormalisedF32toV2(float value)
	{
		assert(value >= 0 && value <= 1 && "Only supports floats in the range [0,1]");
		v2 const shifts = v2(2.559999e2f, 9.999999e-1f);
		v2 packed = Frac(value * shifts);
		packed.y -= packed.x / 256.0f;
		return packed;
	}
	inline float DecodeNormalisedF32(v2 const& value)
	{
		v2 const shifts = v2(3.90625e-3f, 1.0f);
		return Dot2(value, shifts);
	}

	////*****
	//// Compress a normalised quaternion into 32bits.
	//// The algorithm and reasoning is as follows:
	//// - We can drop one component because it can be recreated using the knowledge that the quat is normalised.
	////   Dropping the largest component gives the best accuracy at the cost of having to remember which component
	////   was dropped.
	//// - Dropping the largest component means the magnitude of the next largest component can be no greater than
	////   1/root(2) (0.707106), the third largest no greater than 1/root(3) (0.577350), and 4th component no greater
	////   than 1/root(4) (0.5).
	//// - Proportioning the bits used to store each component with these ratios gives the greatest accuracy.
	//// - Since the 3rd and 4th components have roughly the same proportion, use an equal number of bits for these and store
	////   them in order so that the component position does not need to be stored.
	//// - Reserve 4 bits for the index of the component that was dropped and the next largest component.
	//// - Store the remaining components in 28bits using the ratio: 0.707106 : 0.577350 : 0.577350 -> 10 bits : 9 bits : 9 bits
	//// - The final compressed format is:
	////        2bits: index of the dropped component
	////        2bits: index of the second largest component
	////        10bits: the compressed value of the second largest component
	////        9bits: the compressed value of the first component that isn't the dropped component or the second largest
	////        9bits: the compressed value of the second component that isn't the dropped component or the second largest
	//#define CompressQuat32_Scale1     0x3FF       // = (1 << 10) - 1
	//#define CompressQuat32_Scale2     0x1FF       // = (1 <<  9) - 1
	//#define CompressQuat32_Ofs1           0x1FF
	//#define CompressQuat32_Ofs2           0xFF
	//#define CompressQuat32_FScale1        723.3710f   // = Scale1 / (2.0 * 0.707106)
	//#define CompressQuat32_FScale2        442.5391f   // = Scale2 / (2.0 * 0.577350)
	//// Approximate angular error ~0.27 degrees
	//ri::uint32_t CompressQuat32(MAq orientation)
	//{
	//  RI_S_ASSERT_STR(ASSERTS_MATHS, orientation.isUnit(), "Orientation quaternions must be normalised before being compressed");
	//
	//  // Choose the largest component
	//  int largest1 = 0;
	//  for( int i = 1; i != 4; ++i )
	//  {
	//      if( maAbs(orientation[largest1]) < maAbs(orientation[i]) )
	//      {
	//          largest1 = i;
	//      }
	//  }
	//
	//  // Ensure the component we drop is positive
	//  // MAq does not have a unary '-' operator
	//  if( orientation[largest1] < 0.0f )
	//  {
	//      orientation[0] = -orientation[0];
	//      orientation[1] = -orientation[1];
	//      orientation[2] = -orientation[2];
	//      orientation[3] = -orientation[3];
	//  }
	//
	//  // Choose the next largest component
	//  int largest2 = (largest1 != 0) ? (0) : (1);
	//  for( int i = 0; i != 4; ++i )
	//  {
	//      if( i == largest1 ) continue;
	//      if( maAbs(orientation[largest2]) < maAbs(orientation[i]) )
	//      {
	//          largest2 = i;
	//      }
	//  }
	//
	//  // Compress the remaining three components
	//  int the_big_one = static_cast<int>((orientation[largest2] * CompressQuat32_FScale1) + CompressQuat32_Ofs1);
	//  the_big_one = (int)maClamp(static_cast<float>(the_big_one), 0.0f, CompressQuat32_Scale1);
	//
	//  ri::uint32_t compressed_quat =  (largest1    << 30) |   // index of the largest
	//                                  (largest2    << 28) |   // index of the second largest
	//                                  (the_big_one << 18);    // The compressed value of the second largest
	//  int shift = 9;
	//  for( int i = 0; i != 4; ++i )
	//  {
	//      if( i == largest1 || i == largest2 ) continue;
	//
	//      int compressed_value = static_cast<int>((orientation[i] * CompressQuat32_FScale2) + CompressQuat32_Ofs2);
	//      compressed_value = (int)maClamp(static_cast<float>(compressed_value), 0.0f, CompressQuat32_Scale2);
	//      compressed_quat |= compressed_value << shift;
	//      shift = 0;
	//  }
	//  return compressed_quat;
	//}
	//MAq DecompressQuat32(ri::uint32_t compressed_orientation)
	//{
	//  int largest1 = (compressed_orientation >> 30) & 0x3;
	//  int largest2 = (compressed_orientation >> 28) & 0x3;
	//
	//  MAq orientation;
	//  orientation[largest2] = (static_cast<int>((compressed_orientation >> 18) & CompressQuat32_Scale1) - CompressQuat32_Ofs1) / CompressQuat32_FScale1;
	//
	//  int shift = 9;
	//  float sq_sum = orientation[largest2] * orientation[largest2];
	//  for( int i = 0; i != 4; ++i )
	//  {
	//      if( i == largest1 || i == largest2 ) continue;
	//      orientation[i] = (static_cast<int>((compressed_orientation >> shift) & CompressQuat32_Scale2) - CompressQuat32_Ofs2) / CompressQuat32_FScale2;
	//      shift = 0;
	//
	//      sq_sum += orientation[i] * orientation[i];
	//  }
	//  orientation[largest1] = maSqrt(1.0f - sq_sum);
	//  return orientation;
	//}
	//#undef CompressQuat32_Scale1
	//#undef CompressQuat32_Scale2
	//#undef CompressQuat32_Ofs1
	//#undef CompressQuat32_Ofs2
	//#undef CompressQuat32_FScale1
	//#undef CompressQuat32_FScale2
	//
	////*****
	//// Compress/Decompress a normalised vector into 32 bits. Not particularly fancy version
	//#define CompressNormal32_1_Bits  10
	//#define CompressNormal32_1_Scale 511
	//#define CompressNormal32_1_Mask  0x3FF
	//ri::uint32_t CompressNormal32_1(MAv4ref normal)
	//{
	//  RI_S_ASSERT_STR(ASSERTS_MATHS, maAbs(normal[0]) <= 1.0f && maAbs(normal[1]) <= 1.0f && maAbs(normal[2]) <= 1.0f, "Only supports vectors with components in the range -1 to 1");
	//
	//  ri::uint32_t x = static_cast<ri::uint32_t>(int((normal[0] * CompressNormal32_1_Scale) + CompressNormal32_1_Scale)) & CompressNormal32_1_Mask;
	//  ri::uint32_t y = static_cast<ri::uint32_t>(int((normal[1] * CompressNormal32_1_Scale) + CompressNormal32_1_Scale)) & CompressNormal32_1_Mask;
	//  ri::uint32_t z = static_cast<ri::uint32_t>(int((normal[2] * CompressNormal32_1_Scale) + CompressNormal32_1_Scale)) & CompressNormal32_1_Mask;
	//  return  (x << (2 * CompressNormal32_1_Bits)) |
	//          (y << (    CompressNormal32_1_Bits)) |
	//          (z << (                          0));
	//}
	//MAv4 DecompressNormal32_1(ri::uint32_t packed_normal)
	//{
	//  return MAv4::make(  (((packed_normal >> (2 * CompressNormal32_1_Bits)) & CompressNormal32_1_Mask) - CompressNormal32_1_Scale) / float(CompressNormal32_1_Scale),
	//                          (((packed_normal >> (    CompressNormal32_1_Bits)) & CompressNormal32_1_Mask) - CompressNormal32_1_Scale) / float(CompressNormal32_1_Scale),
	//                          (((packed_normal >> (                          0)) & CompressNormal32_1_Mask) - CompressNormal32_1_Scale) / float(CompressNormal32_1_Scale),
	//                          0.0f);
	//}
	//#undef CompressNormal32_1_Bits
	//#undef CompressNormal32_1_Scale
	//#undef CompressNormal32_1_Mask
	//
	//
	//#define CompressNormal16_1_IndexBits    13        // 16 - 3
	//#define CompressNormal16_1_IndexMask  0x1FFF  // (1 << CompressNormal16_1_IndexBits) - 1
	//#define CompressNormal16_1_MaxYDiv        (89.0f) // = maFloor(maSqrt(1 << CompressNormal16_1_IndexBits)) - 1.0f;
	////*****
	//// Compress/Decompress a normalised vector into 16bits. ~0.7 degrees angular error measured empirically
	//ri::uint16_t CompressNormal16_1(MAv4ref normal)
	//{
	//  RI_ASSERT_STR(normal.lengthWithoutW() - 1.0f < ZERO, "Only normals should be provided");
	//  int sign_bits = (int(normal[0] >= 0.0f) << 2) | (int(normal[1] >= 0.0f) << 1) | int(normal[2] >= 0.0f);
	//
	//  // Project onto the plane x+y+z=1
	//  float x = maAbs(normal[0]);
	//  float y = maAbs(normal[1]);
	//  float z = maAbs(normal[2]);
	//  float sum = x + y + z;
	//  x /= sum;
	//  y /= sum;
	//  z /= sum;
	//
	//  int iy = int((1.0f - y) * CompressNormal16_1_MaxYDiv);
	//
	//  float xz_scale = 1.0f - y;
	//  int MaxXZDiv = 2 * iy;
	//  int ixz;
	//  if( xz_scale < ZERO )   { ixz = 0; }
	//  else                    { ixz = int((z / xz_scale) * float(MaxXZDiv)); }
	//  RI_ASSERT(ixz <= MaxXZDiv);
	//
	//  int index = iy * iy + ixz;
	//  RI_ASSERT((index & CompressNormal16_1_IndexMask) == index);
	//
	//  return ri::uint16_t((sign_bits << CompressNormal16_1_IndexBits) | index);
	//}
	//MAv4 DecompressNormal16_1(ri::uint16_t packed_normal)
	//{
	//  int sign_bits = packed_normal >> CompressNormal16_1_IndexBits;
	//  int index     = packed_normal & CompressNormal16_1_IndexMask;
	//
	//  int iy  = int(maFloor(maSqrt(float(index))));
	//  int ixz = index - iy * iy;
	//  int MaxXZDiv = 2 * iy;
	//
	//  float xz_scale = iy / CompressNormal16_1_MaxYDiv;
	//
	//  float x, y, z;
	//  if( iy != 0 )
	//  {
	//      float xz_ratio = ixz / float(MaxXZDiv);
	//      x = (1.0f - xz_ratio) * xz_scale;
	//      y = (1.0f - xz_scale);
	//      z = (xz_ratio       ) * xz_scale;
	//  }
	//  else
	//  {
	//      x = 0.0f;
	//      y = 1.0f;
	//      z = 0.0f;
	//  }
	//
	//  MAv4 direction;
	//  direction.setAsDirection(
	//      (float((sign_bits >> 2) & 0x1) * 2.0f - 1.0f) * x,
	//      (float((sign_bits >> 1) & 0x1) * 2.0f - 1.0f) * y,
	//      (float((sign_bits >> 0) & 0x1) * 2.0f - 1.0f) * z);
	//
	//  direction.getNormal3(direction);
	//  return direction;
	//}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(CompressionTests)
	{
		float const step = 0.05f;
		{ // Norm5bit
			float max_error = 0.0f;
			for (float z = -1.0f; z <= 1.0f; z += step)
			for (float y = -1.0f; y <= 1.0f; y += step)
			for (float x = -1.0f; x <= 1.0f; x += step)
			{
				v4   in_  = v4::Normal3(x,y,z,0);
				auto enc  = Norm5bit::Compress(in_);
				v4   out_ = Norm5bit::Decompress(enc);
				max_error = std::max(Length3(out_ - in_), max_error);
			}
			PR_CHECK(max_error < 0.6f, true);
		}
	}
}
#endif
