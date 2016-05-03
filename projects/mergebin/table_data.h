//*******************************************************
// mergebin
// Written by Robert Simpson (robert@blackcastlesoft.com)
//
// Released to the public domain, use at your own risk!
//*******************************************************

#pragma once
#include "mergebin/forward.h"
#include "mergebin/meta_data_tables.h"

enum ETableTypes
{
	Module                 = 0,
	TypeRef                = 1,
	TypeDef                = 2,
	FieldPtr               = 3,
	Field                  = 4,
	MethodPtr              = 5,
	MethodDef              = 6,
	ParamPtr               = 7,
	Param                  = 8,
	InterfaceImpl          = 9,
	MemberRef              = 10,
	Constant               = 11,
	CustomAttribute        = 12,
	FieldMarshal           = 13,
	DeclSecurity           = 14,
	ClassLayout            = 15,
	FieldLayout            = 16,
	StandAloneSig          = 17,
	EventMap               = 18,
	EventPtr               = 19,
	Event                  = 20,
	PropertyMap            = 21,
	PropertyPtr            = 22,
	Property               = 23,
	MethodSemantics        = 24,
	MethodImpl             = 25,
	ModuleRef              = 26,
	TypeSpec               = 27,
	ImplMap                = 28,
	FieldRVA               = 29,
	ENCLog                 = 30,
	ENCMap                 = 31,
	Assembly               = 32,
	AssemblyProcessor      = 33,
	AssemblyOS             = 34,
	AssemblyRef            = 35,
	AssemblyRefProcessor   = 36,
	AssemblyRefOS          = 37,
	File                   = 38,
	ExportedType           = 39,
	ManifestResource       = 40,
	NestedClass            = 41,
	GenericParam           = 42,
	MethodSpec             = 43,
	GenericParamConstraint = 44,
};

struct TABLE_COLUMN
{
	UINT uSize;
	LPCTSTR pszName;
	DWORD dwOffset;
};

class CTableData
{
protected:
	CMetadataTables& m_tables;
	LPBYTE           m_pbData;
	mutable UINT     m_uiRowSize;
	TABLE_COLUMN*    m_pColumns;

private:
	void Init()
	{
		m_pbData = m_tables.m_pbData;
		for (UINT n = GetType(); n--;)
		{
			auto p = m_tables.GetTable(n);
			if (p)
			{
				m_pbData = p->m_pbData + (p->GetRowSize() * p->GetRowCount());
				break;
			}
		}

		m_pColumns = _CreateColumns();
		for (auto tc = m_pColumns + 1; tc->pszName && !tc->dwOffset; ++tc)
			tc->dwOffset = tc[-1].dwOffset + tc[-1].uSize;
	}

public:
	CTableData(CMetadataTables& tables)
		:m_tables(tables)
		,m_pbData()
		,m_uiRowSize()
		,m_pColumns()
	{}
	virtual ~CTableData()
	{
		if (m_pColumns)
			delete[] m_pColumns;
	}

	CTableData(CTableData const&) = delete;
	CTableData& operator=(CTableData const&) = delete;

	template<class C> static CTableData* CALLBACK CreateInstance(CMetadataTables *ptables)
	{
		auto p = static_cast<CTableData*>(new C(*ptables));
		p->Init();
		return p;
	}

	virtual UINT GetType() const = 0;
	virtual LPCTSTR GetName() const = 0;
	virtual TABLE_COLUMN *_CreateColumns() = 0;

	virtual DWORD GetRowCount() const
	{
		return *m_tables.TableRowCount(GetType());
	}
	virtual UINT GetRowSize() const
	{
		if (m_uiRowSize == 0)
		{
			for (auto pc = m_pColumns; pc->uSize; ++pc)
				m_uiRowSize += pc->uSize;
		}
		return m_uiRowSize;
	}
	int GetColumnIndex(LPCTSTR pszName) const
	{
		for (int n = 0; m_pColumns[n].pszName; n++)
			if (lstrcmpi(pszName, m_pColumns[n].pszName) == 0)
				return n;
		return -1;
	}
	UINT GetColumnSize(UINT uIndex) const
	{
		return m_pColumns[uIndex].uSize;
	}
	UINT GetColumnSize(LPCTSTR pszName) const
	{
		int n = GetColumnIndex(pszName);
		if (n < 0) return 0;
		return GetColumnSize(n);
	}
	UINT GetColumnCount() const
	{
		auto pc = m_pColumns;
		for (; pc->uSize; ++pc) {}
		return UINT(pc - m_pColumns);
	}
	BYTE* Column(UINT uRow, UINT uIndex) const
	{
		return m_pbData + (uRow * GetRowSize()) + m_pColumns[uIndex].dwOffset;
	}
	BYTE* Column(UINT uRow, LPCTSTR pszName) const
	{
		int n = GetColumnIndex(pszName);
		if (n < 0) return nullptr;
		return Column(uRow, n);
	}
	TABLE_COLUMN* GetColumns() const
	{
		return m_pColumns;
	}
};

static UINT TypeDefOrRefIndex[]        = {ETableTypes::TypeDef,ETableTypes::TypeRef,ETableTypes::TypeSpec, 0 };
static UINT HasConstantIndex[]         = {ETableTypes::Field,ETableTypes::Param,ETableTypes::Property, 0 };
static UINT HasCustomAttributeIndex[]  = {ETableTypes::MethodDef,ETableTypes::Field,ETableTypes::TypeRef,ETableTypes::TypeDef,ETableTypes::Param,ETableTypes::InterfaceImpl,ETableTypes::MemberRef,ETableTypes::Module,ETableTypes::DeclSecurity,ETableTypes::Property,ETableTypes::Event,ETableTypes::StandAloneSig,ETableTypes::ModuleRef,ETableTypes::TypeSpec,ETableTypes::Assembly,ETableTypes::AssemblyRef,ETableTypes::File,ETableTypes::ExportedType,ETableTypes::ManifestResource, 0 };
static UINT HasFieldMarshalIndex[]     = {ETableTypes::Field,ETableTypes::Param, 0 };
static UINT HasDeclSecurityIndex[]     = {ETableTypes::TypeDef,ETableTypes::MethodDef,ETableTypes::Assembly, 0 };
static UINT MemberRefParentIndex[]     = {ETableTypes::TypeDef,ETableTypes::TypeRef,ETableTypes::ModuleRef,ETableTypes::MethodDef,ETableTypes::TypeSpec, 0 };
static UINT HasSemanticsIndex[]        = {ETableTypes::Event,ETableTypes::Property, 0 };
static UINT MethodDefOrRefIndex[]      = {ETableTypes::MethodDef,ETableTypes::MemberRef, 0 };
static UINT MemberForwardedIndex[]     = {ETableTypes::Field,ETableTypes::MethodDef, 0 };
static UINT ImplementationIndex[]      = {ETableTypes::File,ETableTypes::AssemblyRef,ETableTypes::ExportedType, 0 };
static UINT CustomAttributeTypeIndex[] = { 63, 63,ETableTypes::MethodDef,ETableTypes::MemberRef, 63, 0 };
static UINT ResolutionScopeIndex[]     = {ETableTypes::Module,ETableTypes::ModuleRef,ETableTypes::AssemblyRef,ETableTypes::TypeRef, 0 };
static UINT TypeOrMethodDefIndex[]     = {ETableTypes::TypeDef,ETableTypes::MethodDef, 0 };

// Helpers
#define STRING_INDEXSIZE   (m_tables.GetStringIndexSize())
#define GUID_INDEXSIZE     (m_tables.GetGuidIndexSize())
#define BLOB_INDEXSIZE     (m_tables.GetBlobIndexSize())
#define TABLE_ROWCOUNT(x)  (m_tables.TableRowCount(x)[0])
#define TABLE_INDEXSIZE(x) (TABLE_ROWCOUNT(x) > 65535 ? 4 : 2)
#define MAX_INDEXSIZE(x)   (m_tables.GetMaxIndexSizeOf(x))

#define DECLARE_TABLE(classname, typ, nam)\
	public:\
		classname##(CMetadataTables& tables) :CTableData(tables) {}\
		UINT GetType() const { return typ; }\
		LPCTSTR GetName() const { return _T(nam); }

#define BEGIN_COLUMN_MAP()\
	protected: \
		TABLE_COLUMN *_CreateColumns()\
		{\
			TABLE_COLUMN map[] =\
			{
#define COLUMN_ENTRY(name, size)\
				{UINT(size), _T(name), 0},
#define END_COLUMN_MAP()\
				{ 0U, NULL, 0 }\
			};\
			auto p = new TABLE_COLUMN[sizeof(map) / sizeof(TABLE_COLUMN)];\
			CopyMemory(p, map, sizeof(map));\
			return p;\
		}

class CModuleTable : public CTableData
{
	DECLARE_TABLE(CModuleTable, ETableTypes::Module, "Module")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Generation", sizeof(WORD))
		COLUMN_ENTRY("Name", STRING_INDEXSIZE)
		COLUMN_ENTRY("Mvid", GUID_INDEXSIZE)
		COLUMN_ENTRY("EncId", GUID_INDEXSIZE)
		COLUMN_ENTRY("EncBaseId", GUID_INDEXSIZE)
	END_COLUMN_MAP()
};

class CTypeRefTable :public CTableData
{
	DECLARE_TABLE(CTypeRefTable, ETableTypes::TypeRef, "TypeRef")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("ResolutionScope", MAX_INDEXSIZE(ResolutionScopeIndex))
		COLUMN_ENTRY("TypeName", STRING_INDEXSIZE)
		COLUMN_ENTRY("TypeNamespace", STRING_INDEXSIZE)
	END_COLUMN_MAP()
};

class CTypeDefTable : public CTableData
{
	DECLARE_TABLE(CTypeDefTable,ETableTypes::TypeDef, "TypeDef")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Flags", sizeof(DWORD))
		COLUMN_ENTRY("TypeName", STRING_INDEXSIZE)
		COLUMN_ENTRY("TypeNamespace", STRING_INDEXSIZE)
		COLUMN_ENTRY("Extends", MAX_INDEXSIZE(TypeDefOrRefIndex))
		COLUMN_ENTRY("FieldList", TABLE_INDEXSIZE(ETableTypes::Field))
		COLUMN_ENTRY("MethodList", TABLE_INDEXSIZE(ETableTypes::MethodDef))
	END_COLUMN_MAP()
};

class CFieldPtrTable : public CTableData
{
	DECLARE_TABLE(CFieldPtrTable,ETableTypes::FieldPtr, "FieldPtr")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Field", TABLE_INDEXSIZE(ETableTypes::Field))
	END_COLUMN_MAP()
};

class CFieldTable : public CTableData
{
	DECLARE_TABLE(CFieldTable,ETableTypes::Field, "Field")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Flags", sizeof(WORD))
		COLUMN_ENTRY("Name", STRING_INDEXSIZE)
		COLUMN_ENTRY("Signature", BLOB_INDEXSIZE)
	END_COLUMN_MAP()
};

class CMethodPtrTable : public CTableData
{
	DECLARE_TABLE(CMethodPtrTable,ETableTypes::MethodPtr, "MethodPtr")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Method", TABLE_INDEXSIZE(ETableTypes::MethodDef))
	END_COLUMN_MAP()
};

class CMethodTable : public CTableData
{
	DECLARE_TABLE(CMethodTable,ETableTypes::MethodDef, "Method")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("RVA", sizeof(DWORD))
		COLUMN_ENTRY("ImplFlags", sizeof(WORD))
		COLUMN_ENTRY("Flags", sizeof(WORD))
		COLUMN_ENTRY("Name", STRING_INDEXSIZE)
		COLUMN_ENTRY("Signature", BLOB_INDEXSIZE)
		COLUMN_ENTRY("Parameters", TABLE_INDEXSIZE(ETableTypes::Param))
	END_COLUMN_MAP()
};

class CParamPtrTable : public CTableData
{
	DECLARE_TABLE(CParamPtrTable,ETableTypes::ParamPtr, "ParamPtr")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Param", TABLE_INDEXSIZE(ETableTypes::Param))
	END_COLUMN_MAP()
};

class CParamTable : public CTableData
{
	DECLARE_TABLE(CParamTable,ETableTypes::Param, "Param")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Flags", sizeof(WORD))
		COLUMN_ENTRY("Sequence", sizeof(WORD))
		COLUMN_ENTRY("Name", STRING_INDEXSIZE)
	END_COLUMN_MAP()
};

class CInterfaceImplTable : public CTableData
{
	DECLARE_TABLE(CInterfaceImplTable,ETableTypes::InterfaceImpl, "Interface")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Class", TABLE_INDEXSIZE(ETableTypes::TypeDef))
		COLUMN_ENTRY("Interface", MAX_INDEXSIZE(TypeDefOrRefIndex))
	END_COLUMN_MAP()
};

class CMemberRefTable : public CTableData
{
	DECLARE_TABLE(CMemberRefTable,ETableTypes::MemberRef, "Member")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Class", MAX_INDEXSIZE(MemberRefParentIndex))
		COLUMN_ENTRY("Name", STRING_INDEXSIZE)
		COLUMN_ENTRY("Signature", BLOB_INDEXSIZE)
	END_COLUMN_MAP()
};

class CConstantTable : public CTableData
{
	DECLARE_TABLE(CConstantTable,ETableTypes::Constant, "Constant")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Type", sizeof(WORD))
		COLUMN_ENTRY("Parent", MAX_INDEXSIZE(HasConstantIndex))
		COLUMN_ENTRY("Value", BLOB_INDEXSIZE)
	END_COLUMN_MAP()
};

class CCustomAttributeTable : public CTableData
{
	DECLARE_TABLE(CCustomAttributeTable,ETableTypes::CustomAttribute, "CustomAttribute")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Parent", MAX_INDEXSIZE(HasCustomAttributeIndex))
		COLUMN_ENTRY("Type", MAX_INDEXSIZE(CustomAttributeTypeIndex))
		COLUMN_ENTRY("Value", BLOB_INDEXSIZE)
	END_COLUMN_MAP()
};

class CFieldMarshalTable : public CTableData
{
	DECLARE_TABLE(CFieldMarshalTable,ETableTypes::FieldMarshal, "FieldMarshal")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Parent", MAX_INDEXSIZE(HasFieldMarshalIndex))
		COLUMN_ENTRY("NativeType", BLOB_INDEXSIZE)
	END_COLUMN_MAP()
};

class CDeclSecurityTable : public CTableData
{
	DECLARE_TABLE(CDeclSecurityTable,ETableTypes::DeclSecurity, "DeclSecurity")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Action", sizeof(WORD))
		COLUMN_ENTRY("Parent", MAX_INDEXSIZE(HasDeclSecurityIndex))
		COLUMN_ENTRY("PermissionSet", BLOB_INDEXSIZE)
	END_COLUMN_MAP()
};

class CClassLayoutTable : public CTableData
{
	DECLARE_TABLE(CClassLayoutTable,ETableTypes::ClassLayout, "ClassLayout")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("PackingSize", sizeof(WORD))
		COLUMN_ENTRY("ClassSize", sizeof(DWORD))
		COLUMN_ENTRY("Parent", TABLE_INDEXSIZE(ETableTypes::TypeDef))
	END_COLUMN_MAP()
};

class CFieldLayoutTable : public CTableData
{
	DECLARE_TABLE(CFieldLayoutTable,ETableTypes::FieldLayout, "FieldLayout")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Offset", sizeof(DWORD))
		COLUMN_ENTRY("Field", TABLE_INDEXSIZE(ETableTypes::Field))
	END_COLUMN_MAP()
};

class CStandAloneSigTable : public CTableData
{
	DECLARE_TABLE(CStandAloneSigTable,ETableTypes::StandAloneSig, "StandAloneSig")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Signature", BLOB_INDEXSIZE)
	END_COLUMN_MAP()
};

class CEventMapTable : public CTableData
{
	DECLARE_TABLE(CEventMapTable,ETableTypes::EventMap, "EventMap")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Parent", TABLE_INDEXSIZE(ETableTypes::TypeDef))
		COLUMN_ENTRY("EventList", TABLE_INDEXSIZE(ETableTypes::Event))
	END_COLUMN_MAP()
};

class CEventTable : public CTableData
{
	DECLARE_TABLE(CEventTable,ETableTypes::Event, "Event")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("EventFlags", sizeof(WORD))
		COLUMN_ENTRY("Name", STRING_INDEXSIZE)
		COLUMN_ENTRY("EventType", MAX_INDEXSIZE(TypeDefOrRefIndex))
	END_COLUMN_MAP()
};

class CEventPtrTable : public CTableData
{
	DECLARE_TABLE(CEventPtrTable,ETableTypes::EventPtr, "EventPtr")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Event", TABLE_INDEXSIZE(ETableTypes::Event))
	END_COLUMN_MAP()
};

class CPropertyMapTable : public CTableData
{
	DECLARE_TABLE(CPropertyMapTable,ETableTypes::PropertyMap, "PropertyMap")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Parent", TABLE_INDEXSIZE(ETableTypes::TypeDef))
		COLUMN_ENTRY("PropertyList", TABLE_INDEXSIZE(ETableTypes::Property))
	END_COLUMN_MAP()
};

class CPropertyPtrTable : public CTableData
{
	DECLARE_TABLE(CPropertyPtrTable,ETableTypes::EventPtr, "PropertyPtr")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Property", TABLE_INDEXSIZE(ETableTypes::Property))
	END_COLUMN_MAP()
};

class CPropertyTable : public CTableData
{
	DECLARE_TABLE(CPropertyTable,ETableTypes::Property, "Property")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Flags", sizeof(WORD))
		COLUMN_ENTRY("Name", STRING_INDEXSIZE)
		COLUMN_ENTRY("Type", BLOB_INDEXSIZE)
	END_COLUMN_MAP()
};

class CMethodSemanticsTable : public CTableData
{
	DECLARE_TABLE(CMethodSemanticsTable,ETableTypes::MethodSemantics, "MethodSemantics")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Semantics", sizeof(WORD))
		COLUMN_ENTRY("Method", TABLE_INDEXSIZE(ETableTypes::MethodDef))
		COLUMN_ENTRY("Association", MAX_INDEXSIZE(HasSemanticsIndex))
	END_COLUMN_MAP()
};

class CMethodImplTable : public CTableData
{
	DECLARE_TABLE(CMethodImplTable,ETableTypes::MethodImpl, "MethodImpl")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Class", TABLE_INDEXSIZE(ETableTypes::TypeDef))
		COLUMN_ENTRY("MethodBody", MAX_INDEXSIZE(MethodDefOrRefIndex))
		COLUMN_ENTRY("MethodDeclaration", MAX_INDEXSIZE(MethodDefOrRefIndex))
	END_COLUMN_MAP()
};

class CModuleRefTable : public CTableData
{
	DECLARE_TABLE(CModuleRefTable,ETableTypes::ModuleRef, "ModuleRef")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Name", STRING_INDEXSIZE)
	END_COLUMN_MAP()
};

class CTypeSpecTable : public CTableData
{
	DECLARE_TABLE(CTypeSpecTable,ETableTypes::TypeSpec, "TypeSpec")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("Signature", BLOB_INDEXSIZE)
	END_COLUMN_MAP()
};

class CImplMapTable : public CTableData
{
	DECLARE_TABLE(CImplMapTable,ETableTypes::ImplMap, "ImplMap")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("MappingFlags", sizeof(WORD))
		COLUMN_ENTRY("MemberForwarded", MAX_INDEXSIZE(MemberForwardedIndex))
		COLUMN_ENTRY("ImportName", STRING_INDEXSIZE)
		COLUMN_ENTRY("ImportScope", TABLE_INDEXSIZE(ETableTypes::ModuleRef))
	END_COLUMN_MAP()
};

class CFieldRVATable : public CTableData
{
	DECLARE_TABLE(CFieldRVATable,ETableTypes::FieldRVA, "FieldRVA")

	BEGIN_COLUMN_MAP()
		COLUMN_ENTRY("RVA", sizeof(DWORD))
		COLUMN_ENTRY("Field", TABLE_INDEXSIZE(ETableTypes::Field))
	END_COLUMN_MAP()
};

// Only tables up to ETableTypes::FieldRVA are mapped, because they're all I needed.  If you need the tables beyond that, map them yourself!

typedef CTableData* (CALLBACK* CREATEINSTANCE)(CMetadataTables *);
extern CREATEINSTANCE g_arTableTypes[64];