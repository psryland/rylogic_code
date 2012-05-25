//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once 
#ifndef PR_RDR_VERTEX_FORMAT_H
#define PR_RDR_VERTEX_FORMAT_H

#include <new>
#include "pr/renderer/types/forward.h"

namespace pr
{
	namespace rdr
	{
		namespace vf
		{
			typedef uint32                      Type;
			typedef uint32                      Format;
			typedef IDirect3DVertexDeclaration9 VDecl;
			
			namespace EVertType
			{
				enum Type
				{
					PosNormDiffTex,
					PosNormDiffTexFuture,
					NumberOf,
					Invalid
				};
			}
			namespace EFormat
			{
				enum Type
				{
					Pos     = 1 << 0,
					Norm    = 1 << 1,
					Diff    = 1 << 2,
					Tex     = 1 << 3,
					Future  = 1 << 4,
					Invalid = 0xFFFFFFFF
				};
			}
			
			// PosNormDiffTex
			struct PosNormDiffTex       { v3 m_vertex; v3 m_normal; Colour32 m_colour; v2 m_tex; };
			struct PosNormDiffTexFuture { v3 m_vertex; v3 m_normal; Colour32 m_colour; v2 m_tex; v4 m_future; };
			struct MemberOffsets        { int m_vertex; int m_normal; int m_colour; int m_tex; int m_future; };

			// Vertex Format functions
			MemberOffsets const& GetOffsets(Type type);
			std::size_t          GetSize(Type type);
			EVertType::Type      GetEType(Type type);
			Format               GetFormat(Type type);
			EVertType::Type      GetTypeFromGeomType(GeomType geom_type);
			
			struct RefVertex
			{
				union {
				uint8*          m_ptr;
				PosNormDiffTex* m_vert;
				};
				Format          m_format;
				Type            m_vf;
				
				RefVertex*      operator->()   { return this; }
				v3&             vertex()       { static v3 dummy;       return (m_format&EFormat::Pos   ) ? reinterpret_cast<v3&>      (m_ptr[GetOffsets(m_vf).m_vertex]) : dummy; }
				v3&             normal()       { static v3 dummy;       return (m_format&EFormat::Norm  ) ? reinterpret_cast<v3&>      (m_ptr[GetOffsets(m_vf).m_normal]) : dummy; }
				Colour32&       colour()       { static Colour32 dummy; return (m_format&EFormat::Diff  ) ? reinterpret_cast<Colour32&>(m_ptr[GetOffsets(m_vf).m_colour]) : dummy; }
				v2&             tex()          { static v2 dummy;       return (m_format&EFormat::Tex   ) ? reinterpret_cast<v2&>      (m_ptr[GetOffsets(m_vf).m_tex])    : dummy; }
				v4&             future()       { static v4 dummy;       return (m_format&EFormat::Future) ? reinterpret_cast<v4&>      (m_ptr[GetOffsets(m_vf).m_future]) : dummy; }
				v3 const&       vertex() const { return const_cast<RefVertex*>(this)->vertex(); }
				v3 const&       normal() const { return const_cast<RefVertex*>(this)->normal(); }
				Colour32 const& colour() const { return const_cast<RefVertex*>(this)->colour(); }
				v2 const&       tex() const    { return const_cast<RefVertex*>(this)->tex();    }
				v4 const&       future() const { return const_cast<RefVertex*>(this)->future(); }
				void            set(v4 const& pos)                                             { vertex().set(pos); }
				void            set(v4 const& pos, v4 const& norm)                             { vertex().set(pos); normal().set(norm); }
				void            set(v4 const& pos, Colour32 col)                               { vertex().set(pos);                     colour() = col; }
				void            set(v4 const& pos, v2 const& uv)                               { vertex().set(pos); tex() = uv; }
				void            set(v4 const& pos, v4 const& norm, Colour32 col)               { vertex().set(pos); normal().set(norm); colour() = col; }
				void            set(v4 const& pos, Colour32 col, v2 const& uv)                 { vertex().set(pos); colour() = col; tex() = uv; }
				void            set(v4 const& pos, v4 const& norm, Colour32 col, v2 const& uv) { vertex().set(pos); normal().set(norm); colour() = col; tex() = uv; }
				void            set(pr::Vert const& v)                                         { vertex().set(v.m_vertex); normal().set(v.m_normal); colour() = v.m_colour; tex() = v.m_tex_vertex; }
			};
			
			class iterator
			{
				struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
				friend struct RefVertex;
				union {
				uint8*          m_iter;
				PosNormDiffTex* m_vert;
				};
				Type            m_vf;
				Format          m_format;
				std::size_t     m_size;
				
			public:
				iterator() :m_iter(0) ,m_vf(EVertType::Invalid) ,m_format(0) ,m_size(0) {}
				iterator(void* vertex_buffer, Type type) :m_iter(static_cast<uint8*>(vertex_buffer)) ,m_vf(type) ,m_format(GetFormat(type)) ,m_size(GetSize(type)) {}
				RefVertex   operator *  ()                          { RefVertex v = {m_iter, m_format, m_vf}; return v; }
				RefVertex   operator -> ()                          { return operator *(); }
				uint8*      GetRawPointer()                         { return m_iter; }
				iterator&   operator ++ ()                          { m_iter += m_size; return *this; }
				iterator    operator ++ (int)                       { iterator i = *this; i.m_iter += i.m_size; return i; }
				iterator    operator +  (std::size_t ofs) const     { iterator i = *this; i.m_iter += ofs * i.m_size; return i; }
				iterator    operator -  (std::size_t ofs) const     { iterator i = *this; i.m_iter -= ofs * i.m_size; return i; }
				std::size_t operator -  (iterator ptr) const        { PR_ASSERT(PR_DBG_RDR, 0 == (m_iter - ptr.m_iter) % m_size, ""); return (m_iter - ptr.m_iter) / m_size; }
				RefVertex   operator [] (std::size_t ofs) const     { return *operator + (ofs); }
				bool        operator == (iterator const& rhs) const { return m_iter == rhs.m_iter; }
				bool        operator != (iterator const& rhs) const { return m_iter != rhs.m_iter; }
				bool        operator <  (iterator const& rhs) const { return m_iter <  rhs.m_iter; }
				bool        operator >  (iterator const& rhs) const { return m_iter >  rhs.m_iter; }
				bool        operator <= (iterator const& rhs) const { return m_iter <= rhs.m_iter; }
				bool        operator >= (iterator const& rhs) const { return m_iter >= rhs.m_iter; }
				            operator bool_type() const              { return m_iter != 0 ? &bool_tester::x : static_cast<bool_type>(0); }
			};
			
			// Implementation ********************************************************************
			inline MemberOffsets const& GetOffsets(Type type)
			{
				// Return the member offsets for a vertex type
				static MemberOffsets const offsets[] = 
				{
					{// PosNormDiffTex
						offsetof(PosNormDiffTex      , m_vertex),
						offsetof(PosNormDiffTex      , m_normal),
						offsetof(PosNormDiffTex      , m_colour),
						offsetof(PosNormDiffTex      , m_tex),
						-1
					},
					{// PosNormDiffTexFuture
						offsetof(PosNormDiffTexFuture, m_vertex),
						offsetof(PosNormDiffTexFuture, m_normal),
						offsetof(PosNormDiffTexFuture, m_colour),
						offsetof(PosNormDiffTexFuture, m_tex),
						offsetof(PosNormDiffTexFuture, m_future)
					},
					{ -1, -1, -1, -1, -1 } // Invalid
				};
				return offsets[type];
			}
			inline std::size_t GetSize(Type type)
			{
				switch (type)
				{
				default:return 0;
				case EVertType::PosNormDiffTex:       return sizeof(PosNormDiffTex);
				case EVertType::PosNormDiffTexFuture: return sizeof(PosNormDiffTexFuture);
				};
			}
			inline EVertType::Type GetEType(Type type)
			{
				return reinterpret_cast<const EVertType::Type&>(type);
			}
			inline Format GetFormat(Type type)
			{
				switch (type)
				{
				default:return 0;
				case EVertType::PosNormDiffTex:       return EFormat::Pos | EFormat::Norm | EFormat::Diff | EFormat::Tex;
				case EVertType::PosNormDiffTexFuture: return EFormat::Pos | EFormat::Norm | EFormat::Diff | EFormat::Tex | EFormat::Future;
				};
			}
			inline EVertType::Type GetTypeFromGeomType(GeomType geom_type)
			{
				using namespace pr::geom;
				switch (geom_type)
				{
				default: return EVertType::Invalid;
				case EVertex: case EVN: case EVC: case EVNC: case EVT: case EVNT: case EVCT: case EVNCT: return EVertType::PosNormDiffTex;
				}
			}
		}
	}
}

#endif
