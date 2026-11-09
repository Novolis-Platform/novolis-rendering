using System.Diagnostics;

// Prefer the stage profiler under benchmarks/ — keeps one authoritative measurement host.
Console.WriteLine("IlgpuBatchBench redirects to Novolis.Rendering.PathTrace.Profile.");
Console.WriteLine();
Console.WriteLine(
    @"dotnet run -c Release --project d:\novolis\novolis-rendering\benchmarks\Novolis.Rendering.PathTrace.Profile\Novolis.Rendering.PathTrace.Profile.csproj -p:NovolisUseProjectReferences=true");

var profileProj =
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "benchmarks",
        "Novolis.Rendering.PathTrace.Profile", "Novolis.Rendering.PathTrace.Profile.csproj"));
if (!File.Exists(profileProj))
{
    Console.Error.WriteLine($"Profile project not found at {profileProj}");
    return 1;
}

var psi = new ProcessStartInfo("dotnet")
{
    UseShellExecute = false,
    CreateNoWindow = true,
};
psi.ArgumentList.Add("run");
psi.ArgumentList.Add("-c");
psi.ArgumentList.Add("Release");
psi.ArgumentList.Add("--project");
psi.ArgumentList.Add(profileProj);
psi.ArgumentList.Add("-p:NovolisUseProjectReferences=true");
psi.ArgumentList.Add("--");
foreach (var arg in args)
{
    psi.ArgumentList.Add(arg);
}

var process = Process.Start(psi);
if (process is null)
{
    return 1;
}

process.WaitForExit();
return process.ExitCode;
