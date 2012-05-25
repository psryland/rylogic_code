module prd.storage.settings;

import std.exception;
import std.file;
import std.conv;
import std.xml;

private template SerialiseMember(T)
{
	enum bool SerialiseMember = HasSerialisableFlag!(T);
}

private template HasSerialisableFlag(T)
{
	static if (__traits(hasMember, T, "Serialisable"))
		enum bool HasSerialisableFlag = true;
	else
		enum bool HasSerialisableFlag = false;
}

/// Used to mark fields for serialising
struct Serialise(T)
{
	enum Serialisable = true;
	T value;
	alias value this;
	string toString() { return to!string(value); }
}

/// Load user data from an xml file
void Load(T)(ref T data, string file)
{
	// Load the entire file into memory
	string contents = cast(string)std.file.read(file);
	check(contents); // well formed?

	// Make a document object model
	auto doc = new std.xml.Document(contents);
	//foreach (i, field; data.tupleof)
	//{
	//    static if (SerialiseMember!(typeof(field)))
	//    {
	//        std.stdio.writeln("Serialising: ("~to!string(i)~") "~to!string(field));
	//    }
	//}
}

/// Save user data to an xml file
void Save(T)(T data, string file)
{
	// Create a document object model
	//auto doc = new std.xml.Document(new std.xml.Tag("settings"));
	foreach (i, member; __traits(allMembers, T))
	{
		std.stdio.writeln(to!string(member));
		//auto field = data.tupleof[i];
		//static if (SerialiseMember!(typeof(field)))
		{
			{
				//doc ~= new std.xml.Element(member, to!string(field));
			}
		}
	}
	//std.stdio.writefln(std.string.join(doc.pretty(3),"\n"));
}

//unittest
//{
	class TestSettings
	{
		string           m_ignored;
		//Serialise!string m_string;
		
		@property int  Int() { return m_int; }
		@property void Int(int value) { m_int = value; }
		private int m_int;
		
		@property float Float() { return m_float; }
		@property void  Float(float value) { m_float = value; }
		private float m_float;
		
		this()
		{
			m_ignored = "ignored";
			m_int     = 4;
			//m_string  = "not ignored";
			m_float   = 6.28f;
		}
	}

	void RunTest()
	{
		string file = "settings.xml";
		scope(exit)
			if (std.file.exists(file))
				std.file.remove(file);

		TestSettings s1 = new TestSettings();
		s1.m_ignored = "ignored";
		s1.Int     = 4;
		//s1.m_string  = "not ignored";
		s1.Float   = 6.28f;
		Save(s1, file);

		//TestSettings s2;
		//Load(s2, file);
	}
//}
