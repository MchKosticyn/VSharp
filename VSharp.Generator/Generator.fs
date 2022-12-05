module VSharp.Generator.Generator

open VSharp.Generator.GeneratorInfo

ConfigureCommonGenerator [
    PrimitiveGenerator.primitiveGenerator
    EnumGenerator.enumGenerator
    ArrayGenerator.arrayGenerator
    InterfaceGenerator.interfaceGenerator
    ClassGenerator.classGenerator
]
