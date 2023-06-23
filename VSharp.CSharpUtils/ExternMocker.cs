using System.Linq;
using System.Reflection;

namespace VSharp.CSharpUtils;

using System;
using System.Runtime.InteropServices;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.ComponentModel;

public static class ExternMocker
{
    public static bool ExtMocksSupported =
        !OperatingSystem.IsMacOS()
        || RuntimeInformation.OSArchitecture == Architecture.X86
        || RuntimeInformation.OSArchitecture == Architecture.X64;

    public static IntPtr GetExternPtr(MethodInfo mInfo)
    {
        var libName = "";
        var methodName = "";

        // TODO: mInfo.GetCustomAttribute #anya
        // Look to collectImplementations
        // mInfo.GetCustomAttributes(typeof(DllImportAttribute));
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

        var assembly = Assembly.GetCallingAssembly();
        if (!NativeLibrary.TryLoad(libName, assembly, null, out IntPtr libRef))
        {
            throw new Exception("Could not open extern library");
        }

        return libRef.GetFunction(methodName);
    }

    public static NativeDetour BuildAndApplyDetour(IntPtr from, IntPtr to)
    {
        bool manualApply = PlatformHelper.Is(Platform.MacOS);

        var config = new NativeDetourConfig
        {
            ManualApply = manualApply
        };
        NativeDetour d = new NativeDetour(from, to, config);

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
