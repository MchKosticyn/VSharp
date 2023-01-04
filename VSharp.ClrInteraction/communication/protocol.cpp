#include "protocol.h"
#include "../logging.h"
#include "../probes.h"

#include <cstring>
#include <iostream>

ArrayGetterType arrayInfoGetter = nullptr;
ObjectGetterType objectInfoGetter = nullptr;
InstrumentType instrument = nullptr;

void SyncInfoGettersPointers(long arrayInfoPtr, long objectInfoPtr, long instrumentPtr) {
    LOG(tout << "received function pointers: " << arrayInfoPtr << " for arrays and " << objectInfoPtr << " for objects" << std::endl);
    arrayInfoGetter = (ArrayGetterType)arrayInfoPtr;
    objectInfoGetter = (ObjectGetterType)objectInfoPtr;
    instrument = (InstrumentType) instrumentPtr;
}

using namespace vsharp;

bool Protocol::readConfirmation() {
    char *buffer = new char[1];
    int bytesRead = m_communicator.read(buffer, 1);
    if (bytesRead != 1 || buffer[0] != Confirmation) {
        LOG_ERROR(tout << "Communication with server: could not get the confirmation message. Instead read "
                       << bytesRead << " bytes with message [";
              for (int i = 0; i < bytesRead; ++i) tout << buffer[i] << " ";
              tout << "].");
        delete[] buffer;
        return false;
    }
    delete[] buffer;
    return true;
}

bool Protocol::writeConfirmation() {
    int bytesWritten = m_communicator.write(new char[1] {Confirmation}, 1);
    if (bytesWritten != 1) {
        LOG_ERROR(tout << "Communication with server: could not send the confirmation message. Instead sent"
                       << bytesWritten << " bytes.");
        return false;
    }
    return true;
}

bool Protocol::readCount(int &count) {
    int countCount = m_communicator.read((char*)(&count), 4);
    if (countCount != 4) {
        LOG_ERROR(tout << "Communication with server: could not get the amount of bytes of the next message. Instead read " << countCount << " bytes");
        return false;
    }

    return true;
}

bool Protocol::writeCount(int count) {
    int bytesWritten = m_communicator.write((char*)(&count), 4);
    if (bytesWritten != 4) {
        LOG_ERROR(tout << "Communication with server: could not sent the amount of bytes of the next message. Instead sent " << bytesWritten << " bytes");
        return false;
    }
    return true;
}

bool Protocol::readBuffer(char *&buffer, int &count) {
    if (!readCount(count)) {
        return false;
    }
    if (count <= 0) {
        LOG_ERROR(tout << "Communication with server: the amount of bytes is unexpectedly non-positive (count = " << count << ") ");
        return false;
    }
    if (!writeConfirmation()) return false;
    buffer = new char[count];
    char *bufferBeginning = buffer;
    int bytesRead = 0;
    while (bytesRead < count) {
        int newBytesCount = m_communicator.read(buffer, count - bytesRead);
        if (newBytesCount == 0) break;
        bytesRead += newBytesCount;
        buffer += newBytesCount;
    }
    buffer = bufferBeginning;
    if (bytesRead != count) {
        LOG_ERROR(tout << "Communication with server: expected " << count << " bytes, but read " << bytesRead << " bytes");
        delete[] buffer;
        buffer = nullptr;
        return false;
    }
    if (!writeConfirmation()) {
        LOG_ERROR(tout << "Communication with server: I've got the message, but could not confirm it.");
        delete[] buffer;
        buffer = nullptr;
        return false;
    }
    return true;
}

bool Protocol::writeBuffer(char *buffer, int count) {
    if (!writeCount(count) || !readConfirmation()) {
        return false;
    }
    int bytesWritten = m_communicator.write(buffer, count);
    if (bytesWritten != count) {
        LOG_ERROR(tout << "Communication with server: could not sent the message. Instead sent " << bytesWritten << " bytes");
        return false;
    }
    if (!readConfirmation()) {
        LOG_ERROR(tout << "Communication with server: message sent, but no confirmation.");
        return false;
    }
    return true;
}

bool Protocol::handshake() {
    const char *expectedMessage = "Hi!";
    char *message;
    int count;
    if (readBuffer(message, count) && !strcmp(message, expectedMessage)) {
        strcpy(message, "Hi!");
        count = strlen(message);
        if (writeBuffer(message, count)) {
            delete[] message;
            LOG(tout << "Communication with server: handshake success!");
            return true;
        }
        delete[] message;
    }
    LOG_ERROR(tout << "Communication with server: handshake failed!");
    return false;
}

bool Protocol::startSession() {
    return connect() && sendProbes();
}

bool Protocol::sendProbes() {
    unsigned bytesCount = ProbesAddresses.size() * sizeof(unsigned long long);
    LOG(tout << "Sending probes..." << std::endl);
    return writeBuffer((char*)ProbesAddresses.data(), bytesCount);
}

void Protocol::acceptEntryPoint(char *&entryPointBytes, int &length) {
    if (!readBuffer(entryPointBytes, length)) {
        FAIL_LOUD("Exec response validation failed!");
    }
    LOG(tout << "Entry point accepted" << std::endl);
    assert(length >= 0);
}

bool Protocol::acceptCommand(CommandType &command)
{
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading command failed!");
        return false;
    }
    command = (CommandType) *message;
//    CLOG(command == ReadMethodBody, tout << "Accepted ReadMethodBody command");
//    CLOG(command == ReadString, tout << "Accepted ReadString command");
    delete[] message;
    return true;
}

bool Protocol::acceptString(char *&string) {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading instrumented method body failed!");
        return false;
    }
    string = new char[messageLength];
    memcpy(string, message, messageLength);
//    LOG(tout << "Successfully accepted string: " << string);
    delete[] message;
    return true;
}

bool Protocol::acceptWString(WCHAR *&wstring) {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading instrumented method body failed!");
        return false;
    }
    wstring = new WCHAR[(messageLength / sizeof(WCHAR)) + 1];
    memcpy((char *)wstring, message, messageLength);
    wstring[messageLength / sizeof(WCHAR)] = L'\0';
    delete[] message;
    return true;
}

bool Protocol::acceptToken(mdToken &token) {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading instrumented method body failed!");
        return false;
    }
    assert(messageLength == sizeof(mdToken));
    token = *(mdToken *) message;
    delete[] message;
    return true;
}

bool Protocol::acceptInt32(INT32 &value) {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading instrumented method body failed!");
        return false;
    }
    assert(messageLength == sizeof(INT32));
    value = *(INT32 *) message;
    delete[] message;
    return true;
}

bool Protocol::acceptTokenAndInt32(mdToken &token, INT32 &value) {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading instrumented method body failed!");
        return false;
    }
    assert(messageLength == sizeof(mdToken) + sizeof(INT32));
    char *start = message;
    token = *(mdToken *) message; message += sizeof(mdToken);
    value = *(INT32 *) message;
    delete[] start;
    return true;
}

bool Protocol::acceptHeapReadingParameters(VirtualAddress &address, INT32 &size, INT32 &refOffsetsLength, INT32 *&refOffsets) {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading instrumented method body failed!");
        return false;
    }
    char *start = message;
    address.obj = (OBJID) *(UINT_PTR *)message; message += sizeof(UINT_PTR);
    address.offset = (SIZE) *(INT32 *)message; message += sizeof(INT32);
    size = *(INT32 *) message; message += sizeof(INT32);
    refOffsetsLength = *(INT32 *)message; message += sizeof(INT32);
    refOffsets = new int[refOffsetsLength];
    memcpy(refOffsets, message, refOffsetsLength * sizeof(INT32));
    assert(messageLength == sizeof(UINT_PTR) + sizeof(INT32) + sizeof(INT32) + sizeof(INT32) + sizeof(INT32) * refOffsetsLength);
    delete[] start;
    return true;
}

CoverageNode *Protocol::acceptCoverageInformation() {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Accepting coverage information failed!");
        return nullptr;
    }
    assert(messageLength >= sizeof(unsigned));
    char *start = message;
    unsigned entriesCount = *((unsigned *)message);
    message += sizeof(unsigned);
    CoverageNode *result = nullptr;
    CoverageNode *current = nullptr;
    for (unsigned i = 0; i < entriesCount; ++i) {
        auto *node = new CoverageNode{};
        node->deserialize(message);
        if (current) {
            current->next = node;
        } else {
            result = node;
        }
        current = node;
    }
    assert(message - start == messageLength);
    delete[] start;
    LOG(tout << "Coverage path prefix accepted, " << entriesCount << " entries" << std::endl);
    return result;
}

bool Protocol::acceptReadArrayParameters(OBJID &objID, INT32 &elemSize, int &refOffsetsLength, int *&refOffsets) {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading instrumented method body failed!");
        return false;
    }
    char *start = message;
    objID = (OBJID) *(UINT_PTR *)message; message += sizeof(UINT_PTR);
    elemSize = *(INT32 *)message; message += sizeof(INT32);
    refOffsetsLength = *(INT32 *)message; message += sizeof(INT32);
    refOffsets = new int[refOffsetsLength];
    memcpy(refOffsets, message, refOffsetsLength * sizeof(INT32));
    assert(messageLength == sizeof(UINT_PTR) + sizeof(INT32) + sizeof(INT32) + sizeof(INT32) * refOffsetsLength);
    delete[] start;
    return true;
}

bool Protocol::acceptReadObjectParameters(OBJID &objID, int &refOffsetsLength, int *&refOffsets) {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading instrumented method body failed!");
        return false;
    }
    char *start = message;
    objID = (OBJID) *(UINT_PTR *)message; message += sizeof(UINT_PTR);
    refOffsetsLength = *(INT32 *)message; message += sizeof(INT32);
    refOffsets = new int[refOffsetsLength];
    memcpy(refOffsets, message, refOffsetsLength * sizeof(INT32));
    assert(messageLength == sizeof(UINT_PTR) + sizeof(INT32) + sizeof(INT32) * refOffsetsLength);
    delete[] start;
    return true;
}

bool Protocol::getArrayInfo(INT_PTR arrayPtr, OBJID &objID, int &elemSize, int &refOffsetsLength, int *&refOffsets, char *type, unsigned long typeLength) {
    disableProbesThread();
    arrayInfoGetter(arrayPtr, objID, elemSize, refOffsetsLength, refOffsets, type, typeLength);
    enableProbesThread();
    return true;
}

bool Protocol::getObjectInfo(INT_PTR objectPtr, OBJID &objID, int &refOffsetsLength, int *&refOffsets, char *type, unsigned long typeLength) {
    disableProbesThread();
    objectInfoGetter(objectPtr, objID, refOffsetsLength, refOffsets, type, typeLength);
    enableProbesThread();
    return true;
}

bool Protocol::sendStringsPoolIndex(const unsigned index) {
    unsigned messageLength = sizeof(unsigned);
    char *buffer = new char[messageLength];
    *(unsigned *)buffer = index;
    bool result = writeBuffer(buffer, (int)messageLength);
    delete[] buffer;
    return result;
}

bool Protocol::sendToken(mdToken token) {
    unsigned messageLength = sizeof(mdToken);
    char *buffer = new char[messageLength];
    *(unsigned *)buffer = token;
    bool result = writeBuffer(buffer, (int)messageLength);
    delete[] buffer;
    return result;
}

bool Protocol::sendBytes(char *bytes, int size) {
    bool result = writeBuffer(bytes, size);
    delete[] bytes;
    return result;
}

bool Protocol::sendTypeInfoFromMethod(const std::vector<mdToken>& types) {
    unsigned messageLength = sizeof(mdToken) * types.size();
    char *buffer = new char[messageLength];
    memcpy(buffer, types.data(), messageLength);
    bool result = writeBuffer(buffer, (int)messageLength);
    delete[] buffer;
    return result;
}

bool Protocol::acceptMethodBody(char *&bytecode, int &codeLength, unsigned &maxStackSize, char *&ehs, unsigned &ehsLength) {
    char *message;
    int messageLength;
    if (!readBuffer(message, messageLength)) {
        LOG_ERROR(tout << "Reading instrumented method body failed!");
        return false;
    }
    char *origMessage = message;
    LOG(tout << "Successfully accepted " << messageLength << " bytes of message, parsing it...");
    codeLength = *(int*)message;
    message += sizeof(int);
    maxStackSize = *(unsigned*)message;
    message += sizeof(unsigned);
    bytecode = new char[codeLength];
    memcpy(bytecode, message, codeLength);
    ehsLength = messageLength - sizeof(int) - sizeof(unsigned) - codeLength;
    ehs = new char[ehsLength];
    memcpy(ehs, message + codeLength, ehsLength);
    delete[] origMessage;
    return true;
}

void Protocol::acceptExecResult(char *&bytes, int &messageLength) {
    if (!readBuffer(bytes, messageLength)) {
        FAIL_LOUD("Exec response validation failed!");
    }
    assert(messageLength >= 5);
}

bool Protocol::connect() {
    LOG(tout << "Connecting to server...");
    return m_communicator.open() && handshake();
}

bool Protocol::shutdown()
{
    return writeCount(-1);
}

void Protocol::sendTerminateByExceptionCommand() {
    // NOTE: sending command for SILI to terminate execution by exception
    terminateByException();
    LOG(tout << "Sending terminate by exception command" << std::endl);
    sendCommand0(topFrame().ip());
    // NOTE: getting SILI commands, until it creates test
    while (true) getAndHandleCommand();
}
