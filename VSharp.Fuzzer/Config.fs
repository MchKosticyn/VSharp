module VSharp.Fuzzer.FuzzerInfo

type FuzzerConfig = {
    MaxTest: int
}

let defaultFuzzerConfig = {
    MaxTest = 10
}
