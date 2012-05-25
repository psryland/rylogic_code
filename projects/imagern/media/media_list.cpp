//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************

#include "imagern/main/stdafx.h"
#include "imagern/media/media_list.h"
#include "imagern/media/media_file.h"
#include "imagern/settings/user_settings.h"

MediaList::MediaList(UserSettings const& settings)
:m_db(settings.m_db_path.c_str())
{
	// Ensure the tables needed by the media list are in the database
	m_db.CreateTable<MediaFile>();
	m_db.CreateTable<SearchDir>();
}

MediaList::~MediaList()
{
	Cancel();
	Join();
}

// Crawler background thread
void MediaList::Main(void*)
{
	while (!Cancelled())
	{

	}
}
