//**************************************************************
//
//	A class to manage bitmaps
//
//**************************************************************
#include "Bitmap.h"
#include <memory.h>
#include "Common\PRFile.h"

//*****
// Create an 8bit image
Bitmap::Result Bitmap::Create8Bit(long width, long height)
{
	long bytes_per_pixel = 1;
	long stride = (width * bytes_per_pixel + 3) & ~3;
	m_file_header.m_type					= ('B') | ('M' << 8);
	m_file_header.m_size					= sizeof(m_file_header) + sizeof(m_info_header) + 256 * sizeof(PaletteEntry) + stride * height;
	m_file_header.m_reserved1				= 0;
	m_file_header.m_reserved2				= 0;
	m_file_header.m_data_offset				= sizeof(m_file_header) + sizeof(m_info_header) + 256 * sizeof(PaletteEntry);

	m_info_header.m_size					= sizeof(InfoHeader);
	m_info_header.m_width					= width;
	m_info_header.m_height					= height;
	m_info_header.m_planes					= 1;
	m_info_header.m_bits_per_pixel			= 8;
	m_info_header.m_compression				= ctRGB;		
	m_info_header.m_image_size				= stride * m_info_header.m_height;
	m_info_header.m_Xpixels_per_meter		= 4000;
	m_info_header.m_Ypixels_per_meter		= 4000;
	m_info_header.m_num_colours_used		= 256;
	m_info_header.m_num_important_colours	= 0;

	m_palette_count							= 256;
	m_palette								= new PaletteEntry[m_palette_count];
	if( !m_palette ) return AllocPaletteFailure;

	m_data_size								= width * height;
	m_Bdata									= new BYTE[m_data_size];
	if( !m_Bdata ) return AllocImageFailure;
	
	memset(m_Bdata,   0, m_data_size);
	memset(m_palette, 0, m_palette_count * sizeof(PaletteEntry));
	return Success;
}

//*****
// Load a bitmap from disc
Bitmap::Result Bitmap::Load(const char* filename)
{
	PR::File file(filename, "rb");
	if( !file.IsOpen() ) return FileOpenFailure;

	// Read the bitmap file header
	if( file.Read(&m_file_header, sizeof(m_file_header)) != sizeof(m_file_header) ) return FileReadFailure;

	// Read the info header
	if( file.Read(&m_info_header, sizeof(m_info_header)) != sizeof(m_info_header) ) return FileReadFailure;

	// Read the palette
	m_palette_count		= (m_info_header.m_num_colours_used > 0) ? (m_info_header.m_num_colours_used) : (1 << m_info_header.m_bits_per_pixel);
	m_palette			= new PaletteEntry[m_palette_count];
	if( file.Read(m_palette, m_palette_count * sizeof(PaletteEntry)) != m_palette_count * sizeof(PaletteEntry) ) return FileReadFailure;

	// Read the image data
	file.Seek(PR::File::Beginning, m_file_header.m_data_offset);
	char pad[10];
	int byte_width	= m_info_header.m_width * m_info_header.m_bits_per_pixel /  8;
	int stride		= ((byte_width + 3) & ~3) - byte_width;	PR_ASSERT(stride < 10);
	int height		= (m_info_header.m_height > 0) ? (m_info_header.m_height) : (-m_info_header.m_height);
	m_Bdata			= new BYTE[byte_width * height];
	for( int h = height - 1; h >= 0; --h )
	{
		if( file.Read(&m_Bdata[h * byte_width], byte_width) != (unsigned long)byte_width )	return FileReadFailure;
		if( stride > 0 && file.Read(pad, stride) != (unsigned long)stride)					return FileReadFailure;
	}

	return Success;
}

// Save a bitmap to disc
Bitmap::Result Bitmap::Save(const char* filename)
{
	PR_ASSERT_STR(m_file_header.m_data_offset >= sizeof(m_file_header) + m_info_header.m_size + m_palette_count * sizeof(PaletteEntry), "Bitmap data offset is not correct");
	PR_ASSERT_STR(m_file_header.m_size		  == m_file_header.m_data_offset + m_info_header.m_image_size,	"Bitmap sizes are not correct");
	char* pad		= "Paulwashere";

	PR::File file(filename, "w");
	if( !file.IsOpen() ) return FileOpenFailure;

	// Write the bitmap file header
	if( file.Write(&m_file_header, sizeof(m_file_header)) != sizeof(m_file_header) ) return FileWriteFailure;

	// Write the info header
	if( file.Write(&m_info_header, m_info_header.m_size) != m_info_header.m_size ) return FileWriteFailure;

	// Write the palette
	if( file.Write(m_palette, m_palette_count * sizeof(PaletteEntry)) != m_palette_count * sizeof(PaletteEntry) ) return FileWriteFailure;

	// Pad out to the data
	long pad_length = m_file_header.m_data_offset - ftell(file); PR_ASSERT(pad_length >= 0);
	if( pad_length > 0 && fwrite(pad, 1, pad_length, file) != (size_t)pad_length ) { fclose(file); return FileWriteFailure; }

	// Write the image data row by row
	int byte_width	= m_info_header.m_width * m_info_header.m_bits_per_pixel / 8;
	int stride		= ((byte_width + 3) & ~3) - byte_width;
	int height		= (m_info_header.m_height > 0) ? (m_info_header.m_height) : (-m_info_header.m_height);
	for( int h = height - 1; h >= 0; --h )
	{
        if( file.Write(&m_Bdata[h * byte_width], byte_width) != (unsigned long)byte_width ) return FileWriteFailure;
		if( stride > 0 && file.Write(pad, stride) != (unsigned long)stride) return FileWriteFailure;
	}

	return Success;
}


