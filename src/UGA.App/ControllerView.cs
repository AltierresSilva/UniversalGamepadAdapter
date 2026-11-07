using System.ComponentModel;
using System.Drawing.Drawing2D;
using UGA.Core;

namespace UGA.App;

internal enum GamepadShape { Xbox360, PlayStation, Generic }
internal sealed class ControllerView : Control
{
    private VirtualState state;
    private GamepadShape shape;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public VirtualState State { get => state; set { if(state!=value) {state=value;Invalidate();} } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public GamepadShape Shape { get=>shape;set {if(shape!=value){shape=value;Invalidate();}} }
    public ControllerView() { DoubleBuffered=true;Dock=DockStyle.Fill;MinimumSize=new(120,90); }
    public static GamepadShape ForDevice(DeviceInfo? d) => d is null ? GamepadShape.Generic :
        d.Vendor==0x054C || d.Name.Contains("PS4",StringComparison.OrdinalIgnoreCase) || d.Name.Contains("DualShock",StringComparison.OrdinalIgnoreCase) || d.Name.Contains("DualSense",StringComparison.OrdinalIgnoreCase) || d.Name.Contains("Wireless Controller",StringComparison.OrdinalIgnoreCase) ? GamepadShape.PlayStation :
        d.Vendor==0x045E || d.Name.Contains("Xbox",StringComparison.OrdinalIgnoreCase) ? GamepadShape.Xbox360 : GamepadShape.Generic;
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        float scale=Math.Min(Width/420f,Height/265f);
        g.TranslateTransform((Width-420*scale)/2,(Height-265*scale)/2);g.ScaleTransform(scale,scale);
        bool ps=Shape==GamepadShape.PlayStation,symmetric=Shape!=GamepadShape.Xbox360;
        using var body=new GraphicsPath();
        if(symmetric)
        {
            body.StartFigure();body.AddBezier(122,46,102,36,77,34,66,53);body.AddBezier(66,53,47,85,29,150,26,194);
            body.AddBezier(26,194,22,225,42,242,62,228);body.AddBezier(62,228,83,212,94,185,109,168);
            body.AddBezier(109,168,162,184,258,184,311,168);body.AddBezier(311,168,326,185,337,212,358,228);
            body.AddBezier(358,228,378,242,398,225,394,194);body.AddBezier(394,194,391,150,373,85,354,53);
            body.AddBezier(354,53,343,34,318,36,298,46);body.AddBezier(298,46,267,50,153,50,122,46);body.CloseFigure();
        }
        else
        {
            body.StartFigure();body.AddBezier(113,48,91,35,64,42,54,72);body.AddBezier(54,72,43,105,28,156,28,193);
            body.AddBezier(28,193,27,228,54,245,75,220);body.AddLine(75,220,110,169);body.AddBezier(110,169,164,185,256,185,310,169);
            body.AddLine(310,169,345,220);body.AddBezier(345,220,366,245,393,228,392,193);body.AddBezier(392,193,392,156,377,105,366,72);
            body.AddBezier(366,72,356,42,329,35,307,48);body.AddBezier(307,48,268,37,152,37,113,48);body.CloseFigure();
        }
        using(var shadow=new SolidBrush(Theme.IsLight?Color.FromArgb(20,30,45,70):Color.FromArgb(40,0,0,0)))
        { g.TranslateTransform(0,5);g.FillPath(shadow,body);g.TranslateTransform(0,-5); }
        using var fill=new LinearGradientBrush(new Rectangle(20,35,380,210),Theme.IsLight?Color.FromArgb(248,250,253):Color.FromArgb(38,46,60),Theme.IsLight?Color.FromArgb(224,231,240):Color.FromArgb(23,29,40),90);
        g.FillPath(fill,body);using var outline=new Pen(Theme.IsLight?Color.FromArgb(170,184,204):Color.FromArgb(88,102,124),1.8f);g.DrawPath(outline,body);
        using var faint=new Pen(Theme.Border,1.2f);using var dark=new SolidBrush(Theme.IsLight?Color.FromArgb(208,217,230):Color.FromArgb(14,19,28));
        using var textBrush=new SolidBrush(Theme.Muted);using var small=new Font("Segoe UI",8,FontStyle.Bold);
        void Shoulder(int x,string label,PadButtons bit)
        {
            using var path=Theme.Rounded(new(x,30,72,13),5);using var b=new SolidBrush((State.Buttons&bit)!=0?Theme.Green:Theme.SurfaceAlt);g.FillPath(b,path);g.DrawPath(outline,path);
            g.DrawString(label,small,textBrush,x+26,12);
        }
        Shoulder(73,ps?"L1":"LB",PadButtons.LB);Shoulder(275,ps?"R1":"RB",PadButtons.RB);
        void Trigger(int x,byte v,string label)
        {
            using var blue=new SolidBrush(Theme.Blue);g.FillRectangle(dark,x,245,65,4);g.FillRectangle(blue,x,245,65*v/255f,4);g.DrawString($"{label}  {v*100/255}%",small,textBrush,x,251);
        }
        Trigger(65,State.LT,ps?"L2":"LT");Trigger(290,State.RT,ps?"R2":"RT");
        if(ps)
        {
            using var touchpad=Theme.Rounded(new(153,57,114,56),8);g.FillPath(dark,touchpad);g.DrawPath(faint,touchpad);
            using var light=new Pen(Theme.Blue,2);g.DrawLine(light,172,51,248,51);
            using var share=Theme.Rounded(new(132,74,8,20),3);using var shareFill=new SolidBrush((State.Buttons&PadButtons.Back)!=0?Theme.Green:Theme.SurfaceAlt);g.FillPath(shareFill,share);
            using var options=Theme.Rounded(new(280,74,8,20),3);using var optionsFill=new SolidBrush((State.Buttons&PadButtons.Start)!=0?Theme.Green:Theme.SurfaceAlt);g.FillPath(optionsFill,options);
            g.FillEllipse(dark,202,123,16,16);g.DrawEllipse(faint,202,123,16,16);
        }
        else if(Shape==GamepadShape.Xbox360)
        {
            g.FillEllipse(dark,196,57,28,28);g.DrawEllipse(outline,196,57,28,28);using var p=new Pen(Theme.Muted,2);g.DrawLine(p,202,63,218,79);g.DrawLine(p,218,63,202,79);
            foreach(var (x,bit) in new[]{(177,PadButtons.Back),(235,PadButtons.Start)})
            {using var b=new SolidBrush((State.Buttons&bit)!=0?Theme.Green:Theme.SurfaceAlt);g.FillEllipse(b,x,97,13,13);g.DrawEllipse(faint,x,97,13,13);}
        }
        void Stick(int x,int y,short sx,short sy,PadButtons bit)
        {
            g.FillEllipse(dark,x-28,y-28,56,56);g.DrawEllipse(outline,x-28,y-28,56,56);
            float dx=sx/32768f*12,dy=-sy/32768f*12;
            using var cap=new SolidBrush((State.Buttons&bit)!=0?Theme.Blue:Theme.IsLight?Color.FromArgb(174,188,208):Color.FromArgb(48,60,79));
            g.FillEllipse(cap,x-20+dx,y-20+dy,40,40);g.DrawEllipse(outline,x-20+dx,y-20+dy,40,40);
        }
        Stick(symmetric?151:103,symmetric?155:99,State.LX,State.LY,PadButtons.L3);Stick(268,155,State.RX,State.RY,PadButtons.R3);
        void Dpad(int x,int y)
        {
            PointF[] cross=[new(x-7,y-24),new(x+7,y-24),new(x+7,y-7),new(x+24,y-7),new(x+24,y+7),new(x+7,y+7),new(x+7,y+24),new(x-7,y+24),new(x-7,y+7),new(x-24,y+7),new(x-24,y-7),new(x-7,y-7)];
            g.FillPolygon(dark,cross);g.DrawPolygon(outline,cross);using var active=new SolidBrush(Theme.Green);
            foreach(var (bit,r) in new[]{(PadButtons.Up,new Rectangle(x-5,y-22,10,14)),(PadButtons.Down,new Rectangle(x-5,y+8,10,14)),(PadButtons.Left,new Rectangle(x-22,y-5,14,10)),(PadButtons.Right,new Rectangle(x+8,y-5,14,10))})
                if((State.Buttons&bit)!=0)g.FillRectangle(active,r);
        }
        Dpad(symmetric?91:157,symmetric?101:156);
        void Face(int x,int y,PadButtons bit,string label,Color color)
        {
            using var b=new SolidBrush((State.Buttons&bit)!=0?Color.FromArgb(Theme.IsLight?90:65,color):Theme.IsLight?Color.FromArgb(232,237,244):Color.FromArgb(26,34,47));
            g.FillEllipse(b,x-13,y-13,26,26);g.DrawEllipse(outline,x-13,y-13,26,26);using var symbol=new Pen(color,1.8f);
            if(!ps||Shape==GamepadShape.Generic)
            {using var font=new Font("Segoe UI",11,FontStyle.Bold);using var tb=new SolidBrush(color);using var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};g.DrawString(label,font,tb,new RectangleF(x-13,y-13,26,26),format);}
            else if(bit==PadButtons.Y)g.DrawPolygon(symbol,[new PointF(x,y-6),new(x+6,y+5),new(x-6,y+5)]);
            else if(bit==PadButtons.B)g.DrawEllipse(symbol,x-6,y-6,12,12);
            else if(bit==PadButtons.X)g.DrawRectangle(symbol,x-5,y-5,10,10);
            else {g.DrawLine(symbol,x-5,y-5,x+5,y+5);g.DrawLine(symbol,x+5,y-5,x-5,y+5);}
        }
        Face(329,73,PadButtons.Y,Shape==GamepadShape.Generic?"4":"Y",ps?Color.FromArgb(67,191,150):Color.FromArgb(225,188,65));
        Face(301,101,PadButtons.X,Shape==GamepadShape.Generic?"3":"X",ps?Color.FromArgb(213,128,181):Color.FromArgb(82,145,239));
        Face(357,101,PadButtons.B,Shape==GamepadShape.Generic?"2":"B",Color.FromArgb(231,100,116));
        Face(329,129,PadButtons.A,Shape==GamepadShape.Generic?"1":"A",ps?Color.FromArgb(92,163,245):Color.FromArgb(67,191,130));
    }
}
