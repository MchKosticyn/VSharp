module VSharp.Generator.InterfaceGenerator

open System

open VSharp.Generator.Config
open VSharp

let private instancesCache = System.Collections.Generic.Dictionary<Type, Type>()
let (|Interface|_|) (t: Type) = if t.IsInterface then Some Interface else None

let generate commonGenerator (rnd: Random) (conf: GeneratorConfig) (t: Type)  =
    let mutable c = Unchecked.defaultof<_>
    let exist = instancesCache.TryGetValue(t, &c)

    if exist then commonGenerator rnd conf c
    else
        let instances =
            t.Assembly.GetTypes()
            |> Array.filter (fun x -> x.IsClass && t.IsAssignableFrom(x))


        if instances.Length = 0 then internalfail "Mocking not supported" // Mock from type solver?
        let instance = instances[rnd.NextInt64(0, instances.Length) |> int]
        instancesCache.Add(t, instance)
        commonGenerator rnd conf instance


