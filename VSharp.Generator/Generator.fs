module VSharp.Generator.Generator

open System

let rec generate (rnd: Random) (conf: Config.GeneratorConfig) (t: Type) =
    match t with
    | PrimitiveGenerator.Primitive -> PrimitiveGenerator.generate rnd conf t
    | EnumGenerator.Enum -> EnumGenerator.generate rnd conf t
    | ArrayGenerator.Array -> ArrayGenerator.generate generate rnd conf t
    | InterfaceGenerator.Interface -> InterfaceGenerator.generate generate rnd conf t
    | ClassGenerator.Class -> ClassGenerator.generate generate rnd conf t
    | _ -> VSharp.Prelude.__unreachable__()
