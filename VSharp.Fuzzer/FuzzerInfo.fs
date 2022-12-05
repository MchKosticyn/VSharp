module VSharp.Fuzzer.FuzzerInfo

open System.Reflection

open VSharp.Core
open VSharp

type FuzzerConfig = {
    MaxTest: int
}

let defaultFuzzerConfig = {
    MaxTest = 10
}

type IFuzzer =
    abstract member Fuzz : state -> int -> state seq
    abstract member Configure: FuzzerConfig -> unit

let mutable private fuzzer: IFuzzer option = None
let SetFuzzer (newFuzzer: IFuzzer) = fuzzer <- Some newFuzzer
let ConfigureFuzzer (config: FuzzerConfig) = fuzzer |> Option.iter (fun f -> f.Configure config)

let Fuzz (state: state) =
    let seed = System.DateTime.Now.Millisecond
    match fuzzer with
    | Some f -> f.Fuzz state seed
    | None -> failwith "Fuzzer is not configured"
