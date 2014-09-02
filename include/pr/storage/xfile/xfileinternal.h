#ifndef PR_XFILE_INTERNAL_H
#define PR_XFILE_INTERNAL_H

#include <dxfile.h>
#include "pr/common/assert.h"
#include "pr/common/exception.h"
#include "pr/common/hresult.h"
#include "pr/common/d3dptr.h"
#include "pr/storage/xfile/xfile.h"

namespace pr
{
	namespace xfile
	{
		// A helper object for accessing xfile data
		struct XData
		{
			XData(D3DPtr<ID3DXFileData> data) :m_file_data(data)
			{
				if( Failed(m_file_data->Lock(&m_size, &m_ptr.m_data)) )
				{
					throw xfile::Exception(EResult_LockDataFailed);
				}
			}
			~XData()
			{
				m_file_data->Unlock();
			}

			D3DPtr<ID3DXFileData>	m_file_data;
			SIZE_T					m_size;
			union Ptr
			{
				const void*		m_data;
				const int*		m_int;
				const uint*		m_uint;
				const float*	m_float;
				const char*		m_str;
			} m_ptr;
		};
		
		
		//// A helper object for accessing xfile data
		//struct XData
		//{
		//	XData(D3DPtr<ID3DXFileData> data)
		//	:m_file_data(data)
		//	,m_size(0)
		//	,m_ofs(0)
		//	,m_data(0)									{ if( Failed(m_file_data->Lock(&m_size, &m_data)) ) { throw xfile::Exception(EResult_LockDataFailed); } }
		//	~XData()									{ m_file_data->Unlock(); }
		//	int   Int       () const					{                                                return static_cast<const int*>   (m_data)[m_ofs];        }
		//	int   Int       (std::size_t ofs) const		{ PR_ASSERT(PR_DBG_XFILE, ofs + m_ofs < m_size); return static_cast<const int*>   (m_data)[m_ofs + ofs * sizeof(int)];  }
		//	uint  Uint      () const					{                                                return static_cast<const uint*>  (m_data)[m_ofs];        }
		//	uint  Uint      (std::size_t ofs) const		{ PR_ASSERT(PR_DBG_XFILE, ofs + m_ofs < m_size); return static_cast<const uint*>  (m_data)[m_ofs + ofs * sizeof(uint)]; }
		//	float Float     () const					{                                                return static_cast<const float*> (m_data)[m_ofs];        }
		//	float Float     (std::size_t ofs) const		{ PR_ASSERT(PR_DBG_XFILE, ofs + m_ofs < m_size); return static_cast<const float*> (m_data)[m_ofs + ofs * sizeof(float)]; }
		//	const char* Str () const					{												 return static_cast<const char*>  (m_data) + m_ofs;       }
		//	const char* Str (std::size_t ofs) const		{ PR_ASSERT(PR_DBG_XFILE, ofs + m_ofs < m_size); return static_cast<const char*>  (m_data) + m_ofs + ofs * sizeof(const char*); }
		//	XData& operator ++()						{ PR_ASSERT(PR_DBG_XFILE,       m_ofs < m_size); ++m_ofs; return *this; }

		//	D3DPtr<ID3DXFileData> m_file_data;
		//	SIZE_T m_size;		// In units of int's
		//	std::size_t m_ofs;	// int offset
		//	const void* m_data;	// Pointer to int sized data
		//};
	}//namespace xfile
}//namespace pr

#endif//PR_XFILE_INTERNAL_H
