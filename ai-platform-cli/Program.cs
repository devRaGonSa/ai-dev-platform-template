using System.Diagnostics;
using System.IO.Compression;
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

    case "run":
        RunScript("scripts/codex-runner.ps1");
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
            ReadJsonPropertySummary(root, "templateSource"));
    }
    catch
    {
        return new OptionalConfigValues("unavailable (invalid config)", "unavailable (invalid config)");
    }
}

static string ReadJsonPropertySummary(JsonElement root, string propertyName)
{
    if (!root.TryGetProperty(propertyName, out var property))
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
    Console.WriteLine("  ai-platform analyze          Generate read-only project analysis report");
    Console.WriteLine("  ai-platform roadmap-status   Generate read-only roadmap status report");
    Console.WriteLine("  ai-platform reconcile        Generate read-only task reconciliation report");
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

sealed record TaskDirectorySummary(string Label, string Path, bool Exists, int MarkdownTaskCount);

sealed record OptionalConfigValues(string ManagedArtifacts, string TemplateSource);

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

sealed class PlanOptions
{
    public string? RoadmapItem { get; set; }
    public string? Title { get; set; }
    public string? Team { get; set; }
    public string? Priority { get; set; } = "medium";
    public string? Type { get; set; } = "platform";
    public bool DryRun { get; set; }
}

sealed class InstallSummary
{
    public List<string> Created { get; } = new();
    public List<string> Skipped { get; } = new();
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
