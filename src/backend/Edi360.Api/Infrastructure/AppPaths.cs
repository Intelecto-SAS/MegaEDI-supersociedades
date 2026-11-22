namespace Edi360.Api.Infrastructure;

public sealed class AppPaths
{
    public string DataRoot { get; }
    public string EdiDb { get; }
    public string LegacyDb { get; }
    public string Storage { get; }
    public string LegacyStorage { get; }
    public string Pages { get; }

    public AppPaths(IWebHostEnvironment env)
    {
        DataRoot = Environment.GetEnvironmentVariable("EDI_DATA_ROOT")
            ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "..", "data"));
        Directory.CreateDirectory(DataRoot);
        EdiDb = Path.Combine(DataRoot, "edi360.db");
        LegacyDb = Path.Combine(DataRoot, "gedess-legacy.db");
        Storage = Path.Combine(DataRoot, "storage");
        LegacyStorage = Path.Combine(DataRoot, "legacy-storage");
        Pages = Path.Combine(DataRoot, "process-pages");
        Directory.CreateDirectory(Storage);
        Directory.CreateDirectory(LegacyStorage);
        Directory.CreateDirectory(Pages);
    }
}
