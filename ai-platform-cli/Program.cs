using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text.Json;

var command = args.Length > 0 ? args[0] : "";
var commandArgs = args.Skip(1).ToArray();

try
{
    switch (command)
    {
        case "init":
            InstallPlatform(commandArgs);
            break;

        case "refresh":
            RefreshPlatform(commandArgs);
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
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}

static void InstallPlatform(string[] commandArgs)
{
    const string defaultRepoZip = "https://github.com/devRaGonSa/ai-dev-platform-template/archive/refs/heads/main.zip";
    var sourceSelection = ResolveTemplateZipSource(commandArgs, defaultRepoZip);
    var installSummary = new InstallSummary();
    var templateSource = DownloadTemplateSource(sourceSelection.Source, "init", includePersistentConfigHint: false);

    ValidateTemplateStructure(
        templateSource.SourceRoot,
        sourceSelection.Source,
        templateSource.ConfigResult.Config.RequiredTemplatePaths);

    CopyIfMissing(Path.Combine(templateSource.SourceRoot, "ai"), "ai", installSummary);
    CopyIfMissing(Path.Combine(templateSource.SourceRoot, "scripts"), "scripts", installSummary);
    CopyIfMissing(Path.Combine(templateSource.SourceRoot, ".github"), ".github", installSummary);
    CopyIfMissing(Path.Combine(templateSource.SourceRoot, "AGENTS.md"), "AGENTS.md", installSummary);
    CopyIfMissing(Path.Combine(templateSource.SourceRoot, "ai-platform.json"), "ai-platform.json", installSummary);

    PrintInstallSummary(
        sourceSelection.Source,
        sourceSelection.SourceKind,
        templateSource.ConfigResult.Config.RequiredTemplatePaths,
        installSummary);
}

static void RefreshPlatform(string[] commandArgs)
{
    const string defaultRepoZip = "https://github.com/devRaGonSa/ai-dev-platform-template/archive/refs/heads/main.zip";
    var options = ParseRefreshOptions(commandArgs);
    var consumerConfigResult = LoadPlatformConfig(Directory.GetCurrentDirectory());
    var sourceSelection = ResolveRefreshZipSource(options, consumerConfigResult.Config, defaultRepoZip);
    var templateSource = DownloadTemplateSource(sourceSelection.Source, "refresh", includePersistentConfigHint: true);

    ValidateTemplateStructure(
        templateSource.SourceRoot,
        sourceSelection.Source,
        templateSource.ConfigResult.Config.RequiredTemplatePaths);

    var managedArtifacts = templateSource.ConfigResult.Config.ManagedArtifacts;
    var summary = new RefreshSummary(options.Apply);

    foreach (var artifact in managedArtifacts)
    {
        var normalizedArtifact = artifact.Replace('/', Path.DirectorySeparatorChar);
        var sourcePath = Path.Combine(templateSource.SourceRoot, normalizedArtifact);

        if (!File.Exists(sourcePath))
        {
            throw new InvalidDataException(
                $"Managed artifact '{artifact}' is missing from the template source '{sourceSelection.Source}'. Refresh cannot continue safely.");
        }

        var targetPath = Path.Combine(Directory.GetCurrentDirectory(), normalizedArtifact);
        var action = DetermineRefreshAction(sourcePath, targetPath);

        switch (action)
        {
            case RefreshAction.Create:
                summary.ToCreate.Add(artifact);
                if (options.Apply)
                    CopyManagedArtifact(sourcePath, targetPath);
                break;

            case RefreshAction.Update:
                summary.ToUpdate.Add(artifact);
                if (options.Apply)
                    CopyManagedArtifact(sourcePath, targetPath);
                break;

            default:
                summary.Unchanged.Add(artifact);
                break;
        }
    }

    PrintRefreshSummary(
        sourceSelection.Source,
        sourceSelection.SourceKind,
        templateSource.ConfigResult.Config.RequiredTemplatePaths,
        managedArtifacts,
        summary);
}

static RefreshOptions ParseRefreshOptions(string[] commandArgs)
{
    var options = new RefreshOptions();

    for (var i = 0; i < commandArgs.Length; i++)
    {
        var arg = commandArgs[i];

        if (string.Equals(arg, "--apply", StringComparison.OrdinalIgnoreCase))
        {
            options.Apply = true;
            continue;
        }

        if (string.Equals(arg, "--source", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(options.ExplicitSource))
                throw new ArgumentException("Refresh accepts only one explicit source.");

            if (i + 1 >= commandArgs.Length)
                throw new ArgumentException("Refresh requires a ZIP URL after --source.");

            var sourceValue = commandArgs[++i];
            if (sourceValue.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException("Refresh requires a ZIP URL after --source.");

            options.ExplicitSource = sourceValue;
            continue;
        }

        if (arg.StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"Unknown refresh option: {arg}");

        if (options.SourceArgs.Count > 0)
            throw new ArgumentException("Refresh accepts at most one source ZIP argument.");

        options.SourceArgs.Add(arg);
    }

    return options;
}

static TemplateSource DownloadTemplateSource(string repoZip, string operationName, bool includePersistentConfigHint)
{
    var tempZip = Path.Combine(Path.GetTempPath(), "ai-platform.zip");
    var extractPath = Path.Combine(Path.GetTempPath(), "ai-platform");

    Console.WriteLine($"Downloading platform from {repoZip}...");

    try
    {
        using var client = new HttpClient();
        var data = client.GetByteArrayAsync(repoZip).Result;
        File.WriteAllBytes(tempZip, data);
    }
    catch (AggregateException ex) when (ex.InnerException is HttpRequestException httpEx)
    {
        throw new InvalidOperationException(BuildHttpSourceErrorMessage(repoZip, operationName, httpEx, includePersistentConfigHint));
    }
    catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
    {
        throw new InvalidOperationException(BuildTimeoutSourceErrorMessage(repoZip, operationName, includePersistentConfigHint));
    }
    catch (HttpRequestException ex)
    {
        throw new InvalidOperationException(BuildHttpSourceErrorMessage(repoZip, operationName, ex, includePersistentConfigHint));
    }
    catch (TaskCanceledException)
    {
        throw new InvalidOperationException(BuildTimeoutSourceErrorMessage(repoZip, operationName, includePersistentConfigHint));
    }
    catch (UriFormatException)
    {
        throw new InvalidOperationException(
            $"The source URL '{repoZip}' is not valid for `ai-platform {operationName}`. Check the URL format and source selection options: {BuildSourceSelectionHint(includePersistentConfigHint)}");
    }

    if (Directory.Exists(extractPath))
        Directory.Delete(extractPath, true);

    try
    {
        ZipFile.ExtractToDirectory(tempZip, extractPath);
    }
    catch (InvalidDataException)
    {
        throw new InvalidOperationException(
            $"The source '{repoZip}' downloaded successfully, but it is not a valid ZIP archive for `ai-platform {operationName}`. Use a ZIP generated from a compatible AI platform template repository.");
    }

    var sourceRoot = ResolveExtractedSourceDirectory(extractPath);
    var configResult = LoadPlatformConfig(sourceRoot);
    return new TemplateSource(sourceRoot, configResult);
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

static (string Source, string SourceKind) ResolveRefreshZipSource(
    RefreshOptions options,
    PlatformConfig consumerConfig,
    string defaultRepoZip)
{
    if (!string.IsNullOrWhiteSpace(options.ExplicitSource))
        return (options.ExplicitSource, "--source");

    if (options.SourceArgs.Count > 0 && !string.IsNullOrWhiteSpace(options.SourceArgs[0]))
        return (options.SourceArgs[0], "command argument");

    var envSource = Environment.GetEnvironmentVariable("AI_PLATFORM_TEMPLATE_ZIP");
    if (!string.IsNullOrWhiteSpace(envSource))
        return (envSource, "AI_PLATFORM_TEMPLATE_ZIP");

    if (!string.IsNullOrWhiteSpace(consumerConfig.TemplateSourceZip))
        return (consumerConfig.TemplateSourceZip, "ai-platform.json (templateSourceZip)");

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
    throw new InvalidOperationException(
        $"The source '{sourceDescription}' is a ZIP archive, but it is not a compatible AI platform template. Missing required items: {missingSummary}. Use a ZIP generated from a compatible AI platform template repository.");
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

static RefreshAction DetermineRefreshAction(string sourcePath, string targetPath)
{
    if (!File.Exists(targetPath))
        return RefreshAction.Create;

    var sourceBytes = File.ReadAllBytes(sourcePath);
    var targetBytes = File.ReadAllBytes(targetPath);
    return sourceBytes.SequenceEqual(targetBytes) ? RefreshAction.Unchanged : RefreshAction.Update;
}

static void CopyManagedArtifact(string sourcePath, string targetPath)
{
    var targetDir = Path.GetDirectoryName(targetPath);
    if (!string.IsNullOrWhiteSpace(targetDir))
        Directory.CreateDirectory(targetDir);

    File.Copy(sourcePath, targetPath, overwrite: true);
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
    var configResult = LoadPlatformConfig(Directory.GetCurrentDirectory());
    var config = configResult.Config;
    var checks = new List<(string Label, bool Passed, string Help)>
    {
        ("ai-platform.json", File.Exists("ai-platform.json"), "Run: ai-platform init or ai-platform refresh --apply to install the platform config file."),
        ("ai directory", Directory.Exists("ai"), "Run: ai-platform init"),
        ("scripts directory", Directory.Exists("scripts"), "Run: ai-platform init"),
        ("AGENTS.md", File.Exists("AGENTS.md"), "Restore AGENTS.md from a compatible platform template source with ai-platform refresh --apply."),
        ("pending task path", Directory.Exists(config.TaskPaths.Pending), $"Create the pending task directory at '{config.TaskPaths.Pending}' or reinstall the platform files."),
        ("in-progress task path", Directory.Exists(config.TaskPaths.InProgress), $"Create the in-progress task directory at '{config.TaskPaths.InProgress}' or reinstall the platform files."),
        ("done task path", Directory.Exists(config.TaskPaths.Done), $"Create the done task directory at '{config.TaskPaths.Done}' or reinstall the platform files."),
        (".git directory", Directory.Exists(".git"), "Initialize git with: git init"),
        ("codex in PATH", IsCodexAvailable(), "Install Codex CLI and ensure `codex` is available in PATH.")
    };

    Console.WriteLine("AI Platform Doctor");
    Console.WriteLine("");
    Console.WriteLine($"Config status: {configResult.Status}");
    if (configResult.FallbackKeys.Count > 0)
        Console.WriteLine($"Config defaults applied: {string.Join(", ", configResult.FallbackKeys)}");
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

static PlatformConfigLoadResult LoadPlatformConfig(string rootPath)
{
    var configPath = Path.Combine(rootPath, "ai-platform.json");
    if (!File.Exists(configPath))
        return new PlatformConfigLoadResult(
            PlatformConfig.CreateDefault(),
            "missing ai-platform.json (using built-in defaults)",
            PlatformConfig.GetAllFallbackKeys());

    try
    {
        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<PlatformConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var normalized = PlatformConfig.Normalize(config, out var fallbackKeys);
        var status = fallbackKeys.Count == 0
            ? "loaded ai-platform.json"
            : "loaded ai-platform.json with fallback defaults";
        return new PlatformConfigLoadResult(normalized, status, fallbackKeys);
    }
    catch
    {
        return new PlatformConfigLoadResult(
            PlatformConfig.CreateDefault(),
            "invalid ai-platform.json (using built-in defaults)",
            PlatformConfig.GetAllFallbackKeys());
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
    Console.WriteLine("  ai-platform init [zip-url]       Install AI development platform");
    Console.WriteLine("  ai-platform refresh [--apply] [--source <zip-url>] [zip-url]");
    Console.WriteLine("  ai-platform run                  Start worker");
    Console.WriteLine("  ai-platform plan                 Plan feature tasks");
    Console.WriteLine("  ai-platform doctor               Validate repository readiness");
    Console.WriteLine("");
    Console.WriteLine("Environment:");
    Console.WriteLine("  AI_PLATFORM_TEMPLATE_ZIP         Override the template ZIP used by init/refresh");
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

static void PrintRefreshSummary(
    string repoZip,
    string sourceKind,
    IReadOnlyList<string> requiredTemplatePaths,
    IReadOnlyList<string> managedArtifacts,
    RefreshSummary summary)
{
    Console.WriteLine("");
    Console.WriteLine(summary.Apply ? "AI platform refresh applied." : "AI platform refresh dry run completed.");
    Console.WriteLine("");
    Console.WriteLine("Refresh summary:");
    Console.WriteLine($"- Source: {repoZip}");
    Console.WriteLine($"- Source selection: {sourceKind}");
    Console.WriteLine($"- Mode: {(summary.Apply ? "apply" : "dry-run")}");
    Console.WriteLine($"- Validation: checked required template paths ({string.Join(", ", requiredTemplatePaths)})");
    Console.WriteLine($"- Managed artifacts: {string.Join(", ", managedArtifacts)}");
    Console.WriteLine($"- {(summary.Apply ? "Created" : "Would create")}: {FormatSummaryItems(summary.ToCreate)}");
    Console.WriteLine($"- {(summary.Apply ? "Updated" : "Would update")}: {FormatSummaryItems(summary.ToUpdate)}");
    Console.WriteLine("- Unchanged: " + FormatSummaryItems(summary.Unchanged));
    Console.WriteLine("- Scope: create/update managed artifacts only; never deletes artifacts.");
    Console.WriteLine("- Backup: not created in refresh v1.");
    Console.WriteLine("- Commits: not created by refresh v1.");

    if (!summary.Apply && (summary.ToCreate.Count > 0 || summary.ToUpdate.Count > 0))
        Console.WriteLine("- Next step: rerun with `ai-platform refresh --apply` to update managed platform artifacts.");
    else if (!summary.Apply)
        Console.WriteLine("- Next step: no refresh needed.");
    else
        Console.WriteLine("- Next step: review the updated managed artifacts and run `ai-platform doctor`.");
}

static string FormatSummaryItems(IReadOnlyList<string> items)
{
    return items.Count == 0 ? "none" : string.Join(", ", items);
}

static string BuildHttpSourceErrorMessage(
    string repoZip,
    string operationName,
    HttpRequestException exception,
    bool includePersistentConfigHint)
{
    if (exception.StatusCode == HttpStatusCode.NotFound)
    {
        return
            $"The source URL '{repoZip}' returned 404 (Not Found) during `ai-platform {operationName}`. Check the URL, or review the configured source selection: {BuildSourceSelectionHint(includePersistentConfigHint)}";
    }

    if (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
    {
        return
            $"The source URL '{repoZip}' requires authentication or access you do not currently have during `ai-platform {operationName}`. Use an accessible ZIP source, or review the configured source selection: {BuildSourceSelectionHint(includePersistentConfigHint)}";
    }

    if (exception.StatusCode.HasValue)
    {
        return
            $"The source URL '{repoZip}' failed during `ai-platform {operationName}` with HTTP {(int)exception.StatusCode.Value} ({exception.StatusCode.Value}). Check the URL and source selection: {BuildSourceSelectionHint(includePersistentConfigHint)}";
    }

    return
        $"Could not download the source '{repoZip}' during `ai-platform {operationName}`. Check your network connection, verify the host is reachable, and review the configured source selection: {BuildSourceSelectionHint(includePersistentConfigHint)}";
}

static string BuildTimeoutSourceErrorMessage(string repoZip, string operationName, bool includePersistentConfigHint)
{
    return
        $"The source '{repoZip}' did not respond in time during `ai-platform {operationName}`. Check your network connection, retry the command, and review the configured source selection: {BuildSourceSelectionHint(includePersistentConfigHint)}";
}

static string BuildSourceSelectionHint(bool includePersistentConfigHint)
{
    if (includePersistentConfigHint)
        return "`--source`, the positional ZIP argument, `AI_PLATFORM_TEMPLATE_ZIP`, or `templateSourceZip` in `ai-platform.json`";

    return "the ZIP argument or `AI_PLATFORM_TEMPLATE_ZIP`";
}

sealed class PlatformConfig
{
    public string? PlatformVersion { get; set; }
    public string? TemplateSourceZip { get; set; }
    public List<string> RequiredTemplatePaths { get; set; } = new();
    public List<string> ManagedArtifacts { get; set; } = new();
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
            ManagedArtifacts = new List<string>
            {
                "ai-platform.json",
                "AGENTS.md",
                "scripts/codex-runner.ps1",
                ".github/workflows/codex-worker.yml"
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

    public static PlatformConfig Normalize(PlatformConfig? config, out List<string> fallbackKeys)
    {
        fallbackKeys = new List<string>();
        var normalized = config ?? CreateDefault();
        var defaults = CreateDefault();

        if (normalized.RequiredTemplatePaths.Count == 0)
        {
            normalized.RequiredTemplatePaths = defaults.RequiredTemplatePaths;
            fallbackKeys.Add("requiredTemplatePaths");
        }

        if (normalized.ManagedArtifacts.Count == 0)
        {
            normalized.ManagedArtifacts = defaults.ManagedArtifacts;
            fallbackKeys.Add("managedArtifacts");
        }

        if (normalized.TaskPaths is null)
        {
            normalized.TaskPaths = defaults.TaskPaths;
            fallbackKeys.Add("taskPaths");
        }
        else
        {
            if (normalized.TaskPaths.Pending is null)
            {
                normalized.TaskPaths.Pending = defaults.TaskPaths.Pending;
                fallbackKeys.Add("taskPaths.pending");
            }

            if (normalized.TaskPaths.InProgress is null)
            {
                normalized.TaskPaths.InProgress = defaults.TaskPaths.InProgress;
                fallbackKeys.Add("taskPaths.inProgress");
            }

            if (normalized.TaskPaths.Done is null)
            {
                normalized.TaskPaths.Done = defaults.TaskPaths.Done;
                fallbackKeys.Add("taskPaths.done");
            }
        }

        if (normalized.Worker is null)
        {
            normalized.Worker = defaults.Worker;
            fallbackKeys.Add("worker");
        }
        else if (normalized.Worker.LockFile is null)
        {
            normalized.Worker.LockFile = defaults.Worker.LockFile;
            fallbackKeys.Add("worker.lockFile");
        }

        normalized.PlatformVersion ??= defaults.PlatformVersion;
        if (config?.PlatformVersion is null)
            fallbackKeys.Add("platformVersion");

        return normalized;
    }

    public static List<string> GetAllFallbackKeys()
    {
        return new List<string>
        {
            "platformVersion",
            "requiredTemplatePaths",
            "managedArtifacts",
            "taskPaths.pending",
            "taskPaths.inProgress",
            "taskPaths.done",
            "worker.lockFile"
        };
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

sealed class RefreshSummary
{
    public RefreshSummary(bool apply)
    {
        Apply = apply;
    }

    public bool Apply { get; }
    public List<string> ToCreate { get; } = new();
    public List<string> ToUpdate { get; } = new();
    public List<string> Unchanged { get; } = new();
}

sealed class PlatformConfigLoadResult
{
    public PlatformConfigLoadResult(PlatformConfig config, string status, List<string> fallbackKeys)
    {
        Config = config;
        Status = status;
        FallbackKeys = fallbackKeys;
    }

    public PlatformConfig Config { get; }
    public string Status { get; }
    public List<string> FallbackKeys { get; }
}

sealed class RefreshOptions
{
    public bool Apply { get; set; }
    public string? ExplicitSource { get; set; }
    public List<string> SourceArgs { get; } = new();
}

sealed class TemplateSource
{
    public TemplateSource(string sourceRoot, PlatformConfigLoadResult configResult)
    {
        SourceRoot = sourceRoot;
        ConfigResult = configResult;
    }

    public string SourceRoot { get; }
    public PlatformConfigLoadResult ConfigResult { get; }
}

enum RefreshAction
{
    Create,
    Update,
    Unchanged
}
