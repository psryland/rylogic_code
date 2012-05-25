#pragma once

class BlenderDesc
{
public:
	int pointersize;	// 4 or 8 (byte)
	bool littleEndian;  // false means big endian
	char version[4];	// cstring version info
};

struct MVert
{
	float co[3];
	short no[3];
	char flag;
	char mat_nr;
	char bweight;
	float uv[2];	// not in blender file format, filled in during MTFace parsing
	bool isUVSet;	// set to true for first texture coord pair set (to prevent overwriting in later faces)
	long nextSupplVert;  // used in duplicteVertex mode to to point to next additional vertex
};

struct MFace
{
	long v1;
	long v2;
	long v3;
	//long v4; removed: we only have triangles in D3D
	char mat_nr;
	char edcode;
	char flag;
	bool isQuad;
	bool supplV1;  // true if v1 is index from duplicated vertices
	bool supplV2;  // true if v2 is index from duplicated vertices
	bool supplV3;  // true if v3 is index from duplicated vertices
};

struct MTFace
{
	float uv[3][2];  // this is [4][2] in Blender to support Quads - we need only 3 for each triangle
	//void *tpage;

	//long v3;
	//long v4;
	//char mat_nr;
	//char edcode;
	//char flag;
};

class Mesh 
{
public:
	Mesh(UVMapping = UV_SimpleMode);
	~Mesh(void);
	const char *name;
	long totvert;
	long totedge;
	long totface;
	UVMapping mappingMode;	// default is simpleMode
	MVert *mverts;
	MFace *mfaces;
	MTFace *mtfaces;
	std::vector<MVert> supplMVerts; // used in duplicteVertex mode to store additional vertices
};

class BlenderCallback
{
public:
	virtual void meshLoaded(Mesh *mesh) = 0;
};

class StructureField {
public:
  size_t typeIndex;
  size_t nameIndex;
  long offset;
};

class StructureInfo {
public:
  size_t typeIndex;
  std::vector<StructureField> fields;
};

class StructureDNA {
public:
	std::vector<std::string> names;
	std::vector<std::string> types;
	std::vector<long> lengths;
	std::vector<StructureInfo> structures;
};

class FileBlockHeader {
public:
	char code[5];				// 4
	unsigned long size;			// 4
	void *oldMemoryAddress;		// pointersize (4)
	unsigned long sdnaIndex;	// 4
	unsigned long count;		// 4
	unsigned char *buf;	     	// buffer content;
	void release();				// release allocated memory for this block
};

class Blender
{
public:
	Blender(BlenderCallback *bc);
	~Blender(void);
	static wchar_t* string2wstring(std::string text);
	static void Log(std::string text);
	bool parseBlenderFile(std::string filename, UVMapping uvMapping = UV_SimpleMode);
	BlenderDesc blenderDesc;
private:
	void releaseStructureDNA(StructureDNA *sdna, FileBlockHeader *fbheader);
	long getShort(unsigned char *buffer);
	long alterLengthByName(std::string name, long struct_length);
	bool parseStructureDNA(StructureDNA *sdna, FileBlockHeader *fbheader);
	void getFileBlock(FileBlockHeader *fbheader, ifstream *in);
	StructureInfo *findStructureType(char *name, StructureDNA *sdna);
	StructureInfo *findStructureType(size_t typeIndex, StructureDNA *sdna);
	void getOffsets(long *offsetStruct, long *offsetField, const char *structName, const char *fieldName, StructureDNA *sdna, FileBlockHeader &fbh);
	long getMemberOffset(const char *name, StructureDNA *sdna, FileBlockHeader &fbh);
	long getLong(char *name, StructureDNA *sdna, FileBlockHeader &fbh);
	long getLong(unsigned long offset, unsigned long iteration, unsigned long structLength, FileBlockHeader &fbh);
	short getShort(unsigned long offset, unsigned long iteration, unsigned long structLength, FileBlockHeader &fbh);
	float getFloat(unsigned long offset, unsigned long iteration, unsigned long structLength, FileBlockHeader &fbh);
	const char* getString(char *name, StructureDNA *sdna, FileBlockHeader &fbh);
	const char *getStructureName(StructureDNA *sdna, FileBlockHeader *fbh);
	MVert *parseMVerts(StructureDNA *sdna, vector<FileBlockHeader> &blocks);
	MFace *parseMFaces(StructureDNA *sdna, vector<FileBlockHeader> &blocks, long *numfaces);
	void parseMTFaces(StructureDNA *sdna, vector<FileBlockHeader> &blocks, Mesh *mesh);
	void parseMesh(StructureDNA *sdna, vector<FileBlockHeader> &blocks, UVMapping uvMapping);
	void parseFileBlocks(StructureDNA *sdna, vector<FileBlockHeader> &blocks, UVMapping uvMapping);
	void setUV(Mesh *mesh, unsigned long faceIndex);
	void handleUVDuplicationMode(Mesh *mesh, unsigned long faceIndex);

	BlenderCallback *blenderCallback;

	// debug log level:
	static const bool debug_basic = true;
	static const bool debug_uv = false;
};

