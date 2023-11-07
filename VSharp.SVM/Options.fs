namespace VSharp.SVM

open System.Diagnostics
open System.IO

type searchMode =
    | DFSMode
    | BFSMode
    | ShortestDistanceBasedMode
    | RandomShortestDistanceBasedMode
    | ContributedCoverageMode
    | ExecutionTreeMode
    | FairMode of searchMode
    | InterleavedMode of searchMode * int * searchMode * int

type coverageZone =
    | MethodZone
    | ClassZone
    | ModuleZone

type explorationMode =
    | TestCoverageMode of coverageZone * searchMode
    | StackTraceReproductionMode of StackTrace

type fuzzerIsolation =
    | Process

type FuzzerOptions = {
    isolation: fuzzerIsolation
    outputDirectory: DirectoryInfo
}

type SVMOptions = {
    explorationMode : explorationMode
    outputDirectory : DirectoryInfo
    recThreshold : uint
    timeout : int
    solverTimeout : int
    visualize : bool
    releaseBranches : bool
    maxBufferSize : int
    prettyChars : bool // If true, 33 <= char <= 126, otherwise any char
    checkAttributes : bool
    stopOnCoverageAchieved : int
    randomSeed : int
    stepsLimit : uint
}
