using System.Linq;
using System.Reflection;

namespace VSharp.CSharpUtils;

using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using System.Runtime.InteropServices;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.ComponentModel;

public static class ExternMocker
{
    public static bool ExtMocksSupported = !OperatingSystem.IsMacOS() ||
                                          RuntimeInformation.OSArchitecture == Architecture.X86 ||
                                          RuntimeInformation.OSArchitecture == Architecture.X64;

    public static IntPtr GetExternPtr(MethodInfo mInfo)
    {
        var libName = "";
        var methodName = "";

        foreach (var attr in mInfo.CustomAttributes)
        {
            if (attr.AttributeType.Name == "DllImportAttribute")
            {
                foreach (var arg in attr.NamedArguments)
                {
                    if (arg.MemberName == "EntryPoint")
                    {
                        libName = attr.ConstructorArguments.First().ToString();
                        methodName = arg.TypedValue.ToString();
                        break;
                    }
                }
            }
        }

        libName = libName.Replace("\"", "");
        methodName = methodName.Replace("\"", "");

        if (!NativeLibrary.TryLoad(libName, Assembly.GetCallingAssembly(), null, out IntPtr libref))
        {
            throw new Exception("Could not open extern library");
        }

        return libref.GetFunction(methodName);
    }

    public static NativeDetour BuildAndApplyDetour(IntPtr from, IntPtr to)
    {
        bool manualApply = PlatformHelper.Is(Platform.MacOS);

        NativeDetour d = new NativeDetour(
            from,
            to,
            new NativeDetourConfig()
            {
                ManualApply = manualApply
            }
        );

        if (manualApply) {
            try {
                d.Apply();
            } catch (Win32Exception) {
                try
                {
                    d.Dispose();
                }
                finally
                {
                    throw new Exception("Could not apply extern mock");
                }
            }
        }

        if (!d.IsApplied)
            throw new Exception("Could not apply extern mock");

        return d;
    }
}
