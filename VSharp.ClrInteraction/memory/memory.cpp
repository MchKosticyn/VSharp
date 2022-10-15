#include "memory.h"
#include "stack.h"
#include <mutex>

using namespace vsharp;

ThreadID currentThreadNotConfigured() {
    throw std::logic_error("Current thread getter is not configured!");
}

std::function<ThreadID()> vsharp::currentThread(&currentThreadNotConfigured);

Storage vsharp::heap = Storage();

#ifdef _DEBUG
std::map<unsigned, const char*> vsharp::stringsPool;
int topStringIndex = 0;
#endif

ThreadID lastThreadID = 0;
Stack *currentStack = nullptr;

inline void switchContext() {
    ThreadID tid = currentThread();
    if (tid != lastThreadID) {
        lastThreadID = tid;
        Stack *&s = stacks[tid];
        if (!s) s = new Stack(heap);
        currentStack = s;
    }
}

Stack &vsharp::stack() {
    switchContext();
    return *currentStack;
}

StackFrame &vsharp::topFrame() {
    switchContext();
    return currentStack->topFrame();
}

void vsharp::validateStackEmptyness() {
#ifdef _DEBUG
    for (auto &kv : stacks) {
        if (!kv.second->isEmpty()) {
            FAIL_LOUD("Stack is not empty after program termination!");
        }
        if (!kv.second->opmemIsEmpty()) {
            FAIL_LOUD("Opmem is not empty after program termination!");
        }
    }
#endif
}

#ifdef _DEBUG
unsigned vsharp::allocateString(const char *s) {
    unsigned currentIndex = topStringIndex;
    // Place s into intern pool
    stringsPool[currentIndex] = s;
//    LOG(tout << "Allocated string '" << s << "' with index '" << currentIndex << "'");
    // Increment top index
    topStringIndex++;
    // Return string's index
    return currentIndex;
}
#endif

bool _mainLeft = false;

void vsharp::mainLeft() {
    _mainLeft = true;
}

bool vsharp::isMainLeft() {
    return _mainLeft;
}

bool instrumentationEnabled = true;

bool vsharp::instrumentingEnabled() {
    return instrumentationEnabled;
}

void vsharp::enableInstrumentation() {
    if (instrumentationEnabled)
        LOG(tout << "WARNING: enableInstrumentation, instrumentation already enabled" << std::endl);
    instrumentationEnabled = true;
}

void vsharp::disableInstrumentation() {
    if (!instrumentationEnabled)
        LOG(tout << "WARNING: disableInstrumentation, instrumentation already disabled" << std::endl);
    instrumentationEnabled = false;
}

bool mainEntered = false;

void vsharp::enterMain() {
    assert(!mainEntered);
    mainEntered = true;
}

bool vsharp::isMainEntered() {
    return mainEntered;
}

std::mutex mutex;

void vsharp::getLock() {
    mutex.lock();
}

void vsharp::freeLock() {
    mutex.unlock();
}

void vsharp::resolve(INT_PTR p, VirtualAddress &address) {
    heap.physToVirtAddress(p, address);
}

OBJID _exceptionRegister = 0;
ExceptionKind _exceptionKind = NoException;
bool _exceptionConcreteness = true;
bool _isTerminatedByException = false;

void vsharp::throwException(OBJID exception, bool concreteness) {
    _exceptionRegister = exception;
    _exceptionKind = Unhandled;
    _exceptionConcreteness = concreteness;
}

void vsharp::catchException() {
    _exceptionKind = Caught;
}

void vsharp::rethrowException() {
    assert(_exceptionKind == Caught);
    _exceptionKind = Unhandled;
}

void vsharp::terminateByException() {
    _isTerminatedByException = true;
}

bool vsharp::isTerminatedByException() {
    return _isTerminatedByException;
}

std::tuple<ExceptionKind, OBJID, bool> vsharp::exceptionRegister() {
    return std::make_tuple(_exceptionKind, _exceptionRegister, _exceptionConcreteness);
}

std::map<INT_PTR, INT32> functionIDs;

void vsharp::addFunctionId(INT_PTR functionPtr, INT32 functionID) {
    functionIDs[functionPtr] = functionID;
}

INT32 vsharp::getFunctionId(INT_PTR functionPtr) {
    return functionIDs[functionPtr];
}

void vsharp::setExpectedCoverage(const CoverageNode *expectedCoverage) {
    expectedCoverageStep = expectedCoverage;
    expectedCoverageExpirated = !expectedCoverage;
}

bool vsharp::addCoverageStep(OFFSET offset, StackPush &lastStackPush, bool &stillExpectsCoverage) {
    int threadToken = 0; // TODO: support multithreading
    StackFrame &top = topFrame();
    int moduleToken = top.moduleToken();
    mdMethodDef methodToken = top.resolvedToken();
    if (lastCoverageStep && lastCoverageStep->moduleToken == moduleToken && lastCoverageStep->methodToken == methodToken &&
            lastCoverageStep->offset == offset && lastCoverageStep->threadToken == threadToken)
    {
        stillExpectsCoverage = !expectedCoverageExpirated;
        expectedCoverageExpirated = !expectedCoverageStep;
        lastStackPush = lastCoverageStep->stackPush;
        return true;
    }
    if (expectedCoverageStep) {
        stillExpectsCoverage = true;
        if (expectedCoverageStep->moduleToken != moduleToken || expectedCoverageStep->methodToken != methodToken ||
                expectedCoverageStep->offset != offset || expectedCoverageStep->threadToken != threadToken) {
            LOG(tout << "Path divergence detected: expected method token " << HEX(expectedCoverageStep->methodToken) <<
                ", got method token " << HEX(methodToken) << ", expected offset " << HEX(expectedCoverageStep->offset) <<
                ", got offset " << HEX(offset) << std::endl);
            return false;
        }
        lastStackPush = expectedCoverageStep->stackPush;
        expectedCoverageStep = expectedCoverageStep->next;
    } else {
        stillExpectsCoverage = false;
        expectedCoverageExpirated = true;
    }
    LOG(tout << "Cover offset " << offset << " of " << HEX(methodToken));
    CoverageNode *newStep = new CoverageNode{moduleToken, methodToken, offset, threadToken, lastStackPush, nullptr};
    if (lastCoverageStep) {
        lastCoverageStep->next = newStep;
    }
    lastCoverageStep = newStep;
    if (!newCoverageNodes) {
        newCoverageNodes = newStep;
    }
    return true;
}

StackPush vsharp::expectedStackPush() {
    StackPush noPush;
    return expectedCoverageStep ? expectedCoverageStep->stackPush : noPush;
}

unsigned StackPush::size() const {
    if (pushType == 3)
        return sizeof(BYTE) + 2 * sizeof(int) + fieldsLength * 2 * sizeof(int);
    return sizeof(BYTE);
}

void StackPush::serialize(char *&buffer) const {
    WRITE_BYTES(BYTE, buffer, pushType);
    if (pushType == 3) {
        WRITE_BYTES(int, buffer, structSize);
        WRITE_BYTES(int, buffer, fieldsLength);
        for (int i = 0; i < fieldsLength; i++) {
            WRITE_BYTES(int, buffer, symbolicFields[i].first);
            WRITE_BYTES(int, buffer, symbolicFields[i].second);
        }
    }
}

void StackPush::deserialize(char *&buffer) {
    delete[] symbolicFields;

    pushType = READ_BYTES(buffer, BYTE);
    if (pushType >= 0 && pushType < 3) {
        structSize = 0;
        fieldsLength = 0;
        symbolicFields = nullptr;
        return;
    }
    if (pushType == 3) {
        structSize = READ_BYTES(buffer, int);
        fieldsLength = READ_BYTES(buffer, int);
        symbolicFields = new std::pair<int, int>[fieldsLength];
        for (int i = 0; i < fieldsLength; i++) {
            symbolicFields[i].first = READ_BYTES(buffer, int);
            symbolicFields[i].second = READ_BYTES(buffer, int);
        }
        return;
    }
    FAIL_LOUD("unexpected stack push value!");
}

void StackPush::pushToTop(StackFrame &top) const {
    LocalObject structObj;
    switch (pushType) {
        case 0: // no push
            break;
        case 1: // symbolic push
            top.pushPrimitive(false);
            break;
        case 2: // concrete push
            top.pushPrimitive(true);
            break;
        case 3: // struct push
            structObj = LocalObject(structSize, ObjectLocation{});
            for (int i = 0; i < fieldsLength; i++) {
                structObj.writeConcreteness(symbolicFields[i].first, symbolicFields[i].second, false);
            }
            top.push1(structObj);
            break;
        default:
            FAIL_LOUD("unexpected lastPush value!");
    }
}

StackPush::~StackPush() {
    //delete[] symbolicFields;
}

const CoverageNode *vsharp::flushNewCoverageNodes() {
    const CoverageNode *result = newCoverageNodes;
    newCoverageNodes = nullptr;
    return result;
}

unsigned CoverageNode::size() const {
    return staticSizeOfCoverageNode + stackPush.size();
}

int CoverageNode::count() const {
    if (!next)
        return 1;
    return next->count() + 1;
}

void CoverageNode::serialize(char *&buffer) const {
    WRITE_BYTES(int, buffer, moduleToken);
    WRITE_BYTES(mdMethodDef, buffer, methodToken);
    WRITE_BYTES(OFFSET, buffer, offset);
    WRITE_BYTES(int, buffer, threadToken);
    stackPush.serialize(buffer);
}

void CoverageNode::deserialize(char *&buffer) {
    moduleToken = READ_BYTES(buffer, int);
    methodToken = READ_BYTES(buffer, mdMethodDef);
    offset = READ_BYTES(buffer, OFFSET);
    threadToken = READ_BYTES(buffer, int);
    stackPush.deserialize(buffer);
}

CoverageNode::~CoverageNode() {
    //stackPush.~StackPush();
    //delete next;
}

void setCoverageNodeToNext(CoverageNode *&node) {
    auto next = node->next;
    node->next = nullptr;
    delete node;
    node = next;
}
