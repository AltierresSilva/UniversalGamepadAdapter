using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using UGA.Core;
using UGA.Core.Storage;
using UGA.Infrastructure.Input;

namespace UGA.Infrastructure.HidHide;

public sealed record HidHideJournal(string AppPath, string DevicePath, bool AddedApp, bool AddedDevice, bool EnabledCloak, string Before);
public sealed class HidHideService
{
    public const string OfficialDownload = "https://github.com/nefarius/HidHide/releases";
    private readonly string journalPath;
    private readonly Func<string[], Task<string>>? commandOverride;
    private readonly Func<string, DeviceIdentity> resolveIdentity = WindowsDeviceIdentity.Resolve;
    public string? CliPath { get; }
    public bool Installed => CliPath is not null || commandOverride is not null;
    public bool HasChanges => File.Exists(journalPath);
    public HidHideService(string storageRoot)
    {
        journalPath = Path.Combine(storageRoot, "hidhide-changes.json");
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Nefarius Software Solutions", "HidHide");
        CliPath = new[] { Path.Combine(dir, "HidHideCLI.exe"), Path.Combine(dir, "x64", "HidHideCLI.exe") }.FirstOrDefault(File.Exists);
    }
    internal HidHideService(string root, Func<string[], Task<string>> commandRunner, Func<string, DeviceIdentity> resolver) : this(root)
    { commandOverride = commandRunner; resolveIdentity = resolver; }
    private async Task<string> Run(params string[] args)
    {
        if (commandOverride is not null) return await commandOverride(args);
        if (CliPath is null) throw new InvalidOperationException("HidHide não instalado. Use o fornecedor oficial ou a configuração manual.");
        var start = new ProcessStartInfo(CliPath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Não foi possível executar HidHideCLI.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(); throw new IOException("HidHide não respondeu. Feche o Configuration Client e tente novamente."); }
        var output = await stdout; var errors = await stderr;
        if (process.ExitCode != 0 || !string.IsNullOrWhiteSpace(errors)) throw new IOException("HidHide: " + errors.Trim() + " (código " + process.ExitCode + ")");
        return output;
    }
    private static HashSet<string> Entries(string text, string command) => Regex.Matches(text, Regex.Escape(command) + "\\s+\"([^\"]+)\"")
        .Select(m => m.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
    public async Task<string> Inspect() => await Run("--cloak-state", "--inv-state", "--dev-list", "--app-list");
    public async Task Apply(DeviceInfo device, string executable)
    {
        if (HasChanges) throw new InvalidOperationException("Desfaça as alterações anteriores do UGA antes de configurar outro dispositivo.");
        if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable) || !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Execute o UGA pelo executável publicado antes de configurar HidHide.");
        var identity = resolveIdentity(device.Path);
        if (!identity.Resolved || identity.IsVirtual || !identity.InstancePath.StartsWith("HID\\", StringComparison.OrdinalIgnoreCase) ||
            !identity.InstancePath.Equals(device.InstancePath, StringComparison.OrdinalIgnoreCase) || (device.Vendor == 0x045E && device.Product == 0x028E))
            throw new InvalidOperationException("Identificação exata do HID físico indisponível. Use o assistente manual; nenhum dispositivo foi ocultado.");
        var inventory = await Run("--dev-gaming");
        using var json = JsonDocument.Parse(inventory);
        bool Found(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Array) return node.EnumerateArray().Any(Found);
            if (node.ValueKind != JsonValueKind.Object) return false;
            if (node.TryGetProperty("deviceInstancePath", out var p) && p.GetString()?.Equals(identity.InstancePath, StringComparison.OrdinalIgnoreCase) == true) return true;
            return node.EnumerateObject().Any(p => Found(p.Value));
        }
        if (!Found(json.RootElement)) throw new InvalidOperationException("HID selecionado não foi confirmado no inventário HidHide. Configure manualmente.");
        var before = await Inspect();
        if (before.Contains("--inv-on", StringComparison.Ordinal)) throw new InvalidOperationException("HidHide usa lista inversa. O UGA não mudará essa configuração global. Use o cliente oficial.");
        var apps = Entries(before, "--app-reg"); var devices = Entries(before, "--dev-hide");
        bool enable = before.Contains("--cloak-off", StringComparison.Ordinal);
        if (enable && devices.Count > 0) throw new InvalidOperationException("Há dispositivos de outros aplicativos na lista com ocultação desativada. Revise e ative pelo cliente oficial primeiro.");
        var journal = new HidHideJournal(executable, identity.InstancePath, !apps.Contains(executable), !devices.Contains(identity.InstancePath), enable, before);
        JsonStore.WriteAtomic(journalPath, journal); // Persist intent before every possible device visibility change.
        if (journal.AddedApp) await Run("--app-reg", executable);
        if (!Entries(await Run("--app-list"), "--app-reg").Contains(executable)) throw new IOException("O executável não apareceu na lista permitida; ocultação cancelada.");
        if (journal.AddedDevice) await Run("--dev-hide", identity.InstancePath);
        if (enable) await Run("--cloak-on");
        if (!Entries(await Run("--dev-list"), "--dev-hide").Contains(identity.InstancePath)) throw new IOException("HidHide não confirmou o dispositivo. Use Desfazer.");
    }
    public async Task Undo()
    {
        if (!HasChanges) return;
        var change = JsonSerializer.Deserialize<HidHideJournal>(await File.ReadAllTextAsync(journalPath), JsonFormat.Options)
            ?? throw new InvalidDataException("Backup HidHide inválido. Use o cliente oficial para reexibir somente seu controle físico.");
        if (change.AddedDevice) await Run("--dev-unhide", change.DevicePath);
        if (change.AddedApp) await Run("--app-unreg", change.AppPath);
        // Never disable cloaking while another device is hidden.
        if (change.EnabledCloak && Entries(await Run("--dev-list"), "--dev-hide").Count == 0) await Run("--cloak-off");
        File.Move(journalPath, journalPath + ".undone-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), true);
    }
}
