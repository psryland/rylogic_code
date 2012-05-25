//*********************************************
// ImagerN
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "imagern/main/stdafx.h"
#include "imagern/main/photo_model.h"
#include "imagern/media/media_file.h"

Photo::Photo(pr::Renderer& rdr)
:m_rdr(rdr)
,m_tex()
,m_vid()
,m_media_type(EMedia::Unknown)
{
	m_i2w = pr::m4x4Identity;
	m_colour = pr::Colour32One;
	m_model = m_rdr.m_mdl_mgr.CreateModel(pr::rdr::model::Settings(6, 4));
}
	
// Returns the width, height, and, aspect ratio of the current photo/video
size_t Photo::Width() const
{
	if (m_vid) return m_vid->m_info.Width;
	if (m_tex) return m_tex->m_info.Width;
	return 0;
}
size_t Photo::Height() const
{
	if (m_vid) return m_vid->m_info.Height;
	if (m_tex) return m_tex->m_info.Height;
	return 0;
}
float Photo::Aspect() const
{
	return (Width() == 0 || Height() == 0) ? 1.0f : Width() / (float)Height();
}

// Update the model with a new media file
void Photo::Update(EMedia::Type media_type, string const& path)
{
	// Release previous media
	m_tex = 0;
	m_vid = 0;
	
	switch (m_media_type = media_type)
	{
	default: PR_ASSERT(DBG, false, "Update with unknown media type"); break;
	case EMedia::Image:
		m_tex = m_rdr.m_mat_mgr.CreateTexture(pr::rdr::AutoId, path.c_str());
		break;
	case EMedia::Video:
	case EMedia::Audio:
		m_vid = m_rdr.m_mat_mgr.CreateVideoTexture(pr::rdr::AutoId, path.c_str());
		break;
	}
	
	// Update the model geometry based on the texture size
	float w = 0.0f, h = 0.0f;
	float aspect = Aspect();
	if (aspect >= 1.0f) { w = 1.0f; h = 1.0f / aspect; } else { w = aspect; h = 1.0f; }
	
	pr::rdr::model::MLock mlock(m_model);
	pr::rdr::vf::iterator v = mlock.VPtr();
	v->set(pr::v4::make(-w,  h, 0.0f, 1.0f), pr::v4ZAxis, pr::Colour32White, pr::v2::make(0.001f, 0.001f)); ++v;
	v->set(pr::v4::make(-w, -h, 0.0f, 1.0f), pr::v4ZAxis, pr::Colour32White, pr::v2::make(0.001f, 0.999f)); ++v;
	v->set(pr::v4::make( w, -h, 0.0f, 1.0f), pr::v4ZAxis, pr::Colour32White, pr::v2::make(0.999f, 0.999f)); ++v;
	v->set(pr::v4::make( w,  h, 0.0f, 1.0f), pr::v4ZAxis, pr::Colour32White, pr::v2::make(0.999f, 0.001f)); ++v;
	pr::rdr::Index* i = mlock.IPtr();
	*i++ = 3;
	*i++ = 0;
	*i++ = 1;
	*i++ = 1;
	*i++ = 2;
	*i++ = 3;
	
	pr::rdr::Material mat = m_rdr.m_mat_mgr.GetMaterial(pr::geom::EVNCT);
	switch (media_type)
	{
	case EMedia::Image: mat.m_diffuse_texture = m_tex; break;
	case EMedia::Video: mat.m_diffuse_texture = m_vid; break;
	case EMedia::Audio: mat.m_diffuse_texture = 0; break;
	}
	m_model->SetMaterial(mat, pr::rdr::model::EPrimitive::TriangleList, true);
}


