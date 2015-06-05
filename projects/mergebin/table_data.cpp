//*******************************************************
// mergebin
// Written by Robert Simpson (robert@blackcastlesoft.com)
//
// Released to the public domain, use at your own risk!
//*******************************************************

#include "mergebin/forward.h"
#include "mergebin/table_data.h"

CREATEINSTANCE g_arTableTypes[64] =
{
	CTableData::CreateInstance<CModuleTable>,
	CTableData::CreateInstance<CTypeRefTable>,
	CTableData::CreateInstance<CTypeDefTable>,
	CTableData::CreateInstance<CFieldPtrTable>,
	CTableData::CreateInstance<CFieldTable>,
	CTableData::CreateInstance<CMethodPtrTable>,
	CTableData::CreateInstance<CMethodTable>,
	CTableData::CreateInstance<CParamPtrTable>,
	CTableData::CreateInstance<CParamTable>,
	CTableData::CreateInstance<CInterfaceImplTable>,
	CTableData::CreateInstance<CMemberRefTable>,
	CTableData::CreateInstance<CConstantTable>,
	CTableData::CreateInstance<CCustomAttributeTable>,
	CTableData::CreateInstance<CFieldMarshalTable>,
	CTableData::CreateInstance<CDeclSecurityTable>,
	CTableData::CreateInstance<CClassLayoutTable>,
	CTableData::CreateInstance<CFieldLayoutTable>,
	CTableData::CreateInstance<CStandAloneSigTable>,
	CTableData::CreateInstance<CEventMapTable>,
	CTableData::CreateInstance<CEventPtrTable>,
	CTableData::CreateInstance<CEventTable>,
	CTableData::CreateInstance<CPropertyMapTable>,
	CTableData::CreateInstance<CPropertyPtrTable>,
	CTableData::CreateInstance<CPropertyTable>,
	CTableData::CreateInstance<CMethodSemanticsTable>,
	CTableData::CreateInstance<CMethodImplTable>,
	CTableData::CreateInstance<CModuleRefTable>,
	CTableData::CreateInstance<CTypeSpecTable>,
	CTableData::CreateInstance<CImplMapTable>,
	CTableData::CreateInstance<CFieldRVATable>,
	NULL,
};
