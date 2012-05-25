//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************
#pragma once
#ifndef IMAGERN_EVENTS_H
#define IMAGERN_EVENTS_H

#include "imagern/main/forward.h"

// Event generated when a media file is set as currently displayed
struct Event_MediaSet
{
	MediaFile const* m_mf;      // The media file object
	pr::uint m_width, m_height; // Dimensions of the media file
	Event_MediaSet(MediaFile const* mf, pr::uint width, pr::uint height) :m_mf(mf) ,m_width(width) ,m_height(height) {}
};

// Event generated when an error or status message needs to be displayed
struct Event_Message
{
	enum ELevel { Info, Warning, Error };
	char const* m_msg;
	ELevel      m_lvl;
	Event_Message(char const* msg, ELevel lvl) :m_msg(msg) ,m_lvl(lvl) {}
};

#endif
