module VSharp.Generator.GeneratorInfo

open System

open Config
open VSharp.Generator.Config



type Generator = Random -> GeneratorConfig -> Type -> obj option
type CommonGenerator = Random -> GeneratorConfig -> Type -> obj

let mkGenerator (recognizer: Type -> bool) (generator: Generator): Generator =
    fun (r: Random) (conf: GeneratorConfig) (t: Type) ->
        if recognizer t then generator r conf t else None


let private mkCommonGenerator (generators: Generator list): CommonGenerator =
    fun (r: Random) (conf: GeneratorConfig) (t: Type) ->
        let result = List.fold (fun acc g -> if acc = None then g r conf t else acc) None generators
        match result with
        | Some x -> x
        | _ -> failwithf $"Unsupported type %A{t}"

let mutable internal commonGenerator: CommonGenerator = Unchecked.defaultof<CommonGenerator>

let ConfigureCommonGenerator (generators: Generator list) =
    commonGenerator <- mkCommonGenerator generators

let GetGenerator () = commonGenerator
