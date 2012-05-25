//**************************************************************
//
//	A class to manage bitmaps
//
//**************************************************************
#ifndef BITMAP_H
#define BITMAP_H

#include "Common/PRAssert.h"
#include "Maths/Maths.h"

#ifndef NDEBUG
#pragma comment(lib, "BitmapD.lib")
#else//NDEBUG
#pragma comment(lib, "Bitmap.lib")
#endif//NDEBUG

struct Bitmap
{
	enum Result { Success, FileOpenFailure, FileReadFailure, FileWriteFailure, AllocPaletteFailure, AllocImageFailure };

	Bitmap() : m_palette(NULL), m_Bdata(NULL) {}
	~Bitmap() { ReleaseMemory(); }

	void ReleaseMemory() { delete [] m_palette; m_palette = NULL; delete [] m_Bdata; m_Bdata = NULL; }
	Bitmap::Result Create8Bit(long width, long height);
	Bitmap::Result Load(const char* filename);
	Bitmap::Result Save(const char* filename);

	#pragma pack(push, 1)
	struct FileHeader
	{ 
		FileHeader()
		{
			m_type				= ('B') | ('M' << 8);
			m_size				= 0;
			m_reserved1			= 0;
			m_reserved2			= 0;
			m_data_offset		= 0;
		}
		uint16	m_type;				// Identifier for bitmap files
		uint32	m_size;				// The complete size in bytes of the bitmap file
		uint16	m_reserved1;		// Must be zero
		uint16	m_reserved2; 		// Must be zero
		uint32	m_data_offset;		// Offset in bytes to the start of the bitmap data
	};
	#pragma pack(pop)

	// cf BI_RGB, BI_RLE8, BI_RLE4, BI_BITFIELDS, BI_JPEG, BI_PNG
	enum CompressionType { ctRGB = 0L, ctRLE8 = 1L, ctRLE4 = 2L, ctBITFIELDS = 3L, ctJPEG = 4L, ctPNG = 5L };
	#pragma pack(push, 1)
	struct InfoHeader
	{
		InfoHeader()
		{
			m_size					= sizeof(InfoHeader);
			m_width					= 0;
			m_height				= 0;
			m_planes				= 1;
			m_bits_per_pixel		= 8;
			m_compression			= ctRGB;		
			m_image_size			= 0;
			m_Xpixels_per_meter		= 4000;
			m_Ypixels_per_meter		= 4000;
			m_num_colours_used		= 0;
			m_num_important_colours	= 0;
		}
		uint32	m_size;					// The size in bytes of the BitmapInfoHeader
		long	m_width;				// The width in pixels of the image
		long	m_height;				// The height in pixels of the image, negative height indicates the image is stored upside down
		uint16	m_planes;				// The number of planes for the target device. Must be 1
		uint16	m_bits_per_pixel;		// The number of bits per pixel
	    uint32	m_compression;			// ctRGB for uncompressed palettised. ctBITFIELDS for uncompressed colour masks (16bpp and 32bpp), or a compression identifier
	    uint32	m_image_size;			// The size of the image = stride * height, can be zero for uncompressed images
	    long	m_Xpixels_per_meter;	// The horizontal resolution in pixels per meter
	    long	m_Ypixels_per_meter;	// The vertical resolution in pixels per meter
	    uint32	m_num_colours_used;		// The number of colour indices inthe colour table that are actually used. If 0, = 2^m_bits_per_pixel
	    uint32	m_num_important_colours;// The number of colours considered important for displaying the bitmap. If 0, all are important
		// Note about m_compression:
		//	For 16-bpp bitmaps, if m_compression equals BI_RGB, the format is RGB 555.
		//	If m_compression equals BI_BITFIELDS, the format is either RGB 555 or RGB 565.
	};
	#pragma pack(pop)

	#pragma pack(push, 1)
	struct PaletteEntry
	{
		uint8	m_blue;
		uint8	m_green;
		uint8	m_red;
		uint8	m_reserved;
	};
	#pragma pack(pop)

	// Members
    FileHeader		m_file_header;	// The file header
	InfoHeader		m_info_header;	// The info header
	PaletteEntry*	m_palette;		// The palette for the bitmap
	uint32			m_palette_count;// The number of entries in the palette
	union
	{
		uint32*		m_DWdata;		// A uint32 pointer to the data.
		uint16*		m_Wdata;		// A uint16 pointer to the data
		uint8*		m_Bdata;		// A uint8 pointer to the data
	};
	uint32			m_data_size;	// The size in bytes of the data = m_width * m_height * m_bits_per_pixel/8 (doesn't include stride)
};

#endif//BITMAP_H

