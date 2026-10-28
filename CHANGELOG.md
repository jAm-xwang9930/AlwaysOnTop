# 版本记录

本项目从原型到成形在同一天内完成，下面按"版本 + 当天阶段"记录，方便回看每一步为什么这么做。

---

## v2.2 — 2026-10-28（打磨与减法）

**交互**

- 边框粗细从 1–10 滑块改为 **3 / 4 / 5 px 三档按钮**；不透明度改为 **100 / 85 / 70 % 三档**
- 有窗口处于置顶状态时，这两组按钮**置灰并显示红字提示**（置顶期间不允许调参）
- **关闭窗口行为可配**：「收到托盘」（默认）/「退出程序」；托盘「退出」始终是真退出
- 隐藏到托盘时弹一次气泡提示；托盘双击唤回窗口；隐藏 0.9 s 后压缩工作集
- 去掉标题下的副标题（16pt 标题占位压住了 7 px，直接删除而不是硬挤）

**行为**

- topmost 自愈：`EVENT_OBJECT_FOCUS` 时检查已置顶窗口是否还带 `WS_EX_TOPMOST`，掉了就重新置顶
- 最小化 / 还原：`MINIMIZESTART` 拆边框、`MINIMIZEEND` 重新置顶并重建边框
- 窗口被隐藏或销毁：自动从置顶列表移除
- 启动时扫描全部窗口，接管上次遗留的置顶（依据窗口属性 `DshAotPinned`）
- 虚拟桌面感知：窗口不在当前桌面时不画边框，切回自动补
- 全屏独占（D3D）程序在前台时忽略快捷键（对齐 PowerToys `blockInGameMode`）
- 失焦/前台事件驱动菜单与边框刷新；无置顶窗口时轮询降到 1 次/秒

**渲染**

- 边框渲染整体换成 per-pixel alpha：`Format32bppPArgb` + `UpdateLayeredWindow` + `AC_SRC_ALPHA`，替换掉颜色键抠黑，**消除边缘暗边**
- 窗口加 `DWMWA_EXCLUDED_FROM_PEEK`（不参与 Aero Peek）

**移除**

- **下线「窗口右键系统菜单 → 置顶窗口(&A)」整个功能**，同时删掉 `WH_MOUSE_LL` 低层鼠标钩子。原因见下方"踩坑记录"第 2 条——它会偶发冻结整机鼠标输入。`SystemMenu` 键不再从 ini 读取，老配置不会把它复活。

**修复**

- 换渲染内核后 `ApplySettings()` 仍在调 `Invalidate()`（新渲染不看 `WM_PAINT`），导致改粗细/不透明度对已存在的边框无效——改为重新提交构图
- 「行为」卡片删行后漏删配套的 `r3 += 34`，把「开机自启」挤出卡片底部
- 档位按钮左侧白边：相邻 Label 宽度 120 与按钮组起点 132 重叠 8 px，且 Label 先加入容器、z 序在上，深色选中态下露出白底——Label 宽度收到 104

---

## v2.1 — 2026-10-28（文档与分发）

- 便携包改为 zip 分发：从压缩包解压出来的副本不带"来自网络"标记，从而避免 SmartScreen 提示（也让进程拿到正常的完整性级别）
- 补齐使用说明：托盘、快捷键修改、配置项、SmartScreen 成因、卸载
- 加入 `--selftest` / `--screenshot` 开发模式，自检输出 `selftest.log`

---

## v2.0 — 2026-10-28（重构为多文件 + 自有界面）

- 拆分为 `Native.cs`（P/Invoke）/ `Core.cs`（设置、主题、边框、控制器）/ `Ui.cs`（界面、托盘）/ `AssemblyInfo.cs`
- 自带设置界面，风格参考 Windows 11 系统设置：卡片式、开关、强调色标题
- 边框改为**独立置顶分层窗口 + 环带几何**（参考 PowerToys `FrameDrawer`）：centerline 内缩 `halfThickness + 1`、内外圆角分开算、`FillMode.Alternate` 一次填充
- 拖动期高频跟随：`timeBeginPeriod(1)` + 5 ms 定时器，松手退回 80 ms
- 设置项：快捷键、边框开关、粗细、不透明度、圆角、提示音、开机自启
- 单实例、托盘常驻、置顶窗口列表

---

## v1.0 — 2026-10-28（单文件原型）

- 全局热键 `Win + Ctrl + T` → `SetWindowPos(HWND_TOPMOST, SWP_NOMOVE|SWP_NOSIZE)`
- 用一个普通 `Form` 当边框，GDI+ 描边
- 已知问题：边框"框在外面"而不是贴着窗口；拖动时明显滞后；无法持久化置顶状态

---

## 踩坑记录（给后来人）

### 1. 完整性级别（UIPI）：`SetWindowPos` 返回 5

从聊天工具、下载器的"打开"按钮启动时，进程会继承**低完整性令牌**。此时对其它进程的窗口调用 `SetWindowPos` 会被 UIPI 拦下，返回 `FALSE`，`GetLastError() == 5`（拒绝访问），且界面上没有任何提示——表现为"按了快捷键，没反应"。

处理：启动时用 `OpenProcessToken` + `GetTokenInformation(TokenIntegrityLevel)` 读自己的 RID，`< 0x2000` 即低权限；界面红字提示并提供"以管理员身份重启"；分发时让用户**解压后用资源管理器双击**，这样是正常 Medium 级别。

顺带一提：解析完整性 SID 时别用 `GetSidSubAuthorityCount` 返回 `int`（应为 `UCHAR`），高位垃圾会让索引越界，直接崩在启动阶段——本项目就是这么崩过几次的。

### 2. `WH_MOUSE_LL` 回调里做跨进程同步调用 = 冻结整机鼠标

为了让"右键系统菜单"里那一项能响应点击，用了低层鼠标钩子：截 `WM_LBUTTONUP` → `WindowFromPoint` → 命中 `#32768` 菜单窗口 → `SendMessage(MN_GETHMENU)` 取 HMENU → 遍历菜单项矩形判断点了哪一项。

问题在于：**LL 钩子的回调跑在安装钩子的线程（也就是我们的 UI 线程）上，而 `SendMessage` 是同步跨进程调用**。只要目标进程一时不响应，回调不返回，Windows 就会挂住整个鼠标输入队列——用户侧看到的是"鼠标变成沙漏、整机卡住"，而且难复现（取决于目标进程是否忙）。

结论：这个功能收益很低（快捷键已经够用），整个删掉。如果将来一定要做菜单集成，改用 `EVENT_OBJECT_INVOKED` + `OBJID_SYSMENU` 的 MSAA 事件，别用 LL 钩子。

### 3. 换了渲染内核，旧的刷新代码会变成空操作

边框从"GDI+ `OnPaint` 描边"换成"分层窗口逐像素 alpha"之后，内容不再走 `WM_PAINT`，于是设置里调粗细时调用的 `Invalidate()` 完全没效果。没有任何报错、没有异常，只是"改了不动"。

教训：**换渲染通道时，把所有触发重绘的入口一起检查一遍**（`Invalidate` / `Refresh` / `Update` / 自定义的提交函数）。

### 4. 颜色键抠黑会留下暗边

`SetLayeredWindowAttributes(hwnd, RGB(0,0,0), 255, LWA_COLORKEY)` 这种土办法够用，但抗锯齿像素是往黑色混合的，边框外沿会有一圈发暗的过渡。想干净只能用 per-pixel alpha（`UpdateLayeredWindow` + `AC_SRC_ALPHA`，源位图用 `Format32bppPArgb` 让 GDI+ 直接产出预乘值）。

### 5. 矩形要用 `DWMWA_EXTENDED_FRAME_BOUNDS`

`GetWindowRect` 包含 Win10 之后不可见的拖拽边距（左右各 7 px 左右），直接用来画边框会明显浮在窗口外面。

### 6. 边框几何：环带，而不是描边

- centerline 内缩 `halfThickness + 1`（这 1 px 用来吃掉边框和窗口之间的缝）
- 外圈半径 `r + halfThickness`，内圈半径 `max(r - halfThickness, 0)`
- 两个圆角矩形放进 `FillMode.Alternate` 的同一个路径，一次 `FillPath`

用 `DrawPath` 描边 + `PenAlignment.Inset` 也能画，但内外圆角不同心，四个角会露缝。

### 7. WinForms 的 z 序陷阱

`Controls.Add` 是"后加的排在下面"，所以先加的 Label 会**盖住**后加的按钮。本项目两次踩到：标题压住副标题 7 px（视觉上像文字被裁掉）、标签压住按钮左侧 8 px（深色选中态下露出白边）。布局时要么留够间距，要么显式调 `BringToFront()`。
