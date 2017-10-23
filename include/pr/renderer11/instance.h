//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/instances/instance.h"

// Use this to define class types that are compatible with the renderer
// Example:
//  #define PR_RDR_INST(x)\
//     x(ModelPtr ,m_model  ,EInstComp::ModelPtr)\
//     x(Colour32 ,m_colour ,EInstComp::TintColour32)
//  PR_RDR_DEFINE_INSTANCE(name, PR_RDR_INST)
//  #undef PR_RDR_INST

// Macro instance generator functions
#define PR_RDR_INST_INITIALISERS(ty,nm,em)      ,nm()
#define PR_RDR_INST_MEMBER_COUNT(ty,nm,em)      + 1
#define PR_RDR_INST_MEMBERS(ty,nm,em)           ty nm;
#define PR_RDR_INST_INIT_COMPONENTS(ty,nm,em)   m_cpt[i++] = CompDesc::make(em, offsetof(inst_type, nm));

// Notes:
// No inheritance in this type. It relies on POD behaviour
// Be careful with alignment of members, esp. m4x4's
#define PR_RDR_DEFINE_INSTANCE(name, fields)\
	struct name\
	{\
		pr::rdr::BaseInstance m_base;\
		pr::rdr::CompDesc m_cpt[0 fields(PR_RDR_INST_MEMBER_COUNT)];\
		fields(PR_RDR_INST_MEMBERS)\
\
		name()\
			:m_base()\
			,m_cpt()\
			fields(PR_RDR_INST_INITIALISERS)\
		{\
			using namespace pr::rdr;\
			using inst_type = name;\
			static_assert(offsetof(inst_type, m_base) == 0, "'m_base' must be be the first member");\
			int i = 0;\
			m_base.m_cpt_count = _countof(m_cpt);\
			fields(PR_RDR_INST_INIT_COMPONENTS)\
		}\
\
		/* Overload operator new/delete to ensure alignment*/\
		static void* __cdecl operator new(size_t count)\
		{\
			return pr::impl::AlignedAlloc<16>(count);\
		}\
		static void __cdecl operator delete (void* obj)\
		{\
			pr::impl::AlignedFree<16>(obj);\
		}\
	};
