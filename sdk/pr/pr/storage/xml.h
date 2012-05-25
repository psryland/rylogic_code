//*****************************************
// XmlLite wrapper
//	(c)opyright Paul Ryland 2009
//*****************************************
// Required lib: xmllite.lib
//
#ifndef PR_XML_LITE_WRAPPER_H
#define PR_XML_LITE_WRAPPER_H
#pragma once

#include <atlbase.h>
#include <xmllite.h> // import xmllite.lib shlwapi.lib
#include <vector>
#include <string>
#include <locale>
#include <sstream>
#include <exception>
#include <algorithm>

namespace pr
{
	namespace xml
	{
		enum EProperty
		{
			EProperty_MultiLanguage			= 1 << XmlWriterProperty_MultiLanguage,
			EProperty_Indent				= 1 << XmlWriterProperty_Indent,
			EProperty_ByteOrderMark			= 1 << XmlWriterProperty_ByteOrderMark,
			EProperty_OmitXmlDeclaration	= 1 << XmlWriterProperty_OmitXmlDeclaration,
			EProperty_ConformanceLevel		= 1 << XmlWriterProperty_ConformanceLevel,
		};

		// forward declarations
		typedef int HashValue;
		struct Attr;
		struct Node;
		typedef std::vector<std::wstring>	StrVec;
		typedef std::vector<Attr>			AttrVec;
		typedef std::vector<Node>			NodeVec;

		// Convert a wide string into a hash code (crc32 algorithm)
		inline HashValue Hash(std::wstring const& str)
		{
			unsigned char const* ptr = reinterpret_cast<unsigned char const*>(str.c_str());
			unsigned char const* end = ptr + str.size()*sizeof(*str.c_str());
			unsigned int crc = 0xFFFFFFFF;
			for (; ptr != end; ++ptr)
			{
				unsigned char b = static_cast<unsigned char>(crc ^ *ptr);
				unsigned int value = 0xFF ^ b;
				for (int j = 8; j > 0; --j) { value = (value >> 1) ^ ((value & 1) ? 0xEDB88320 : 0); }
				value = value ^ 0xFF000000;
				crc = value ^ (crc >> 8);
			}
			// using 'HashValue' as a signed int so that enum's declared equal to this
			// hash values do not have signed/unsigned warning problems
			return reinterpret_cast<HashValue const&>(crc);
		}

		// helpers for converting things to std::wstring
		inline std::wstring const& str(std::wstring const& s)	{ return s; }
		inline std::wstring str(std::string const& s)			{ return std::wstring(s.begin(), s.end()); }
		inline std::wstring str(char const* s)					{ return std::wstring(s, s + strlen(s)); }
		template <typename T> inline std::wstring str(T v)		{ std::wstringstream s; s << v; return s.str(); }

		struct Attr
		{
			std::wstring	m_prefix;
			std::wstring	m_localname;
			std::wstring	m_value;
			Attr(std::wstring const& prefix, std::wstring const& localname, std::wstring const& value) :m_prefix(prefix) ,m_localname(localname) ,m_value(value) {}
		};

		struct Node
		{
			std::wstring	m_prefix;		// The tag prefix for the element
			std::wstring	m_tag;			// The tag for the element
			std::wstring	m_value;		// The value of this element
			bool			m_cdata;		// true if m_value is literal data [CDATA[...]]
			NodeVec			m_child;		// Child elements
			AttrVec			m_attr;			// Attributes
			AttrVec			m_proc_instr;	// Processing instructions
			StrVec			m_comments;		// Comments

			Node()
			:m_tag		(L"root")
			,m_cdata	(false)
			{}
			template <typename Str1>
			explicit Node(Str1 const& tag)
			:m_prefix	()
			,m_tag		(str(tag))
			,m_value	()
			,m_cdata	(false)
			{}
			template <typename Str1, typename Str2>
			Node(Str1 const& tag, Str2 const& value)
			:m_prefix	()
			,m_tag		(str(tag))
			,m_value	(str(value))
			,m_cdata	(false)
			{}
			template <typename Str1, typename Str2, typename Str3>
			Node(Str1 const& prefix, Str2 const& tag, Str3 const& value, bool cdata = false)
			:m_prefix	(str(prefix))
			,m_tag		(str(tag))
			,m_value	(str(value))
			,m_cdata	(cdata)
			{}

			template <typename Type>	Type			as() const;
			template <>					bool			as() const { return _wcsicmp(m_value.c_str(), L"true") == 0 || _wtoi(m_value.c_str()) != 0; }
			template <>					char			as() const { return static_cast<char>(as<int>()); }
			template <>					unsigned char	as() const { return static_cast<unsigned char>(as<unsigned int>()); }
			template <>					short			as() const { return static_cast<short>(as<int>()); }
			template <>					unsigned short	as() const { return static_cast<unsigned short>(as<unsigned int>()); }
			template <>					int				as() const { return _wtoi(m_value.c_str()); }
			template <>					unsigned int	as() const { return static_cast<unsigned int>(_wtoi(m_value.c_str())); }
			template <>					float			as() const { return static_cast<float>(_wtof(m_value.c_str())); }
			template <>					double			as() const { return _wtof(m_value.c_str()); }
			template <>					std::string		as() const { return std::string(m_value.begin(), m_value.end()); }
			
			NodeVec::const_iterator		begin() const	{ return m_child.begin(); }
			NodeVec::const_iterator		end() const		{ return m_child.end(); }
			NodeVec::iterator			begin()			{ return m_child.begin(); }
			NodeVec::iterator			end()			{ return m_child.end(); }

			std::wstring				tag() const		{ return m_prefix + (m_prefix.empty() ? L"" : L":") + m_tag; }
			HashValue					hash() const	{ return Hash(tag()); }

			Node& add(Node const& node)					{ m_child.push_back(node); return m_child.back(); }
		};
	
		namespace impl
		{
			// Parse xml tag attributes
			template <typename Node>
			void ParseAttributes(ATL::CComPtr<IXmlReader>& reader, Node& node)
			{
				// Parse all attributes
				HRESULT hr;
				for (hr = reader->MoveToFirstAttribute(); hr == S_OK; hr = reader->MoveToNextAttribute())
				{
					if (reader->IsDefault())
						continue;

					// e.g. <prefix:localname="value"/>
					WCHAR const *prefix, *localname, *value;
					if (FAILED(hr = reader->GetPrefix   (&prefix,    0))) throw hr;
					if (FAILED(hr = reader->GetLocalName(&localname, 0))) throw hr;
					if (FAILED(hr = reader->GetValue    (&value,     0))) throw hr;
					node.m_attr.push_back(Attr(prefix, localname, value));
				}
			}

			// Parse an ending xml tag
			template <typename Node>
			void ParseEndElement(ATL::CComPtr<IXmlReader>& reader, Node& node)
			{
				HRESULT hr;

				WCHAR const *prefix, *localname;
				if (FAILED(hr = reader->GetPrefix   (&prefix,    0))) throw hr;
				if (FAILED(hr = reader->GetLocalName(&localname, 0))) throw hr;
				node.m_prefix = prefix;
				node.m_tag = localname;
			}

			// Parse a string value between tags
			template <typename Node>
			void ParseValue(ATL::CComPtr<IXmlReader>& reader, Node& node)
			{
				HRESULT hr;

				WCHAR const* value;
				if (FAILED(hr = reader->GetValue(&value, 0))) throw hr;
				node.m_value = value;
			}

			// Parse a processing instruction
			template <typename Node>
			void ParseProcessingInstruction(ATL::CComPtr<IXmlReader>& reader, Node& node)
			{
				HRESULT hr;

				WCHAR const *localname, *value;
				if (FAILED(hr = reader->GetLocalName(&localname, 0))) throw hr;
				if (FAILED(hr = reader->GetValue    (&value,     0))) throw hr;
				node.m_proc_instr.push_back(Attr(L"", localname, value));
			}

			// Parse an xml element 
			template <typename Node>
			void ParseComment(ATL::CComPtr<IXmlReader>& reader, Node& node)
			{
				HRESULT hr;

				WCHAR const* value;
				if (FAILED(hr = reader->GetValue(&value, 0))) throw hr;
				node.m_comments.push_back(value);
			}

			// Parse an xml element 
			template <typename Node>
			void ParseElement(ATL::CComPtr<IXmlReader>& reader, Node& node, bool top_level)
			{
				if (reader->IsEmptyElement())
					return ParseEndElement(reader, node);

				HRESULT hr;
				XmlNodeType xml_node;
				for (hr = reader->Read(&xml_node); hr == S_OK; hr = reader->Read(&xml_node))
				{
					switch (xml_node)
					{
					default: break;
					case XmlNodeType_XmlDeclaration:
						ParseAttributes(reader, node);
						break;
					case XmlNodeType_Element:
						if (top_level)
							ParseElement(reader, node, false);
						else
						{
							Node child;
							ParseElement(reader, child, false);
							node.m_child.push_back(child);
						}break;
					case XmlNodeType_EndElement:
						ParseEndElement(reader, node);
						return;
					case XmlNodeType_Text:
					case XmlNodeType_CDATA:
						ParseValue(reader, node);
						break;
					case XmlNodeType_ProcessingInstruction:
						ParseProcessingInstruction(reader, node);
						break;
					case XmlNodeType_Comment:
						ParseComment(reader, node);
						break;
					case XmlNodeType_Whitespace:
						break;
					case XmlNodeType_DocumentType:
						break;
					}
				}
				if (hr != S_FALSE)
					throw hr;
			}

			// Write an xml element 
			template <typename Node>
			void WriteElement(ATL::CComPtr<IXmlWriter>& writer, Node& node)
			{
				HRESULT hr;

				// Write the comments about the element
				for (StrVec::const_iterator i = node.m_comments.begin(), i_end = node.m_comments.end(); i != i_end; ++i)
					if (FAILED(hr = writer->WriteComment(i->c_str())))
						throw hr;

				// Write the processing instructions
				for (AttrVec::const_iterator i = node.m_proc_instr.begin(), i_end = node.m_proc_instr.end(); i != i_end; ++i)
					if (FAILED(hr = writer->WriteProcessingInstruction(i->m_localname.c_str(), i->m_value.c_str())))
						throw hr;

				// Begin the element
				if (FAILED(hr = writer->WriteStartElement(0, node.m_tag.c_str(), 0))) throw hr;
	    
				// Write the attributes
				for (AttrVec::const_iterator i = node.m_attr.begin(), i_end = node.m_attr.end(); i != i_end; ++i)
					if (FAILED(hr = writer->WriteAttributeString(i->m_prefix.c_str(), i->m_localname.c_str(), 0, i->m_value.c_str())))
						throw hr;

				if (node.m_child.empty())
				{
					// Write the value
					if (!node.m_cdata)
					{
						if (FAILED(hr = writer->WriteString(node.m_value.c_str())))
							throw hr;
					}
					else
					{
						if (FAILED(hr = writer->WriteCData(node.m_value.c_str())))
							throw hr;
					}
				}
				else
				{
					// Write the children
					for (NodeVec::const_iterator i = node.m_child.begin(), i_end = node.m_child.end(); i != i_end; ++i)
						WriteElement(writer, *i);
				}

				// End the element
				if (FAILED(hr = writer->WriteEndElement())) throw hr;
			}
		}//namespace impl

		// Parse xml data from a stream generating a 'Node' tree
		inline HRESULT Load(ATL::CComPtr<IStream>& stream, Node& out)
		{
			try
			{ 
				ATL::CComPtr<IXmlReader> reader;

				HRESULT hr;
				if (FAILED(hr = CreateXmlReader(__uuidof(IXmlReader), (void**)&reader, 0)))
					throw hr;

				// Set it as the stream source for the reader
				if (FAILED(hr = reader->SetInput(stream)))
					throw hr;

				impl::ParseElement(reader, out, true);
				return S_OK;
			}
			catch (HRESULT ex)
			{
				return ex;
			}
		}
		inline HRESULT Load(char const* filename, Node& out)
		{
			// Create an 'IStream' from a file
			ATL::CComPtr<IStream> stream;
			HRESULT hr = SHCreateStreamOnFileA(filename, STGM_READ, &stream);
			return FAILED(hr) ? hr : Load(stream, out);
		}
		inline HRESULT Load(wchar_t const* filename, Node& out)
		{
			// Create an 'IStream' from a file
			ATL::CComPtr<IStream> stream;
			HRESULT hr = SHCreateStreamOnFileW(filename, STGM_READ, &stream);
			return FAILED(hr) ? hr : Load(stream, out);
		}
		inline HRESULT Load(char const* xml_string, std::size_t length, Node& out)
		{
			// Create an 'IStream' from a string
			ATL::CComPtr<IStream> stream = SHCreateMemStream((BYTE const*)xml_string, (UINT)length);
			return Load(stream, out);
		}

		// Save data in an xml format
		// 'properties' is a bitwise combination of 'EProperty' (note, only some supported)
		inline HRESULT Save(ATL::CComPtr<IStream>& stream, Node const& in, int properties = EProperty_Indent)
		{
			try
			{
				ATL::CComPtr<IXmlWriter> writer;

				HRESULT hr;
				if (FAILED(hr = CreateXmlWriter(__uuidof(IXmlWriter), (void**)&writer, 0)))
					throw hr;

				// Set it as the stream source for the writer
				if (FAILED(hr = writer->SetOutput(stream)))
					throw hr;

				if (properties & EProperty_Indent) 
					if (FAILED(hr = writer->SetProperty(XmlWriterProperty_Indent, TRUE)))
						throw hr;
				if (properties & EProperty_ByteOrderMark) 
					if (FAILED(hr = writer->SetProperty(XmlWriterProperty_ByteOrderMark, TRUE)))
						throw hr;
				if (properties & EProperty_OmitXmlDeclaration) 
					if (FAILED(hr = writer->SetProperty(XmlWriterProperty_OmitXmlDeclaration, TRUE)))
						throw hr;
		
				// Start the document
				if (FAILED(hr = writer->WriteStartDocument(XmlStandalone_Omit))) throw hr;

				impl::WriteElement(writer, in);

				// End the document
				if (FAILED(hr = writer->WriteEndDocument())) throw hr;
				if (FAILED(hr = writer->Flush())) throw hr;
				return S_OK;
			}
			catch (HRESULT ex)
			{
				return ex;
			}
		}
		inline HRESULT Save(char const* filename, Node const& in, int properties = EProperty_Indent)
		{
			// Create an 'IStream' from a file
			ATL::CComPtr<IStream> stream;
			HRESULT hr = SHCreateStreamOnFileA(filename, STGM_WRITE|STGM_CREATE, &stream);
			return FAILED(hr) ? hr : Save(stream, in, properties);
		}
		inline HRESULT Save(wchar_t const* filename, Node const& in, int properties = EProperty_Indent)
		{
			// Create an 'IStream' from a file
			ATL::CComPtr<IStream> stream;
			HRESULT hr = SHCreateStreamOnFileW(filename, STGM_WRITE|STGM_CREATE, &stream);
			return FAILED(hr) ? hr : Save(stream, in, properties);
		}

		// Test for a node equal to a hash value
		inline bool operator == (Node const& lhs, HashValue rhs)	{ return lhs.hash() == rhs; }
		inline bool operator == (HashValue lhs, Node const& rhs)	{ return lhs == rhs.hash(); }
		inline bool operator != (Node const& lhs, HashValue rhs)	{ return lhs.hash() != rhs; }
		inline bool operator != (HashValue lhs, Node const& rhs)	{ return lhs != rhs.hash(); }

		// Returns true if the prefix and tag in 'node' match 'prefix_tag'
		// 'prefix_tag' should have the format "prefix:tag".
		// If the node has no prefix then just the tag is compared and 'prefix_tag' should have the format "tag".
		inline bool operator == (Node const& node, std::wstring const prefix_tag)	{ return prefix_tag.compare(node.tag()) == 0; }
		inline bool operator != (Node const& node, std::wstring const prefix_tag)	{ return !(node == prefix_tag); }

		// Find a child node within 'node' that matches 'prefix:tag'
		inline Node const* Find(Node const& node, HashValue hash)
		{
			NodeVec::const_iterator iter = std::find(node.m_child.begin(), node.m_child.end(), hash);
			return iter != node.m_child.end() ? &*iter : 0;
		}
		inline Node const* Next(Node const& node, Node const* prev, HashValue hash)
		{
			if (prev == 0 || node.m_child.empty()) return 0;
			Node const* end = &node.m_child[0] + node.m_child.size();
			Node const* iter = std::find(prev + 1, end, hash);
			return iter != end ? iter : 0;
		}
		inline Node const* Find(Node const& node, Node const* prev, std::wstring const& tag, std::wstring const& prefix = L"")
		{
			return Next(node, prev, Hash(prefix + (prefix.empty() ? L"" : L":") + tag));
		}

		// Return a named child node of 'parent'
		// Throws an HRESULT if the child is not found
		inline Node const& Child(Node const& parent, HashValue hash)
		{
			Node const* node = Find(parent, hash);
			if (node == 0) throw ERROR_NOT_FOUND;
			return *node;
		}
	}
}

#endif
