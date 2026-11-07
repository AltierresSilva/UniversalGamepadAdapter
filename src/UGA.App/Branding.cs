using System.Reflection;
using System.Drawing.Drawing2D;

namespace UGA.App;

internal static class Branding
{
    public static Icon CreateIcon()
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("UGA.BrandIcon")
            ?? throw new InvalidOperationException("Ícone UGA ausente na aplicação.");
        using var icon=new Icon(stream,32,32);
        return (Icon)icon.Clone();
    }

    public static Bitmap CreateLogoImage()
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("UGA.BrandImage")
            ?? throw new InvalidOperationException("Imagem da marca UGA ausente na aplicação.");
        using var source=new Bitmap(stream);
        return new Bitmap(source);
    }
}

internal sealed class LogoView : Control
{
    private readonly Bitmap logo=Branding.CreateLogoImage();
    public LogoView(){DoubleBuffered=true;AccessibleName="Logo UGA, controle verde neon com esfera azul e vermelha e X branco";}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        float scale=Math.Min(Width/(float)logo.Width,Height/(float)logo.Height);
        float width=logo.Width*scale,height=logo.Height*scale;
        e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
        e.Graphics.DrawImage(logo,new RectangleF((Width-width)/2,(Height-height)/2,width,height));
    }
    protected override void Dispose(bool disposing){if(disposing)logo.Dispose();base.Dispose(disposing);}
}
