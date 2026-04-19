using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpellCards.Tests;

public sealed class PdfGenerationE2ETests
{
    [Theory]
    [InlineData("3.5")]
    [InlineData("5e")]
    [InlineData("5.5")]
    [InlineData("pf1")]
    [InlineData("pf2")]
    public async Task GeneratesTimestampedPdfForEachSupportedRuleSet(string ruleSet)
    {
        var spells = BuildCustomSpells(ruleSet);

        using var sandbox = await CreateSandboxAsync(
            $"ruleset: {ruleSet}",
            spells[0].Name,
            spells[1].Name);

        await sandbox.WriteCustomSpellsAsync(SerializeCustomSpells(spells));

        var result = await sandbox.RunAsync();

        Assert.True(
            result.ExitCode == 0,
            $"SpellCards exited with code {result.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{result.StandardError}");
        Assert.Contains("Generated:", result.StandardOutput);
        AssertGeneratedTimestampedPdf(sandbox.DirectoryPath, result.StandardOutput);
    }

    private static void AssertGeneratedTimestampedPdf(string sandboxPath, string standardOutput)
    {
        var match = Regex.Match(standardOutput, @"spellcards_\d{8}_\d{6}\.pdf", RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"Expected timestamped PDF name in output. Output:{Environment.NewLine}{standardOutput}");

        var outDir = Path.Combine(sandboxPath, "out");
        var generatedFiles = Directory.GetFiles(outDir, "spellcards_*.pdf", SearchOption.TopDirectoryOnly);
        Assert.Single(generatedFiles);
        Assert.EndsWith(match.Value, generatedFiles[0], StringComparison.OrdinalIgnoreCase);

        var info = new FileInfo(generatedFiles[0]);
        Assert.True(info.Exists && info.Length > 0, "Expected generated PDF to exist and be non-empty.");
    }

    private static string SerializeCustomSpells(IReadOnlyList<CustomSpellDefinition> spells)
    {
        return JsonSerializer.Serialize(
            spells,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            });
    }

    private static IReadOnlyList<CustomSpellDefinition> BuildCustomSpells(string ruleSet)
    {
        return ruleSet switch
        {
            "3.5" =>
            [
                new CustomSpellDefinition(
                    ruleSet,
                    "Emergency Summon: Tiny Lawyer",
                    "Wizard 1",
                    "Conjuration",
                    "conjuration",
                    "1 standard action",
                    "Close",
                    "Effect: one summoned Tiny outsider",
                    "1 round/level",
                    "None",
                    "No",
                    "V S",
                    "Custom - Silly",
                    "You conjure a Tiny extraplanar lawyer carrying a briefcase and far too many forms. Once per round it may object loudly, imposing a -1 penalty on one target's next attack roll, saving throw, or skill check.",
                    "The lawyer speaks Common and Passive-Aggressive."),
                new CustomSpellDefinition(
                    ruleSet,
                    "Arcane Coffee",
                    "Wizard 0",
                    "Transmutation",
                    "transmutation",
                    "1 standard action",
                    "Touch",
                    "Target: one mug of liquid",
                    "Instantaneous",
                    "None",
                    "No",
                    "V S",
                    "Custom - Caffeinated",
                    "You transform one mundane beverage into intensely energizing coffee. The first creature to drink it gains a +1 morale bonus on the next initiative check it makes within 1 hour.",
                    "The coffee is always slightly too hot.")
            ],
            "5e" =>
            [
                new CustomSpellDefinition(
                    ruleSet,
                    "Blessing of Unicorn",
                    "Bard 2",
                    "Transmutation",
                    "transmutation",
                    "1 action",
                    "Touch",
                    "Target: 1 willing creature",
                    "1 minute",
                    "None",
                    "No",
                    "V S M",
                    "Custom - Sparkly",
                    "A shimmering unicorn horn grows from the target's forehead. The target sheds rainbow light in a 10-foot radius and can use a bonus action to dramatically puke a harmless rainbow in a 15-foot cone.",
                    "M: a pinch of glitter and one heroic neigh"),
                new CustomSpellDefinition(
                    ruleSet,
                    "Stellar Clipboard",
                    "Wizard 1",
                    "Divination",
                    "divination",
                    "1 action",
                    "Self",
                    "Effect: one luminous clipboard",
                    "10 minutes",
                    "None",
                    "No",
                    "V S",
                    "Custom - Bureaucratic",
                    "A floating clipboard of starlight records your heroic achievements. You gain advantage on one Intelligence check made to recall lore or complete paperwork before the spell ends.",
                    "Approved by the Department of Radiant Forms.")
            ],
            "5.5" =>
            [
                new CustomSpellDefinition(
                    ruleSet,
                    "Blessing of Unicorn 2024",
                    "Bard 2",
                    "Transmutation",
                    "transmutation",
                    "1 action",
                    "Touch",
                    "Target: 1 willing creature",
                    "1 minute",
                    "None",
                    "No",
                    "V S M",
                    "Custom - Sparkly",
                    "A shimmering unicorn horn grows from the target's forehead. The target sheds rainbow light in a 10-foot radius and can use a bonus action to dramatically puke a harmless rainbow in a 15-foot cone.",
                    "M: a pinch of glitter and one heroic neigh"),
                new CustomSpellDefinition(
                    ruleSet,
                    "Heroic Post-it Note",
                    "Wizard 0",
                    "Abjuration",
                    "abjuration",
                    "1 action",
                    "30 feet",
                    "Target: one willing creature",
                    "1 hour",
                    "None",
                    "No",
                    "V S",
                    "Custom - Organized",
                    "You conjure a glowing reminder that hovers over the target. Once before the spell ends, the target can add 1d4 to an ability check after seeing the d20 roll but before knowing whether it succeeds.",
                    "The note always uses neat handwriting.")
            ],
            "pf1" =>
            [
                new CustomSpellDefinition(
                    ruleSet,
                    "Goblin Alarm Clock",
                    "Wizard 1",
                    "Evocation",
                    "evocation",
                    "1 standard action",
                    "Close",
                    "Target: one sleeping creature",
                    "Instantaneous",
                    "Will negates",
                    "No",
                    "V S",
                    "Custom - Noisy",
                    "You conjure an enthusiastic goblin voice that shouts wake-up advice in the target's ear. A sleeping target that fails its save wakes up startled and takes a -1 penalty on its next initiative check.",
                    "The voice insists it is helping."),
                new CustomSpellDefinition(
                    ruleSet,
                    "Bureaucratic Shield",
                    "Cleric 2",
                    "Abjuration",
                    "abjuration",
                    "1 standard action",
                    "Touch",
                    "Target: creature touched",
                    "1 minute/level",
                    "Will negates (harmless)",
                    "Yes (harmless)",
                    "V S",
                    "Custom - Protective",
                    "Invisible paperwork circles the target and intercepts minor trouble. The target gains a +1 deflection bonus to AC and a +1 resistance bonus on saving throws against fear effects.",
                    "Stamped in triplicate.")
            ],
            "pf2" =>
            [
                new CustomSpellDefinition(
                    ruleSet,
                    "Unicorn Applause",
                    "Rank 1 (occult)",
                    "Occult",
                    "enchantment",
                    "2 actions",
                    "30 feet",
                    "Area: 15-foot emanation",
                    "1 round",
                    "Will",
                    string.Empty,
                    "V S",
                    "Custom - Sparkly",
                    "A chorus of invisible unicorns applauds with supernatural enthusiasm. Creatures in the area that fail their save become dazzled until the start of your next turn as glittering approval rains down upon them.",
                    "The applause sounds oddly judgmental."),
                new CustomSpellDefinition(
                    ruleSet,
                    "Administrative Familiar",
                    "Rank 1 (arcane)",
                    "Arcane",
                    "evocation",
                    "2 actions",
                    "30 feet",
                    "Target: one unattended object",
                    "10 minutes",
                    string.Empty,
                    string.Empty,
                    "V S",
                    "Custom - Helpful",
                    "You animate a small spectral assistant that alphabetizes loose papers, sharpens quills, and keeps one unattended object tidy. Once during the spell's duration, it can Interact to draw or stow a tiny item for you.",
                    "Its tiny spectacles are purely decorative.")
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(ruleSet), ruleSet, "Unsupported ruleset test case.")
        };
    }

    private static async Task<TestSandbox> CreateSandboxAsync(params string[] requests)
    {
        var solutionRoot = FindSolutionRoot();
        var sandboxPath = Path.Combine(Path.GetTempPath(), "SpellCardsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandboxPath);

        var sourceRuntimeDir = AppContext.BaseDirectory;
        CopyDirectory(sourceRuntimeDir, sandboxPath);

        var assetsSourceDir = Path.Combine(solutionRoot, "SpellCards", "assets");
        var assetsTargetDir = Path.Combine(sandboxPath, "assets");
        CopyDirectory(assetsSourceDir, assetsTargetDir);

        await File.WriteAllLinesAsync(Path.Combine(sandboxPath, "requests.txt"), requests, Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(sandboxPath, "settings.json"), "{}", Encoding.UTF8);

        return new TestSandbox(sandboxPath);
    }

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SpellCards.sln")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the solution root.");
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, directory);
            Directory.CreateDirectory(Path.Combine(targetDir, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var destination = Path.Combine(targetDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private sealed class TestSandbox : IDisposable
    {
        public TestSandbox(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; }

        public Task WriteCustomSpellsAsync(string json)
            => File.WriteAllTextAsync(Path.Combine(DirectoryPath, "custom-spells.json"), json.Trim(), Encoding.UTF8);

        public async Task<(int ExitCode, string StandardOutput, string StandardError)> RunAsync()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "SpellCards.dll --no-condense",
                WorkingDirectory = DirectoryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start SpellCards process.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return (process.ExitCode, await stdoutTask, await stderrTask);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DirectoryPath))
                    Directory.Delete(DirectoryPath, recursive: true);
            }
            catch
            {
            }
        }
    }

    private sealed record CustomSpellDefinition(
        string RuleSet,
        string Name,
        string ClassLevel,
        string SchoolText,
        string SchoolKey,
        string Cast,
        string Range,
        string TargetOrArea,
        string Duration,
        string Save,
        string Sr,
        string Components,
        string Tags,
        string Description,
        string Notes)
    {
        public string SourceUrl { get; init; } = "custom-spells.json";
    }
}
