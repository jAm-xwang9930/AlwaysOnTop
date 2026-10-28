// AlwaysOnTop - 轻量置顶工具（复刻 PowerToys AlwaysOnTop 的实现思路）
// 依赖：.NET Framework 4.x（Win10/11 自带），单文件，无安装，无服务，无计划任务
// 编译：csc /target:winexe /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll AlwaysOnTop.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DshAlwaysOnTop
{
    internal static class Native
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_DISABLED = 0x08000000;

        public const int LWA_COLORKEY = 0x00000001;
        public const int LWA_ALPHA = 0x00000002;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_NOREDRAW = 0x0008;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public const int WM_HOTKEY = 0x0312;

        public const int MF_BYCOMMAND = 0x00000000;
        public const int MIIM_STATE = 0x00000001;
        public const int MIIM_ID = 0x00000002;
        public const int MIIM_DATA = 0x00000020;
        public const int MIIM_STRING = 0x00000040;
        public const int MIIM_FTYPE = 0x00000100;
        public const int MFS_CHECKED = 0x00000008;
        public const int MFS_UNCHECKED = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        public const int DWMWA_EXCLUDED_FROM_PEEK = 12;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        public const uint EVENT_OBJECT_FOCUS = 0x8005;
        public const uint EVENT_OBJECT_DESTROY = 0x8001;
        public const uint EVENT_OBJECT_INVOKED = 0x8013;
        public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; public int Width { get { return Right - Left; } } public int Height { get { return Bottom - Top; } } }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MENUITEMINFO
        {
            public int cbSize;
            public int fMask;
            public int fType;
            public int fState;
            public int wID;
            public IntPtr hSubMenu;
            public IntPtr hbmpChecked;
            public IntPtr hbmpUnchecked;
            public IntPtr dwItemData;
            public string dwTypeData;
            public int cch;
            public IntPtr hbmpItem;
        }

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool InsertMenuItem(IntPtr hMenu, int uItem, bool fByPosition, ref MENUITEMINFO item);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool SetMenuItemInfo(IntPtr hMenu, int uItem, bool fByPosition, ref MENUITEMINFO item);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool GetMenuItemInfo(IntPtr hMenu, int uItem, bool fByPosition, ref MENUITEMINFO item);
        [DllImport("user32.dll")] public static extern bool RemoveMenu(IntPtr hMenu, int uPosition, int uFlags);
        [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr hMenu);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SetProp(IntPtr hWnd, string lpString, IntPtr hData);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetProp(IntPtr hWnd, string lpString);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr RemoveProp(IntPtr hWnd, string lpString);
        [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
        [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
        public static bool GetWindowRectPublic(IntPtr h, out RECT r) { return GetWindowRect(h, out r); }
        public static bool MoveWindowForTest(IntPtr h, int x, int y, int w, int hh) { return MoveWindow(h, x, y, w, hh, true); }
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);
        [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
    }

    internal sealed class BorderStyleConfig
    {
        public int Thickness = 3;
        public int Opacity = 100;
        public bool UseAccentColor = true;
        public Color CustomColor = Color.FromArgb(0x00, 0x78, 0xD7);
        public bool RoundCorners = true;

        public Color EffectiveColor()
        {
            Color c = UseAccentColor ? SystemColors2.AccentColor() : CustomColor;
            int op = Math.Max(20, Math.Min(100, Opacity));
            return Color.FromArgb(255, c.R * op / 100, c.G * op / 100, c.B * op / 100);
        }
    }

    internal static class SystemColors2
    {
        private static Color cached = Color.FromArgb(0x00, 0x78, 0xD7);

        public static Color AccentColor()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("ColorizationColor");
                        if (v is int)
                        {
                            int argb = (int)v;
                            cached = Color.FromArgb(255, (argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF);
                        }
                    }
                }
            }
            catch { }
            return cached;
        }
    }

    internal sealed class HotkeySpec
    {
        public int Modifiers;
        public int VirtualKey;

        public HotkeySpec() { Modifiers = Native.MOD_WIN | Native.MOD_CONTROL; VirtualKey = (int)Keys.T; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if ((Modifiers & Native.MOD_WIN) != 0) sb.Append("Win + ");
            if ((Modifiers & Native.MOD_CONTROL) != 0) sb.Append("Ctrl + ");
            if ((Modifiers & Native.MOD_ALT) != 0) sb.Append("Alt + ");
            if ((Modifiers & Native.MOD_SHIFT) != 0) sb.Append("Shift + ");
            sb.Append(KeyName(VirtualKey));
            return sb.ToString();
        }

        private static string KeyName(int vk)
        {
            Keys k = (Keys)vk;
            if (k >= Keys.D0 && k <= Keys.D9) return ((char)('0' + (k - Keys.D0))).ToString();
            if (k >= Keys.NumPad0 && k <= Keys.NumPad9) return "小键盘" + (k - Keys.NumPad0);
            return k.ToString();
        }

        public static int ModifiersFromKeys(Keys keyData)
        {
            int m = 0;
            if ((keyData & Keys.Control) == Keys.Control) m |= Native.MOD_CONTROL;
            if ((keyData & Keys.Alt) == Keys.Alt) m |= Native.MOD_ALT;
            if ((keyData & Keys.Shift) == Keys.Shift) m |= Native.MOD_SHIFT;
            if ((keyData & Keys.LWin) == Keys.LWin || (keyData & Keys.RWin) == Keys.RWin) m |= Native.MOD_WIN;
            return m;
        }
    }

    internal sealed class Settings
    {
        public HotkeySpec Hotkey = new HotkeySpec();
        public BorderStyleConfig Border = new BorderStyleConfig();
        public bool ShowSystemMenuItem = true;
        public bool EnableBorder = true;

        private string path;

        public Settings(string iniPath) { path = iniPath; Load(); }

        public string Path { get { return path; } }

        public void Load()
        {
            try
            {
                if (!File.Exists(path)) { Save(); return; }
                foreach (string rawLine in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string val = line.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "hotkey": ParseHotkey(val); break;
                        case "thickness": Border.Thickness = ClampInt(val, 1, 10, 3); break;
                        case "opacity": Border.Opacity = ClampInt(val, 20, 100, 100); break;
                        case "accent": Border.UseAccentColor = val == "1" || val.ToLowerInvariant() == "true"; break;
                        case "color": Border.CustomColor = ParseColor(val, Border.CustomColor); break;
                        case "roundcorners": Border.RoundCorners = val == "1" || val.ToLowerInvariant() == "true"; break;
                        case "systemmenu": ShowSystemMenuItem = val == "1" || val.ToLowerInvariant() == "true"; break;
                        case "border": EnableBorder = val == "1" || val.ToLowerInvariant() == "true"; break;
                    }
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; AlwaysOnTop 配置文件，改完重启程序生效（设置界面里改会自动保存）");
                sb.AppendLine("Hotkey=" + Hotkey.ToString());
                sb.AppendLine("Thickness=" + Border.Thickness.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Opacity=" + Border.Opacity.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Accent=" + (Border.UseAccentColor ? "1" : "0"));
                sb.AppendLine("Color=" + string.Format("#{0:X2}{1:X2}{2:X2}", Border.CustomColor.R, Border.CustomColor.G, Border.CustomColor.B));
                sb.AppendLine("RoundCorners=" + (Border.RoundCorners ? "1" : "0"));
                sb.AppendLine("SystemMenu=" + (ShowSystemMenuItem ? "1" : "0"));
                sb.AppendLine("Border=" + (EnableBorder ? "1" : "0"));
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void ParseHotkey(string text)
        {
            try
            {
                HotkeySpec spec = new HotkeySpec();
                spec.Modifiers = 0;
                string[] parts = text.Split('+');
                for (int i = 0; i < parts.Length; i++)
                {
                    string p = parts[i].Trim().ToLowerInvariant();
                    if (p == "win" || p == "windows") spec.Modifiers |= Native.MOD_WIN;
                    else if (p == "ctrl" || p == "control") spec.Modifiers |= Native.MOD_CONTROL;
                    else if (p == "alt") spec.Modifiers |= Native.MOD_ALT;
                    else if (p == "shift") spec.Modifiers |= Native.MOD_SHIFT;
                    else if (p.Length > 0)
                    {
                        Keys parsed;
                        if (p.Length == 1) parsed = (Keys)char.ToUpperInvariant(p[0]);
                        else parsed = (Keys)Enum.Parse(typeof(Keys), p, true);
                        spec.VirtualKey = (int)parsed;
                    }
                }
                if (spec.VirtualKey == 0 || spec.Modifiers == 0) return;
                Hotkey = spec;
            }
            catch { }
        }

        private static int ClampInt(string s, int min, int max, int fallback)
        {
            int v;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                return Math.Max(min, Math.Min(max, v));
            return fallback;
        }

        private static Color ParseColor(string s, Color fallback)
        {
            try
            {
                s = s.Trim().TrimStart('#');
                if (s.Length == 6)
                {
                    int v = int.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return Color.FromArgb(255, (v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF);
                }
            }
            catch { }
            return fallback;
        }
    }

    internal sealed class BorderForm : Form
    {
        private readonly IntPtr target;
        private readonly BorderStyleConfig cfg;
        private int lastCornerPref = -1;

        public BorderForm(IntPtr target, BorderStyleConfig cfg)
        {
            this.target = target;
            this.cfg = cfg;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            Enabled = false;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Show();
            Native.SetLayeredWindowAttributes(Handle, 0x000000, 255, Native.LWA_COLORKEY);
            int exclude = 1;
            Native.DwmSetWindowAttribute(Handle, Native.DWMWA_EXCLUDED_FROM_PEEK, ref exclude, sizeof(int));
            UpdateToTarget();
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style = Native.WS_POPUP | Native.WS_DISABLED;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOPMOST | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        public void UpdateToTarget()
        {
            if (!Native.IsWindow(target)) { Visible = false; return; }
            if (!Native.IsWindowVisible(target)) { Visible = false; return; }

            Native.RECT rect;
            if (Native.DwmGetWindowAttribute(target, Native.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(Native.RECT))) != 0)
            {
                Visible = false;
                return;
            }

            int t = Math.Max(1, cfg.Thickness);
            int x = rect.Left - t;
            int y = rect.Top - t;
            int w = rect.Width + t * 2;
            int h = rect.Height + t * 2;
            if (w <= 0 || h <= 0) { Visible = false; return; }

            // 关键：hWndInsertAfter = 目标窗口 → 边框紧贴在目标窗口下方，不会挡住内容
            Native.SetWindowPos(Handle, target, x, y, w, h, Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
            if (!Visible) Visible = true;
            Invalidate();
        }

        private int CornerRadius()
        {
            if (!cfg.RoundCorners) return 0;
            if (lastCornerPref < 0)
            {
                int pref;
                if (Native.DwmGetWindowAttribute(target, Native.DWMWA_WINDOW_CORNER_PREFERENCE, out pref, sizeof(int)) == 0)
                    lastCornerPref = pref;
                else
                    lastCornerPref = 0;
            }
            if (lastCornerPref == 2) return 8;
            if (lastCornerPref == 3) return 4;
            return 0;
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black); // 黑色 = 色键，透明

            int t = Math.Max(1, cfg.Thickness);
            float half = t / 2f;
            RectangleF box = new RectangleF(half, half, Width - t, Height - t);
            if (box.Width <= 0 || box.Height <= 0) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            int r = Math.Min(CornerRadius(), (int)(Math.Min(box.Width, box.Height) / 2));
            using (GraphicsPath path = BuildPath(box, r))
            using (Pen pen = new Pen(cfg.EffectiveColor(), t))
            {
                pen.Alignment = PenAlignment.Center;
                g.DrawPath(pen, path);
            }
        }

        private static GraphicsPath BuildPath(RectangleF r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0)
            {
                p.AddRectangle(r);
                return p;
            }
            float d = radius * 2f;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084) { m.Result = new IntPtr(-1); return; } // WM_NCHITTEST -> HTTRANSPARENT
            base.WndProc(ref m);
        }
    }

    internal sealed class Controller : IDisposable
    {
        public const int HotkeyId = 0xA011;
        public const int MenuCommandId = 0xA021;
        public static readonly IntPtr MenuOwnerTag = new IntPtr(0x414F54);
        private const string PropName = "DshAotPinned";
        public static bool DebugEnabled = false;
        private static readonly object LogLock = new object();

        public static void Dbg(string s)
        {
            if (!DebugEnabled) return;
            try
            {
                lock (LogLock)
                {
                    string p = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "aot-debug.log");
                    File.AppendAllText(p, DateTime.Now.ToString("HH:mm:ss.fff") + "  " + s + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }

        private readonly Settings settings;
        private readonly Dictionary<IntPtr, BorderForm> borders = new Dictionary<IntPtr, BorderForm>();
        private MessageWindow msgWindow;
        private readonly List<IntPtr> hooks = new List<IntPtr>();
        private Native.WinEventDelegate winEventProc;
        private System.Windows.Forms.Timer ticker;
        private IntPtr lastMenuWindow = IntPtr.Zero;
        private bool hotkeyOk;

        public event EventHandler HotkeyRegistrationFailed;
        public event EventHandler PinnedSetChanged;

        public Controller(Settings settings)
        {
            this.settings = settings;
            msgWindow = new MessageWindow();
            msgWindow.HotkeyPressed += OnHotkey;
            ticker = new System.Windows.Forms.Timer();
            ticker.Interval = 100;
            ticker.Tick += OnTick;
            InstallHooks();
            ticker.Start();
        }

        public bool HotkeyRegistered { get { return hotkeyOk; } }

        public bool RegisterHotkey()
        {
            Native.UnregisterHotKey(msgWindow.Handle, HotkeyId);
            hotkeyOk = Native.RegisterHotKey(msgWindow.Handle, HotkeyId, settings.Hotkey.Modifiers, settings.Hotkey.VirtualKey);
            if (!hotkeyOk && HotkeyRegistrationFailed != null) HotkeyRegistrationFailed(this, EventArgs.Empty);
            return hotkeyOk;
        }

        private void InstallHooks()
        {
            winEventProc = new Native.WinEventDelegate(OnWinEvent);
            AddHook(Native.EVENT_SYSTEM_FOREGROUND);
            AddHook(Native.EVENT_OBJECT_FOCUS);
            AddHook(Native.EVENT_OBJECT_INVOKED);
        }

        private void AddHook(uint ev)
        {
            IntPtr h = Native.SetWinEventHook(ev, ev, IntPtr.Zero, winEventProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS);
            if (h != IntPtr.Zero) hooks.Add(h);
        }

        private void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
        {
            try
            {
                if (eventType == Native.EVENT_OBJECT_INVOKED)
                {
                    Dbg("INVOKED hwnd=0x" + hwnd.ToInt64().ToString("X") + " obj=" + idObject + " child=0x" + idChild.ToString("X") + " want=0x" + MenuCommandId.ToString("X"));
                    if (idChild != MenuCommandId) return;
                    IntPtr cmdWindow = IntPtr.Zero;
                    if (lastMenuWindow != IntPtr.Zero && Native.IsWindow(lastMenuWindow) && HasOurMenuItem(lastMenuWindow)) cmdWindow = lastMenuWindow;
                    if (cmdWindow == IntPtr.Zero && hwnd != IntPtr.Zero && HasOurMenuItem(hwnd)) cmdWindow = hwnd;
                    if (cmdWindow == IntPtr.Zero)
                    {
                        IntPtr fg = Native.GetForegroundWindow();
                        if (fg != IntPtr.Zero && HasOurMenuItem(fg)) cmdWindow = fg;
                    }
                    if (cmdWindow != IntPtr.Zero) Toggle(cmdWindow);
                    return;
                }

                if (hwnd == IntPtr.Zero) return;
                if (eventType == Native.EVENT_SYSTEM_FOREGROUND || eventType == Native.EVENT_OBJECT_FOCUS)
                {
                    IntPtr fg = Native.GetForegroundWindow();
                    if (fg != IntPtr.Zero) UpdateSystemMenuItem(fg);
                }
            }
            catch { }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (borders.Count == 0) return;
            List<IntPtr> dead = new List<IntPtr>();
            foreach (KeyValuePair<IntPtr, BorderForm> pair in borders)
            {
                IntPtr h = pair.Key;
                if (!Native.IsWindow(h)) { dead.Add(h); continue; }
                // 目标窗口若被别的程序抢走 topmost，这里补回来
                if ((Native.GetWindowLong(h, Native.GWL_EXSTYLE) & Native.WS_EX_TOPMOST) == 0)
                    Native.SetWindowPos(h, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
                pair.Value.UpdateToTarget();
            }
            for (int i = 0; i < dead.Count; i++) Unpin(dead[i], false);

            IntPtr fg = Native.GetForegroundWindow();
            if (fg != IntPtr.Zero && fg != lastMenuWindow) UpdateSystemMenuItem(fg);
        }

        public bool IsPinned(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            IntPtr v = Native.GetProp(hwnd, PropName);
            if (v != IntPtr.Zero) return true;
            return borders.ContainsKey(hwnd);
        }

        public bool Toggle(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd)) return false;
            if (IsPinned(hwnd)) { Unpin(hwnd, true); return false; }
            Pin(hwnd);
            return true;
        }

        public void Pin(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd)) return;
            Native.SetProp(hwnd, PropName, new IntPtr(1));
            Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            if (settings.EnableBorder && !borders.ContainsKey(hwnd))
            {
                try { borders[hwnd] = new BorderForm(hwnd, settings.Border); }
                catch { }
            }
            UpdateSystemMenuItem(hwnd);
            if (PinnedSetChanged != null) PinnedSetChanged(this, EventArgs.Empty);
        }

        public void Unpin(IntPtr hwnd, bool restoreZOrder)
        {
            if (hwnd == IntPtr.Zero) return;
            BorderForm bf;
            if (borders.TryGetValue(hwnd, out bf))
            {
                borders.Remove(hwnd);
                try { bf.Close(); bf.Dispose(); } catch { }
            }
            Native.RemoveProp(hwnd, PropName);
            if (Native.IsWindow(hwnd) && restoreZOrder)
                Native.SetWindowPos(hwnd, Native.HWND_NOTOPMOST, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            if (PinnedSetChanged != null) PinnedSetChanged(this, EventArgs.Empty);
        }

        public void UnpinAll()
        {
            List<IntPtr> all = new List<IntPtr>(borders.Keys);
            for (int i = 0; i < all.Count; i++) Unpin(all[i], true);
        }

        public int PinnedCount { get { return borders.Count; } }

        public void ApplyBorderSettings()
        {
            foreach (KeyValuePair<IntPtr, BorderForm> pair in borders) pair.Value.UpdateToTarget();
        }

        // ---- 系统菜单项（照 PowerToys 的做法：GetSystemMenu + InsertMenuItem，不注入）----
        public void UpdateSystemMenuItem(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd)) return;
            IntPtr menu = Native.GetSystemMenu(hwnd, false);
            if (menu == IntPtr.Zero) return;

            if (!settings.ShowSystemMenuItem)
            {
                if (HasOurMenuItem(hwnd)) RemoveOurMenuItem(hwnd);
                return;
            }

            Native.MENUITEMINFO info = new Native.MENUITEMINFO();
            info.cbSize = Marshal.SizeOf(typeof(Native.MENUITEMINFO));
            info.fMask = Native.MIIM_FTYPE | Native.MIIM_ID | Native.MIIM_STATE | Native.MIIM_STRING | Native.MIIM_DATA;
            info.fType = 0;
            info.wID = MenuCommandId;
            info.fState = IsPinned(hwnd) ? Native.MFS_CHECKED : Native.MFS_UNCHECKED;
            info.dwTypeData = "置顶窗口(&A)";
            info.dwItemData = MenuOwnerTag;

            if (!HasOurMenuItem(hwnd))
            {
                Native.InsertMenuItem(menu, Native.SC_CLOSE, false, ref info);
                lastMenuWindow = hwnd;
            }
            else
            {
                Native.MENUITEMINFO upd = new Native.MENUITEMINFO();
                upd.cbSize = Marshal.SizeOf(typeof(Native.MENUITEMINFO));
                upd.fMask = Native.MIIM_STATE | Native.MIIM_STRING;
                upd.fState = info.fState;
                upd.dwTypeData = info.dwTypeData;
                Native.SetMenuItemInfo(menu, MenuCommandId, false, ref upd);
                lastMenuWindow = hwnd;
            }
        }

        public bool HasOurMenuItem(IntPtr hwnd)
        {
            IntPtr menu = Native.GetSystemMenu(hwnd, false);
            if (menu == IntPtr.Zero) return false;
            int count = Native.GetMenuItemCount(menu);
            for (int i = 0; i < count; i++)
            {
                Native.MENUITEMINFO info = new Native.MENUITEMINFO();
                info.cbSize = Marshal.SizeOf(typeof(Native.MENUITEMINFO));
                info.fMask = Native.MIIM_ID | Native.MIIM_DATA;
                if (!Native.GetMenuItemInfo(menu, i, true, ref info)) continue;
                if (info.wID == MenuCommandId && info.dwItemData == MenuOwnerTag) return true;
            }
            return false;
        }

        private void RemoveOurMenuItem(IntPtr hwnd)
        {
            IntPtr menu = Native.GetSystemMenu(hwnd, false);
            if (menu == IntPtr.Zero) return;
            int count = Native.GetMenuItemCount(menu);
            for (int i = count - 1; i >= 0; i--)
            {
                Native.MENUITEMINFO info = new Native.MENUITEMINFO();
                info.cbSize = Marshal.SizeOf(typeof(Native.MENUITEMINFO));
                info.fMask = Native.MIIM_ID | Native.MIIM_DATA;
                if (!Native.GetMenuItemInfo(menu, i, true, ref info)) continue;
                if (info.wID == MenuCommandId && info.dwItemData == MenuOwnerTag)
                {
                    Native.RemoveMenu(menu, i, 0x00000400); // MF_BYPOSITION
                    return;
                }
            }
        }

        private void OnHotkey(int id)
        {
            IntPtr fg = Native.GetForegroundWindow();
            if (fg == IntPtr.Zero) return;
            if (Native.GetWindowLong(fg, Native.GWL_EXSTYLE) == 0 && !Native.IsWindowVisible(fg)) return;
            Toggle(fg);
        }

        public void Dispose()
        {
            try { ticker.Stop(); ticker.Dispose(); } catch { }
            for (int i = 0; i < hooks.Count; i++) { try { Native.UnhookWinEvent(hooks[i]); } catch { } }
            hooks.Clear();
            UnpinAll();
            try { Native.UnregisterHotKey(msgWindow.Handle, HotkeyId); } catch { }
            try { msgWindow.DestroyHandle(); } catch { }
        }
    }

    internal sealed class MessageWindow : NativeWindow
    {
        public event Action<int> HotkeyPressed;

        public MessageWindow()
        {
            CreateParams cp = new CreateParams();
            cp.Caption = "DshAlwaysOnTopMessageWindow";
            cp.Style = 0;
            cp.ExStyle = 0;
            cp.X = 0; cp.Y = 0; cp.Width = 0; cp.Height = 0;
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                Action<int> h = HotkeyPressed;
                if (h != null) h(m.WParam.ToInt32());
                return;
            }
            base.WndProc(ref m);
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly Settings settings;
        private TextBox hotkeyBox;
        private NumericUpDown thickness;
        private NumericUpDown opacity;
        private CheckBox accent;
        private Button colorButton;
        private CheckBox roundCorners;
        private CheckBox systemMenu;
        private CheckBox borderEnabled;
        private Label hint;
        private Color chosenColor;

        public SettingsForm(Settings settings)
        {
            this.settings = settings;
            this.chosenColor = settings.Border.CustomColor;

            Text = "AlwaysOnTop 设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 300);
            Font = new Font("Microsoft YaHei UI", 9f);

            int y = 16;
            Label l1 = new Label(); l1.Text = "快捷键（点进框里直接按新组合）："; l1.SetBounds(16, y, 260, 20); Controls.Add(l1);
            y += 24;
            hotkeyBox = new TextBox();
            hotkeyBox.SetBounds(16, y, 260, 26);
            hotkeyBox.ReadOnly = true;
            hotkeyBox.Text = settings.Hotkey.ToString();
            hotkeyBox.KeyDown += OnHotkeyCapture;
            hotkeyBox.GotFocus += delegate { hint.Text = "现在按下你想要的组合键（需含至少一个修饰键）…"; };
            hotkeyBox.LostFocus += delegate { hint.Text = ""; };
            Controls.Add(hotkeyBox);

            Button resetHotkey = new Button();
            resetHotkey.Text = "恢复默认";
            resetHotkey.SetBounds(288, y, 110, 26);
            resetHotkey.Click += delegate
            {
                settings.Hotkey = new HotkeySpec();
                hotkeyBox.Text = settings.Hotkey.ToString();
            };
            Controls.Add(resetHotkey);

            y += 38;
            hint = new Label();
            hint.SetBounds(16, y, 380, 20);
            hint.ForeColor = Color.DimGray;
            Controls.Add(hint);

            y += 30;
            Label l2 = new Label(); l2.Text = "边框粗细："; l2.SetBounds(16, y + 3, 80, 20); Controls.Add(l2);
            thickness = new NumericUpDown();
            thickness.SetBounds(100, y, 60, 24);
            thickness.Minimum = 1; thickness.Maximum = 10;
            thickness.Value = settings.Border.Thickness;
            Controls.Add(thickness);
            Label l2b = new Label(); l2b.Text = "px   不透明度："; l2b.SetBounds(170, y + 3, 90, 20); Controls.Add(l2b);
            opacity = new NumericUpDown();
            opacity.SetBounds(262, y, 60, 24);
            opacity.Minimum = 20; opacity.Maximum = 100; opacity.Increment = 5;
            opacity.Value = settings.Border.Opacity;
            Controls.Add(opacity);
            Label l2c = new Label(); l2c.Text = "%"; l2c.SetBounds(326, y + 3, 20, 20); Controls.Add(l2c);

            y += 34;
            accent = new CheckBox();
            accent.Text = "跟随系统强调色";
            accent.SetBounds(16, y, 140, 24);
            accent.Checked = settings.Border.UseAccentColor;
            Controls.Add(accent);

            colorButton = new Button();
            colorButton.SetBounds(170, y - 2, 90, 26);
            colorButton.Text = "边框颜色…";
            colorButton.Click += delegate
            {
                using (ColorDialog dlg = new ColorDialog())
                {
                    dlg.Color = chosenColor;
                    dlg.FullOpen = true;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        chosenColor = dlg.Color;
                        accent.Checked = false;
                        UpdateColorButton();
                    }
                }
            };
            Controls.Add(colorButton);

            y += 34;
            roundCorners = new CheckBox();
            roundCorners.Text = "圆角（跟随窗口是否圆角）";
            roundCorners.SetBounds(16, y, 240, 24);
            roundCorners.Checked = settings.Border.RoundCorners;
            Controls.Add(roundCorners);

            y += 30;
            systemMenu = new CheckBox();
            systemMenu.Text = "在窗口右键菜单（系统菜单）里加“置顶窗口”";
            systemMenu.SetBounds(16, y, 340, 24);
            systemMenu.Checked = settings.ShowSystemMenuItem;
            Controls.Add(systemMenu);

            y += 30;
            borderEnabled = new CheckBox();
            borderEnabled.Text = "置顶时显示高亮边框";
            borderEnabled.SetBounds(16, y, 240, 24);
            borderEnabled.Checked = settings.EnableBorder;
            Controls.Add(borderEnabled);

            y += 40;
            Button ok = new Button();
            ok.Text = "确定";
            ok.SetBounds(230, y, 80, 28);
            ok.DialogResult = DialogResult.OK;
            ok.Click += delegate { ApplyToSettings(); };
            Controls.Add(ok);
            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.SetBounds(320, y, 80, 28);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;

            UpdateColorButton();
            UpdateEnabledState();
            accent.CheckedChanged += delegate { UpdateEnabledState(); };
        }

        private void UpdateColorButton()
        {
            colorButton.BackColor = chosenColor;
            colorButton.ForeColor = (chosenColor.R + chosenColor.G + chosenColor.B) > 380 ? Color.Black : Color.White;
        }

        private void UpdateEnabledState()
        {
            colorButton.Enabled = !accent.Checked;
        }

        private void OnHotkeyCapture(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            Keys code = e.KeyCode;
            if (code == Keys.ControlKey || code == Keys.ShiftKey || code == Keys.Menu || code == Keys.LWin || code == Keys.RWin) return;
            int mods = HotkeySpec.ModifiersFromKeys(e.Modifiers);
            if (mods == 0) { hint.Text = "至少要按住 Win / Ctrl / Alt / Shift 之一。"; return; }
            HotkeySpec spec = new HotkeySpec();
            spec.Modifiers = mods;
            spec.VirtualKey = (int)code;
            settings.Hotkey = spec;
            hotkeyBox.Text = spec.ToString();
            hint.Text = "已记录，点“确定”生效。";
        }

        private void ApplyToSettings()
        {
            settings.Border.Thickness = (int)thickness.Value;
            settings.Border.Opacity = (int)opacity.Value;
            settings.Border.UseAccentColor = accent.Checked;
            settings.Border.CustomColor = chosenColor;
            settings.Border.RoundCorners = roundCorners.Checked;
            settings.ShowSystemMenuItem = systemMenu.Checked;
            settings.EnableBorder = borderEnabled.Checked;
        }
    }

    internal sealed class TrayContext : ApplicationContext
    {
        private readonly Settings settings;
        private readonly Controller controller;
        private readonly NotifyIcon tray;
        private readonly Icon trayIcon;
        private readonly ToolStripMenuItem toggleItem;
        private readonly ToolStripMenuItem unpinAllItem;

        public TrayContext()
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            settings = new Settings(Path.Combine(dir, "AlwaysOnTop.ini"));
            controller = new Controller(settings);
            controller.PinnedSetChanged += delegate { RefreshMenuText(); };
            controller.HotkeyRegistrationFailed += delegate { ShowBalloon("快捷键注册失败", "快捷键被别的程序占用了（PowerToys 装了的话会冲突），请在“设置”里换一个。", ToolTipIcon.Warning); };
            controller.RegisterHotkey();

            trayIcon = BuildTrayIcon(settings.Border.EffectiveColor());

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = new Font("Microsoft YaHei UI", 9f);

            toggleItem = new ToolStripMenuItem("置顶/取消置顶当前窗口");
            toggleItem.ShortcutKeyDisplayString = settings.Hotkey.ToString();
            toggleItem.Click += delegate { controller.Toggle(Native.GetForegroundWindow()); };
            menu.Items.Add(toggleItem);

            unpinAllItem = new ToolStripMenuItem("取消所有置顶");
            unpinAllItem.Click += delegate { controller.UnpinAll(); };
            menu.Items.Add(unpinAllItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem settingsItem = new ToolStripMenuItem("设置…");
            settingsItem.Click += delegate { ShowSettings(); };
            menu.Items.Add(settingsItem);

            ToolStripMenuItem openIni = new ToolStripMenuItem("打开配置文件目录");
            openIni.Click += delegate
            {
                try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + settings.Path + "\""); }
                catch { }
            };
            menu.Items.Add(openIni);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += delegate { ExitThread(); };
            menu.Items.Add(exitItem);

            tray = new NotifyIcon();
            tray.Icon = trayIcon;
            tray.Text = "AlwaysOnTop（" + settings.Hotkey.ToString() + "）";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.DoubleClick += delegate { controller.Toggle(Native.GetForegroundWindow()); };

            RefreshMenuText();
            if (!controller.HotkeyRegistered)
                ShowBalloon("AlwaysOnTop 已启动", "快捷键注册失败，请在托盘图标右键 → 设置里改一个。", ToolTipIcon.Warning);
        }

        private void RefreshMenuText()
        {
            IntPtr fg = Native.GetForegroundWindow();
            bool pinned = controller.IsPinned(fg);
            toggleItem.Text = pinned ? "取消置顶当前窗口" : "置顶当前窗口";
            unpinAllItem.Enabled = controller.PinnedCount > 0;
            unpinAllItem.Text = "取消所有置顶（" + controller.PinnedCount.ToString() + "）";
        }

        private void ShowSettings()
        {
            using (SettingsForm f = new SettingsForm(settings))
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    settings.Save();
                    controller.RegisterHotkey();
                    controller.ApplyBorderSettings();
                    if (tray != null) tray.Text = "AlwaysOnTop（" + settings.Hotkey.ToString() + "）";
                    RefreshMenuText();
                }
            }
        }

        private void ShowBalloon(string title, string text, ToolTipIcon icon)
        {
            try { tray.ShowBalloonTip(4000, title, text, icon); }
            catch { }
        }

        private static Icon BuildTrayIcon(Color c)
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(c, 4f))
                        g.DrawRectangle(pen, 3, 3, 25, 25);
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                        g.FillRectangle(brush, 9, 9, 13, 13);
                }
                IntPtr h = bmp.GetHicon();
                return Icon.FromHandle(h);
            }
        }

        protected override void ExitThreadCore()
        {
            try { tray.Visible = false; tray.Dispose(); } catch { }
            try { controller.Dispose(); } catch { }
            base.ExitThreadCore();
        }
    }

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

        [STAThread]
        private static void Main(string[] args)
        {
            // 每显示器 DPI 感知（V2），失败则退回系统 DPI 感知
            try { Native.SetProcessDpiAwarenessContext(new IntPtr(-4)); }
            catch { try { Native.SetProcessDPIAware(); } catch { } }

            if (args != null && args.Length > 0 && args[0] == "--selftest")
            {
                SelfTest.Run();
                return;
            }

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++) if (args[i] == "--log") Controller.DebugEnabled = true;
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, "DshAlwaysOnTop_SingleInstance", out createdNew))
            {
                if (!createdNew) return;
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                    Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e) { CrashLog("ThreadException: " + e.Exception.ToString()); };
                    AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { CrashLog("UnhandledException: " + (e.ExceptionObject == null ? "null" : e.ExceptionObject.ToString())); };
                    CrashLog("startup ok, pid=" + System.Diagnostics.Process.GetCurrentProcess().Id);
                    Application.Run(new TrayContext());
                    CrashLog("message loop exited normally");
                }
                catch (Exception ex)
                {
                    CrashLog("FATAL in Main: " + ex.ToString());
                }
                GC.KeepAlive(mutex);
            }
        }
    }

    internal static class SelfTest
    {
        private static readonly StringBuilder Log = new StringBuilder();

        private static void W(string s)
        {
            Log.AppendLine(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + s);
        }

        public static void Run()
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            string logPath = Path.Combine(dir, "selftest.log");
            Controller c = null;
            Form probe = null;
            System.Diagnostics.Process cmdProc = null;
            try
            {
                W("=== AlwaysOnTop selftest ===");
                Settings settings = new Settings(Path.Combine(dir, "AlwaysOnTop.ini"));
                c = new Controller(settings);
                W("hotkey register: " + c.RegisterHotkey() + " (" + settings.Hotkey.ToString() + ")");

                probe = new Form();
                probe.Text = "AOT 测试窗口（自检用）";
                probe.FormBorderStyle = FormBorderStyle.Sizable;
                probe.StartPosition = FormStartPosition.Manual;
                probe.SetBounds(200, 200, 420, 300);
                probe.ShowInTaskbar = false;
                probe.Show();
                Pump(6);
                IntPtr hwnd = probe.Handle;
                W("probe hwnd: 0x" + hwnd.ToInt64().ToString("X") + " visible=" + Native.IsWindowVisible(hwnd));

                c.UpdateSystemMenuItem(hwnd);
                W("menu item inserted: " + c.HasOurMenuItem(hwnd) +
                  " count=" + Native.GetMenuItemCount(Native.GetSystemMenu(hwnd, false)).ToString());

                c.Pin(hwnd);
                int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
                W("pin -> WS_EX_TOPMOST=" + ((ex & Native.WS_EX_TOPMOST) != 0) + " isPinned=" + c.IsPinned(hwnd) + " borders=" + c.PinnedCount);
                Pump(6);
                W("border visible: " + AnyBorderVisible());

                // ---- 跟随测试：移动被测窗口，看边框窗口是否跟着走 ----
                W("border rects before move: " + DumpBorderRects());
                Native.MoveWindowForTest(hwnd, 700, 480, 520, 360);
                Pump(14);
                W("probe moved to 700,480 520x360");
                W("border rects after move : " + DumpBorderRects());
                Native.RECT pr; Native.GetWindowRectPublic(hwnd, out pr);
                W("probe rect now: " + pr.Left + "," + pr.Top + " " + pr.Width + "x" + pr.Height);

                c.Unpin(hwnd, true);
                Pump(4);
                ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
                W("unpin -> WS_EX_TOPMOST cleared=" + ((ex & Native.WS_EX_TOPMOST) == 0) + " borders=" + c.PinnedCount);
                c.UpdateSystemMenuItem(hwnd);
                W("menu item still present: " + c.HasOurMenuItem(hwnd));

                try
                {
                    cmdProc = System.Diagnostics.Process.Start("cmd.exe", "/k title AOT_PROBE");
                    IntPtr ch = IntPtr.Zero;
                    for (int i = 0; i < 40 && ch == IntPtr.Zero; i++)
                    {
                        Thread.Sleep(100);
                        if (cmdProc.HasExited) break;
                        cmdProc.Refresh();
                        ch = cmdProc.MainWindowHandle;
                    }
                    W("cmd hwnd: 0x" + ch.ToInt64().ToString("X"));
                    if (ch != IntPtr.Zero)
                    {
                        c.Pin(ch);
                        int cex = Native.GetWindowLong(ch, Native.GWL_EXSTYLE);
                        W("cmd pin -> WS_EX_TOPMOST=" + ((cex & Native.WS_EX_TOPMOST) != 0) + " borders=" + c.PinnedCount);
                        Pump(5);
                        c.Unpin(ch, true);
                        W("cmd unpin ok, borders=" + c.PinnedCount);
                    }
                }
                catch (Exception e2) { W("cmd probe skipped: " + e2.Message); }

                W("=== done ===");
            }
            catch (Exception ex)
            {
                W("EXCEPTION: " + ex.ToString());
            }
            finally
            {
                try { if (cmdProc != null && !cmdProc.HasExited) cmdProc.Kill(); } catch { }
                try { if (probe != null) { probe.Close(); probe.Dispose(); } } catch { }
                try { if (c != null) c.Dispose(); } catch { }
                try { File.WriteAllText(logPath, Log.ToString(), Encoding.UTF8); } catch { }
            }
        }

        private static void Pump(int rounds)
        {
            for (int i = 0; i < rounds; i++) { Application.DoEvents(); Thread.Sleep(80); }
        }

        private static string DumpBorderRects()
        {
            StringBuilder sb = new StringBuilder();
            uint me = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            Native.EnumWindows(delegate(IntPtr h, IntPtr l)
            {
                uint pid;
                Native.GetWindowThreadProcessId(h, out pid);
                if (pid != me) return true;
                int ex = Native.GetWindowLong(h, Native.GWL_EXSTYLE);
                if ((ex & Native.WS_EX_LAYERED) == 0) return true;
                Native.RECT r;
                Native.GetWindowRectPublic(h, out r);
                sb.Append("[vis=").Append(Native.IsWindowVisible(h)).Append(" ").Append(r.Left).Append(",").Append(r.Top).Append(" ").Append(r.Width).Append("x").Append(r.Height).Append("] ");
                return true;
            }, IntPtr.Zero);
            return sb.Length == 0 ? "(无分层窗口)" : sb.ToString();
        }

        private static bool AnyBorderVisible()
        {
            foreach (Form f in Application.OpenForms) if (f is BorderForm && f.Visible) return true;
            return false;
        }
    }
}
