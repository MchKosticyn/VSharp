module VSharp.Generator.Generator

open VSharp.Generator.GeneratorInfo

let GetGenerator () =
    ConfigureCommonGenerator [
        PrimitiveGenerator.primitiveGenerator
        EnumGenerator.enumGenerator
        ArrayGenerator.arrayGenerator
        InterfaceGenerator.interfaceGenerator
        ClassGenerator.classGenerator
    ]
    commonGenerator
