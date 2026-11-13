using EasyEPlanner;
using PInvoke;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IO.View
{
    // Функционал окна отвечающий за его отображение в окне EPLAN
    [ExcludeFromCodeCoverage]
    public partial class IOViewControl : Form
    {
        private bool isLoaded = false;

        static string caption = "ПЛК\0";

        static byte[] newCapt = EncodingDetector.Windows1251.GetBytes(caption);

        private IntPtr dialogHookPtr = IntPtr.Zero;

        private PI.HookProc dialogCallbackDelegate = null;

        private IntPtr dialogHandle = IntPtr.Zero;

        private IntPtr panelPtr = IntPtr.Zero;

        private static IntPtr wndPlcVisibilePtr;

        private PI.LowLevelKeyboardProc mainWndKeyboardCallbackDelegate = null;

        private IntPtr globalKeyboardHookPtr = IntPtr.Zero;

        /// <summary>
        /// Показать окно в формой
        /// </summary>
        public void ShowDlg()
        {
            System.Diagnostics.Process oCurrent = System.Diagnostics.Process.GetCurrentProcess();

            // Идентификатор команды вызова окна "ПЛК"
            const int wndWmCommand = 35084;
            string windowName = "ПЛК";

            if (isLoaded)
            {
                if (wndPlcVisibilePtr != IntPtr.Zero)
                {
                    StaticHelper.GUIHelper.ShowHiddenWindow(oCurrent,
                        wndPlcVisibilePtr, wndWmCommand);
                }
                else
                {
                    Show();
                    BringToFront();
                }
                return;
            }

            StaticHelper.GUIHelper.SearchWindowDescriptor(oCurrent, windowName,
                wndWmCommand, ref dialogHandle, ref wndPlcVisibilePtr);
            if (wndPlcVisibilePtr != IntPtr.Zero)
            {
                StaticHelper.GUIHelper.ShowHiddenWindow(oCurrent,
                    wndPlcVisibilePtr, wndWmCommand);

                StaticHelper.GUIHelper.ChangeWindowMainPanels(
                    ref dialogHandle, ref panelPtr);

                Controls.Clear();

                // Переносим на найденное окно свои элементы (SetParent) и
                // подгоняем их размеры и позицию.
                PI.SetParent(MainTableLayoutPanel.Handle, dialogHandle);
                ChangeUISize();

                // Устанавливаем свой хук для найденного окна
                // (для изменения размеров своих элементов, сохранения
                // изменений при закрытии и отключения хука).
                SetUpHook();

                isLoaded = true;
            }
            else
            {
                ShowFloating();
            }
            ChangeUISize();
        }

        /// <summary>
        /// Показать окно ПЛК как обычную Form (standalone / без EPLAN).
        /// </summary>
        private void ShowFloating()
        {
            isLoaded = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            Text = "Структура ПЛК";
            Width = Math.Max(Width, 700);
            Height = Math.Max(Height, 500);
            Show();
            BringToFront();
        }

        /// <summary>
        /// Встроить содержимое в WinForms-хост (DockPanel App).
        /// </summary>
        public void ShowInHost(Control host)
        {
            if (host == null)
                return;

            if (MainTableLayoutPanel.Parent != host)
            {
                MainTableLayoutPanel.Parent?.Controls
                    .Remove(MainTableLayoutPanel);
                host.Controls.Clear();
                MainTableLayoutPanel.Dock = DockStyle.Fill;
                host.Controls.Add(MainTableLayoutPanel);
            }

            MainTableLayoutPanel.Show();
            isLoaded = false;
        }

        public static readonly string CfgShowWindowKey = "show_plc_window";

        public static void SaveCfg()
        {
            SaveCfg(PI.IsWindowVisible(wndPlcVisibilePtr));
        }

        public static void SaveCfg(bool wndState)
        {
            var path = Environment.GetFolderPath(
                            Environment.SpecialFolder.ApplicationData);

            var ini = new IniFile(path + @"\Eplan\eplan.cfg");

            ini.WriteString("main", CfgShowWindowKey, wndState.ToString().ToLower());
        }

        private void SetUpHook()
        {
            dialogCallbackDelegate = DlgWndHookCallbackFunction;

            uint pid = PI.GetWindowThreadProcessId(dialogHandle, IntPtr.Zero);
            dialogHookPtr = PI.SetWindowsHookEx(PI.HookType.WH_CALLWNDPROC,
                dialogCallbackDelegate, IntPtr.Zero, pid);

            InstallKeyboardHook();
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
            StructPLC?.Focused == true || isCellEditing;

        private void MaybeReleaseKeyboardHook()
        {
            if (!ShouldKeepKeyboardHook())
                ReleaseKeyboardHook();
        }

        private IntPtr GlobalHookKeyboardCallbackFunction(int code,
            PI.WM wParam, PI.KBDLLHOOKSTRUCT lParam)
        {
            bool Ctrl = KeyboardHookHelper.IsCtrlPressed();
            uint vkCode = lParam.vkCode;

            // Перехватываем комбинации Ctrl + PgDn/PgUp для всех окон,
            // так как они ломают отрисовку
            // (переключаются вкладки дерево-список на оригинальном окне)
            if (wParam == PI.WM.KEYDOWN && Ctrl &&
                (vkCode is PI.VIRTUAL_KEY.VK_PRIOR or PI.VIRTUAL_KEY.VK_NEXT))
            {
                return (IntPtr)1;
            }


            if (code < 0 || StructPLC is null || !ShouldKeepKeyboardHook())
                return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam); 

            if (KeyboardHookHelper.ShouldBlockPlainTab(wParam, vkCode))
                return (IntPtr)1;

            //Отпускание клавиш - если активно окно редактора, то не пускаем дальше.
            if (wParam is PI.WM.KEYUP or PI.WM.CHAR &&
                vkCode is PI.VIRTUAL_KEY.VK_DELETE &&
                ShouldKeepKeyboardHook())
            {
                return (IntPtr)1;
            }

            if (wParam is not PI.WM.KEYDOWN)
                return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
            //  || Нажатие клавиш ||
            //  \/                \/

            // Если активен текстовый редактор - команды работы с текстом
            if (KeyCommands.ContainsKey(vkCode) && Ctrl && isCellEditing)
            {

                PI.SendMessage(PI.GetFocus(), KeyCommands[vkCode], 0, 0);
                return (IntPtr)1;
            }

            // Перехватываем используемые комбинации клавиш:
            switch (vkCode)
            {
                case PI.VIRTUAL_KEY.VK_ESCAPE:  // Esc
                case PI.VIRTUAL_KEY.VK_RETURN:  // Enter
                case PI.VIRTUAL_KEY.VK_DELETE:  // Delete

                case PI.VIRTUAL_KEY.VK_UP:      // Up
                case PI.VIRTUAL_KEY.VK_DOWN:    // Down
                case PI.VIRTUAL_KEY.VK_LEFT:    // Left
                case PI.VIRTUAL_KEY.VK_RIGHT:   // Right
                    PI.SendMessage(PI.GetFocus(), (int)PI.WM.KEYDOWN, (int)vkCode, 0);
                    return (IntPtr)1;
            }

            return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private static readonly Dictionary<uint, uint> KeyCommands
            = new Dictionary<uint, uint>
            {
                [(uint)Keys.X] = (int)PI.WM.CUT,    // Вырезать
                [(uint)Keys.C] = (int)PI.WM.COPY,   // Копировать
                [(uint)Keys.V] = (int)PI.WM.PASTE,  // Вставить
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

            if (msg.hwnd == dialogHandle)
            {
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

                        PI.SetParent(MainTableLayoutPanel.Handle, this.Handle);
                        this.Controls.Add(MainTableLayoutPanel);
                        MainTableLayoutPanel.Hide();
                        System.Threading.Thread.Sleep(1);
                        isLoaded = false;
                        break;

                    case (int)PI.WM.GETTEXT:
                        System.Runtime.InteropServices.Marshal.Copy(
                            newCapt, 0, lParam, newCapt.Length);
                        return (IntPtr)newCapt.Length;
                }
            }

            return PI.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }


        /// <summary>
        /// Изменить UI размер
        /// </summary>
        private void ChangeUISize()
        {
            IntPtr dialogPtr = PI.GetParent(MainTableLayoutPanel.Handle);

            PI.GetWindowRect(dialogPtr, out PI.RECT rctDialog);

            MainTableLayoutPanel.Location = new Point(0, 0);

            MainTableLayoutPanel.Width = rctDialog.Right - rctDialog.Left;
            MainTableLayoutPanel.Height = rctDialog.Bottom - rctDialog.Top;
        }


        private void StructPLC_MouseEnter(object sender, EventArgs e)
        {
            InstallKeyboardHook();
        }

        private void StructPLC_MouseLeave(object sender, EventArgs e)
        {
            MaybeReleaseKeyboardHook();
        }
    }
}
