//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_KEY_BINDINGS_H
#define LDR_KEY_BINDINGS_H

#include <winuser.h>

namespace ldr
{
	namespace EKey
	{
		enum Type
		{
			Left,
			Up,
			Right,
			Down,
			In,
			Out,
			Rotate,		// Key to enable camera rotations, maps translation keys to rotations
			TranslateZ,	// Key to set In/Out to be z translations rather than zoom
			Accurate,
			NumberOf
		};
	}

	struct KeyBindings
	{
		int m_bindings[EKey::NumberOf];
		int operator[](EKey::Type key) const { return m_bindings[key]; }
		KeyBindings()
		{
			m_bindings[EKey::Left		] = VK_LEFT;
			m_bindings[EKey::Up			] = VK_UP;
			m_bindings[EKey::Right		] = VK_RIGHT;
			m_bindings[EKey::Down		] = VK_DOWN;
			m_bindings[EKey::In			] = VK_HOME;
			m_bindings[EKey::Out			] = VK_END;
			m_bindings[EKey::Rotate		] = VK_SHIFT;
			m_bindings[EKey::TranslateZ	] = VK_SHIFT;
			m_bindings[EKey::Accurate	] = VK_SHIFT;
		}
	};
}

#endif//LDR_KEY_BINDINGS_H
