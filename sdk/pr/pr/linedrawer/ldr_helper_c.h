//************************************
// LineDrawerHelper (C)
//  Copyright © Rylogic Ltd 2006
//************************************
#ifndef PR_LINE_DRAWER_HELPER_C_H
#define PR_LINE_DRAWER_HELPER_C_H

#include <stdio.h>
#include "shared/maths/maths.h"
#include "shared/maths/maths_float.h"
#include "shared/fmt_c.h"
	
static __inline char const* Ldr_Pos(vec4_t const* pos)
{
	static char buf[64];
	if (!pos) return "";
	return Fmt(buf, sizeof(buf), "*pos {%d %d %d}" ,pos->x ,pos->y ,pos->z);
}
	
static __inline char const* Ldr_Scl(vec4_t const* scale)
{
	static char buf[64];
	if (!scale) return "";
	return Fmt(buf, sizeof(buf), "*scale {%d %d %d}" ,scale->x ,scale->y ,scale->z);
}
	
static __inline char const* Ldr_Quat(vec4_t const* quat)
{
	static char buf[64];
	return Fmt(buf, sizeof(buf), "*quat{%f %f %f %f}" ,quat->x*FUNIT_LENGTH_INV ,quat->y*FUNIT_LENGTH_INV ,quat->z*FUNIT_LENGTH_INV ,quat->w*FUNIT_LENGTH_INV);
}
	
static __inline char const* Ldr_O2W(char const* txfm)
{
	static char buf[64];
	return Fmt(buf, sizeof(buf), "*o2w{%s}" ,txfm);
}
	
static __inline char const* Ldr_O2W2(char const* txfm1, char const* txfm2)
{
	static char buf[128];
	return Fmt(buf, sizeof(buf), "*o2w{%s %s}" ,txfm1 ,txfm2);
}
	
static __inline char const* Ldr_O2W3(char const* txfm1, char const* txfm2, char const* txfm3)
{
	static char buf[192];
	return Fmt(buf, sizeof(buf), "*o2w{%s %s %s}" ,txfm1 ,txfm2 ,txfm3);
}
	
static __inline void Ldr_Position(vec4_t const* pos, FILE* f)
{
	if (!pos) return;
	fprintf(f,	"%s\n" ,Ldr_O2W(Ldr_Pos(pos)));
}
	
static __inline void Ldr_Scale(vec4_t const* scale, FILE* f)
{
	if (!scale) return;
	fprintf(f, "%s\n", Ldr_O2W(Ldr_Scl(scale)));
}
	
static __inline void Ldr_PosScale(vec4_t const* pos, vec4_t const* scale, FILE* f)
{
	fprintf(f, "%s\n", Ldr_O2W2(Ldr_Pos(pos), Ldr_Scl(scale)));
}
	
static __inline void Ldr_Line(char const* name, unsigned int colour, vec4_t const* start, vec4_t const* end, FILE* f)
{
	fprintf(f, "*Line %s %08X "
				"{ "
					"%d %d %d %d %d %d "
				"}\n"
				,name ,colour
				,start->x ,start->y ,start->z
				,end->x ,end->y ,end->z
				);
	fflush(f);
}
	
static __inline void Ldr_LineD(char const* name, unsigned int colour, vec4_t const* start, vec4_t const* dir, float scaler, FILE* f)
{
	fprintf(f, "*LineD %s %08X "
				"{ "
					"%f %f %f %f %f %f "
				"}\n"
				,name ,colour
				,start->x*1.0f ,start->y*1.0f ,start->z*1.0f
				,dir->x*scaler ,dir->y*scaler ,dir->z*scaler
				);
	fflush(f);
}
	
static __inline void Ldr_Box(char const* name, unsigned int colour, float size, vec4_t const* pos, FILE* f)
{
	fprintf(f,
		"*Box %s %08X "
		"{ "
			"%f "
			"%s "
		"}\n"
		,name ,colour
		,size
		,Ldr_O2W(Ldr_Pos(pos))
		);
	fflush(f);
}
	
static __inline void Ldr_BoxLine(char const* name, unsigned int colour, float size, vec4_t const* s, vec4_t const* e, FILE* f)
{
	vec4_t tmp;

	vec4_sub(&tmp, e, s);
	size *= 0.5f;
	fprintf(f,
		"*Box %s %08X "
		"{ "
			"%f %f %f "
			"*Line ray %08X {0 0 0 %f %f %f} "
			"%s "
		"}\n"
		,name ,colour
		,size ,size ,size
		,colour
		,tmp.x*1.0f ,tmp.y*1.0f ,tmp.z*1.0f
		,Ldr_O2W(Ldr_Pos(s))
		);
	fflush(f);
}
	
static __inline void Ldr_BoxLineD(char const* name, unsigned int colour, float size, vec4_t const* s, vec4_t const* dir, float scaler, FILE* f)
{
	size *= 0.5f;
	fprintf(f,
		"*Box %s %08X "
		"{ "
			"%f %f %f "
			"*LineD ray %08X {0 0 0 %f %f %f} "
			"%s "
		"}\n"
		,name ,colour
		,size ,size ,size
		,colour
		,dir->x*scaler ,dir->y*scaler ,dir->z*scaler
		,Ldr_O2W(Ldr_Pos(s))
		);
	fflush(f);
}
	
static __inline void Ldr_Matrix(char const* name, unsigned int colour, float size, vec4_t const* pos, vec4_t const* quat, FILE* f)
{
	fprintf(f,
		"*Matrix3x3 %s %08X "
		"{ "
			"%f 0 0 "
			"0 %f 0 "
			"0 0 %f "
			"%s "
		"}\n"
		,name ,colour
		,size ,size ,size
		,Ldr_O2W2(Ldr_Pos(pos), Ldr_Quat(quat))
		);
	fflush(f);
}
	
static __inline void Ldr_Group(char const* name, unsigned int colour, FILE* f)
{
	fprintf(f,	"*Group %s %08X {\n" ,name ,colour);
}
	
static __inline void Ldr_GroupEnd(FILE* f)
{
	fprintf(f,	"}\n");
}
	
#endif
