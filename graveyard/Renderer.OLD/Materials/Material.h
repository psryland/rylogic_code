//**********************************************************************************
//
// rdr::Material
//
//**********************************************************************************
#ifndef PR_RDR_MATERIAL_H
#define PR_RDR_MATERIAL_H

#include "PR/Common/PRTypes.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
		// A object the references a texture and an effect
		struct Material
		{
			rdr::Texture* m_texture;
			effect::Base* m_effect;
		};

		//struct MaterialIndex
		//{
		//	enum
		//	{
		//		InvalidIndex	= 0x7FFFFFFF,
		//		ALPHA			= 0x80000000,
		//		EFFECT_MASK		= 0x7FFF0000,
		//		EFFECT_OFS		= 16,
		//		TEXTURE_MASK	= 0x0000FFFF,
		//		TEXTURE_OFS		= 0,
		//	};

		//	MaterialIndex() : m_mat_index(InvalidIndex) {}
		//	MaterialIndex(const MaterialIndex& m) : m_mat_index(m.m_mat_index) {}
		//	MaterialIndex(bool has_alpha, uint effect_index, uint tex_index)
		//	{
		//		if( effect_index	== InvalidIndex ||
		//			tex_index		== InvalidIndex )
		//		{
		//			m_mat_index = InvalidIndex;
		//		}
		//		else
		//		{
		//			m_mat_index =	((has_alpha ? 1 : 0) * ALPHA) |
		//							((effect_index	<< EFFECT_OFS  ) & EFFECT_MASK	) |
		//							((tex_index		<< TEXTURE_OFS ) & TEXTURE_MASK	) ;
		//		}
		//	}

		//	bool	HasAlpha() const					{ return (m_mat_index & ALPHA) == ALPHA; }
		//	uint	EffectIndex() const					{ return (m_mat_index & EFFECT_MASK) >> EFFECT_OFS; }
		//	uint	TexIndex() const					{ return (m_mat_index & TEXTURE_MASK) >> TEXTURE_OFS; }
		//	void	SetAlpha(bool has_alpha)			{ m_mat_index &= ~ALPHA;			m_mat_index |= (has_alpha ? 1 : 0) * ALPHA; }
		//	void	SetEffectIndex(uint effect_index)	{ m_mat_index &= ~EFFECT_MASK;		m_mat_index |= ((effect_index << EFFECT_OFS) & EFFECT_MASK); }
		//	void	SetTexIndex(uint tex_index)			{ m_mat_index &= ~TEXTURE_MASK;		m_mat_index |= ((tex_index << TEXTURE_OFS) & TEXTURE_MASK); }
		//	
		//	bool	operator == (const MaterialIndex& m) const { return m_mat_index == m.m_mat_index; }
		//	bool	operator <  (const MaterialIndex& m) const { return m_mat_index <  m.m_mat_index; }
		//	bool	operator >  (const MaterialIndex& m) const { return m_mat_index >  m.m_mat_index; }
		//	bool	operator != (const MaterialIndex& m) const { return m_mat_index != m.m_mat_index; }
		//	bool	operator <= (const MaterialIndex& m) const { return m_mat_index <= m.m_mat_index; }
		//	bool	operator >= (const MaterialIndex& m) const { return m_mat_index >= m.m_mat_index; }
		//	operator const uint&() const		{ return m_mat_index; }
		//	//operator     uint&()				{ return m_mat_index; } // Don't allow this

		//private:
		//	uint	m_mat_index;
		//};
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_MATERIAL_H
