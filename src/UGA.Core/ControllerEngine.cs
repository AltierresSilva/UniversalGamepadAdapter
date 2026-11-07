using System.Collections.Concurrent;
using System.Diagnostics;

namespace UGA.Core;

public sealed record EngineSnapshot(PhysicalState Input, VirtualState Output, MappingProfile? Profile,
    bool Running, string Status, string Error, IReadOnlyList<DeviceInfo> Devices, double ActualHz,
    ControllerOutputCapabilities OutputCapabilities = default, string OutputStatus = "");

public sealed class ControllerEngine : IDisposable
{
    private readonly IPhysicalControllerSource source;
    private readonly IVirtualControllerBackend backend;
    private readonly IProfileStore store;
    private readonly IMappingService mapping;
    private readonly PhysicalState input = new();
    private readonly ConcurrentQueue<Action> commands = new();
    private readonly CancellationTokenSource cancel = new();
    private Task? worker;
    private MappingProfile? profile;
    private string? deviceKey;
    private uint? deviceInstanceId;
    private AppSettings outputSettings;
    private ControllerOutputCapabilities outputCapabilities;
    private string outputStatus = "";
    private readonly object feedbackLock = new();
    private bool acceptFeedback, rumbleActive, rumbleFailed;
    private int feedbackValue, feedbackSequence, sentFeedbackSequence = -1;
    private long nextFeedback, testUntil;
    private bool requested, suspended, autoStart, initialized;
    private int pollHz;
    private string error = "", status = "Aguardando controle";
    private VirtualState output;
    private double measuredHz;
    private EngineSnapshot snapshot = new(new(), default, null, false, "Inicializando", "", [], 0);
    public EngineSnapshot Snapshot => Volatile.Read(ref snapshot);
    public ControllerEngine(IPhysicalControllerSource source, IVirtualControllerBackend backend, IProfileStore store,
        AppSettings settings, IMappingService? mapping = null)
    {
        settings.Validate(); this.source = source; this.backend = backend; this.store = store;
        this.mapping = mapping ?? new MappingService(); autoStart = settings.AutoStart; pollHz = settings.PollHz;
        source.PreferredDeviceKey = settings.PreferredDeviceKey;
        outputSettings = settings;
        if (backend is IVirtualControllerFeedback feedback) feedback.FeedbackReceived += ReceiveFeedback;
    }
    public void StartWorker() => worker ??= Task.Factory.StartNew(Run, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    public void RequestEmulation(bool enabled) => commands.Enqueue(() => { requested = enabled; error = ""; });
    public void Suspend(bool value) => commands.Enqueue(() => suspended = value);
    public void SelectDevice(string key) => commands.Enqueue(() => { StopOutput(); source.PreferredDeviceKey = key; });
    public void Configure(AppSettings settings)
    {
        settings.Validate(); commands.Enqueue(() =>
        {
            autoStart = settings.AutoStart; pollHz = settings.PollHz;
            bool vibrationChanged = settings.VibrationPercent != outputSettings.VibrationPercent;
            outputSettings = settings;
            outputStatus = "Preferências de LED e vibração salvas.";
            ApplyLed();
            if (vibrationChanged) { StopRumble(); sentFeedbackSequence = -1; }
        });
    }
    public void TestVibration(string key) => commands.Enqueue(() =>
    {
        if (input.Device?.Key != key || !outputCapabilities.Rumble) return;
        rumbleFailed = false;
        if (SendRumble(ScaleMotor(255), ScaleMotor(255), 500))
        {
            testUntil = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 2;
            outputStatus = outputSettings.VibrationPercent == 0 ? "Vibração desativada (0%)." : "Teste de vibração: 0,5 segundo.";
        }
    });
    public void StopVibration() => commands.Enqueue(() =>
    {
        // Discard pending game feedback; a new report from the game can resume it.
        lock (feedbackLock) { feedbackValue = 0; feedbackSequence++; }
        StopRumble(); outputStatus = "Vibração interrompida.";
    });
    public void ApplyProfile(MappingProfile value)
    {
        value.Validate(); var copy = value.Copy();
        commands.Enqueue(() => { StopOutput(); if (input.Device?.Key == copy.DeviceKey) profile = copy; });
    }
    // All device/backend operations occur on this one thread. Public for deterministic simulated tests.
    public void Step()
    {
        try
        {
            if (!initialized) { source.Initialize(); initialized = true; }
            source.Poll(input);
            outputCapabilities = input.Device is not null && source is IPhysicalControllerOutput effects ? effects.OutputCapabilities : default;
            if (input.Device?.Key != deviceKey || input.Device?.InstanceId != deviceInstanceId)
            {
                StopOutput(); StopRumble(); deviceKey = input.Device?.Key; deviceInstanceId = input.Device?.InstanceId; profile = null;
                rumbleFailed = false; outputStatus = ""; ApplyLed();
                if (input.Device is { } device)
                {
                    try { profile = store.Load(device.Key); }
                    catch (Exception ex) { error = "Perfil recuperado: " + ex.Message; }
                    profile ??= device.HasSdlMapping ? MappingService.SdlDefault(device) : new() { DeviceKey = device.Key };
                    profile.Validate();
                    if (autoStart && profile.Complete) requested = true;
                }
            }
            while (commands.TryDequeue(out var command)) command();
            if (input.Device is null || suspended || !requested || profile?.Complete != true)
            {
                StopOutput(); status = input.Device is null ? "Aguardando controle" : suspended ? "Mapeamento / emulação suspensa" :
                    profile?.Complete != true ? "Conclua o assistente de mapeamento" : "Pronto para iniciar";
                UpdateVibration();
                return;
            }
            if (!backend.Connected)
            {
                lock (feedbackLock) { feedbackValue = 0; feedbackSequence++; acceptFeedback = true; }
                rumbleFailed = false;
                backend.Connect(); backend.Submit(VirtualState.Neutral);
            }
            output = mapping.Map(input, profile); backend.Submit(output); status = "Xbox 360 virtual ativo";
            UpdateVibration();
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message; requested = false;
            input.Clear();
            StopOutput(); StopRumble(); outputCapabilities = default; status = "Falha; corrija e clique em Iniciar";
        }
    }
    public void Publish() => Volatile.Write(ref snapshot, new(input.Copy(), output, profile?.Copy(), backend.Connected,
        status, error, source.Devices.ToArray(), measuredHz, outputCapabilities, outputStatus));
    private void StopOutput()
    {
        output = default;
        lock (feedbackLock) { acceptFeedback = false; feedbackValue = 0; }
        if (!backend.Connected) return;
        StopRumble();
        try { backend.Submit(VirtualState.Neutral); } catch (Exception ex) { error = ex.Message; }
        finally { try { backend.Disconnect(); } catch (Exception ex) { error = ex.Message; } }
    }
    private void ReceiveFeedback(byte low, byte high)
    {
        lock (feedbackLock)
        {
            if (!acceptFeedback) return;
            feedbackValue = low | (high << 8); feedbackSequence++;
        }
    }
    private ushort ScaleMotor(byte value) => (ushort)(value * 257 * outputSettings.VibrationPercent / 100);
    private void ApplyLed()
    {
        if (!outputSettings.CustomLedEnabled || !outputCapabilities.RgbLed || source is not IPhysicalControllerOutput effects) return;
        try
        {
            int color = outputSettings.LedColor;
            effects.SetLed((byte)(color >> 16), (byte)(color >> 8), (byte)color);
            outputStatus = "Cor do LED aplicada.";
        }
        catch (Exception ex) { outputStatus = "Não foi possível ajustar o LED: " + ex.Message; }
    }
    private bool SendRumble(ushort low, ushort high, uint duration)
    {
        if (!outputCapabilities.Rumble || source is not IPhysicalControllerOutput effects) return false;
        try { effects.Rumble(low, high, duration); rumbleActive = low != 0 || high != 0; return true; }
        catch (Exception ex) { rumbleFailed = true; outputStatus = "Não foi possível ajustar a vibração: " + ex.Message; return false; }
    }
    private void StopRumble()
    {
        testUntil = 0;
        if (rumbleActive) SendRumble(0, 0, 0);
        rumbleActive = false;
    }
    private void UpdateVibration()
    {
        long now = Stopwatch.GetTimestamp();
        if (testUntil > now) return;
        if (testUntil != 0) { StopRumble(); sentFeedbackSequence = -1; }
        if (!backend.Connected || rumbleFailed || !outputCapabilities.Rumble) return;
        int value, sequence;
        lock (feedbackLock) { value = feedbackValue; sequence = feedbackSequence; }
        if (sequence == sentFeedbackSequence && (value == 0 || now < nextFeedback)) return;
        sentFeedbackSequence = sequence; nextFeedback = now + Stopwatch.Frequency / 10;
        // Short renewable pulses ensure vibration expires if the game or worker stops responding.
        SendRumble(ScaleMotor((byte)value), ScaleMotor((byte)(value >> 8)), 250);
    }
    private void Run()
    {
        var clock = Stopwatch.StartNew(); var nextUi = TimeSpan.Zero; var measureStart = clock.Elapsed; int samples = 0;
        using var waiter = new HighResolutionWaiter(cancel.Token.WaitHandle);
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var start = clock.Elapsed; Step(); samples++;
                if ((clock.Elapsed - measureStart).TotalSeconds >= 1) { measuredHz = samples / (clock.Elapsed - measureStart).TotalSeconds; samples = 0; measureStart = clock.Elapsed; }
                if (clock.Elapsed >= nextUi) { Publish(); nextUi = clock.Elapsed + TimeSpan.FromMilliseconds(40); }
                var remaining = TimeSpan.FromSeconds(1.0 / pollHz) - (clock.Elapsed - start);
                if (remaining > TimeSpan.Zero) waiter.Wait(remaining);
            }
        }
        finally { StopOutput(); StopRumble(); backend.Dispose(); source.Dispose(); }
    }
    public void Dispose()
    {
        cancel.Cancel();
        if (worker is not null) worker.GetAwaiter().GetResult();
        else { StopOutput(); StopRumble(); backend.Dispose(); source.Dispose(); }
        if (backend is IVirtualControllerFeedback feedback) feedback.FeedbackReceived -= ReceiveFeedback;
        cancel.Dispose();
    }
}
