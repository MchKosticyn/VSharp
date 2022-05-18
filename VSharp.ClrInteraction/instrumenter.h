#ifndef INSTRUMENTER_H_
#define INSTRUMENTER_H_

#include <map>
#include "corProfiler.h"
#include "communication/protocol.h"
#include "cComPtr.h"

struct COR_ILMETHOD_SECT_EH;
struct IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT;

namespace vsharp {

class Protocol;

struct MethodInfo {
    unsigned token;
    char *bytecode;
    unsigned codeLength;
    unsigned maxStackSize;
    char *ehs;
    unsigned ehsLength;
};

class Instrumenter {
private:
    ICorProfilerInfo8 &m_profilerInfo;  // Does not have ownership
    IMethodMalloc *m_methodMalloc;  // Does not have ownership

    Protocol &m_protocol;

    mdMethodDef m_jittedToken;
    ModuleID m_moduleId;

    char *m_signatureTokens;
    unsigned m_signatureTokensLength;

    mdToken     m_tkLocalVarSig;
    unsigned    m_maxStack;
    unsigned    m_flags;
    bool        m_generateTinyHeader;

    unsigned    m_nEH;
    IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT *m_pEH;

    char        *m_code;
    unsigned    m_codeSize;

    std::map<INT32, ModuleID> moduleIDs;
    std::vector<std::pair<ModuleID, mdMethodDef>> reJITedMethods;

    unsigned codeSize() const;
    char *code() const;
    unsigned ehCount() const;
    IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT *ehs() const;
    unsigned maxStackSize() const;

    HRESULT setILFunctionBody(LPCBYTE pBody);
    LPBYTE allocateILMemory(unsigned size);

    HRESULT importIL();
    HRESULT importEH(const COR_ILMETHOD_SECT_EH* pILEH, unsigned nEH);
    HRESULT exportIL(char *bytecode, unsigned codeLength, unsigned maxStackSize, char *ehs, unsigned ehsLength);

    CommandType getAndHandleCommand();

public:
    explicit Instrumenter(ICorProfilerInfo8 &profilerInfo, Protocol &protocol);
    ~Instrumenter();

    const char *signatureTokens() const { return m_signatureTokens; }
    unsigned signatureTokensLength() const { return m_signatureTokensLength; }

    HRESULT instrument(FunctionID functionId);
    void startReJit(INT32 moduleToken, mdMethodDef methodToken);
};

}

#endif // INSTRUMENTER_H_
