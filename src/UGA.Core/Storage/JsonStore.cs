using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UGA.Core.Storage;

public sealed class JsonStore : IProfileStore
{
    public string Root { get; }
    public string LastRecovery { get; private set; } = "";
    public JsonStore(string? root = null)
    {
        Root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalGamepadAdapter");
        Directory.CreateDirectory(Path.Combine(Root, "Profiles"));
    }
    private string ProfilePath(string key) => Path.Combine(Root, "Profiles", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))) + ".json");
    public MappingProfile? Load(string deviceKey) => Read<MappingProfile>(ProfilePath(deviceKey), p =>
    { p.Validate(); if (p.DeviceKey != deviceKey) throw new InvalidDataException("Identificação divergente."); });
    public void Save(MappingProfile profile) { profile.Validate(); WriteAtomic(ProfilePath(profile.DeviceKey), profile); }
    public AppSettings LoadSettings() => Read<AppSettings>(Path.Combine(Root, "settings.json"), s => s.Validate()) ?? new();
    public void SaveSettings(AppSettings settings) { settings.Validate(); WriteAtomic(Path.Combine(Root, "settings.json"), settings); }
    private T? Read<T>(string file, Action<T> validate) where T : class
    {
        if (!File.Exists(file)) return null;
        try
        {
            var info = new FileInfo(file); if (info.Length > 1024 * 1024) throw new InvalidDataException("JSON muito grande.");
            var value = JsonSerializer.Deserialize<T>(File.ReadAllText(file), JsonFormat.Options) ?? throw new InvalidDataException("JSON nulo.");
            validate(value); return value;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or NotSupportedException or ArgumentException or NullReferenceException)
        {
            var recovery = file + ".invalid-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
            File.Move(file, recovery); LastRecovery = "JSON inválido preservado em " + recovery; return null;
        }
    }
    public static void WriteAtomic<T>(string file, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            { JsonSerializer.Serialize(stream, value, JsonFormat.Options); stream.Flush(true); }
            File.Move(temp, file, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
