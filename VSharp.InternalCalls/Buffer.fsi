namespace VSharp.System

open global.System
open VSharp
open VSharp.Core

module internal Buffer =
    // this function might call unmarshall, which is now done along with the sendCommand
    // TODO: implement a probe unmarshalling the array info to SILI
    [<Implements("System.Void System.Buffer.Memmove(T&, T&, System.UIntPtr)")>]
    val internal Memmove : state -> term list -> term
