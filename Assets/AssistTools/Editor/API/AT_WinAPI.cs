/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

public class ATUtils_WinAPI
{
    #region Define
    public enum WindowShowStatus
    {
        SW_HIDE = 0,// 隐藏窗口
        SW_MAXIMIZE = 3,// 最大化窗口
        SW_MINIMIZE = 6,// 最小化窗口
        SW_RESTORE = 9,// 用原来的大小和位置显示一个窗口，同时令其进入活动状态
        SW_SHOW = 5,// 用当前的大小和位置显示一个窗口，同时令其进入活动状态
        SW_SHOWMAXIMIZED = 3,// 最大化窗口，并将其激活
        SW_SHOWMINIMIZED = 2,// 最小化窗口，并将其激活
        SW_SHOWMINNOACTIVE = 7,// 最小化一个窗口，同时不改变活动窗口
        SW_SHOWNA = 8,// 用当前的大小和位置显示一个窗口，不改变活动窗口
        SW_SHOWNOACTIVATE = 4,// 用最近的大小和位置显示一个窗口，同时不改变活动窗口
        SW_SHOWNORMAL = 1,// 用原来的大小和位置显示一个窗口，同时令其进入活动状态，与SW_RESTORE 相同
        WM_CLOSE = 0x10// 关闭窗体
    }

    public struct WindowInfo
    {
        public IntPtr hWnd;
        public string szWindowName;
        public string szClassName;
    }

    public delegate bool CallBack(IntPtr hwnd, int lParam);
    public delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);

    public const uint PROCESS_VM_OPERATION = 8u;
    public const uint PROCESS_VM_READ = 16u;
    public const uint PROCESS_VM_WRITE = 32u;
    public const uint MEM_COMMIT = 4096u;
    public const uint MEM_RELEASE = 32768u;
    public const uint MEM_RESERVE = 8192u;
    public const uint PAGE_READWRITE = 4u;

    public const int WM_MOUSEWHEEL = 0x020A; //鼠标滚轮  
    public const int WM_LBUTTONDOWN = 0x0201;//鼠标左键  
    public const int WM_LBUTTONUP = 0x0202;

    public const int WM_KEYDOWN = 0x0100;//模拟按键  
    public const int WM_KEYUP = 0x0101;
    public const int WM_CHAR = 0x0102;
    public const int WM_SYSKEYDOWN = 0x104;
    public const int WM_SYSKEYUP = 0x105;

    public const int MOUSEEVENTF_MOVE = 0x0001;//用于琴台鼠标移动  
    public const int MOUSEEVENTF_LEFTDOWN = 0x0002;//前台鼠标单击  
    public const int MOUSEEVENTF_LEFTUP = 0x0004;

    public const int WM_SETTEXT = 0x000C;//设置文字  
    public const int WM_GETTEXT = 0x000D;//读取文字  
    #endregion

    #region User32 API
    /// <summary>
    /// 显示窗体 命令 0:关闭窗口 1:正常大小显示窗口 2:最小化窗口3:最大化窗口
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern void GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern int FindWindow(string strClassName, string strWindowName);
    //public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll")]//,EntryPoint="FindWindowEx"
    public static extern int FindWindowEx(int hwndParent, int hwndChildAfter, string className, string windowName);
    //public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, String lpszClass, String lpszWindow);

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, string sParam);
    [DllImport("User32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, StringBuilder lParam);
    //public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, string lParam);

    //使用此功能，安装了一个钩子
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);
    //调用此函数卸载钩子
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern bool UnhookWindowsHookEx(int idHook);
    //使用此功能，通过信息钩子继续下一个钩子
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int EnumWindows(CallBack x, int y);
    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(int hwnd, out int processId);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(int hWnd, ref RECT lpRect);

    //ToAscii职能的转换指定的虚拟键码和键盘状态的相应字符或字符
    [DllImport("user32")]
    public static extern int ToAscii(int uVirtKey, //[in] 指定虚拟关键代码进行翻译。
                                     int uScanCode, // [in] 指定的硬件扫描码的关键须翻译成英文。高阶位的这个值设定的关键，如果是（不压）
                                     byte[] lpbKeyState, // [in] 指针，以256字节数组，包含当前键盘的状态。每个元素（字节）的数组包含状态的一个关键。如果高阶位的字节是一套，关键是下跌（按下）。在低比特，如果设置表明，关键是对切换。在此功能，只有肘位的CAPS LOCK键是相关的。在切换状态的NUM个锁和滚动锁定键被忽略。
                                     byte[] lpwTransKey, // [out] 指针的缓冲区收到翻译字符或字符。
                                     int fuState); // [in] Specifies whether a menu is active. This parameter must be 1 if a menu is active, or 0 otherwise.
    //获取按键的状态
    [DllImport("user32")]
    public static extern int GetKeyboardState(byte[] pbKeyState);
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    private static extern short GetKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;                             //最左坐标
        public int Top;                             //最上坐标
        public int Right;                           //最右坐标
        public int Bottom;                        //最下坐标
    }
    #endregion

    #region Kernel32 API
    [DllImport("kernel32.dll")]
    protected static extern int VirtualAllocEx(int hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
    [DllImport("kernel32.dll")]
    protected static extern bool VirtualFreeEx(int hProcess, int lpAddress, uint dwSize, uint dwFreeType);
    [DllImport("kernel32.dll")]
    protected static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead);
    [DllImport("kernel32.dll")]
    protected static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead);
    [DllImport("kernel32.dll")]
    protected static extern int OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int processId);
    [DllImport("kernel32.dll")]
    protected static extern bool CloseHandle(int handle);

    // 取得当前线程编号（线程钩子需要用到）
    [DllImport("kernel32.dll")]
    static extern int GetCurrentThreadId();
    //使用WINDOWS API函数代替获取当前实例的函数,防止钩子失效
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetModuleHandle(string name);
    #endregion

    #region Process
    static public int GetProcessId(int hwnd)
    {
        int result = 0;
        GetWindowThreadProcessId(hwnd, out result);
        return result;
    }

    static public int InjectProcess(int processId)
    {
        return OpenProcess(56u, false, processId);
    }
    #endregion

    #region Window Operate
    static public void SendMsg2Window(IntPtr ihd, string sInfo, bool Apend = true)
    {
        if (Apend)
        {
            int buffer_size = 1000;
            StringBuilder buffer = new StringBuilder(buffer_size);
            SendMessage(ihd, WM_GETTEXT, buffer_size, buffer);
            sInfo = buffer.ToString() + sInfo;
        }

        SendMessage(ihd, WM_SETTEXT, 0, sInfo);
    }

    /// <summary>
    /// 根据类名获取窗口信息
    /// </summary>
    static public List<WindowInfo> GetAllDesktopWindows(string className = "", string windowText = "")
    {
        List<WindowInfo> wndList = new List<WindowInfo>();
        EnumWindows(delegate(IntPtr hWnd, int lParam)
        {
            WindowInfo wnd = new WindowInfo();
            StringBuilder sb = new StringBuilder(256);

            wnd.hWnd = hWnd;

            GetWindowText(hWnd, sb, sb.Capacity);
            wnd.szWindowName = sb.ToString();

            GetClassName(hWnd, sb, sb.Capacity);
            wnd.szClassName = sb.ToString();

            if ((className == "" || wnd.szClassName == className)
                && (windowText == "" || wnd.szWindowName == windowText))
            {
                wndList.Add(wnd);
            }
            return true;
        }, 0);
        return wndList;
    }

    /// <summary>
    /// 功能：更具窗体类名获取窗体的信息
    /// </summary>
    static public WindowInfo GetIntPtrByWindowClName(string strWinClassName)
    {
        WindowInfo windowInfo = new WindowInfo();

        EnumWindows(delegate(IntPtr hWnd, int lParam)
        {
            WindowInfo wnd = new WindowInfo();
            StringBuilder sb = new StringBuilder(256);

            wnd.hWnd = hWnd;

            GetWindowText(hWnd, sb, sb.Capacity);
            wnd.szWindowName = sb.ToString();

            GetClassName(hWnd, sb, sb.Capacity);
            wnd.szClassName = sb.ToString();

            if (wnd.szClassName == strWinClassName)
            {
                windowInfo = wnd;
            }
            return true;
        }, 0);

        return windowInfo;
    }

    /// <summary>
    /// 功能：根据窗体标题获取窗体的信息
    /// </summary>
    static public WindowInfo GetIntPtrByWindowTitle(string strWinHeadName)
    {
        WindowInfo windowInfo = new WindowInfo();

        EnumWindows(delegate(IntPtr hWnd, int lParam)
        {
            WindowInfo wnd = new WindowInfo();
            StringBuilder sb = new StringBuilder(256);
            wnd.hWnd = hWnd;
            GetWindowText(hWnd, sb, sb.Capacity);
            wnd.szWindowName = sb.ToString();
            GetClassName(hWnd, sb, sb.Capacity);
            wnd.szClassName = sb.ToString();
            if (wnd.szWindowName == strWinHeadName)
            {
                windowInfo = wnd;
            }
            return true;
        }, 0);
        return windowInfo;
    }
    #endregion

    #region ListView Catcher
    public class ListViewCatecher
    {
        public class ListView
        {
            public List<string> Columns = new List<string>();
            public List<string[]> Items = new List<string[]>();
        }

        private struct LVITEM
        {
            public int mask;
            public int iItem;
            public int iSubItem;
            //public int state;
            //public int stateMask;
            public IntPtr pszText;
            public int cchTextMax;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        protected class HDITEM
        {
            public uint mask;
            public int cxy;
            public IntPtr pszText;
            public IntPtr hbm;
            public int cchTextMax;
            public int fmt;
            public int lParam;
            public int iImage;
            public int iOrder;
        }

        protected const uint LVM_FIRST = 4096u;
        protected const uint LVM_GETHEADER = 4127u;
        protected const uint LVM_GETITEMCOUNT = 4100u;
        protected const uint LVM_GETITEMTEXTA = 4141u;
        protected const uint LVM_GETITEMTEXTW = 4211u;
        protected const uint HDM_FIRST = 4608u;
        protected const uint HDM_GETITEMCOUNT = 4608u;
        protected const uint HDM_GETITEMW = 4619u;
        protected const uint HDM_GETITEMA = 4611u;
        protected int LVIF_TEXT = 1;
        protected int HDI_TEXT = 2;

        public int GetHeaderHwnd(int hwndListView)
        {
            return SendMessage(new IntPtr(hwndListView), 4127u, 0, 0);
        }

        public int GetRowCount(int hwndListView)
        {
            return SendMessage(new IntPtr(hwndListView), 4100u, 0, 0);
        }

        public int GetColumnCount(int hwndHeader)
        {
            return SendMessage(new IntPtr(hwndHeader), 4608u, 0, 0);
        }

        public List<string> GetColumnsHeaderText(int processHandle, int headerhwnd, int colCount)
        {
            List<string> list = new List<string>();
            uint num = 256u;
            int num2 = VirtualAllocEx(processHandle, IntPtr.Zero, (uint)Marshal.SizeOf(typeof(ListViewCatecher.HDITEM)), 12288u, 4u);
            int num3 = VirtualAllocEx(processHandle, IntPtr.Zero, num, 12288u, 4u);
            for (int i = 0; i < colCount; i++)
            {
                byte[] array = new byte[num];
                HDITEM hDITEM = new HDITEM();
                hDITEM.mask = (uint)this.HDI_TEXT;
                hDITEM.fmt = 0;
                hDITEM.cchTextMax = (int)num;
                hDITEM.pszText = (IntPtr)num3;
                IntPtr intPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(hDITEM));
                Marshal.StructureToPtr(hDITEM, intPtr, false);
                uint count = 0u;
                WriteProcessMemory(processHandle, num2, intPtr, Marshal.SizeOf(typeof(ListViewCatecher.HDITEM)), ref count);
                SendMessage(new IntPtr(headerhwnd), 4611u, i, num2);
                ReadProcessMemory(processHandle, num3, Marshal.UnsafeAddrOfPinnedArrayElement(array, 0), (int)num, ref count);
                string @string = Encoding.Default.GetString(array, 0, (int)count);
                string text = "";
                string text2 = @string;
                for (int j = 0; j < text2.Length; j++)
                {
                    char c = text2[j];
                    if (c == '\0')
                    {
                        break;
                    }
                    text += c;
                }
                list.Add(text);
            }
            VirtualFreeEx(processHandle, num2, 0u, 32768u);
            VirtualFreeEx(processHandle, num3, 0u, 32768u);
            return list;
        }

        public string[,] GetItemCellsText(int processHandle, int hwndListView, int rows, int cols)
        {
            string[,] array = new string[rows, cols];
            uint num = 256u;
            int num2 = VirtualAllocEx(processHandle, IntPtr.Zero, (uint)Marshal.SizeOf(typeof(ListViewCatecher.HDITEM)), 12288u, 4u);
            int num3 = VirtualAllocEx(processHandle, IntPtr.Zero, num, 12288u, 4u);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    byte[] array2 = new byte[num];
                    ListViewCatecher.LVITEM lVITEM = default(ListViewCatecher.LVITEM);
                    lVITEM.mask = this.LVIF_TEXT;
                    lVITEM.iItem = i;
                    lVITEM.iSubItem = j;
                    lVITEM.cchTextMax = (int)num;
                    lVITEM.pszText = (IntPtr)num3;
                    IntPtr intPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(lVITEM));
                    Marshal.StructureToPtr(lVITEM, intPtr, false);
                    uint count = 0u;
                    WriteProcessMemory(processHandle, num2, intPtr, Marshal.SizeOf(typeof(ListViewCatecher.LVITEM)), ref count);
                    SendMessage(new IntPtr(hwndListView), 4141u, i, num2);
                    ReadProcessMemory(processHandle, num3, Marshal.UnsafeAddrOfPinnedArrayElement(array2, 0), array2.Length, ref count);
                    string @string = Encoding.Default.GetString(array2, 0, (int)count);
                    array[i, j] = @string;
                }
            }
            VirtualFreeEx(processHandle, num2, 0u, 32768u);
            VirtualFreeEx(processHandle, num3, 0u, 32768u);
            return array;
        }

        public static void DoCatch(int hwnd, ListView LV)
        {
            LV.Columns.Clear();
            LV.Items.Clear();
            ListViewCatecher listViewAPIHelper = new ListViewCatecher();
            int headerHwnd = listViewAPIHelper.GetHeaderHwnd(hwnd);
            int rowCount = listViewAPIHelper.GetRowCount(hwnd);
            int columnCount = listViewAPIHelper.GetColumnCount(headerHwnd);
            int processId = GetProcessId(hwnd);
            int processHandle = InjectProcess(processId);
            List<string> columnsHeaderText = listViewAPIHelper.GetColumnsHeaderText(processHandle, headerHwnd, columnCount);
            for (int i = 0; i < columnsHeaderText.Count; i++)
            {
                string text = i.ToString();
                if (!string.IsNullOrEmpty(columnsHeaderText[i]))
                {
                    text = columnsHeaderText[i];
                }
                LV.Columns.Add(text);
            }
            string[,] itemCellsText = listViewAPIHelper.GetItemCellsText(processHandle, hwnd, rowCount, columnCount);
            for (int i = 0; i < rowCount; i++)
            {
                string[] array = new string[columnCount];
                for (int j = 0; j < columnCount; j++)
                {
                    array[j] = itemCellsText[i, j];
                }
                LV.Items.Add(array);
            }
        }
    }
    #endregion

    #region KeyEvent Hook
    public class KeyboardHook
    {
        public class KeyEventArgs : EventArgs
        {
            public KeyEventArgs(int icode) { KeyCode = icode; }
            public int KeyCode { get; private set; }
        }

        public class KeyPressEventArgs : EventArgs
        {
            public KeyPressEventArgs(int icode) { KeyCode = icode; }
            public int KeyCode { get; private set; }
        }

        public delegate void KeyEventHandler(object obj, KeyEventArgs arg);
        public delegate void KeyPressEventHandler(object obj, KeyPressEventArgs arg);

        public event KeyEventHandler KeyDownEvent;
        public event KeyPressEventHandler KeyPressEvent;
        public event KeyEventHandler KeyUpEvent;

        static int hKeyboardHook = 0; //声明键盘钩子处理的初始值
        public const int WH_KEYBOARD_LL = 13;   //线程键盘钩子监听鼠标消息设为2，全局键盘监听鼠标消息设为13
        HookProc KeyboardHookProcedure;
        //键盘结构
        [StructLayout(LayoutKind.Sequential)]
        public class KeyboardHookStruct
        {
            public int vkCode;  //虚拟键码。范围1至254
            public int scanCode; // 硬件扫描码
            public int flags;  // 键标志
            public int time; // 时间戳
            public int dwExtraInfo; // 额外信息
        }

        public void Start()
        {
            // 安装键盘钩子
            if (hKeyboardHook == 0)
            {
                KeyboardHookProcedure = new HookProc(KeyboardHookProc);
                hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProcedure, GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName), 0);
                //hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
                SetWindowsHookEx(13, KeyboardHookProcedure, IntPtr.Zero, GetCurrentThreadId());//指定要监听的线程idGetCurrentThreadId(),
                //键盘全局钩子,需要引用空间(using System.Reflection;)
                //SetWindowsHookEx( 13,MouseHookProcedure,Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),0);
                //
                //关于SetWindowsHookEx (int idHook, HookProc lpfn, IntPtr hInstance, int threadId)函数将钩子加入到钩子链表中，说明一下四个参数：
                //idHook 钩子类型，即确定钩子监听何种消息，上面的代码中设为2，即监听键盘消息并且是线程钩子，如果是全局钩子监听键盘消息应设为13，
                //线程钩子监听鼠标消息设为7，全局钩子监听鼠标消息设为14。lpfn 钩子子程的地址指针。如果dwThreadId参数为0 或是一个由别的进程创建的
                //线程的标识，lpfn必须指向DLL中的钩子子程。 除此以外，lpfn可以指向当前进程的一段钩子子程代码。钩子函数的入口地址，当钩子钩到任何
                //消息后便调用这个函数。hInstance应用程序实例的句柄。标识包含lpfn所指的子程的DLL。如果threadId 标识当前进程创建的一个线程，而且子
                //程代码位于当前进程，hInstance必须为NULL。可以很简单的设定其为本应用程序的实例句柄。threaded 与安装的钩子子程相关联的线程的标识符
                //如果为0，钩子子程与所有的线程关联，即为全局钩子
                if (hKeyboardHook == 0)
                {
                    Stop();
                    throw new Exception("安装键盘钩子失败");
                }
            }
        }

        public void Stop()
        {
            bool retKeyboard = true;
            if (hKeyboardHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
                hKeyboardHook = 0;
            }
            if (!(retKeyboard)) throw new Exception("卸载钩子失败！");
        }

        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            if ((nCode >= 0) && (KeyDownEvent != null || KeyUpEvent != null || KeyPressEvent != null))
            {
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                if (KeyDownEvent != null && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
                {
                    int keyData = (int)MyKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyDownEvent(this, e);
                }

                if (KeyPressEvent != null && wParam == WM_KEYDOWN)
                {
                    byte[] keyState = new byte[256];
                    GetKeyboardState(keyState);

                    byte[] inBuffer = new byte[2];
                    if (ToAscii(MyKeyboardHookStruct.vkCode, MyKeyboardHookStruct.scanCode, keyState, inBuffer, MyKeyboardHookStruct.flags) == 1)
                    {
                        KeyPressEventArgs e = new KeyPressEventArgs((char)inBuffer[0]);
                        KeyPressEvent(this, e);
                    }
                }

                if (KeyUpEvent != null && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
                {
                    int keyData = MyKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyUpEvent(this, e);
                }
            }
            //如果返回1，则结束消息，这个消息到此为止，不再传递。
            //如果返回0或调用CallNextHookEx函数则消息出了这个钩子继续往下传递，也就是传给消息真正的接受者
            return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }
        ~KeyboardHook()
        {
            Stop();
        }
    }
    #endregion
}


