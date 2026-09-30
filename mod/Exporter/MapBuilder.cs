using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

sealed class MapBuilder
{
    public sealed class Config
    {
        public string Dotnet = "";
        public string Generator = "";
    }

    volatile string _status = "";
    volatile bool _running;

    public string Status => _status;
    public bool Running => _running;

    public void Start(Config config, string gameDir, string trace, string output, string log)
    {
        string dotnet = FindDotnet(config.Dotnet);
        string project = FindGenerator(config.Generator);
        string generator = Path.Combine(Path.GetDirectoryName(project), "bin", "Release", "net10.0", "IGTAP.Ultras.dll");
        _running = true;
        _status = "building the generator";
        new Thread(() =>
        {
            string result;
            try
            {
                using (var writer = new StreamWriter(log, false))
                {
                    if (Run(writer, dotnet, $"build {Quote(project)} -c Release -nologo -v q -p:IgtapGame={Quote(gameDir)}") != 0)
                        throw new InvalidOperationException("the generator's build failed");
                    _status = "simulating";
                    if (Run(writer, dotnet, $"{Quote(generator)} {Quote(trace)} --out {Quote(output)}") != 0)
                        throw new InvalidOperationException("the generator failed");
                }
                result = "built " + Path.GetFileName(output);
            }
            catch (Exception ex) { result = ex.Message + " (" + Path.GetFileName(log) + ")"; }
            _running = false;
            _status = result;
        })
        { IsBackground = true, Name = "Ultra map builder" }.Start();
    }

    int Run(StreamWriter log, string exe, string arguments)
    {
        log.WriteLine("> " + exe + " " + arguments);
        var info = new ProcessStartInfo(exe, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using (var process = Process.Start(info))
        {
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (log) log.WriteLine(e.Data); };
            process.BeginErrorReadLine();
            for (string line; (line = process.StandardOutput.ReadLine()) != null;)
            {
                lock (log) log.WriteLine(line);
                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith(" launches", StringComparison.Ordinal))
                    _status = "simulating " + line.Substring(line.IndexOf(']') + 2);
            }
            process.WaitForExit();
            log.Flush();
            return process.ExitCode;
        }
    }

    static string Quote(string path) => "\"" + path + "\"";

    static string FindDotnet(string configured)
    {
        if (!string.IsNullOrEmpty(configured)) return configured;
        string exe = Path.DirectorySeparatorChar == '\\' ? "dotnet.exe" : "dotnet";
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).ToList();
        dirs.Add(Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? "");
        dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet"));
        dirs.Add("/usr/share/dotnet");
        dirs.Add("/usr/lib/dotnet");
        foreach (string dir in dirs.Where(d => d.Length > 0))
        {
            string path = Path.Combine(dir.Trim('"'), exe);
            if (File.Exists(path)) return path;
        }
        throw new InvalidOperationException("no dotnet found: install the .NET 10 SDK or set Dotnet in config.json");
    }

    static string FindGenerator(string configured)
    {
        if (!string.IsNullOrEmpty(configured))
            return File.Exists(configured) ? configured : throw new InvalidOperationException("Generator in config.json doesn't exist");
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        foreach (string root in new[] { Environment.GetEnvironmentVariable("RECHARGE_MODS_DIR"), Path.Combine(local, "co.za.codecade.recharge", "mods") })
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
            foreach (string dir in Directory.GetDirectories(root))
            {
                string project = Path.Combine(dir, "Ultras", "IGTAP.Ultras.csproj");
                if (File.Exists(project)) return project;
            }
        }
        throw new InvalidOperationException("mod/Ultras not found: set Generator in config.json");
    }
}
