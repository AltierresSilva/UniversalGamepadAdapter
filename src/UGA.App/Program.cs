using System.Diagnostics;
using UGA.Core;
using UGA.Core.Storage;
using UGA.Infrastructure.Input;
using UGA.Infrastructure.Virtual;

namespace UGA.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(true, @"Local\UniversalGamepadAdapter-1", out bool first);
        if (!first) { MessageBox.Show("O UGA já está em execução. Procure o ícone na bandeja.", "Universal Gamepad Adapter"); return; }
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        ControllerEngine? activeEngine = null;
        Application.ThreadException += (_, e) => { activeEngine?.RequestEmulation(false); MessageBox.Show(e.Exception.Message, "UGA — erro", MessageBoxButtons.OK, MessageBoxIcon.Error); };
        try
        {
            var store = new JsonStore(); var settings = store.LoadSettings();
            using var engine = new ControllerEngine(new SdlControllerSource(), new ViGEmBackend(), store, settings);
            activeEngine = engine;
            using var main = new MainForm(engine, store, settings);
            if (args.Contains("--smoke-test"))
            {
                var timer = new System.Windows.Forms.Timer { Interval = 5000 };
                timer.Tick += (_, _) => { timer.Stop(); main.CloseForTest(); timer.Dispose(); };
                main.Shown += (_, _) => timer.Start();
            }
            engine.StartWorker(); Application.Run(main);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "UGA — não foi possível iniciar", MessageBoxButtons.OK, MessageBoxIcon.Error); Environment.ExitCode = 1; }
        finally { mutex.ReleaseMutex(); }
    }
}
