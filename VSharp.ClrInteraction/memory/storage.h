#ifndef HEAP_H_
#define HEAP_H_

#include <map>
#include <vector>
#include <unordered_set>
#include "intervalTree.h"
#include "cor.h"
#include "corprof.h"
#include "corhdr.h"

namespace vsharp {

#define ADDR UINT_PTR
#define SIZE UINT_PTR
#define OBJID UINT_PTR

#define UNKNOWN_ADDRESS 1

class Shift {
public:
    ADDR oldBase;
    ADDR newBase;
    ADDR move(ADDR addr) const;
};

class Interval {
private:
    bool marked;
    bool flushed;
    bool handledByGC;
public:
    ADDR left;
    ADDR right;

    Interval();
    Interval(ADDR leftValue, SIZE size);
    virtual ~Interval();

    bool operator==(const Interval &other) const;

    bool contains(ADDR point) const;
    bool includes(const Interval &other) const;
    bool intersects(const Interval &other) const;

    Interval intersect(const Interval& other) const;

    void move(const Shift &shift);

    virtual std::string toString() const;

    // TODO: move 'marked' and 'flushed' fields to Object class; traverse objects in heap class, instead of IntervalTree
    void mark();
    void unmark();
    bool isMarked() const;
    void flush();
    bool isFlushed() const;
    void disableGC();
    bool isHandledByGC() const;
};

typedef char cell;

struct StackKey {
    char frame;
    char idx;
};

union ObjectKey {
    StackKey stackKey;
    INT16 staticFieldKey;
    void *none;
};

enum ObjectType {
    ReferenceType = 1,
    LocalVariable = 2,
    Parameter = 3,
    Statics = 4,
    TemporaryAllocatedStruct = 5
};

// TODO: use 'ObjectLocation' with const refs
struct ObjectLocation {
    ObjectType type;
    ObjectKey key;
};

// TODO: track concreteness of whole object (add field 'bool fullConcreteness')
class Object : public Interval {
protected:
    // NOTE: each bit corresponds of concreteness of memory byte
    cell *concreteness = nullptr;
    bool fullConcreteness = true;
    ObjectLocation m_location;
    bool isArray;
    char *type;
    unsigned long typeLength;
public:
    Object(ADDR address, SIZE size, const ObjectLocation &location, bool isArray, char *type, unsigned long typeLength);
    Object(ADDR address, SIZE size, const ObjectLocation &location);
    ~Object() override;
    std::string toString() const override;
    bool readConcreteness(SIZE offset, SIZE size) const;
    bool isFullyConcrete() const;
    bool isArrayData() const;
    char *getType() const;
    unsigned long getTypeLength() const;
    void writeConcretenessWholeObject(bool vConcreteness);
    void writeConcreteness(SIZE offset, SIZE size, bool vConcreteness);
    char *readBytes(SIZE offset, SIZE size) const;
    void getLocation(ObjectLocation &location) const;
    int sizeOf() const;
};

// NOTE: this type is used for stack cells (evaluation stack, locals and arguments
// NOTE: there are two types of LocalObjects:
// - simplified (size = 1, ObjectLocation is default, address = UNKNOWN_ADDRESS) --- used for primitive locations
// - normal --- used for structs and locations with address (cells after ldloca, ldarga)
class LocalObject : public Object {
private:
    void copyConcreteness(const LocalObject &s);
public:
    LocalObject(int size, const ObjectLocation &location);
    LocalObject(const LocalObject &s);
    LocalObject();
    ~LocalObject() override;
    void changeAddress(ADDR address);
    void setSize(int size);
    void setLocation(const ObjectLocation &location);
    LocalObject& operator=(const LocalObject& other);
};

typedef IntervalTree<Interval, Shift, ADDR> Intervals;

struct VirtualAddress
{
    OBJID obj;
    SIZE offset;
    ObjectLocation location;

    void serialize(char *&buffer) const {
        *(UINT_PTR *)buffer = obj; buffer += sizeof(UINT_PTR);
        *(UINT_PTR *)buffer = offset; buffer += sizeof(UINT_PTR);
        ObjectType type = location.type;
        *(BYTE *)buffer = type; buffer += sizeof(BYTE);
        switch (type) {
            case TemporaryAllocatedStruct:
            case LocalVariable:
            case Parameter: {
                StackKey key = location.key.stackKey;
                *(BYTE *) buffer = key.frame; buffer += sizeof(BYTE);
                *(BYTE *) buffer = key.idx; buffer += sizeof(BYTE);
                break;
            }
            case Statics:
                *(INT16 *) buffer = location.key.staticFieldKey; buffer += sizeof(INT16);
                break;
            default:
                buffer += sizeof(BYTE) * 2;
                break;
        }
    }

    void deserialize(char *&buffer) {
        obj = (OBJID) *(UINT_PTR *)buffer; buffer += sizeof(UINT_PTR);
        offset = (SIZE) *(UINT_PTR *)buffer; buffer += sizeof(UINT_PTR);
        // NOTE: deserialization of object location is not needed, because updateMemory needs only address and offset
    }
};

class Storage {
private:
    Intervals tree;
    // TODO: store new addresses or get them from tree? #do
    std::map<OBJID, std::pair<char*, unsigned long>> newAddresses;
    std::vector<OBJID> deletedAddresses;
    std::map<OBJID, std::pair<INT32, OBJID>> delegates;
    std::unordered_set<OBJID> unmarshalledObjects;

    bool resolve(ADDR address, VirtualAddress &vAddress) const;
    void resolveRefInHeapBytes(char *bytes) const;
    static char *readBytes(const VirtualAddress &address, SIZE sizeOfPtr);

public:
    Storage();

    OBJID allocateObject(ADDR address, SIZE size, char *type, unsigned long typeLength, bool isArray);
    // Allocate block of memory controlled by stack
    OBJID allocateLocal(LocalObject *s);
    // Allocate block of static memory
    OBJID allocateStaticField(ADDR address, INT32 size, INT16 id);
    void allocateDelegate(OBJID delegateId, INT32 functionId, OBJID closureId);

    void moveAndMark(ADDR oldLeft, ADDR newLeft, SIZE length);
    void markSurvivedObjects(ADDR start, SIZE length);
    void clearAfterGC();

    void deleteObjects(const std::vector<Interval *> &objects);
    std::vector<OBJID> flushDeletedByGC();
    std::map<OBJID, std::pair<INT32, OBJID>> flushDelegates();
    std::map<OBJID, std::pair<char*, unsigned long>> flushObjects();

    void physToVirtAddress(ADDR physAddress, VirtualAddress &vAddress) const;
    static ADDR virtToPhysAddress(const VirtualAddress &virtAddress);

    bool readConcreteness(ADDR address, SIZE sizeOfPtr) const;
    bool readConcretenessWholeObject(ADDR address) const;
    void writeConcreteness(ADDR address, SIZE sizeOfPtr, bool vConcreteness) const;
    void writeConcretenessWholeObject(ADDR address, bool vConcreteness) const;
    char *readBytes(const VirtualAddress &address, SIZE sizeOfPtr, int refOffsetsLength, int *refOffsets);
    void readWholeObject(OBJID objID, char *&buffer, SIZE &size, int refOffsetsLength, int *refOffsets) const;
    void readArray(OBJID objID, char *&buffer, SIZE &size, INT32 elemSize, int refOffsetsLength, int *refOffsets) const;
    void unmarshall(OBJID objID, char *&buffer, SIZE &size, int refOffsetsLength, int *refOffsets);
    void unmarshallArray(OBJID objID, char *&buffer, SIZE &size, INT32 elemSize, int refOffsetsLength, int *refOffsets);
    bool checkUnmarshalled(OBJID objID) const;

    void dump() const;
};

}

#endif // HEAP_H_
