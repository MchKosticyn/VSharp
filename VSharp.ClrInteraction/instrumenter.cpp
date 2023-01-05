#include "instrumenter.h"
#include "logging.h"
#include "cComPtr.h"
#include "reflection.h"
#include <vector>
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
        count = codeLength + 6 * sizeof(unsigned) + ehsLength + assemblyNameLength + moduleNameLength + signatureTokensLength;
        bytes = new char[count];
        char *buffer = bytes;
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
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U)
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
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_I1, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_I2, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_I8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_R4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_R8, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x04, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_OFFSET)
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
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I1, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I2, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_I8, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_R4, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
    SIG_DEF(IMAGE_CEE_CS_CALLCONV_STDCALL, 0x05, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I, ELEMENT_TYPE_I, ELEMENT_TYPE_R8, ELEMENT_TYPE_I4, ELEMENT_TYPE_OFFSET)
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
    , m_reJitInstrumentedStarted(false)
    , m_mainModuleName(nullptr)
    , m_mainModuleSize(0)
    , m_mainMethod(0)
    , m_mainReached(false)
{
}

Instrumenter::~Instrumenter()
{
    delete[] m_signatureTokens;
    delete[] m_mainModuleName;
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

void Instrumenter::configureEntryPoint() {
    char *bytes; int messageLength;
    m_protocol.acceptEntryPoint(bytes, messageLength);
    char *start = bytes;
    m_mainModuleSize = *(INT32*) bytes; bytes += sizeof(INT32);
    m_mainMethod = *(INT32*) bytes; bytes += sizeof(INT32);
    m_mainModuleName = new WCHAR[m_mainModuleSize];
    unsigned bytesCount = m_mainModuleSize * sizeof(WCHAR);
    memcpy(m_mainModuleName, bytes, m_mainModuleSize * sizeof(WCHAR)); bytes += bytesCount;
    assert(bytes - start == messageLength);
    delete[] start;
}

bool Instrumenter::currentMethodIsMain(const WCHAR *moduleName, int moduleSize, mdMethodDef method) const {
    // NOTE: decrementing 'moduleSize', because of null terminator
    if (m_mainModuleSize != moduleSize - 1 || m_mainMethod != method)
        return false;
    for (int i = 0; i < m_mainModuleSize; i++)
        if (m_mainModuleName[i] != moduleName[i]) return false;
    return true;
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

HRESULT Instrumenter::startReJitSkipped() {
    LOG(tout << "ReJIT of skipped methods is started" << std::endl);
    ULONG count = skippedBeforeMain.size();
    auto *modules = new ModuleID[count];
    auto *methods = new mdMethodDef[count];
    int i = 0;
    for (const auto &it : skippedBeforeMain) {
        modules[i] = it.first;
        methods[i] = it.second;
        i++;
    }
    HRESULT hr = m_profilerInfo.RequestReJIT(count, modules, methods);
    skippedBeforeMain.clear();
    delete[] modules;
    delete[] methods;
    return hr;
}

mdToken Instrumenter::FieldRefTypeToken(mdToken fieldRef) {
    auto *reflection = new Reflection(m_profilerInfo);
    reflection->configure(m_moduleId, m_jittedToken);
    mdToken typeSpec = reflection->getTypeTokenFromFieldRef(fieldRef);
    delete reflection;
    return typeSpec;
}

mdToken Instrumenter::FieldDefTypeToken(mdToken fieldDef) {
    auto *reflection = new Reflection(m_profilerInfo);
    reflection->configure(m_moduleId, m_jittedToken);
    mdToken typeSpec = reflection->getTypeTokenFromFieldDef(fieldDef);
    delete reflection;
    return typeSpec;
}

mdToken Instrumenter::ArgTypeToken(mdToken method, INT32 argIndex) {
    auto *reflection = new Reflection(m_profilerInfo);
    reflection->configure(m_moduleId, m_jittedToken);
    mdToken typeSpec = reflection->getTypeTokenFromParameter(method, argIndex);
    delete reflection;
    return typeSpec;
}

mdToken Instrumenter::LocalTypeToken(INT32 localIndex) {
    auto *reflection = new Reflection(m_profilerInfo);
    reflection->configure(m_moduleId, m_jittedToken);
    mdToken typeSpec = reflection->getTypeTokenFromLocal(m_tkLocalVarSig, localIndex);
    delete reflection;
    return typeSpec;
}

mdToken Instrumenter::ReturnTypeToken() {
    auto *reflection = new Reflection(m_profilerInfo);
    reflection->configure(m_moduleId, m_jittedToken);
    mdToken typeToken = reflection->getTypeTokenOfReturnType(m_jittedToken);
    delete reflection;
    return typeToken;
}

mdToken Instrumenter::DeclaringTypeToken(mdToken method) {
    auto *reflection = new Reflection(m_profilerInfo);
    reflection->configure(m_moduleId, m_jittedToken);
    mdToken typeToken = reflection->getTypeTokenOfDeclaringType(method);
    delete reflection;
    return typeToken;
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

HRESULT Instrumenter::doInstrumentation(ModuleID oldModuleId, const WCHAR *assemblyName, ULONG assemblyNameLength, const WCHAR *moduleName, ULONG moduleNameLength) {
    HRESULT hr;
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

    LOG(tout << "Instrumenting token " << HEX(m_jittedToken) << "..." << std::endl);

    IfFailRet(importIL());

    unsigned codeLength = codeSize();
    char *bytes = new char[codeLength];
    char *ehcs = new char[ehCount()];
    memcpy(bytes, code(), codeLength);
    memcpy(ehcs, ehs(), ehCount());
    MethodInfo mi = MethodInfo{m_jittedToken, bytes, codeLength, maxStackSize(), ehcs, ehCount()};
    instrumentedFunctions[{m_moduleId, m_jittedToken}] = mi;

//    MethodBodyInfo info{
//        (unsigned)m_jittedToken,
//        (unsigned)codeSize(),
//        (unsigned)(assemblyNameLength - 1) * sizeof(WCHAR),
//        (unsigned)(moduleNameLength - 1) * sizeof(WCHAR),
//        (unsigned)maxStackSize(),
//        (unsigned)ehCount(),
//        m_signatureTokensLength,
//        m_signatureTokens,
//        assemblyName,
//        moduleName,
//        code(),
//        (char*)ehs()
//    };
//    if (!m_protocol.sendSerializable(InstrumentCommand, info)) FAIL_LOUD("Instrumenting: serialization of method failed!");
    LOG(tout << "Successfully sent method body!");
    char *bytecodeR; int lengthR; int maxStackSizeR; char *ehsR; int ehsLengthR;
//    CommandType command;
//    do {
//        command = getAndHandleCommand();
//    } while (command != ReadMethodBody);
    LOG(tout << "Reading method body back...");
    tout << "imported " << codeSize() << " bytes" << std::endl;
    getLock();
    disableInstrumentation();
    m_protocol.instrumentR((unsigned)m_jittedToken, (unsigned)codeSize(), (unsigned)(assemblyNameLength - 1) * sizeof(WCHAR),
                          (unsigned)(moduleNameLength - 1) * sizeof(WCHAR),(unsigned)maxStackSize(),(unsigned)ehCount(),
                          m_signatureTokensLength,m_signatureTokens,assemblyName,moduleName,code(),(char*)ehs(),
                          // result
                          &bytecodeR, &lengthR, &maxStackSizeR, &ehsR, &ehsLengthR);
    enableInstrumentation();
    freeLock();
//    if (!m_protocol.acceptMethodBody(bytecode, length, maxStackSize, ehs, ehsLength))
//        FAIL_LOUD("Instrumenting: accepting method body failed!");
    LOG(tout << "Exporting " << lengthR << " IL bytes!");
    IfFailRet(exportIL(bytecodeR, lengthR, maxStackSizeR, ehsR, ehsLengthR));

    return S_OK;
}

HRESULT Instrumenter::instrument(FunctionID functionId, bool reJIT) {
    HRESULT hr = S_OK;
    ModuleID newModuleId;
    ClassID classId;
    IfFailRet(m_profilerInfo.GetFunctionInfo(functionId, &classId, &newModuleId, &m_jittedToken));
    assert((m_jittedToken & 0xFF000000L) == mdtMethodDef);

    if (!instrumentingEnabled() && !reJIT) {
        tout << "instrumentingEnabled = " << instrumentingEnabled() << std::endl;
        tout << "reJIT = " << reJIT << std::endl;
        // TODO: unify mainReached and instrumentingEnabled #do
//        skippedBeforeMain.insert({m_moduleId, m_jittedToken});
        LOG(tout << "Instrumentation of token " << HEX(m_jittedToken) << " is skipped" << std::endl);
        return S_OK;
    } else {
        // TODO: move this to probes #do
//        if (!skippedBeforeMain.empty()) IfFailRet(startReJitSkipped());
    }

    LPCBYTE baseLoadAddress;
    ULONG moduleNameLength;
    AssemblyID assembly;
    IfFailRet(m_profilerInfo.GetModuleInfo(newModuleId, &baseLoadAddress, 0, &moduleNameLength, nullptr, &assembly));
    WCHAR *moduleName = new WCHAR[moduleNameLength];
    IfFailRet(m_profilerInfo.GetModuleInfo(newModuleId, &baseLoadAddress, moduleNameLength, &moduleNameLength, moduleName, &assembly));
    ULONG assemblyNameLength;
    AppDomainID appDomainId;
    ModuleID startModuleId;
    IfFailRet(m_profilerInfo.GetAssemblyInfo(assembly, 0, &assemblyNameLength, nullptr, &appDomainId, &startModuleId));
    WCHAR *assemblyName = new WCHAR[assemblyNameLength];
    IfFailRet(m_profilerInfo.GetAssemblyInfo(assembly, assemblyNameLength, &assemblyNameLength, assemblyName, &appDomainId, &startModuleId));

    bool shouldInstrument = isMainEntered();
    if (!m_mainReached) {
        if (currentMethodIsMain(moduleName, (int) moduleNameLength, m_jittedToken)) {
            LOG(tout << "Main function reached!" << std::endl);
            m_mainReached = true;
            shouldInstrument = true;
            IfFailRet(startReJitSkipped());
        }
    }

    if (m_mainReached && shouldInstrument) {
        // TODO: analyze the IL code instead to understand that we've injected functions?
        if (instrumentedFunctions.find({newModuleId, m_jittedToken}) != instrumentedFunctions.end()) {
            LOG(tout << "Duplicate jitting of " << HEX(m_jittedToken) << std::endl);
            return S_OK;
        }
        if (!skippedBeforeMain.empty())
            IfFailRet(startReJitSkipped());
        if (isMainLeft()) {
            freeLock();
            // NOTE: main left, further instrumentation is not needed, so doing nothing
            while (true) { }
//            if (!m_reJitInstrumentedStarted)
//                IfFailRet(startReJitInstrumented());
//            LOG(tout << "Main left! Skipping instrumentation of " << HEX(m_jittedToken) << std::endl);
            return S_OK;
        }
        ModuleID oldModuleId = m_moduleId;
        m_moduleId = newModuleId;
        hr = doInstrumentation(oldModuleId, assemblyName, assemblyNameLength, moduleName, moduleNameLength);
    } else {
        tout << "m_mainReached = " << m_mainReached << std::endl;
        tout << "shouldInstrument = " << shouldInstrument << std::endl;
        LOG(tout << "Instrumentation of token " << HEX(m_jittedToken) << " is skipped" << std::endl);
        skippedBeforeMain.insert({newModuleId, m_jittedToken});
    }

    delete[] moduleName;
    delete[] assemblyName;

    return hr;
}

HRESULT Instrumenter::undoInstrumentation(FunctionID functionId) {
    HRESULT hr;
    ClassID classId;
    IfFailRet(m_profilerInfo.GetFunctionInfo(functionId, &classId, &m_moduleId, &m_jittedToken));
    assert((m_jittedToken & 0xFF000000L) == mdtMethodDef);
    const auto instrumented = instrumentedFunctions.find({m_moduleId, m_jittedToken});
    if (instrumented != instrumentedFunctions.end()) {
        MethodInfo mi = instrumented->second;
        LOG(tout << "Undo instrumentation token " << HEX(m_jittedToken) << "..." << std::endl);
        IfFailRet(exportIL(mi.bytecode, mi.codeLength, mi.maxStackSize, mi.ehs, mi.ehsLength));
        instrumentedFunctions.erase(instrumented);
    }
    return S_OK;
}

HRESULT Instrumenter::reInstrument(FunctionID functionId) {
    // NOTE: if main is left, rejit needs to delete probes
    // NOTE: otherwise, rejit needs to place probes
    if (isMainLeft())
        return undoInstrumentation(functionId);

    return instrument(functionId, true);
}
