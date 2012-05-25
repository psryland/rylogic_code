//******************************************************************
//
//	Nugget implementation
//
//******************************************************************
#ifndef PR_NUGGET_FILE_NUGGET_IMPL_H
#define PR_NUGGET_FILE_NUGGET_IMPL_H

#include "pr/common/byte_data.h"
#include "pr/str/prstring.h"
#include "pr/filesys/filesys.h"
#include "pr/filesys/fileex.h"
#include "pr/storage/nugget_file/nuggetfileassertenable.h"
#include "pr/storage/nugget_file/types.h"

namespace pr
{
	namespace nugget
	{
		// Each nugget has a header and a block of data that follows immediately after the header
		struct Header
		{
			enum { MaxDescriptionLength = 64 };
			std::size_t	m_four_cc;								// A fourCC identifier used to verify that this is nugget data
			std::size_t	m_id;									// A fourCC identifier used to identifier this nugget type
			std::size_t	m_version;								// A version for the data within this nugget
			std::size_t	m_user_flags;							// User defined flags. Information about the data in this nugget.
			char		m_description[MaxDescriptionLength];	// Description of the data of this nugget. (helpful for avoiding unnecessary decompression of the data)
			std::size_t	m_data_start;							// The number of bytes from the start of this header to the data
			std::size_t	m_data_length;							// The length of the data that follows this nugget

			void CopyDescription(const char* description)
			{
				memset(m_description, 0, MaxDescriptionLength);
				str::Assign(description, m_description, MaxDescriptionLength);
			}
			static Header Construct(std::size_t four_cc, std::size_t id, std::size_t user_flags, std::size_t version, const char* description)
			{
				Header header;
				header.m_four_cc		= four_cc;
				header.m_id				= id;
				header.m_version		= version;
				header.m_user_flags		= user_flags;
				header.CopyDescription	(description);
				header.m_data_start		= sizeof(Header);
				header.m_data_length	= 0;
				return header;
			}
		};

		// A single nugget including header and data
		template <typename T>
		class NuggetImpl
		{
		public:
			NuggetImpl();																		// Default nugget
			// This is an empty nugget, start with this when making a nugget
			NuggetImpl(std::size_t id, std::size_t version, std::size_t user_flags, const char* description);	// Construct empty nugget
			// Construct a nugget from a pointer to nugget data. This throws an exception 
			// if 'data' does not point to a nugget. The size of the data is given in the
			// nugget header therefore no 'data_size' parameter is needed.
			NuggetImpl(const void* data, ECopyFlag copy_flag);									// Construct from nugget data
			// Construct a nugget from a source of nugget data. Same as above
			NuggetImpl(const ISrc& src, std::size_t offset, ECopyFlag copy_flag);				// Construct from nugget data
			// Copy a nugget
			NuggetImpl(const NuggetImpl<T>& copy);

			void		Initialise(std::size_t id, std::size_t version, std::size_t user_flags, const char* description);
			EResult		Initialise(const void* data, ECopyFlag copy_flag);
			EResult		Initialise(const ISrc& src, std::size_t offset, ECopyFlag copy_flag);
			EResult		Save(IDest& dst, std::size_t& offset) const;

			std::size_t	GetId() const					{ return m_header.m_id; }
			std::size_t	GetVersion() const				{ return m_header.m_version; }
			std::size_t	GetUserFlags() const			{ return m_header.m_user_flags; }
			const char*	GetDescription() const			{ return m_header.m_description; }
			std::size_t	GetNuggetSizeInBytes() const	{ return sizeof(Header) + GetDataSize(); }
			std::size_t	GetDataSize() const				{ return m_header.m_data_length; }
			EResult		GetData(IDest& dst, std::size_t offset) const { return GetData(0, GetDataSize(), dst, offset); }
			EResult		GetData(std::size_t first, std::size_t last, IDest& dst, std::size_t offset) const;
			const void* GetData() const;
			
			EResult		GetChildNugget(std::size_t offset, NuggetImpl<T>& child, ECopyFlag copy_flag) const;
		
			void		Reserve(std::size_t data_size);	// Buffered data only
			EResult		SetData(const void* data, std::size_t data_size, ECopyFlag copy_flag);
			EResult		SetData(const ISrc& src, std::size_t offset, std::size_t data_size, ECopyFlag copy_flag);
			EResult		SetData(const char* external_filename, ECopyFlag copy_flag);
			EResult		SetData(const NuggetImpl<T>& nugget);
			EResult		AppendData(const void* data, std::size_t data_size, ECopyFlag copy_flag);
			EResult		AppendData(const ISrc& src, std::size_t offset, std::size_t data_size, ECopyFlag copy_flag);
			EResult		AppendData(const NuggetImpl<T>& nugget, ECopyFlag copy_flag);
			void		DeleteData();

		private:
			enum
			{
				Version				= 1,
				NuggetDataHeaderID	= ('N' << 0) | ('G' << 8) | ('T' << 16) | (Version << 24),
				BlockCopySize		= 4096
			};
			enum EDataRef
			{
				EDataRef_NoData,			// No data assigned to the nugget
				EDataRef_Source,			// Data is referenced by an ISrc interface
				EDataRef_Referenced,		// Data is referenced directly via a const void*
				EDataRef_Buffered,			// Data is buffered internally in a ByteCont
				EDataRef_TempFile,			// Data is buffered internally using a temp file
				EDataRef_ExternalFile		// Data is contained in an external file
			};
			struct SourceData
			{
				void clear()	{ m_src = 0; m_base = 0; }
				const ISrc*	m_src;			// The base 'pointer' to the source data
				std::size_t	m_base;			// The initial offset with the source data
			};
			bool IsNuggetDataHeader    (std::size_t four_cc) const	{ return (four_cc & 0x00FFFFFF) == (NuggetDataHeaderID & 0x00FFFFFF); }
			bool IsNuggetDataFooter    (std::size_t four_cc) const	{ return four_cc == NuggetDataFooterID; }
			bool IsCorrectNuggetVersion(std::size_t four_cc) const	{ return (four_cc >> 24) == Version; }
			EResult OpenTempFile();
			EResult Copy(const ISrc& src, std::size_t src_offset, IDest& dst, std::size_t dst_offset) const;
			EResult Copy(HANDLE src, IDest& dst, std::size_t dst_offset) const;
			EResult Copy(const ISrc& src, std::size_t src_offset, HANDLE dst) const;
			EResult Copy(HANDLE src, HANDLE dst) const;

		private:
			Header			m_header;		// Header for the data. Always reflects the state of the data in this nugget
			EDataRef		m_data_ref;		// How we are referencing the data
			SourceData		m_src;			// Source data and offset
			ByteCont		m_buffer;		// Buffered data
			const uint8*	m_ref;			// A pointer to some external data
			HANDLE			m_file;			// Temporary file containing buffered data
			std::string		m_ext_filename;	// The filename of an external file
		};
			
		//*****************************************************************
		// NuggetImpl methods
		//*****
		// Construct a default nugget
		template <typename T>
		NuggetImpl<T>::NuggetImpl()
		:m_header(Header::Construct(NuggetDataHeaderID, 0, 0, 0, ""))
		,m_data_ref(EDataRef_NoData)
		{}

		//*****
		// Construct an empty nugget
		template <typename T>
		NuggetImpl<T>::NuggetImpl(std::size_t id, std::size_t version, std::size_t user_flags, const char* description)
		:m_header(Header::Construct(NuggetDataHeaderID, id, version, user_flags, description))
		,m_data_ref(EDataRef_NoData)
		{}

		//*****
		// Construct a nugget from existing data
		template <typename T>
		NuggetImpl<T>::NuggetImpl(const void* data, ECopyFlag copy_flag)
		:m_header(Header::Construct(NuggetDataHeaderID, 0, 0, 0, ""))
		,m_data_ref(EDataRef_NoData)
		{
			EResult result = Initialise(data, copy_flag);
			if( Failed(result) ) throw Exception(result);
		}

		//*****
		// Construct from 'src' data
		template <typename T>
		NuggetImpl<T>::NuggetImpl(const ISrc& src, std::size_t offset, ECopyFlag copy_flag)
		:m_header(Header::Construct(NuggetDataHeaderID, 0, 0, 0, ""))
		,m_data_ref(EDataRef_NoData)
		{
			EResult result = Initialise(src, offset, copy_flag);
			if( Failed(result) ) throw Exception(result);
		}

		//*****
		// Copy Constructor
		template <typename T>
		NuggetImpl<T>::NuggetImpl(const NuggetImpl<T>& copy)
		:m_header(copy.m_header)
		,m_data_ref(copy.m_data_ref)
		{
			switch( m_data_ref )
			{
			case EDataRef_NoData:		break;
			case EDataRef_Source:		m_src = copy.m_src; break;
			case EDataRef_Referenced:	m_ref = copy.m_ref; break;
			case EDataRef_Buffered:		m_buffer = copy.m_buffer; break;
			case EDataRef_TempFile:
				{
					EResult result = OpenTempFile(); if( Failed(result) ) throw Exception(result);

					// Copy the contents of the temporary file
					SetFilePointer(copy.m_file, 0, 0, FILE_BEGIN);
					char buffer[BlockCopySize];
					DWORD bytes_read;
					do
					{
						FileRead(copy.m_file, buffer, BlockCopySize, &bytes_read);
						if (FileWrite(m_file, buffer, bytes_read)) { throw Exception(EResult_WriteToTempFileFailed); }
					}
					while( bytes_read == BlockCopySize );
				}break;
			case EDataRef_ExternalFile:	m_ext_filename = copy.m_ext_filename; break;
			default: PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Unknown data ref type");
			};
		}

		//*****
		// Initialise this as an empty nugget
		template <typename T>
		void NuggetImpl<T>::Initialise(std::size_t id, std::size_t version, std::size_t user_flags, const char* description)
		{
			DeleteData();
			m_header = Header::Construct(NuggetDataHeaderID, id, version, user_flags, description);
		}

		//*****
		// Initialise this nugget with some data, the data is assumed to point to nugget data
		template <typename T>
		EResult NuggetImpl<T>::Initialise(const void* data, ECopyFlag copy_flag)
		{
			DeleteData();
			const uint8* bdata = static_cast<const uint8*>(data);

			// Read in the header
			m_header = *reinterpret_cast<const Header*>(data);
			if( !IsNuggetDataHeader    (m_header.m_four_cc) ) { return EResult_NotNuggetData; }
			if( !IsCorrectNuggetVersion(m_header.m_four_cc) ) { return EResult_IncorrectNuggetFileVersion; } 

			// Set up the data
			return SetData(bdata + m_header.m_data_start, m_header.m_data_length, copy_flag);
		}

		//*****
		// Initialise this nugget from a data source
		template <typename T>
		EResult NuggetImpl<T>::Initialise(const ISrc& src, std::size_t offset, ECopyFlag copy_flag)
		{
			DeleteData();

			// Read in the header
			if( src.Read(&m_header, sizeof(m_header), offset) != sizeof(m_header) )	{ return EResult_FailedToReadSourceData; }
			if( !IsNuggetDataHeader    (m_header.m_four_cc) )						{ return EResult_NotNuggetData; }
			if( !IsCorrectNuggetVersion(m_header.m_four_cc) )						{ return EResult_IncorrectNuggetFileVersion; } 

			// Set up the data
			return SetData(src, offset + m_header.m_data_start, m_header.m_data_length, copy_flag);
		}

		//*****
		// Save this nugget and its data to 'dst'
		template <typename T>
		EResult NuggetImpl<T>::Save(IDest& dst, std::size_t& offset) const
		{
			// Write the header
			if( dst.Write(&m_header, sizeof(m_header), offset) != sizeof(m_header) ) { return EResult_WriteToDestFailed; }
			offset += sizeof(m_header);

			// Write the data
			EResult result = GetData(dst, offset);
			if( Failed(result) ) { return EResult_WriteToDestFailed; }
			offset += m_header.m_data_length;
			
			return EResult_Success;
		}

		//*****
		// Copy a range of data from this nugget into "&dst[offset]"
		template <typename T>
		EResult NuggetImpl<T>::GetData(std::size_t first, std::size_t last, IDest& dst, std::size_t offset) const
		{
			first; last;
			PR_ASSERT(PR_DBG_NUGGET_FILE, first == 0 && last == GetDataSize(), "GetData by range not implemented yet!");
			switch( m_data_ref )
			{
			case EDataRef_NoData:		return EResult_Success;
			case EDataRef_Source:		return Copy(*m_src.m_src, m_src.m_base, dst, offset);
			case EDataRef_Referenced:	return (dst.Write(m_ref, GetDataSize(), offset) == GetDataSize()) ? (EResult_Success) : (EResult_WriteToDestFailed);
			case EDataRef_Buffered:		return (dst.Write(&m_buffer[0], GetDataSize(), offset) == GetDataSize()) ? (EResult_Success) : (EResult_WriteToDestFailed);
			case EDataRef_TempFile:		return Copy(m_file, dst, offset);
			case EDataRef_ExternalFile:
				{
					Handle ext_file = FileOpen(m_ext_filename.c_str(), GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL|FILE_FLAG_SEQUENTIAL_SCAN, 0);
					if (ext_file == INVALID_HANDLE_VALUE) { return EResult_ReadingExternalFileFailed; }
					return Copy(ext_file, dst, offset);
				}
			default:
				PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Unknown data ref type");
				return EResult_Failed;
			};
		}

		//*****
		// Return const access to the data
		template <typename T>
		const void* NuggetImpl<T>::GetData() const
		{
			switch( m_data_ref )
			{
			case EDataRef_NoData:		return 0;
			case EDataRef_Source:		return m_src.m_src->GetData(m_src.m_base);
			case EDataRef_Referenced:	return m_ref;
			case EDataRef_Buffered:		return &m_buffer[0];
			case EDataRef_TempFile:		
			case EDataRef_ExternalFile:	PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Unable to get direct access to data referenced in this way"); return 0;
			default:					PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Unknown data ref type"); return 0;
			};
		}

		//*****
		// Return the data of this nugget interpreted as a child nugget.
		template <typename T>
		EResult NuggetImpl<T>::GetChildNugget(std::size_t offset, NuggetImpl<T>& child, ECopyFlag copy_flag) const
		{
			switch( m_data_ref )
			{
			case EDataRef_NoData:		return EResult_NoData;
			case EDataRef_Source:		return child.Initialise(*m_src.m_src, offset, copy_flag);
			case EDataRef_Referenced:	return child.Initialise(m_ref + offset, copy_flag);
			case EDataRef_Buffered:		return child.Initialise(&m_buffer[offset], copy_flag);
			case EDataRef_TempFile:
			case EDataRef_ExternalFile:	PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Unable to get direct access to data referenced in this way"); return EResult_Failed;
			default:					PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Unknown data ref type"); return EResult_Failed;
			}
		}

		//*****
		// Reserve space
		template <typename T>
		inline void NuggetImpl<T>::Reserve(std::size_t data_size)
		{
			m_buffer.reserve(data_size);
		}

		//*****
		// Set the data of this nugget
		template <typename T>
		EResult NuggetImpl<T>::SetData(const void* data, std::size_t data_size, ECopyFlag copy_flag)
		{
			DeleteData();
			switch( copy_flag )
			{
			case ECopyFlag_Reference:
				m_ref = static_cast<const uint8*>(data);
				m_header.m_data_length = data_size;
				m_data_ref = EDataRef_Referenced;
				return EResult_Success;

			case ECopyFlag_CopyToBuffer:
				m_buffer.resize(data_size);
				memcpy(&m_buffer[0], data, data_size);
				m_header.m_data_length = data_size;
				m_data_ref = EDataRef_Buffered;
				return EResult_Success;

			case ECopyFlag_CopyToTempFile:
				{
					// Open a temporary file
					EResult result = OpenTempFile(); if( Failed(result) ) return result;

					// Write the data into the temp file
					if (FileWrite(m_file, data, DWORD(data_size))) { return EResult_WriteToTempFileFailed; }
					m_header.m_data_length = data_size;				
					m_data_ref = EDataRef_TempFile;
					return EResult_Success;
				}
			};
			PR_ASSERT(PR_DBG_NUGGET_FILE, false, "This method does not support this type of copy flag");
			return EResult_Failed;
		}

		//*****
		// Set the data of this nugget from some source
		template <typename T>
		EResult NuggetImpl<T>::SetData(const ISrc& src, std::size_t offset, std::size_t data_size, ECopyFlag copy_flag)
		{
			EResult result;
			DeleteData();
			switch( copy_flag )
			{
			case ECopyFlag_Reference:
				m_src.m_src  = &src;
				m_src.m_base = offset;
				m_header.m_data_length = data_size;
				m_data_ref = NuggetImpl::EDataRef_Source;
				return EResult_Success;
			
			case ECopyFlag_CopyToBuffer:
				m_buffer.resize(data_size);
				if( src.Read(&m_buffer[0], data_size, offset) != data_size )	{ return EResult_NuggetDataCorrupt; }
				m_header.m_data_length = data_size;
				m_data_ref = EDataRef_Buffered;
				return EResult_Success;
			
			case ECopyFlag_CopyToTempFile:
				result = OpenTempFile();			if( Failed(result) ) return result;
				result = Copy(src, offset, m_file); if( Failed(result) ) return result;
				m_header.m_data_length = data_size;
				m_data_ref = EDataRef_TempFile;
				return EResult_Success;
			};
			PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Unknown copy flag");
			return EResult_Failed;
		}
		
		//*****
		// Set the data of this nugget as an external file
		template <typename T>
		EResult NuggetImpl<T>::SetData(const char* external_filename, ECopyFlag copy_flag)
		{
			EResult result;
			DeleteData();
			switch( copy_flag )
			{
			case ECopyFlag_Reference:
				m_ext_filename = external_filename;
				m_header.m_data_length = static_cast<std::size_t>(pr::filesys::FileLength(m_ext_filename));
				m_data_ref = NuggetImpl::EDataRef_ExternalFile;
				return EResult_Success;
			
			case ECopyFlag_CopyToBuffer:
				if( !FileToBuffer(external_filename, m_buffer) ) { return EResult_ReadingExternalFileFailed; }
				m_header.m_data_length = m_buffer.size();
				m_data_ref = EDataRef_Buffered;
				return EResult_Success;

			case ECopyFlag_CopyToTempFile:
				{
					Handle ext_file = FileOpen(m_ext_filename.c_str(), GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL|FILE_FLAG_SEQUENTIAL_SCAN, 0);
					if (ext_file == INVALID_HANDLE_VALUE) { return EResult_ReadingExternalFileFailed; }
					result = OpenTempFile();			if( Failed(result) ) return result;
					result = Copy(ext_file, m_file);	if( Failed(result) ) return result;
					m_header.m_data_length = GetFileSize(m_file, 0);
					m_data_ref = EDataRef_TempFile;
					return EResult_Success;
				}
			}
			PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Unknown copy flag");
			return EResult_Failed;
		}

		//*****
		// Set the data of this nugget to the header and contents of 'nugget'
		template <typename T>
		EResult NuggetImpl<T>::SetData(const NuggetImpl<T>& nugget)
		{
			PR_ASSERT(1, false, "Not implementated");
			EResult result;
			DeleteData();
			switch( copy_flag )
			{
			case ECopyFlag_Reference:
				{}break;
			case ECopyFlag_CopyToBuffer:
				//m_buffer.resize(nugget.GetNuggetSizeInBytes());
				//RawData dst(&m_buffer[0], m_buffer.size());
				//std::size_t offset = 0;
				//nugget.Save(dst, offset);
				//if( src.Read(&m_buffer[0], data_size, offset) != data_size )	{ return EResult_NuggetDataCorrupt; }
				//m_header.m_data_length = data_size;
				//m_data_ref = EDataRef_Buffered;
				//return EResult_Success;

				{}break;//nugget.Save(
			case ECopyFlag_CopyToTempFile:
				{}break;
			}
			PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Unknown copy flag");
			return EResult_Failed;
		}

		//*****
		// Add some data to this nugget.
		template <typename T>
		EResult NuggetImpl<T>::AppendData(const void* data, std::size_t data_size, ECopyFlag copy_flag)
		{
			if( m_data_ref == EDataRef_NoData )
			{
				switch( copy_flag )
				{
				case ECopyFlag_CopyToBuffer:	m_data_ref = EDataRef_Buffered; break;
				case ECopyFlag_CopyToTempFile:	m_data_ref = EDataRef_TempFile; break;
				default: PR_ASSERT(PR_DBG_NUGGET_FILE, false, "This copy type is not allowed for appending data");
				}
			}
			switch( m_data_ref )
			{
			case EDataRef_Buffered:
				{
					std::size_t size = m_buffer.size();
					m_buffer.resize(size + data_size);
					memcpy(&m_buffer[size], data, data_size);
					m_header.m_data_length += data_size;
					return EResult_Success;
				}
			case EDataRef_TempFile:
				{
					if (FileWrite(m_file, data, DWORD(data_size))) { return EResult_WriteToTempFileFailed; }
					m_header.m_data_length += data_size;
					return EResult_Success;
				}
			};
			PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Data cannot be appended to this reference type");
			return EResult_Failed;
		}

		//*****
		// Add some data to this nugget
		template <typename T>
		EResult NuggetImpl<T>::AppendData(const ISrc& src, std::size_t offset, std::size_t data_size, ECopyFlag copy_flag)
		{
			if( m_data_ref == EDataRef_NoData )
			{
				switch( copy_flag )
				{
				case ECopyFlag_CopyToBuffer:	m_data_ref = EDataRef_Buffered; break;
				case ECopyFlag_CopyToTempFile:	m_data_ref = EDataRef_TempFile; break;
				default: PR_ASSERT(PR_DBG_NUGGET_FILE, false, "This copy type is not allowed for appending data");
				}
			}
			switch( m_data_ref )
			{
			case EDataRef_Buffered:
				{
					std::size_t size = m_buffer.size();
					m_buffer.resize(size + data_size);
					if( src.Read(&m_buffer[size], data_size, offset) != data_size ) { return EResult_NuggetDataCorrupt; }
					m_header.m_data_length += data_size;
					return EResult_Success;
				}
			case EDataRef_TempFile:
				{
					EResult result = Copy(src, offset, m_file); if( Failed(result) ) { return EResult_WriteToTempFileFailed; }
					m_header.m_data_length += data_size;
					return EResult_Success;
				}
			};
			PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Data cannot be appended to this reference type");
			return EResult_Failed;
		}

		//*****
		// Add the contains of a nugget to this nugget
		template <typename T>
		EResult NuggetImpl<T>::AppendData(const NuggetImpl<T>& nugget, ECopyFlag copy_flag)
		{
			if( m_data_ref == EDataRef_NoData )
			{
				switch( copy_flag )
				{
				case ECopyFlag_CopyToBuffer:	m_data_ref = EDataRef_Buffered; break;
				case ECopyFlag_CopyToTempFile:	m_data_ref = EDataRef_TempFile; break;
				default: PR_ASSERT(PR_DBG_NUGGET_FILE, false, "This copy type is not allowed for appending data");
				}
			}
			switch( m_data_ref )
			{
			case EDataRef_Buffered:
				{
					BufferedO dst(&m_buffer);
					EResult result = nugget.GetData(dst, 0);
					if( Failed(result) ) { return result; }
					m_header.m_data_length = m_buffer.size();
					return EResult_Success;
				}
			case EDataRef_TempFile:
				{
					FileO dst(&m_file);
					EResult result = nugget.GetData(dst, 0);
					if( Failed(result) ) { return result; }
					m_header.m_data_length += nugget.GetDataSize();
					return EResult_Success;
				}
			};
			PR_ASSERT(PR_DBG_NUGGET_FILE, false, "Data cannot be appended to this reference type");
			return EResult_Failed;
		}

		//*****
		// Delete this nuggets data
		template <typename T>
		void NuggetImpl<T>::DeleteData()
		{
			CloseHandle(m_file);
			m_ref = 0;
			m_buffer.clear();
			m_src.clear();
			m_ext_filename.clear();
			m_header.m_data_length = 0;
			m_data_ref = EDataRef_NoData;
		}

		//*****
		// Open the temporary file for this nugget
		template <typename T>
		EResult NuggetImpl<T>::OpenTempFile()
		{
			PR_ASSERT(PR_DBG_NUGGET_FILE, m_file == INVALID_HANDLE_VALUE, "The temp file shouldn't already be open");

			std::string filename = pr::filesys::MakeUniqueFilename<std::string>("NuggetTmp_XXXXXX");
			m_file = pr::FileOpen(filename.c_str(), GENERIC_READ|GENERIC_WRITE, 0, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_TEMPORARY|FILE_FLAG_DELETE_ON_CLOSE|FILE_FLAG_RANDOM_ACCESS, 0);
			return m_file == INVALID_HANDLE_VALUE ? EResult_FailedToCreateTempFile : EResult_Success;
		}

		//*****
		// Copy from a 'src' to a 'dst'
		template <typename T>
		EResult NuggetImpl<T>::Copy(const ISrc& src, std::size_t src_offset, IDest& dst, std::size_t dst_offset) const
		{
			char buffer[BlockCopySize];
			std::size_t bytes_read;
			do
			{
				bytes_read = src.Read(buffer, BlockCopySize, src_offset);
				if( dst.Write(buffer, bytes_read, dst_offset) != bytes_read ) { return EResult_WriteToDestFailed; }
				src_offset += bytes_read;
				dst_offset += bytes_read;
			}
			while( bytes_read == BlockCopySize );
			return EResult_Success;
		}

		//*****
		// Copy from a file to 'dst'
		template <typename T>
		EResult NuggetImpl<T>::Copy(HANDLE src, IDest& dst, std::size_t dst_offset) const
		{
			SetFilePointer(src, 0, 0, FILE_BEGIN);
			char buffer[BlockCopySize];
			DWORD bytes_read;
			do
			{
				FileRead(src, buffer, BlockCopySize, &bytes_read);
				if( dst.Write(buffer, bytes_read, dst_offset) != bytes_read ) { SetFilePointer(src, 0, 0, FILE_END); return EResult_WriteToDestFailed; }
				dst_offset += bytes_read;
			}
			while( bytes_read == BlockCopySize );
			return EResult_Success;
		}

		//*****
		// Copy from a source to a file
		template <typename T>
		EResult NuggetImpl<T>::Copy(const ISrc& src, std::size_t src_offset, HANDLE dst) const
		{
			char buffer[BlockCopySize];
			std::size_t bytes_read;
			do
			{
				bytes_read = src.Read(buffer, BlockCopySize, src_offset);
				if (!FileWrite(dst, buffer, DWORD(bytes_read))) { return EResult_WriteToTempFileFailed; }
				src_offset += bytes_read;
			}
			while( bytes_read == BlockCopySize );
			return EResult_Success;
		}

		//*****
		// Copy from a src file to a dst file
		template <typename T>
		EResult NuggetImpl<T>::Copy(HANDLE src, HANDLE dst) const
		{
			char buffer[BlockCopySize];
			std::size_t bytes_read;
			do
			{
				bytes_read = FileRead(src, buffer, BlockCopySize);
				if (!FileWrite(dst, buffer, DWORD(bytes_read))) { return EResult_WriteToTempFileFailed; }
			}
			while( bytes_read == BlockCopySize );
			return EResult_Success;
		}

		typedef NuggetImpl<void> Nugget;
	}//namespace nugget
}//namespace pr

#endif//PR_NUGGET_FILE_NUGGET_IMPL_H
