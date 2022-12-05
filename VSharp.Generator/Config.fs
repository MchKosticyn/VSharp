module VSharp.Generator.Config

open System.Reflection
open VSharp.Core

type GeneratorConfig = {
    ArrayMaxSize: int
    StringMaxSize: int
}

let generateConfigForArg (state: state) (arg: ParameterInfo): GeneratorConfig =
    let arrayMaxSize = 10
    let stringMaxSize = 10
    { ArrayMaxSize = arrayMaxSize; StringMaxSize = stringMaxSize }

let generateConfigForType (state: state) (t: System.Type): GeneratorConfig =
    let arrayMaxSize = 10
    let stringMaxSize = 10
    { ArrayMaxSize = arrayMaxSize; StringMaxSize = stringMaxSize }

let defaultGeneratorConfig = {
    ArrayMaxSize = 10
    StringMaxSize = 10
}
