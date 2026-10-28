using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DshAlwaysOnTop
{
    // ============================ 开关 ============================
    internal sealed class ToggleSwitch : Control
    {
        private readonly Theme T;
        public bool Checked;
        public event EventHandler Changed;

        public ToggleSwitch(Theme t)
        {
            T = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(44, 22);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : T.Card);
            int h = Height;
            Rectangle track = new Rectangle(0, (Height - 20) / 2, Width - 1, 20);
            using (GraphicsPath p = Shapes.RoundedPath(track, 10))
            using (SolidBrush b = new SolidBrush(Checked ? T.Accent : T.SwitchOff))
                g.FillPath(b, p);
            int d = 14;
            int x = Checked ? Width - d - 3 : 3;
            using (SolidBrush b = new SolidBrush(Color.White))
                g.FillEllipse(b, x, (Height - d) / 2, d, d);
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            Invalidate();
            if (Changed != null) Changed(this, EventArgs.Empty);
            base.OnClick(e);
        }
    }

    // ============================ 勾选（带文字） ============================
    internal sealed class CheckRow : Control
    {
        private readonly Theme T;
        public bool Checked;
        public event EventHandler Changed;

        public CheckRow(Theme t)
        {
            T = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = 24;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : T.Card);
            Rectangle box = new Rectangle(1, (Height - 18) / 2, 18, 18);
            using (GraphicsPath p = Shapes.RoundedPath(box, 4))
            {
                using (SolidBrush b = new SolidBrush(Checked ? T.Accent : Color.Transparent))
                    g.FillPath(b, p);
                using (Pen pen = new Pen(Checked ? T.Accent : T.FieldBorder, 1.4f))
                    g.DrawPath(pen, p);
            }
            if (Checked)
            {
                using (Pen pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLines(pen, new PointF[] {
                        new PointF(box.X + 4, box.Y + 9),
                        new PointF(box.X + 7.5f, box.Y + 12.5f),
                        new PointF(box.X + 14, box.Y + 5.5f) });
                }
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(26, 0, Width - 26, Height), T.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            Invalidate();
            if (Changed != null) Changed(this, EventArgs.Empty);
            base.OnClick(e);
        }
    }

    // ============================ 滑块 ============================
    internal sealed class Slider : Control
    {
        private readonly Theme T;
        public int Min = 1;
        public int Max = 10;
        public int Value = 3;
        public event EventHandler Changed;
        private bool dragging;

        public Slider(Theme t)
        {
            T = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = 22;
            Cursor = Cursors.Hand;
        }

        private int ThumbX
        {
            get
            {
                if (Max <= Min) return 8;
                double f = (double)(Value - Min) / (Max - Min);
                return (int)Math.Round(8 + f * (Width - 16));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : T.Card);
            int cy = Height / 2;
            Rectangle track = new Rectangle(6, cy - 2, Width - 12, 4);
            using (GraphicsPath p = Shapes.RoundedPath(track, 2))
            using (SolidBrush b = new SolidBrush(T.Track))
                g.FillPath(b, p);
            int tx = ThumbX;
            Rectangle filled = new Rectangle(6, cy - 2, Math.Max(0, tx - 6), 4);
            if (filled.Width > 0)
            {
                using (GraphicsPath p = Shapes.RoundedPath(filled, 2))
                using (SolidBrush b = new SolidBrush(T.Accent))
                    g.FillPath(b, p);
            }
            using (SolidBrush b = new SolidBrush(Color.White))
            using (Pen pen = new Pen(T.Accent, 2f))
            {
                g.FillEllipse(b, tx - 7, cy - 7, 14, 14);
                g.DrawEllipse(pen, tx - 7, cy - 7, 14, 14);
            }
        }

        private void SetFromX(int x)
        {
            double f = (double)(x - 8) / Math.Max(1, Width - 16);
            if (f < 0) f = 0;
            if (f > 1) f = 1;
            int v = Min + (int)Math.Round(f * (Max - Min));
            if (v != Value)
            {
                Value = v;
                Invalidate();
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            dragging = true;
            SetFromX(e.X);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging) SetFromX(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }
    }

    // ============================ 按钮 ============================
    internal sealed class FlatButton : Control
    {
        public bool Primary = false;
        protected override void OnClick(EventArgs e)
        {
            Program.CrashLog("click btn: " + Text);
            base.OnClick(e);
        }
        private readonly Theme T;
        private bool hover;
        private bool down;

        public FlatButton(Theme t)
        {
            T = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = 30;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : T.Card);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color bg;
            if (!Enabled) bg = Primary ? T.SwitchOff : T.Field;
            else if (Primary) bg = down ? Darken(T.Accent, 0.85) : (hover ? Lighten(T.Accent, 1.12) : T.Accent);
            else bg = down ? T.FieldBorder : (hover ? T.Hover : T.Field);
            using (GraphicsPath p = Shapes.RoundedPath(r, 5))
            {
                using (SolidBrush b = new SolidBrush(bg)) g.FillPath(b, p);
                if (!Primary)
                {
                    using (Pen pen = new Pen(T.FieldBorder)) g.DrawPath(pen, p);
                }
            }
            Color fg = !Enabled ? (Primary ? Color.White : T.SubText) : (Primary ? Color.White : T.Text);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private static Color Lighten(Color c, double f)
        {
            return Color.FromArgb(c.A,
                Math.Min(255, (int)(c.R * f)), Math.Min(255, (int)(c.G * f)), Math.Min(255, (int)(c.B * f)));
        }

        private static Color Darken(Color c, double f) { return Lighten(c, f); }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    }

    // ============================ 快捷键输入框 ============================
    internal sealed class HotkeyBox : Control
    {
        private readonly Theme T;
        public HotkeySpec Value;
        public event EventHandler Changed;
        private bool capturing;
        private bool armed;

        public HotkeyBox(Theme t)
        {
            T = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = 32;
            Cursor = Cursors.IBeam;
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : T.Card);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Shapes.RoundedPath(r, 5))
            {
                using (SolidBrush b = new SolidBrush(T.Field)) g.FillPath(b, p);
                using (Pen pen = new Pen(capturing ? T.Accent : T.FieldBorder, capturing ? 2f : 1f)) g.DrawPath(pen, p);
            }
            string text = Value == null ? "未设置" : Value.ToString();
            TextRenderer.DrawText(g, text, Font, new Rectangle(10, 0, Width - 130, Height), T.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (capturing)
            {
                TextRenderer.DrawText(g, "按新组合键替换", Font, new Rectangle(Width - 124, 0, 116, Height), T.SubText,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPrefix);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            armed = true;
            capturing = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            capturing = false;
            armed = false;
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!capturing)
            {
                base.OnKeyDown(e);
                return;
            }
            if (e.KeyCode == Keys.Escape) { capturing = false; armed = false; Invalidate(); return; }
            if (!armed) return;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown ||
                e.KeyCode == Keys.Tab || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) return;
            HotkeySpec spec = HotkeySpec.FromKeys(e.KeyCode, e.Modifiers);
            if (spec == null) return;
            bool isFunctionKey = (e.KeyCode >= Keys.F1 && e.KeyCode <= Keys.F24);
            if (spec.Modifiers == 0 && !isFunctionKey) return;
            Value = spec;
            capturing = false;
            armed = false;
            Invalidate();
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (Changed != null) Changed(this, EventArgs.Empty);
        }
    }

    // ============================ 颜色块 ============================
    internal sealed class Card : Panel
    {
        private readonly Theme T;
        public int Radius = 8;

        public Card(Theme t)
        {
            T = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = t.Card;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(T.Window);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Shapes.RoundedPath(r, Radius))
            {
                using (SolidBrush b = new SolidBrush(T.Card)) g.FillPath(b, p);
                using (Pen pen = new Pen(T.CardBorder)) g.DrawPath(pen, p);
            }
        }
    }

    // ============================ 主窗口 ============================
    internal sealed class MainForm : Form
    {
        private readonly Settings s;
        private readonly Controller c;
        private readonly Theme T;

        private Label hotkeyStatus;
        private HotkeyBox hkBox;
        private ToggleSwitch tgBorder;
        private ToggleSwitch tgRound;
        private ToggleSwitch tgSound;
        private ToggleSwitch tgAuto;
        private FlatButton[] thickSeg;
        private FlatButton[] opaSeg;
        private FlatButton[] closeSeg;
        private Label segHint;
        private static readonly int[] ThickVals = new int[] { 3, 4, 5 };
        private static readonly int[] OpaVals = new int[] { 100, 85, 70 };
        private Panel listHost;
        private Label emptyLabel;
        private System.Windows.Forms.Timer listTimer;
        private string listSig = "";
        private float S = 1f;

        private int Px(float v) { return (int)Math.Round(v * S); }

        private float DetectScale()
        {
            try
            {
                int d = Native.GetDpiForSystem();
                if (d >= 96 && d <= 480) return d / 96f;
            }
            catch { }
            try
            {
                using (Graphics g = CreateGraphics())
                {
                    if (g.DpiX >= 96 && g.DpiX <= 480) return g.DpiX / 96f;
                }
            }
            catch { }
            return 1f;
        }

        public MainForm(Settings settings, Controller controller, Theme theme)
        {
            s = settings;
            c = controller;
            T = theme;

            Text = "AlwaysOnTop 设置";
            Icon = IconStore.Window;
            BackColor = T.Window;
            ForeColor = T.Text;
            Font = (Font)SystemFonts.MessageBoxFont.Clone();
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            S = DetectScale();
            ClientSize = new Size(Px(720), Px(856));
            MinimumSize = new Size(Px(560), Px(420));
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            MaximizeBox = true;

            BuildUi();


            c.Changed += OnControllerChanged;
            listTimer = new System.Windows.Forms.Timer();
            listTimer.Interval = 700;
            listTimer.Tick += delegate { RefreshList(); };
            listTimer.Start();
            RefreshList();
            RefreshHotkeyStatus();
        }

        private static FontFamily SystemUiFamily()
        {
            try { return SystemFonts.MessageBoxFont.FontFamily; }
            catch { }
            try { return SystemFonts.DefaultFont.FontFamily; }
            catch { }
            return FontFamily.GenericSansSerif;
        }

        private Font F(float size, FontStyle style) { return new Font(SystemUiFamily(), size, style); }

        // -------------------- 构建界面 --------------------
        private void BuildUi()
        {
            Panel scroll = new Panel();
            scroll.Dock = DockStyle.Fill;
            scroll.AutoScroll = true;
            scroll.BackColor = T.Window;

            Panel content = new Panel();
            content.Location = Point.Empty;
            content.Size = new Size(640, 900);
            content.BackColor = T.Window;
            scroll.Controls.Add(content);

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 40;
            footer.BackColor = T.Window;

            Controls.Add(scroll);
            Controls.Add(footer);

            int pad = 18;
            int cardW = 604;
            int y = 16;

            // 标题区
            PictureBox icon = new PictureBox();
            icon.Bounds = new Rectangle(pad, y, 40, 40);
            icon.SizeMode = PictureBoxSizeMode.Zoom;
            Bitmap bmp = IconStore.ToBitmap(64);
            if (bmp != null) icon.Image = bmp;
            icon.BackColor = T.Window;
            content.Controls.Add(icon);

            Label title = new Label();
            title.Text = "AlwaysOnTop";
            title.Font = F(16F, FontStyle.Bold);
            title.ForeColor = T.Title;
            title.AutoSize = true;
            title.Location = new Point(pad + 50, y - 2);
            title.BackColor = T.Window;
            content.Controls.Add(title);

            y += 48;

            // ---------------- 卡片1：快捷键 ----------------
            Card card1 = NewCard(content, pad, y, cardW, 96);
            CardTitle(card1, "快捷键");
            CardDesc(card1, "按下组合键即可置顶或取消置顶当前窗口");
            hkBox = new HotkeyBox(T);
            hkBox.Value = s.Hotkey;
            hkBox.Bounds = new Rectangle(20, 56, 240, 30);
            hkBox.Font = F(9.5F, FontStyle.Regular);
            hkBox.BackColor = T.Card;
            hkBox.Changed += delegate
            {
                s.Hotkey = hkBox.Value;
                Apply();
            };
            card1.Controls.Add(hkBox);

            FlatButton btnDefault = NewButton(card1, 272, 57, 96, 28, "恢复默认");
            btnDefault.Click += delegate
            {
                s.Hotkey = HotkeySpec.Default();
                hkBox.Value = s.Hotkey;
                hkBox.Invalidate();
                Apply();
            };

            hotkeyStatus = NewLabel(card1, 380, 62, cardW - 400, 20, "", F(8.5F, FontStyle.Regular), T.SubText);
            y += 96 + 10;

            // ---------------- 卡片2：边框 ----------------
            Card card2 = NewCard(content, pad, y, cardW, 178);
            CardTitle(card2, "置顶边框");
            CardDesc(card2, "边框绘制在窗口外沿、紧贴窗口下方，不遮挡内容，移动和缩放时实时跟随");
            int row = 56;
            tgBorder = NewToggle(card2, cardW, row, s.ShowBorder);
            NewLabel(card2, 20, row + 2, 420, 22, "显示置顶高亮边框", F(9.5F, FontStyle.Regular), T.Text);
            tgBorder.Changed += delegate { s.ShowBorder = tgBorder.Checked; Apply(); };

            row += 32;
            NewLabel(card2, 20, row + 2, 104, 22, "边框粗细", F(9.5F, FontStyle.Regular), T.Text);
            thickSeg = NewSeg(card2, 132, row + 1, 64, 26, new string[] { "3 px", "4 px", "5 px" },
                NearestIdx(ThickVals, s.Thickness), delegate(int idx)
                {
                    s.Thickness = ThickVals[idx];
                    PaintSeg(thickSeg, idx);
                    Apply();
                });
            segHint = NewLabel(card2, 352, row + 4, 240, 22, "", F(8.5F, FontStyle.Regular), T.SubText);

            row += 32;
            NewLabel(card2, 20, row + 2, 104, 22, "不透明度", F(9.5F, FontStyle.Regular), T.Text);
            opaSeg = NewSeg(card2, 132, row + 1, 64, 26, new string[] { "100%", "85%", "70%" },
                NearestIdx(OpaVals, s.Opacity), delegate(int idx)
                {
                    s.Opacity = OpaVals[idx];
                    PaintSeg(opaSeg, idx);
                    Apply();
                });

            row += 32;
            tgRound = NewToggle(card2, cardW, row, s.RoundCorners);
            NewLabel(card2, 20, row + 2, 460, 22, "边框圆角跟随窗口系统圆角", F(9.5F, FontStyle.Regular), T.Text);
            tgRound.Changed += delegate { s.RoundCorners = tgRound.Checked; Apply(); };


            y += 178 + 10;

            // ---------------- 卡片3：行为 ----------------
            Card card3 = NewCard(content, pad, y, cardW, 146);
            CardTitle(card3, "行为");
            int r3 = 42;
            tgSound = NewToggle(card3, cardW, r3, s.PlaySound);
            NewLabel(card3, 20, r3 + 2, 520, 22, "置顶 / 取消置顶时播放提示音", F(9.5F, FontStyle.Regular), T.Text);
            tgSound.Changed += delegate { s.PlaySound = tgSound.Checked; Apply(); };

            r3 += 34;
            NewLabel(card3, 20, r3 + 2, 104, 22, "关闭窗口时", F(9.5F, FontStyle.Regular), T.Text);
            closeSeg = NewSeg(card3, 132, r3 + 1, 96, 26, new string[] { "收到托盘", "退出程序" },
                s.CloseToTray ? 0 : 1, delegate(int idx)
                {
                    s.CloseToTray = (idx == 0);
                    PaintSeg(closeSeg, idx);
                    Apply();
                });

            r3 += 34;
            tgAuto = NewToggle(card3, cardW, r3, AutoStartUtil.IsEnabled());
            NewLabel(card3, 20, r3 + 2, 520, 22, "开机自启（在启动文件夹创建快捷方式）", F(9.5F, FontStyle.Regular), T.Text);
            tgAuto.Changed += delegate { SetAutoStart(tgAuto.Checked); };
            y += 146 + 10;

            // ---------------- 卡片4：置顶窗口 ----------------
            Card card4 = NewCard(content, pad, y, cardW, 104);
            CardTitle(card4, "当前置顶的窗口");
            FlatButton btnAll = NewButton(card4, cardW - 20 - 90, 10, 90, 26, "全部取消");
            btnAll.Click += delegate { c.UnpinAll(); RefreshList(); };

            listHost = new Panel();
            listHost.Bounds = new Rectangle(16, 40, cardW - 32, 54);
            listHost.BackColor = T.Card;
            listHost.AutoScroll = true;
            card4.Controls.Add(listHost);

            emptyLabel = new Label();
            emptyLabel.Text = "暂无置顶窗口。选中一个窗口后按快捷键即可置顶。";
            emptyLabel.Font = F(9F, FontStyle.Regular);
            emptyLabel.ForeColor = T.SubText;
            emptyLabel.AutoSize = false;
            emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
            emptyLabel.Bounds = new Rectangle(0, 0, cardW - 32, 52);
            emptyLabel.BackColor = T.Card;
            emptyLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            listHost.Controls.Add(emptyLabel);

            y += 104 + 12;

            // ---------------- 页脚 ----------------

            Label foot2 = new Label();
            foot2.Text = "配置文件 AlwaysOnTop.ini 与程序同目录，改动自动保存";
            foot2.Font = F(8.5F, FontStyle.Regular);
            foot2.ForeColor = T.Text;
            if (Program.LowIntegrity)
            {
                foot2.Text = "低权限运行，无法置顶其它程序；请从资源管理器双击 exe 或点右侧重启";
                foot2.ForeColor = Color.FromArgb(196, 43, 28);
            }
            else if (!Program.TrayOk)
            {
                foot2.Text = "托盘图标未能注册（系统拒绝），关闭窗口后请重新运行程序";
                foot2.ForeColor = Color.FromArgb(196, 43, 28);
            }
            foot2.AutoSize = false;
            foot2.UseMnemonic = false;
            foot2.Bounds = new Rectangle(18, 9, 468, 22);
            foot2.BackColor = T.Window;
            footer.Controls.Add(foot2);

            FlatButton btnOpen2 = new FlatButton(T);
            btnOpen2.Font = F(9F, FontStyle.Regular);
            btnOpen2.BackColor = T.Window;
            if (Program.LowIntegrity)
            {
                btnOpen2.Text = "以管理员身份重启";
                btnOpen2.Bounds = new Rectangle(620 - 124, 6, 124, 28);
                btnOpen2.Click += delegate { RestartElevated(); };
            }
            else
            {
                btnOpen2.Text = "打开程序目录";
                btnOpen2.Bounds = new Rectangle(620 - 108, 6, 108, 28);
                btnOpen2.Click += delegate
                {
                    try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + Application.ExecutablePath + "\""); }
                    catch { }
                };
            }
            footer.Controls.Add(btnOpen2);

            footer.Scale(new SizeF(S, S));
            footer.Height = Px(40);

            content.Height = y + 16;
            content.Scale(new SizeF(S, S));
            scroll.AutoScrollMinSize = content.Size;

            int wantH = content.Height + footer.Height + 2;
            int maxH = Screen.PrimaryScreen.WorkingArea.Height - Px(70);
            if (maxH < Px(480)) maxH = Px(480);
            if (wantH > maxH) wantH = maxH;
            ClientSize = new Size(Px(640), wantH);
        }

        private void RestartElevated()
        {
            try
            {
                System.Diagnostics.ProcessStartInfo psi =
                    new System.Diagnostics.ProcessStartInfo(Application.ExecutablePath);
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex) { Program.CrashLog("elevate fail: " + ex.Message); }
            Application.Exit();
        }

        private Card NewCard(Control parent, int x, int y, int w, int h)
        {
            Card card = new Card(T);
            card.Bounds = new Rectangle(x, y, w, h);
            parent.Controls.Add(card);
            return card;
        }

        private void CardTitle(Card card, string text)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = F(11F, FontStyle.Bold);
            l.ForeColor = T.Title;
            l.AutoSize = true;
            l.Location = new Point(20, 12);
            l.BackColor = T.Card;
            l.UseMnemonic = false;
            card.Controls.Add(l);
        }

        private void CardDesc(Card card, string text)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = F(8.5F, FontStyle.Regular);
            l.ForeColor = T.SubText;
            l.AutoSize = true;
            l.Location = new Point(20, 34);
            l.BackColor = T.Card;
            l.UseMnemonic = false;
            card.Controls.Add(l);
        }

        private Label NewLabel(Control parent, int x, int y, int w, int h, string text, Font font, Color color)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = font;
            l.ForeColor = color;
            l.Bounds = new Rectangle(x, y, w, h);
            l.AutoSize = false;
            l.BackColor = parent is Card ? T.Card : T.Window;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.UseMnemonic = false;
            parent.Controls.Add(l);
            return l;
        }

        private ToggleSwitch NewToggle(Control parent, int cardWidth, int y, bool value)
        {
            ToggleSwitch t = new ToggleSwitch(T);
            t.Checked = value;
            t.Bounds = new Rectangle(cardWidth - 20 - 44, y, 44, 22);
            t.BackColor = T.Card;
            parent.Controls.Add(t);
            return t;
        }

        private FlatButton NewButton(Control parent, int x, int y, int w, int h, string text)
        {
            FlatButton b = new FlatButton(T);
            b.Text = text;
            b.Bounds = new Rectangle(x, y, w, h);
            b.Font = F(9F, FontStyle.Regular);
            b.BackColor = parent is Card ? T.Card : T.Window;
            parent.Controls.Add(b);
            return b;
        }

        // -------------------- 逻辑 --------------------
        private void Apply()
        {
            s.Save();
            c.ApplySettings();
            RefreshHotkeyStatus();
        }

        private void RefreshHotkeyStatus()
        {
            if (hotkeyStatus == null) return;
            if (c.HotkeyRegistered)
            {
                hotkeyStatus.Text = "已生效：" + s.Hotkey.ToString();
                hotkeyStatus.ForeColor = T.Title;
            }
            else
            {
                hotkeyStatus.Text = c.HotkeyError;
                hotkeyStatus.ForeColor = Color.FromArgb(196, 43, 28);
            }
        }

        private void SetAutoStart(bool on)
        {
            try
            {
                AutoStartUtil.Set(on);
                s.AutoStart = on;
                s.Save();
                if (on && !AutoStartUtil.IsEnabled())
                    MessageBox.Show(this, "创建快捷方式失败，可能被安全软件拦了。", "开机自启",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "设置开机自启失败：" + ex.Message, "开机自启",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tgAuto.Checked = AutoStartUtil.IsEnabled();
                tgAuto.Invalidate();
            }
        }

        private void OnControllerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired) BeginInvoke(new MethodInvoker(RefreshList));
                else RefreshList();
            }
            catch { }
        }

        private void RefreshList2(){ }

        private void RefreshList()
        {
            UpdateBorderControls();
            if (listHost == null || listHost.IsDisposed) return;
            List<PinInfo> items = c.Enumerate();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < items.Count; i++) sb.Append(items[i].Handle.ToInt64()).Append('|');
            string sig = sb.ToString();
            if (sig == listSig) return;
            listSig = sig;

            List<Control> old = new List<Control>();
            foreach (Control ctl in listHost.Controls) old.Add(ctl);
            for (int i = 0; i < old.Count; i++)
            {
                if (old[i] != emptyLabel)
                {
                    listHost.Controls.Remove(old[i]);
                    old[i].Dispose();
                }
            }

            emptyLabel.Visible = items.Count == 0;
            int rowY = 0;
            for (int i = 0; i < items.Count; i++)
            {
                PinInfo info = items[i];
                Panel rowPanel = new Panel();
                rowPanel.Bounds = new Rectangle(0, rowY, listHost.ClientSize.Width - 4, 32);
                rowPanel.BackColor = T.Card;

                Label t = new Label();
                t.Text = info.Title + (info.Process.Length > 0 ? "  ·  " + info.Process : "");
                t.Font = F(9F, FontStyle.Regular);
                t.ForeColor = T.Text;
                t.Bounds = new Rectangle(2, 4, rowPanel.Width - 100, 24);
                t.AutoSize = false;
                t.BackColor = T.Card;
                t.TextAlign = ContentAlignment.MiddleLeft;
                t.AutoEllipsis = true;
                t.UseMnemonic = false;
                rowPanel.Controls.Add(t);

                FlatButton btn = new FlatButton(T);
                btn.Text = "取消置顶";
                btn.Font = F(8.5F, FontStyle.Regular);
                btn.Bounds = new Rectangle(rowPanel.Width - 88, 2, 84, 26);
                btn.BackColor = T.Card;
                IntPtr handle = info.Handle;
                btn.Click += delegate { c.Unpin(handle, true); RefreshList(); };
                rowPanel.Controls.Add(btn);

                listHost.Controls.Add(rowPanel);
                rowY += 34;
            }
            listHost.Controls.SetChildIndex(emptyLabel, 0);
        }

        public void ShowHotkeyWarning(string msg)
        {
            if (hotkeyStatus != null)
            {
                hotkeyStatus.Text = msg;
                hotkeyStatus.ForeColor = Color.FromArgb(196, 43, 28);
            }
        }

        private delegate void SegPick(int idx);

        private FlatButton[] NewSeg(Control parent, int x, int y, int w, int h, string[] texts, int sel, SegPick pick)
        {
            FlatButton[] arr = new FlatButton[texts.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                FlatButton b = new FlatButton(T);
                b.Font = F(9F, FontStyle.Regular);
                b.Bounds = new Rectangle(x + i * (w + 6), y, w, h);
                b.Text = texts[i];
                b.Primary = (i == sel);
                int idx = i;
                b.Click += delegate { if (pick != null) pick(idx); };
                parent.Controls.Add(b);
                arr[i] = b;
            }
            return arr;
        }

        private static void PaintSeg(FlatButton[] seg, int sel)
        {
            if (seg == null) return;
            for (int i = 0; i < seg.Length; i++)
            {
                seg[i].Primary = (i == sel);
                seg[i].Invalidate();
            }
        }

        private static int NearestIdx(int[] vals, int v)
        {
            int best = 0;
            for (int i = 1; i < vals.Length; i++)
            {
                if (Math.Abs(vals[i] - v) < Math.Abs(vals[best] - v)) best = i;
            }
            return best;
        }

        private void UpdateBorderControls()
        {
            bool can = (c.PinnedCount == 0);
            if (thickSeg != null) for (int i = 0; i < thickSeg.Length; i++) thickSeg[i].Enabled = can;
            if (opaSeg != null) for (int i = 0; i < opaSeg.Length; i++) opaSeg[i].Enabled = can;
            if (segHint != null)
            {
                segHint.Text = can ? "" : "有窗口置顶中，粗细与不透明度不可调";
                segHint.ForeColor = can ? T.SubText : Color.FromArgb(196, 43, 28);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                Program.CrashLog("UI shown: DeviceDpi=" + DeviceDpi + " gfx=" + CreateGraphics().DpiX +
                    " client=" + ClientSize + " bounds=" + Bounds);
            }
            catch { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int dark = 0;
                Native.DwmSetWindowAttribute(Handle, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, 4);
                int pref = Native.DWMWCP_ROUND;
                Native.DwmSetWindowAttribute(Handle, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, 4);
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (s.CloseToTray && !Program.ExitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Program.CrashLog("form: 用户关闭 -> 收到托盘（CloseToTray=1，进程保留）");
                Hide();
                Program.TrimSoon();
                if (!Program.HideTipShown && Program.Tray != null)
                {
                    Program.HideTipShown = true;
                    try
                    {
                        Program.Tray.ShowBalloonTip(2500, "仍在后台运行",
                            "快捷键与置顶状态都还在；右键托盘图标可以退出。", ToolTipIcon.Info);
                    }
                    catch { }
                }
                return;
            }
            Program.CrashLog("form: closing reason=" + e.CloseReason + " -> 退出进程");
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            try { Application.Exit(); }
            catch { }
        }

        public void ForceClose() { Close(); }

        public void BringToFrontAndShow()
        {
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
            try { Native.SetForegroundWindow(Handle); }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { if (listTimer != null) { listTimer.Stop(); listTimer.Dispose(); } }
                catch { }
            }
            base.Dispose(disposing);
        }
    }

    // ============================ 托盘 ============================
    internal sealed class TrayContext : ApplicationContext
    {
        private readonly Settings s;
        private readonly Controller c;
        private readonly Theme T;
        private NotifyIcon tray;
        private ContextMenuStrip menu;
        private MainForm form;
        private EventWaitHandle showEvent;
        private Thread waiter;

        public TrayContext()
        {
            T = Theme.Current();
            s = Settings.Load(Settings.DefaultPath());
            c = new Controller(s);
            bool hotkeyOk = c.RegisterHotkey();

            form = new MainForm(s, c, T);
            form.Show();
            form.BringToFrontAndShow();

            BuildTray();
            c.Changed += delegate { UpdateTrayText(); };

            showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DshAlwaysOnTop_Show");
            waiter = new Thread(WaitLoop);
            waiter.IsBackground = true;
            waiter.Name = "AotShowWaiter";
            waiter.Start();

            if (!hotkeyOk && tray != null)
            {
                tray.ShowBalloonTip(4000, "快捷键注册失败",
                    "默认组合键可能被其它程序占用（例如 PowerToys 的 Always on Top）。打开设置换一个。",
                    ToolTipIcon.Warning);
            }
            try { if (s.AutoStart && !AutoStartUtil.IsEnabled()) AutoStartUtil.Set(true); }
            catch { }
        }

        private void WaitLoop()
        {
            while (true)
            {
                try { showEvent.WaitOne(); }
                catch { return; }
                try
                {
                    if (form != null && !form.IsDisposed)
                        form.BeginInvoke(new MethodInvoker(form.BringToFrontAndShow));
                }
                catch { }
            }
        }

        private void BuildTray()
        {
            menu = new ContextMenuStrip();
            menu.Font = (Font)SystemFonts.MenuFont.Clone();
            ToolStripMenuItem miOpen = new ToolStripMenuItem("打开设置(&S)");
            miOpen.Font = new Font(menu.Font, FontStyle.Bold);
            miOpen.Click += delegate { ShowSettings(); };
            menu.Items.Add(miOpen);

            ToolStripMenuItem miToggle = new ToolStripMenuItem("置顶 / 取消置顶当前窗口(&T)");
            miToggle.Click += delegate { c.ToggleForeground(); };
            menu.Items.Add(miToggle);

            ToolStripMenuItem miAll = new ToolStripMenuItem("取消所有置顶(&U)");
            miAll.Click += delegate { c.UnpinAll(); };
            menu.Items.Add(miAll);
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem miExit = new ToolStripMenuItem("退出(&X)");
            miExit.Click += delegate { ExitApp(); };
            menu.Items.Add(miExit);

            tray = new NotifyIcon();
            Program.Tray = tray;
            tray.Icon = IconStore.Tray;
            tray.Text = TrayTextSafe();
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.DoubleClick += delegate { ShowSettings(); };
            Program.CrashLog("tray: created visible=true probeOk=" + Program.TrayOk +
                             " icon=" + (IconStore.Tray == null ? "null" : IconStore.Tray.Width + "x" + IconStore.Tray.Height));
            System.Windows.Forms.Timer reShow = new System.Windows.Forms.Timer();
            reShow.Interval = 1200;
            reShow.Tick += delegate
            {
                reShow.Stop();
                reShow.Dispose();
                try
                {
                    tray.Visible = false;
                    tray.Visible = true;
                }
                catch { }
            };
            reShow.Start();
        }


        private string TrayTextSafe()
        {
            string t = "AlwaysOnTop · " + s.Hotkey.ToString();
            if (t.Length > 62) t = t.Substring(0, 62);
            return t;
        }

        private void UpdateTrayText()
        {
            try { if (tray != null) tray.Text = TrayTextSafe(); }
            catch { }
        }

        public void ShowSettings()
        {
            if (form == null || form.IsDisposed) return;
            form.BringToFrontAndShow();
        }

        public void ExitApp()
        {
            Program.ExitRequested = true;
            try { if (tray != null) { tray.Visible = false; tray.Dispose(); } }
            catch { }
            try { c.Dispose(); }
            catch { }
            try { if (form != null) form.ForceClose(); }
            catch { }
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { if (tray != null) tray.Dispose(); }
                catch { }
                try { if (menu != null) menu.Dispose(); }
                catch { }
            }
            base.Dispose(disposing);
        }
    }

    // ============================ 入口 ============================
    internal static class Program
    {
        public static void CrashLog(string msg)
        {
            try
            {
                string p = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "aot-error.log");
                File.AppendAllText(p, DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        public static bool ExitRequested = false;
        public static bool HideTipShown = false;
        public static NotifyIcon Tray = null;

        public static void TrimWorkingSet()
        {
            try
            {
                Native.SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle,
                    (IntPtr)(-1), (IntPtr)(-1));
                Program.CrashLog("mem: 已压缩工作集");
            }
            catch { }
        }

        public static void TrimSoon()
        {
            try
            {
                System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
                t.Interval = 900;
                t.Tick += delegate
                {
                    t.Stop();
                    t.Dispose();
                    TrimWorkingSet();
                };
                t.Start();
            }
            catch { }
        }

        private static void Shot(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (args.Length >= 3 && args[2] == "dark") Theme.ForceDark = true;
                Settings s = Settings.Load(Settings.DefaultPath());
                Controller c = new Controller(s);
                c.RegisterHotkey();
                Theme t = Theme.Current();
                MainForm f = new MainForm(s, c, t);
                f.Show();
                for (int i = 0; i < 25; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(60); }
                Bitmap bmp = new Bitmap(f.Width, f.Height);
                f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                bmp.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                bmp.Dispose();
                f.ForceClose();
                c.Dispose();
            }
            catch (Exception ex)
            {
                CrashLog("shot fail: " + ex.ToString());
            }
        }

        private static void Demo(string[] args)
        {
            Form probe = null;
            Controller c = null;
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (args.Length >= 3 && args[2] == "dark") Theme.ForceDark = true;
                Settings s = Settings.Load(Settings.DefaultPath());
                c = new Controller(s);
                c.RegisterHotkey();
                Theme t = Theme.Current();

                probe = new Form();
                probe.Text = "工作笔记.txt - 记事本";
                probe.StartPosition = FormStartPosition.Manual;
                probe.SetBounds(120, 120, 600, 420);
                probe.Show();
                MainForm f = new MainForm(s, c, t);
                f.Show();
                for (int i = 0; i < 12; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(60); }

                c.Pin(probe.Handle);
                for (int i = 0; i < 20; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(60); }

                Bitmap bmp = new Bitmap(f.Width, f.Height);
                f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                bmp.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                bmp.Dispose();

                Native.RECT br;
                Native.GetWindowRect(probe.Handle, out br);
                try
                {
                    Rectangle cap = new Rectangle(br.Left - 14, br.Top - 14, br.Width + 28, br.Height + 28);
                    Bitmap b2 = new Bitmap(cap.Width, cap.Height);
                    using (Graphics gg = Graphics.FromImage(b2))
                        gg.CopyFromScreen(cap.Left, cap.Top, 0, 0, cap.Size);
                    b2.Save(args[1].Replace(".png", "_border.png"), System.Drawing.Imaging.ImageFormat.Png);
                    b2.Dispose();
                }
                catch (Exception ex2) { CrashLog("border shot fail: " + ex2.Message); }
                CrashLog("demo: probe=" + br.Left + "," + br.Top + " " + br.Width + "x" + br.Height +
                         " pinned=" + c.IsPinned(probe.Handle) + " count=" + c.PinnedCount);

                f.ForceClose();
                c.Unpin(probe.Handle, false);
                probe.Close();
            }
            catch (Exception ex)
            {
                CrashLog("demo fail: " + ex.ToString());
            }
            finally
            {
                try { if (probe != null) probe.Dispose(); } catch { }
                try { if (c != null) c.Dispose(); } catch { }
            }
        }

        public static bool TrayOk = true;
        public static bool LowIntegrity = false;
        public static int IntegrityRid = -1;
        public static string LastError = "";

        [STAThread]
        private static void Main(string[] args)
        {
            IntegrityRid = Native.IntegrityRid();
            LowIntegrity = (IntegrityRid > 0 && IntegrityRid < 0x2000);
            if (args != null && args.Length > 0 && args[0] == "--selftest")
            {
                SelfTest.Run();
                return;
            }
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                    if (args[i] == "--log") Controller.DebugEnabled = true;
            }

            if (args != null && args.Length >= 2 && args[0] == "--screenshot")
            {
                Shot(args);
                return;
            }

            if (args != null && args.Length >= 2 && args[0] == "--demo")
            {
                Demo(args);
                return;
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, "DshAlwaysOnTop_SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    try
                    {
                        EventWaitHandle e;
                        if (EventWaitHandle.TryOpenExisting("DshAlwaysOnTop_Show", out e))
                        {
                            e.Set();
                            e.Dispose();
                        }
                    }
                    catch { }
                    return;
                }
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                    Application.ThreadException += delegate(object o, ThreadExceptionEventArgs e)
                    {
                        CrashLog("ThreadException: " + e.Exception.ToString());
                    };
                    AppDomain.CurrentDomain.UnhandledException += delegate(object o, UnhandledExceptionEventArgs e)
                    {
                        CrashLog("Unhandled: " + (e.ExceptionObject == null ? "null" : e.ExceptionObject.ToString()));
                    };
                    CrashLog("startup pid=" + System.Diagnostics.Process.GetCurrentProcess().Id);
                    IntegrityRid = Native.IntegrityRid();
                    LowIntegrity = (IntegrityRid > 0 && IntegrityRid < 0x2000);
                    CrashLog("integrity: rid=0x" + IntegrityRid.ToString("X") + " low=" + LowIntegrity + " trayOk=" + TrayOk);
                    Application.Run(new TrayContext());
                    CrashLog("message loop exited");
                }
                catch (Exception ex)
                {
                    CrashLog("FATAL: " + ex.ToString());
                }
                GC.KeepAlive(mutex);
            }
        }
    }

    // ============================ 自检 ============================
    internal static class SelfTest
    {
        private static readonly StringBuilder Log = new StringBuilder();

        private static void W(string s)
        {
            Log.Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("  ").Append(s).AppendLine();
        }

        private static void Pump(int rounds)
        {
            for (int i = 0; i < rounds; i++)
            {
                Application.DoEvents();
                Thread.Sleep(70);
            }
        }

        private static string DumpBorders()
        {
            StringBuilder sb = new StringBuilder();
            uint me = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            Native.EnumWindows(delegate(IntPtr h, IntPtr l)
            {
                uint pid;
                Native.GetWindowThreadProcessId(h, out pid);
                if (pid != me) return true;
                if ((Native.GetWindowLong(h, Native.GWL_EXSTYLE) & Native.WS_EX_LAYERED) == 0) return true;
                Native.RECT r;
                Native.GetWindowRect(h, out r);
                sb.Append("[vis=").Append(Native.IsWindowVisible(h) ? "1" : "0").Append(" ")
                  .Append(r.Left).Append(",").Append(r.Top).Append(" ")
                  .Append(r.Width).Append("x").Append(r.Height).Append("] ");
                return true;
            }, IntPtr.Zero);
            return sb.Length == 0 ? "(无)" : sb.ToString().Trim();
        }

        private static int MenuItemCount(IntPtr h)
        {
            IntPtr menu = Native.GetSystemMenu(h, false);
            return menu == IntPtr.Zero ? -1 : Native.GetMenuItemCount(menu);
        }

        public static bool ShellIconWorks()
        {
            NativeWindow w = new NativeWindow();
            try
            {
                w.CreateHandle(new CreateParams());
                Native.NOTIFYICONDATA nid = new Native.NOTIFYICONDATA();
                nid.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.NOTIFYICONDATA));
                nid.hWnd = w.Handle;
                nid.uID = 1;
                nid.uFlags = Native.NIF_MESSAGE | Native.NIF_ICON | Native.NIF_TIP;
                nid.uCallbackMessage = Native.WM_APP + 1;
                nid.hIcon = IconStore.Tray.Handle;
                nid.szTip = "AlwaysOnTop 托盘图标自检";
                bool add = Native.Shell_NotifyIcon(Native.NIM_ADD, ref nid);
                if (!add) W("  NIM_ADD err=" + System.Runtime.InteropServices.Marshal.GetLastWin32Error() +
                            " trayWnd=" + Native.FindWindow("Shell_TrayWnd", null));
                Native.Shell_NotifyIcon(Native.NIM_DELETE, ref nid);
                return add;
            }
            catch (Exception ex)
            {
                W("shell icon test failed: " + ex.Message);
                return false;
            }
            finally
            {
                try { w.DestroyHandle(); }
                catch { }
            }
        }

        public static void Run()
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            Controller ctrl = null;
            Form probe = null;
            try
            {
                W("=== AlwaysOnTop selftest ===");
                Settings s = Settings.Load(Settings.DefaultPath());
                ctrl = new Controller(s);
                W("hotkey " + s.Hotkey.ToString() + " -> " + ctrl.RegisterHotkey());
                W("sound enabled=" + s.PlaySound + " uiAccent=#64748B borderColor=" +
                  Settings.ToHex(new Controller(s).CurrentBorderColor()));
                Icon ti = IconStore.Tray;
                W("tray icon=" + (ti == null ? "null" : ti.Width + "x" + ti.Height) +
                  " resource=" + (typeof(IconStore).Assembly.GetManifestResourceStream("AlwaysOnTop.app.ico") != null));
                W("shell accept icon -> " + ShellIconWorks());

                probe = new Form();
                probe.Text = "AOT 自检窗口";
                probe.FormBorderStyle = FormBorderStyle.Sizable;
                probe.StartPosition = FormStartPosition.Manual;
                probe.SetBounds(240, 240, 460, 320);
                probe.ShowInTaskbar = false;
                probe.Show();
                Pump(6);
                IntPtr h = probe.Handle;
                W("probe hwnd=0x" + h.ToInt64().ToString("X") + " menuItems=" + MenuItemCount(h));
                ctrl.EnsureMenu(h);
                W("after EnsureMenu items=" + MenuItemCount(h));

                ctrl.Pin(h);
                W("pin -> pinned=" + ctrl.IsPinned(h) + " topmost=" + Native.IsTopmost(h) +
                  " count=" + ctrl.PinnedCount);
                Pump(6);
                W("borders: " + DumpBorders());

                Native.MoveWindow(h, 780, 520, 520, 340, true);
                Pump(14);
                W("after move: " + DumpBorders());
                W("list: " + ctrl.Enumerate().Count);

                ctrl.Unpin(h, true);
                Pump(4);
                W("unpin -> topmost=" + Native.IsTopmost(h) + " count=" + ctrl.PinnedCount);
                W("borders after unpin: " + DumpBorders());
                W("=== done ===");
            }
            catch (Exception ex)
            {
                W("EXCEPTION: " + ex.ToString());
            }
            finally
            {
                try { if (probe != null) { probe.Close(); probe.Dispose(); } }
                catch { }
                try { if (ctrl != null) ctrl.Dispose(); }
                catch { }
                try { File.WriteAllText(Path.Combine(dir, "selftest.log"), Log.ToString(), Encoding.UTF8); }
                catch { }
            }
        }
    }
}
