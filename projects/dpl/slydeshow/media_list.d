module media_list;

import std.concurrency;
import etc.c.sqlite3;
import prd.db.sqlite;

//extern (Windows) HRESULT SHGetKnownFolderPath(in REFKNOWNFOLDERID rfid, in DWORD dwFlags, in_opt HANDLE hToken, out PWSTR *ppszPath);

///// 
//public Tid RunMediaListBuilder()
//{
//    return 0;
//}

/// A class managing a thread that builds a database of available media files
public class MediaList
{
	private string m_dbfile;
	private scope SqliteDatabase m_db;
	
	public this()
	{
		m_dbfile = "medialist.db";
		m_db = new SqliteDatabase(m_dbfile);
	}
}

