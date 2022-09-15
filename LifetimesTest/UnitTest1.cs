using System.Diagnostics;
using System.Text;

namespace LifetimesTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    private void RunDotnet(string args, DirectoryInfo outputDir)
    {
        var output = new StringBuilder();
        var info = new ProcessStartInfo();
        info.WorkingDirectory = outputDir.FullName;
        info.FileName = "dotnet";
        info.Arguments = args;
        info.UseShellExecute = false;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        var proc = Process.Start(info);
        proc.OutputDataReceived += new DataReceivedEventHandler(delegate(object sender, DataReceivedEventArgs eventArgs)
        {
            output.AppendLine(eventArgs.Data);
        });
        proc.ErrorDataReceived += new DataReceivedEventHandler(delegate(object sender, DataReceivedEventArgs eventArgs)
        {
            output.AppendLine(eventArgs.Data);
        });
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
        TestContext.Progress.WriteLine(output);
    }


    [Test]
    public void Test1()
    {
        VSharp.Logger.ConfigureWriter(TestContext.Progress);
        var t = typeof(JetBrains.Util.BitHacks); // Synchronized.SynchronizedList<object>); // SynchronizedList<>
        var stats = VSharp.TestGenerator.Cover(t, 5);
        var dotcoverReport = stats.OutputDir + "/" + "dotCover.Output.html";
        RunDotnet($"dotcover /Users/michael/Documents/Work/VSharp/VSharp.TestRunner/bin/Release/netcoreapp6.0/VSharp.TestRunner.dll . --dcReportType=HTML --dcDisableDefaultFilters", stats.OutputDir);
        // Process.Start($"open //Applications/safari.app", dotcoverReport);
    }
}
