#include "DXUT.h"
#include "Vertex.h"
#include "ShadedPath.h"
#include "Terrain.h"
#include "Camera.h"
#include "d3dUtil.h"
#include <algorithm>
#include <list>
#include <iostream>
#include <fstream>
using namespace std;
#include "Blender.h"

// comment/uncomment next lines to disable/enable extended logging
//#define DEBUG_VERTICES


Blender::Blender(BlenderCallback* bc)
{
	if (debug_basic) Log("Blender c'tor\n");
	blenderCallback = bc;
}

Blender::~Blender(void)
{
}

void Blender::Log(std::string text)
{
#ifdef UNICODE
	OutputDebugStr(string2wstring(text));
#else
	OutputDebugStr(text.c_str());
#endif
}

wchar_t* Blender::string2wstring(std::string text)
{
	static wchar_t wstring[5000];
	MultiByteToWideChar(CP_ACP, 0, text.c_str() , -1, wstring , 5000);
	return wstring;
}

void FileBlockHeader::release()
{
	if (this->buf)
		delete [] this->buf;
}

void Blender::releaseStructureDNA(StructureDNA* sdna, FileBlockHeader* fbheader)
{
	/*  std::vector<std::string>::const_iterator iter;
	    for(iter = sdna->names.begin(); iter != sdna->names.end(); ++iter){
	        //delete *iter;
	    }
	*/
}

long Blender::getShort(unsigned char* buffer)
{
	long one = buffer[0];
	long two = buffer[1];
	return one + 256 * two;
}

long Blender::alterLengthByName(std::string name, long struct_length)
{
	//if (name.compare(0, 1, "*")) return 4;
	// check for pointer or function pointer
	if (name.at(0) == '*' || name.at(0) == '(')
		return 4;
	// look for arrays:
	//size_t posX = name.find("][");
	//if (posX != string::npos) {
	//  Log("dgzu");
	//}
	size_t pos = name.find("[");
	int arrayMult = 1;
	while (pos != string::npos)
	{
		// we may find multiple array dimensions: multiply all of them
		size_t end = name.find("]", pos);
		std::string arrayLength = name.substr(pos+1, end-pos-1);
		stringstream ss(arrayLength);
		long num;
		if ((ss >> num).fail())
		{
			Blender::Log("Error in number conversion");
			return 0;
		}
		arrayMult *= num;
		pos = name.find("[", end);
	}
	return arrayMult * struct_length;
}

bool Blender::parseStructureDNA(StructureDNA* sdna, FileBlockHeader* fbheader)
{
	sdna->names.clear();
	sdna->types.clear();
	sdna->lengths.clear();
	sdna->structures.clear();
	unsigned char* buf = fbheader->buf;
	size_t i = 0;
	if (0 != strncmp("SDNA", (char*)&buf[i], 4))
	{
		Blender::Log("parse error in structure DNA");
		return false;
	}
	i += 4;
	if (0 != strncmp("NAME", (char*)&buf[i], 4))
	{
		Blender::Log("parse error in structure DNA");
		return false;
	}
	i += 4;
	// get names
	unsigned long numNames = *((unsigned long*)&buf[i]);
	i += 4;
	for (unsigned long k = 0; k < numNames; k++)
	{
		sdna->names.push_back(std::string((char*)&buf[i]));
		i += (sdna->names[k].size() + (size_t)1);
	}
	
	// get types:
	while ((i%4) != 0) i++; // align to 4 byte boundary
	if (0 != strncmp("TYPE", (char*)&buf[i], 4))
	{
		Blender::Log("parse error in structure DNA");
		return false;
	}
	i += 4;
	unsigned long numTypes = *((unsigned long*)&buf[i]);
	i += 4;
	for (unsigned long k = 0; k < numTypes; k++)
	{
		sdna->types.push_back(std::string((char*)&buf[i]));
		i += (sdna->types[k].size() + (size_t)1);
	}
	
	// get lengths:
	while ((i%4) != 0) i++; // align to 4 byte boundary
	if (0 != strncmp("TLEN", (char*)&buf[i], 4))
	{
		Blender::Log("parse error in structure DNA");
		return false;
	}
	i += 4;
	for (unsigned long k = 0; k < numTypes; k++)
	{
		long len = getShort(&buf[i]);
		i += 2;
		sdna->lengths.push_back(len);
	}
	
	// get structures:
	while ((i%4) != 0) i++; // align to 4 byte boundary
	if (0 != strncmp("STRC", (char*)&buf[i], 4))
	{
		Blender::Log("parse error in structure DNA");
		return false;
	}
	i += 4;
	unsigned long numStructures = *((unsigned long*)&buf[i]);
	i += 4;
	for (unsigned long k = 0; k < numStructures; k++)
	{
		StructureInfo sinfo;
		StructureField sfield;
		size_t typeIndex = getShort(&buf[i]);
		i += 2;
		sinfo.typeIndex = typeIndex;
		size_t numFields = getShort(&buf[i]);
		i += 2;
		long offset = 0; // track field offset
		for (unsigned long l = 0; l < numFields; l++)
		{
			// parse fields of this structure:
			size_t fieldTypeIndex = getShort(&buf[i]);
			i += 2;
			size_t fieldNameIndex = getShort(&buf[i]);
			i += 2;
			sfield.typeIndex = fieldTypeIndex;
			sfield.nameIndex = fieldNameIndex;
			sfield.offset = offset;
			offset += alterLengthByName(sdna->names[fieldNameIndex], sdna->lengths[fieldTypeIndex]);
			sinfo.fields.push_back(sfield);
		}
		sdna->structures.push_back(sinfo);
	}
	
	if (debug_basic)
	{
		std::ostringstream oss;
		oss << " names collected: " << sdna->names.size() << "\n";
		oss << " types collected: " << sdna->types.size() << "\n";
		oss << " lengths collected: " << sdna->lengths.size() << "\n";
		oss << " structures collected: " << sdna->structures.size() << "\n";
		Blender::Log(oss.str());
	}
	
	//#define DUMPNAMES
	
#ifdef DUMPNAMES
	for (unsigned long k = 0; k < sdna->names.size(); k++)
	{
		oss.clear();
		oss.str("");
		oss << " name[" << k << "]: " << sdna->names[k] << "\n";
		Blender::Log(oss.str());
	}
#endif
	return true;
}

void Blender::getFileBlock(FileBlockHeader* fbheader, ifstream* in)
{
	in->read(fbheader->code, 4);
	fbheader->code[4] = 0; // terminate string
	in->read((char*)&fbheader->size, 4);
	in->read((char*)&fbheader->oldMemoryAddress, 4);
	in->read((char*)&fbheader->sdnaIndex, 4);
	in->read((char*)&fbheader->count, 4);
	fbheader->buf = new unsigned char[fbheader->size];
	in->read((char*)fbheader->buf, fbheader->size);
}

StructureInfo* Blender::findStructureType(char* name, StructureDNA* sdna)
{
	for (unsigned long i = 0; i < sdna->structures.size(); i++)
	{
		if (0 == strcmp(name, sdna->types[sdna->structures[i].typeIndex].c_str()))
		{
			return &sdna->structures[i];
		}
	}
	return 0;
}

StructureInfo* Blender::findStructureType(size_t typeIndex, StructureDNA* sdna)
{
	//char *typeName = sdna->types[typeIndex].c_str();
	for (unsigned long i = 0; i < sdna->structures.size(); i++)
	{
		//if (0 == strcmp(typeName, sdna->structures[i].)) {
		if (typeIndex == sdna->structures[i].typeIndex)
		{
			return &sdna->structures[i];
		}
	}
	return 0;
}

void Blender::getOffsets(long* offsetStruct, long* offsetField, const char* structName, const char* fieldName, StructureDNA* sdna, FileBlockHeader& fbh)
{
	// get offsets for a sub structure and a field inside the sub structure
	StructureInfo s = sdna->structures[fbh.sdnaIndex];
	for (unsigned long i = 0; i < s.fields.size(); i++)
	{
		if (strcmp(structName, sdna->names[s.fields[i].nameIndex].c_str()) == 0)
		{
			// we found the structure
			StructureField field = s.fields[i];
			*offsetStruct = field.offset;
			// get info for substructure
			StructureInfo* subStructure = findStructureType(field.typeIndex, sdna);
			// find offset in substructure:
			for (unsigned long k = 0; k < subStructure->fields.size(); k++)
			{
				//Blender::Log(sdna->names[subStructure->fields[k].nameIndex] + "\n");
				if (0 == strcmp(fieldName, sdna->names[subStructure->fields[k].nameIndex].c_str()))
				{
					// found field
					*offsetField = subStructure->fields[k].offset;
					return;
				}
			}
		}
	}
}


long Blender::getMemberOffset(const char* name, StructureDNA* sdna, FileBlockHeader& fbh)
{
	std::string n(name);
	size_t pos = n.find('.');
	if (pos != string::npos)
	{
		std::string structname = n.substr(0, pos);
		std::string fieldname = n.substr(pos+1, n.size()-pos-1);
		//Blender::Log(structname + " " + fieldname);
		long offset_struct = 0;
		long offset_field = 0;
		getOffsets(&offset_struct, &offset_field, structname.c_str(), fieldname.c_str(), sdna, fbh);
		return offset_struct + offset_field;
	}
	
	StructureInfo s = sdna->structures[fbh.sdnaIndex];
	for (unsigned long i = 0; i < s.fields.size(); i++)
	{
		if (strcmp(name, sdna->names[s.fields[i].nameIndex].c_str()) == 0)
		{
			return s.fields[i].offset;
		}
	}
	return -1;
}

long Blender::getLong(char* name, StructureDNA* sdna, FileBlockHeader& fbh)
{
	long offset = getMemberOffset(name, sdna, fbh);
	if (offset == -1) return -1;
	long* lptr = (long*)&fbh.buf[offset];
	return *lptr;
}

long Blender::getLong(unsigned long offset, unsigned long iteration, unsigned long structLength, FileBlockHeader& fbh)
{
	long* lptr = (long*)&fbh.buf[offset + iteration * structLength];
	return *lptr;
}

short Blender::getShort(unsigned long offset, unsigned long iteration, unsigned long structLength, FileBlockHeader& fbh)
{
	short* sptr = (short*)&fbh.buf[offset + iteration * structLength];
	return *sptr;
}

float Blender::getFloat(unsigned long offset, unsigned long iteration, unsigned long structLength, FileBlockHeader& fbh)
{
	float* fptr = (float*)&fbh.buf[offset + iteration * structLength];
	return *fptr;
}

const char* Blender::getString(char* name, StructureDNA* sdna, FileBlockHeader& fbh)
{
	long offset = getMemberOffset(name, sdna, fbh);
	if (offset == -1) return "n/a";
	const char* cptr = (const char*)&fbh.buf[offset];
	return cptr;
}

const char* Blender::getStructureName(StructureDNA* sdna, FileBlockHeader* fbh)
{
	return sdna->types[sdna->structures[fbh->sdnaIndex].typeIndex].c_str();
}

MVert* Blender::parseMVerts(StructureDNA* sdna, vector<FileBlockHeader> &blocks)
{
	StructureInfo* mvertStructure = findStructureType("MVert", sdna);
	unsigned long len = sdna->lengths[mvertStructure->typeIndex];
	// find MVert Block:
	for (unsigned long i = 0; i < blocks.size(); i++)
	{
		FileBlockHeader fbh = blocks[i];
		if (0 == strcmp("MVert", getStructureName(sdna, &fbh)))
		{
			unsigned long count = blocks[i].count;
			MVert* mverts = new MVert[count];
			long offset_co = getMemberOffset("co[3]", sdna, fbh);
			long offset_no = getMemberOffset("no[3]", sdna, fbh);
			for (unsigned long k = 0; k < count; k++)
			{
				mverts[k].co[0] = getFloat(offset_co, k, len, fbh);
				mverts[k].co[1] = getFloat(offset_co+4, k, len, fbh);
				mverts[k].co[2] = getFloat(offset_co+8, k, len, fbh);
				mverts[k].no[0] = getShort(offset_no, k, len, fbh);
				mverts[k].no[1] = getShort(offset_no+2, k, len, fbh);
				mverts[k].no[2] = getShort(offset_no+4, k, len, fbh);
				mverts[k].isUVSet = false;
				mverts[k].nextSupplVert = -1;
			}
			return mverts;
		}
	}
	return 0;
}

MFace* Blender::parseMFaces(StructureDNA* sdna, vector<FileBlockHeader> &blocks, long* numFaces)
{
	StructureInfo* mfaceStructure = findStructureType("MFace", sdna);
	unsigned long len = sdna->lengths[mfaceStructure->typeIndex];
	// find MFace Block:
	for (unsigned long i = 0; i < blocks.size(); i++)
	{
		FileBlockHeader fbh = blocks[i];
		if (0 == strcmp("MFace", getStructureName(sdna, &fbh)))
		{
			unsigned long count = blocks[i].count;
			MFace* mfaces = new MFace[count*2]; // we triangulize quads, so make double room
			long offset_v1 = getMemberOffset("v1", sdna, fbh);
			unsigned long outFace = 0;  //count output faces
			for (unsigned long k = 0; k < count; k++)
			{
				long v1 = getLong(offset_v1, k, len, fbh);
				long v2 = getLong(offset_v1 + 4, k, len, fbh);
				long v3 = getLong(offset_v1 + 8, k, len, fbh);
				long v4 = getLong(offset_v1 + 12, k, len, fbh);
				
				mfaces[outFace].supplV1 = false;
				mfaces[outFace].supplV2 = false;
				mfaces[outFace].supplV3 = false;
				mfaces[outFace].isQuad = false;
				mfaces[outFace].v1 = v1;
				mfaces[outFace].v2 = v2;
				mfaces[outFace++].v3 = v3;
				// check for quad:
				if (v4 != 0)
				{
					mfaces[outFace-1].isQuad = true;
					mfaces[outFace].supplV1 = false;
					mfaces[outFace].supplV2 = false;
					mfaces[outFace].supplV3 = false;
					mfaces[outFace].isQuad = true;
					mfaces[outFace].v1 = v1;
					mfaces[outFace].v2 = v3;
					mfaces[outFace++].v3 = v4;
				}
			}
			*numFaces = outFace;
			return mfaces;
		}
	}
	return 0;
}

float inline adjustTextureV(const float v)
{
	return 1.0f - v;
}

void Blender::parseMTFaces(StructureDNA* sdna, vector<FileBlockHeader> &blocks, Mesh* mesh)
{
	if (debug_basic && mesh->mappingMode == UV_SimpleMode)
	{
		Blender::Log("UV Mapping Simple mode: No Vertices will be duplicated. Some Textures may be distorted.\n");
	}
	else if (debug_basic && mesh->mappingMode == UV_DuplicateVertex)
	{
		Blender::Log("UV Mapping Duplicate Vertex mode: Vertices will be duplicated as needed.\n");
	}
	StructureInfo* mtfaceStructure = findStructureType("MTFace", sdna);
	unsigned long len = sdna->lengths[mtfaceStructure->typeIndex];
	// find MTFace Block:
	for (unsigned long i = 0; i < blocks.size(); i++)
	{
		FileBlockHeader fbh = blocks[i];
		if (0 == strcmp("MTFace", getStructureName(sdna, &fbh)))
		{
			unsigned long count = blocks[i].count;
			MTFace* mtfaces = new MTFace[count*2]; // we triangulize quads, so make double room
			mesh->mtfaces = mtfaces;
			long offset_uv = getMemberOffset("uv[4][2]", sdna, fbh);
			//long offset_tpage = getMemberOffset("*tpage", sdna, fbh);
			unsigned long outFace = 0;  //count output faces
			for (unsigned long k = 0; k < count; k++)
			{
				//memcpy(&mtfaces[outFace].uv, fbh.buf + k * len + offset_uv, sizeof(float[4][2]));
				//mtfaces[outFace].tpage = (void*)(fbh.buf + k * len + offset_tpage);
				mtfaces[outFace].uv[0][0] = getFloat(offset_uv, k, len, fbh);
				mtfaces[outFace].uv[0][1] = adjustTextureV(getFloat(offset_uv + 4, k, len, fbh));
				mtfaces[outFace].uv[1][0] = getFloat(offset_uv + 8, k, len, fbh);
				mtfaces[outFace].uv[1][1] = adjustTextureV(getFloat(offset_uv + 12, k, len, fbh));
				mtfaces[outFace].uv[2][0] = getFloat(offset_uv + 16, k, len, fbh);
				mtfaces[outFace].uv[2][1] = adjustTextureV(getFloat(offset_uv + 20, k, len, fbh));
				setUV(mesh, outFace);
				if (mesh->mfaces[outFace].isQuad)
				{
					outFace++;
					mtfaces[outFace].uv[0][0] = getFloat(offset_uv, k, len, fbh);
					mtfaces[outFace].uv[0][1] = adjustTextureV(getFloat(offset_uv + 4, k, len, fbh));
					mtfaces[outFace].uv[1][0] = getFloat(offset_uv + 16, k, len, fbh);
					mtfaces[outFace].uv[1][1] = adjustTextureV(getFloat(offset_uv + 20, k, len, fbh));
					mtfaces[outFace].uv[2][0] = getFloat(offset_uv + 24, k, len, fbh);
					mtfaces[outFace].uv[2][1] = adjustTextureV(getFloat(offset_uv + 28, k, len, fbh));
					setUV(mesh, outFace);
					//memcpy(&mtfaces[outFace].uv, fbh.buf + k * len + offset_uv, sizeof(float[4][2]));
					//mtfaces[outFace].tpage = (void*)(fbh.buf + k * len + offset_tpage);
				}
				outFace++;
			}
		}
	}
}

void Blender::setUV(Mesh* mesh, unsigned long faceIndex)
{
	MTFace* mtface = &mesh->mtfaces[faceIndex];
	MFace* mface = &mesh->mfaces[faceIndex];
	// go through all vertices of the face and set uv coordinates:
	MVert* vert = &mesh->mverts[mface->v1];
	bool v1AlreadySet = false;
	bool v2AlreadySet = false;
	bool v3AlreadySet = false;
	if (!vert->isUVSet)
	{
		vert->uv[0] = mtface->uv[0][0];
		vert->uv[1] = mtface->uv[0][1];
		vert->isUVSet = true;
	}
	else
	{
		if (vert->uv[0] != mtface->uv[0][0] || vert->uv[1] != mtface->uv[0][1])
			v1AlreadySet = true;
	}
	vert = &mesh->mverts[mface->v2];
	if (!vert->isUVSet)
	{
		vert->uv[0] = mtface->uv[1][0];
		vert->uv[1] = mtface->uv[1][1];
		vert->isUVSet = true;
	}
	else
	{
		if (vert->uv[0] != mtface->uv[1][0] || vert->uv[1] != mtface->uv[1][1])
			v2AlreadySet = true;
	}
	vert = &mesh->mverts[mface->v3];
	if (!vert->isUVSet)
	{
		vert->uv[0] = mtface->uv[2][0];
		vert->uv[1] = mtface->uv[2][1];
		vert->isUVSet = true;
	}
	else
	{
		if (vert->uv[0] != mtface->uv[2][0] || vert->uv[1] != mtface->uv[2][1])
			v3AlreadySet = true;
	}
	if (v1AlreadySet || v2AlreadySet || v3AlreadySet)
	{
		if (debug_uv)
		{
			std::ostringstream oss;
			oss << " face " << faceIndex << " has uv already set on ";
			if (v1AlreadySet) oss << "v1 ";
			if (v2AlreadySet) oss << "v2 ";
			if (v3AlreadySet) oss << "v3 ";
			oss << "\n";
			Blender::Log(oss.str());
		}
		if (mesh->mappingMode == UV_DuplicateVertex)
		{
			handleUVDuplicationMode(mesh, faceIndex);
		}
	}
}

long findMatchingVert(Mesh* mesh, MVert* vert, MTFace* mtface, int index)
{
	// try to find a matching additional vertex with same uv coords:
	MVert* vertSearch = vert;
	while (vertSearch->nextSupplVert != -1)
	{
		long i = vertSearch->nextSupplVert;
		vertSearch = &mesh->supplMVerts[i];
		if (vertSearch->uv[0] == mtface->uv[index][0] && vertSearch->uv[1] == mtface->uv[index][1])
		{
			// found matching vertex index in suppl list
			return i;
		}
	}
	return -1;
}

long createDuplicateVertex(Mesh* mesh, MVert* vert)
{
	MVert newVert = *vert;
	// vert->nextSupplVert is copied, so we insert at head of supplement vertices
	mesh->supplMVerts.push_back(newVert);
	vert->nextSupplVert = (long)mesh->supplMVerts.size() - 1;
	return vert->nextSupplVert;
}

void checkCreateDuplicateVertex(int index, Mesh* mesh, MFace* mface, MTFace* mtface)
{
	MVert* vert;
	switch (index)
	{
	case 0:
		vert = &mesh->mverts[mface->v1];
		break;
	case 1:
		vert = &mesh->mverts[mface->v2];
		break;
	case 2:
		vert = &mesh->mverts[mface->v3];
		break;
	default:
		vert = 0;
	}
	
	if (vert->isUVSet && (vert->uv[0] != mtface->uv[index][0] || vert->uv[1] != mtface->uv[index][1]))
	{
		// try to find a matching additional vertex with same uv coords:
		long supplMVertIndex = findMatchingVert(mesh, vert, mtface, index);
		if (supplMVertIndex == -1)
		{
			// not found: create duplicate of this vertex:
			supplMVertIndex = createDuplicateVertex(mesh, vert);
		}
		switch (index)
		{
		case 0:
			mface->v1 = supplMVertIndex;
			mface->supplV1 = true;
			break;
		case 1:
			mface->v2 = supplMVertIndex;
			mface->supplV2 = true;
			break;
		case 2:
			mface->v3 = supplMVertIndex;
			mface->supplV3 = true;
			break;
		}
		mesh->supplMVerts[supplMVertIndex].uv[0] = mtface->uv[index][0];
		mesh->supplMVerts[supplMVertIndex].uv[1] = mtface->uv[index][1];
	}
}

void Blender::handleUVDuplicationMode(Mesh* mesh, unsigned long faceIndex)
{
	MTFace* mtface = &mesh->mtfaces[faceIndex];
	MFace* mface = &mesh->mfaces[faceIndex];
	// go through all vertices of the face and set uv coordinates:
	// for vertices that didn't have uv already set,
	// uv had to be set into regular vertices before calling this method
	checkCreateDuplicateVertex(0, mesh, mface, mtface);
	checkCreateDuplicateVertex(1, mesh, mface, mtface);
	checkCreateDuplicateVertex(2, mesh, mface, mtface);
	/*  MVert *vert = &mesh->mverts[mface->v1];
	    if (vert->isUVSet && vert->uv[0] != mtface->uv[0][0] && vert->uv[1] != mtface->uv[0][1]) {
	        // try to find a matching additional vertex with same uv coords:
	        long supplMVertIndex = findMatchingVert(mesh, vert, mtface, 0);
	        if (supplMVertIndex != -1) {
	            mface->v1 = supplMVertIndex;
	            mface->supplV1 = true;
	        } else {
	            supplMVertIndex = createDuplicateVertex(mesh, vert);
	            mface->v1 = supplMVertIndex;
	            mface->supplV1 = true;
	            mesh->supplMVerts[supplMVertIndex].uv[0] = mtface->uv[0][0];
	            mesh->supplMVerts[supplMVertIndex].uv[1] = mtface->uv[0][1];
	        }
	    }*/
}

void Blender::parseMesh(StructureDNA* sdna, vector<FileBlockHeader> &blocks, UVMapping uvMapping)
{
	FileBlockHeader fbh = blocks[0];
	if (debug_basic) Blender::Log("parse Mesh");
	Mesh mesh(uvMapping);
	mesh.totvert = getLong("totvert", sdna, fbh);
	mesh.totedge = getLong("totedge", sdna, fbh);
	mesh.totface = getLong("totface", sdna, fbh);
	mesh.name = getString("id.name[24]", sdna, fbh);
	mesh.mverts = parseMVerts(sdna, blocks);
	mesh.mfaces = parseMFaces(sdna, blocks, &mesh.totface);
	parseMTFaces(sdna, blocks, &mesh);
	
	if (debug_basic)
	{
		std::ostringstream oss;
		oss << " name = " << mesh.name << "\n";
		oss << " totvert = " << mesh.totvert << "\n";
		oss << " totedge = " << mesh.totedge << "\n";
		oss << " totface = " << mesh.totface << "\n";
		oss << " additional vertices for UV mapping: " << mesh.supplMVerts.size() << "\n";
		Blender::Log(oss.str());
	}
	blenderCallback->meshLoaded(&mesh);
}

void Blender::parseFileBlocks(StructureDNA* sdna, vector<FileBlockHeader> &blocks, UVMapping uvMapping)
{
	for (unsigned long blockNum = 0; blockNum < blocks.size(); blockNum++)
	{
		FileBlockHeader* fbh = &blocks[blockNum];
		const char* structureName = getStructureName(sdna, fbh);
		if (debug_basic)
		{
			std::ostringstream oss;
			oss << "--" << fbh->code << " " << structureName << " size == " << fbh->size << " # == " << fbh->count << "\n";
			Blender::Log(oss.str());
		}
		if (strcmp("Mesh", structureName) == 0)
		{
			parseMesh(sdna, blocks, uvMapping);
		}
	}
}


bool Blender::parseBlenderFile(std::string filename, UVMapping uvMapping)
{
	vector<FileBlockHeader> fileBlocks;
	ifstream bfile(filename.c_str(), ios::in | ios::binary);
	if (!bfile)
	{
		Log("file not found: " + filename + "\n");
		return false;
	}
	if (debug_basic) Log("file opened: " + filename + "\n");
	
	// basic assumptions about data types:
	if (sizeof(char) != 1 || sizeof(unsigned long) != 4 || sizeof(float) != 4)
	{
		Log("invalid data type lengths detected. compile with different options.\n");
		return false;
	}
	
	// header processing:
	char header[12];
	bfile.read(header, 12);
	if (strncmp("BLENDER", &header[0], 7) == 0)
	{
		if (debug_basic) Log("Blender id found\n");
	}
	else
	{
		// check for gzip file:
		if (header[0] == '\x1f' && header[1] == '\x8b')
		{
			Log("compressed file cannot be read (you have to disable compression in save dialog of blender): " + filename + "\n");
		}
		else
		{
			Log("no blender file: " + filename + "\n");
		}
		return false;
	}
	if (header[7] == '_')
		blenderDesc.pointersize = 4;
	else if (header[7] == '-')
	{
		Log("pointer size 8 bytes not supported\n");
		return false;
	}
	else
	{
		Log("unknown pointer size\n");
		return false;
	}
	if (header[8] == 'v')
		blenderDesc.littleEndian = true;
	else if (header[8] == 'V')
	{
		blenderDesc.littleEndian = false;
		Log("big endian not supported\n");
		return false;
	}
	else
	{
		Log("unknown endianness\n");
		return false;
	}
	blenderDesc.version[0] = header[9];
	blenderDesc.version[1] = header[10];
	blenderDesc.version[2] = header[11];
	blenderDesc.version[3] = 0;
	
	StructureDNA sdna;
	FileBlockHeader fbheader;
	fbheader.buf = 0;
	bool mesh_found = false;            // true for first Mesh found
	bool in_data_gathering = false;     // true while gathering DATA block for Mesh
	bool done = false;                  // done if first mesh and data was gathered
	do
	{
		// we are only interested in (first) Mesh block and subsequent DATA blocks
		getFileBlock(&fbheader, &bfile);
		//Log(std::string("Header code: ") + fbheader.code + "\n");
		if (strcmp(fbheader.code, "DNA1") == 0)
		{
			bool ok = parseStructureDNA(&sdna, &fbheader);
			if (!ok) return false;
			fbheader.release();
		}
		else
		{
			if (!done && strcmp(fbheader.code, "ME") == 0)
			{
				mesh_found = true;
			}
			// store mesh file block and its data file blocks
			if (mesh_found)
			{
				fileBlocks.push_back(fbheader);
				mesh_found = false;
				in_data_gathering = true;
			}
			else if (in_data_gathering && strcmp(fbheader.code, "DATA") == 0)
			{
				fileBlocks.push_back(fbheader);
			}
			else
			{
				if (in_data_gathering)
				{
					// we already gathered mesh and data blocks
					done = true;
					in_data_gathering = false;
				}
				fbheader.release();
			}
		}
	}
	while (strcmp(fbheader.code, "ENDB") != 0);
	
	bfile.close();
	if (debug_basic) Log("file closed\n");
	
	//
	parseFileBlocks(&sdna, fileBlocks, uvMapping);
	
	// cleanup:
	for (unsigned long k = 0; k < fileBlocks.size(); k++)
	{
		fileBlocks[k].release();
	}
	return true;
}

// mesh implementations:
Mesh::Mesh(UVMapping mode)
{
	this->mverts = 0;
	this->mfaces = 0;
	this->mtfaces = 0;
	this->mappingMode = mode;
	this->supplMVerts.clear();
}

Mesh::~Mesh(void)
{
	if (this->mverts)
	{
		delete []mverts;
	}
	if (this->mfaces)
	{
		delete []mfaces;
	}
	if (this->mtfaces)
	{
		delete []mtfaces;
	}
}

