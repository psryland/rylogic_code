//********************************************************
//
//	Image2D
//
//********************************************************
#ifndef PR_IMAGE_2D_ITER_H
#define PR_IMAGE_2D_ITER_H

#include "pr/common/PRTypes.h"
#include "pr/common/assert.h"
#include "pr/Image/ImageInfo.h"
#include "pr/Image/ImageAssertEnable.h"

namespace pr
{
	namespace image
	{
		namespace impl
		{
			class Image2DDataProxy
			{
			public:
				Image2DDataProxy(void* ptr, uint bytes_per_pixel)
				:m_ptr(ptr)
				,m_bytes_per_pixel(bytes_per_pixel)
				{}

				Image2DDataProxy& operator = (uint value)
				{
					switch( m_bytes_per_pixel )
					{
					default: PR_ASSERT(PR_DBG_IMAGE, false, "Unsupported bit depth"); break;
					case 4: *(uint32*)m_ptr = (uint32)value; break;
					case 2: *(uint16*)m_ptr = (uint16)value; break;
					case 1:  *(uint8*)m_ptr =  (uint8)value; break;
					}
					return *this;
				}
				Image2DDataProxy& operator = (const Image2DDataProxy& copy)
				{
					switch( m_bytes_per_pixel )
					{
					default: PR_ASSERT(PR_DBG_IMAGE, false, "Unsupported bit depth"); break;
					case 4: *(uint32*)m_ptr = *(uint32*)copy.m_ptr; break;
					case 2: *(uint16*)m_ptr = *(uint16*)copy.m_ptr; break;
					case 1:  *(uint8*)m_ptr =  *(uint8*)copy.m_ptr; break;
					}
					return *this;
				}

			private:
				void*	m_ptr;
				uint	m_bytes_per_pixel;
			};

			class Image2DIter
			{
				struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
			public:
				Image2DIter()
				:m_data_start		(NULL)
				,m_line_start		(NULL)
				,m_data				(NULL)
				,m_width			(0)
				,m_height			(0)
				,m_pitch			(0)
				,m_bytes_per_pixel	(1)
				{}
				Image2DIter(uint8* data_start, uint xpos, uint ypos, uint width, uint height, uint pitch, uint bytes_per_pixel)
				:m_data_start		(data_start)
				,m_line_start		(data_start + ypos * pitch)
				,m_data				(data_start + ypos * pitch + xpos * bytes_per_pixel)
				,m_width			(width)
				,m_height			(height)
				,m_pitch			(pitch)
				,m_bytes_per_pixel	(bytes_per_pixel)
				{}
				bool operator == (const Image2DIter& other) const
				{
					return	m_data_start		== other.m_data_start	&&
							m_line_start		== other.m_line_start	&&
							m_data				== other.m_data			&&
							m_width				== other.m_width		&&
							m_height			== other.m_height		&&
							m_pitch				== other.m_pitch		&&
							m_bytes_per_pixel	== other.m_bytes_per_pixel;
				}
				bool operator != (const Image2DIter& other) const
				{
					return !(*this == other);
				}
				operator bool_type() const
				{
					return m_data_start != 0 ? &bool_tester::x : static_cast<bool_type>(0);
				}
				bool IsValid() const
				{
					return m_line_start >= m_data_start && m_line_start < m_data_start + m_height * m_pitch;
				}
				Image2DIter& operator ++ ()
				{
					m_data += m_bytes_per_pixel;
					if( m_data >= m_line_start + m_width * m_bytes_per_pixel )
					{
						m_line_start	+= m_pitch;
						m_data			=  m_line_start;
					}
					return *this;
				}
				Image2DIter& operator -- ()
				{
					m_data -= m_bytes_per_pixel;
					if( m_data < m_line_start )
					{
						m_line_start	-= m_pitch;
						m_data			=  m_line_start + (m_width - 1) * m_bytes_per_pixel;
					}
					return *this;
				}
				Image2DDataProxy operator * ()
				{
					PR_ASSERT(PR_DBG_IMAGE, IsValid(), "Accessing outside the image area");
					return Image2DDataProxy(m_data, m_bytes_per_pixel);
				}
				Image2DDataProxy operator ()(uint x, uint y)
				{
					x += (uint)(m_data - m_line_start) / m_bytes_per_pixel;
					y += (uint)(m_line_start - m_data_start);

					PR_ASSERT(PR_DBG_IMAGE, x < m_width && y < m_height, "Accessing outside the image area");
					return Image2DDataProxy(m_data_start + y * m_pitch + x * m_bytes_per_pixel, m_bytes_per_pixel);
				}

			private:
				uint8*	m_data_start;
				uint8*	m_line_start;
				uint8*	m_data;
				uint	m_width;
				uint	m_height;
				uint	m_pitch;
				uint	m_bytes_per_pixel;
			};
	
		}//namespace impl
	}//namespace image
}//namespace pr

#endif//PR_IMAGE_2D_ITER_H
