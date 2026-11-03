using PInvoke;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace EasyEPlanner.Devices.View
{
    /// <summary>
    /// Встройка окна в панель EPLAN «Клеммники» (заголовок «Устройства»).
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class DevicesViewControl : Form
    {
        public static readonly string CfgShowWindowKey = "show_devices_new_window";

        private bool isLoaded;

        private static readonly string caption = "Устройства\0";
        private static readonly byte[] newCapt =
            EncodingDetector.Windows1251.GetBytes(caption);

        private IntPtr dialogHookPtr = IntPtr.Zero;
        private PI.HookProc dialogCallbackDelegate;
        private IntPtr dialogHandle = IntPtr.Zero;
        private IntPtr panelPtr = IntPtr.Zero;
        private static IntPtr wndDevicesVisiblePtr;

        private PI.LowLevelKeyboardProc mainWndKeyboardCallbackDelegate;
        private IntPtr globalKeyboardHookPtr = IntPtr.Zero;

        /// <summary>
        /// Показать окно в панели EPLAN.
        /// </summary>
        public void ShowDlg()
        {
            Process currentProcess = Process.GetCurrentProcess();

            const int wndWmCommand = 35094;
            const string windowName = "Клеммники";

            if (isLoaded)
            {
                GUIHelper.ShowHiddenWindow(currentProcess,
                    wndDevicesVisiblePtr, wndWmCommand);
                return;
            }

            if (TryEmbedInEplanPanel(currentProcess, windowName, wndWmCommand))
                return;

            ShowFloating();
            Show();
        }

        private bool TryEmbedInEplanPanel(Process currentProcess,
            string windowName, int wndWmCommand)
        {
            if (!GUIHelper.SearchWindowDescriptor(currentProcess, windowName,
                wndWmCommand, ref dialogHandle, ref wndDevicesVisiblePtr))
            {
                return false;
            }

            if (wndDevicesVisiblePtr == IntPtr.Zero)
                return false;

            System.Threading.Thread.Sleep(200);

            GUIHelper.ShowHiddenWindow(currentProcess,
                wndDevicesVisiblePtr, wndWmCommand);

            if (!GUIHelper.ChangeWindowMainPanels(ref dialogHandle, ref panelPtr))
                return false;

            Controls.Clear();
            PI.SetParent(MainTableLayoutPanel.Handle, dialogHandle);
            ChangeUISize();
            SetUpHook();
            isLoaded = true;
            ChangeUISize();
            return true;
        }

        private void ShowFloating()
        {
            if (MainTableLayoutPanel.Parent != this)
            {
                PI.SetParent(MainTableLayoutPanel.Handle, Handle);
                Controls.Add(MainTableLayoutPanel);
            }

            MainTableLayoutPanel.Dock = DockStyle.Fill;
            MainTableLayoutPanel.Show();
            isLoaded = false;
        }

        public static void SaveCfg()
        {
            SaveCfg(PI.IsWindowVisible(wndDevicesVisiblePtr));
        }

        public static void SaveCfg(bool wndState)
        {
            var path = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            var ini = new IniFile(path + @"\Eplan\eplan.cfg");
            ini.WriteString("main", CfgShowWindowKey, wndState.ToString().ToLower());
        }

        private void InitKeyboardHook()
        {
            mainWndKeyboardCallbackDelegate ??= GlobalHookKeyboardCallbackFunction;
        }

        private void InstallKeyboardHook()
        {
            if (globalKeyboardHookPtr != IntPtr.Zero)
                return;

            InitKeyboardHook();
            globalKeyboardHookPtr = PI.SetWindowsHookEx(
                PI.HookType.WH_KEYBOARD_LL, mainWndKeyboardCallbackDelegate,
                IntPtr.Zero, 0);

            if (globalKeyboardHookPtr == IntPtr.Zero)
            {
                MessageBox.Show("Ошибка! Не удалось переназначить клавиши!");
            }
        }

        private void ReleaseKeyboardHook()
        {
            if (globalKeyboardHookPtr == IntPtr.Zero)
                return;

            PI.UnhookWindowsHookEx(globalKeyboardHookPtr);
            globalKeyboardHookPtr = IntPtr.Zero;
        }

        private bool ShouldKeepKeyboardHook() =>
            devicesTree?.Focused == true
            || isCellEditing
            || textBox_search.Focused;

        private void MaybeReleaseKeyboardHook()
        {
            if (!ShouldKeepKeyboardHook())
                ReleaseKeyboardHook();
        }

        private void SetUpHook()
        {
            dialogCallbackDelegate = DlgWndHookCallbackFunction;

            uint pid = PI.GetWindowThreadProcessId(dialogHandle, IntPtr.Zero);
            dialogHookPtr = PI.SetWindowsHookEx(PI.HookType.WH_CALLWNDPROC,
                dialogCallbackDelegate, IntPtr.Zero, pid);

            InstallKeyboardHook();
        }

        private IntPtr GlobalHookKeyboardCallbackFunction(int code,
            PI.WM wParam, PI.KBDLLHOOKSTRUCT lParam)
        {
            bool ctrl = KeyboardHookHelper.IsCtrlPressed();
            uint vkCode = lParam.vkCode;

            if (TryBlockCtrlPageNavigation(wParam, ctrl, vkCode, out var handled))
                return handled;

            if (code < 0 || devicesTree is null || !ShouldKeepKeyboardHook())
                return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);

            if (KeyboardHookHelper.ShouldBlockPlainTab(wParam, vkCode))
                return (IntPtr)1;

            if (TryBlockClipboardKeys(wParam, vkCode, ctrl, out handled))
                return handled;

            if (wParam is not PI.WM.KEYDOWN)
                return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);

            if (TryHandleKeyDown(vkCode, ctrl, out handled))
                return handled;

            return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private static bool TryBlockCtrlPageNavigation(PI.WM wParam, bool ctrl,
            uint vkCode, out IntPtr result)
        {
            if (wParam == PI.WM.KEYDOWN && ctrl &&
                (vkCode is PI.VIRTUAL_KEY.VK_PRIOR or PI.VIRTUAL_KEY.VK_NEXT))
            {
                result = (IntPtr)1;
                return true;
            }

            result = IntPtr.Zero;
            return false;
        }

        private static bool TryBlockClipboardKeys(PI.WM wParam, uint vkCode,
            bool ctrl, out IntPtr result)
        {
            if (wParam is not (PI.WM.KEYUP or PI.WM.CHAR))
            {
                result = IntPtr.Zero;
                return false;
            }

            switch ((Keys)vkCode)
            {
                case Keys.Delete:
                case Keys.C when ctrl:
                case Keys.V when ctrl:
                case Keys.X when ctrl:
                    result = (IntPtr)1;
                    return true;
                default:
                    result = IntPtr.Zero;
                    return false;
            }
        }

        private bool TryHandleKeyDown(uint vkCode, bool ctrl, out IntPtr result)
        {
            if (KeyCommands.ContainsKey(vkCode) && ctrl &&
                (isCellEditing || textBox_search.Focused))
            {
                PI.SendMessage(PI.GetFocus(), KeyCommands[vkCode], 0, 0);
                result = (IntPtr)1;
                return true;
            }

            switch (vkCode)
            {
                case (int)Keys.F when ctrl:
                    searchTSButton.PerformClick();
                    result = (IntPtr)1;
                    return true;

                case PI.VIRTUAL_KEY.VK_ESCAPE:
                case PI.VIRTUAL_KEY.VK_RETURN:
                case PI.VIRTUAL_KEY.VK_DELETE:
                case PI.VIRTUAL_KEY.VK_UP:
                case PI.VIRTUAL_KEY.VK_DOWN:
                case PI.VIRTUAL_KEY.VK_LEFT:
                case PI.VIRTUAL_KEY.VK_RIGHT:
                    PI.SendMessage(PI.GetFocus(), (int)PI.WM.KEYDOWN, (int)vkCode, 0);
                    result = (IntPtr)1;
                    return true;
            }

            result = IntPtr.Zero;
            return false;
        }

        private static readonly Dictionary<uint, uint> KeyCommands =
            new Dictionary<uint, uint>
            {
                [(uint)Keys.X] = (int)PI.WM.CUT,
                [(uint)Keys.C] = (int)PI.WM.COPY,
                [(uint)Keys.V] = (int)PI.WM.PASTE,
            };

        private IntPtr DlgWndHookCallbackFunction(int code, IntPtr wParam,
            IntPtr lParam)
        {
            PI.CWPSTRUCT msg = (PI.CWPSTRUCT)System.Runtime.InteropServices
                .Marshal.PtrToStructure(lParam, typeof(PI.CWPSTRUCT));

            if (msg.hwnd == panelPtr)
            {
                switch (msg.message)
                {
                    case (int)PI.WM.MOVE:
                    case (int)PI.WM.SIZE:
                        ChangeUISize();
                        break;
                }

                return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
            }

            if (msg.hwnd != dialogHandle)
                return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);

            switch (msg.message)
            {
                case (int)PI.WM.GETTEXTLENGTH:
                    return (IntPtr)newCapt.Length;

                case (int)PI.WM.SETTEXT:
                    return IntPtr.Zero;

                case (int)PI.WM.DESTROY:
                    PI.UnhookWindowsHookEx(dialogHookPtr);
                    dialogHookPtr = IntPtr.Zero;
                    dialogHandle = IntPtr.Zero;
                    ReleaseKeyboardHook();

                    PI.SetParent(MainTableLayoutPanel.Handle, Handle);
                    Controls.Add(MainTableLayoutPanel);
                    MainTableLayoutPanel.Hide();
                    System.Threading.Thread.Sleep(1);
                    isLoaded = false;
                    break;

                case (int)PI.WM.GETTEXT:
                    System.Runtime.InteropServices.Marshal.Copy(
                        newCapt, 0, lParam, newCapt.Length);
                    return (IntPtr)newCapt.Length;
            }

            return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private void ChangeUISize()
        {
            IntPtr dialogPtr = PI.GetParent(MainTableLayoutPanel.Handle);

            PI.GetWindowRect(dialogPtr, out PI.RECT rctDialog);

            MainTableLayoutPanel.Location = new Point(0, 0);
            MainTableLayoutPanel.Width = rctDialog.Right - rctDialog.Left;
            MainTableLayoutPanel.Height = rctDialog.Bottom - rctDialog.Top;
            searchBoxTLP.Invalidate();
        }

        private void DevicesTree_MouseEnter(object sender, EventArgs e)
        {
            InstallKeyboardHook();
        }

        private void DevicesTree_MouseLeave(object sender, EventArgs e)
        {
            MaybeReleaseKeyboardHook();
        }

        private void SearchInput_MouseEnter(object sender, EventArgs e)
        {
            InstallKeyboardHook();
        }

        private void SearchInput_MouseLeave(object sender, EventArgs e)
        {
            MaybeReleaseKeyboardHook();
        }
    }
}
