module VSharp.Fuzzer.Fuzzer

open System.Reflection
open VSharp

#nowarn "67" // This type test or downcast will always hold

type FuzzingResult = {
    Args: obj array
    Returned: obj option
    Thrown: System.Exception option
}

type Config = {
    MaxTest: int
}

let private fuzzOne (method: MethodBase): FuzzingResult =

    let argsTypes = method.GetParameters() |> Array.map (fun p -> p.ParameterType)
    let args = argsTypes |> Array.map Generator.generateObj
    let mutable obj = null
    if method.IsStatic |> not then
        obj <- Generator.generateObj method.DeclaringType

    let result = {
        Args = args
        Returned = None
        Thrown = None
    }

    try
        let returned = method.Invoke(obj, args)
        { result with Returned = Some returned }
    with
    | :? System.Exception as e -> { result with Thrown = Some e }


let fuzz (config: Config) (method: MethodBase): FuzzingResult list =
    [0..config.MaxTest] |> List.map (fun _ -> fuzzOne method)
