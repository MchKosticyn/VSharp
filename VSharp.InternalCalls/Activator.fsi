namespace VSharp.System

open VSharp.Core

module internal Activator =

    // [<Implements("T System.Activator.CreateInstance()")>]
    val CreateInstance : state -> term list -> term
