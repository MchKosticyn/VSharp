#ifndef INTERVALTREE_H_
#define INTERVALTREE_H_

#include "../logging.h"
#include <cassert>

template<typename Interval>
class TreapNode {
public:
    TreapNode *left;
    TreapNode *right;
    int key;
    Interval *obj;

    ~TreapNode();
    TreapNode(Interval *node);
};

template<typename Interval, typename Shift, typename Point>
class IntervalTree {
private:
    TreapNode<Interval> *root = nullptr;
    TreapNode<Interval> *marked = nullptr;
    TreapNode<Interval> *unhandledByGC = nullptr;

    TreapNode<Interval> *merge(TreapNode<Interval> *left, TreapNode<Interval> *right);
    std::pair<TreapNode<Interval>*, TreapNode<Interval>*> split(TreapNode<Interval> *tree, Point point);
    void moveSubTree(TreapNode<Interval> *tree, const Shift &shift);
    void markSubTree(TreapNode<Interval> *tree);
    void unmarkSubTree(TreapNode<Interval> *tree);
    void getAllSubTreeNodes(TreapNode<Interval> *tree, std::vector<TreapNode<Interval>*> &array) const;
    void addToTree(TreapNode<Interval> *&tree, Interval &node);
    bool unhandledPresenceCheck(const Interval &interval);
    TreapNode<Interval> *cutFromTree(TreapNode<Interval> *&tree, const Interval &interval);
    TreapNode<Interval> *findInTree(TreapNode<Interval> *tree, const Point &p) const;
    void clearUnmarkedOnSubTree(TreapNode<Interval> *tree, std::vector<Interval *> &array);
    
public:
    void add(Interval &node);

    bool find(const Point &p, const Interval *&result) const;

    void moveAndMark(const Interval &interval, const Shift &shift);

    void mark(const Interval &interval);

    // TODO: copy all marked and clear or remove unmarked one by one?
    std::vector<Interval *> clearUnmarked();

    void deleteIntervals(const std::vector<Interval *> &intervals);

    std::vector<Interval *> flush();

    std::string dumpObjects() const;
};

// decoupling it from the .h file results in compilation/linking issues due to templates
#include "intervalTree.cpp"

#endif // INTERVALTREE_H_
