using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace UGA.App;

internal enum TextTone { Normal, Muted, Caption, Heading }
internal enum ButtonTone { Neutral, Primary, Danger, Navigation, Quick }

internal static class Theme
{
    public static bool IsLight { get; private set; }
    public static Color Background => IsLight ? Color.FromArgb(243,245,249) : Color.FromArgb(12,15,20);
    public static Color Sidebar => IsLight ? Color.FromArgb(250,251,253) : Color.FromArgb(16,19,25);
    public static Color Surface => IsLight ? Color.White : Color.FromArgb(21,25,32);
    public static Color SurfaceAlt => IsLight ? Color.FromArgb(245,247,251) : Color.FromArgb(28,33,43);
    public static Color Border => IsLight ? Color.FromArgb(218,225,235) : Color.FromArgb(43,49,60);
    public static Color Text => IsLight ? Color.FromArgb(28,37,53) : Color.FromArgb(235,239,247);
    public static Color Muted => IsLight ? Color.FromArgb(99,113,135) : Color.FromArgb(144,156,177);
    public static Color Blue => IsLight ? Color.FromArgb(37,99,220) : Color.FromArgb(91,149,255);
    public static Color Green => IsLight ? Color.FromArgb(12,132,92) : Color.FromArgb(55,207,154);
    public static Color Red => IsLight ? Color.FromArgb(192,55,67) : Color.FromArgb(246,113,126);
    public static Color BlueTint => IsLight ? Color.FromArgb(232,240,255) : Color.FromArgb(27,42,68);
    public static void Configure(string preference) => IsLight = preference == "Light";
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint window,uint attribute,ref int value,uint size);
    public static void TitleBar(Form form)
    { if(form.IsHandleCreated) { int dark=IsLight?0:1; DwmSetWindowAttribute(form.Handle,20,ref dark,4); } }
    public static void Apply(Control root,bool surface=false,bool sidebar=false)
    {
        surface = root is CardPanel || surface; sidebar = Equals(root.Tag,"sidebar") || sidebar;
        root.BackColor = sidebar ? Sidebar : surface ? Surface : Background;
        root.ForeColor = root.Tag is TextTone.Muted or TextTone.Caption ? Muted : Text;
        if(root is Form form) { form.Font=new Font("Segoe UI",10); TitleBar(form); }
        if(root is TextBoxBase or NumericUpDown or ComboBox or ListView) root.BackColor=SurfaceAlt;
        if(root is ComboBox combo) combo.FlatStyle=FlatStyle.Flat;
        if(root is TextBox text) text.BorderStyle=BorderStyle.None;
        if(root is ListView list)
        {
            list.BorderStyle=BorderStyle.None;
            foreach(ListViewItem row in list.Items) { row.BackColor=row.Index%2==0?Surface:SurfaceAlt; row.ForeColor=Text; }
        }
        foreach(Control child in root.Controls) Apply(child,surface,sidebar);
        root.Invalidate(true);
    }
    public static Label Label(string text="",bool large=false) => new() { Text=text,AutoSize=true,MaximumSize=new Size(1300,0),Margin=new Padding(0,4,0,4),
        Tag=large?TextTone.Heading:TextTone.Normal,Font=new Font("Segoe UI",large?19:10,large?FontStyle.Bold:FontStyle.Regular) };
    public static Label MutedLabel(string text="") { var l=Label(text); l.Tag=TextTone.Muted; return l; }
    public static Label Caption(string text) { var l=Label(text); l.Tag=TextTone.Caption; l.Font=new Font("Segoe UI",8,FontStyle.Bold); return l; }
    public static ModernButton Button(string text,Action action,ButtonTone tone=ButtonTone.Neutral,string glyph="")
    { var b=new ModernButton { Text=text,Tone=tone,Glyph=glyph,Margin=new Padding(0,0,8,0) }; b.Click+=(_,_)=>action(); return b; }
    public static FlowLayoutPanel Flow(bool vertical=false) => new() { Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=vertical?FlowDirection.TopDown:FlowDirection.LeftToRight,WrapContents=!vertical,Padding=Padding.Empty };
    public static GraphicsPath Rounded(RectangleF r,float radius)
    {
        var p=new GraphicsPath(); float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
        p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
}

internal sealed class CardPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] internal bool Hero { get; set; }
    public CardPanel() { DoubleBuffered=true;Padding=new Padding(22);Margin=new Padding(0,0,16,16); }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor??Theme.Background);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        if(Width<2||Height<2)return;
        using var shape=Theme.Rounded(new RectangleF(.5f,.5f,Width-1,Height-1),12);
        using var brush=new SolidBrush(Theme.Surface);
        e.Graphics.FillPath(brush,shape);using var border=new Pen(Hero?Theme.BlueTint:Theme.Border);e.Graphics.DrawPath(border,shape);
    }
    protected override void OnResize(EventArgs e) { base.OnResize(e);Invalidate(); }
}

internal sealed class ModernButton : Button
{
    private bool hover;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] internal ButtonTone Tone { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] internal string Glyph { get; set; }="";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] internal string Description { get; set; }="";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] internal bool Selected { get; set; }
    public ModernButton()
    {
        FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Height=40;AutoSize=true;
        Padding=new Padding(14,8,14,8);Font=new Font("Segoe UI",9.5f);
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
    }
    public override Size GetPreferredSize(Size proposedSize)=>new(TextRenderer.MeasureText(Text,Font).Width+(Glyph.Length>0?52:30),Tone==ButtonTone.Quick?62:40);
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(Parent?.BackColor??Theme.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;if(Width<2||Height<2)return;
        bool nav=Tone==ButtonTone.Navigation,quick=Tone==ButtonTone.Quick;
        Color fill=Tone==ButtonTone.Primary?Theme.Blue:Tone==ButtonTone.Danger?Theme.IsLight?Color.FromArgb(255,235,238):Color.FromArgb(54,30,38):nav&&Selected?Theme.BlueTint:hover?Theme.SurfaceAlt:nav?Theme.Sidebar:quick?Theme.Surface:Theme.SurfaceAlt;
        Color foreground=!Enabled?Theme.Muted:Tone==ButtonTone.Primary?Color.White:Tone==ButtonTone.Danger?Theme.Red:nav&&Selected?Theme.Blue:Theme.Text;
        using var shape=Theme.Rounded(new RectangleF(.5f,.5f,Width-1,Height-1),8);using var brush=new SolidBrush(fill);g.FillPath(brush,shape);
        if(!quick&&!nav){using var border=new Pen(Tone==ButtonTone.Primary?Theme.Blue:Theme.Border);g.DrawPath(border,shape);}
        if(nav&&Selected){using var accent=new SolidBrush(Theme.Blue);g.FillRectangle(accent,0,10,3,Height-20);}
        int x=nav?17:quick?12:13;
        if(Glyph.Length>0){IconPainter.Draw(g,Glyph,new Rectangle(x,(Height-20)/2,20,20),quick?Theme.Blue:foreground);x+=quick?36:29;}
        var flags=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix;
        if(quick)
        {
            using var bold=new Font(Font,FontStyle.Bold);using var small=new Font("Segoe UI",8.5f);
            TextRenderer.DrawText(g,Text,bold,new Rectangle(x,8,Width-x-32,22),foreground,flags);
            TextRenderer.DrawText(g,Description,small,new Rectangle(x,30,Width-x-32,21),Theme.Muted,flags);
            IconPainter.Draw(g,"chevron",new Rectangle(Width-24,(Height-14)/2,14,14),Theme.Muted);
        }
        else TextRenderer.DrawText(g,Text,Font,new Rectangle(x,0,Width-x-10,Height),foreground,flags);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-4,-4),foreground,fill);
    }
}

internal static class IconPainter
{
    public static void Draw(Graphics g,string name,Rectangle bounds,Color color)
    {
        var save=g.Save();g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/24f,bounds.Height/24f);
        using var p=new Pen(color,1.6f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
        void Line(float x,float y,float x2,float y2)=>g.DrawLine(p,x,y,x2,y2);
        switch(name)
        {
            case "home":g.DrawLines(p,[new PointF(3,10),new(12,3),new(21,10)]);g.DrawLines(p,[new PointF(5,9),new(5,21),new(10,21),new(10,14),new(14,14),new(14,21),new(19,21),new(19,9)]);break;
            case "gamepad":using(var s=Theme.Rounded(new(2,6,20,13),5))g.DrawPath(p,s);Line(6,12,12,12);Line(9,9,9,15);g.DrawEllipse(p,16,10,1.5f,1.5f);g.DrawEllipse(p,18,13,1.5f,1.5f);break;
            case "sliders":Line(3,6,21,6);Line(3,12,21,12);Line(3,18,21,18);g.DrawEllipse(p,7,3,6,6);g.DrawEllipse(p,14,9,6,6);g.DrawEllipse(p,5,15,6,6);break;
            case "pulse":g.DrawLines(p,[new PointF(2,12),new(6,12),new(9,5),new(13,20),new(16,9),new(18,12),new(22,12)]);break;
            case "settings":g.DrawEllipse(p,3,3,18,18);g.DrawEllipse(p,9,9,6,6);for(int i=0;i<8;i++){double a=i*Math.PI/4;Line((float)(12+9*Math.Cos(a)),(float)(12+9*Math.Sin(a)),(float)(12+11*Math.Cos(a)),(float)(12+11*Math.Sin(a)));}break;
            case "sun":g.DrawEllipse(p,7,7,10,10);for(int i=0;i<8;i++){double a=i*Math.PI/4;Line((float)(12+8*Math.Cos(a)),(float)(12+8*Math.Sin(a)),(float)(12+11*Math.Cos(a)),(float)(12+11*Math.Sin(a)));}break;
            case "moon":g.DrawArc(p,3,3,18,18,55,280);g.DrawArc(p,9,0,15,17,80,160);break;
            case "power":g.DrawArc(p,4,4,16,17,-52,284);Line(12,2,12,11);break;
            case "shield":g.DrawLines(p,[new PointF(12,2),new(21,6),new(20,15),new(17,19),new(12,22),new(7,19),new(4,15),new(3,6),new(12,2)]);Line(8,12,11,15);Line(11,15,16,9);break;
            case "check":Line(5,12,10,17);Line(10,17,20,6);break;
            case "chevron":Line(8,4,16,12);Line(16,12,8,20);break;
            case "exit":g.DrawLines(p,[new PointF(10,3),new(4,3),new(4,21),new(10,21)]);Line(10,12,22,12);Line(17,7,22,12);Line(22,12,17,17);break;
        }
        g.Restore(save);
    }
}
