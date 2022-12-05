module VSharp.Generator.Config

open VSharp.Core

type GeneratorConfig = {
    ArrayMaxSize: int
    StringMaxSize: int
}

let generateConfigForArg (state: state): GeneratorConfig =
    let arrayMaxSize = 10
    let stringMaxSize = 10
    { ArrayMaxSize = arrayMaxSize; StringMaxSize = stringMaxSize }

let defaultGeneratorConfig = {
    ArrayMaxSize = 10
    StringMaxSize = 10
}
