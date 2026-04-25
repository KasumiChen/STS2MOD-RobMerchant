namespace OfflineTestHarness;

public static class TestAssemblyPathResolver
{
    public static string ResolveFromHarnessDirectory(string harnessDirectory)
    {
        var repoRoot = FindRepoRoot(harnessDirectory);

        return Path.Combine(
            repoRoot,
            "tests",
            "MerchantDiscountMod.Tests",
            "bin",
            "Debug",
            "net8.0",
            "MerchantDiscountMod.Tests.dll");
    }

    private static string FindRepoRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MerchantDiscountMod.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not find repository root from '{startDirectory}'.");
    }
}
