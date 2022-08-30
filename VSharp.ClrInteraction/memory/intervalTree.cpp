//#include "intervalTree.h"

// --------------------------- IntervalTree ---------------------------//
// based on cartesian tree

template<typename Interval, typename Shift, typename Point>
TreapNode<Interval> *IntervalTree<Interval, Shift, Point>::merge(TreapNode<Interval> *left, TreapNode<Interval> *right) {
    if (left == nullptr)
        return right;
    if (right == nullptr)
        return left;
    if (left->key > right->key) {
        left->right = merge(left->right, right);
        return left;
    }
    right->left = merge(left, right->left);
    return right;
}

template<typename Interval, typename Shift, typename Point>
std::pair<TreapNode<Interval>*, TreapNode<Interval>*> IntervalTree<Interval, Shift, Point>::split(TreapNode<Interval> *tree, Point point) {
    if (tree == nullptr)
        return {nullptr, nullptr};
    if (tree->obj->left >= point) {
        auto res = split(tree->left, point);
        tree->left = res.second;
        return {res.first, tree};
    }
    auto res = split(tree->right, point);
    tree->right = res.first;
    return {tree, res.second};
}

// cuts nodes in the interval out of the tree and returns them, removing the cut part from the tree in the process
template<typename Interval, typename Shift, typename Point>
TreapNode<Interval> *IntervalTree<Interval, Shift, Point>::cutFromTree(TreapNode<Interval> *&tree, const Interval &interval) {
    auto treeLeftSplit = split(tree, interval.left);
    // increasing by one to include the right bound of the interval to the split part
    auto treeRightSplit = split(treeLeftSplit.second, interval.right + 1);
    tree = merge(treeLeftSplit.first, treeRightSplit.second);
    return treeRightSplit.first;
}

template<typename Interval, typename Shift, typename Point>
bool IntervalTree<Interval, Shift, Point>::unhandledPresenceCheck(const Interval &interval) {
    return cutFromTree(unhandledByGC, interval) != nullptr;
}

template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::addToTree(TreapNode<Interval> *&tree, Interval &node) {
    auto nodeSplit = split(tree, node.left);
    auto newNode = new TreapNode<Interval>(&node, resultRange(rng));
    auto rootLeftAndNewNode = merge(nodeSplit.first, newNode);
    tree = merge(rootLeftAndNewNode, nodeSplit.second);
}

template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::add(Interval &node) {
    TreapNode<Interval> **addTo = node.isMarked() ? &marked : &root;
    addTo = node.isHandledByGC() ? addTo : &unhandledByGC;
    addToTree(*addTo, node);
}

template<typename Interval, typename Shift, typename Point>
TreapNode<Interval> *IntervalTree<Interval, Shift, Point>::findInTree(TreapNode<Interval> *tree, const Point &p) const {
    auto curNode = tree;
    while (curNode != nullptr && !curNode->obj->contains(p)) {
        if (curNode->obj->left > p)
            curNode = curNode->left;
        else
            curNode = curNode->right;
    }
    return curNode;
}

template<typename Interval, typename Shift, typename Point>
bool IntervalTree<Interval, Shift, Point>::find(const Point &p, const Interval *&result) const {
    // TODO: add functionality to be able to iterate through all trees available?
    // throw exception if p was found in more than one tree?
    auto node = findInTree(root, p);
    if (node == nullptr)
        node = findInTree(marked, p);
    if (node == nullptr)
        node = findInTree(unhandledByGC, p);
    if (node != nullptr)
        result = node->obj;
    return node != nullptr;
}


// TODO: combine functions below into one, actionOnSubTree(tree, action), to reuse the same code?
template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::moveSubTree(TreapNode<Interval> *tree, const Shift &shift) {
    if (tree == nullptr)
        return;
    tree->obj->move(shift);
    moveSubTree(tree->left, shift);
    moveSubTree(tree->right, shift);
}

template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::markSubTree(TreapNode<Interval> *tree) {
    if (tree == nullptr)
        return;
    tree->obj->mark();
    markSubTree(tree->left);
    markSubTree(tree->right);
}

template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::unmarkSubTree(TreapNode<Interval> *tree) {
    if (tree == nullptr)
        return;
    tree->obj->unmark();
    unmarkSubTree(tree->left);
    unmarkSubTree(tree->right);
}

template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::getAllSubTreeNodes(TreapNode<Interval> *tree, std::vector<TreapNode<Interval>*> &array) const {
    if (tree == nullptr)
        return;
    getAllSubTreeNodes(tree->left, array);
    array.push_back(tree);
    getAllSubTreeNodes(tree->right, array);
}

template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::clearUnmarkedOnSubTree(TreapNode<Interval> *tree, std::vector<Interval *> &array) {
    if (tree == nullptr)
        return;
    clearUnmarkedOnSubTree(tree->left, array);
    array.push_back(tree->obj);
    clearUnmarkedOnSubTree(tree->right, array);
    tree->left = nullptr;
    tree->right = nullptr;
    delete tree;
}

template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::moveAndMark(const Interval &interval, const Shift &shift) {
    if (unhandledPresenceCheck(interval))
        FAIL_LOUD("IntervalTree: nodes unhandled by GC were unexpectedly moved");

    auto toMove = cutFromTree(root, interval);
    moveSubTree(toMove, shift);
    markSubTree(toMove);
    auto shifted = shift.newBase - shift.oldBase;
    // assuming the addresses we just shifted to were free; no need to do the second split
    auto markedLeftSplit = split(marked, interval.left + shifted);
    auto res = merge(markedLeftSplit.first, toMove);
    marked = merge(res, markedLeftSplit.second);
}

template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::mark(const Interval &interval) {
    if (unhandledPresenceCheck(interval))
        FAIL_LOUD("IntervalTree: nodes unhandled by GC were unexpectedly marked");

    auto toMark = cutFromTree(root, interval);

    markSubTree(toMark);
    auto markedLeftSplit = split(marked, interval.left);
    auto res = merge(markedLeftSplit.first, toMark);
    marked = merge(res, markedLeftSplit.second);
}

template<typename Interval, typename Shift, typename Point>
std::vector<Interval *> IntervalTree<Interval, Shift, Point>::clearUnmarked() {
    // unhandledByGC is not being changed as we always treat those nodes as marked
    std::vector<Interval *> unmarked;
    clearUnmarkedOnSubTree(root, unmarked);
    root = nullptr;
    unmarkSubTree(marked);
    std::swap(root, marked);
    return unmarked;
}

template<typename Interval, typename Shift, typename Point>
void IntervalTree<Interval, Shift, Point>::deleteIntervals(const std::vector<Interval *> &intervals) {
    for (auto interval : intervals) {
        delete cutFromTree(unhandledByGC, *interval);
    }
}

template<typename Interval, typename Shift, typename Point>
std::vector<Interval*> IntervalTree<Interval, Shift, Point>::flush() {
    std::vector<Interval*> newAddresses;
    std::vector<TreapNode<Interval>*> nodes;

    getAllSubTreeNodes(root, nodes);
    getAllSubTreeNodes(marked, nodes);
    getAllSubTreeNodes(unhandledByGC, nodes);

    for (auto node : nodes)
        if (!node->obj->isFlushed()) {
            newAddresses.push_back(node->obj);
            node->obj->flush();
        }

    return newAddresses;
}

template<typename Interval, typename Shift, typename Point>
std::string IntervalTree<Interval, Shift, Point>::dumpObjects() const {
    std::string dump;
    std::vector<TreapNode<Interval>*> nodes;

    getAllSubTreeNodes(root, nodes);
    getAllSubTreeNodes(marked, nodes);
    getAllSubTreeNodes(unhandledByGC, nodes);

    for (const auto node : nodes)
        dump += node->obj->toString() + "\n";

    return dump;
}

template<typename Interval, typename Shift, typename Point>
IntervalTree<Interval, Shift, Point>::IntervalTree() {
    std::random_device seedDevice;
    rng = std::mt19937(seedDevice());
}

// --------------------------- TreapNode ---------------------------

template<typename Interval>
TreapNode<Interval>::~TreapNode() {
    delete left;
    if (obj->isHandledByGC())
        delete obj;
    delete right;
}

template<typename Interval>
TreapNode<Interval>::TreapNode(Interval *node, int key)
        : left(nullptr), right(nullptr), obj(node), key(key) { }
