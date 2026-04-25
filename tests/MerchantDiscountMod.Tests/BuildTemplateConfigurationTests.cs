using System.Xml.Linq;
using OfflineTestHarness;

namespace MerchantDiscountMod.Tests;

public sealed class BuildTemplateConfigurationTests
{
    [Fact]
    public void ModProjectImportsLocalPropsAndPathDiscovery()
    {
        var project = LoadXml("src/MerchantDiscountMod/MerchantDiscountMod.csproj");

        var imports = project
            .Descendants("Import")
            .Select(import => import.Attribute("Project")?.Value)
            .Where(value => value is not null)
            .ToArray();

        Assert.Contains(".\\local.props", imports);
        Assert.Contains(".\\Sts2PathDiscovery.props", imports);
    }

    [Fact]
    public void ModProjectExposesExplicitSts2ValidationAndCopyTargets()
    {
        var project = LoadXml("src/MerchantDiscountMod/MerchantDiscountMod.csproj");
        var targetNames = project
            .Descendants("Target")
            .Select(target => target.Attribute("Name")?.Value)
            .ToArray();

        Assert.Contains("ValidateSts2LocalPaths", targetNames);
        Assert.Contains("CopyModPackageToModsFolder", targetNames);
    }

    [Fact]
    public void Sts2ReferencesAreGatedToCompatibleTargetFramework()
    {
        var project = LoadXml("src/MerchantDiscountMod/MerchantDiscountMod.csproj");
        var sts2ReferenceGroupCondition = project
            .Descendants("ItemGroup")
            .Single(itemGroup => itemGroup.Descendants("Reference").Any(reference => reference.Attribute("Include")?.Value == "sts2"))
            .Attribute("Condition")
            ?.Value;

        Assert.Contains("'$(TargetFramework)' == '$(Sts2ReferenceTargetFramework)'", sts2ReferenceGroupCondition);
        Assert.Contains("net9.0", project.ToString(SaveOptions.DisableFormatting));
    }

    [Fact]
    public void ModProjectPackagesRobMerchantAssemblyAndManifest()
    {
        var repoRoot = FindRepoRoot();
        var project = LoadXml("src/MerchantDiscountMod/MerchantDiscountMod.csproj")
            .ToString(SaveOptions.DisableFormatting);
        var manifest = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "MerchantDiscountMod",
            "RobMerchant.json"));

        Assert.Contains("<AssemblyName>RobMerchant</AssemblyName>", project);
        Assert.Contains("RobMerchant.json", project);
        Assert.Contains("\"id\": \"RobMerchant\"", manifest);
        Assert.Contains("\"name\": \"Rob the Merchant\"", manifest);
    }

    [Fact]
    public void LocalPropsTemplateDocumentsNet9RequirementForGameReferences()
    {
        var template = LoadXml("src/MerchantDiscountMod/local.props.template");
        var allText = template.ToString(SaveOptions.DisableFormatting);

        Assert.Contains("Sts2ReferenceTargetFramework", allText);
        Assert.Contains("net9.0", allText);
    }

    [Fact]
    public void PathDiscoveryUsesPublicTemplatePlatformSpecificDataDirs()
    {
        var props = LoadXml("src/MerchantDiscountMod/Sts2PathDiscovery.props");
        var allText = props.ToString(SaveOptions.DisableFormatting);

        Assert.Contains("data_sts2_windows_x86_64", allText);
        Assert.Contains("data_sts2_linuxbsd_x86_64", allText);
        Assert.Contains("data_sts2_macos_x86_64", allText);
        Assert.Contains("SlayTheSpire2.app/Contents/MacOS/mods", allText);
    }

    [Fact]
    public void OfflineHarnessResolvesTestAssemblyFromBuildOutput()
    {
        var repoRoot = FindRepoRoot();
        var harnessOutputDirectory = Path.Combine(
            repoRoot,
            "tools",
            "OfflineTestHarness",
            "bin",
            "Debug",
            "net8.0");

        var testAssemblyPath = TestAssemblyPathResolver.ResolveFromHarnessDirectory(harnessOutputDirectory);

        Assert.Equal(
            Path.Combine(repoRoot, "tests", "MerchantDiscountMod.Tests", "bin", "Debug", "net8.0", "MerchantDiscountMod.Tests.dll"),
            testAssemblyPath);
    }

    [Fact]
    public void LivePackageHasSts2ModInitializerEntrypoint()
    {
        var initializer = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "MerchantDiscountMod",
            "ModEntry",
            "Sts2ModInitializer.cs"));

        Assert.Contains("[ModInitializer(nameof(Initialize))]", initializer);
        Assert.Contains("public static void Initialize()", initializer);
        Assert.Contains("CreateLiveBootstrap().ApplyHarmonyPatches()", initializer);
    }

    private static XDocument LoadXml(string repoRelativePath) =>
        XDocument.Load(Path.Combine(FindRepoRoot(), repoRelativePath));

    private static string FindRepoRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(currentDirectory, "MerchantDiscountMod.sln")))
        {
            return currentDirectory;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MerchantDiscountMod.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}
