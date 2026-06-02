using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var command = args.Length > 0 ? args[0] : "";
var commandArgs = args.Skip(1).ToArray();

switch (command)
{
    case "init":
        InstallPlatform(commandArgs);
        break;

    case "status":
        RunStatus();
        break;

    case "refresh":
        RunRefresh(commandArgs);
        break;

    case "git-ignore":
        RunGitIgnore(commandArgs);
        break;

    case "run":
        RunScript("scripts/codex-runner.ps1");
        break;

    case "codex-exec":
        RunScript("scripts/codex-exec-runner.ps1");
        break;

    case "watch":
        RunScript("scripts/task-watcher.ps1");
        break;

    case "update":
        RunScript("scripts/update-platform.ps1");
        break;

    case "plan":
        RunPlan(commandArgs);
        break;

    case "doctor":
        RunDoctor();
        break;

    case "analyze":
        RunAnalyze();
        break;

    case "roadmap-status":
        RunRoadmapStatus();
        break;

    case "reconcile":
        RunReconcile();
        break;

    case "review":
        RunReview(commandArgs);
        break;

    case "implement":
        RunImplement(commandArgs);
        break;

    case "task":
        RunTaskCommand(commandArgs);
        break;

    default:
        ShowHelp();
        break;
}

static void InstallPlatform(string[] commandArgs)
{
    var defaultRepoZip = GetBuiltInTemplateZipSource();
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
    var sourceConfigResult = LoadPlatformConfig(source);
    ValidateTemplateStructure(source, repoZip, sourceConfigResult.Config.RequiredTemplatePaths);

    CopyIfMissing(Path.Combine(source, "ai"), "ai", installSummary);
    CopyIfMissing(Path.Combine(source, "scripts"), "scripts", installSummary);
    CopyIfMissing(Path.Combine(source, ".github"), ".github", installSummary);
    CopyIfMissing(Path.Combine(source, "AGENTS.md"), "AGENTS.md", installSummary);
    CopyIfMissing(Path.Combine(source, "ai-platform.json"), "ai-platform.json", installSummary);

    PrintInstallSummary(repoZip, sourceKind, sourceConfigResult.Config.RequiredTemplatePaths, installSummary);
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

static string GetBuiltInTemplateZipSource()
{
    return "https://github.com/devRaGonSa/ai-dev-platform-template/archive/refs/heads/main.zip";
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
    var configResult = LoadPlatformConfig(Directory.GetCurrentDirectory());
    var config = configResult.Config;
    var shouldRecommendGitIgnore = IsConsumerLocalInstallMode(config.InstallMode)
        && !HasManagedGitIgnoreBlock(Directory.GetCurrentDirectory());
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

    if (shouldRecommendGitIgnore)
    {
        Console.WriteLine("");
        Console.WriteLine("Recommendation:");
        Console.WriteLine("- consumer-local install mode is configured, but .gitignore does not contain the managed AI tooling block.");
        Console.WriteLine("  Run `ai-platform git-ignore` after reviewing the intended local-only scope.");
    }
}

static void RunStatus()
{
    var rootPath = Directory.GetCurrentDirectory();
    var configResult = LoadPlatformConfig(rootPath);
    var config = configResult.Config;
    var refreshSource = ResolveStatusRefreshSource(config);

    Console.WriteLine("AI Platform Status");
    Console.WriteLine("");
    Console.WriteLine($"- Config: {configResult.Status}");
    Console.WriteLine($"- Platform version: {FormatOptionalValue(config.PlatformVersion)}");
    Console.WriteLine($"- Install mode: {FormatOptionalValue(config.InstallMode)}");
    Console.WriteLine($"- Worker lock file: {FormatOptionalValue(config.Worker.LockFile)}");
    Console.WriteLine($"- Refresh source: {refreshSource.Source}");
    Console.WriteLine($"- Refresh source selection: {refreshSource.Selection}");
    Console.WriteLine($"- Managed artifacts: {FormatManagedArtifacts(config.ManagedArtifacts)}");
    Console.WriteLine($"- Task paths: pending={config.TaskPaths.Pending}, in-progress={config.TaskPaths.InProgress}, review={config.TaskPaths.Review}, done={config.TaskPaths.Done}, blocked={config.TaskPaths.Blocked}, obsolete={config.TaskPaths.Obsolete}");
    Console.WriteLine("- Local essentials:");
    PrintStatusCheck("ai/", Directory.Exists("ai"));
    PrintStatusCheck("scripts/", Directory.Exists("scripts"));
    PrintStatusCheck("AGENTS.md", File.Exists("AGENTS.md"));
    PrintStatusCheck(".git", Directory.Exists(".git"));
    Console.WriteLine("");
    Console.WriteLine("Next step: run `ai-platform doctor` for full validation, or `ai-platform analyze` for a project report.");
}

static void RunGitIgnore(string[] commandArgs)
{
    var options = ParseGitIgnoreOptions(commandArgs);
    if (options.ShowHelp)
        return;

    var rootPath = Directory.GetCurrentDirectory();
    var config = LoadPlatformConfig(rootPath).Config;
    var gitIgnorePath = Path.Combine(rootPath, ".gitignore");
    var existingContent = File.Exists(gitIgnorePath) ? File.ReadAllText(gitIgnorePath) : "";
    var managedBlock = BuildManagedGitIgnoreBlock();
    var update = BuildGitIgnoreUpdate(existingContent, managedBlock);

    Console.WriteLine(options.DryRun ? "AI Platform Git Ignore dry run" : "AI Platform Git Ignore");
    Console.WriteLine("");
    Console.WriteLine($"Install mode: {FormatOptionalValue(config.InstallMode)}");
    Console.WriteLine(".gitignore: .gitignore");
    Console.WriteLine($"Action: {update.Action}");
    Console.WriteLine("");

    if (options.DryRun)
    {
        Console.WriteLine(update.Action switch
        {
            "unchanged" => "Managed block is already up to date.",
            "update" => "Would update the managed AI DEV PLATFORM LOCAL TOOLING block.",
            _ => "Would add the managed AI DEV PLATFORM LOCAL TOOLING block."
        });
        Console.WriteLine("");
        Console.WriteLine("No files were changed.");
    }
    else if (update.Action == "unchanged")
    {
        Console.WriteLine("Managed AI DEV PLATFORM LOCAL TOOLING block is already up to date.");
    }
    else
    {
        File.WriteAllText(gitIgnorePath, update.UpdatedContent);
        Console.WriteLine(update.Action == "update"
            ? "Updated the managed AI DEV PLATFORM LOCAL TOOLING block."
            : "Added the managed AI DEV PLATFORM LOCAL TOOLING block.");
    }

    Console.WriteLine("");
    Console.WriteLine("Already tracked files:");
    Console.WriteLine("If these platform files are already tracked by Git, .gitignore alone is not enough.");
    Console.WriteLine("To stop tracking them without deleting local files, review and run:");
    Console.WriteLine("git rm -r --cached AGENTS.md ai-platform.json ai scripts/codex-runner.ps1 scripts/run-integration-tests.ps1 .github/workflows/codex-worker.yml ai-platform-cli");
    Console.WriteLine("");
    Console.WriteLine(options.DryRun
        ? "Next step: rerun without `--dry-run` in a consumer repository when you want to apply the ignore block."
        : "Next step: review `.gitignore`, then decide whether tracked AI tooling should be removed from the Git index.");
}

static void RunRefresh(string[] commandArgs)
{
    var options = ParseRefreshOptions(commandArgs);
    if (options.ShowHelp)
        return;

    var rootPath = Directory.GetCurrentDirectory();
    var configResult = LoadPlatformConfig(rootPath);
    var config = configResult.Config;

    if (configResult.FallbackKeys.Contains("managedArtifacts", StringComparer.OrdinalIgnoreCase)
        || config.ManagedArtifacts.Count == 0)
    {
        Console.WriteLine("AI Platform Refresh");
        Console.WriteLine("");
        Console.WriteLine("Managed artifacts are not configured.");
        Console.WriteLine("Add `managedArtifacts` to ai-platform.json before running refresh.");
        return;
    }

    var refreshSource = ResolveRefreshSource(options, configResult);
    var modeLabel = options.Apply ? "apply" : "dry-run";
    var tempRoot = Path.Combine(Path.GetTempPath(), $"ai-platform-refresh-{Guid.NewGuid():N}");
    var tempZipPath = Path.Combine(tempRoot, "template.zip");
    var extractPath = Path.Combine(tempRoot, "extracted");

    try
    {
        Directory.CreateDirectory(tempRoot);
        DownloadTemplateZip(refreshSource.Source, tempZipPath);
        try
        {
            ZipFile.ExtractToDirectory(tempZipPath, extractPath);
        }
        catch (InvalidDataException ex)
        {
            throw new RefreshCommandException($"The downloaded ZIP is invalid or corrupt: {ex.Message}");
        }

        var sourceRoot = ResolveExtractedSourceDirectory(extractPath);
        ValidateTemplateStructure(sourceRoot, refreshSource.Source, config.RequiredTemplatePaths);

        var comparison = CompareManagedArtifacts(rootPath, sourceRoot, config.ManagedArtifacts);

        if (options.Apply)
            ApplyRefreshChanges(rootPath, sourceRoot, comparison);

        PrintRefreshSummary(config, refreshSource, comparison, options.Apply);
    }
    catch (RefreshCommandException ex)
    {
        Console.WriteLine(options.Apply ? "AI Platform Refresh" : "AI Platform Refresh dry run");
        Console.WriteLine("");
        Console.WriteLine($"Source: {refreshSource.Source}");
        Console.WriteLine($"Source selection: {refreshSource.Selection}");
        Console.WriteLine($"Mode: {modeLabel}");
        Console.WriteLine("");
        Console.WriteLine(ex.Message);
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine(options.Apply ? "AI Platform Refresh" : "AI Platform Refresh dry run");
        Console.WriteLine("");
        Console.WriteLine($"Source: {refreshSource.Source}");
        Console.WriteLine($"Source selection: {refreshSource.Selection}");
        Console.WriteLine($"Mode: {modeLabel}");
        Console.WriteLine("");
        Console.WriteLine($"Could not resolve the extracted template root: {ex.Message}");
    }
    catch (InvalidDataException ex)
    {
        Console.WriteLine(options.Apply ? "AI Platform Refresh" : "AI Platform Refresh dry run");
        Console.WriteLine("");
        Console.WriteLine($"Source: {refreshSource.Source}");
        Console.WriteLine($"Source selection: {refreshSource.Selection}");
        Console.WriteLine($"Mode: {modeLabel}");
        Console.WriteLine("");
        Console.WriteLine("Source not compatible.");
        Console.WriteLine(ex.Message);
        Console.WriteLine("Review --source, AI_PLATFORM_TEMPLATE_ZIP, or templateSourceZip.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
            Directory.Delete(tempRoot, true);
    }
}

static void RunAnalyze()
{
    var rootPath = Directory.GetCurrentDirectory();
    var configResult = LoadPlatformConfig(rootPath);
    var config = configResult.Config;
    var reportPath = Path.Combine(rootPath, "ai", "reports", "project-analysis.md");
    var reportDirectory = Path.GetDirectoryName(reportPath)!;

    Directory.CreateDirectory(reportDirectory);

    var roadmapPath = "ai/roadmap.md";
    var currentStatePath = "ai/current-state.md";
    var teamsPath = "ai/teams";
    var commandsPath = "ai/commands";
    var risksPath = "ai/project-memory/risks.md";
    var knownGapsPath = "ai/project-memory/known-gaps.md";

    var taskSummaries = new[]
    {
        SummarizeTaskDirectory("pending", config.TaskPaths.Pending!),
        SummarizeTaskDirectory("in-progress", config.TaskPaths.InProgress!),
        SummarizeTaskDirectory("done", config.TaskPaths.Done!)
    };

    var roadmapText = ReadTextIfExists(roadmapPath);
    var roadmapIds = roadmapText is null
        ? 0
        : Regex.Matches(roadmapText, @"\bR-\d{3}\b").Count;
    var roadmapStates = CountRoadmapStates(roadmapText);

    var teamDocs = Directory.Exists(teamsPath)
        ? Directory.GetFiles(teamsPath, "README.md", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(teamsPath, Path.GetDirectoryName(path)!))
            .Where(name => name != ".")
            .OrderBy(name => name)
            .ToList()
        : new List<string>();

    var commandSpecs = Directory.Exists(commandsPath)
        ? Directory.GetFiles(commandsPath, "*.md", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => name!)
            .Where(name => !string.Equals(name, "README", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name)
            .ToList()
        : new List<string>();

    var optionalConfig = ReadOptionalConfigValues(rootPath);
    var report = BuildAnalysisReport(
        configResult,
        config,
        optionalConfig,
        taskSummaries,
        roadmapIds,
        roadmapStates,
        teamDocs,
        commandSpecs,
        risksPath,
        knownGapsPath);

    File.WriteAllText(reportPath, report);

    Console.WriteLine("AI Platform Analysis");
    Console.WriteLine("");
    Console.WriteLine("Report written to: ai/reports/project-analysis.md");
    Console.WriteLine("");
    Console.WriteLine("Summary:");
    Console.WriteLine($"- Roadmap: {FormatFound(File.Exists(roadmapPath))}");
    Console.WriteLine($"- Current state: {FormatFound(File.Exists(currentStatePath))}");
    Console.WriteLine($"- Teams: {teamDocs.Count} team docs found");
    Console.WriteLine($"- Command specs: {commandSpecs.Count} specs found");
    Console.WriteLine($"- Tasks: pending={taskSummaries[0].MarkdownTaskCount}, in-progress={taskSummaries[1].MarkdownTaskCount}, done={taskSummaries[2].MarkdownTaskCount}");
    Console.WriteLine("");
    Console.WriteLine("Next step: review ai/reports/project-analysis.md");
}

static void RunRoadmapStatus()
{
    var rootPath = Directory.GetCurrentDirectory();
    var roadmapPath = "ai/roadmap.md";
    var reportPath = Path.Combine(rootPath, "ai", "reports", "roadmap-status.md");
    var reportDirectory = Path.GetDirectoryName(reportPath)!;

    Directory.CreateDirectory(reportDirectory);

    var roadmapExists = File.Exists(roadmapPath);
    var roadmapText = ReadTextIfExists(roadmapPath);
    var items = ParseRoadmapItems(roadmapText);
    var counts = CountRoadmapItemsByStatus(items);
    var report = BuildRoadmapStatusReport(roadmapExists, items, counts);

    File.WriteAllText(reportPath, report);

    Console.WriteLine("AI Platform Roadmap Status");
    Console.WriteLine("");
    Console.WriteLine("Report written to: ai/reports/roadmap-status.md");
    Console.WriteLine("");
    Console.WriteLine("Summary:");
    Console.WriteLine($"- Total items: {items.Count}");
    Console.WriteLine($"- Done: {counts["done"]}");
    Console.WriteLine($"- In progress: {counts["in-progress"]}");
    Console.WriteLine($"- Planned: {counts["planned"]}");
    Console.WriteLine($"- Blocked: {counts["blocked"]}");
    Console.WriteLine($"- Deferred: {counts["deferred"]}");
    Console.WriteLine($"- Unknown: {counts["unknown"]}");
    Console.WriteLine("");
    Console.WriteLine("Next step: review ai/reports/roadmap-status.md");
}

static void RunPlan(string[] commandArgs)
{
    var options = ParsePlanOptions(commandArgs);
    if (string.IsNullOrWhiteSpace(options.Title))
    {
        ShowPlanHelp();
        return;
    }

    var rootPath = Directory.GetCurrentDirectory();
    var config = LoadPlatformConfig(rootPath).Config;
    var taskId = GetNextTaskId(config);
    var team = string.IsNullOrWhiteSpace(options.Team)
        ? InferTeam(options.RoadmapItem)
        : options.Team.Trim().ToLowerInvariant();
    var priority = string.IsNullOrWhiteSpace(options.Priority) ? "medium" : options.Priority.Trim();
    var type = string.IsNullOrWhiteSpace(options.Type) ? "platform" : options.Type.Trim();
    var pendingPath = config.TaskPaths.Pending!;
    var outputPath = Path.Combine(pendingPath, $"{taskId}.md");
    var displayPath = NormalizeDisplayPath(outputPath);
    var content = BuildTaskContent(taskId, options.Title!, options.RoadmapItem, team, priority, type);

    if (options.DryRun)
    {
        Console.WriteLine("AI Platform Plan dry run");
        Console.WriteLine("");
        Console.WriteLine($"Would create task: {displayPath}");
        Console.WriteLine($"Roadmap item: {FormatOptionalValue(options.RoadmapItem)}");
        Console.WriteLine($"Team: {team}");
        Console.WriteLine($"Priority: {priority}");
        Console.WriteLine("");
        Console.WriteLine("Preview:");
        Console.WriteLine($"# {taskId} - {options.Title}");
        Console.WriteLine("");
        Console.WriteLine("No files were changed.");
        return;
    }

    Directory.CreateDirectory(pendingPath);
    if (File.Exists(outputPath))
        throw new IOException($"Task file already exists: {displayPath}");

    File.WriteAllText(outputPath, content);

    Console.WriteLine("AI Platform Plan");
    Console.WriteLine("");
    Console.WriteLine($"Created task: {displayPath}");
    Console.WriteLine($"Roadmap item: {FormatOptionalValue(options.RoadmapItem)}");
    Console.WriteLine($"Team: {team}");
    Console.WriteLine($"Priority: {priority}");
    Console.WriteLine("");
    Console.WriteLine("Next step: review the generated task, then run the implementation workflow.");
}

static void RunReconcile()
{
    var rootPath = Directory.GetCurrentDirectory();
    var config = LoadPlatformConfig(rootPath).Config;
    var roadmapPath = "ai/roadmap.md";
    var knownGapsPath = "ai/project-memory/known-gaps.md";
    var reportPath = Path.Combine(rootPath, "ai", "reports", "task-reconciliation.md");
    var reportDirectory = Path.GetDirectoryName(reportPath)!;

    Directory.CreateDirectory(reportDirectory);

    var roadmapItems = ParseRoadmapItems(ReadTextIfExists(roadmapPath));
    var roadmapIds = roadmapItems.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var taskSummaries = new[]
    {
        SummarizeTaskDirectory("pending", config.TaskPaths.Pending!),
        SummarizeTaskDirectory("in-progress", config.TaskPaths.InProgress!),
        SummarizeTaskDirectory("done", config.TaskPaths.Done!)
    };
    var tasks = ReadTaskFiles(config);
    var referencedRoadmapIds = tasks
        .SelectMany(task => task.RoadmapReferences)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var roadmapItemsWithoutTaskReferences = roadmapIds
        .Where(id => !referencedRoadmapIds.Contains(id))
        .OrderBy(id => id)
        .ToList();
    var tasksReferencingUnknownRoadmapItems = tasks
        .SelectMany(task => task.RoadmapReferences
            .Where(id => !roadmapIds.Contains(id))
            .Select(id => new TaskReferenceIssue(task.DisplayPath, id)))
        .OrderBy(issue => issue.TaskPath)
        .ThenBy(issue => issue.RoadmapItem)
        .ToList();
    var doneTasksWithoutRoadmapReference = tasks
        .Where(task => task.State == "done" && task.RoadmapReferences.Count == 0)
        .OrderBy(task => task.DisplayPath)
        .ToList();
    var pendingTasksWithoutRoadmapReference = tasks
        .Where(task => task.State == "pending" && task.RoadmapReferences.Count == 0)
        .OrderBy(task => task.DisplayPath)
        .ToList();
    var weakPendingTasks = tasks
        .Where(task => task.State == "pending" && HasWeakPendingMetadata(task))
        .OrderBy(task => task.DisplayPath)
        .ToList();
    var knownGapSignals = ReadKnownGapSignals(knownGapsPath);

    var report = BuildReconciliationReport(
        taskSummaries,
        roadmapItems,
        referencedRoadmapIds,
        roadmapItemsWithoutTaskReferences,
        tasksReferencingUnknownRoadmapItems,
        doneTasksWithoutRoadmapReference,
        pendingTasksWithoutRoadmapReference,
        weakPendingTasks,
        knownGapsPath,
        knownGapSignals);

    File.WriteAllText(reportPath, report);

    Console.WriteLine("AI Platform Reconcile");
    Console.WriteLine("");
    Console.WriteLine("Report written to: ai/reports/task-reconciliation.md");
    Console.WriteLine("");
    Console.WriteLine("Summary:");
    Console.WriteLine($"- Tasks: pending={taskSummaries[0].MarkdownTaskCount}, in-progress={taskSummaries[1].MarkdownTaskCount}, done={taskSummaries[2].MarkdownTaskCount}");
    Console.WriteLine($"- Roadmap items: {roadmapItems.Count}");
    Console.WriteLine($"- Roadmap items without task references: {roadmapItemsWithoutTaskReferences.Count}");
    Console.WriteLine($"- Tasks referencing unknown roadmap items: {tasksReferencingUnknownRoadmapItems.Count}");
    Console.WriteLine($"- Stale/weak pending candidates: {weakPendingTasks.Count}");
    Console.WriteLine("");
    Console.WriteLine("Next step: review ai/reports/task-reconciliation.md");
}

static void RunReview(string[] commandArgs)
{
    var options = ParseReviewOptions(commandArgs);
    if (string.IsNullOrWhiteSpace(options.TaskId) && string.IsNullOrWhiteSpace(options.FilePath))
    {
        ShowReviewHelp();
        return;
    }

    var rootPath = Directory.GetCurrentDirectory();
    var taskPath = ResolveReviewTaskPath(options);
    if (taskPath is null)
    {
        var target = string.IsNullOrWhiteSpace(options.FilePath) ? options.TaskId : options.FilePath;
        Console.WriteLine($"Task not found: {target}");
        return;
    }

    var text = File.ReadAllText(taskPath);
    var roadmapIds = ParseRoadmapItems(ReadTextIfExists("ai/roadmap.md"))
        .Select(item => item.Id)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var result = AnalyzeTaskForReview(taskPath, text, roadmapIds, options.Strict);
    var reportPath = Path.Combine(rootPath, "ai", "reports", "task-review.md");
    Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
    File.WriteAllText(reportPath, BuildTaskReviewReport(result));

    var okCount = result.Checks.Count(check => check.Status == "OK");
    var missingCount = result.Checks.Count(check => check.Status == "MISSING");
    var warningCount = result.Checks.Count(check => check.Status == "WARNING");

    Console.WriteLine("AI Platform Review");
    Console.WriteLine("");
    Console.WriteLine("Report written to: ai/reports/task-review.md");
    Console.WriteLine("");
    Console.WriteLine("Reviewed task:");
    Console.WriteLine($"- ID: {result.TaskId}");
    Console.WriteLine($"- File: {result.DisplayPath}");
    Console.WriteLine($"- Status: {FormatOptionalValue(result.DetectedStatus)}");
    Console.WriteLine($"- Team: {FormatOptionalValue(result.DetectedTeam)}");
    Console.WriteLine($"- Roadmap item: {FormatOptionalValue(result.DetectedRoadmapItem)}");
    Console.WriteLine("");
    Console.WriteLine("Checks:");
    Console.WriteLine($"- OK: {okCount}");
    Console.WriteLine($"- Missing: {missingCount}");
    Console.WriteLine($"- Warnings: {warningCount}");
    Console.WriteLine("");
    Console.WriteLine($"Recommended outcome: {result.RecommendedOutcome}");
    Console.WriteLine("");
    Console.WriteLine("Recommended command:");
    Console.WriteLine(BuildReviewRecommendedNextCommand(result));
    Console.WriteLine("");
    Console.WriteLine("Next step: review ai/reports/task-review.md");
}

static void RunImplement(string[] commandArgs)
{
    var options = ParseImplementOptions(commandArgs);
    if (options.ShowHelp)
        return;

    var rootPath = Directory.GetCurrentDirectory();
    var config = LoadPlatformConfig(rootPath).Config;
    var selectedTask = ResolveImplementTask(config, options);

    if (selectedTask is null)
    {
        if (!string.IsNullOrWhiteSpace(options.TaskId) && IsTaskInProgress(config, options.TaskId))
        {
            Console.WriteLine("AI Platform Implement");
            Console.WriteLine("");
            Console.WriteLine($"Task {NormalizeTaskId(options.TaskId)} is already in progress.");
            Console.WriteLine("implement v1 only starts tasks from pending.");
            return;
        }

        Console.WriteLine("AI Platform Implement");
        Console.WriteLine("");
        Console.WriteLine(string.IsNullOrWhiteSpace(options.TaskId)
            ? "No pending tasks found."
            : $"Pending task not found: {NormalizeTaskId(options.TaskId)}");
        Console.WriteLine("");
        Console.WriteLine("Next step: create a pending task with ai-platform plan or choose a task from ai/tasks/pending.");
        return;
    }

    var taskText = File.ReadAllText(selectedTask.PendingPath);
    var taskInfo = AnalyzeTaskForImplementation(selectedTask.PendingPath, taskText);
    var warnings = BuildImplementationWarnings(taskInfo);
    var promptPath = Path.Combine(rootPath, "ai", "reports", "implementation-prompt.md");
    var destinationPath = Path.Combine(config.TaskPaths.InProgress!, Path.GetFileName(selectedTask.PendingPath));
    var destinationDisplayPath = NormalizeDisplayPath(destinationPath);

    if (options.DryRun)
    {
        Console.WriteLine("AI Platform Implement dry run");
        Console.WriteLine("");
        Console.WriteLine($"Would select task: {taskInfo.TaskId}");
        Console.WriteLine($"Would move from: {selectedTask.DisplayPath}");
        Console.WriteLine($"Would move to: {destinationDisplayPath}");
        Console.WriteLine("Would write prompt to: ai/reports/implementation-prompt.md");
        Console.WriteLine("");
        Console.WriteLine("Warnings:");
        PrintWarnings(warnings);
        Console.WriteLine("");
        Console.WriteLine("Recommended next command after a real implementation:");
        Console.WriteLine(BuildImplementRecommendedReviewCommand(taskInfo.TaskId));
        Console.WriteLine("");
        Console.WriteLine("No files were changed.");
        return;
    }

    var promptTaskPath = options.NoMove ? selectedTask.DisplayPath : destinationDisplayPath;

    if (!options.NoMove)
    {
        Directory.CreateDirectory(config.TaskPaths.InProgress!);
        if (File.Exists(destinationPath))
        {
            Console.WriteLine("AI Platform Implement");
            Console.WriteLine("");
            Console.WriteLine($"Cannot move task because destination already exists: {destinationDisplayPath}");
            Console.WriteLine("No files were changed.");
            return;
        }

        File.Move(selectedTask.PendingPath, destinationPath);
    }

    Directory.CreateDirectory(Path.GetDirectoryName(promptPath)!);
    File.WriteAllText(promptPath, BuildImplementationPrompt(taskInfo, promptTaskPath, taskText));

    Console.WriteLine("AI Platform Implement");
    Console.WriteLine("");
    Console.WriteLine($"Selected task: {taskInfo.TaskId}");
    if (options.NoMove)
        Console.WriteLine("Task was not moved because --no-move was used.");
    else
        Console.WriteLine($"Moved task to: {destinationDisplayPath}");
    Console.WriteLine("Implementation prompt written to: ai/reports/implementation-prompt.md");
    Console.WriteLine("");
    Console.WriteLine("Warnings:");
    PrintWarnings(warnings);
    Console.WriteLine("");
    Console.WriteLine("Recommended next command after implementation:");
    Console.WriteLine(BuildImplementRecommendedReviewCommand(taskInfo.TaskId));
    Console.WriteLine("");
    Console.WriteLine(options.NoMove
        ? "Next step: review the generated prompt."
        : "Next step: open ai/reports/implementation-prompt.md and run it with Codex.");
}

static void RunTaskCommand(string[] commandArgs)
{
    if (commandArgs.Length == 0)
    {
        ShowTaskHelp();
        return;
    }

    var subcommand = commandArgs[0];
    var subcommandArgs = commandArgs.Skip(1).ToArray();

    switch (subcommand)
    {
        case "move":
            RunTaskMove(subcommandArgs);
            break;
        case "-h":
        case "--help":
            ShowTaskHelp();
            break;
        default:
            Console.WriteLine($"Unknown task subcommand: {subcommand}");
            Console.WriteLine("");
            ShowTaskHelp();
            break;
    }
}

static void RunTaskMove(string[] commandArgs)
{
    TaskMoveOptions options;
    try
    {
        options = ParseTaskMoveOptions(commandArgs);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("AI Platform Task Move");
        Console.WriteLine("");
        Console.WriteLine(ex.Message);
        Console.WriteLine("");
        ShowTaskMoveHelp();
        return;
    }

    if (options.ShowHelp || string.IsNullOrWhiteSpace(options.TaskId) || string.IsNullOrWhiteSpace(options.TargetState))
    {
        ShowTaskMoveHelp();
        return;
    }

    var rootPath = Directory.GetCurrentDirectory();
    var config = LoadPlatformConfig(rootPath).Config;
    var targetState = NormalizeTaskStatus(options.TargetState);
    if (!GetAllowedTaskMoveStates().Contains(targetState, StringComparer.OrdinalIgnoreCase))
    {
        Console.WriteLine("AI Platform Task Move");
        Console.WriteLine("");
        Console.WriteLine($"Unsupported target state: {options.TargetState}");
        Console.WriteLine($"Allowed states: {string.Join(", ", GetAllowedTaskMoveStates())}");
        return;
    }

    var matches = FindTaskLocations(config, options.TaskId);
    if (matches.Count == 0)
    {
        Console.WriteLine("AI Platform Task Move");
        Console.WriteLine("");
        Console.WriteLine($"Task not found: {NormalizeTaskId(options.TaskId)}");
        return;
    }

    if (matches.Count > 1)
    {
        Console.WriteLine("AI Platform Task Move");
        Console.WriteLine("");
        Console.WriteLine($"Task is ambiguous: {NormalizeTaskId(options.TaskId)}");
        Console.WriteLine("The same task was found in multiple lifecycle states:");
        foreach (var match in matches.OrderBy(match => match.DisplayPath, StringComparer.OrdinalIgnoreCase))
            Console.WriteLine($"- {match.State}: {match.DisplayPath}");
        Console.WriteLine("");
        Console.WriteLine("Resolve the duplicate task locations before moving it.");
        return;
    }

    var task = matches[0];
    if (string.Equals(task.State, targetState, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine(options.DryRun ? "AI Platform Task Move dry run" : "AI Platform Task Move");
        Console.WriteLine("");
        Console.WriteLine($"Task: {task.TaskId}");
        Console.WriteLine($"Current state: {task.State}");
        Console.WriteLine($"Target state: {targetState}");
        Console.WriteLine($"Source: {task.DisplayPath}");
        Console.WriteLine($"Destination: {task.DisplayPath}");
        Console.WriteLine("Task is already in the requested state.");
        if (options.DryRun)
        {
            Console.WriteLine("");
            Console.WriteLine("No files were changed.");
        }
        return;
    }

    var destinationRoot = GetTaskPathByState(config, targetState);
    var destinationPath = Path.Combine(destinationRoot, Path.GetFileName(task.Path));
    var destinationDisplayPath = NormalizeDisplayPath(destinationPath);
    var requiresForce = RequiresForceForTaskMove(task.State, targetState);

    var originalText = File.ReadAllText(task.Path);
    var statusUpdate = UpdateTaskStatusMetadata(originalText, targetState);

    if (!options.DryRun && requiresForce && !options.Force)
    {
        Console.WriteLine("AI Platform Task Move");
        Console.WriteLine("");
        Console.WriteLine($"Task: {task.TaskId}");
        Console.WriteLine($"Current state: {task.State}");
        Console.WriteLine($"Target state: {targetState}");
        Console.WriteLine($"Source: {task.DisplayPath}");
        Console.WriteLine($"Destination: {destinationDisplayPath}");
        Console.WriteLine("");
        Console.WriteLine("This move requires --force.");
        Console.WriteLine("Use --dry-run first if you want to inspect the transition safely.");
        return;
    }

    if (options.DryRun)
    {
        Console.WriteLine("AI Platform Task Move dry run");
        Console.WriteLine("");
        Console.WriteLine($"Task: {task.TaskId}");
        Console.WriteLine($"Current state: {task.State}");
        Console.WriteLine($"Target state: {targetState}");
        Console.WriteLine($"Source: {task.DisplayPath}");
        Console.WriteLine($"Destination: {destinationDisplayPath}");
        Console.WriteLine($"Force required: {(requiresForce ? "yes" : "no")}");
        Console.WriteLine($"Status metadata: {DescribeTaskStatusUpdate(statusUpdate, task.State, targetState, true)}");
        Console.WriteLine("");
        Console.WriteLine("No files were changed.");
        return;
    }

    Directory.CreateDirectory(destinationRoot);
    if (File.Exists(destinationPath))
    {
        Console.WriteLine("AI Platform Task Move");
        Console.WriteLine("");
        Console.WriteLine($"Cannot move task because destination already exists: {destinationDisplayPath}");
        Console.WriteLine("No files were changed.");
        return;
    }

    if (statusUpdate.Updated)
        File.WriteAllText(task.Path, statusUpdate.UpdatedText);

    File.Move(task.Path, destinationPath);

    Console.WriteLine("AI Platform Task Move");
    Console.WriteLine("");
    Console.WriteLine($"Task: {task.TaskId}");
    Console.WriteLine($"Moved from: {task.State}");
    Console.WriteLine($"Moved to: {targetState}");
    Console.WriteLine($"Source: {task.DisplayPath}");
    Console.WriteLine($"Destination: {destinationDisplayPath}");
    Console.WriteLine($"Status metadata: {DescribeTaskStatusUpdate(statusUpdate, task.State, targetState, false)}");
    if (requiresForce)
        Console.WriteLine("Force: applied");
    Console.WriteLine("");
    Console.WriteLine(GetTaskMoveNextStep(targetState, task.TaskId));
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

static ReviewOptions ParseReviewOptions(string[] args)
{
    var options = new ReviewOptions();

    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        switch (arg)
        {
            case "--task":
                options.TaskId = ReadOptionValue(args, ref index, arg).ToUpperInvariant();
                break;
            case "--file":
                options.FilePath = ReadOptionValue(args, ref index, arg);
                break;
            case "--strict":
                options.Strict = true;
                break;
            case "-h":
            case "--help":
                break;
            default:
                Console.WriteLine($"Ignoring unknown review argument: {arg}");
                break;
        }
    }

    return options;
}

static void ShowReviewHelp()
{
    Console.WriteLine("AI Platform Review");
    Console.WriteLine("");
    Console.WriteLine("Reviews one task and writes ai/reports/task-review.md.");
    Console.WriteLine("");
    Console.WriteLine("Usage:");
    Console.WriteLine("  ai-platform review --task TASK-0001 [--strict]");
    Console.WriteLine("  ai-platform review --file ai/tasks/review/TASK-0001.md [--strict]");
    Console.WriteLine("");
    Console.WriteLine("If both --task and --file are provided, --file is used.");
}

static TaskMoveOptions ParseTaskMoveOptions(string[] args)
{
    var options = new TaskMoveOptions();

    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        switch (arg)
        {
            case "--task":
                options.TaskId = NormalizeTaskId(ReadOptionValue(args, ref index, arg));
                break;
            case "--to":
                options.TargetState = NormalizeTaskStatus(ReadOptionValue(args, ref index, arg));
                break;
            case "--dry-run":
                options.DryRun = true;
                break;
            case "--force":
                options.Force = true;
                break;
            case "-h":
            case "--help":
                options.ShowHelp = true;
                break;
            default:
                Console.WriteLine($"Ignoring unknown task move argument: {arg}");
                break;
        }
    }

    return options;
}

static GitIgnoreOptions ParseGitIgnoreOptions(string[] args)
{
    var options = new GitIgnoreOptions();

    foreach (var arg in args)
    {
        switch (arg)
        {
            case "--dry-run":
                options.DryRun = true;
                break;
            case "-h":
            case "--help":
                options.ShowHelp = true;
                break;
            default:
                Console.WriteLine($"Ignoring unknown git-ignore argument: {arg}");
                break;
        }
    }

    if (options.ShowHelp)
        ShowGitIgnoreHelp();

    return options;
}

static void ShowGitIgnoreHelp()
{
    Console.WriteLine("AI Platform Git Ignore");
    Console.WriteLine("");
    Console.WriteLine("Adds or updates the managed AI DEV PLATFORM LOCAL TOOLING block in .gitignore.");
    Console.WriteLine("Use this explicitly in consumer repositories that want platform tooling to remain local.");
    Console.WriteLine("");
    Console.WriteLine("Usage:");
    Console.WriteLine("  ai-platform git-ignore [--dry-run]");
}

static void ShowTaskHelp()
{
    Console.WriteLine("AI Platform Task");
    Console.WriteLine("");
    Console.WriteLine("Subcommands:");
    Console.WriteLine("  ai-platform task move   Move a task between lifecycle states");
}

static void ShowTaskMoveHelp()
{
    Console.WriteLine("AI Platform Task Move");
    Console.WriteLine("");
    Console.WriteLine("Moves one task between lifecycle states with explicit safety rules.");
    Console.WriteLine("");
    Console.WriteLine("Usage:");
    Console.WriteLine("  ai-platform task move --task TASK-0001 --to review [--dry-run] [--force]");
    Console.WriteLine("");
    Console.WriteLine("Allowed states:");
    Console.WriteLine($"  {string.Join(", ", GetAllowedTaskMoveStates())}");
    Console.WriteLine("");
    Console.WriteLine("Notes:");
    Console.WriteLine("  - --task and --to are required.");
    Console.WriteLine("  - --force is required for dangerous transitions such as direct jumps to done outside review.");
    Console.WriteLine("  - The command updates task status metadata when it finds a recognized status line.");
}

static RefreshOptions ParseRefreshOptions(string[] args)
{
    var options = new RefreshOptions();

    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        switch (arg)
        {
            case "--apply":
                options.Apply = true;
                break;
            case "--source":
                options.Source = ReadOptionValue(args, ref index, arg);
                break;
            case "-h":
            case "--help":
                options.ShowHelp = true;
                break;
            default:
                Console.WriteLine($"Ignoring unknown refresh argument: {arg}");
                break;
        }
    }

    if (options.ShowHelp)
        ShowRefreshHelp();

    return options;
}

static GitIgnoreUpdate BuildGitIgnoreUpdate(string existingContent, string managedBlock)
{
    const string beginMarker = "# BEGIN AI DEV PLATFORM LOCAL TOOLING";
    const string endMarker = "# END AI DEV PLATFORM LOCAL TOOLING";
    var normalizedExisting = existingContent.Replace("\r\n", "\n");
    var normalizedBlock = managedBlock.Replace("\r\n", "\n");
    var beginIndex = normalizedExisting.IndexOf(beginMarker, StringComparison.Ordinal);
    var endIndex = normalizedExisting.IndexOf(endMarker, StringComparison.Ordinal);

    if (beginIndex >= 0 && endIndex >= beginIndex)
    {
        var endExclusive = endIndex + endMarker.Length;
        var currentBlock = normalizedExisting[beginIndex..endExclusive];
        if (string.Equals(currentBlock, normalizedBlock, StringComparison.Ordinal))
            return new GitIgnoreUpdate("unchanged", existingContent);

        var updated = normalizedExisting[..beginIndex]
            + normalizedBlock
            + normalizedExisting[endExclusive..];
        return new GitIgnoreUpdate("update", RestorePlatformLineEndings(updated));
    }

    var prefix = string.IsNullOrWhiteSpace(existingContent)
        ? ""
        : existingContent.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? existingContent + Environment.NewLine
            : existingContent + Environment.NewLine + Environment.NewLine;
    return new GitIgnoreUpdate("add", prefix + managedBlock + Environment.NewLine);
}

static string BuildManagedGitIgnoreBlock()
{
    return string.Join(Environment.NewLine, new[]
    {
        "# BEGIN AI DEV PLATFORM LOCAL TOOLING",
        "AGENTS.md",
        "ai-platform.json",
        "ai/",
        "scripts/codex-runner.ps1",
        "scripts/run-integration-tests.ps1",
        ".github/workflows/codex-worker.yml",
        "ai-platform-cli/",
        "# END AI DEV PLATFORM LOCAL TOOLING"
    });
}

static string RestorePlatformLineEndings(string content)
{
    return content.Replace("\n", Environment.NewLine);
}

static bool HasManagedGitIgnoreBlock(string rootPath)
{
    var gitIgnorePath = Path.Combine(rootPath, ".gitignore");
    if (!File.Exists(gitIgnorePath))
        return false;

    var content = File.ReadAllText(gitIgnorePath);
    return content.Contains("# BEGIN AI DEV PLATFORM LOCAL TOOLING", StringComparison.Ordinal)
        && content.Contains("# END AI DEV PLATFORM LOCAL TOOLING", StringComparison.Ordinal);
}

static bool IsConsumerLocalInstallMode(string? installMode)
{
    return string.Equals(installMode, "consumer-local", StringComparison.OrdinalIgnoreCase);
}

static void ShowRefreshHelp()
{
    Console.WriteLine("AI Platform Refresh");
    Console.WriteLine("");
    Console.WriteLine("Refreshes managed platform artifacts from a compatible template ZIP source.");
    Console.WriteLine("Dry-run is the default. Use --apply to write changes.");
    Console.WriteLine("");
    Console.WriteLine("Usage:");
    Console.WriteLine("  ai-platform refresh [--source <zip-url>] [--apply]");
    Console.WriteLine("");
    Console.WriteLine("Source precedence:");
    Console.WriteLine("  1. --source");
    Console.WriteLine("  2. AI_PLATFORM_TEMPLATE_ZIP");
    Console.WriteLine("  3. templateSourceZip in ai-platform.json");
    Console.WriteLine("  4. built-in default");
}

static RefreshSourceInfo ResolveRefreshSource(RefreshOptions options, PlatformConfigLoadResult configResult)
{
    if (!string.IsNullOrWhiteSpace(options.Source))
        return new RefreshSourceInfo(options.Source.Trim(), "command argument");

    var envSource = Environment.GetEnvironmentVariable("AI_PLATFORM_TEMPLATE_ZIP");
    if (!string.IsNullOrWhiteSpace(envSource))
        return new RefreshSourceInfo(envSource, "AI_PLATFORM_TEMPLATE_ZIP");

    if (!configResult.FallbackKeys.Contains("templateSourceZip", StringComparer.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(configResult.Config.TemplateSourceZip))
        return new RefreshSourceInfo(configResult.Config.TemplateSourceZip, "ai-platform.json");

    return new RefreshSourceInfo(GetBuiltInTemplateZipSource(), "built-in default");
}

static void DownloadTemplateZip(string source, string targetPath)
{
    if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri)
        || (sourceUri.Scheme != Uri.UriSchemeHttps && sourceUri.Scheme != Uri.UriSchemeHttp))
        throw new RefreshCommandException("Invalid source URL. Provide a valid HTTP or HTTPS ZIP URL.");

    try
    {
        using var client = new HttpClient();
        using var response = client.GetAsync(sourceUri).Result;

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            throw new RefreshCommandException($"Source request failed with {(int)response.StatusCode} {response.StatusCode}. Check repository access or credentials.");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new RefreshCommandException("Source request failed with 404 Not Found. Check --source, AI_PLATFORM_TEMPLATE_ZIP, or templateSourceZip.");

        if (!response.IsSuccessStatusCode)
            throw new RefreshCommandException($"Source request failed with {(int)response.StatusCode} {response.StatusCode}.");

        var bytes = response.Content.ReadAsByteArrayAsync().Result;
        File.WriteAllBytes(targetPath, bytes);
    }
    catch (AggregateException ex) when (ex.InnerException is HttpRequestException httpEx)
    {
        throw CreateRefreshHttpException(httpEx);
    }
    catch (HttpRequestException ex)
    {
        throw CreateRefreshHttpException(ex);
    }
    catch (IOException ex)
    {
        throw new RefreshCommandException($"Could not store the downloaded ZIP: {ex.Message}");
    }
    catch (InvalidOperationException ex)
    {
        throw new RefreshCommandException($"Could not use the configured source URL: {ex.Message}");
    }
}

static RefreshCommandException CreateRefreshHttpException(HttpRequestException ex)
{
    if (ex.StatusCode == HttpStatusCode.Unauthorized || ex.StatusCode == HttpStatusCode.Forbidden)
        return new RefreshCommandException($"Source request failed with {(int)ex.StatusCode} {ex.StatusCode}. Check repository access or credentials.");

    if (ex.StatusCode == HttpStatusCode.NotFound)
        return new RefreshCommandException("Source request failed with 404 Not Found. Check --source, AI_PLATFORM_TEMPLATE_ZIP, or templateSourceZip.");

    if (ex.StatusCode is not null)
        return new RefreshCommandException($"Source request failed with {(int)ex.StatusCode} {ex.StatusCode}.");

    return new RefreshCommandException($"Network error while downloading the source ZIP: {ex.Message}");
}

static RefreshComparisonResult CompareManagedArtifacts(string rootPath, string sourceRoot, IReadOnlyList<string> managedArtifacts)
{
    var created = new List<string>();
    var updated = new List<string>();
    var unchanged = new List<string>();
    var missingInSource = new List<string>();

    foreach (var artifact in managedArtifacts)
    {
        var sourcePath = Path.Combine(sourceRoot, artifact.Replace('/', Path.DirectorySeparatorChar));
        var localPath = Path.Combine(rootPath, artifact.Replace('/', Path.DirectorySeparatorChar));
        var displayArtifact = NormalizeDisplayPath(artifact);

        var sourceIsFile = File.Exists(sourcePath);
        var sourceIsDirectory = Directory.Exists(sourcePath);

        if (!sourceIsFile && !sourceIsDirectory)
        {
            missingInSource.Add(displayArtifact);
            continue;
        }

        if (sourceIsFile)
        {
            if (!File.Exists(localPath))
            {
                created.Add(displayArtifact);
                continue;
            }

            if (FilesEqual(sourcePath, localPath))
                unchanged.Add(displayArtifact);
            else
                updated.Add(displayArtifact);

            continue;
        }

        if (!Directory.Exists(localPath))
        {
            created.Add(displayArtifact);
            continue;
        }

        var sourceFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
            .Select(path => NormalizeDisplayPath(Path.GetRelativePath(sourcePath, path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sourceFiles.Count == 0)
        {
            unchanged.Add(displayArtifact);
            continue;
        }

        var hasChanges = false;
        foreach (var relativePath in sourceFiles)
        {
            var sourceFilePath = Path.Combine(sourcePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var localFilePath = Path.Combine(localPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(localFilePath) || !FilesEqual(sourceFilePath, localFilePath))
            {
                hasChanges = true;
                break;
            }
        }

        if (hasChanges)
            updated.Add(displayArtifact);
        else
            unchanged.Add(displayArtifact);
    }

    return new RefreshComparisonResult(created, updated, unchanged, missingInSource);
}

static bool FilesEqual(string leftPath, string rightPath)
{
    var leftInfo = new FileInfo(leftPath);
    var rightInfo = new FileInfo(rightPath);
    if (!leftInfo.Exists || !rightInfo.Exists)
        return false;

    if (leftInfo.Length != rightInfo.Length)
        return false;

    return string.Equals(GetFileHash(leftPath), GetFileHash(rightPath), StringComparison.OrdinalIgnoreCase);
}

static string GetFileHash(string path)
{
    using var stream = File.OpenRead(path);
    using var sha = SHA256.Create();
    return Convert.ToHexString(sha.ComputeHash(stream));
}

static void ApplyRefreshChanges(string rootPath, string sourceRoot, RefreshComparisonResult comparison)
{
    foreach (var artifact in comparison.Created.Concat(comparison.Updated).Distinct(StringComparer.OrdinalIgnoreCase))
    {
        var sourcePath = Path.Combine(sourceRoot, artifact.Replace('/', Path.DirectorySeparatorChar));
        var localPath = Path.Combine(rootPath, artifact.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(sourcePath))
        {
            if (Directory.Exists(localPath))
                throw new RefreshCommandException($"Cannot update managed artifact '{artifact}' because the source is a file and the local path is a directory.");

            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            File.Copy(sourcePath, localPath, true);
            continue;
        }

        if (Directory.Exists(sourcePath))
        {
            if (File.Exists(localPath))
                throw new RefreshCommandException($"Cannot update managed artifact '{artifact}' because the source is a directory and the local path is a file.");

            CopyDirectoryContents(sourcePath, localPath);
            continue;
        }

        throw new RefreshCommandException($"Managed artifact '{artifact}' was selected for apply, but it is missing in the source.");
    }
}

static void CopyDirectoryContents(string sourceDir, string destDir)
{
    Directory.CreateDirectory(destDir);

    foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(sourceDir, file);
        var destinationPath = Path.Combine(destDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(file, destinationPath, true);
    }
}

static void PrintRefreshSummary(PlatformConfig config, RefreshSourceInfo refreshSource, RefreshComparisonResult comparison, bool apply)
{
    Console.WriteLine(apply ? "AI Platform Refresh" : "AI Platform Refresh dry run");
    Console.WriteLine("");
    Console.WriteLine($"Source: {refreshSource.Source}");
    Console.WriteLine($"Source selection: {refreshSource.Selection}");
    Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");
    Console.WriteLine("");
    Console.WriteLine($"Validation: checked required template paths: {string.Join(", ", config.RequiredTemplatePaths)}");
    Console.WriteLine("");
    Console.WriteLine("Managed artifacts:");
    Console.WriteLine($"- Create: {FormatSummaryItems(comparison.Created)}");
    Console.WriteLine($"- Update: {FormatSummaryItems(comparison.Updated)}");
    Console.WriteLine($"- Unchanged: {FormatSummaryItems(comparison.Unchanged)}");
    Console.WriteLine($"- Missing in source: {FormatSummaryItems(comparison.MissingInSource)}");
    Console.WriteLine("");

    if (!apply)
    {
        Console.WriteLine("No files were changed.");
        Console.WriteLine("");
        Console.WriteLine("Next step: rerun with `ai-platform refresh --apply` to update managed artifacts.");
        return;
    }

    Console.WriteLine("Scope: create/update managed artifacts only; never deletes artifacts.");
    Console.WriteLine("Commits: not created by refresh v1.");
}

static ImplementOptions ParseImplementOptions(string[] args)
{
    var options = new ImplementOptions();

    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        switch (arg)
        {
            case "--task":
                options.TaskId = NormalizeTaskId(ReadOptionValue(args, ref index, arg));
                break;
            case "--dry-run":
                options.DryRun = true;
                break;
            case "--no-move":
                options.NoMove = true;
                break;
            case "-h":
            case "--help":
                options.ShowHelp = true;
                break;
            default:
                Console.WriteLine($"Ignoring unknown implement argument: {arg}");
                break;
        }
    }

    if (options.ShowHelp)
        ShowImplementHelp();

    return options;
}

static void ShowImplementHelp()
{
    Console.WriteLine("AI Platform Implement");
    Console.WriteLine("");
    Console.WriteLine("Prepares one pending task for implementation.");
    Console.WriteLine("");
    Console.WriteLine("Usage:");
    Console.WriteLine("  ai-platform implement [--task TASK-0001] [--dry-run] [--no-move]");
    Console.WriteLine("");
    Console.WriteLine("v1 moves pending tasks to in-progress and writes ai/reports/implementation-prompt.md.");
    Console.WriteLine("It does not execute Codex, implement code, or move tasks to done.");
}

static PendingTaskSelection? ResolveImplementTask(PlatformConfig config, ImplementOptions options)
{
    var pendingPath = config.TaskPaths.Pending!;
    if (!Directory.Exists(pendingPath))
        return null;

    string? taskPath;
    if (string.IsNullOrWhiteSpace(options.TaskId))
    {
        taskPath = Directory.GetFiles(pendingPath, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }
    else
    {
        var taskId = NormalizeTaskId(options.TaskId);
        var directPath = Path.Combine(pendingPath, $"{taskId}.md");
        taskPath = File.Exists(directPath)
            ? directPath
            : Directory.GetFiles(pendingPath, $"{taskId}*.md", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
    }

    return taskPath is null
        ? null
        : new PendingTaskSelection(taskPath, NormalizeDisplayPath(taskPath));
}

static bool IsTaskInProgress(PlatformConfig config, string taskId)
{
    var inProgressPath = config.TaskPaths.InProgress!;
    if (!Directory.Exists(inProgressPath))
        return false;

    var normalizedTaskId = NormalizeTaskId(taskId);
    return File.Exists(Path.Combine(inProgressPath, $"{normalizedTaskId}.md"))
        || Directory.GetFiles(inProgressPath, $"{normalizedTaskId}*.md", SearchOption.TopDirectoryOnly).Any();
}

static ImplementationTaskInfo AnalyzeTaskForImplementation(string path, string text)
{
    var taskId = DetectTaskId(text, path) ?? Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
    var roadmapReferences = Regex.Matches(text, @"\bR-\d{3}\b", RegexOptions.IgnoreCase)
        .Select(match => match.Value.ToUpperInvariant())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value)
        .ToList();
    var roadmapItem = ReadMetadataValue(text, "roadmap_item")
        ?? roadmapReferences.FirstOrDefault();

    return new ImplementationTaskInfo(
        taskId,
        ExtractTaskTitle(text, path),
        ReadMetadataValue(text, "team"),
        roadmapItem,
        HasTaskId(text, path),
        !string.IsNullOrWhiteSpace(ExtractTaskTitle(text, path)),
        HasTaskMetadata(text, "team"),
        !string.IsNullOrWhiteSpace(roadmapItem) || HasJustificationSignal(text),
        HasAcceptanceCriteria(text),
        HasHeading(text, "Validation"),
        HasHeading(text, "Commit and push"));
}

static bool HasTaskId(string text, string path)
{
    return DetectTaskId(text, path) is not null;
}

static bool HasJustificationSignal(string text)
{
    return Regex.IsMatch(text, @"\b(justification|justificacion|justificación|explicit planning request)\b", RegexOptions.IgnoreCase);
}

static List<string> BuildImplementationWarnings(ImplementationTaskInfo task)
{
    var warnings = new List<string>();

    if (!task.HasTaskId)
        warnings.Add("task id is missing.");
    if (!task.HasTitle)
        warnings.Add("title is missing.");
    if (!task.HasTeam)
        warnings.Add("team is missing.");
    if (!task.HasRoadmapItemOrJustification)
        warnings.Add("roadmap item or justification is missing.");
    if (!task.HasAcceptanceCriteria)
        warnings.Add("acceptance criteria are missing.");
    if (!task.HasValidation)
        warnings.Add("validation section is missing.");
    if (!task.HasCommitAndPush)
        warnings.Add("commit and push section is missing.");

    return warnings;
}

static void PrintWarnings(IReadOnlyList<string> warnings)
{
    if (warnings.Count == 0)
    {
        Console.WriteLine("- none");
        return;
    }

    foreach (var warning in warnings)
        Console.WriteLine($"- {warning}");
}

static string BuildImplementationPrompt(ImplementationTaskInfo task, string taskPath, string taskText)
{
    var builder = new StringBuilder();

    builder.AppendLine("# Implementation Prompt");
    builder.AppendLine();
    builder.AppendLine("## Task");
    builder.AppendLine($"- ID: {task.TaskId}");
    builder.AppendLine($"- File: {taskPath}");
    builder.AppendLine($"- Team: {FormatOptionalValue(task.Team)}");
    builder.AppendLine($"- Roadmap item: {FormatOptionalValue(task.RoadmapItem)}");
    builder.AppendLine();
    builder.AppendLine("## Instructions for Codex");
    builder.AppendLine();
    builder.AppendLine("You are implementing this task from the AI platform workflow.");
    builder.AppendLine();
    builder.AppendLine("Follow these rules:");
    builder.AppendLine("1. Read AGENTS.md first.");
    builder.AppendLine("2. Read the task file completely.");
    builder.AppendLine("3. Read all files listed in \"Files to read first\".");
    builder.AppendLine("4. Implement the smallest safe increment.");
    builder.AppendLine("5. Do not change unrelated files.");
    builder.AppendLine("6. Update documentation when behavior changes.");
    builder.AppendLine("7. Run validation commands.");
    builder.AppendLine("8. Do not commit build artifacts.");
    builder.AppendLine("9. When implementation is complete, move the task to review if appropriate, not directly to done unless repository policy explicitly allows it.");
    builder.AppendLine("10. Commit with a clear message.");
    builder.AppendLine("11. Push the branch to the remote.");
    builder.AppendLine();
    builder.AppendLine("## Recommended next command after implementation");
    builder.AppendLine();
    builder.AppendLine("After Codex finishes the implementation and validation, move the task to review with:");
    builder.AppendLine();
    builder.AppendLine("```bash");
    builder.AppendLine(BuildImplementRecommendedReviewCommand(task.TaskId));
    builder.AppendLine("```");
    builder.AppendLine();
    builder.AppendLine("Do not move the task directly to done. Run review before closing the task lifecycle.");
    builder.AppendLine();
    builder.AppendLine("## Task content");
    builder.AppendLine();
    builder.AppendLine(taskText.TrimEnd());
    builder.AppendLine();

    return builder.ToString();
}

static string BuildImplementRecommendedReviewCommand(string taskId)
{
    return $"ai-platform task move --task {taskId} --to review";
}

static string NormalizeTaskId(string value)
{
    return Path.GetFileNameWithoutExtension(value.Trim()).ToUpperInvariant();
}

static string? ResolveReviewTaskPath(ReviewOptions options)
{
    if (!string.IsNullOrWhiteSpace(options.FilePath))
        return File.Exists(options.FilePath) ? options.FilePath : null;

    if (string.IsNullOrWhiteSpace(options.TaskId))
        return null;

    var taskId = options.TaskId.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
        ? Path.GetFileNameWithoutExtension(options.TaskId)
        : options.TaskId;
    var searchRoots = new[]
    {
        "ai/tasks/review",
        "ai/tasks/in-progress",
        "ai/tasks/pending",
        "ai/tasks/done",
        "ai/tasks/blocked",
        "ai/tasks/obsolete"
    };

    foreach (var root in searchRoots.Where(Directory.Exists))
    {
        var directPath = Path.Combine(root, $"{taskId}.md");
        if (File.Exists(directPath))
            return directPath;

        var match = Directory.GetFiles(root, $"{taskId}*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path)
            .FirstOrDefault();
        if (match is not null)
            return match;
    }

    return null;
}

static TaskReviewResult AnalyzeTaskForReview(
    string path,
    string text,
    IReadOnlySet<string> roadmapIds,
    bool strict)
{
    var displayPath = NormalizeDisplayPath(path);
    var taskId = DetectTaskId(text, path);
    var title = ExtractTaskTitle(text, path);
    var detectedStatus = ReadMetadataValue(text, "status");
    var detectedTeam = ReadMetadataValue(text, "team");
    var detectedType = ReadMetadataValue(text, "type");
    var detectedPriority = ReadMetadataValue(text, "priority");
    var roadmapReferences = Regex.Matches(text, @"\bR-\d{3}\b", RegexOptions.IgnoreCase)
        .Select(match => match.Value.ToUpperInvariant())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value)
        .ToList();
    var detectedRoadmapItem = ReadMetadataValue(text, "roadmap_item")
        ?? roadmapReferences.FirstOrDefault();
    var folderStatus = DetectStatusFromPath(path);
    var checks = new List<ReviewCheck>
    {
        BuildCheck("task id", !string.IsNullOrWhiteSpace(taskId), "Task ID detected."),
        BuildCheck("title", !string.IsNullOrWhiteSpace(title), "Title detected."),
        BuildStatusCheck(detectedStatus),
        BuildCheck("team", !string.IsNullOrWhiteSpace(detectedTeam), "Team detected."),
        BuildCheck("roadmap item", !string.IsNullOrWhiteSpace(detectedRoadmapItem), "Roadmap item or R-xxx reference detected."),
        BuildCheck("acceptance criteria", HasAcceptanceCriteria(text), "Acceptance criteria section detected."),
        BuildCheck("validation", HasHeading(text, "Validation"), "Validation section detected."),
        BuildCheck("commit and push", HasHeading(text, "Commit and push"), "Commit and push section detected."),
        BuildCheck("files to read first", HasHeading(text, "Files to read first"), "Files to read first section detected."),
        BuildCheck("expected files to modify", HasHeading(text, "Expected files to modify"), "Expected files to modify section detected."),
        BuildCheck("implementation steps", HasHeading(text, "Implementation steps") || HasHeading(text, "Steps"), "Implementation steps or steps section detected.")
    };
    var issues = BuildReviewIssues(
        text,
        taskId,
        detectedStatus,
        folderStatus,
        detectedTeam,
        detectedRoadmapItem,
        roadmapReferences,
        roadmapIds,
        checks,
        strict);
    var outcome = DetermineReviewOutcome(text, detectedStatus, folderStatus, checks, issues, detectedTeam, detectedRoadmapItem);
    var recommendations = BuildReviewRecommendations(outcome, issues);

    return new TaskReviewResult(
        taskId ?? Path.GetFileNameWithoutExtension(path),
        displayPath,
        detectedStatus,
        folderStatus,
        detectedTeam,
        detectedType,
        detectedPriority,
        detectedRoadmapItem,
        title,
        checks,
        issues,
        outcome,
        recommendations);
}

static string? DetectTaskId(string text, string path)
{
    var metadata = ReadMetadataValue(text, "id");
    if (!string.IsNullOrWhiteSpace(metadata) && Regex.IsMatch(metadata, @"^TASK-\d+", RegexOptions.IgnoreCase))
        return metadata.ToUpperInvariant();

    var match = Regex.Match(text, @"\bTASK-\d+\b", RegexOptions.IgnoreCase);
    if (match.Success)
        return match.Value.ToUpperInvariant();

    var fileMatch = Regex.Match(Path.GetFileName(path), @"\bTASK-\d+\b", RegexOptions.IgnoreCase);
    return fileMatch.Success ? fileMatch.Value.ToUpperInvariant() : null;
}

static string? ReadMetadataValue(string text, string metadataName)
{
    var match = Regex.Match(text, $@"(?im)^\s*{Regex.Escape(metadataName)}\s*:\s*[""']?(.+?)[""']?\s*$");
    if (!match.Success)
        return null;

    var value = match.Groups[1].Value.Trim().Trim('"', '\'');
    return string.IsNullOrWhiteSpace(value) ? null : value;
}

static string DetectStatusFromPath(string path)
{
    var normalized = NormalizeDisplayPath(path).ToLowerInvariant();
    foreach (var status in new[] { "review", "in-progress", "pending", "done", "blocked", "obsolete" })
    {
        if (normalized.Contains($"/{status}/", StringComparison.Ordinal))
            return status;
    }

    return "unknown";
}

static ReviewCheck BuildCheck(string name, bool passed, string message)
{
    return new ReviewCheck(name, passed ? "OK" : "MISSING", passed ? message : $"{name} is missing.");
}

static ReviewCheck BuildStatusCheck(string? status)
{
    if (string.IsNullOrWhiteSpace(status))
        return new ReviewCheck("status", "MISSING", "status is missing.");

    var allowed = new[] { "pending", "in-progress", "review", "done", "blocked", "obsolete" };
    return allowed.Contains(status, StringComparer.OrdinalIgnoreCase)
        ? new ReviewCheck("status", "OK", $"status detected: {status}.")
        : new ReviewCheck("status", "WARNING", $"unknown status: {status}.");
}

static bool HasHeading(string text, string heading)
{
    return Regex.IsMatch(text, $@"(?im)^##\s+{Regex.Escape(heading)}\s*$");
}

static List<string> BuildReviewIssues(
    string text,
    string? taskId,
    string? detectedStatus,
    string folderStatus,
    string? detectedTeam,
    string? detectedRoadmapItem,
    IReadOnlyList<string> roadmapReferences,
    IReadOnlySet<string> roadmapIds,
    IReadOnlyList<ReviewCheck> checks,
    bool strict)
{
    var issues = new List<string>();

    foreach (var check in checks.Where(check => check.Status != "OK"))
        issues.Add($"{check.Name}: {check.Message}");

    if (string.Equals(folderStatus, "done", StringComparison.OrdinalIgnoreCase) && !HasAcceptanceCriteria(text))
        issues.Add("task is in done but has no acceptance criteria.");

    if (string.Equals(folderStatus, "pending", StringComparison.OrdinalIgnoreCase)
        && Regex.IsMatch(ExtractTaskTitle(text, taskId ?? ""), @"\b(test|temporary)\b", RegexOptions.IgnoreCase))
        issues.Add("pending task title contains test or temporary wording.");

    foreach (var roadmapReference in roadmapReferences.Where(reference => !roadmapIds.Contains(reference)))
        issues.Add($"task references unknown roadmap item: {roadmapReference}.");

    if (!string.IsNullOrWhiteSpace(detectedStatus)
        && folderStatus != "unknown"
        && !string.Equals(NormalizeTaskStatus(detectedStatus), folderStatus, StringComparison.OrdinalIgnoreCase))
        issues.Add($"task folder '{folderStatus}' does not match detected status '{detectedStatus}'.");

    if (strict && string.IsNullOrWhiteSpace(detectedRoadmapItem))
        issues.Add("strict mode requires a roadmap item or explicit R-xxx reference.");

    if (string.IsNullOrWhiteSpace(detectedTeam))
        issues.Add("team is missing.");

    return issues.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(issue => issue).ToList();
}

static string DetermineReviewOutcome(
    string text,
    string? detectedStatus,
    string folderStatus,
    IReadOnlyList<ReviewCheck> checks,
    IReadOnlyList<string> issues,
    string? detectedTeam,
    string? detectedRoadmapItem)
{
    if (Regex.IsMatch(text, @"\b(obsolete|superseded|no longer needed)\b", RegexOptions.IgnoreCase))
        return "obsolete-candidate";

    if (Regex.IsMatch(text, @"\b(blocked|waiting|dependency|cannot proceed)\b", RegexOptions.IgnoreCase))
        return "blocked";

    var missingCritical = checks.Any(check =>
        check.Status == "MISSING"
        && new[] { "acceptance criteria", "validation", "team", "implementation steps" }
            .Contains(check.Name, StringComparer.OrdinalIgnoreCase));
    if (missingCritical)
        return "needs-rework";

    var hasReviewStatus = string.Equals(detectedStatus, "review", StringComparison.OrdinalIgnoreCase)
        || string.Equals(folderStatus, "review", StringComparison.OrdinalIgnoreCase);
    var hasRequiredSignals = hasReviewStatus
        && !string.IsNullOrWhiteSpace(detectedTeam)
        && !string.IsNullOrWhiteSpace(detectedRoadmapItem)
        && checks.Where(check => check.Name is "acceptance criteria" or "validation")
            .All(check => check.Status == "OK")
        && issues.Count == 0;

    if (hasRequiredSignals)
        return "ready-for-done";

    return issues.Count > 0 ? "needs-rework" : "unknown";
}

static string NormalizeTaskStatus(string status)
{
    var normalized = status.Trim().ToLowerInvariant();
    return normalized switch
    {
        "pending" => "pending",
        "in-progress" => "in-progress",
        "in progress" => "in-progress",
        "review" => "review",
        "done" => "done",
        "blocked" => "blocked",
        "obsolete" => "obsolete",
        _ => "unknown"
    };
}

static List<string> BuildReviewRecommendations(string outcome, IReadOnlyList<string> issues)
{
    var recommendations = new List<string>();

    if (issues.Count > 0)
        recommendations.Add("Resolve reported issues before moving the task to done.");

    recommendations.Add(outcome switch
    {
        "ready-for-done" => "Confirm validation evidence and repository policy before moving to done.",
        "blocked" => "Move or keep the task in blocked only after confirming the dependency.",
        "obsolete-candidate" => "Use review or reconciliation to confirm whether the task should move to obsolete.",
        "needs-rework" => "Update the task or implementation, then run review again.",
        _ => "Add missing evidence or metadata, then run review again."
    });
    recommendations.Add("Do not move tasks automatically based only on this report.");

    return recommendations.Distinct().ToList();
}

static string BuildReviewRecommendedNextCommand(TaskReviewResult result)
{
    return result.RecommendedOutcome switch
    {
        "ready-for-done" => $"ai-platform task move --task {result.TaskId} --to done",
        "needs-rework" => $"ai-platform task move --task {result.TaskId} --to in-progress",
        "blocked" => $"ai-platform task move --task {result.TaskId} --to blocked",
        "obsolete-candidate" => $"ai-platform task move --task {result.TaskId} --to obsolete",
        _ => "No command recommended; manually inspect the report first."
    };
}

static string BuildTaskReviewReport(TaskReviewResult result)
{
    var builder = new StringBuilder();

    builder.AppendLine("# Task Review");
    builder.AppendLine();
    builder.AppendLine("## Generated at");
    builder.AppendLine();
    builder.AppendLine(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
    builder.AppendLine();
    builder.AppendLine("## Reviewed task");
    builder.AppendLine();
    builder.AppendLine($"- Task ID: {result.TaskId}");
    builder.AppendLine($"- File path: {result.DisplayPath}");
    builder.AppendLine($"- Detected status: {FormatOptionalValue(result.DetectedStatus)}");
    builder.AppendLine($"- Folder status: {result.FolderStatus}");
    builder.AppendLine($"- Detected team: {FormatOptionalValue(result.DetectedTeam)}");
    builder.AppendLine($"- Detected roadmap item: {FormatOptionalValue(result.DetectedRoadmapItem)}");
    builder.AppendLine($"- Detected title: {FormatOptionalValue(result.DetectedTitle)}");
    builder.AppendLine();
    builder.AppendLine("## Structural checks");
    builder.AppendLine();
    builder.AppendLine("| Check | Result | Notes |");
    builder.AppendLine("|---|---|---|");
    foreach (var check in result.Checks)
        builder.AppendLine($"| {EscapeTableCell(check.Name)} | {check.Status} | {EscapeTableCell(check.Message)} |");
    builder.AppendLine();
    builder.AppendLine("## Issues");
    builder.AppendLine();
    AppendBulletList(builder, result.Issues);
    builder.AppendLine();
    builder.AppendLine("## Recommended outcome");
    builder.AppendLine();
    builder.AppendLine(result.RecommendedOutcome);
    builder.AppendLine();
    builder.AppendLine("## Recommended next command");
    builder.AppendLine();
    builder.AppendLine(BuildReviewRecommendedNextCommand(result));
    builder.AppendLine();
    builder.AppendLine("## Recommendations");
    builder.AppendLine();
    AppendBulletList(builder, result.Recommendations);

    return builder.ToString();
}

static PlanOptions ParsePlanOptions(string[] args)
{
    var options = new PlanOptions();

    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        switch (arg)
        {
            case "--roadmap":
                options.RoadmapItem = ReadOptionValue(args, ref index, arg).ToUpperInvariant();
                break;
            case "--title":
                options.Title = ReadOptionValue(args, ref index, arg);
                break;
            case "--team":
                options.Team = ReadOptionValue(args, ref index, arg);
                break;
            case "--priority":
                options.Priority = ReadOptionValue(args, ref index, arg);
                break;
            case "--type":
                options.Type = ReadOptionValue(args, ref index, arg);
                break;
            case "--dry-run":
                options.DryRun = true;
                break;
            case "-h":
            case "--help":
                break;
            default:
                Console.WriteLine($"Ignoring unknown plan argument: {arg}");
                break;
        }
    }

    return options;
}

static string ReadOptionValue(string[] args, ref int index, string optionName)
{
    if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        throw new ArgumentException($"Missing value for {optionName}.");

    index++;
    return args[index];
}

static void ShowPlanHelp()
{
    Console.WriteLine("AI Platform Plan");
    Console.WriteLine("");
    Console.WriteLine("Creates one Markdown task in ai/tasks/pending.");
    Console.WriteLine("");
    Console.WriteLine("Usage:");
    Console.WriteLine("  ai-platform plan --title \"Task title\" [--roadmap R-005] [--team orchestration] [--priority medium] [--type platform] [--dry-run]");
    Console.WriteLine("");
    Console.WriteLine("Examples:");
    Console.WriteLine("  ai-platform plan --roadmap R-005 --title \"Implement roadmap-driven plan command\"");
    Console.WriteLine("  ai-platform plan --title \"Add team routing metadata to tasks\" --dry-run");
}

static string GetNextTaskId(PlatformConfig config)
{
    var paths = new[]
    {
        config.TaskPaths.Pending!,
        config.TaskPaths.InProgress!,
        config.TaskPaths.Done!
    };
    var max = 0;

    foreach (var path in paths.Where(Directory.Exists))
    {
        foreach (var file in Directory.GetFiles(path, "TASK-*.md", SearchOption.TopDirectoryOnly))
        {
            var match = Regex.Match(Path.GetFileName(file), @"^TASK-(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
                max = Math.Max(max, number);
        }
    }

    return $"TASK-{max + 1:0000}";
}

static IReadOnlyList<string> GetAllowedTaskMoveStates()
{
    return new[]
    {
        "pending",
        "in-progress",
        "review",
        "done",
        "blocked",
        "obsolete"
    };
}

static List<TaskLocation> FindTaskLocations(PlatformConfig config, string taskId)
{
    var normalizedTaskId = NormalizeTaskId(taskId);
    var taskRoots = new[]
    {
        ("pending", config.TaskPaths.Pending!),
        ("in-progress", config.TaskPaths.InProgress!),
        ("review", config.TaskPaths.Review!),
        ("done", config.TaskPaths.Done!),
        ("blocked", config.TaskPaths.Blocked!),
        ("obsolete", config.TaskPaths.Obsolete!)
    };
    var matches = new List<TaskLocation>();

    foreach (var (state, root) in taskRoots.Where(root => !string.IsNullOrWhiteSpace(root.Item2) && Directory.Exists(root.Item2)))
    {
        var candidates = Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly)
            .Where(path => string.Equals(DetectTaskId(File.ReadAllText(path), path), normalizedTaskId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileNameWithoutExtension(path), normalizedTaskId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        matches.AddRange(candidates.Select(path => new TaskLocation(
            normalizedTaskId,
            state,
            path,
            NormalizeDisplayPath(path))));
    }

    return matches;
}

static string GetTaskPathByState(PlatformConfig config, string state)
{
    return state switch
    {
        "pending" => config.TaskPaths.Pending!,
        "in-progress" => config.TaskPaths.InProgress!,
        "review" => config.TaskPaths.Review!,
        "done" => config.TaskPaths.Done!,
        "blocked" => config.TaskPaths.Blocked!,
        "obsolete" => config.TaskPaths.Obsolete!,
        _ => throw new ArgumentOutOfRangeException(nameof(state), $"Unsupported task state: {state}")
    };
}

static bool RequiresForceForTaskMove(string fromState, string toState)
{
    return !IsSafeTaskMove(fromState, toState);
}

static bool IsSafeTaskMove(string fromState, string toState)
{
    var normalizedFrom = NormalizeTaskStatus(fromState);
    var normalizedTo = NormalizeTaskStatus(toState);

    return (normalizedFrom, normalizedTo) switch
    {
        ("pending", "in-progress") => true,
        ("in-progress", "review") => true,
        ("review", "done") => true,
        ("pending", "blocked") => true,
        ("in-progress", "blocked") => true,
        ("review", "blocked") => true,
        ("pending", "obsolete") => true,
        ("blocked", "pending") => true,
        ("review", "in-progress") => true,
        _ => false
    };
}

static TaskStatusUpdateResult UpdateTaskStatusMetadata(string text, string targetState)
{
    foreach (var pattern in new[]
    {
        @"(?im)^(?<prefix>\s*-\s*\*\*Status:\*\*\s*)(?<value>.+?)\s*$",
        @"(?im)^(?<prefix>\s*Status\s*:\s*)(?<value>.+?)\s*$",
        @"(?im)^(?<prefix>\s*status\s*:\s*)(?<value>.+?)\s*$"
    })
    {
        var match = Regex.Match(text, pattern);
        if (!match.Success)
            continue;

        var originalValue = match.Groups["value"].Value.Trim().Trim('"', '\'', '`');
        var regex = new Regex(pattern);
        var updatedText = regex.Replace(text, m => $"{m.Groups["prefix"].Value}{targetState}", 1);

        return new TaskStatusUpdateResult(true, updatedText, NormalizeTaskStatus(originalValue));
    }

    return new TaskStatusUpdateResult(false, text, null);
}

static string DescribeTaskStatusUpdate(TaskStatusUpdateResult update, string fromState, string targetState, bool dryRun)
{
    if (!update.Updated)
        return "no recognized status line found";

    var previousState = string.IsNullOrWhiteSpace(update.PreviousStatus)
        ? fromState
        : update.PreviousStatus;
    return dryRun
        ? $"would update from {previousState} to {targetState}"
        : "updated";
}

static string GetTaskMoveNextStep(string targetState, string taskId)
{
    return targetState switch
    {
        "review" => $"Next step: run `ai-platform review --task {taskId}`.",
        "in-progress" => "Next step: continue the implementation workflow for this task.",
        "blocked" => "Next step: document the blocking dependency or decision clearly before resuming.",
        "obsolete" => "Next step: keep the reason for obsolescence visible in the task history.",
        "done" => "Next step: keep roadmap/current-state updated if this task completed a roadmap item.",
        _ => "Next step: review the task state and continue the workflow deliberately."
    };
}

static List<TaskFileInfo> ReadTaskFiles(PlatformConfig config)
{
    var taskRoots = new[]
    {
        ("pending", config.TaskPaths.Pending!),
        ("in-progress", config.TaskPaths.InProgress!),
        ("done", config.TaskPaths.Done!)
    };
    var tasks = new List<TaskFileInfo>();

    foreach (var (state, path) in taskRoots)
    {
        if (!Directory.Exists(path))
            continue;

        foreach (var file in Directory.GetFiles(path, "*.md", SearchOption.TopDirectoryOnly).OrderBy(file => file))
        {
            var text = File.ReadAllText(file);
            var references = Regex.Matches(text, @"\bR-\d{3}\b", RegexOptions.IgnoreCase)
                .Select(match => match.Value.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();

            tasks.Add(new TaskFileInfo(
                state,
                NormalizeDisplayPath(file),
                ExtractTaskTitle(text, file),
                references,
                HasTaskMetadata(text, "team"),
                HasAcceptanceCriteria(text),
                HasSuspiciousTaskTitle(text, file)));
        }
    }

    return tasks;
}

static string ExtractTaskTitle(string text, string path)
{
    var frontMatterTitle = Regex.Match(text, @"(?im)^\s*title\s*:\s*[""']?(.+?)[""']?\s*$");
    if (frontMatterTitle.Success)
        return frontMatterTitle.Groups[1].Value.Trim();

    var heading = Regex.Match(text, @"(?m)^#\s+(.+?)\s*$");
    if (heading.Success)
        return heading.Groups[1].Value.Trim();

    return Path.GetFileNameWithoutExtension(path);
}

static bool HasTaskMetadata(string text, string metadataName)
{
    return Regex.IsMatch(text, $@"(?im)^\s*{Regex.Escape(metadataName)}\s*:");
}

static bool HasAcceptanceCriteria(string text)
{
    return Regex.IsMatch(text, @"(?im)^##\s+Acceptance criteria\s*$");
}

static bool HasSuspiciousTaskTitle(string text, string path)
{
    var title = ExtractTaskTitle(text, path);
    return Regex.IsMatch(title, @"\b(test|temporary)\b", RegexOptions.IgnoreCase);
}

static bool HasWeakPendingMetadata(TaskFileInfo task)
{
    return task.RoadmapReferences.Count == 0
        || !task.HasTeam
        || !task.HasAcceptanceCriteria
        || task.HasSuspiciousTitle;
}

static List<string> ReadKnownGapSignals(string knownGapsPath)
{
    var text = ReadTextIfExists(knownGapsPath);
    if (text is null)
        return new List<string>();

    var keywords = new[]
    {
        "reconcile",
        "implement",
        "review",
        "multi-task planning",
        "routing automatico",
        "routing automático",
        "ejecucion multi-equipo",
        "ejecución multi-equipo",
        "multi-team execution"
    };

    return text.Split('\n')
        .Select(line => line.Trim())
        .Where(line => line.StartsWith("-", StringComparison.Ordinal))
        .Select(line => line.TrimStart('-').Trim())
        .Where(line => keywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(line => line)
        .ToList();
}

static string BuildReconciliationReport(
    IReadOnlyList<TaskDirectorySummary> taskSummaries,
    IReadOnlyList<RoadmapItem> roadmapItems,
    IReadOnlySet<string> referencedRoadmapIds,
    IReadOnlyList<string> roadmapItemsWithoutTaskReferences,
    IReadOnlyList<TaskReferenceIssue> tasksReferencingUnknownRoadmapItems,
    IReadOnlyList<TaskFileInfo> doneTasksWithoutRoadmapReference,
    IReadOnlyList<TaskFileInfo> pendingTasksWithoutRoadmapReference,
    IReadOnlyList<TaskFileInfo> weakPendingTasks,
    string knownGapsPath,
    IReadOnlyList<string> knownGapSignals)
{
    var builder = new StringBuilder();

    builder.AppendLine("# Task Reconciliation");
    builder.AppendLine();
    builder.AppendLine("## Generated at");
    builder.AppendLine();
    builder.AppendLine(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
    builder.AppendLine();
    builder.AppendLine("## Task summary");
    builder.AppendLine();
    foreach (var summary in taskSummaries)
    {
        var value = summary.Exists ? $"{summary.MarkdownTaskCount} Markdown task(s)" : $"missing path ({summary.Path})";
        builder.AppendLine($"- {summary.Label}: {value}");
    }

    builder.AppendLine();
    builder.AppendLine("## Roadmap coverage");
    builder.AppendLine();
    builder.AppendLine($"- Total roadmap items: {roadmapItems.Count}");
    builder.AppendLine($"- Roadmap items referenced by tasks: {referencedRoadmapIds.Count}");
    builder.AppendLine($"- Roadmap items with no task references: {roadmapItemsWithoutTaskReferences.Count}");
    AppendBulletList(builder, roadmapItemsWithoutTaskReferences);

    builder.AppendLine();
    builder.AppendLine("## Task reference issues");
    builder.AppendLine();
    builder.AppendLine($"- Tasks referencing unknown roadmap items: {tasksReferencingUnknownRoadmapItems.Count}");
    foreach (var issue in tasksReferencingUnknownRoadmapItems)
        builder.AppendLine($"  - {issue.TaskPath}: {issue.RoadmapItem}");
    builder.AppendLine($"- Done tasks without roadmap reference: {doneTasksWithoutRoadmapReference.Count}");
    AppendTaskList(builder, doneTasksWithoutRoadmapReference);
    builder.AppendLine($"- Pending tasks without roadmap reference: {pendingTasksWithoutRoadmapReference.Count}");
    AppendTaskList(builder, pendingTasksWithoutRoadmapReference);

    builder.AppendLine();
    builder.AppendLine("## Stale or weak task candidates");
    builder.AppendLine();
    builder.AppendLine($"- Pending tasks with weak metadata: {weakPendingTasks.Count}");
    foreach (var task in weakPendingTasks)
    {
        var reasons = new List<string>();
        if (task.RoadmapReferences.Count == 0)
            reasons.Add("missing roadmap reference");
        if (!task.HasTeam)
            reasons.Add("missing team");
        if (!task.HasAcceptanceCriteria)
            reasons.Add("missing acceptance criteria");
        if (task.HasSuspiciousTitle)
            reasons.Add("temporary/test wording");
        builder.AppendLine($"  - {task.DisplayPath}: {string.Join(", ", reasons)}");
    }
    builder.AppendLine($"- Pending tasks missing team: {weakPendingTasks.Count(task => !task.HasTeam)}");
    builder.AppendLine($"- Pending tasks missing acceptance criteria: {weakPendingTasks.Count(task => !task.HasAcceptanceCriteria)}");
    builder.AppendLine($"- Pending tasks with suspicious temporary/test wording: {weakPendingTasks.Count(task => task.HasSuspiciousTitle)}");

    builder.AppendLine();
    builder.AppendLine("## Known gaps alignment");
    builder.AppendLine();
    builder.AppendLine($"- {knownGapsPath}: {FormatFound(File.Exists(knownGapsPath))}");
    builder.AppendLine($"- Relevant gaps detected: {knownGapSignals.Count}");
    AppendBulletList(builder, knownGapSignals);
    builder.AppendLine("- `reconcile` v1 does not resolve gaps automatically; keep this file aligned after each phase.");

    builder.AppendLine();
    builder.AppendLine("## Recommendations");
    builder.AppendLine();
    builder.AppendLine("- Review stale pending candidates before implementation.");
    builder.AppendLine("- Use `ai-platform plan` to create missing tasks for roadmap items.");
    builder.AppendLine("- Do not move tasks to done without review.");
    builder.AppendLine("- Implement review before automating task closure.");
    builder.AppendLine("- Keep roadmap/current-state/known-gaps aligned after each phase.");

    return builder.ToString();
}

static void AppendBulletList(StringBuilder builder, IReadOnlyList<string> items)
{
    if (items.Count == 0)
    {
        builder.AppendLine("  - none");
        return;
    }

    foreach (var item in items)
        builder.AppendLine($"  - {item}");
}

static void AppendTaskList(StringBuilder builder, IReadOnlyList<TaskFileInfo> tasks)
{
    if (tasks.Count == 0)
    {
        builder.AppendLine("  - none");
        return;
    }

    foreach (var task in tasks)
        builder.AppendLine($"  - {task.DisplayPath}");
}

static string InferTeam(string? roadmapItem)
{
    return roadmapItem?.ToUpperInvariant() switch
    {
        "R-005" => "orchestration",
        "R-006" => "orchestration",
        "R-007" => "platform",
        "R-008" => "qa",
        "R-009" => "orchestration",
        "R-010" => "platform",
        _ => "platform"
    };
}

static string BuildTaskContent(
    string taskId,
    string title,
    string? roadmapItem,
    string team,
    string priority,
    string type)
{
    var goal = $"Create the smallest safe increment for: {title}.";
    var context = string.IsNullOrWhiteSpace(roadmapItem)
        ? "This task was generated from an explicit planning request."
        : $"This task was generated from roadmap item {roadmapItem}.";

    return $"""
---
id: {taskId}
title: "{EscapeYamlValue(title)}"
status: pending
type: {EscapeYamlValue(type)}
team: {EscapeYamlValue(team)}
priority: {EscapeYamlValue(priority)}
roadmap_item: {FormatYamlOptionalValue(roadmapItem)}
created_by: ai-platform plan
---

# {taskId} - {title}

## Goal

{goal}

## Context

{context}

## Files to read first

- README.md
- AGENTS.md
- ai/roadmap.md
- ai/current-state.md
- ai/commands/plan.md

## Expected files to modify

- TBD by implementer

## Implementation steps

1. Review the files listed above.
2. Confirm the requested scope.
3. Implement the smallest safe increment.
4. Update documentation if behavior changes.
5. Validate the change.
6. Do not move this task to done without review.

## Acceptance criteria

- The requested change is implemented or the task is refined with clear blockers.
- Documentation is updated if behavior changes.
- Validation commands pass.
- No build artifacts are committed.

## Validation

- Run repository-relevant validation.
- For CLI changes, run `dotnet build ai-platform-cli/ai-platform-cli.csproj`.

## Commit and push

At the end:
1. Run `git status`.
2. Stage the intended files.
3. Commit with a clear message.
4. Push the branch to the remote.
""";
}

static string EscapeYamlValue(string value)
{
    return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

static string FormatYamlOptionalValue(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? "\"\"" : EscapeYamlValue(value);
}

static string FormatOptionalValue(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? "none" : value;
}

static string NormalizeDisplayPath(string path)
{
    return path.Replace(Path.DirectorySeparatorChar, '/');
}

static List<RoadmapItem> ParseRoadmapItems(string? roadmapText)
{
    var items = new List<RoadmapItem>();
    if (string.IsNullOrWhiteSpace(roadmapText))
        return items;

    var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var lines = roadmapText.Split('\n');

    foreach (var line in lines)
    {
        var tableMatch = Regex.Match(line, @"^\|\s*(R-\d{3})\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|", RegexOptions.IgnoreCase);
        if (!tableMatch.Success)
            continue;

        var id = tableMatch.Groups[1].Value.Trim();
        if (!seenIds.Add(id))
            continue;

        items.Add(new RoadmapItem(
            id,
            CleanMarkdownCell(tableMatch.Groups[2].Value),
            NormalizeRoadmapStatus(tableMatch.Groups[3].Value)));
    }

    var headingMatches = Regex.Matches(roadmapText, @"(?m)^#{1,6}\s+(R-\d{3})(?:\s*[-:]\s*(.+?))?\s*$", RegexOptions.IgnoreCase);
    for (var index = 0; index < headingMatches.Count; index++)
    {
        var match = headingMatches[index];
        var id = match.Groups[1].Value.Trim();
        if (!seenIds.Add(id))
            continue;

        var title = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "Unknown";
        var blockStart = match.Index + match.Length;
        var blockEnd = index + 1 < headingMatches.Count ? headingMatches[index + 1].Index : roadmapText.Length;
        var block = roadmapText[blockStart..blockEnd];
        var statusMatch = Regex.Match(block, @"(?im)^\s*(?:-\s*)?(?:\*\*)?Status(?:\*\*)?\s*:?\s*`?([A-Za-z-]+)");
        var status = statusMatch.Success ? NormalizeRoadmapStatus(statusMatch.Groups[1].Value) : "unknown";

        items.Add(new RoadmapItem(id, string.IsNullOrWhiteSpace(title) ? "Unknown" : title, status));
    }

    return items.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase).ToList();
}

static string CleanMarkdownCell(string value)
{
    return value.Trim().Trim('`').Trim();
}

static string NormalizeRoadmapStatus(string value)
{
    var normalized = CleanMarkdownCell(value).ToLowerInvariant();
    return normalized switch
    {
        "done" => "done",
        "in-progress" => "in-progress",
        "in progress" => "in-progress",
        "planned" => "planned",
        "blocked" => "blocked",
        "deferred" => "deferred",
        _ => "unknown"
    };
}

static Dictionary<string, int> CountRoadmapItemsByStatus(IReadOnlyList<RoadmapItem> items)
{
    var result = new[] { "done", "in-progress", "planned", "blocked", "deferred", "unknown" }
        .ToDictionary(status => status, _ => 0);

    foreach (var item in items)
        result[item.Status] = result.TryGetValue(item.Status, out var count) ? count + 1 : 1;

    return result;
}

static string BuildRoadmapStatusReport(
    bool roadmapExists,
    IReadOnlyList<RoadmapItem> items,
    IReadOnlyDictionary<string, int> counts)
{
    var builder = new StringBuilder();

    builder.AppendLine("# Roadmap Status");
    builder.AppendLine();
    builder.AppendLine("## Generated at");
    builder.AppendLine();
    builder.AppendLine(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
    builder.AppendLine();
    builder.AppendLine("## Summary");
    builder.AppendLine();
    builder.AppendLine($"- Total roadmap items: {items.Count}");
    builder.AppendLine($"- Done: {counts["done"]}");
    builder.AppendLine($"- In-progress: {counts["in-progress"]}");
    builder.AppendLine($"- Planned: {counts["planned"]}");
    builder.AppendLine($"- Blocked: {counts["blocked"]}");
    builder.AppendLine($"- Deferred: {counts["deferred"]}");
    builder.AppendLine($"- Unknown: {counts["unknown"]}");
    builder.AppendLine();
    builder.AppendLine("## Items");
    builder.AppendLine();
    builder.AppendLine("| ID | Title | Status |");
    builder.AppendLine("|---|---|---|");

    foreach (var item in items)
        builder.AppendLine($"| {item.Id} | {EscapeTableCell(item.Title)} | {item.Status} |");

    if (items.Count == 0)
        builder.AppendLine("| none | none | unknown |");

    builder.AppendLine();
    builder.AppendLine("## Status by phase");
    builder.AppendLine();
    builder.AppendLine("| ID | Status |");
    builder.AppendLine("|---|---|");
    foreach (var item in items)
        builder.AppendLine($"| {item.Id} | {item.Status} |");

    if (items.Count == 0)
        builder.AppendLine("| none | unknown |");

    builder.AppendLine();
    builder.AppendLine("## Observations");
    builder.AppendLine();
    if (!roadmapExists)
        builder.AppendLine("- `ai/roadmap.md` is missing.");
    if (roadmapExists && items.Count == 0)
        builder.AppendLine("- `ai/roadmap.md` exists but no roadmap items were detected.");
    if (counts["unknown"] > 0)
        builder.AppendLine("- Some roadmap items have unknown status.");
    if (items.Count > 0 && counts.Any(count => count.Value == items.Count))
        builder.AppendLine("- All detected roadmap items currently share the same status.");
    builder.AppendLine("- This command does not semantically validate whether code implements each roadmap item yet.");
    builder.AppendLine("- Use `reconcile` later to compare roadmap, tasks, and code evidence.");
    builder.AppendLine();
    builder.AppendLine("## Next steps");
    builder.AppendLine();
    builder.AppendLine("- Keep `ai/roadmap.md` updated after each phase.");
    builder.AppendLine("- Use `ai-platform analyze` for broader project state.");
    builder.AppendLine("- Implement `reconcile` later to compare roadmap, tasks, and code evidence.");

    return builder.ToString();
}

static string EscapeTableCell(string value)
{
    return value.Replace("|", "\\|").Trim();
}

static TaskDirectorySummary SummarizeTaskDirectory(string label, string path)
{
    if (!Directory.Exists(path))
        return new TaskDirectorySummary(label, path, false, 0);

    var count = Directory.GetFiles(path, "*.md", SearchOption.TopDirectoryOnly).Length;
    return new TaskDirectorySummary(label, path, true, count);
}

static string? ReadTextIfExists(string path)
{
    return File.Exists(path) ? File.ReadAllText(path) : null;
}

static Dictionary<string, int> CountRoadmapStates(string? roadmapText)
{
    var states = new[] { "done", "in-progress", "planned", "blocked", "deferred" };
    var result = states.ToDictionary(state => state, _ => 0);

    if (roadmapText is null)
        return result;

    foreach (var state in states)
        result[state] = Regex.Matches(roadmapText, $@"\b{Regex.Escape(state)}\b", RegexOptions.IgnoreCase).Count;

    return result;
}

static OptionalConfigValues ReadOptionalConfigValues(string rootPath)
{
    var configPath = Path.Combine(rootPath, "ai-platform.json");
    if (!File.Exists(configPath))
        return new OptionalConfigValues("not configured", "not configured");

    try
    {
        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        var root = document.RootElement;
        return new OptionalConfigValues(
            ReadJsonPropertySummary(root, "managedArtifacts"),
            ReadJsonPropertySummary(root, "templateSourceZip", "templateSource"));
    }
    catch
    {
        return new OptionalConfigValues("unavailable (invalid config)", "unavailable (invalid config)");
    }
}

static string ReadJsonPropertySummary(JsonElement root, params string[] propertyNames)
{
    JsonElement property = default;
    var found = false;

    foreach (var propertyName in propertyNames)
    {
        if (root.TryGetProperty(propertyName, out property))
        {
            found = true;
            break;
        }
    }

    if (!found)
        return "not configured";

    return property.ValueKind switch
    {
        JsonValueKind.Array => $"{property.GetArrayLength()} item(s)",
        JsonValueKind.Object => "configured",
        JsonValueKind.String => property.GetString() ?? "configured",
        JsonValueKind.Null => "not configured",
        _ => property.ToString()
    };
}

static string BuildAnalysisReport(
    PlatformConfigLoadResult configResult,
    PlatformConfig config,
    OptionalConfigValues optionalConfig,
    IReadOnlyList<TaskDirectorySummary> taskSummaries,
    int roadmapIds,
    IReadOnlyDictionary<string, int> roadmapStates,
    IReadOnlyList<string> teamDocs,
    IReadOnlyList<string> commandSpecs,
    string risksPath,
    string knownGapsPath)
{
    var builder = new StringBuilder();

    builder.AppendLine("# Project Analysis");
    builder.AppendLine();
    builder.AppendLine("## Generated at");
    builder.AppendLine();
    builder.AppendLine(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
    builder.AppendLine();
    builder.AppendLine("## Platform configuration");
    builder.AppendLine();
    builder.AppendLine($"- Platform version: {config.PlatformVersion}");
    builder.AppendLine($"- Config status: {configResult.Status}");
    builder.AppendLine($"- Task paths:");
    builder.AppendLine($"  - pending: {config.TaskPaths.Pending}");
    builder.AppendLine($"  - in-progress: {config.TaskPaths.InProgress}");
    builder.AppendLine($"  - done: {config.TaskPaths.Done}");
    builder.AppendLine($"- Worker lock file: {config.Worker.LockFile}");
    builder.AppendLine($"- Managed artifacts: {optionalConfig.ManagedArtifacts}");
    builder.AppendLine($"- Template source: {optionalConfig.TemplateSource}");
    builder.AppendLine();
    builder.AppendLine("## Core files");
    builder.AppendLine();

    foreach (var path in new[]
    {
        "ai-platform.json",
        "AGENTS.md",
        "README.md",
        "ai/roadmap.md",
        "ai/current-state.md",
        "ai/teams/README.md",
        "ai/commands/README.md"
    })
    {
        builder.AppendLine($"- {path}: {FormatFound(File.Exists(path))}");
    }

    builder.AppendLine();
    builder.AppendLine("## Document signals");
    builder.AppendLine();
    foreach (var path in new[]
    {
        "ai/roadmap.md",
        "ai/current-state.md",
        "ai/project-memory/decisions.md",
        "ai/project-memory/risks.md",
        "ai/project-memory/known-gaps.md",
        "ai/teams/README.md",
        "ai/commands/README.md"
    })
    {
        AppendDocumentSignal(builder, path);
    }

    builder.AppendLine();
    builder.AppendLine("## Task summary");
    builder.AppendLine();

    foreach (var summary in taskSummaries)
    {
        var status = summary.Exists ? $"{summary.MarkdownTaskCount} Markdown task(s)" : $"missing path ({summary.Path})";
        builder.AppendLine($"- {summary.Label}: {status}");
    }

    builder.AppendLine();
    builder.AppendLine("## Roadmap summary");
    builder.AppendLine();
    builder.AppendLine($"- Roadmap IDs found: {roadmapIds}");
    foreach (var item in roadmapStates)
        builder.AppendLine($"- `{item.Key}` occurrences: {item.Value}");

    builder.AppendLine();
    builder.AppendLine("## Team model summary");
    builder.AppendLine();
    builder.AppendLine($"- `ai/teams/` exists: {FormatFound(Directory.Exists("ai/teams"))}");
    builder.AppendLine($"- Team README files found: {teamDocs.Count}");
    builder.AppendLine($"- Teams detected: {FormatList(teamDocs)}");
    builder.AppendLine();
    builder.AppendLine("## Command specs summary");
    builder.AppendLine();
    builder.AppendLine($"- `ai/commands/` exists: {FormatFound(Directory.Exists("ai/commands"))}");
    builder.AppendLine($"- Command specs found: {commandSpecs.Count}");
    builder.AppendLine($"- Specs detected: {FormatList(commandSpecs)}");
    builder.AppendLine();
    builder.AppendLine("## Known gaps and risks");
    builder.AppendLine();
    AppendDocumentSignal(builder, knownGapsPath);
    AppendDocumentSignal(builder, risksPath);
    builder.AppendLine();
    builder.AppendLine("## Recommendations");
    builder.AppendLine();
    builder.AppendLine("- Run `ai-platform roadmap-status` once implemented.");
    builder.AppendLine("- Keep `ai/current-state.md` updated after major changes.");
    builder.AppendLine("- Review pending tasks before running implementation automation.");
    builder.AppendLine("- Implement command specs incrementally.");

    return builder.ToString();
}

static void AppendDocumentSignal(StringBuilder builder, string path)
{
    var text = ReadTextIfExists(path);
    if (text is null)
    {
        builder.AppendLine($"- {path}: missing");
        return;
    }

    var lineCount = text.Split('\n').Length;
    var headingCount = Regex.Matches(text, @"^#+\s", RegexOptions.Multiline).Count;
    builder.AppendLine($"- {path}: found ({lineCount} line(s), {headingCount} heading(s))");
}

static string FormatFound(bool exists)
{
    return exists ? "found" : "missing";
}

static RefreshSourceInfo ResolveStatusRefreshSource(PlatformConfig config)
{
    if (!string.IsNullOrWhiteSpace(config.TemplateSourceZip))
        return new RefreshSourceInfo(config.TemplateSourceZip, "ai-platform.json");

    var envSource = Environment.GetEnvironmentVariable("AI_PLATFORM_TEMPLATE_ZIP");
    if (!string.IsNullOrWhiteSpace(envSource))
        return new RefreshSourceInfo(envSource, "AI_PLATFORM_TEMPLATE_ZIP");

    return new RefreshSourceInfo(GetBuiltInTemplateZipSource(), "built-in default");
}

static string FormatManagedArtifacts(IReadOnlyList<string> managedArtifacts)
{
    return managedArtifacts.Count == 0 ? "not configured" : string.Join(", ", managedArtifacts);
}

static void PrintStatusCheck(string label, bool exists)
{
    var status = exists ? "OK" : "MISSING";
    Console.WriteLine($"  [{status}] {label}");
}

static string FormatList(IReadOnlyList<string> items)
{
    return items.Count == 0 ? "none" : string.Join(", ", items);
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
    Console.WriteLine("  ai-platform status           Show quick operational platform status");
    Console.WriteLine("  ai-platform refresh          Refresh managed artifacts from a compatible template ZIP");
    Console.WriteLine("  ai-platform git-ignore       Add or update the managed local-tooling .gitignore block");
    Console.WriteLine("  ai-platform analyze          Generate read-only project analysis report");
    Console.WriteLine("  ai-platform roadmap-status   Generate read-only roadmap status report");
    Console.WriteLine("  ai-platform reconcile        Generate read-only task reconciliation report");
    Console.WriteLine("  ai-platform review           Generate read-only task review report");
    Console.WriteLine("  ai-platform implement        Prepare a pending task for implementation");
    Console.WriteLine("  ai-platform task move        Move a task between lifecycle states");
    Console.WriteLine("  ai-platform codex-exec       Start a single non-interactive Codex exec run");
    Console.WriteLine("  ai-platform watch            Start the local task watcher");
    Console.WriteLine("  ai-platform update           Update platform-managed artifacts");
    Console.WriteLine("  ai-platform run              Start the polling worker");
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
    public string? InstallMode { get; set; }
    public string? TemplateSourceZip { get; set; }
    public List<string> ManagedArtifacts { get; set; } = new();
    public List<string> RequiredTemplatePaths { get; set; } = new();
    public TaskPathConfig TaskPaths { get; set; } = new();
    public WorkerConfig Worker { get; set; } = new();

    public static PlatformConfig CreateDefault()
    {
        return new PlatformConfig
        {
            PlatformVersion = "1.0",
            InstallMode = "template-source",
            TemplateSourceZip = "https://github.com/devRaGonSa/ai-dev-platform-template/archive/refs/heads/main.zip",
            ManagedArtifacts = new List<string>
            {
                "AGENTS.md",
                "ai-platform.json",
                "scripts/codex-runner.ps1",
                "scripts/update-platform.ps1",
                "scripts/run-integration-tests.ps1",
                ".github/workflows/codex-worker.yml",
                "ai/task-template.md"
            },
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
                Review = "ai/tasks/review",
                Blocked = "ai/tasks/blocked",
                Obsolete = "ai/tasks/obsolete",
                Done = "ai/tasks/done"
            },
            Worker = new WorkerConfig
            {
                LockFile = "ai/worker.lock",
                PollIntervalSeconds = 30
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

        if (string.IsNullOrWhiteSpace(normalized.TemplateSourceZip))
        {
            normalized.TemplateSourceZip = defaults.TemplateSourceZip;
            fallbackKeys.Add("templateSourceZip");
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

            if (normalized.TaskPaths.Review is null)
            {
                normalized.TaskPaths.Review = defaults.TaskPaths.Review;
                fallbackKeys.Add("taskPaths.review");
            }

            if (normalized.TaskPaths.Blocked is null)
            {
                normalized.TaskPaths.Blocked = defaults.TaskPaths.Blocked;
                fallbackKeys.Add("taskPaths.blocked");
            }

            if (normalized.TaskPaths.Obsolete is null)
            {
                normalized.TaskPaths.Obsolete = defaults.TaskPaths.Obsolete;
                fallbackKeys.Add("taskPaths.obsolete");
            }
        }

        if (normalized.Worker is null)
        {
            normalized.Worker = defaults.Worker;
            fallbackKeys.Add("worker");
        }
        else
        {
            if (normalized.Worker.LockFile is null)
            {
                normalized.Worker.LockFile = defaults.Worker.LockFile;
                fallbackKeys.Add("worker.lockFile");
            }

            if (normalized.Worker.PollIntervalSeconds <= 0)
            {
                normalized.Worker.PollIntervalSeconds = defaults.Worker.PollIntervalSeconds;
                fallbackKeys.Add("worker.pollIntervalSeconds");
            }
        }

        normalized.PlatformVersion ??= defaults.PlatformVersion;
        if (config?.PlatformVersion is null)
            fallbackKeys.Add("platformVersion");

        normalized.InstallMode ??= defaults.InstallMode;
        if (config?.InstallMode is null)
            fallbackKeys.Add("installMode");

        return normalized;
    }

    public static List<string> GetAllFallbackKeys()
    {
        return new List<string>
        {
            "platformVersion",
            "installMode",
            "templateSourceZip",
            "managedArtifacts",
            "requiredTemplatePaths",
            "taskPaths.pending",
            "taskPaths.inProgress",
            "taskPaths.review",
            "taskPaths.blocked",
            "taskPaths.obsolete",
            "taskPaths.done",
            "worker.lockFile",
            "worker.pollIntervalSeconds"
        };
    }
}

sealed class TaskPathConfig
{
    public string? Pending { get; set; } = "ai/tasks/pending";
    public string? InProgress { get; set; } = "ai/tasks/in-progress";
    public string? Review { get; set; } = "ai/tasks/review";
    public string? Blocked { get; set; } = "ai/tasks/blocked";
    public string? Obsolete { get; set; } = "ai/tasks/obsolete";
    public string? Done { get; set; } = "ai/tasks/done";
}

sealed class WorkerConfig
{
    public string? LockFile { get; set; } = "ai/worker.lock";
    public int PollIntervalSeconds { get; set; } = 30;
}

sealed record TaskDirectorySummary(string Label, string Path, bool Exists, int MarkdownTaskCount);

sealed record OptionalConfigValues(string ManagedArtifacts, string TemplateSource);

sealed record RefreshSourceInfo(string Source, string Selection);

sealed record RefreshComparisonResult(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Updated,
    IReadOnlyList<string> Unchanged,
    IReadOnlyList<string> MissingInSource);

sealed record RoadmapItem(string Id, string Title, string Status);

sealed record TaskFileInfo(
    string State,
    string DisplayPath,
    string Title,
    IReadOnlyList<string> RoadmapReferences,
    bool HasTeam,
    bool HasAcceptanceCriteria,
    bool HasSuspiciousTitle);

sealed record TaskReferenceIssue(string TaskPath, string RoadmapItem);

sealed record ReviewCheck(string Name, string Status, string Message);

sealed record TaskReviewResult(
    string TaskId,
    string DisplayPath,
    string? DetectedStatus,
    string FolderStatus,
    string? DetectedTeam,
    string? DetectedType,
    string? DetectedPriority,
    string? DetectedRoadmapItem,
    string? DetectedTitle,
    IReadOnlyList<ReviewCheck> Checks,
    IReadOnlyList<string> Issues,
    string RecommendedOutcome,
    IReadOnlyList<string> Recommendations);

sealed record PendingTaskSelection(string PendingPath, string DisplayPath);

sealed record ImplementationTaskInfo(
    string TaskId,
    string Title,
    string? Team,
    string? RoadmapItem,
    bool HasTaskId,
    bool HasTitle,
    bool HasTeam,
    bool HasRoadmapItemOrJustification,
    bool HasAcceptanceCriteria,
    bool HasValidation,
    bool HasCommitAndPush);

sealed record TaskLocation(
    string TaskId,
    string State,
    string Path,
    string DisplayPath);

sealed record TaskStatusUpdateResult(
    bool Updated,
    string UpdatedText,
    string? PreviousStatus);

sealed class ImplementOptions
{
    public string? TaskId { get; set; }
    public bool DryRun { get; set; }
    public bool NoMove { get; set; }
    public bool ShowHelp { get; set; }
}

sealed class TaskMoveOptions
{
    public string? TaskId { get; set; }
    public string? TargetState { get; set; }
    public bool DryRun { get; set; }
    public bool Force { get; set; }
    public bool ShowHelp { get; set; }
}

sealed class ReviewOptions
{
    public string? TaskId { get; set; }
    public string? FilePath { get; set; }
    public bool Strict { get; set; }
}

sealed class PlanOptions
{
    public string? RoadmapItem { get; set; }
    public string? Title { get; set; }
    public string? Team { get; set; }
    public string? Priority { get; set; } = "medium";
    public string? Type { get; set; } = "platform";
    public bool DryRun { get; set; }
}

sealed class RefreshOptions
{
    public bool Apply { get; set; }
    public string? Source { get; set; }
    public bool ShowHelp { get; set; }
}

sealed class GitIgnoreOptions
{
    public bool DryRun { get; set; }
    public bool ShowHelp { get; set; }
}

sealed record GitIgnoreUpdate(string Action, string UpdatedContent);

sealed class InstallSummary
{
    public List<string> Created { get; } = new();
    public List<string> Skipped { get; } = new();
}

sealed class RefreshCommandException : Exception
{
    public RefreshCommandException(string message)
        : base(message)
    {
    }
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
