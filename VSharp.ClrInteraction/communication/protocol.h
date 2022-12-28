#ifndef PROTOCOL_H_
#define PROTOCOL_H_

#ifdef IMAGEHANDLER_EXPORTS
#define IMAGEHANDLER_API __declspec(dllexport)
#else
#define IMAGEHANDLER_API __declspec(dllimport)
#endif

#include "communicator.h"
#include "../memory/memory.h"
#include "cor.h"
#include <vector>

#ifdef UNIX
#include "pal_mstypes.h"
#include "corhdr.h"
#endif

#ifdef WIN32
#include "../profiler_win.h"
#endif

typedef void (*ArrayGetterType)(INT_PTR, OBJID&, int&, int&, int*&, char*, unsigned long);
typedef void (*ObjectGetterType)(INT_PTR, OBJID&, int&, int*&, char*, unsigned long);
typedef void (*InstrumentType)(unsigned, unsigned, unsigned, unsigned, unsigned, unsigned, unsigned, char*,
        const WCHAR*, const WCHAR*, char*, char*,
        // result
        char**, int*, int*, char**, int*);

extern "C" IMAGEHANDLER_API void SyncInfoGettersPointers(long arrayInfoPtr, long objectInfoPtr);
extern "C" IMAGEHANDLER_API char *GetProbes(unsigned *bytesCount);

#define sizeOfDelegate (2 * sizeof(UINT_PTR) + sizeof(INT32))

namespace vsharp {

enum CommandType {
    Confirmation = 0x55,
    InstrumentCommand = 0x56,
    ExecuteCommand = 0x57,
    ReadMethodBody = 0x58,
    ReadString = 0x59,
    ParseTypeInfoFromMethod = 0x60,
    GetTypeTokenFromTypeRef = 0x61,
    GetTypeTokenFromTypeSpec = 0x62,
    ReadHeapBytes = 0x63,
    ReadExecResponse = 0x64,
    Unmarshall = 0x65,
    UnmarshallArray = 0x66,
    ReadWholeObject = 0x67,
    ReadArray = 0x68,
    ParseFieldRefTypeToken = 0x69,
    ParseFieldDefTypeToken = 0x70,
    ParseArgTypeToken = 0x71,
    ParseLocalTypeToken = 0x72,
    ParseReturnTypeToken = 0x73,
    ParseDeclaringTypeToken = 0x74
};

class Protocol {
private:
    Communicator m_communicator;

    bool readConfirmation();
    bool writeConfirmation();

    bool readCount(int &count);
    bool writeCount(int count);

    bool readBuffer(char *&buffer, int &count);
    bool writeBuffer(char *buffer, int count);

    bool handshake();

public:
    bool connect();
    bool sendProbes();
    bool startSession();
    void acceptEntryPoint(char *&entryPointBytes, int &length);
    bool acceptCommand(CommandType &command);
    bool acceptString(char *&string);
    bool acceptWString(WCHAR *&string);
    bool acceptToken(mdToken &token);
    bool acceptInt32(INT32 &value);
    bool acceptTokenAndInt32(mdToken &token, INT32 &value);
    bool acceptReadObjectParameters(OBJID &objID, int &refOffsetsLength, int *&refOffsets);
    bool acceptReadArrayParameters(OBJID &objID, INT32 &elemSize, int &refOffsetsLength, int *&refOffsets);
    bool acceptHeapReadingParameters(VirtualAddress &address, INT32 &size, int &refOffsetsLength, int *&refOffsets);
    CoverageNode *acceptCoverageInformation();
    bool getArrayInfo(INT_PTR arrayPtr, OBJID &objID, int &elemSize, int &refOffsetsLength, int *&refOffsets, char *type, unsigned long typeLength);
    bool getObjectInfo(INT_PTR objectPtr, OBJID &objID, int &refOffsetsLength, int *&refOffsets, char *type, unsigned long typeLength);
    void instrumentR(unsigned token, unsigned codeSize, unsigned assemblyNameLength, unsigned moduleNameLength, unsigned maxStackSize, unsigned ehsSize, unsigned signatureTokensLength, char *signatureTokensPtr,
                    const WCHAR *assemblyNamePtr, const WCHAR *moduleNamePtr, char *byteCodePtr, char *ehsPtr,
                    // result
                    char **instrumentedBody, int *length, int *resultMaxStackSize, char **resultEhs, int *ehsLength);
    bool sendToken(mdToken token);
    bool sendBytes(char *bytes, int size);
    bool sendStringsPoolIndex(unsigned index);
    bool sendTypeInfoFromMethod(const std::vector<mdToken>& types);
    bool acceptMethodBody(char *&bytecode, int &codeLength, unsigned &maxStackSize, char *&ehs, unsigned &ehsLength);
    template<typename T>
    bool sendSerializable(char commandByte, const T &object) {
        if (!writeBuffer(new char[1] {commandByte}, 1)) return false;
        char *bytes;
        unsigned count;
        object.serialize(bytes, count);
        bool result = writeBuffer(bytes, count);
        delete[] bytes;
        return result;
    }

    void acceptExecResult(char *&bytes, int &messageLength);
    bool shutdown();
    static void sendTerminateByExceptionCommand();
};

}

#endif // PROTOCOL_H_
