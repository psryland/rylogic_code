//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************
#pragma once
#ifndef IMAGERN_MEDIA_LIST_H
#define IMAGERN_MEDIA_LIST_H

#include "imagern/main/forward.h"
#include "pr/storage/sqlite.h"
#include "pr/threads/thread.h"

// This object provides an interface to the media list and also contains a background
// thread for compiling the media file database
class MediaList :pr::threads::Thread<MediaList>
{
	pr::sqlite::Database m_db;
	
	// Worker thread entry point.
	void Main(void*);
	
public:
	explicit MediaList(UserSettings const& settings);
	~MediaList();
};

#endif
