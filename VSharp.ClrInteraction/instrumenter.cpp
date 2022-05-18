#include "instrumenter.h"
#include "logging.h"
#include "cComPtr.h"
#include "reflection.h"
#include <vector>
#include <map>
#include <stdexcept>
#include <corhlpr.cpp>
#include "memory/memory.h"

using namespace vsharp;


#define TOKENPASTE(x, y) x ## y
#define TOKENPASTE2(x, y) TOKENPASTE(x, y)
#define UNIQUE TOKENPASTE2(Sig, __LINE__)
#define SIG_DEF(...) \
    constexpr COR_SIGNATURE UNIQUE[] = {__VA_ARGS__};\
    IfFailRet(metadataEmit->GetTokenFromSig(UNIQUE, sizeof(UNIQUE), &signatureToken));\
    tokens.push_back(signatureToken);

#define ELEMENT_TYPE_COND ELEMENT_TYPE_I
#define ELEMENT_TYPE_TOKEN ELEMENT_TYPE_U4
#define ELEMENT_TYPE_OFFSET ELEMENT_TYPE_I4
#define ELEMENT_TYPE_SIZE ELEMENT_TYPE_U

struct MethodBodyInfo {
    BYTE instrumentationEnabled;
    unsigned token;
    unsigned codeLength;
    unsigned assemblyNameLength;
    unsigned moduleNameLength;
    unsigned maxStackSize;
    unsigned ehsLength;
    unsigned signatureTokensLength;
    char *signatureTokens;
    const WCHAR *assemblyName;
    const WCHAR *moduleName;
    const char *bytecode;
    const char *ehs;

    void serialize(char *&bytes, unsigned &count) const {
        count = codeLength + sizeof(BYTE) + 6 * sizeof(unsigned) + ehsLength + assemblyNameLength + moduleNameLength + signatureTokensLength;
        bytes = new char[count];
        char *buffer = bytes;
        *(BYTE *)buffer = instrumentationEnabled; buffer += sizeof(BYTE);
        unsigned size = sizeof(unsigned);
        *(unsigned *)buffer = token; buffer += size;
        *(unsigned *)buffer = codeLength; buffer += size;
        *(unsigned *)buffer = assemblyNameLength; buffer += size;
        *(unsigned *)buffer = moduleNameLength; buffer += size;
        *(unsigned *)buffer = maxStackSize; buffer += size;
        *(unsigned *)buffer = signatureTokensLength;
        buffer += size; size = signatureTokensLength;
        memcpy(buffer, signatureTokens, size);
        buffer += size; size = assemblyNameLength;
        memcpy(buffer, (char*)assemblyName, size);
        buffer += size; size = moduleNameLength;
        memcpy(buffer, (char*)moduleName, size);
        buffer += size; size = codeLength;
        memcpy(buffer, bytecode, size);
        buffer += size; size = ehsLength;
        memcpy(buffer, ehs, size);
    }
};

HRESULT initTokens(const CComPtr<IMetaDataEmit> &metadataEmit, std::vector<mdSignature> &tokens) {
    HRESULT hr;
    mdSignature signatureToken;
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x00, ELEMENT_TYPE_VOID)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x00, ELEMENT_TYPE_COND)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x00, ELEMENT_TYPE_I)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U4)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_COND, ELEMENT_TYPE_I)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_COND, ELEMENT_TYPE_U2)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_I1, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_I2, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_I4, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_I8, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_R4, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_R8, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_I, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I2)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_U2)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I8)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_R4)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_R8)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I1, ELEMENT_TYPE_SIZE)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_TOKEN)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_COND, ELEMENT_TYPE_I, ELEMENT_TYPE_I4)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_COND, ELEMENT_TYPE_I, ELEMENT_TYPE_I)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_COND, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I4)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I2)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I4)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I8)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_R4)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_R8)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I1, ELEMENT_TYPE_I1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2, ELEMENT_TYPE_I1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_I1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I8, ELEMENT_TYPE_I1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_R4, ELEMENT_TYPE_I1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_R8, ELEMENT_TYPE_I1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I8, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I8, ELEMENT_TYPE_I8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_R4, ELEMENT_TYPE_R4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_R8, ELEMENT_TYPE_R8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I1, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_I2)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_VOID, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U2, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_R4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_R8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_TOKEN, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x02, ELEMENT_TYPE_VOID, ELEMENT_TYPE_SIZE, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I2, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_R4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_R8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_TOKEN, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x03, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_U2, ELEMENT_TYPE_SIZE)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U2, ELEMENT_TYPE_I4, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U2, ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U2, ELEMENT_TYPE_I8, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U2, ELEMENT_TYPE_I8, ELEMENT_TYPE_I8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U2, ELEMENT_TYPE_R4, ELEMENT_TYPE_R4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U2, ELEMENT_TYPE_R8, ELEMENT_TYPE_R8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U2, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U2, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I2, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_R4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_R8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I1, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_I8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_R4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_R8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_TOKEN, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_TOKEN, ELEMENT_TYPE_TOKEN, ELEMENT_TYPE_BOOLEAN, ELEMENT_TYPE_U2, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x06, ELEMENT_TYPE_VOID, ELEMENT_TYPE_TOKEN, ELEMENT_TYPE_U4, ELEMENT_TYPE_U4, ELEMENT_TYPE_U4, ELEMENT_TYPE_U4, ELEMENT_TYPE_I1)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x06, ELEMENT_TYPE_VOID, ELEMENT_TYPE_TOKEN, ELEMENT_TYPE_U4, ELEMENT_TYPE_U2, ELEMENT_TYPE_BOOLEAN, ELEMENT_TYPE_U4, ELEMENT_TYPE_U4)
    return S_OK;
}


Instrumenter::Instrumenter(ICorProfilerInfo8 &profilerInfo, Protocol &protocol)
    : m_profilerInfo(profilerInfo)
    , m_protocol(protocol)
    , m_methodMalloc(nullptr)
    , m_moduleId(0)
    , m_signatureTokens(nullptr)
    , m_generateTinyHeader(false)
    , m_pEH(nullptr)
{
    moduleIDs = std::map<INT32, ModuleID>();
    protocol.kek(this);
}

Instrumenter::~Instrumenter()
{
    delete[] m_signatureTokens;
}

unsigned Instrumenter::codeSize() const
{
    return m_codeSize;
}

char *Instrumenter::code() const
{
    return m_code;
}

unsigned Instrumenter::ehCount() const
{
    return m_nEH * sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT);
}

IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT *Instrumenter::ehs() const
{
    return m_pEH;
}

unsigned Instrumenter::maxStackSize() const
{
    return m_maxStack;
}

HRESULT Instrumenter::setILFunctionBody(LPCBYTE pBody)
{
    return m_profilerInfo.SetILFunctionBody(m_moduleId, m_jittedToken, pBody);
}

LPBYTE Instrumenter::allocateILMemory(unsigned size)
{
    if (FAILED(m_profilerInfo.GetILFunctionBodyAllocator(m_moduleId, &m_methodMalloc)))
        return nullptr;

    return (LPBYTE)m_methodMalloc->Alloc(size);
}

HRESULT Instrumenter::importIL()
{
    HRESULT hr;
    LPCBYTE pMethodBytes;

    IfFailRet(m_profilerInfo.GetILFunctionBody(m_moduleId, m_jittedToken, &pMethodBytes, NULL));

    COR_ILMETHOD_DECODER decoder((COR_ILMETHOD*)pMethodBytes);

    // Import the header flags
    m_tkLocalVarSig = decoder.GetLocalVarSigTok();
    m_maxStack = decoder.GetMaxStack();
    m_flags = (decoder.GetFlags() & CorILMethod_InitLocals);

    m_codeSize = decoder.GetCodeSize();
    m_code = new char[m_codeSize];
    memcpy(m_code, decoder.Code, m_codeSize);

    IfFailRet(importEH(decoder.EH, decoder.EHCount()));

    return S_OK;
}

HRESULT Instrumenter::importEH(const COR_ILMETHOD_SECT_EH* pILEH, unsigned nEH)
{
    assert(m_pEH == nullptr);

    m_nEH = nEH;

    if (nEH == 0)
        return S_OK;

    IfNullRet(m_pEH = new IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT[m_nEH]);
    for (unsigned iEH = 0; iEH < m_nEH; iEH++)
    {
        // If the EH clause is in tiny form, the call to pILEH->EHClause() below will
        // use this as a scratch buffer to expand the EH clause into its fat form.
        COR_ILMETHOD_SECT_EH_CLAUSE_FAT scratch;
        const COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo;
        ehInfo = (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)pILEH->EHClause(iEH, &scratch);
        memcpy(m_pEH + iEH, ehInfo, sizeof (IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
    }

    return S_OK;
}


HRESULT Instrumenter::exportIL(char *bytecode, unsigned codeLength, unsigned maxStackSize, char *ehs, unsigned ehsLength)
{
    HRESULT hr;

    // Use FAT header
    m_nEH = ehsLength / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT);
    m_maxStack = maxStackSize;

    unsigned alignedCodeSize = (codeLength + 3) & ~3;

    unsigned totalSize = sizeof(IMAGE_COR_ILMETHOD_FAT) + alignedCodeSize +
        (m_nEH ? (sizeof(IMAGE_COR_ILMETHOD_SECT_FAT) + sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT) * m_nEH) : 0);

    LPBYTE pBody = allocateILMemory(totalSize);
    IfNullRet(pBody);

    BYTE * pCurrent = pBody;

    IMAGE_COR_ILMETHOD_FAT *pHeader = (IMAGE_COR_ILMETHOD_FAT *)pCurrent;
    pHeader->Flags = m_flags | (m_nEH ? CorILMethod_MoreSects : 0) | CorILMethod_FatFormat;
    pHeader->Size = sizeof(IMAGE_COR_ILMETHOD_FAT) / sizeof(DWORD);
    pHeader->MaxStack = m_maxStack;
    pHeader->CodeSize = codeLength;
    pHeader->LocalVarSigTok = m_tkLocalVarSig;

    pCurrent = (BYTE*)(pHeader + 1);

    CopyMemory(pCurrent, bytecode, codeLength);
    pCurrent += alignedCodeSize;

    if (m_nEH != 0)
    {
        IMAGE_COR_ILMETHOD_SECT_FAT *pEH = (IMAGE_COR_ILMETHOD_SECT_FAT *)pCurrent;
        pEH->Kind = CorILMethod_Sect_EHTable | CorILMethod_Sect_FatFormat;
        pEH->DataSize = (unsigned)(sizeof(IMAGE_COR_ILMETHOD_SECT_FAT) + sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT) * m_nEH);

        pCurrent = (BYTE*)(pEH + 1);

        for (unsigned iEH = 0; iEH < m_nEH; iEH++)
        {
            IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT * pDst = (IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT *)pCurrent;
            *pDst = *(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT *)(ehs + iEH * sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
            pCurrent = (BYTE*)(pDst + 1);
        }
    }

    IfFailRet(setILFunctionBody(pBody));

    if (m_pEH) {
        delete[] m_pEH;
        m_pEH = nullptr;
    }

    if (m_methodMalloc)
        m_methodMalloc->Release();

    return S_OK;
}

// TODO: if method was already reJITed, do not reJIT #do
void Instrumenter::startReJit(INT32 moduleToken, mdMethodDef methodToken) {
    assert(moduleIDs.find(moduleToken) != moduleIDs.end());
    ModuleID moduleId = moduleIDs[moduleToken];
    if (std::find(reJITedMethods.begin(), reJITedMethods.end(), std::make_pair(moduleId, methodToken)) != reJITedMethods.end())
        return;
    auto *modules = new ModuleID[1] { moduleId };
    auto *methods = new mdMethodDef[1] { methodToken };
    LOG(tout << "ReJIT of skipped method " << HEX(methodToken) << " is started" << std::endl);
    HRESULT hr = m_profilerInfo.RequestReJIT(1, modules, methods);
    reJITedMethods.emplace_back(moduleId, methodToken);
    if (FAILED(hr)) {
        LOG(tout << "reJIT error code = " << HEX(hr) << std::endl);
        FAIL_LOUD("startReJit: reJIT failed!");
    }
    delete[] modules;
    delete[] methods;
}

CommandType Instrumenter::getAndHandleCommand() {
    CommandType command;
    if (!m_protocol.acceptCommand(command)) FAIL_LOUD("Instrumenting: accepting command failed!");
    switch (command) {
        case ReadString: {
            char *string;
            if (!m_protocol.acceptString(string)) FAIL_LOUD("Instrumenting: accepting string failed!");
            unsigned index = allocateString(string);
            if (!m_protocol.sendStringsPoolIndex(index)) FAIL_LOUD("Instrumenting: sending strings internal pool index failed!");
            break;
        }
        case GetTypeTokenFromTypeRef: {
            WCHAR *wstring;
            if (!m_protocol.acceptWString(wstring)) FAIL_LOUD("Instrumenting: accepting name of typeRef failed!");
            auto *reflection = new Reflection(m_profilerInfo);
            reflection->configure(m_moduleId, m_jittedToken);
            mdToken typeSpec = reflection->getTypeRefByName(wstring);
            if (!m_protocol.sendToken(typeSpec)) FAIL_LOUD("Instrumenting: sending typeRef token failed!");
            delete reflection;
            delete[] wstring;
            break;
        }
        case GetTypeTokenFromTypeSpec: {
            WCHAR *wstring;
            if (!m_protocol.acceptWString(wstring)) FAIL_LOUD("Instrumenting: accepting name of typeSpec failed!");
            auto *reflection = new Reflection(m_profilerInfo);
            reflection->configure(m_moduleId, m_jittedToken);
            mdToken typeSpec = reflection->getTypeSpecByName(wstring);
            if (!m_protocol.sendToken(typeSpec)) FAIL_LOUD("Instrumenting: sending typeSpec token failed!");
            delete reflection;
            delete[] wstring;
            break;
        }
        case ParseTypeInfoFromMethod: {
            mdToken method;
            if (!m_protocol.acceptToken(method)) FAIL_LOUD("Instrumenting: accepting token of method to parse failed!");
            auto *reflection = new Reflection(m_profilerInfo);
            reflection->configure(m_moduleId, m_jittedToken);
            std::vector<mdToken> types = reflection->getTypeInfoFromMethod(method);
            delete reflection;
            if (!m_protocol.sendTypeInfoFromMethod(types)) FAIL_LOUD("Instrumenting: sending type info of method failed!");
            break;
        }
        case ParseFieldRefTypeToken: {
            mdToken fieldRef;
            if (!m_protocol.acceptToken(fieldRef)) FAIL_LOUD("Instrumenting: accepting fieldRef token to parse failed!");
            auto *reflection = new Reflection(m_profilerInfo);
            reflection->configure(m_moduleId, m_jittedToken);
            mdToken typeSpec = reflection->getTypeTokenFromFieldRef(fieldRef);
            if (!m_protocol.sendToken(typeSpec)) FAIL_LOUD("Instrumenting: sending typeSpec token failed!");
            delete reflection;
            break;
        }

        case ParseFieldDefTypeToken: {
            mdToken typeDef;
            mdToken fieldDef;
            if (!m_protocol.acceptToken(fieldDef)) FAIL_LOUD("Instrumenting: accepting fieldDef token to parse failed!");
            auto *reflection = new Reflection(m_profilerInfo);
            reflection->configure(m_moduleId, m_jittedToken);
            mdToken typeSpec = reflection->getTypeTokenFromFieldDef(fieldDef);
            if (!m_protocol.sendToken(typeSpec)) FAIL_LOUD("Instrumenting: sending typeSpec token failed!");
            delete reflection;
            break;
        }
        case ParseArgTypeToken: {
            mdToken method;
            INT32 argIndex;
            if (!m_protocol.acceptTokenAndInt32(method, argIndex)) FAIL_LOUD("Instrumenting: accepting argument index failed!");
            auto *reflection = new Reflection(m_profilerInfo);
            reflection->configure(m_moduleId, m_jittedToken);
            mdToken typeSpec = reflection->getTypeTokenFromParameter(method, argIndex);
            if (!m_protocol.sendToken(typeSpec)) FAIL_LOUD("Instrumenting: sending typeSpec token failed!");
            delete reflection;
            break;
        }
        case ParseLocalTypeToken: {
            INT32 localIndex;
            if (!m_protocol.acceptInt32(localIndex)) FAIL_LOUD("Instrumenting: accepting local index failed!");
            auto *reflection = new Reflection(m_profilerInfo);
            reflection->configure(m_moduleId, m_jittedToken);
            mdToken typeSpec = reflection->getTypeTokenFromLocal(m_tkLocalVarSig, localIndex);
            if (!m_protocol.sendToken(typeSpec)) FAIL_LOUD("Instrumenting: sending typeSpec token failed!");
            delete reflection;
            break;
        }
        case ParseReturnTypeToken: {
            auto *reflection = new Reflection(m_profilerInfo);
            reflection->configure(m_moduleId, m_jittedToken);
            mdToken typeToken = reflection->getTypeTokenOfReturnType(m_jittedToken);
            if (!m_protocol.sendToken(typeToken)) FAIL_LOUD("Instrumenting: sending type token failed!");
            delete reflection;
            break;
        }
        case ParseDeclaringTypeToken: {
            mdToken method;
            if (!m_protocol.acceptToken(method)) FAIL_LOUD("Instrumenting: accepting method token failed!");
            auto *reflection = new Reflection(m_profilerInfo);
            reflection->configure(m_moduleId, m_jittedToken);
            mdToken typeToken = reflection->getTypeTokenOfDeclaringType(method);
            if (!m_protocol.sendToken(typeToken)) FAIL_LOUD("Instrumenting: sending type token failed!");
            delete reflection;
            break;
        }
        default:
            break;
    }
    return command;
}

HRESULT Instrumenter::instrument(FunctionID functionId) {

    if (isMainLeft()) {
        // NOTE: main left, further instrumentation is not needed, so doing nothing
        while (true) { }
        return S_OK;
    }

    // TODO: analyze the IL code instead to understand that we've injected functions?

    HRESULT hr = S_OK;
    ClassID classId;
    ModuleID oldModuleId = m_moduleId;
    IfFailRet(m_profilerInfo.GetFunctionInfo(functionId, &classId, &m_moduleId, &m_jittedToken));
    assert((m_jittedToken & 0xFF000000L) == mdtMethodDef);

    LOG(tout << "Instrumenting token " << HEX(m_jittedToken) << "..." << std::endl);

    LPCBYTE baseLoadAddress;
    ULONG moduleNameLength;
    AssemblyID assembly;
    IfFailRet(m_profilerInfo.GetModuleInfo(m_moduleId, &baseLoadAddress, 0, &moduleNameLength, nullptr, &assembly));
    WCHAR *moduleName = new WCHAR[moduleNameLength];
    IfFailRet(m_profilerInfo.GetModuleInfo(m_moduleId, &baseLoadAddress, moduleNameLength, &moduleNameLength, moduleName, &assembly));
    ULONG assemblyNameLength;
    AppDomainID appDomainId;
    ModuleID startModuleId;
    IfFailRet(m_profilerInfo.GetAssemblyInfo(assembly, 0, &assemblyNameLength, nullptr, &appDomainId, &startModuleId));
    WCHAR *assemblyName = new WCHAR[assemblyNameLength];
    IfFailRet(m_profilerInfo.GetAssemblyInfo(assembly, assemblyNameLength, &assemblyNameLength, assemblyName, &appDomainId, &startModuleId));

    CComPtr<IMetaDataImport> metadataImport;
    CComPtr<IMetaDataEmit> metadataEmit;
    IfFailRet(m_profilerInfo.GetModuleMetaData(m_moduleId, ofRead | ofWrite, IID_IMetaDataImport, reinterpret_cast<IUnknown **>(&metadataImport)));
    IfFailRet(metadataImport->QueryInterface(IID_IMetaDataEmit, reinterpret_cast<void **>(&metadataEmit)));

    if (oldModuleId != m_moduleId) {
        delete[] m_signatureTokens;
        std::vector<mdSignature> tokens;
        initTokens(metadataEmit, tokens);
        m_signatureTokensLength = tokens.size() * sizeof(mdSignature);
        m_signatureTokens = new char[m_signatureTokensLength];
        memcpy(m_signatureTokens, (char *)&tokens[0], m_signatureTokensLength);
    }

    IfFailRet(importIL());

    BYTE isInstrumentationEnabled = instrumentationEnabled() ? 1 : 0;
    MethodBodyInfo info{
            isInstrumentationEnabled,
            (unsigned)m_jittedToken,
            (unsigned)codeSize(),
            (unsigned)(assemblyNameLength - 1) * sizeof(WCHAR),
            (unsigned)(moduleNameLength - 1) * sizeof(WCHAR),
            (unsigned)maxStackSize(),
            (unsigned)ehCount(),
            m_signatureTokensLength,
            m_signatureTokens,
            assemblyName,
            moduleName,
            code(),
            (char*)ehs()
    };
    if (!m_protocol.sendSerializable(InstrumentCommand, info)) FAIL_LOUD("Instrumenting: serialization of method failed!");
    LOG(tout << "Successfully sent method body!");
    char *bytecode; int length; unsigned maxStackSize; char *ehs; unsigned ehsLength;
#ifdef _DEBUG
    CommandType command;
    do {
        command = getAndHandleCommand();
    } while (command != ReadMethodBody);
#endif
    LOG(tout << "Reading method body back...");
    INT32 moduleToken;
    if (!m_protocol.acceptMethodBody(bytecode, moduleToken, length, maxStackSize, ehs, ehsLength)) FAIL_LOUD("Instrumenting: accepting method body failed!");
    moduleIDs[moduleToken] = m_moduleId;
    LOG(tout << "Exporting " << length << " IL bytes!");
    IfFailRet(exportIL(bytecode, length, maxStackSize, ehs, ehsLength));

    return hr;
}
