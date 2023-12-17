namespace VSharp.CSharpUtils;

public class DiagnosticsUtils
{
    [Implements("System.Boolean System.Diagnostics.Debugger.get_IsAttached()")]
    public static bool DebuggerIsAttached()
    {
        return false;
    }
}
