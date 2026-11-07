using UGA.Core;
using Xunit;

namespace UGA.Tests;

public class MappingTests
{
    internal static DeviceInfo Device => new(1, "Controle de teste", "test-device", "test-guid", 1, 2, "", "", "Simulado", true);
    internal static PhysicalState Input() => new() { Device = Device, Axes = new short[8], Buttons = new bool[32], Hats = new byte[1] };
    [Theory]
    [InlineData(0, PadButtons.A)] [InlineData(1, PadButtons.B)] [InlineData(2, PadButtons.X)] [InlineData(3, PadButtons.Y)]
    [InlineData(4, PadButtons.Back)] [InlineData(6, PadButtons.Start)] [InlineData(7, PadButtons.L3)] [InlineData(8, PadButtons.R3)]
    [InlineData(9, PadButtons.LB)] [InlineData(10, PadButtons.RB)] [InlineData(11, PadButtons.Up)] [InlineData(12, PadButtons.Down)]
    [InlineData(13, PadButtons.Left)] [InlineData(14, PadButtons.Right)]
    public void SdlButtonsMapToXbox(int index, PadButtons expected)
    { var s = Input(); s.SdlButtons[index] = true; Assert.Equal(expected, new MappingService().Map(s, MappingService.SdlDefault(Device)).Buttons); }
    [Theory]
    [InlineData(-32768, -1f)] [InlineData(0, 0f)] [InlineData(32767, 1f)] [InlineData(16384, .5000153f)]
    public void SignedAxisNormalization(int raw, float expected) => Assert.Equal(expected, MappingService.Normalize(raw, new(), false), 4);
    [Theory]
    [InlineData(0f, 0f)] [InlineData(.079f, 0f)] [InlineData(-.08f, 0f)] [InlineData(.54f, .5f)] [InlineData(-1f, -1f)] [InlineData(1f, 1f)]
    public void DeadzoneRescalesRemainingRange(float value, float expected) => Assert.Equal(expected, MappingService.Deadzone(value, .08f), 4);
    [Fact] public void ManualReversedAxisUsesMeasuredExtremes()
    {
        var b = new InputBinding { Center = 1000, PositiveEnd = -30000, NegativeEnd = 31000 };
        Assert.Equal(1, MappingService.Normalize(-30000, b, false)); Assert.Equal(-1, MappingService.Normalize(31000, b, false)); Assert.Equal(0, MappingService.Normalize(1000, b, false));
        Assert.Equal(-1, MappingService.Normalize(-30000, b with { Invert = true }, false));
    }
    [Theory]
    [InlineData(-32768, 0f)] [InlineData(0, .5000076f)] [InlineData(32767, 1f)]
    public void RawTriggerUsesRestAndPressedEndpoints(int raw, float expected) => Assert.Equal(expected,
        MappingService.Normalize(raw, new() { Center = -32768, PositiveEnd = 32767 }, true), 4);
    [Fact] public void TriggerCanHaveNegativeDirection()
    { Assert.Equal(0, MappingService.Normalize(32767, new() { Center = 32767, PositiveEnd = -32768 }, true)); Assert.Equal(1, MappingService.Normalize(-32768, new() { Center = 32767, PositiveEnd = -32768 }, true)); }
    [Fact] public void TriggerClampsAndSupportsDigitalButton()
    {
        var s = Input(); var p = MappingService.SdlDefault(Device); p.Bindings[Target.LT] = new() { Kind = InputKind.Button, Index = 20 };
        s.Buttons[20] = true; s.SdlAxes[5] = short.MaxValue;
        var output = new MappingService().Map(s, p); Assert.Equal(255, output.LT); Assert.Equal(255, output.RT);
    }
    [Fact] public void HatDiagonalAndOppositeDirections()
    {
        var s = Input(); var p = new MappingProfile { DeviceKey = Device.Key };
        p.Bindings[Target.Up] = new() { Kind = InputKind.Hat, Index = 0, HatMask = 1 };
        p.Bindings[Target.Right] = new() { Kind = InputKind.Hat, Index = 0, HatMask = 2 };
        p.Bindings[Target.Down] = new() { Kind = InputKind.Button, Index = 0 };
        s.Hats[0] = 3; Assert.Equal(PadButtons.Up | PadButtons.Right, new MappingService().Map(s,p).Buttons);
        s.Buttons[0] = true; Assert.Equal(PadButtons.Right, new MappingService().Map(s,p).Buttons);
    }
    [Fact] public void NeutralAndUnknownInputsNeverDriveCamera()
    {
        var s = Input(); var p = MappingService.SdlDefault(Device);
        Assert.Equal(VirtualState.Neutral, new MappingService().Map(s,p));
        s.Axes[7] = 32000; s.Buttons[31] = true; s.SdlButtons[20] = true; // unknown raw axis and touchpad button.
        Assert.Equal(VirtualState.Neutral, new MappingService().Map(s,p));
    }
    [Fact] public void SdlVerticalAxesAreInvertedForXInput()
    { var s = Input(); s.SdlAxes[1] = short.MinValue; s.SdlAxes[3] = short.MaxValue; var v = new MappingService().Map(s, MappingService.SdlDefault(Device)); Assert.Equal(short.MaxValue, v.LY); Assert.Equal(short.MinValue, v.RY); }
    [Fact] public void DisconnectedOrDifferentDeviceIsNeutral()
    { var s = Input(); s.SdlButtons[0] = true; s.Device = null; Assert.Equal(default, new MappingService().Map(s, MappingService.SdlDefault(Device))); s.Device = Device with { Key = "other" }; Assert.Equal(default, new MappingService().Map(s,MappingService.SdlDefault(Device))); }
    [Fact] public void DuplicateBindingsAndMalformedProfilesAreRejected()
    {
        var p = MappingService.SdlDefault(Device); p.Bindings[Target.B] = p.Bindings[Target.A]; Assert.Throws<InvalidDataException>(p.Validate);
        Assert.Throws<InvalidDataException>((p with { Deadzone = float.NaN }).Validate);
        Assert.Throws<InvalidDataException>((p with { Deadzone = .31f }).Validate);
        Assert.Throws<InvalidDataException>((p with { Version = 99 }).Validate);
    }
    [Fact] public void InvalidAxisAndButtonTypesAreRejected()
    {
        var p = new MappingProfile { DeviceKey = Device.Key }; p.Bindings[Target.RX] = new() { Kind = InputKind.Button, Index = 0 }; Assert.Throws<InvalidDataException>(p.Validate);
        p.Bindings[Target.RX] = new() { Kind = InputKind.Axis, PositiveEnd = 0 }; Assert.Throws<InvalidDataException>(p.Validate);
        p.Bindings.Clear(); p.Bindings[Target.Up] = new() { Kind = InputKind.Hat, HatMask = 3 }; Assert.Throws<InvalidDataException>(p.Validate);
    }
}
