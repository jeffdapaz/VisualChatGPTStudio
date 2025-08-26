using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using WebBrowser = System.Windows.Controls.WebBrowser;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    /// <summary>
    /// Subclasses the internal <c>Internet Explorer_Server</c> window inside a WPF
    /// <see cref="System.Windows.Controls.WebBrowser"/> to intercept keyboard
    /// messages (e.g. Ctrl+C) and perform copy operations on the hosted MSHTML document.
    /// </summary>
    public static class WebBrowserCopyHook
    {
        // P/Invoke
        private const int GWL_WNDPROC = -4;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int VK_C = 0x43;
        private const int VK_CONTROL = 0x11;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        // keep alive
        private static readonly Dictionary<IntPtr, WndProcDelegate> _delegates = [];
        private static readonly Dictionary<IntPtr, IntPtr> _oldProcs = [];

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            }
            else
            {
                return SetWindowLong32(hWnd, nIndex, newProc);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        // Public install/uninstall
        public static void Install(WebBrowser webBrowser)
        {
            if (webBrowser == null)
            {
                return;
            }
            // Wait until loaded / handle created
            webBrowser.LoadCompleted += WebBrowser_LoadCompleted;
            webBrowser.Unloaded += WebBrowser_Unloaded;
        }

        private static void WebBrowser_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is WebBrowser wb)
            {
                Uninstall(wb);
            }
        }

        private static void WebBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (sender is WebBrowser wb)
            {
                // Find the Internet Explorer_Server window inside this WebBrowser
                IntPtr topHwnd = GetTopLevelHwnd(wb);
                if (topHwnd == IntPtr.Zero)
                {
                    return;
                }

                IntPtr ieServer = IntPtr.Zero;
                EnumChildWindows(topHwnd, (hwnd, lParam) =>
                {
                    StringBuilder sb = new(256);
                    GetClassName(hwnd, sb, sb.Capacity);
                    string cls = sb.ToString();
                    if (cls == "Internet Explorer_Server")
                    {
                        ieServer = hwnd;
                        return false; // stop enumeration
                    }
                    return true;
                }, IntPtr.Zero);

                if (ieServer == IntPtr.Zero)
                {
                    return;
                }

                // Subclass it if not already done
                if (!_oldProcs.ContainsKey(ieServer))
                {
                    WndProcDelegate del = (hWnd, msg, wParam, lParam) =>
                    {
                        // intercept keydown
                        if (msg == WM_KEYDOWN)
                        {
                            int vk = wParam.ToInt32();
                            bool ctrlDown = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
                            if (ctrlDown && vk == VK_C)
                            {
                                try
                                {
                                    // Try to copy using document.execCommand or fallback to selection->clipboard
                                    dynamic doc = wb.Document;
                                    try
                                    {
                                        // prefer execCommand
                                        doc.execCommand("Copy");
                                    }
                                    catch
                                    {
                                        // fallback: get selection text and set clipboard
                                        string text = null;
                                        try
                                        {
                                            dynamic sel = doc.selection;
                                            text = sel.createRange().text as string;
                                        }
                                        catch { }
                                        if (!string.IsNullOrEmpty(text))
                                        {
                                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                System.Windows.Clipboard.SetText(text);
                                            });
                                        }
                                    }

                                    // optionally swallow the key so IE doesn't duplicate handling
                                    return new IntPtr(1);
                                }
                                catch
                                {
                                    // ignore and fallthrough
                                }
                            }
                        }

                        // call original
                        return CallWindowProc(_oldProcs[hWnd], hWnd, msg, wParam, lParam);
                    };

                    IntPtr newProcPtr = Marshal.GetFunctionPointerForDelegate(del);
                    IntPtr old = SetWindowLongPtr(ieServer, GWL_WNDPROC, newProcPtr);
                    _delegates[ieServer] = del;       // keep delegate alive
                    _oldProcs[ieServer] = old;
                }
            }
        }

        public static void Uninstall(WebBrowser webBrowser)
        {
            if (webBrowser == null)
            {
                return;
            }

            IntPtr topHwnd = GetTopLevelHwnd(webBrowser);
            if (topHwnd == IntPtr.Zero)
            {
                return;
            }

            List<IntPtr> toRemove = [];
            EnumChildWindows(topHwnd, (hwnd, lParam) =>
            {
                StringBuilder sb = new(256);
                GetClassName(hwnd, sb, sb.Capacity);
                string cls = sb.ToString();
                if (cls == "Internet Explorer_Server")
                {
                    toRemove.Add(hwnd);
                }
                return true;
            }, IntPtr.Zero);

            foreach (IntPtr hwnd in toRemove)
            {
                if (_oldProcs.TryGetValue(hwnd, out IntPtr old))
                {
                    SetWindowLongPtr(hwnd, GWL_WNDPROC, old);
                    _oldProcs.Remove(hwnd);
                }
                if (_delegates.ContainsKey(hwnd))
                {
                    _delegates.Remove(hwnd);
                }
            }
        }

        private static IntPtr GetTopLevelHwnd(WebBrowser wb)
        {
            // the WebBrowser is a HwndHost child; get parent window handle (WPF Window)
            Window w = Window.GetWindow(wb);
            if (w == null)
            {
                return IntPtr.Zero;
            }

            WindowInteropHelper helper = new(w);
            IntPtr main = helper.Handle;
            return main;
        }
    }
}
