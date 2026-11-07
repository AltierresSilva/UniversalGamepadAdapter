using System.Runtime.InteropServices;
using UGA.Core;
using UGA.Infrastructure.Input;
using UGA.Infrastructure.Virtual;
using Xunit;

namespace UGA.Tests;

[CollectionDefinition("Native", DisableParallelization = true)]
public sealed class NativeCollection;
public sealed class DriverFactAttribute : FactAttribute
{
    public DriverFactAttribute() { if (Environment.GetEnvironmentVariable("UGA_DRIVER_TESTS") != "1") Skip = "Defina UGA_DRIVER_TESTS=1 para testar ViGEmBus real com relatórios neutros."; }
}
[Collection("Native")]
public class NativeTests
{
    [StructLayout(LayoutKind.Sequential)] private struct XInputState { public uint Packet; public ushort Buttons; public byte LT, RT; public short LX, LY, RX, RY; }
    [DllImport("xinput1_4.dll")] private static extern uint XInputGetState(uint index, out XInputState state);
    private static int CountXInput() { int count = 0; for (uint i = 0; i < 4; i++) if (XInputGetState(i, out _) == 0) count++; return count; }
    [Fact] public void NativeSdlInitializesAndPollsWithoutRequiringHardware()
    {
        using var source = new SdlControllerSource(); source.Initialize(); var state = new PhysicalState(); source.Poll(state);
        Assert.DoesNotContain(source.Devices, d => d.Vendor == 0x045E && d.Product == 0x028E);
    }
    [Fact] public void DriverProbeAlwaysReturnsDiagnosticInsteadOfThrowing()
    { using var backend = new ViGEmBackend(); Assert.NotEmpty(backend.Probe()); Assert.False(backend.Connected); }
    [DriverFact] public void RealDriverCreatesSingleNeutralXInputDeviceAndReleasesIt()
    {
        int before = CountXInput(); Assert.True(before < 4, "Todos os slots XInput estão ocupados.");
        using var backend = new ViGEmBackend();
        try
        {
            backend.Connect(); backend.Connect(); backend.Submit(VirtualState.Neutral); Assert.True(backend.Connected);
            Assert.True(SpinWait.SpinUntil(() => CountXInput() == before + 1, TimeSpan.FromSeconds(5)), "Xbox virtual não apareceu no XInput.");
            using var source = new SdlControllerSource(); source.Initialize(); source.Poll(new());
            Assert.DoesNotContain(source.Devices, d => d.Vendor == 0x045E && d.Product == 0x028E);
        }
        finally { backend.Disconnect(); }
        Assert.False(backend.Connected);
        Assert.True(SpinWait.SpinUntil(() => CountXInput() == before, TimeSpan.FromSeconds(5)), "Xbox virtual não foi liberado.");
    }
}
