//****************************************************************************
//
//	Vertex Format
//
//****************************************************************************
#ifndef VERTEX_FORMAT_H
#define VERTEX_FORMAT_H

#include "PR/Common/D3DPtr.h"
#include "PR/Geometry/PRGeometry.h"

namespace pr
{
	namespace rdr
	{
		namespace vf
		{
			typedef uint32						Type;
			typedef uint32						Format;
			typedef IDirect3DVertexDeclaration9	VDecl;

			enum EType
			{
				EType_PosNormDiffTex,
				EType_PosNormDiffTexFuture,
				EType_NumberOf,
				EType_Invalid
			};

			enum EFormat
			{
				EFormat_Pos		= 1 << 0,
				EFormat_Norm	= 1 << 1,
				EFormat_Diff	= 1 << 2,
				EFormat_Tex		= 1 << 3,
				EFormat_Future	= 1 << 4,
				EFormat_Invalid	= 0xFFFFFFFF
			};

			// PosNormTex
			struct PosNormDiffTex			{ v3 m_vertex; v3 m_normal; Colour32 m_colour; v2 m_tex; };
			struct PosNormDiffTexFuture		{ v3 m_vertex; v3 m_normal; Colour32 m_colour; v2 m_tex; v4 m_future; };

			// Vertex Format functions
			uint	GetSize(Type type);
			EType	GetEType(Type type);
			EType	GetTypeFromGeomType(GeomType geom_type);
			Format	GetFormat(Type type);

			// Manager for creating/destroying vertex declarations
			class Manager
			{
			public:
				Manager(D3DPtr<IDirect3DDevice9> d3d_device);
				~Manager();
				void CreateDeviceDependentObjects(D3DPtr<IDirect3DDevice9> d3d_device);
				void ReleaseDeviceDependentObjects();
				D3DPtr<IDirect3DVertexDeclaration9> GetVertexDeclaration(Type type) const 
				{
					PR_ASSERT_STR(PR_DBG_RDR, type < EType_NumberOf, "Unknown vertex format type");
					return m_vd[type];
				};

			private:
				D3DPtr<IDirect3DDevice9>			m_d3d_device;
				D3DPtr<IDirect3DVertexDeclaration9> m_vd[EType_NumberOf];
			};

			// General Vertex Type Iterator
			struct MemberOffsets;
			class Iter
			{
				struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
			public:
				Iter();
				Iter(void* vertex_buffer, Type type);

				void		Set(const Vertex& vertex);
				void		Set(const v4& pos = v4Origin, const v4& norm = v4Zero, Colour32 col = Colour32One, const v2& uv = v2Zero);
				v3&			Vertex();
				v3&			Normal();
				Colour32&	Colour();
				v2&			Tex();
				Iter&		 operator ++()					{ m_iter += m_size; return *this; }
				Iter		 operator[](uint ofs) const		{ return Iter(m_iter + ofs * m_size, m_vf); }
							operator bool_type() const		{ return m_iter != 0 ? &bool_tester::x : static_cast<bool_type>(0); }
			private:
				uint8*		m_iter;
				Type		m_vf;
				Format		m_format;
				uint		m_size;
				const MemberOffsets* m_ofs;
			};
		}//namespace vf
	}//namespace rdr
}//namespace rd

#include "PR/Renderer/VertexFormatImpl.h"

#endif//VERTEX_FORMAT_H