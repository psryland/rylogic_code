//******************************************
// CPU info
//  Copyright (c) Sept 2010 Paul Ryland
//******************************************

#ifndef PR_CPUINFO_H
#define PR_CPUINFO_H

#include <intrin.h>
#include <sstream>

namespace pr
{
	struct CpuInfo
	{
		char CPUString[0x20];
		char CPUBrandString[0x40];
		int  nSteppingID;
		int  nModel;
		int  nFamily;
		int  nProcessorType;
		int  nExtendedmodel;
		int  nExtendedfamily;
		int  nBrandIndex;
		int  nCLFLUSHcachelinesize;
		int  nLogicalProcessors;
		int  nAPICPhysicalID;
		int  nFeatureInfo;
		int  nCacheLineSize;
		int  nL2Associativity;
		int  nCacheSizeK;
		int  nPhysicalAddress;
		int  nVirtualAddress;
		int  nRet;
		int  nCores;
		struct cache_info_t
		{
			int  nCacheType;
			int  nCacheLevel;
			bool bSelfInit;
			bool bFullyAssociative;
			int  nMaxThread;
			int  nSysLineSize;
			int  nPhysicalLinePartitions;
			int  nWaysAssociativity;
			int  nNumberSets;
		}    CacheInfo[5];
		int  nCacheInfoCount;

		bool bSSE3Instructions;
		bool bMONITOR_MWAIT;
		bool bCPLQualifiedDebugStore;
		bool bVirtualMachineExtensions;
		bool bEnhancedIntelSpeedStepTechnology;
		bool bThermalMonitor2;
		bool bSupplementalSSE3;
		bool bL1ContextID;
		bool bCMPXCHG16B;
		bool bxTPRUpdateControl;
		bool bPerfDebugCapabilityMSR;
		bool bSSE41Extensions;
		bool bSSE42Extensions;
		bool bPOPCNT;
		bool bMultithreading;
		bool bLAHF_SAHFAvailable;
		bool bCmpLegacy;
		bool bSVM;
		bool bExtApicSpace;
		bool bAltMovCr8;
		bool bLZCNT;
		bool bSSE4A;
		bool bMisalignedSSE;
		bool bPREFETCH;
		bool bSKINITandDEV;
		bool bSYSCALL_SYSRETAvailable;
		bool bExecuteDisableBitAvailable;
		bool bMMXExtensions;
		bool bFFXSR;
		bool b1GBSupport;
		bool bRDTSCP;
		bool b64Available;
		bool b3DNowExt;
		bool b3DNow;
		bool bNestedPaging;
		bool bLBRVisualization;
		bool bFP128;
		bool bMOVOptimization;
		
		void ReadCPUInfo(int (&cpu_info)[4], int info_type)
		{
			#ifdef _MSC_VER
			__cpuid(cpu_info, info_type);
			#else
			#endif
		}
		void ReadCPUInfo(int (&cpu_info)[4], int info_type, int ECXvalue)
		{
			#ifdef _MSC_VER
			__cpuidex(cpu_info, info_type, ECXvalue);
			#else
			#endif
		}
		
		CpuInfo()
		{
			memset(this, 0, sizeof(CpuInfo));
			int CPUInfo[4] = {-1};

			// __cpuid with an InfoType argument of 0 returns the number of
			// valid Ids in CPUInfo[0] and the CPU identification string in
			// the other three array elements. The CPU identification string is
			// not in linear order. The code below arranges the information 
			// in a human readable form.
			ReadCPUInfo(CPUInfo, 0);
			unsigned int nIds = CPUInfo[0];
			memset(CPUString, 0, sizeof(CPUString));
			*((int*)CPUString) = CPUInfo[1];
			*((int*)(CPUString+4)) = CPUInfo[3];
			*((int*)(CPUString+8)) = CPUInfo[2];

			// Get the information associated with each valid Id
			if (nIds >= 1)
			{
				ReadCPUInfo(CPUInfo, 1);

				// Interpret CPU feature information.
				nSteppingID                       = (CPUInfo[0]      ) & 0xf;
				nModel                            = (CPUInfo[0] >>  4) & 0xf;
				nFamily                           = (CPUInfo[0] >>  8) & 0xf;
				nProcessorType                    = (CPUInfo[0] >> 12) & 0x3;
				nExtendedmodel                    = (CPUInfo[0] >> 16) & 0xf;
				nExtendedfamily                   = (CPUInfo[0] >> 20) & 0xff;
				nBrandIndex                       = (CPUInfo[1]      ) & 0xff;
				nCLFLUSHcachelinesize             = ((CPUInfo[1] >> 8) & 0xff) * 8;
				nLogicalProcessors                = ((CPUInfo[1] >>16) & 0xff);
				nAPICPhysicalID                   = (CPUInfo[1] >> 24) & 0xff;
				bSSE3Instructions                 = (CPUInfo[2] & 0x1) || false;
				bMONITOR_MWAIT                    = (CPUInfo[2] & 0x8) || false;
				bCPLQualifiedDebugStore           = (CPUInfo[2] & 0x10) || false;
				bVirtualMachineExtensions         = (CPUInfo[2] & 0x20) || false;
				bEnhancedIntelSpeedStepTechnology = (CPUInfo[2] & 0x80) || false;
				bThermalMonitor2                  = (CPUInfo[2] & 0x100) || false;
				bSupplementalSSE3                 = (CPUInfo[2] & 0x200) || false;
				bL1ContextID                      = (CPUInfo[2] & 0x300) || false;
				bCMPXCHG16B                       = (CPUInfo[2] & 0x2000) || false;
				bxTPRUpdateControl                = (CPUInfo[2] & 0x4000) || false;
				bPerfDebugCapabilityMSR           = (CPUInfo[2] & 0x8000) || false;
				bSSE41Extensions                  = (CPUInfo[2] & 0x80000) || false;
				bSSE42Extensions                  = (CPUInfo[2] & 0x100000) || false;
				bPOPCNT                           = (CPUInfo[2] & 0x800000) || false;
				nFeatureInfo                      = (CPUInfo[3]);
				bMultithreading                   = (nFeatureInfo & (1 << 28)) || false;
			}

			// Calling __cpuid with 0x80000000 as the InfoType argument
			// gets the number of valid extended IDs.
			ReadCPUInfo(CPUInfo, 0x80000000);
			unsigned int nExIds = CPUInfo[0];
			memset(CPUBrandString, 0, sizeof(CPUBrandString));

			// Get the information associated with each extended ID.
			for (unsigned int i = 0x80000000; i <= nExIds; ++i)
			{
				ReadCPUInfo(CPUInfo, i);

				if  (i == 0x80000001)
				{
					bLAHF_SAHFAvailable         = (CPUInfo[2] & 0x1) || false;
					bCmpLegacy                  = (CPUInfo[2] & 0x2) || false;
					bSVM                        = (CPUInfo[2] & 0x4) || false;
					bExtApicSpace               = (CPUInfo[2] & 0x8) || false;
					bAltMovCr8                  = (CPUInfo[2] & 0x10) || false;
					bLZCNT                      = (CPUInfo[2] & 0x20) || false;
					bSSE4A                      = (CPUInfo[2] & 0x40) || false;
					bMisalignedSSE              = (CPUInfo[2] & 0x80) || false;
					bPREFETCH                   = (CPUInfo[2] & 0x100) || false;
					bSKINITandDEV               = (CPUInfo[2] & 0x1000) || false;
					bSYSCALL_SYSRETAvailable    = (CPUInfo[3] & 0x800) || false;
					bExecuteDisableBitAvailable = (CPUInfo[3] & 0x10000) || false;
					bMMXExtensions              = (CPUInfo[3] & 0x40000) || false;
					bFFXSR                      = (CPUInfo[3] & 0x200000) || false;
					b1GBSupport                 = (CPUInfo[3] & 0x400000) || false;
					bRDTSCP                     = (CPUInfo[3] & 0x8000000) || false;
					b64Available                = (CPUInfo[3] & 0x20000000) || false;
					b3DNowExt                   = (CPUInfo[3] & 0x40000000) || false;
					b3DNow                      = (CPUInfo[3] & 0x80000000) || false;
				}
				// Interpret CPU brand string and cache information.
				else if (i == 0x80000002) memcpy(CPUBrandString     , CPUInfo, sizeof(CPUInfo));
				else if (i == 0x80000003) memcpy(CPUBrandString + 16, CPUInfo, sizeof(CPUInfo));
				else if (i == 0x80000004) memcpy(CPUBrandString + 32, CPUInfo, sizeof(CPUInfo));
				else if (i == 0x80000006)
				{
					nCacheLineSize   = CPUInfo[2] & 0xff;
					nL2Associativity = (CPUInfo[2] >> 12) & 0xf;
					nCacheSizeK      = (CPUInfo[2] >> 16) & 0xffff;
				}
				else if (i == 0x80000008)
				{
				   nPhysicalAddress = CPUInfo[0] & 0xff;
				   nVirtualAddress  = (CPUInfo[0] >> 8) & 0xff;
				}
				else if  (i == 0x8000000A)
				{
					bNestedPaging     = (CPUInfo[3] & 0x1) || false;
					bLBRVisualization = (CPUInfo[3] & 0x2) || false;
				}
				else if  (i == 0x8000001A)
				{
					bFP128           = (CPUInfo[0] & 0x1) || false;
					bMOVOptimization = (CPUInfo[0] & 0x2) || false;
				}
			}
	
			for (int i = 0; i != 5; ++i)
			{
				ReadCPUInfo(CPUInfo, 0x4, i);
				if (!(CPUInfo[0] & 0xf0)) break;

				if (i == 0) nCores = (CPUInfo[0] >> 26) + 1;
				CacheInfo[i].nCacheType              = ((CPUInfo[0] & 0x1f));
				CacheInfo[i].nCacheLevel             = ((CPUInfo[0] & 0xe0) >> 5) + 1;
				CacheInfo[i].bSelfInit               = ((CPUInfo[0] & 0x100) >> 8) || false;
				CacheInfo[i].bFullyAssociative       = ((CPUInfo[0] & 0x200) >> 9) || false;
				CacheInfo[i].nMaxThread              = ((CPUInfo[0] & 0x03ffc000) >> 14) + 1;
				CacheInfo[i].nSysLineSize            = ((CPUInfo[1] & 0x0fff)) + 1;
				CacheInfo[i].nPhysicalLinePartitions = ((CPUInfo[1] & 0x03ff000) >> 12) + 1;
				CacheInfo[i].nWaysAssociativity      = ((CPUInfo[1]) >> 22) + 1;
				CacheInfo[i].nNumberSets             = ((CPUInfo[2])) + 1;
				++nCacheInfoCount;
			}
		}
	
		// Generate a report of the cpuinfo
		std::string Report()
		{
			std::stringstream s;
			
			s << "CPU String: "<<CPUString<<"\n";
			if (nSteppingID)                             s << "Stepping ID = "<<nSteppingID<<"\n";
			if (nModel)                                  s << "Model = "<<nModel<<"\n";
			if (nFamily)                                 s << "Family = "<<nFamily<<"\n";
			if (nProcessorType)                          s << "Processor Type = "<<nProcessorType<<"\n";
			if (nExtendedmodel)                          s << "Extended model = "<<nExtendedmodel<<"\n";
			if (nExtendedfamily)                         s << "Extended family = "<<nExtendedfamily<<"\n";
			if (nBrandIndex)                             s << "Brand Index = "<<nBrandIndex<<"\n";
			if (nCLFLUSHcachelinesize)                   s << "CLFLUSH cache line size = "<<nCLFLUSHcachelinesize<<"\n";
			if (bMultithreading && nLogicalProcessors>0) s << "Logical Processor Count = "<<nLogicalProcessors<<"\n";
			if (nAPICPhysicalID)                         s << "APIC Physical ID = "<<nAPICPhysicalID<<"\n";

			s << "\n";
			s << "The following features are supported:\n";
			if (bSSE3Instructions)                       s << "\tSSE3\n";
			if (bMONITOR_MWAIT)                          s << "\tMONITOR/MWAIT\n";
			if (bCPLQualifiedDebugStore)                 s << "\tCPL Qualified Debug Store\n";
			if (bVirtualMachineExtensions)               s << "\tVirtual Machine Extensions\n";
			if (bEnhancedIntelSpeedStepTechnology)       s << "\tEnhanced Intel SpeedStep Technology\n";
			if (bThermalMonitor2)                        s << "\tThermal Monitor 2\n";
			if (bSupplementalSSE3)                       s << "\tSupplemental Streaming SIMD Extensions 3\n";
			if (bL1ContextID)                            s << "\tL1 Context ID\n";
			if (bCMPXCHG16B)                             s << "\tCMPXCHG16B Instruction\n";
			if (bxTPRUpdateControl)                      s << "\txTPR Update Control\n";
			if (bPerfDebugCapabilityMSR)                 s << "\tPerf\\Debug Capability MSR\n";
			if (bSSE41Extensions)                        s << "\tSSE4.1 Extensions\n";
			if (bSSE42Extensions)                        s << "\tSSE4.2 Extensions\n";
			if (bPOPCNT)                                 s << "\tPPOPCNT Instruction\n";
			if (nFeatureInfo & (1 <<  0))                s << "\tx87 FPU On Chip\n";
			if (nFeatureInfo & (1 <<  1))                s << "\tVirtual-8086 Mode Enhancement\n";
			if (nFeatureInfo & (1 <<  2))                s << "\tDebugging Extensions\n";
			if (nFeatureInfo & (1 <<  3))                s << "\tPage Size Extensions\n";
			if (nFeatureInfo & (1 <<  4))                s << "\tTime Stamp Counter\n";
			if (nFeatureInfo & (1 <<  5))                s << "\tRDMSR and WRMSR Support\n";
			if (nFeatureInfo & (1 <<  6))                s << "\tPhysical Address Extensions\n";
			if (nFeatureInfo & (1 <<  7))                s << "\tMachine Check Exception\n";
			if (nFeatureInfo & (1 <<  8))                s << "\tCMPXCHG8B Instruction\n";
			if (nFeatureInfo & (1 <<  9))                s << "\tAPIC On Chip\n";
			if (nFeatureInfo & (1 << 10))                s << "\tUnknown1\n";
			if (nFeatureInfo & (1 << 11))                s << "\tSYSENTER and SYSEXIT\n";
			if (nFeatureInfo & (1 << 12))                s << "\tMemory Type Range Registers\n";
			if (nFeatureInfo & (1 << 13))                s << "\tPTE Global Bit\n";
			if (nFeatureInfo & (1 << 14))                s << "\tMachine Check Architecture\n";
			if (nFeatureInfo & (1 << 15))                s << "\tConditional Move/Compare Instruction\n";
			if (nFeatureInfo & (1 << 16))                s << "\tPage Attribute Table\n";
			if (nFeatureInfo & (1 << 17))                s << "\t36-bit Page Size Extension\n";
			if (nFeatureInfo & (1 << 18))                s << "\tProcessor Serial Number\n";
			if (nFeatureInfo & (1 << 19))                s << "\tCFLUSH Extension\n";
			if (nFeatureInfo & (1 << 20))                s << "\tUnknown2\n";
			if (nFeatureInfo & (1 << 21))                s << "\tDebug Store\n";
			if (nFeatureInfo & (1 << 22))                s << "\tThermal Monitor and Clock Ctrl\n";
			if (nFeatureInfo & (1 << 23))                s << "\tMMX Technology\n";
			if (nFeatureInfo & (1 << 24))                s << "\tFXSAVE/FXRSTOR\n";
			if (nFeatureInfo & (1 << 25))                s << "\tSSE Extensions\n";
			if (nFeatureInfo & (1 << 26))                s << "\tSSE2 Extensions\n";
			if (nFeatureInfo & (1 << 27))                s << "\tSelf Snoop\n";
			if (nFeatureInfo & (1 << 28))                s << "\tMultithreading Technology\n";
			if (nFeatureInfo & (1 << 29))                s << "\tThermal Monitor\n";
			if (nFeatureInfo & (1 << 30))                s << "\tUnknown4\n";
			if (nFeatureInfo & (1 << 31))                s << "\tPending Break Enable\n";
			if (bLAHF_SAHFAvailable)                     s << "\tLAHF/SAHF in 64-bit mode\n";
			if (bCmpLegacy)                              s << "\tCore multi-processing legacy mode\n";
			if (bSVM)                                    s << "\tSecure Virtual Machine\n";
			if (bExtApicSpace)                           s << "\tExtended APIC Register Space\n";
			if (bAltMovCr8)                              s << "\tAltMovCr8\n";
			if (bLZCNT)                                  s << "\tLZCNT instruction\n";
			if (bSSE4A)                                  s << "\tSSE4A (EXTRQ, INSERTQ, MOVNTSD, MOVNTSS)\n";
			if (bMisalignedSSE)                          s << "\tMisaligned SSE mode\n";
			if (bPREFETCH)                               s << "\tPREFETCH and PREFETCHW Instructions\n";
			if (bSKINITandDEV)                           s << "\tSKINIT and DEV support\n";
			if (bSYSCALL_SYSRETAvailable)                s << "\tSYSCALL/SYSRET in 64-bit mode\n";
			if (bExecuteDisableBitAvailable  )           s << "\tExecute Disable Bit\n";
			if (bMMXExtensions)                          s << "\tExtensions to MMX Instructions\n";
			if (bFFXSR)                                  s << "\tFFXSR\n";
			if (b1GBSupport)                             s << "\t1GB page support\n";
			if (bRDTSCP)                                 s << "\tRDTSCP instruction\n";
			if (b64Available)                            s << "\t64 bit Technology\n";
			if (b3DNowExt)                               s << "\t3Dnow Ext\n";
			if (b3DNow)                                  s << "\t3Dnow! instructions\n";
			if (bNestedPaging)                           s << "\tNested Paging\n";
			if (bLBRVisualization)                       s << "\tLBR Visualization\n";
			if (bFP128)                                  s << "\tFP128 optimization\n";
			if (bMOVOptimization)                        s << "\tMOVU Optimization\n";
			s << "\n\t<end>\n";
			s << "\n";

			s << "CPU Brand String: "<<CPUBrandString<<"\n";
			s << "Cache Line Size = "<<nCacheLineSize<<"\n";
			s << "L2 Associativity = "<<nL2Associativity<<"\n";
			s << "Cache Size = "<<nCacheSizeK<<"K\n";
			s << "Number of Cores = "<<nCores<<"\n";
			s << "\n";

			for (int i = 0; i != nCacheInfoCount; ++i)
			{
				cache_info_t const& c = CacheInfo[i];

				s << "Cache Index "<<i<<"\n";
				switch (c.nCacheType)
				{
				default: s << "\tType: Unknown\n"; break;
				case 0:  s << "\tType: Null\n"; break;
				case 1:  s << "\tType: Data Cache\n"; break;
				case 2:  s << "\tType: Instruction Cache\n"; break;
				case 3:  s << "\tType: Unified Cache\n"; break;
				}
				s << "\tLevel = "<<c.nCacheLevel<<"\n"; 
				s << "\t"<<(c.bSelfInit?"":"Not ")<<"Self Initializing\n";
				s << "\tIs "<<(c.bFullyAssociative?"":"Not ")<<"Fully Associatve\n";
				s << "\tMax Threads = "<<c.nMaxThread<<"\n";
				s << "\tSystem Line Size = "<<c.nSysLineSize<<"\n";
				s << "\tPhysical Line Partions = "<<c.nPhysicalLinePartitions<<"\n";
				s << "\tWays of Associativity = "<<c.nWaysAssociativity<<"\n";
				s << "\tNumber of Sets = "<<c.nNumberSets<<"\n";
				s << "\n";
			}

			return s.str();
		}
	};
}

#endif
