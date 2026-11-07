using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using UGA.Core;

namespace UGA.Infrastructure.Virtual;

public sealed class ViGEmBackend : IVirtualControllerBackend, IVirtualControllerFeedback
{
    private ViGEmClient? client;
    private IXbox360Controller? controller;
    public bool Connected { get; private set; }
    public event Action<byte, byte>? FeedbackReceived;
    public string Probe()
    {
        try { using var probe = new ViGEmClient(); return "ViGEmBus acessível (legado)"; }
        catch (Exception ex) { return "ViGEmBus ausente ou indisponível: " + ex.GetType().Name; }
    }
    public void Connect()
    {
        if (Connected) return;
        Disconnect();
        try
        {
            client = new ViGEmClient(); controller = client.CreateXbox360Controller();
            var target = controller;
            target.FeedbackReceived += (_, feedback) =>
            {
                if (Connected && ReferenceEquals(controller, target))
                    FeedbackReceived?.Invoke(feedback.LargeMotor, feedback.SmallMotor);
            };
            controller.AutoSubmitReport = false; controller.Connect(); Connected = true; Submit(VirtualState.Neutral);
        }
        catch (Exception ex)
        {
            try { Disconnect(); } catch { /* Preserve the connection error while releasing the client in Disconnect's finally. */ }
            throw new InvalidOperationException("ViGEmBus não conseguiu criar o Xbox virtual. Feche outros remapeadores, reconecte e tente novamente. Se persistir, reinicie o Windows e verifique o driver oficial. Detalhe: " + ex.GetType().Name + " — " + ex.Message, ex);
        }
    }
    public void Submit(in VirtualState state)
    {
        if (controller is null || !Connected) throw new InvalidOperationException("Xbox virtual desconectado.");
        controller.ResetReport();
        (PadButtons bit, Xbox360Button button)[] buttons = ButtonMap;
        foreach (var (bit,button) in buttons) controller.SetButtonState(button, (state.Buttons & bit) != 0);
        controller.SetAxisValue(Xbox360Axis.LeftThumbX, state.LX); controller.SetAxisValue(Xbox360Axis.LeftThumbY, state.LY);
        controller.SetAxisValue(Xbox360Axis.RightThumbX, state.RX); controller.SetAxisValue(Xbox360Axis.RightThumbY, state.RY);
        controller.SetSliderValue(Xbox360Slider.LeftTrigger, state.LT); controller.SetSliderValue(Xbox360Slider.RightTrigger, state.RT);
        controller.SubmitReport();
    }
    private static readonly (PadButtons, Xbox360Button)[] ButtonMap =
    [(PadButtons.A,Xbox360Button.A),(PadButtons.B,Xbox360Button.B),(PadButtons.X,Xbox360Button.X),(PadButtons.Y,Xbox360Button.Y),
     (PadButtons.Up,Xbox360Button.Up),(PadButtons.Down,Xbox360Button.Down),(PadButtons.Left,Xbox360Button.Left),(PadButtons.Right,Xbox360Button.Right),
     (PadButtons.Start,Xbox360Button.Start),(PadButtons.Back,Xbox360Button.Back),(PadButtons.L3,Xbox360Button.LeftThumb),(PadButtons.R3,Xbox360Button.RightThumb),
     (PadButtons.LB,Xbox360Button.LeftShoulder),(PadButtons.RB,Xbox360Button.RightShoulder)];
    public void Disconnect()
    {
        var target = controller; controller = null; var wasConnected = Connected; Connected = false;
        try
        {
            if (target is not null)
            {
                try { if (wasConnected) { target.ResetReport(); target.SubmitReport(); } }
                finally { try { target.Disconnect(); } finally { (target as IDisposable)?.Dispose(); } }
            }
        }
        finally { client?.Dispose(); client = null; }
    }
    public void Dispose() => Disconnect();
}
