using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StaticHelper
{
    /// <summary>
    /// Общая логика встройки окон в панели EPLAN и перехвата клавиатуры.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class EplanEmbeddedWindowHelper
    {
        private static readonly Dictionary<uint, uint> ClipboardKeyCommands =
            new Dictionary<uint, uint>
            {
                [(uint)Keys.X] = (int)PI.WM.CUT,
                [(uint)Keys.C] = (int)PI.WM.COPY,
                [(uint)Keys.V] = (int)PI.WM.PASTE,
            };

        public sealed class PanelEmbedRequest
        {
            public Form Form { get; set; }

            public Control MainPanel { get; set; }

            public IntPtr DialogHandle { get; set; }

            public IntPtr VisibleWindowPtr { get; set; }

            public IntPtr PanelPtr { get; set; }
        }

        public sealed class DialogHookContext
        {
            public IntPtr PanelPtr { get; set; }

            public IntPtr DialogHandle { get; set; }

            public IntPtr DialogHookPtr { get; set; }

            public byte[] CaptionBytes { get; set; }

            public Form Form { get; set; }

            public Control MainPanel { get; set; }

            public Action ReleaseKeyboardHook { get; set; }

            public Action OnSizeChanged { get; set; }

            public Action OnDestroyed { get; set; }
        }

        public static void SaveCfg(string cfgKey, bool wndState)
        {
            var path = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            var ini = new IniFile(path + @"\Eplan\eplan.cfg");
            ini.WriteString("main", cfgKey, wndState.ToString().ToLower());
        }

        public static void ShowFloating(Form form, Control mainPanel,
            ref bool isLoaded)
        {
            if (mainPanel.Parent != form)
            {
                PI.SetParent(mainPanel.Handle, form.Handle);
                form.Controls.Add(mainPanel);
            }

            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Show();
            isLoaded = false;
        }

        public static bool TryEmbedInEplanPanel(
            PanelEmbedRequest request,
            Process currentProcess,
            string windowName,
            int wndWmCommand)
        {
            var dialogHandle = request.DialogHandle;
            var visiblePtr = request.VisibleWindowPtr;
            var panelPtr = request.PanelPtr;

            if (!GUIHelper.SearchWindowDescriptor(currentProcess, windowName,
                wndWmCommand, ref dialogHandle, ref visiblePtr))
            {
                return false;
            }

            if (visiblePtr == IntPtr.Zero)
                return false;

            System.Threading.Thread.Sleep(200);

            GUIHelper.ShowHiddenWindow(currentProcess, visiblePtr, wndWmCommand);

            if (!GUIHelper.ChangeWindowMainPanels(ref dialogHandle, ref panelPtr))
                return false;

            request.Form.Controls.Clear();
            PI.SetParent(request.MainPanel.Handle, dialogHandle);
            request.DialogHandle = dialogHandle;
            request.VisibleWindowPtr = visiblePtr;
            request.PanelPtr = panelPtr;
            return true;
        }

        public static void ChangeUISize(Control mainPanel,
            Control invalidateControl)
        {
            IntPtr dialogPtr = PI.GetParent(mainPanel.Handle);
            PI.GetWindowRect(dialogPtr, out PI.RECT rctDialog);

            mainPanel.Location = new Point(0, 0);
            mainPanel.Width = rctDialog.Right - rctDialog.Left;
            mainPanel.Height = rctDialog.Bottom - rctDialog.Top;
            invalidateControl?.Invalidate();
        }

        public static void InstallKeyboardHook(
            ref IntPtr hookPtr,
            ref PI.LowLevelKeyboardProc storedDelegate,
            PI.LowLevelKeyboardProc callback)
        {
            if (hookPtr != IntPtr.Zero)
                return;

            storedDelegate ??= callback;
            hookPtr = PI.SetWindowsHookEx(
                PI.HookType.WH_KEYBOARD_LL, storedDelegate, IntPtr.Zero, 0);

            if (hookPtr == IntPtr.Zero)
                MessageBox.Show("Ошибка! Не удалось переназначить клавиши!");
        }

        public static void ReleaseKeyboardHook(ref IntPtr hookPtr)
        {
            if (hookPtr == IntPtr.Zero)
                return;

            PI.UnhookWindowsHookEx(hookPtr);
            hookPtr = IntPtr.Zero;
        }

        public static IntPtr SetDialogHook(IntPtr dialogHandle,
            PI.HookProc callback)
        {
            uint pid = PI.GetWindowThreadProcessId(dialogHandle, IntPtr.Zero);
            return PI.SetWindowsHookEx(PI.HookType.WH_CALLWNDPROC,
                callback, IntPtr.Zero, pid);
        }

        public static bool TryBlockCtrlPageNavigation(PI.WM wParam, bool ctrl,
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

        public static bool TryBlockClipboardKeys(PI.WM wParam, uint vkCode,
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

        public static bool TryDispatchCommonKeyboardHook(
            int code,
            PI.WM wParam,
            PI.KBDLLHOOKSTRUCT lParam,
            bool canHandle,
            out uint vkCode,
            out bool ctrl,
            out IntPtr result)
        {
            ctrl = KeyboardHookHelper.IsCtrlPressed();
            vkCode = lParam.vkCode;

            if (TryBlockCtrlPageNavigation(wParam, ctrl, vkCode, out result))
                return true;

            if (code < 0 || !canHandle)
            {
                result = PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
                return true;
            }

            if (KeyboardHookHelper.ShouldBlockPlainTab(wParam, vkCode))
            {
                result = (IntPtr)1;
                return true;
            }

            if (TryBlockClipboardKeys(wParam, vkCode, ctrl, out result))
                return true;

            if (wParam is not PI.WM.KEYDOWN)
            {
                result = PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
                return true;
            }

            result = IntPtr.Zero;
            return false;
        }

        public static bool TrySendClipboardCommand(uint vkCode, bool ctrl,
            bool targetFocused, out IntPtr result)
        {
            if (ClipboardKeyCommands.ContainsKey(vkCode) && ctrl && targetFocused)
            {
                PI.SendMessage(PI.GetFocus(), ClipboardKeyCommands[vkCode], 0, 0);
                result = (IntPtr)1;
                return true;
            }

            result = IntPtr.Zero;
            return false;
        }

        public static bool TryForwardTreeNavigationKeys(uint vkCode,
            out IntPtr result)
        {
            switch (vkCode)
            {
                case PI.VIRTUAL_KEY.VK_ESCAPE:
                case PI.VIRTUAL_KEY.VK_RETURN:
                case PI.VIRTUAL_KEY.VK_DELETE:
                case PI.VIRTUAL_KEY.VK_UP:
                case PI.VIRTUAL_KEY.VK_DOWN:
                case PI.VIRTUAL_KEY.VK_LEFT:
                case PI.VIRTUAL_KEY.VK_RIGHT:
                    PI.SendMessage(PI.GetFocus(), (int)PI.WM.KEYDOWN,
                        (int)vkCode, 0);
                    result = (IntPtr)1;
                    return true;
            }

            result = IntPtr.Zero;
            return false;
        }

        public static IntPtr HandleDialogHook(
            int code, IntPtr wParam, IntPtr lParam, DialogHookContext context)
        {
            var msg = (PI.CWPSTRUCT)Marshal.PtrToStructure(lParam,
                typeof(PI.CWPSTRUCT));

            if (msg.hwnd == context.PanelPtr)
            {
                switch (msg.message)
                {
                    case (int)PI.WM.MOVE:
                    case (int)PI.WM.SIZE:
                        context.OnSizeChanged();
                        break;
                }

                return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
            }

            if (msg.hwnd != context.DialogHandle)
                return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);

            switch (msg.message)
            {
                case (int)PI.WM.GETTEXTLENGTH:
                    return (IntPtr)context.CaptionBytes.Length;

                case (int)PI.WM.SETTEXT:
                    return IntPtr.Zero;

                case (int)PI.WM.DESTROY:
                    PI.UnhookWindowsHookEx(context.DialogHookPtr);
                    context.DialogHookPtr = IntPtr.Zero;
                    context.DialogHandle = IntPtr.Zero;
                    context.ReleaseKeyboardHook();

                    PI.SetParent(context.MainPanel.Handle, context.Form.Handle);
                    context.Form.Controls.Add(context.MainPanel);
                    context.MainPanel.Hide();
                    System.Threading.Thread.Sleep(1);
                    context.OnDestroyed();
                    break;

                case (int)PI.WM.GETTEXT:
                    Marshal.Copy(context.CaptionBytes, 0, lParam,
                        context.CaptionBytes.Length);
                    return (IntPtr)context.CaptionBytes.Length;
            }

            return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }
    }
}
