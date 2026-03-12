using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

var command = args.Length > 0 ? args[0] : "";
var commandArgs = args.Skip(1).ToArray();

switch (command)
{
    case "init":
        InstallPlatform(commandArgs);
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

static void InstallPlatform(string[] commandArgs)
{
    const string defaultRepoZip = "https://github.com/devRaGonSa/ai-dev-platform-template/archive/refs/heads/main.zip";
    var (repoZip, sourceKind) = ResolveTemplateZipSource(commandArgs, defaultRepoZip);
    var tempZip = Path.Combine(Path.GetTempPath(), "ai-platform.zip");
    var extractPath = Path.Combine(Path.GetTempPath(), "ai-platform");
    var installSummary = new InstallSummary();

    Console.WriteLine($"Downloading platform from {repoZip}...");

    using var client = new HttpClient();
    var data = client.GetByteArrayAsync(repoZip).Result;
    File.WriteAllBytes(tempZip, data);

    if (Directory.Exists(extractPath))
        Directory.Delete(extractPath, true);

    ZipFile.ExtractToDirectory(tempZip, extractPath);

    var source = ResolveExtractedSourceDirectory(extractPath);
    var sourceConfig = LoadPlatformConfig(source);
    ValidateTemplateStructure(source, repoZip, sourceConfig.RequiredTemplatePaths);

    CopyIfMissing(Path.Combine(source, "ai"), "ai", installSummary);
    CopyIfMissing(Path.Combine(source, "scripts"), "scripts", installSummary);
    CopyIfMissing(Path.Combine(source, ".github"), ".github", installSummary);
    CopyIfMissing(Path.Combine(source, "AGENTS.md"), "AGENTS.md", installSummary);
    CopyIfMissing(Path.Combine(source, "ai-platform.json"), "ai-platform.json", installSummary);

    PrintInstallSummary(repoZip, sourceKind, sourceConfig.RequiredTemplatePaths, installSummary);
}

static (string Source, string SourceKind) ResolveTemplateZipSource(string[] commandArgs, string defaultRepoZip)
{
    if (commandArgs.Length > 0 && !string.IsNullOrWhiteSpace(commandArgs[0]))
        return (commandArgs[0], "command argument");

    var envSource = Environment.GetEnvironmentVariable("AI_PLATFORM_TEMPLATE_ZIP");
    if (!string.IsNullOrWhiteSpace(envSource))
        return (envSource, "AI_PLATFORM_TEMPLATE_ZIP");

    return (defaultRepoZip, "built-in default");
}

static string ResolveExtractedSourceDirectory(string extractPath)
{
    var directories = Directory.GetDirectories(extractPath);
    if (directories.Length == 1)
        return directories[0];

    if (Directory.Exists(Path.Combine(extractPath, "ai")))
        return extractPath;

    throw new DirectoryNotFoundException(
        $"Could not determine the template root inside '{extractPath}'. Expected a single extracted top-level directory or platform files at the archive root.");
}

static void ValidateTemplateStructure(string source, string sourceDescription, IReadOnlyList<string> requiredPaths)
{
    var missingItems = new List<string>();

    foreach (var requiredPath in requiredPaths)
    {
        var normalizedPath = requiredPath.Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(source, normalizedPath);
        if (!Directory.Exists(absolutePath) && !File.Exists(absolutePath))
            missingItems.Add(requiredPath);
    }

    if (missingItems.Count == 0)
        return;

    var missingSummary = string.Join(", ", missingItems);
    throw new InvalidDataException(
        $"The template source '{sourceDescription}' is not compatible. Missing required items: {missingSummary}. Use a ZIP generated from a compatible AI platform template repository.");
}

static void CopyIfMissing(string source, string target, InstallSummary installSummary)
{
    if (File.Exists(source))
    {
        if (!File.Exists(target))
        {
            File.Copy(source, target);
            installSummary.Created.Add(target);
        }
        else
        {
            installSummary.Skipped.Add(target);
        }
        return;
    }

    if (Directory.Exists(source))
    {
        if (!Directory.Exists(target))
        {
            DirectoryCopy(source, target);
            installSummary.Created.Add(target);
        }
        else
        {
            installSummary.Skipped.Add(target);
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
    var config = LoadPlatformConfig(Directory.GetCurrentDirectory());
    var checks = new List<(string Label, bool Passed, string Help)>
    {
        ("ai-platform.json", File.Exists("ai-platform.json"), "Run: ai-platform init to install the platform config file."),
        ("ai directory", Directory.Exists("ai"), "Run: ai-platform init"),
        ("scripts directory", Directory.Exists("scripts"), "Run: ai-platform init"),
        ("AGENTS.md", File.Exists("AGENTS.md"), "Create AGENTS.md from a compatible platform template source."),
        ("pending task path", Directory.Exists(config.TaskPaths.Pending), $"Create the pending task directory at '{config.TaskPaths.Pending}' or reinstall the platform files."),
        ("in-progress task path", Directory.Exists(config.TaskPaths.InProgress), $"Create the in-progress task directory at '{config.TaskPaths.InProgress}' or reinstall the platform files."),
        ("done task path", Directory.Exists(config.TaskPaths.Done), $"Create the done task directory at '{config.TaskPaths.Done}' or reinstall the platform files."),
        (".git directory", Directory.Exists(".git"), "Initialize git with: git init"),
        ("codex in PATH", IsCodexAvailable(), "Install Codex CLI and ensure `codex` is available in PATH.")
    };

    Console.WriteLine("AI Platform Doctor");
    Console.WriteLine("");
    Console.WriteLine($"Platform config version: {config.PlatformVersion}");
    Console.WriteLine($"Configured worker lock file: {config.Worker.LockFile}");
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

static PlatformConfig LoadPlatformConfig(string rootPath)
{
    var configPath = Path.Combine(rootPath, "ai-platform.json");
    if (!File.Exists(configPath))
        return PlatformConfig.CreateDefault();

    try
    {
        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<PlatformConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return PlatformConfig.Normalize(config);
    }
    catch
    {
        return PlatformConfig.CreateDefault();
    }
}

static bool IsCodexAvailable()
{
    if (RunCommand("codex", "--help"))
        return true;

    if (OperatingSystem.IsWindows())
    {
        if (RunCommand("cmd.exe", "/c codex --help"))
            return true;

        return RunCommand("where.exe", "codex");
    }

    if (RunCommand("/bin/sh", "-lc \"codex --help\""))
        return true;

    return RunCommand("which", "codex");
}

static bool RunCommand(string fileName, string arguments)
{
    try
    {
        var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
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
    Console.WriteLine("  ai-platform init [zip-url]   Install AI development platform");
    Console.WriteLine("  ai-platform run              Start worker");
    Console.WriteLine("  ai-platform plan             Plan feature tasks");
    Console.WriteLine("  ai-platform doctor           Validate repository readiness");
    Console.WriteLine("");
    Console.WriteLine("Environment:");
    Console.WriteLine("  AI_PLATFORM_TEMPLATE_ZIP     Override the template ZIP used by init");
}

static void PrintInstallSummary(
    string repoZip,
    string sourceKind,
    IReadOnlyList<string> requiredTemplatePaths,
    InstallSummary installSummary)
{
    Console.WriteLine("");
    Console.WriteLine("AI platform installed.");
    Console.WriteLine("");
    Console.WriteLine("Install summary:");
    Console.WriteLine($"- Source: {repoZip}");
    Console.WriteLine($"- Source selection: {sourceKind}");
    Console.WriteLine($"- Validation: checked required template paths ({string.Join(", ", requiredTemplatePaths)})");
    Console.WriteLine($"- Copied: {FormatSummaryItems(installSummary.Created)}");
    Console.WriteLine($"- Skipped (already existed): {FormatSummaryItems(installSummary.Skipped)}");
    Console.WriteLine("- Next step: run `ai-platform doctor`, then review `ai-platform.json` and `AGENTS.md` for repository-specific adjustments.");
}

static string FormatSummaryItems(IReadOnlyList<string> items)
{
    return items.Count == 0 ? "none" : string.Join(", ", items);
}

sealed class PlatformConfig
{
    public string? PlatformVersion { get; set; }
    public List<string> RequiredTemplatePaths { get; set; } = new();
    public TaskPathConfig TaskPaths { get; set; } = new();
    public WorkerConfig Worker { get; set; } = new();

    public static PlatformConfig CreateDefault()
    {
        return new PlatformConfig
        {
            PlatformVersion = "1.0",
            RequiredTemplatePaths = new List<string>
            {
                "ai",
                "scripts",
                "AGENTS.md",
                "ai-platform.json"
            },
            TaskPaths = new TaskPathConfig
            {
                Pending = "ai/tasks/pending",
                InProgress = "ai/tasks/in-progress",
                Done = "ai/tasks/done"
            },
            Worker = new WorkerConfig
            {
                LockFile = "ai/worker.lock"
            }
        };
    }

    public static PlatformConfig Normalize(PlatformConfig? config)
    {
        var normalized = config ?? CreateDefault();
        var defaults = CreateDefault();

        if (normalized.RequiredTemplatePaths.Count == 0)
            normalized.RequiredTemplatePaths = defaults.RequiredTemplatePaths;

        normalized.TaskPaths ??= defaults.TaskPaths;
        normalized.TaskPaths.Pending ??= defaults.TaskPaths.Pending;
        normalized.TaskPaths.InProgress ??= defaults.TaskPaths.InProgress;
        normalized.TaskPaths.Done ??= defaults.TaskPaths.Done;

        normalized.Worker ??= defaults.Worker;
        normalized.Worker.LockFile ??= defaults.Worker.LockFile;

        normalized.PlatformVersion ??= defaults.PlatformVersion;

        return normalized;
    }
}

sealed class TaskPathConfig
{
    public string? Pending { get; set; } = "ai/tasks/pending";
    public string? InProgress { get; set; } = "ai/tasks/in-progress";
    public string? Done { get; set; } = "ai/tasks/done";
}

sealed class WorkerConfig
{
    public string? LockFile { get; set; } = "ai/worker.lock";
}

sealed class InstallSummary
{
    public List<string> Created { get; } = new();
    public List<string> Skipped { get; } = new();
}
