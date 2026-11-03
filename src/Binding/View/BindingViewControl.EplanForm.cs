using PInvoke;
using StaticHelper;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace EasyEPlanner.Binding.View
{
    [ExcludeFromCodeCoverage]
    public partial class BindingViewControl : Form
    {
        public static readonly string CfgShowWindowKey = "show_binding_window";

        private bool isLoaded;

        private static readonly string caption = "Привязка\0";
        private static readonly byte[] newCapt =
            EncodingDetector.Windows1251.GetBytes(caption);

        private IntPtr dialogHookPtr = IntPtr.Zero;
        private PI.HookProc dialogCallbackDelegate;
        private IntPtr dialogHandle = IntPtr.Zero;
        private IntPtr panelPtr = IntPtr.Zero;
        private static IntPtr wndBindingVisiblePtr;

        private PI.LowLevelKeyboardProc mainWndKeyboardCallbackDelegate;
        private IntPtr globalKeyboardHookPtr = IntPtr.Zero;

        public bool IsVisible()
        {
            if (isLoaded)
                return PI.IsWindowVisible(wndBindingVisiblePtr);
            return Visible;
        }

        public void ShowDlg()
        {
            Process currentProcess = Process.GetCurrentProcess();

            const int wndWmCommand = 35093;
            const string windowName = "Штекеры";

            if (isLoaded)
            {
                GUIHelper.ShowHiddenWindow(currentProcess,
                    wndBindingVisiblePtr, wndWmCommand);
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
            var request = new EplanEmbeddedWindowHelper.PanelEmbedRequest
            {
                Form = this,
                MainPanel = MainTableLayoutPanel,
                DialogHandle = dialogHandle,
                VisibleWindowPtr = wndBindingVisiblePtr,
                PanelPtr = panelPtr,
            };

            bool embedded = EplanEmbeddedWindowHelper.TryEmbedInEplanPanel(
                request, currentProcess, windowName, wndWmCommand);
            if (!embedded)
                return false;

            dialogHandle = request.DialogHandle;
            wndBindingVisiblePtr = request.VisibleWindowPtr;
            panelPtr = request.PanelPtr;
            AfterEmbed();
            return true;
        }

        private void AfterEmbed()
        {
            ChangeUISize();
            SetUpHook();
            isLoaded = true;
            ChangeUISize();
        }

        private void ShowFloating() =>
            EplanEmbeddedWindowHelper.ShowFloating(this, MainTableLayoutPanel,
                ref isLoaded);

        public static void SaveCfg()
        {
            SaveCfg(PI.IsWindowVisible(wndBindingVisiblePtr) ||
                (Instance?.Visible == true && !Instance.isLoaded));
        }

        public static void SaveCfg(bool wndState) =>
            EplanEmbeddedWindowHelper.SaveCfg(CfgShowWindowKey, wndState);

        private void InitKeyboardHook()
        {
            mainWndKeyboardCallbackDelegate ??= GlobalHookKeyboardCallbackFunction;
        }

        private void InstallKeyboardHook() =>
            EplanEmbeddedWindowHelper.InstallKeyboardHook(
                ref globalKeyboardHookPtr, ref mainWndKeyboardCallbackDelegate,
                GlobalHookKeyboardCallbackFunction);

        private void ReleaseKeyboardHook() =>
            EplanEmbeddedWindowHelper.ReleaseKeyboardHook(
                ref globalKeyboardHookPtr);

        private bool ShouldKeepKeyboardHook() =>
            bindingTree?.Focused == true || textBox_search.Focused;

        private void MaybeReleaseKeyboardHook()
        {
            if (!ShouldKeepKeyboardHook())
                ReleaseKeyboardHook();
        }

        private void SetUpHook()
        {
            dialogCallbackDelegate = DlgWndHookCallbackFunction;
            dialogHookPtr = EplanEmbeddedWindowHelper.SetDialogHook(
                dialogHandle, dialogCallbackDelegate);
            InstallKeyboardHook();
        }

        private IntPtr GlobalHookKeyboardCallbackFunction(int code,
            PI.WM wParam, PI.KBDLLHOOKSTRUCT lParam)
        {
            if (EplanEmbeddedWindowHelper.TryDispatchCommonKeyboardHook(
                code, wParam, lParam,
                bindingTree is not null && ShouldKeepKeyboardHook(),
                out var vkCode, out var ctrl, out var handled))
            {
                return handled;
            }

            if (TryHandleKeyDown(vkCode, ctrl, out handled))
                return handled;

            return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private bool TryHandleKeyDown(uint vkCode, bool ctrl, out IntPtr result)
        {
            if (EplanEmbeddedWindowHelper.TrySendClipboardCommand(
                vkCode, ctrl, textBox_search.Focused, out result))
            {
                return true;
            }

            if (vkCode == (int)Keys.F && ctrl)
            {
                searchTSButton.PerformClick();
                result = (IntPtr)1;
                return true;
            }

            return EplanEmbeddedWindowHelper.TryForwardTreeNavigationKeys(
                vkCode, out result);
        }

        private IntPtr DlgWndHookCallbackFunction(int code, IntPtr wParam,
            IntPtr lParam)
        {
            var context = new EplanEmbeddedWindowHelper.DialogHookContext
            {
                PanelPtr = panelPtr,
                DialogHandle = dialogHandle,
                DialogHookPtr = dialogHookPtr,
                CaptionBytes = newCapt,
                Form = this,
                MainPanel = MainTableLayoutPanel,
                ReleaseKeyboardHook = ReleaseKeyboardHook,
                OnSizeChanged = ChangeUISize,
                OnDestroyed = () => isLoaded = false,
            };

            var result = EplanEmbeddedWindowHelper.HandleDialogHook(
                code, wParam, lParam, context);
            dialogHandle = context.DialogHandle;
            dialogHookPtr = context.DialogHookPtr;
            return result;
        }

        private void ChangeUISize() =>
            EplanEmbeddedWindowHelper.ChangeUISize(MainTableLayoutPanel,
                searchBoxTLP);

        private void BindingTree_MouseEnter(object sender, EventArgs e)
        {
            InstallKeyboardHook();
        }

        private void BindingTree_MouseLeave(object sender, EventArgs e)
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
