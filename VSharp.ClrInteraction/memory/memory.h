#ifndef MEMORY_H_
#define MEMORY_H_

#include "cor.h"
#include "stack.h"
#include "storage.h"
#include <functional>
#include <map>
#include <set>

#define staticSizeOfCoverageNode (2 * sizeof(int) + sizeof(mdMethodDef) + sizeof(OFFSET))
#define READ_BYTES(src, type) *(type*)(src); (src) += sizeof(type)
#define WRITE_BYTES(type, dest, src) *(type*)(dest) = (src); (dest) += sizeof(type)

typedef UINT_PTR ThreadID;

namespace vsharp {

extern std::function<ThreadID()> currentThread;
static std::map<ThreadID, Stack *> stacks;
static std::set<ThreadID> disabledThreadProbes;
extern Storage heap;
#ifdef _DEBUG
extern std::map<unsigned, const char*> stringsPool;
#endif

// Memory tracking

Stack &stack();
StackFrame &topFrame();

void mainLeft();
bool isMainLeft();

bool areProbesEnabled();
void enableProbesThread();
void disableProbesThread();

bool instrumentingEnabled();
void enableInstrumentation();
void disableInstrumentation();

void enterMain();
bool isMainEntered();

void getLock();
void freeLock();

unsigned allocateString(const char *s);

void validateStackEmptyness();

void resolve(INT_PTR p, VirtualAddress &vAddress);

// Exceptions handling

void throwException(UINT_PTR exception, bool concreteness);
void catchException();
void rethrowException();
void terminateByException();
bool isTerminatedByException();

enum ExceptionKind {
    Unhandled = 1,
    Caught = 2,
    NoException = 3
};

std::tuple<ExceptionKind, OBJID, bool> exceptionRegister();

// Matching function pointers on IDs

void addFunctionId(INT_PTR functionPtr, INT32 functionID);
INT32 getFunctionId(INT_PTR functionPtr);

// Coverage collection

enum StackPushType : BYTE {
    NoPush = 0,
    SymbolicPush = 1,
    ConcretePush = 2,
    StructPush = 3
};

struct StackPush {
    StackPushType pushType = NoPush;

private:
    // struct case only info; must be equal to null otherwise
    int structSize = 0;
    int fieldsLength = 0;
    std::pair<int, int> *symbolicFields = nullptr;

public:
    unsigned size() const;
    void serialize(char *&buffer) const;
    void deserialize(char *&buffer);
    void pushToTop(StackFrame &top) const;
};

struct CoverageNode {
    int moduleToken;
    mdMethodDef methodToken;
    OFFSET offset;
    int threadToken;
    StackPush stackPush;
    CoverageNode *next;

    unsigned size() const;
    int count() const;
    void serialize(char *&buffer) const;
    void deserialize(char *&buffer);
};

static const CoverageNode *expectedCoverageStep = nullptr;
static bool expectedCoverageExpirated = true;
static CoverageNode *lastCoverageStep = nullptr;
static CoverageNode *newCoverageNodes = nullptr;

void setExpectedCoverage(const CoverageNode *expectedCoverage);
void expectedStackPush(StackPush &stackPush);
bool addCoverageStep(OFFSET offset, StackPush &lastStackPush, bool &stillExpectsCoverage);
const CoverageNode *flushNewCoverageNodes();

}

#endif // MEMORY_H_
