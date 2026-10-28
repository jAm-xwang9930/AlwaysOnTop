using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DshAlwaysOnTop
{
    // ============================ 快捷键 ============================
    internal sealed class HotkeySpec
    {
        public uint Modifiers;
        public uint Key;

        public HotkeySpec() { }
        public HotkeySpec(uint mods, uint key) { Modifiers = mods; Key = key; }

        public static HotkeySpec Default() { return new HotkeySpec(Native.MOD_WIN | Native.MOD_CONTROL, 0x54); }

        public bool IsValid { get { return Key != 0; } }

        public bool SameAs(HotkeySpec o)
        {
            return o != null && o.Modifiers == Modifiers && o.Key == Key;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if ((Modifiers & Native.MOD_WIN) != 0) sb.Append("Win + ");
            if ((Modifiers & Native.MOD_CONTROL) != 0) sb.Append("Ctrl + ");
            if ((Modifiers & Native.MOD_ALT) != 0) sb.Append("Alt + ");
            if ((Modifiers & Native.MOD_SHIFT) != 0) sb.Append("Shift + ");
            sb.Append(KeyName(Key));
            return sb.ToString();
        }

        public static string KeyName(uint vk)
        {
            if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
            if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
            if (vk >= 0x70 && vk <= 0x87) return "F" + ((vk - 0x6F).ToString());
            switch (vk)
            {
                case 0x08: return "Backspace";
                case 0x09: return "Tab";
                case 0x0D: return "Enter";
                case 0x1B: return "Esc";
                case 0x20: return "Space";
                case 0x21: return "PageUp";
                case 0x22: return "PageDown";
                case 0x23: return "End";
                case 0x24: return "Home";
                case 0x25: return "Left";
                case 0x26: return "Up";
                case 0x27: return "Right";
                case 0x28: return "Down";
                case 0x2D: return "Insert";
                case 0x2E: return "Delete";
                case 0xBA: return ";";
                case 0xBB: return "=";
                case 0xBC: return ",";
                case 0xBD: return "-";
                case 0xBE: return ".";
                case 0xBF: return "/";
                case 0xC0: return "`";
                case 0xDB: return "[";
                case 0xDC: return "\\";
                case 0xDD: return "]";
                case 0xDE: return "'";
            }
            return "0x" + vk.ToString("X2");
        }

        private static bool TryParseKey(string name, out uint vk)
        {
            vk = 0;
            if (name.Length == 0) return false;
            if (name.Length == 1)
            {
                char c = char.ToUpperInvariant(name[0]);
                if (c >= 'A' && c <= 'Z') { vk = (uint)c; return true; }
                if (c >= '0' && c <= '9') { vk = (uint)c; return true; }
            }
            string up = name.ToUpperInvariant();
            if (up.Length >= 2 && up[0] == 'F' && char.IsDigit(up[1]))
            {
                int n;
                if (int.TryParse(up.Substring(1), out n) && n >= 1 && n <= 24) { vk = (uint)(0x6F + n); return true; }
            }
            switch (up)
            {
                case "SPACE": vk = 0x20; return true;
                case "TAB": vk = 0x09; return true;
                case "ENTER": vk = 0x0D; return true;
                case "ESC":
                case "ESCAPE": vk = 0x1B; return true;
                case "BACKSPACE": vk = 0x08; return true;
                case "INSERT": vk = 0x2D; return true;
                case "DELETE":
                case "DEL": vk = 0x2E; return true;
                case "HOME": vk = 0x24; return true;
                case "END": vk = 0x23; return true;
                case "PAGEUP":
                case "PGUP": vk = 0x21; return true;
                case "PAGEDOWN":
                case "PGDN": vk = 0x22; return true;
                case "UP": vk = 0x26; return true;
                case "DOWN": vk = 0x28; return true;
                case "LEFT": vk = 0x25; return true;
                case "RIGHT": vk = 0x27; return true;
                case ";": vk = 0xBA; return true;
                case "=": vk = 0xBB; return true;
                case ",": vk = 0xBC; return true;
                case "-": vk = 0xBD; return true;
                case ".": vk = 0xBE; return true;
                case "/": vk = 0xBF; return true;
                case "`": vk = 0xC0; return true;
                case "[": vk = 0xDB; return true;
                case "\\": vk = 0xDC; return true;
                case "]": vk = 0xDD; return true;
                case "'": vk = 0xDE; return true;
            }
            return false;
        }

        public static HotkeySpec Parse(string s)
        {
            if (s == null) return Default();
            string[] parts = s.Split(new char[] { '+', '＋' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return Default();
            uint mods = 0;
            uint key = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length == 0) continue;
                string up = p.ToUpperInvariant();
                if (up == "WIN" || up == "WINDOWS" || up == "META" || up == "WINKEY") { mods |= Native.MOD_WIN; continue; }
                if (up == "CTRL" || up == "CONTROL") { mods |= Native.MOD_CONTROL; continue; }
                if (up == "ALT") { mods |= Native.MOD_ALT; continue; }
                if (up == "SHIFT") { mods |= Native.MOD_SHIFT; continue; }
                uint k;
                if (TryParseKey(p, out k)) key = k;
            }
            if (key == 0) return Default();
            return new HotkeySpec(mods, key);
        }

        public static HotkeySpec FromKeys(Keys kc, Keys mods)
        {
            uint m = 0;
            if ((mods & Keys.Control) != 0) m |= Native.MOD_CONTROL;
            if ((mods & Keys.Alt) != 0) m |= Native.MOD_ALT;
            if ((mods & Keys.Shift) != 0) m |= Native.MOD_SHIFT;
            int vk = (int)kc;
            if (vk == 0x11 || vk == 0x10 || vk == 0x12 || vk == 0x5B || vk == 0x5C || vk == 0xA0 || vk == 0xA1 ||
                vk == 0xA2 || vk == 0xA3 || vk == 0xA4 || vk == 0xA5 || vk == 0x5D) return null;
            return new HotkeySpec(m, (uint)vk);
        }
    }

    // ============================ 设置 ============================
    internal sealed class Settings
    {
        public HotkeySpec Hotkey = HotkeySpec.Default();
        public int Thickness = 3;
        public int Opacity = 100;
        public bool FollowAccent = true;
        public Color CustomColor = Color.FromArgb(100, 116, 139);
        public bool RoundCorners = true;
        public bool ShowBorder = true;
        public bool ShowSystemMenu = false;   // 已停用：右键菜单功能下线
        public bool PlaySound = true;
        public bool AutoStart = false;
        public bool CloseToTray = true;
        public string FilePath = "";

        public static string DefaultPath()
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(dir, "AlwaysOnTop.ini");
        }

        public static Settings Load(string path)
        {
            Settings s = new Settings();
            s.FilePath = path;
            try
            {
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string ln = lines[i].Trim();
                        if (ln.Length == 0 || ln.StartsWith(";") || ln.StartsWith("#")) continue;
                        int eq = ln.IndexOf('=');
                        if (eq <= 0) continue;
                        string k = ln.Substring(0, eq).Trim();
                        string v = ln.Substring(eq + 1).Trim();
                        int semi = v.IndexOf(';');
                        if (semi >= 0) v = v.Substring(0, semi).Trim();
                        s.Apply(k, v);
                    }
                }
            }
            catch { }
            return s;
        }

        private void Apply(string k, string v)
        {
            try
            {
                switch (k.ToLowerInvariant())
                {
                    case "hotkey": Hotkey = HotkeySpec.Parse(v); break;
                    case "thickness": Thickness = Clamp(Int(v, Thickness), 1, 10); break;
                    case "opacity": Opacity = Clamp(Int(v, Opacity), 20, 100); break;
                    case "accent": FollowAccent = Bool(v, FollowAccent); break;
                    case "color": try { CustomColor = ColorTranslator.FromHtml(v); } catch { } break;
                    case "roundcorners": RoundCorners = Bool(v, RoundCorners); break;
                    case "border": ShowBorder = Bool(v, ShowBorder); break;
                    case "sound": PlaySound = Bool(v, PlaySound); break;
                    case "autostart": AutoStart = Bool(v, AutoStart); break;
                    case "closetotray": CloseToTray = Bool(v, CloseToTray); break;
                }
            }
            catch { }
        }

        private static int Int(string v, int def)
        {
            int r;
            if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out r)) return r;
            return def;
        }

        private static bool Bool(string v, bool def)
        {
            if (v == "1" || v.ToLowerInvariant() == "true" || v.ToLowerInvariant() == "yes") return true;
            if (v == "0" || v.ToLowerInvariant() == "false" || v.ToLowerInvariant() == "no") return false;
            return def;
        }

        private static int Clamp(int v, int lo, int hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; AlwaysOnTop 配置文件（界面里改会自动保存，也可手动改后重启）");
                sb.AppendLine("Hotkey=" + Hotkey.ToString());
                sb.AppendLine("Thickness=" + Thickness.ToString());
                sb.AppendLine("Opacity=" + Opacity.ToString());
                sb.AppendLine("Accent=" + (FollowAccent ? "1" : "0"));
                sb.AppendLine("Color=" + ToHex(CustomColor));
                sb.AppendLine("RoundCorners=" + (RoundCorners ? "1" : "0"));
                sb.AppendLine("Border=" + (ShowBorder ? "1" : "0"));
                sb.AppendLine("SystemMenu=" + (ShowSystemMenu ? "1" : "0"));
                sb.AppendLine("Sound=" + (PlaySound ? "1" : "0"));
                sb.AppendLine("AutoStart=" + (AutoStart ? "1" : "0"));
                sb.AppendLine("CloseToTray=" + (CloseToTray ? "1" : "0"));
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        public static string ToHex(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }

    // ============================ 主题 ============================
    internal sealed class Theme
    {
        public bool Dark;
        public Color Window;
        public Color Card;
        public Color CardBorder;
        public Color Text;
        public Color SubText;
        public Color Field;
        public Color FieldBorder;
        public Color Track;
        public Color Hover;
        public Color Accent;
        public Color Title;
        public Color SwitchOff;

        public static bool ForceDark = false;

        public static Theme Current()
        {
            Theme t = new Theme();
            t.Dark = false;
            t.Accent = Color.FromArgb(100, 116, 139);
            t.Title = t.Accent;
            if (t.Dark)
            {
                t.Window = Color.FromArgb(32, 32, 32);
                t.Card = Color.FromArgb(44, 44, 44);
                t.CardBorder = Color.FromArgb(58, 58, 58);
                t.Text = Color.FromArgb(243, 243, 243);
                t.SubText = Color.FromArgb(160, 160, 160);
                t.Field = Color.FromArgb(56, 56, 56);
                t.FieldBorder = Color.FromArgb(78, 78, 78);
                t.Track = Color.FromArgb(80, 80, 80);
                t.Hover = Color.FromArgb(60, 60, 60);
                t.SwitchOff = Color.FromArgb(95, 95, 95);
            }
            else
            {
                t.Window = Color.FromArgb(243, 243, 243);
                t.Card = Color.FromArgb(255, 255, 255);
                t.CardBorder = Color.FromArgb(229, 229, 229);
                t.Text = Color.Black;
                t.SubText = Color.Black;
                t.Field = Color.FromArgb(251, 251, 251);
                t.FieldBorder = Color.FromArgb(214, 214, 214);
                t.Track = Color.FromArgb(200, 200, 200);
                t.Hover = Color.FromArgb(243, 243, 243);
                t.SwitchOff = Color.FromArgb(140, 140, 140);
            }
            return t;
        }

        public static bool IsDarkMode()
        {
            try
            {
                object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                if (v is int) return ((int)v) == 0;
            }
            catch { }
            return false;
        }

        public static Color AccentColor()
        {
            try
            {
                object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColor", null);
                if (v is int)
                {
                    int d = (int)v;
                    int r = (d >> 16) & 0xFF;
                    int g = (d >> 8) & 0xFF;
                    int b = d & 0xFF;
                    Color c = Color.FromArgb(r, g, b);
                    if (r + g + b > 40) return c;
                }
            }
            catch { }
            return Color.FromArgb(0, 120, 212);
        }

        public Color AccentOr(Color custom, bool followAccent)
        {
            return followAccent ? Accent : custom;
        }
    }

    // ============================ 图标 ============================
    internal static class IconStore
    {
        private static Icon trayIcon;
        private static Icon winIcon;

        public static Icon Tray
        {
            get
            {
                if (trayIcon == null) trayIcon = Load(20);
                return trayIcon;
            }
        }

        public static Icon Window
        {
            get
            {
                if (winIcon == null) winIcon = Load(32);
                return winIcon;
            }
        }

        private static Icon Load(int size)
        {
            try
            {
                Stream s = typeof(IconStore).Assembly.GetManifestResourceStream("AlwaysOnTop.app.ico");
                if (s != null)
                {
                    Icon ic = new Icon(s, size, size);
                    s.Close();
                    return ic;
                }
            }
            catch { }
            try
            {
                Icon exe = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (exe != null) return exe;
            }
            catch { }
            return SystemIcons.Application;
        }

        public static Bitmap ToBitmap(int size)
        {
            try
            {
                using (Icon ic = Load(size)) return ic.ToBitmap();
            }
            catch { return null; }
        }
    }

    // ============================ 提示音 ============================
    internal static class Sound
    {
        public static bool Enabled = true;
        private static byte[] pinWav;
        private static byte[] unpinWav;

        public static void Pin() { if (Enabled) Play(true); }
        public static void Unpin() { if (Enabled) Play(false); }

        private static void Play(bool pin)
        {
            try
            {
                if (pin)
                {
                    if (pinWav == null) pinWav = Tone(1046, 45);
                    Native.PlaySound(pinWav, IntPtr.Zero, Native.SND_MEMORY | Native.SND_ASYNC | Native.SND_NODEFAULT);
                }
                else
                {
                    if (unpinWav == null) unpinWav = Tone(700, 45);
                    Native.PlaySound(unpinWav, IntPtr.Zero, Native.SND_MEMORY | Native.SND_ASYNC | Native.SND_NODEFAULT);
                }
            }
            catch { }
        }

        private static byte[] Tone(int freq, int ms)
        {
            int rate = 22050;
            int n = rate * ms / 1000;
            int bytes = n * 2;
            MemoryStream ms2 = new MemoryStream(44 + bytes);
            BinaryWriter w = new BinaryWriter(ms2);
            w.Write(new char[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + bytes);
            w.Write(new char[] { 'W', 'A', 'V', 'E' });
            w.Write(new char[] { 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(rate);
            w.Write(rate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(new char[] { 'd', 'a', 't', 'a' });
            w.Write(bytes);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / rate;
                double env = 1.0;
                int fade = n / 6;
                if (fade > 0)
                {
                    if (i < fade) env = (double)i / fade;
                    else if (i > n - fade) env = (double)(n - i) / fade;
                }
                double v = Math.Sin(2 * Math.PI * freq * t) * env * 0.22;
                w.Write((short)(v * 32767));
            }
            w.Flush();
            byte[] data = ms2.ToArray();
            w.Dispose();
            ms2.Dispose();
            return data;
        }
    }

    // ============================ 开机自启（启动文件夹快捷方式） ============================
    internal static class AutoStartUtil
    {
        public static string ShortcutPath()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(dir, "AlwaysOnTop.lnk");
        }

        public static bool IsEnabled()
        {
            try { return File.Exists(ShortcutPath()); }
            catch { return false; }
        }

        public static void Set(bool on)
        {
            string lnk = ShortcutPath();
            if (!on)
            {
                if (File.Exists(lnk)) File.Delete(lnk);
                return;
            }
            object shell = null;
            try
            {
                Type t = Type.GetTypeFromProgID("WScript.Shell");
                if (t == null) throw new InvalidOperationException("WScript.Shell 不可用");
                shell = Activator.CreateInstance(t);
                object sc = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnk });
                Type st = sc.GetType();
                st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { Application.ExecutablePath });
                st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { Path.GetDirectoryName(Application.ExecutablePath) });
                st.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { Application.ExecutablePath + ",0" });
                st.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { "AlwaysOnTop 窗口置顶工具" });
                st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
            }
            finally
            {
                if (shell != null)
                {
                    try { Marshal.ReleaseComObject(shell); }
                    catch { }
                }
            }
        }
    }

    // ============================ 置顶边框 ============================
    // ============================ 虚拟桌面 ============================
    [ComImport, Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVirtualDesktopManager
    {
        int IsWindowOnCurrentVirtualDesktop([In] IntPtr topLevelWindow, [Out] out int onCurrentDesktop);
        int GetWindowDesktopId([In] IntPtr topLevelWindow, [Out] out Guid desktopId);
        int MoveWindowToDesktop([In] IntPtr topLevelWindow, [In] ref Guid desktopId);
    }

    [ComImport, Guid("AA509086-5CA9-4C25-8F95-589D3C07B48A")]
    internal class CVirtualDesktopManager { }

    internal static class VirtualDesktop
    {
        private static IVirtualDesktopManager mgr;
        private static bool tried;

        public static bool IsOnCurrent(IntPtr h)
        {
            if (!tried)
            {
                tried = true;
                try { mgr = (IVirtualDesktopManager)new CVirtualDesktopManager(); }
                catch { mgr = null; }
            }
            if (mgr == null) return true;
            try
            {
                int on;
                if (mgr.IsWindowOnCurrentVirtualDesktop(h, out on) == 0) return on != 0;
            }
            catch { }
            return true;
        }
    }
    internal sealed class BorderForm : Form
    {
        private readonly IntPtr target;
        private readonly Controller owner;
        public bool FastMode = false;

        private Bitmap bmp;
        private IntPtr hBmp = IntPtr.Zero;
        private int bmpW = -1;
        private int bmpH = -1;
        private string lastSig = "";
        private Rectangle lastDst = Rectangle.Empty;
        private string lastState = "";
        private bool placed;

        public BorderForm(IntPtr target, Controller owner)
        {
            this.target = target;
            this.owner = owner;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque, true);
            Show();
            Native.SetExcludedFromPeek(Handle);
        }

        public IntPtr Target { get { return target; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style = Native.WS_POPUP | Native.WS_DISABLED;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override void Dispose(bool disposing)
        {
            ReleaseHBitmap();
            if (bmp != null) { bmp.Dispose(); bmp = null; }
            base.Dispose(disposing);
        }

        private void ReleaseHBitmap()
        {
            if (hBmp != IntPtr.Zero)
            {
                try { Native.DeleteObject(hBmp); } catch { }
                hBmp = IntPtr.Zero;
            }
        }

        public void UpdateToTarget()
        {
            string st = !Native.IsWindow(target) ? "目标句柄无效"
                      : (!Native.IsWindowVisible(target) ? "目标不可见"
                      : (Native.IsIconic(target) ? "目标最小化" : "ok"));
            if (st != lastState)
            {
                lastState = st;
                Program.CrashLog("border state=" + st + " | " + Controller.Describe(target));
            }
            if (st != "ok")
            {
                if (Visible) Visible = false;
                return;
            }
            int cloaked;
            if (Native.DwmGetWindowAttributeInt(target, Native.DWMWA_CLOAKED, out cloaked, 4) == 0 && cloaked != 0)
            {
                if (Visible) Visible = false;
                return;
            }

            Native.RECT r;
            int hr = Native.DwmGetWindowAttribute(target, Native.DWMWA_EXTENDED_FRAME_BOUNDS, out r,
                Marshal.SizeOf(typeof(Native.RECT)));
            if (hr != 0)
            {
                if (!Native.GetWindowRect(target, out r)) return;
            }

            int th = Math.Max(1, owner.Settings.Thickness);
            int op = Math.Max(20, Math.Min(100, owner.Settings.Opacity));
            Rectangle dst = new Rectangle(r.Left - th, r.Top - th, r.Width + 2 * th, r.Height + 2 * th);
            if (dst.Width < 8 || dst.Height < 8) return;

            string sig = th + "|" + op + "|" + owner.CurrentCornerRadius(target) + "|" +
                         owner.CurrentBorderColor().ToArgb() + "|" + dst.Width + "x" + dst.Height;
            bool sigChanged = (sig != lastSig);
            bool moved = (dst != lastDst);
            if (!sigChanged && !moved)
            {
                if (!Visible) Visible = true;
                return;
            }
            lastDst = dst;
            if (sigChanged)
            {
                lastSig = sig;
                Rebuild(dst.Size, th);
            }
            Apply(dst, op);

            if (!Visible) Visible = true;
            if (!placed)
            {
                placed = true;
                Native.SetWindowPos(Handle, target, 0, 0, 0, 0,
                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            }
            if (!FastMode)
            {
                Program.CrashLog("border pos -> form=" + dst.Left + "," + dst.Top + " " + dst.Width + "x" + dst.Height +
                                 " target=" + r.Left + "," + r.Top + " " + r.Width + "x" + r.Height +
                                 " th=" + th + " radius=" + owner.CurrentCornerRadius(target));
            }
        }

        // 生成 per-pixel alpha 位图（照 PT FrameDrawer 的环带几何）
        private void Rebuild(Size sz, int th)
        {
            if (bmp == null || bmpW != sz.Width || bmpH != sz.Height)
            {
                ReleaseHBitmap();
                if (bmp != null) bmp.Dispose();
                bmp = new Bitmap(sz.Width, sz.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                bmpW = sz.Width;
                bmpH = sz.Height;
            }
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.CompositingQuality = CompositingQuality.HighQuality;
                Color c = owner.CurrentBorderColor();
                if (c.R < 20 && c.G < 20 && c.B < 20) c = Color.FromArgb(0, 120, 212);

                float ht = th / 2f;
                float radius = owner.CurrentCornerRadius(target);
                float l = ht + 1f;
                float t = ht + 1f;
                float rr = sz.Width - ht - 1f;
                float bb = sz.Height - ht - 1f;
                if (rr - l < 2f || bb - t < 2f) return;

                RectangleF outer = RectangleF.FromLTRB(l - ht, t - ht, rr + ht, bb + ht);
                RectangleF inner = RectangleF.FromLTRB(l + ht, t + ht, rr - ht, bb - ht);
                float rOut = radius > 0f ? radius + ht : 0f;
                float rIn = radius > 0f ? Math.Max(radius - ht, 0f) : 0f;

                using (GraphicsPath p = new GraphicsPath(FillMode.Alternate))
                {
                    Shapes.AddRounded(p, outer, rOut);
                    if (inner.Width > 1f && inner.Height > 1f) Shapes.AddRounded(p, inner, rIn);
                    using (SolidBrush br = new SolidBrush(c)) g.FillPath(br, p);
                }
            }
            hBmp = bmp.GetHbitmap(Color.FromArgb(0, 0, 0, 0));
        }

        private void Apply(Rectangle dst, int op)
        {
            if (hBmp == IntPtr.Zero) return;
            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero) return;
            IntPtr memDc = Native.CreateCompatibleDC(screenDc);
            if (memDc == IntPtr.Zero) { Native.ReleaseDC(IntPtr.Zero, screenDc); return; }
            IntPtr oldObj = Native.SelectObject(memDc, hBmp);
            try
            {
                Native.POINT pSrc = new Native.POINT();
                Native.POINT pDst = new Native.POINT();
                Native.SIZE sSize = new Native.SIZE();
                Native.BLENDFUNCTION bf = new Native.BLENDFUNCTION();
                pSrc.x = 0; pSrc.y = 0;
                pDst.x = dst.Left; pDst.y = dst.Top;
                sSize.cx = dst.Width; sSize.cy = dst.Height;
                bf.BlendOp = Native.AC_SRC_OVER;
                bf.BlendFlags = 0;
                bf.SourceConstantAlpha = (byte)(op * 255 / 100);
                bf.AlphaFormat = Native.AC_SRC_ALPHA;
                Native.UpdateLayeredWindow(Handle, screenDc, ref pDst, ref sSize, memDc, ref pSrc, 0, ref bf, Native.ULW_ALPHA);
            }
            finally
            {
                Native.SelectObject(memDc, oldObj);
                Native.DeleteDC(memDc);
                Native.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_NCHITTEST) { m.Result = (IntPtr)(-1); return; }
            if (m.Msg == 0x000F) { return; }   // WM_PAINT：内容由 UpdateLayeredWindow 提供
            if (m.Msg == 0x0014) { m.Result = (IntPtr)1; return; }
            if (m.Msg == 0x0020) { m.Result = (IntPtr)1; return; }
            base.WndProc(ref m);
        }
    }
    // ============================ 圆角路径工具 ============================
    internal static class Shapes
    {
        public static void AddRounded(GraphicsPath p, RectangleF r, float radius)
        {
            if (radius <= 0.5f || r.Width < 4f || r.Height < 4f)
            {
                p.AddRectangle(r);
                return;
            }
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
        }

        public static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0 || r.Width < 4 || r.Height < 4)
            {
                p.AddRectangle(r);
                return p;
            }
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // ============================ 置顶信息 ============================
    internal sealed class PinInfo
    {
        public IntPtr Handle;
        public string Title;
        public string Process;
    }

    // ============================ 核心控制器 ============================
    internal sealed class Controller : IDisposable
    {
        public const int HotkeyId = 0xA011;
        public const int MenuCommandId = 0xA021;
        private static readonly IntPtr MenuOwnerTag = new IntPtr(0x414F54);
        private const string PropName = "DshAotPinned";
        private const string MenuText = "置顶窗口(&A)";

        public static bool DebugEnabled = false;
        private static readonly object LogLock = new object();

        private readonly Settings settings;
        private readonly MessageWindow msg;
        private readonly System.Windows.Forms.Timer timer;
        private readonly HashSet<IntPtr> pinned = new HashSet<IntPtr>();
        private readonly Dictionary<IntPtr, BorderForm> borders = new Dictionary<IntPtr, BorderForm>();
        private Native.WinEventDelegate hookProc;
        private IntPtr hook = IntPtr.Zero;
        private IntPtr hookMove = IntPtr.Zero;
        private IntPtr hookDrag = IntPtr.Zero;
        private IntPtr hookMin = IntPtr.Zero;
        private IntPtr hookFocus = IntPtr.Zero;
        private IntPtr hookFg = IntPtr.Zero;
        private IntPtr lastToggleHwnd = IntPtr.Zero;
        private DateTime lastToggleTime = DateTime.MinValue;
        private bool fastMode = false;
        private DateTime fastSince = DateTime.MinValue;
        private IntPtr lastMenuHwnd = IntPtr.Zero;
        private HotkeySpec registered;

        public string HotkeyError = "";
        public event EventHandler Changed;

        public Controller(Settings s)
        {
            settings = s;
            Sound.Enabled = s.PlaySound;
            msg = new MessageWindow(this);
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 80;
            timer.Tick += OnTick;
            timer.Start();
            hookProc = OnWinEvent;
            hook = Native.SetWinEventHook(Native.EVENT_OBJECT_INVOKED, Native.EVENT_OBJECT_INVOKED,
                IntPtr.Zero, hookProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS);
            hookMove = Native.SetWinEventHook(Native.EVENT_OBJECT_LOCATIONCHANGE, Native.EVENT_OBJECT_LOCATIONCHANGE,
                IntPtr.Zero, hookProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS);
            hookDrag = Native.SetWinEventHook(Native.EVENT_SYSTEM_MOVESIZESTART, Native.EVENT_SYSTEM_MOVESIZEEND,
                IntPtr.Zero, hookProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT);
            hookMin = Native.SetWinEventHook(Native.EVENT_SYSTEM_MINIMIZESTART, Native.EVENT_SYSTEM_MINIMIZEEND,
                IntPtr.Zero, hookProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS);
            hookFocus = Native.SetWinEventHook(Native.EVENT_OBJECT_FOCUS, Native.EVENT_OBJECT_FOCUS,
                IntPtr.Zero, hookProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS);
            hookFg = Native.SetWinEventHook(Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, hookProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS);
            Program.CrashLog("hooks: invoked/move/drag/min/focus/fg ok=" +
                (hook != IntPtr.Zero) + "," + (hookMove != IntPtr.Zero) + "," + (hookDrag != IntPtr.Zero) + "," +
                (hookMin != IntPtr.Zero) + "," + (hookFocus != IntPtr.Zero) + "," + (hookFg != IntPtr.Zero) +
                "");
            AdoptExistingPins();
            Dbg("controller created");
        }

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

        public Settings Settings { get { return settings; } }
        public int PinnedCount { get { return pinned.Count; } }

        private void Raise()
        {
            if (Changed != null) Changed(this, EventArgs.Empty);
        }

        // ---------- 快捷键 ----------
        public bool RegisterHotkey()
        {
            UnregisterHotkey();
            if (settings.Hotkey == null || !settings.Hotkey.IsValid) { HotkeyError = "快捷键无效"; return false; }
            bool ok = Native.RegisterHotKey(msg.Handle, HotkeyId,
                settings.Hotkey.Modifiers | Native.MOD_NOREPEAT, settings.Hotkey.Key);
            if (ok)
            {
                registered = new HotkeySpec(settings.Hotkey.Modifiers, settings.Hotkey.Key);
                HotkeyError = "";
            }
            else
            {
                registered = null;
                HotkeyError = "这个组合被占用了（常见原因：PowerToys 的 Always on Top），换一个组合试试";
            }
            Dbg("hotkey register " + settings.Hotkey.ToString() + " -> " + ok);
            return ok;
        }

        public bool HotkeyRegistered { get { return registered != null; } }

        private void UnregisterHotkey()
        {
            try { Native.UnregisterHotKey(msg.Handle, HotkeyId); }
            catch { }
            registered = null;
        }

        internal static string Describe(IntPtr h)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("hwnd=0x").Append(h.ToInt64().ToString("X"));
            if (!Native.IsWindow(h)) { sb.Append(" 句柄无效"); return sb.ToString(); }
            Native.RECT r;
            Native.GetWindowRect(h, out r);
            sb.Append(" cls=").Append(Native.ClassName(h));
            sb.Append(" title=").Append(Native.WindowTitle(h));
            sb.Append(" pid=").Append(Native.ProcessName(h));
            sb.Append(" rect=").Append(r.Left).Append(",").Append(r.Top).Append(" ")
              .Append(r.Width).Append("x").Append(r.Height);
            sb.Append(" vis=").Append(Native.IsWindowVisible(h));
            sb.Append(" icon=").Append(Native.IsIconic(h));
            sb.Append(" topmost=").Append(Native.IsTopmost(h));
            return sb.ToString();
        }

        internal void ToggleForeground()
        {
            IntPtr fg = Native.GetForegroundWindow();
            Program.CrashLog("hotkey: fg -> " + Describe(fg) + " own=" + (fg != IntPtr.Zero && IsOwnWindow(fg)));
            if (fg == IntPtr.Zero || IsOwnWindow(fg)) return;
            int qs;
            if (Native.SHQueryUserNotificationState(out qs) == 0 && qs == 3)
            {
                Program.CrashLog("hotkey: 前台是全屏直连(D3D)程序，按 PT 的 blockInGameMode 跳过");
                return;
            }
            Toggle(fg);
        }

        // ---------- 置顶 ----------
        public bool IsPinned(IntPtr h)
        {
            if (pinned.Contains(h)) return true;
            return Native.IsWindow(h) && Native.GetProp(h, PropName) != IntPtr.Zero;
        }

        public void Toggle(IntPtr h)
        {
            if (IsPinned(h)) Unpin(h, true);
            else Pin(h);
        }

        public void Pin(IntPtr h)
        {
            if (h == IntPtr.Zero || !Native.IsWindow(h)) return;
            if (IsPinned(h)) return;
            bool setOk = Native.SetTopmostEx(h, true);
            int err = Marshal.GetLastWin32Error();
            Program.CrashLog("pin: " + Describe(h) + " | SetWindowPos=" + setOk +
                             " err=" + err + " 读回topmost=" + Native.IsTopmost(h));
            if (!setOk)
            {
                Program.LastError = (err == 5)
                    ? "无法置顶：目标程序权限高于本程序（错误 5），请从资源管理器双击启动 AlwaysOnTop"
                    : ("无法置顶：SetWindowPos 失败，错误码 " + err);
                Program.CrashLog("pin fail -> " + Program.LastError);
                Raise();
                return;
            }
            Program.LastError = "";
            Native.SetProp(h, PropName, new IntPtr(1));
            pinned.Add(h);
            EnsureBorder(h);
            EnsureMenu(h);
            Sound.Pin();
            Raise();
            Dbg("pin 0x" + h.ToInt64().ToString("X"));
        }

        public void Unpin(IntPtr h, bool userAction)
        {
            bool was = pinned.Contains(h);
            pinned.Remove(h);
            if (Native.IsWindow(h))
            {
                Native.RemoveProp(h, PropName);
                bool unOk = Native.SetTopmostEx(h, false);
                Program.CrashLog("unpin: " + Describe(h) + " | SetWindowPos=" + unOk +
                                 " err=" + Marshal.GetLastWin32Error() + " 读回topmost=" + Native.IsTopmost(h));
                try { EnsureMenu(h); }
                catch { }
            }
            RemoveBorder(h);
            if (userAction && was) Sound.Unpin();
            if (was) Raise();
            Dbg("unpin 0x" + h.ToInt64().ToString("X"));
        }

        public void UnpinAll()
        {
            List<IntPtr> all = new List<IntPtr>(pinned);
            for (int i = 0; i < all.Count; i++) Unpin(all[i], false);
            if (all.Count > 0) Sound.Unpin();
            Raise();
        }

        public List<PinInfo> Enumerate()
        {
            List<PinInfo> list = new List<PinInfo>();
            foreach (IntPtr h in new List<IntPtr>(pinned))
            {
                if (!Native.IsWindow(h)) continue;
                PinInfo p = new PinInfo();
                p.Handle = h;
                p.Title = Native.WindowTitle(h);
                if (p.Title.Length == 0) p.Title = "(无标题窗口)";
                p.Process = Native.ProcessName(h);
                list.Add(p);
            }
            return list;
        }

        // ---------- 边框 ----------
        private void EnsureBorder(IntPtr h)
        {
            if (!settings.ShowBorder) return;
            if (borders.ContainsKey(h)) return;
            if (!VirtualDesktop.IsOnCurrent(h))
            {
                Program.CrashLog("border: 目标不在当前虚拟桌面，暂不画");
                return;
            }
            try
            {
                Program.CrashLog("border: create for " + Describe(h));
                BorderForm b = new BorderForm(h, this);
                borders[h] = b;
                b.UpdateToTarget();
            }
            catch (Exception ex)
            {
                Dbg("border fail: " + ex.Message);
            }
        }

        private void RemoveBorder(IntPtr h)
        {
            BorderForm b;
            if (borders.TryGetValue(h, out b))
            {
                borders.Remove(h);
                try { b.Hide(); b.Close(); b.Dispose(); }
                catch { }
            }
        }

        public Color CurrentBorderColor()
        {
            return Theme.AccentColor();
        }

        public int CurrentCornerRadius(IntPtr h)
        {
            if (!settings.RoundCorners) return 0;
            if (Native.IsZoomed(h)) return 0;
            int pref = Native.DWMWCP_DEFAULT;
            Native.DwmGetWindowAttributeInt(h, Native.DWMWA_WINDOW_CORNER_PREFERENCE, out pref, 4);
            if (pref == Native.DWMWCP_DONOTROUND) return 0;
            int dpi = 96;
            try
            {
                int d = Native.GetDpiForWindow(h);
                if (d > 0) dpi = d;
            }
            catch { }
            double scale = dpi / 96.0;
            if (pref == Native.DWMWCP_ROUNDSMALL) return (int)Math.Round(4 * scale);
            return (int)Math.Round(8 * scale);
        }

        // ---------- 系统菜单项 ----------
        private bool IsOwnWindow(IntPtr h)
        {
            uint pid;
            Native.GetWindowThreadProcessId(h, out pid);
            return pid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        private bool Eligible(IntPtr h)
        {
            if (h == IntPtr.Zero || !Native.IsWindow(h) || !Native.IsWindowVisible(h)) return false;
            if (IsOwnWindow(h)) return false;
            string cls = Native.ClassName(h);
            if (cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" || cls == "Progman" ||
                cls == "WorkerW" || cls == "NotifyIconOverflowWindow" || cls == "Windows.UI.Core.CoreWindow") return false;
            if (Native.GetWindowLong(h, Native.GWL_STYLE) == 0) return false;
            return true;
        }

        private static bool HasOurItem(IntPtr menu)
        {
            int n = Native.GetMenuItemCount(menu);
            for (int i = 0; i < n; i++)
            {
                Native.MENUITEMINFO mi = new Native.MENUITEMINFO();
                mi.cbSize = Marshal.SizeOf(typeof(Native.MENUITEMINFO));
                mi.fMask = Native.MIIM_ID | Native.MIIM_DATA;
                if (Native.GetMenuItemInfo(menu, (uint)i, true, ref mi))
                {
                    if (mi.wID == MenuCommandId) return true;
                }
            }
            return false;
        }

        public void EnsureMenu(IntPtr h)
        {
            if (!settings.ShowSystemMenu) return;
            if (!Eligible(h)) return;
            IntPtr menu = Native.GetSystemMenu(h, false);
            if (menu == IntPtr.Zero) return;
            int state = Native.MFS_ENABLED | (IsPinned(h) ? Native.MFS_CHECKED : 0);
            if (!HasOurItem(menu))
            {
                Native.MENUITEMINFO mii = new Native.MENUITEMINFO();
                mii.cbSize = Marshal.SizeOf(typeof(Native.MENUITEMINFO));
                mii.fMask = Native.MIIM_ID | Native.MIIM_STATE | Native.MIIM_STRING | Native.MIIM_DATA;
                mii.wID = MenuCommandId;
                mii.fState = state;
                mii.dwTypeData = MenuText;
                mii.cch = MenuText.Length;
                mii.dwItemData = MenuOwnerTag;
                Native.InsertMenuItem(menu, (uint)Native.SC_CLOSE, false, ref mii);
                Dbg("menu inserted 0x" + h.ToInt64().ToString("X"));
            }
            else
            {
                Native.MENUITEMINFO mi = new Native.MENUITEMINFO();
                mi.cbSize = Marshal.SizeOf(typeof(Native.MENUITEMINFO));
                mi.fMask = Native.MIIM_STATE;
                mi.fState = state;
                Native.SetMenuItemInfo(menu, (uint)MenuCommandId, false, ref mi);
            }
            lastMenuHwnd = h;
        }

        public void RemoveMenu(IntPtr h)
        {
            if (h == IntPtr.Zero || !Native.IsWindow(h)) return;
            IntPtr menu = Native.GetSystemMenu(h, false);
            if (menu == IntPtr.Zero) return;
            try { Native.DeleteMenu(menu, (uint)MenuCommandId, Native.MF_BYCOMMAND); }
            catch { }
        }

        private void OnWinEvent(IntPtr hookHandle, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint thread, uint time)
        {
            if (eventType == Native.EVENT_OBJECT_FOCUS) { SelfHealTopmost(); return; }
            if (eventType == Native.EVENT_OBJECT_DESTROY) { OnTargetDestroyed(hwnd); return; }
            if (eventType == Native.EVENT_SYSTEM_FOREGROUND) { OnForeground(); return; }
            if (eventType == Native.EVENT_SYSTEM_MINIMIZESTART) { OnMinimize(hwnd, true); return; }
            if (eventType == Native.EVENT_SYSTEM_MINIMIZEEND) { OnMinimize(hwnd, false); return; }
            if (eventType == Native.EVENT_SYSTEM_MOVESIZESTART) { OnMoveSizeStart(hwnd); return; }
            if (eventType == Native.EVENT_SYSTEM_MOVESIZEEND) { OnMoveSizeEnd(hwnd); return; }
            if (eventType == Native.EVENT_OBJECT_LOCATIONCHANGE) { OnLocationChanged(hwnd, idObject); return; }
            if (eventType != Native.EVENT_OBJECT_INVOKED) return;

            IntPtr target = hwnd;
            IntPtr root = target == IntPtr.Zero ? IntPtr.Zero : Native.GetAncestor(target, Native.GA_ROOT);
            if (root != IntPtr.Zero) target = root;
            if (target == IntPtr.Zero || !Native.IsWindow(target))
            {
                target = Native.GetForegroundWindow();
                root = target == IntPtr.Zero ? IntPtr.Zero : Native.GetAncestor(target, Native.GA_ROOT);
                if (root != IntPtr.Zero) target = root;
            }
            if (target == IntPtr.Zero) return;

            IntPtr menu = Native.GetSystemMenu(target, false);
            if (menu == IntPtr.Zero) return;
            int idx = IndexOfOurItem(menu);
            if (idx < 0) return;

            // 有的程序把菜单项按"命令ID"上报，有的按"位置(0 或 1 基)"上报，两种都认
            bool hit = (idChild == MenuCommandId || idChild == idx || idChild == idx + 1);
            if (hit && target == lastToggleHwnd && (DateTime.UtcNow - lastToggleTime).TotalMilliseconds < 500)
            {
                Program.CrashLog("menu invoke: 已被鼠标钩子处理，跳过重复");
                return;
            }
            Program.CrashLog("menu invoke: child=0x" + idChild.ToString("X") + "(" + idChild + ") obj=" + idObject +
                             " idx=" + idx + " hwnd=0x" + target.ToInt64().ToString("X") +
                             " cls=" + Native.ClassName(target) + " hit=" + hit);
            if (!hit) return;
            if (IsOwnWindow(target)) return;
            lastToggleHwnd = target;
            lastToggleTime = DateTime.UtcNow;
            Toggle(target);
        }

        // ---------- 生命周期与自愈 ----------
        private void OnTargetDestroyed(IntPtr hwnd)
        {
            IntPtr h = Native.GetAncestor(hwnd, Native.GA_ROOT);
            if (h == IntPtr.Zero) h = hwnd;
            if (pinned.Contains(h))
            {
                Unpin(h, false);
                Program.CrashLog("destroy: 目标已销毁，自动取消置顶");
            }
        }

        private void SelfHealTopmost()
        {
            if (pinned.Count == 0) return;
            List<IntPtr> list = new List<IntPtr>(pinned);
            for (int i = 0; i < list.Count; i++)
            {
                IntPtr h = list[i];
                if (!Native.IsWindow(h) || Native.IsIconic(h)) continue;
                if (!Native.IsTopmost(h))
                {
                    Native.SetTopmostEx(h, true);
                    Program.CrashLog("self-heal: topmost 被外部清掉，重新置顶 | " + Describe(h));
                }
            }
        }

        private void OnMinimize(IntPtr hwnd, bool start)
        {
            IntPtr h = Native.GetAncestor(hwnd, Native.GA_ROOT);
            if (h == IntPtr.Zero) h = hwnd;
            if (!pinned.Contains(h)) return;
            if (start)
            {
                RemoveBorder(h);
                Program.CrashLog("min: 最小化，拆掉边框");
            }
            else
            {
                bool ok = Native.SetTopmostEx(h, true);
                EnsureBorder(h);
                EnsureMenu(h);
                Program.CrashLog("min: 还原，重新置顶 ok=" + ok + " 读回=" + Native.IsTopmost(h));
            }
        }

        private void OnForeground()
        {
            RefreshBorders();
            if (settings.ShowSystemMenu)
            {
                IntPtr fg = Native.GetForegroundWindow();
                if (fg != IntPtr.Zero && fg != lastMenuHwnd && Eligible(fg))
                {
                    try { EnsureMenu(fg); }
                    catch { }
                }
            }
        }

        private void RefreshBorders()
        {
            if (pinned.Count == 0) return;
            List<IntPtr> list = new List<IntPtr>(pinned);
            for (int i = 0; i < list.Count; i++)
            {
                IntPtr h = list[i];
                if (!Native.IsWindow(h)) continue;
                if (VirtualDesktop.IsOnCurrent(h))
                {
                    if (!borders.ContainsKey(h)) EnsureBorder(h);
                }
                else
                {
                    RemoveBorder(h);
                }
            }
        }

        private void AdoptExistingPins()
        {
            try
            {
                Native.EnumWindows(delegate(IntPtr h, IntPtr l)
                {
                    if (!Native.IsWindowVisible(h)) return true;
                    if (IsOwnWindow(h)) return true;
                    if (Native.GetProp(h, PropName) == IntPtr.Zero) return true;
                    if (!Eligible(h)) return true;
                    if (pinned.Add(h))
                    {
                        EnsureBorder(h);
                        EnsureMenu(h);
                        Program.CrashLog("adopt: 接管上次遗留的置顶窗口 | " + Describe(h));
                    }
                    return true;
                }, IntPtr.Zero);
                if (pinned.Count > 0) Raise();
            }
            catch (Exception ex) { Program.CrashLog("adopt fail: " + ex.Message); }
        }

        private void OnLocationChanged(IntPtr hwnd, int idObject)
        {
            if (idObject != 0) return;
            if (borders.Count == 0) return;
            BorderForm b;
            if (!borders.TryGetValue(hwnd, out b))
            {
                IntPtr h = Native.GetAncestor(hwnd, Native.GA_ROOT);
                if (h == IntPtr.Zero || !borders.TryGetValue(h, out b)) return;
            }
            try { b.UpdateToTarget(); } catch { }
        }

        private void OnMoveSizeStart(IntPtr hwnd)
        {
            IntPtr h = Native.GetAncestor(hwnd, Native.GA_ROOT);
            if (h == IntPtr.Zero) h = hwnd;
            if (!borders.ContainsKey(h)) return;
            if (!fastMode)
            {
                fastMode = true;
                fastSince = DateTime.UtcNow;
                Native.timeBeginPeriod(1);
                timer.Interval = 5;
                Program.CrashLog("drag: 进入高频跟随 interval=5ms");
            }
            BorderForm b;
            if (borders.TryGetValue(h, out b)) b.FastMode = true;
        }

        private void OnMoveSizeEnd(IntPtr hwnd)
        {
            IntPtr h = Native.GetAncestor(hwnd, Native.GA_ROOT);
            if (h == IntPtr.Zero) h = hwnd;
            BorderForm b;
            bool had = borders.TryGetValue(h, out b);
            if (had) b.FastMode = false;
            if (!fastMode) return;
            fastMode = false;
            timer.Interval = 80;
            Native.timeEndPeriod(1);
            Program.CrashLog("drag: 退出高频跟随 interval=80ms");
            if (had)
            {
                try { b.UpdateToTarget(); } catch { }
            }
        }

        private int IndexOfOurItem(IntPtr menu)
        {
            int count = Native.GetMenuItemCount(menu);
            for (int i = 0; i < count; i++)
            {
                Native.MENUITEMINFO mi = new Native.MENUITEMINFO();
                mi.cbSize = Marshal.SizeOf(typeof(Native.MENUITEMINFO));
                mi.fMask = Native.MIIM_ID | Native.MIIM_DATA;
                if (Native.GetMenuItemInfo(menu, (uint)i, true, ref mi))
                {
                    if (mi.wID == MenuCommandId) return i;
                }
            }
            return -1;
        }

        // ---------- 定时器 ----------
        private void OnTick(object sender, EventArgs e)
        {
            int want = (pinned.Count > 0 || borders.Count > 0) ? 80 : 1000;
            if (!fastMode && timer.Interval != want)
            {
                timer.Interval = want;
                Program.CrashLog("tick: 间隔 -> " + want + "ms");
            }
            if (pinned.Count > 0)
            {
                List<IntPtr> dead = null;
                if (fastMode && (DateTime.UtcNow - fastSince).TotalSeconds > 10)
                {
                    fastMode = false;
                    timer.Interval = 80;
                    Native.timeEndPeriod(1);
                    Program.CrashLog("drag: 超时兜底退出高频跟随");
                }
                foreach (IntPtr h in pinned)
                {
                    if (!Native.IsWindow(h) || (!Native.IsWindowVisible(h) && !Native.IsIconic(h)))
                    {
                        if (dead == null) dead = new List<IntPtr>();
                        dead.Add(h);
                    }
                }
                if (dead != null)
                {
                    for (int i = 0; i < dead.Count; i++) Unpin(dead[i], false);
                }
            }

            List<BorderForm> bs = new List<BorderForm>(borders.Values);
            for (int i = 0; i < bs.Count; i++)
            {
                try { bs[i].UpdateToTarget(); }
                catch { }
            }

            if (settings.ShowSystemMenu)
            {
                IntPtr fg = Native.GetForegroundWindow();
                if (fg != IntPtr.Zero && fg != lastMenuHwnd && Eligible(fg))
                {
                    try { EnsureMenu(fg); }
                    catch { }
                }
            }
        }

        // ---------- 设置变更 ----------
        public void ApplySettings()
        {
            Sound.Enabled = settings.PlaySound;
            if (!HotkeyRegistered || registered == null || !registered.SameAs(settings.Hotkey))
                RegisterHotkey();

            if (!settings.ShowBorder)
            {
                foreach (IntPtr h in new List<IntPtr>(borders.Keys)) RemoveBorder(h);
            }
            else
            {
                foreach (IntPtr h in new List<IntPtr>(pinned)) EnsureBorder(h);
            }

            if (!settings.ShowSystemMenu) RemoveMenu(lastMenuHwnd);

            // per-pixel alpha 渲染不走 WM_PAINT，Invalidate 无效，必须重新提交构图
            List<BorderForm> bs = new List<BorderForm>(borders.Values);
            for (int i = 0; i < bs.Count; i++)
            {
                try { bs[i].UpdateToTarget(); }
                catch { }
            }

            if (lastMenuHwnd != IntPtr.Zero) EnsureMenu(lastMenuHwnd);
            Raise();
        }

        public void Dispose()
        {
            try { timer.Stop(); timer.Dispose(); }
            catch { }
            UnregisterHotkey();
            if (hook != IntPtr.Zero)
            {
                try { Native.UnhookWinEvent(hook); }
                catch { }
                hook = IntPtr.Zero;
            }
            foreach (IntPtr h in new List<IntPtr>(borders.Keys)) RemoveBorder(h);
            try { msg.DestroyHandle(); }
            catch { }
        }

        // ---------- 消息窗口 ----------
        private sealed class MessageWindow : NativeWindow
        {
            private readonly Controller owner;

            public MessageWindow(Controller o)
            {
                owner = o;
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == Native.WM_HOTKEY)
                {
                    owner.ToggleForeground();
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }
}
