//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************
#pragma once
#ifndef IMAGERN_PHOTO_MODEL_H
#define IMAGERN_PHOTO_MODEL_H

#include "imagern/main/forward.h"
#include "imagern/media/media_file.h"

// An instance for passing to the renderer
PR_RDR_DECLARE_INSTANCE_TYPE4
(
	ImgInstance,
	pr::rdr::ModelPtr  ,m_model        ,pr::rdr::instance::ECpt_ModelPtr,
	pr::m4x4           ,m_i2w          ,pr::rdr::instance::ECpt_I2WTransform,
	pr::rdr::rs::Block ,m_render_state ,pr::rdr::instance::ECpt_RenderState,
	pr::Colour32       ,m_colour       ,pr::rdr::instance::ECpt_TintColour32
);

// An instance of a photo
struct Photo :ImgInstance
{
	pr::Renderer&       m_rdr;
	pr::rdr::TexturePtr m_tex;
	pr::rdr::TexturePtr m_vid;
	EMedia::Type        m_media_type;
	
	Photo(pr::Renderer& rdr);
	
	// Returns the width, height, and, aspect ratio of the current photo/video (1 for audio)
	size_t Width() const;
	size_t Height() const;
	float  Aspect() const;
	
	// Update the model with a new media file
	void Update(EMedia::Type media_type, string const& path);
	
	// Return the type of media currently displayed
	EMedia::Type MediaType() const { return m_media_type; }
	
private:
	Photo(Photo const&);
	Photo& operator =(Photo const&);
};


#endif
