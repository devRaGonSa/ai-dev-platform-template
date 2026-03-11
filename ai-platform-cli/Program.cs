using System.Diagnostics;
using System.IO.Compression;

var command = args.Length > 0 ? args[0] : "";

switch (command)
{
    case "init":
        InstallPlatform();
        break;

    case "run":
        RunScript("scripts/codex-runner.ps1");
        break;

    case "plan":
        Console.WriteLine("Use the orchestrator prompts to generate tasks.");
        break;

    case "doctor":
        RunDoctor();
        break;

    default:
        ShowHelp();
        break;
}

static void InstallPlatform()
{
    var repoZip = "https://github.com/devRaGonSa/ai-dev-platform-template/archive/refs/heads/main.zip";
    var tempZip = Path.Combine(Path.GetTempPath(), "ai-platform.zip");
    var extractPath = Path.Combine(Path.GetTempPath(), "ai-platform");

    Console.WriteLine("Downloading platform...");

    using var client = new HttpClient();
    var data = client.GetByteArrayAsync(repoZip).Result;
    File.WriteAllBytes(tempZip, data);

    if (Directory.Exists(extractPath))
        Directory.Delete(extractPath, true);

    ZipFile.ExtractToDirectory(tempZip, extractPath);

    var source = Path.Combine(extractPath, "ai-dev-platform-template-main");

    CopyIfMissing(Path.Combine(source, "ai"), "ai");
    CopyIfMissing(Path.Combine(source, "scripts"), "scripts");
    CopyIfMissing(Path.Combine(source, ".github"), ".github");
    CopyIfMissing(Path.Combine(source, "AGENTS.md"), "AGENTS.md");

    Console.WriteLine("AI platform installed.");
}

static void CopyIfMissing(string source, string target)
{
    if (File.Exists(source))
    {
        if (!File.Exists(target))
        {
            File.Copy(source, target);
            Console.WriteLine($"Created {target}");
        }
        return;
    }

    if (Directory.Exists(source))
    {
        if (!Directory.Exists(target))
        {
            DirectoryCopy(source, target);
            Console.WriteLine($"Created {target}");
        }
    }
}

static void DirectoryCopy(string sourceDir, string destDir)
{
    Directory.CreateDirectory(destDir);

    foreach (var file in Directory.GetFiles(sourceDir))
        File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));

    foreach (var dir in Directory.GetDirectories(sourceDir))
        DirectoryCopy(dir, Path.Combine(destDir, Path.GetFileName(dir)));
}

static void RunScript(string script)
{
    var process = new Process();

    process.StartInfo.FileName = "powershell";
    process.StartInfo.Arguments = $"-ExecutionPolicy Bypass -File {script}";
    process.StartInfo.UseShellExecute = false;

    process.Start();
    process.WaitForExit();
}

static void RunDoctor()
{
    var checks = new List<(string Label, bool Passed, string Help)>
    {
        ("ai directory", Directory.Exists("ai"), "Run: ai-platform init"),
        ("scripts directory", Directory.Exists("scripts"), "Run: ai-platform init"),
        ("AGENTS.md", File.Exists("AGENTS.md"), "Create AGENTS.md from your platform template."),
        (".git directory", Directory.Exists(".git"), "Initialize git with: git init"),
        ("codex in PATH", IsCodexAvailable(), "Install Codex CLI and ensure `codex` is available in PATH.")
    };

    Console.WriteLine("AI Platform Doctor");
    Console.WriteLine("");

    foreach (var check in checks)
    {
        var status = check.Passed ? "[OK]" : "[MISSING]";
        Console.WriteLine($"{status} {check.Label}");
        if (!check.Passed)
            Console.WriteLine($"  -> {check.Help}");
    }

    Console.WriteLine("");
    if (checks.All(c => c.Passed))
        Console.WriteLine("Platform ready.");
    else
        Console.WriteLine("Platform is not ready. Fix missing items and run `ai-platform doctor` again.");
}

static bool IsCodexAvailable()
{
    try
    {
        var process = new Process();
        process.StartInfo.FileName = "codex";
        process.StartInfo.Arguments = "--version";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.Start();
        process.WaitForExit(5000);
        return process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}

static void ShowHelp()
{
    Console.WriteLine("AI Platform CLI");
    Console.WriteLine("");
    Console.WriteLine("Commands:");
    Console.WriteLine("  ai-platform init   Install AI development platform");
    Console.WriteLine("  ai-platform run    Start worker");
    Console.WriteLine("  ai-platform plan   Plan feature tasks");
    Console.WriteLine("  ai-platform doctor Validate repository readiness");
}
