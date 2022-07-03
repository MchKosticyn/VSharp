#include "memory.h"
#include "stack.h"

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

void vsharp::   throwException(OBJID exception, bool concreteness) {
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

void vsharp::setExpectedCoverage(const CoverageNode *expectedCoverage) {
    expectedCoverageStep = expectedCoverage;
}

bool vsharp::stillExpectsCoverage() {
    return expectedCoverageStep;
}

bool vsharp::addCoverageStep(OFFSET offset) {
    int threadToken = 0; // TODO: support multithreading
    StackFrame &top = topFrame();
    int moduleToken = top.moduleToken();
    mdMethodDef methodToken = top.resolvedToken();
    if (expectedCoverageStep) {
        if (expectedCoverageStep->moduleToken != moduleToken || expectedCoverageStep->methodToken != methodToken ||
                expectedCoverageStep->offset != offset || expectedCoverageStep->threadToken != threadToken) {
            LOG(tout << "Path divergence detected at offset " << offset << " of " << HEX(methodToken));
            return false;
        }
        expectedCoverageStep = expectedCoverageStep->next;
    }
    if (lastCoverageStep && lastCoverageStep->moduleToken == moduleToken && lastCoverageStep->methodToken == methodToken &&
            lastCoverageStep->offset == offset && lastCoverageStep->threadToken == threadToken)
    {
        return true;
    }
    LOG(tout << "cover offset " << offset << " of " << HEX(methodToken));
    CoverageNode *newStep = new CoverageNode{moduleToken, methodToken, offset, threadToken, nullptr};
    if (lastCoverageStep) {
        lastCoverageStep->next = newStep;
    }
    lastCoverageStep = newStep;
    return true;
}
