using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using EplanDevice;
using IO.View;
using PInvoke;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TechObject;

namespace EasyEPlanner
{ 
    public partial class DFrm : Form
    {
        private static DFrm frm = null;

        public static DFrm GetInstance()
        {
            if (frm == null)
            {
                frm = new DFrm();
            }

            return frm;
        }

        public bool IsVisible()
        {
            return deviceIsShown;
        }

        public static void CheckShown()
        {
            if (PI.IsWindowVisible(wndDevVisibilePtr) == true)
            {
                deviceIsShown = true;
            }
            else
            {
                deviceIsShown = false;
            }
        }

        public static void SaveCfg(bool wndState)
        {
            string path = Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData);
            var ini = new IniFile(path + @"\Eplan\eplan.cfg");
            if (wndState == true)
            {
                ini.WriteString("main", "show_dev_window", "true");
            }
            else
            {
                ini.WriteString("main", "show_dev_window", "false");
            }
        }

        static string caption = "Устройства\0";
        static byte[] newCapt = EncodingDetector.Windows1251.GetBytes(caption);

        private PI.HookProc dialogCallbackDelegate = null;
        private IntPtr dialogHookPtr = IntPtr.Zero;

        private IntPtr dialogHandle = IntPtr.Zero;
        private IntPtr wndHandle = IntPtr.Zero;
        private IntPtr panelPtr = IntPtr.Zero;

        /// <summary>
        /// Функция для обработки завершения работы окна устройств.
        /// </summary>
        public void CloseEditor()
        {
            devicesTreeViewAdv.Model = null;

            PI.UnhookWindowsHookEx(dialogHookPtr);

            PI.SetParent(devicesTreeViewAdv.Handle, this.Handle);
            PI.SetParent(toolStrip.Handle, this.Handle);

            System.Threading.Thread.Sleep(1);
            deviceIsShown = false;
            isLoaded = false;
        }

        #region Dialog window hook
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

                    case (int)PI.WM.WINDOWPOSCHANGED:
                        PI.WINDOWPOS p = new PI.WINDOWPOS();
                        p = (PI.WINDOWPOS)
                            System.Runtime.InteropServices.Marshal
                            .PtrToStructure(lParam, typeof(PI.WINDOWPOS));

                        break;

                    case (int)PI.WM.DESTROY:
                        PI.UnhookWindowsHookEx(dialogHookPtr);
                        dialogHookPtr = IntPtr.Zero;
                        dialogHandle = IntPtr.Zero;

                        PI.SetParent(devicesTreeViewAdv.Handle, this.Handle);
                        PI.SetParent(toolStrip.Handle, this.Handle);
                        this.Controls.Add(devicesTreeViewAdv);
                        this.Controls.Add(toolStrip);
                        devicesTreeViewAdv.Hide();
                        System.Threading.Thread.Sleep(1);
                        deviceIsShown = false;
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
            IntPtr dialogPtr = PI.GetParent(devicesTreeViewAdv.Handle);

            PI.RECT rctDialog;
            PI.GetWindowRect(dialogPtr, out rctDialog);

            int w = rctDialog.Right - rctDialog.Left;
            int h = rctDialog.Bottom - rctDialog.Top;

            toolStrip.Location = new Point(0, 0);
            devicesTreeViewAdv.Location = new Point(0, toolStrip.Height);

            toolStrip.Width = w;
            devicesTreeViewAdv.Width = w;
            devicesTreeViewAdv.Height = h - toolStrip.Height;

            if (devicesTreeViewAdv.Columns.Count > 1 &&
                displayParamsBtn.Checked == true)
            {
                w -= 100;
            }
            devicesTreeViewAdv.Columns.First().Width = w;
        }
        #endregion

        #region ShowDialog

        /// <summary>
        /// Дескриптор окна устройств
        /// </summary>
        public static IntPtr wndDevVisibilePtr;

        /// <summary>
        /// Показано ли окно
        /// </summary>
        public static bool deviceIsShown = false;

        /// <summary>
        /// Загружено ли окно
        /// </summary>
        public static bool isLoaded = false;

        /// <summary>
        /// Показать окно в формой
        /// </summary>
        public void ShowDlg()
        {
            System.Diagnostics.Process oCurrent =
                System.Diagnostics.Process.GetCurrentProcess();

            // Идентификатор команды вызова окна "Устройства"
            const int wndWmCommand = 35116;
            string windowName = "Устройства";

            if (isLoaded == true)
            {
                StaticHelper.GUIHelper.ShowHiddenWindow(oCurrent, 
                    wndDevVisibilePtr, wndWmCommand);
                return;
            }

            StaticHelper.GUIHelper.SearchWindowDescriptor(oCurrent, windowName,
                wndWmCommand, ref dialogHandle, ref wndDevVisibilePtr);
            if(wndDevVisibilePtr != IntPtr.Zero)
            {
                StaticHelper.GUIHelper.ShowHiddenWindow(oCurrent,
                    wndDevVisibilePtr, wndWmCommand);
                
                if (isLoaded == false)
                {
                    StaticHelper.GUIHelper.ChangeWindowMainPanels(
                        ref dialogHandle, ref panelPtr);

                    Controls.Clear();

                    // Переносим на найденное окно свои элементы (SetParent) и
                    // подгоняем их размеры и позицию.
                    PI.SetParent(devicesTreeViewAdv.Handle, dialogHandle);
                    PI.SetParent(toolStrip.Handle, dialogHandle);
                    ChangeUISize();

                    // Устанавливаем свой хук для найденного окна
                    // (для изменения размеров своих элементов, сохранения
                    // изменений при закрытии и отключения хука).
                    SetUpHook();

                    deviceIsShown = true;
                    isLoaded = true;
                }           
            }
            ChangeUISize();
        }

        /// <summary>
        /// Устанавливаем хук для найденного окна.
        /// </summary>
        private void SetUpHook()
        {
            uint pid = PI.GetWindowThreadProcessId(dialogHandle, IntPtr.Zero);
            dialogHookPtr = PI.SetWindowsHookEx(PI.HookType.WH_CALLWNDPROC,
                dialogCallbackDelegate, IntPtr.Zero, pid);
        }
        #endregion

        private DFrm()
        {
            InitializeComponent();

            const string columnName1 = "Устройства";
            const string columnName2 = "Значения"; 
            StaticHelper.GUIHelper.SetUpAdvTreeView(devicesTreeViewAdv,
                columnName1, columnName2,
                devicesTreeViewAdv_DrawNodeColumn1,
                devicesTreeViewAdv_DrawNodeColumn2,
                devicesTreeViewAdv_EditNodeColumn2,
                nodeCheckBox);

            dialogCallbackDelegate = new PI.HookProc(
                DlgWndHookCallbackFunction);
        }

        ///<summary>
        /// Чекбокс для дерева
        ///</summary>
        NodeCheckBox nodeCheckBox = new NodeCheckBox();

        // Коррекция уровней дерева для нового элемента управления
        private static int correctionValue = 1;

        public delegate void OnSetNewValue(string str);
        public OnSetNewValue functionAfterCheck = null;

        /// <summary>
        /// Выбор устройства на дереве, которые входят в шаг.
        /// </summary> 
        /// <param name="checkedObjects">Строка с объектами, 
        /// которые надо отметить.</param>
        /// <param name="fn">Делегат, который вызывается при 
        /// последующих изменениях устройств шага.</param>
        public void SelectDisplayObjects(string checkedObjects,
            OnSetNewValue fn)
        {
            devicesTreeViewAdv.BeginUpdate();

            foreach (TreeNodeAdv boxedNode in devicesTreeViewAdv.AllNodes)
            {
                var node = boxedNode.Tag as Node;
                node.CheckState = CheckState.Unchecked;
            }

            checkedObjects = ' ' + checkedObjects + ' ';

            nodeCheckBox.CheckStateChanged -= 
                new EventHandler<TreePathEventArgs>(treeItem_AfterCheck);

            if (fn != null)
            {
                functionAfterCheck = fn;
            }

            var treeModel = devicesTreeViewAdv.Model as TreeModel;
            List<Node> nodes = treeModel.Nodes.ToList();
            SelectedDisplayObjects(nodes, checkedObjects);

            nodeCheckBox.CheckStateChanged += 
                new EventHandler<TreePathEventArgs>(treeItem_AfterCheck);

            devicesTreeViewAdv.EndUpdate();
        }

        /// <summary>
        /// Рекурсивная функция отметки галочками узлов
        /// </summary>
        /// <param name="nodes">Узлы для проверки</param>
        /// <param name="checkedObjects">Выбранные объекты</param>
        private void SelectedDisplayObjects(List<Node> nodes,
            string checkedObjects)
        {
            foreach (Node subNode in nodes)
            {
                switch(subNode.Tag)
                {
                    case EplanDevice.Device dev:
                        string devName = $" {dev.Name} ";
                        SetUpCheckState(subNode, devName, checkedObjects);
                        break;

                    case TechObject.Param par:
                        SetUpCheckStateParameter(subNode, par, checkedObjects);
                        break;
                }

                SelectedDisplayObjects(subNode.Nodes.ToList(), checkedObjects);
            }
        }

        /// <summary>
        /// Настроить селектор CheckBox в узле
        /// </summary>
        /// <param name="node">Узел</param>
        /// <param name="checkedObj">Выбранный объект</param>
        /// <param name="checkedObjects">Полный список выбранных объектов
        /// </param>
        private void SetUpCheckState(Node node, string checkedObj,
            string checkedObjects)
        {
            if (checkedObjects.Contains(checkedObj))
            {
                node.CheckState = CheckState.Checked;
                StaticHelper.GUIHelper.CheckCheckState(node);
            };
        }

        /// <summary>
        /// Настроить селектор CheckBox в узле для параметров
        /// </summary>
        /// <param name="node">Узел</param>
        /// <param name="param">Параметр</param>
        /// <param name="checkedObjects">Список выбранных объектов</param>
        public static void SetUpCheckStateParameter(Node node,
            Param param, string checkedObjects)
        {
            if (checkedObjects.Contains($" {param.GetNameLua()} ") ||
                checkedObjects.Contains($" {param.GetParameterNumber} "))
            {
                node.CheckState = CheckState.Checked;
                StaticHelper.GUIHelper.CheckCheckState(node);
            }
        }

        /// <summary>
        /// Скрыть все устройства
        /// </summary>
        /// <returns></returns>
        public bool ShowNoDevices()
        {
            devicesTreeViewAdv.BeginUpdate();

            var model = new TreeModel();
            model.Nodes.Clear();

            var root = new Node("Устройства проекта");
            model.Nodes.Add(root);
            devicesTreeViewAdv.Model = model;

            devicesTreeViewAdv.EndUpdate();
            devTypesLastSelected = null;
            devSubTypesLastSelected = null;

            return true;
        }

        private Editor.ITreeViewItem treeViewItemLastSelected = null;
        private EplanDevice.DeviceType[] devTypesLastSelected = null;
        private EplanDevice.DeviceSubType[] devSubTypesLastSelected = null;
        private bool displayParametersLastSelected = false;
        private int techObjectIndexLastSelected = -1;
        private bool prevShowChannels = false;
        private bool prevShowCheckboxes = false;

        /// <summary>
        /// Обновления видимости дерева устройств
        /// </summary>
        static class RefreshOperationTree
        {
            public static void Execute(TreeNodeAdv treeNode)
            {
                var node = treeNode.Tag as Node;

                if(node.Tag is EplanDevice.IODevice)
                {
                    RefreshDevice(node, treeNode);
                }
                else if(node.Tag is EplanDevice.IODevice.IOChannel)
                {
                    RefreshChannel(node, treeNode);
                }
                else
                {
                    RefreshOthers(node, treeNode);
                }
            }

            /// <summary>
            /// Обновить устройства
            /// </summary>
            private static void RefreshDevice(Node node, TreeNodeAdv treeNode)
            {
                var dev = node.Tag as EplanDevice.IODevice;

                bool isDevHidden = true;
                foreach (EplanDevice.IODevice.IOChannel ch in dev.Channels)
                {
                    if (ch.IsEmpty())
                    {
                        //Показываем каналы.
                        isDevHidden = false;
                    }
                }

                node.IsHidden = isDevHidden;

                if (treeNode.Children.Count < 1)
                {
                    return;
                }
                else
                {
                    RefreshChildRecursive(treeNode);
                }

            }

            /// <summary>
            /// Обновить канал
            /// </summary>
            private static void RefreshChannel(Node node, TreeNodeAdv treeNode)
            {
                var chn = node.Tag as EplanDevice.IODevice.IOChannel;
                if (chn.IsEmpty())
                {
                    node.IsHidden = false;
                }
                else
                {
                    node.IsHidden = true;
                }

                if (treeNode.Children.Count < 1)
                {
                    return;
                }
                else
                {
                    RefreshChildRecursive(treeNode);
                }
            }

            /// <summary>
            /// Обновить остальные узлы
            /// </summary>
            private static void RefreshOthers(Node node, TreeNodeAdv treeNode)
            {
                if (treeNode.Children.Count < 1)
                {
                    node.IsHidden = false;
                }
                else
                {
                    RefreshChildRecursive(treeNode);
                }
            }

            /// <summary>
            /// Рекурсивный вызов обновления дочерних элементов
            /// </summary>
            private static void RefreshChildRecursive(TreeNodeAdv treeNode)
            {
                List<TreeNodeAdv> childs = treeNode.Children.ToList();
                foreach (TreeNodeAdv child in childs)
                {
                    Execute(child);
                }

                return;
            }
        }

        /// <summary>
        /// Скрытие пустых узлов
        /// </summary>
        static class OnHideOperationTree
        {
            public static void Execute(TreeNodeAdv treeNode, 
                bool isHiddenNodeFull = false)
            {
                var node = treeNode.Tag as Node;

                bool firstTreeLevel = treeNode.Level == 1 + correctionValue;
                bool notExistChildren = firstTreeLevel &&
                    treeNode.Children.Count < 1;
                bool existChildren = firstTreeLevel &&
                    treeNode.Children.Count >= 1;

                if (notExistChildren)
                {
                    treeNode.IsHidden = true;
                    return;
                }
                else if (existChildren)
                {
                    // Проверка, есть ли устройства, которые не отображаются
                    if (node.Text.Contains("(0)"))
                    {
                        treeNode.IsHidden = true;
                        List<TreeNodeAdv> childs = treeNode.Children.ToList();
                        foreach (TreeNodeAdv child in childs)
                        {
                            Execute(child, true);
                        }
                        return;
                    }
                }

                if (treeNode.Children.Count > 0)
                {
                    bool isHidden = true;
                    List<TreeNodeAdv> childs = treeNode.Children.ToList();
                    foreach (TreeNodeAdv child in childs)
                    {
                        if (isHiddenNodeFull == true)
                        {
                            Execute(child, true);
                        }

                        if (child.IsHidden == false)
                        {
                            isHidden = false;
                            Execute(child);
                        }

                        treeNode.IsHidden = isHidden;
                    }
                }
            }
        }

        #region Refresh
        /// <summary>
        /// Обновление дерева на основе текущих объектов проекта.
        /// </summary>
        /// <param name="checkedObjects">Выбранные объекты.</param>
        /// <param name="techObjectIndex">Индекс технологического объекта
        /// </param>
        [ExcludeFromCodeCoverage]
        private void Refresh(string checkedObjects, int techObjectIndex)
        {
            devicesTreeViewAdv.BeginUpdate();

            var oldRoot = devicesTreeViewAdv.Root;
            if (devicesTreeViewAdv.GetNodeAt(new Point(1,devicesTreeViewAdv.Height - 25)) is { } lastNodeVisible)
            {
                // Полчаем последний видимый элемент для последующего восстановления прокрутки окна
                devicesTreeViewAdv.SelectedNode = lastNodeVisible;
            }
            
            devicesTreeViewAdv.Model = null;
            devicesTreeViewAdv.Refresh();
            var treeModel = new TreeModel();

            FillDevicesNode(ref treeModel);

            bool showParameters = displayParametersLastSelected 
                && techObjectIndex >= 0;
            if (showParameters)
            {
                TechObject.TechObject techObject = TechObject.TechObjectManager
                    .GetInstance().TechObjects[techObjectIndex];
                TechObject.Params parameters = techObject.GetParamsManager()
                    .Float;
                FillParametersNode(ref treeModel, parameters);
            }

            SortTreeView(treeModel);

            devicesTreeViewAdv.Model = treeModel;

            List<TreeNodeAdv> nodes = devicesTreeViewAdv.AllNodes.ToList();
            TreeNodeAdv treeNode = nodes[0];
            OnHideOperationTree.Execute(treeNode);

            if (checkedObjects != string.Empty || !RestoreExpanding(devicesTreeViewAdv.Root, oldRoot))
                devicesTreeViewAdv.ExpandAll();

            devicesTreeViewAdv.Refresh();
            if (devicesTreeViewAdv.SelectedNode is { } selectedNode)
            {
                devicesTreeViewAdv.ScrollTo(selectedNode);
                selectedNode.IsSelected = false;
            }
            devicesTreeViewAdv.EndUpdate();

            SelectDisplayObjects(checkedObjects, functionAfterCheck);
        }

        /// <summary>
        /// Востановление развертки узлов устройств после обновления
        /// </summary>
        /// <param name="node">Новый узел</param>
        /// <param name="referenceNode">Референсный узел</param>
        [ExcludeFromCodeCoverage]
        private bool RestoreExpanding(TreeNodeAdv node, TreeNodeAdv referenceNode)
        {
            if (node is null || referenceNode is null)
                return false;

            if(referenceNode.IsExpanded)
                node.Expand();
            node.IsSelected = referenceNode.IsSelected;
            
            var referenceChildNodes = referenceNode.Children
                .ToDictionary(rcn => GetTextWithoutCount(rcn), rcn => rcn);

            foreach (var childNode in node.Children)
            {
                if (referenceChildNodes.TryGetValue(GetTextWithoutCount(childNode), out var referenceChildNode))
                {
                    RestoreExpanding(childNode, referenceChildNode);
                }
            }

            return true;
        }

        /// <summary>
        /// Получить текст узла без обозначения количества дочерних элементов в скобках
        /// </summary>
        private static string GetTextWithoutCount(TreeNodeAdv node)
            => Regex.Replace((node.Tag as Node)?.Text, @"\(\s*\d*\s*\)$", "", 
                RegexOptions.None, TimeSpan.FromMilliseconds(100)).Trim();

        /// <summary>
        /// Заполнить дерево устройствами/сигналами проекта
        /// </summary>
        /// <param name="treeModel">Корень модели</param>
        private void FillDevicesNode(ref TreeModel treeModel)
        {
            string nodeName = "Устройства проекта";
            var root = new Node(nodeName);
            treeModel.Nodes.Add(root);

            // Подтипы, которые отдельно записываем в устройства
            var devicesSubTypesEnum = new string[]
            {
                "AI_VIRT",
                "AO_VIRT",
                "DI_VIRT",
                "DO_VIRT"
            };
            object[] devicesArray = GenerateArrayObjectsForFill(
                devicesSubTypesEnum);
            FillMainDevicesInNode(root, devicesArray);

            Dictionary<string, int> countDev = MakeDevicesCounterDictionary(
                root.Nodes);
            var deviceManager = EplanDevice.DeviceManager.GetInstance();
            foreach (EplanDevice.IODevice dev in deviceManager.Devices)
            {
                string deviceDescription = GenerateDeviceDescription(dev);
                string devSubType = dev.GetDeviceSubTypeStr(dev.DeviceType,
                    dev.DeviceSubType);
                if (devicesSubTypesEnum.Contains(devSubType))
                {
                    FillSubTypeNode(dev, root, deviceDescription, countDev);
                }
                else
                {
                    FillTypeNode(dev, root, deviceDescription, countDev);
                }
            }

            UpdateDevicesCountInHeaders(devicesArray, root, countDev);
        }

        /// <summary>
        /// Заполнить узел с параметрами
        /// </summary>
        /// <param name="treeModel">Корень модели</param>
        /// <param name="parameters">Параметры объекта</param>
        private void FillParametersNode(ref TreeModel treeModel,
            TechObject.Params parameters)
        {
            var root = new Node();
            treeModel.Nodes.Add(root);

            var luaNames = new List<string>();
            foreach(TechObject.Param param in parameters.Items)
            {
                luaNames.Add(param.GetNameLua());
            }

            foreach(var name in luaNames.Distinct())
            {
                var param = parameters.GetParam(name);

                var newNode = new Node($"{param.GetParameterNumber}. {name}")
                {
                    Tag = param
                };
                root.Nodes.Add(newNode);
            }

            string nodeName = "Параметры объекта";
            root.Text = nodeName + $" ({luaNames.Count})";
        }

        /// <summary>
        /// Сгенерировать массив со список типов и подтипов для заполнения
        /// </summary>
        /// <param name="subTypes">Список подтипов добавляемых в узлы</param>
        /// <returns></returns>
        private object[] GenerateArrayObjectsForFill(string[] subTypes)
        {
            Array devicesTypesEnum = Enum.GetValues(typeof(EplanDevice.DeviceType));
            int publicLength = devicesTypesEnum.Length + subTypes.Length;
            var objectArray = new object[publicLength];
            
            int index = 0;       
            foreach(EplanDevice.DeviceType devType in devicesTypesEnum)
            {
                objectArray[index] = devType;
                index++;
            }
            
            foreach(var devSubType in subTypes)
            {
                objectArray[index] = devSubType;
                index++;
            }

            return objectArray;
        }

        /// <summary>
        /// Заполнить узел типами и подтипами устройств
        /// </summary>
        /// <param name="deviceEnum">Список типов и подтипов для добавления
        /// </param>
        /// <param name="root">Главный узел</param>
        private void FillMainDevicesInNode(Node root, object[] deviceEnum)
        {
            foreach (var devType in deviceEnum)
            {
                var r = new Node(devType.ToString());
                if (devType is EplanDevice.DeviceType)
                {
                    r.Tag = (EplanDevice.DeviceType)devType;
                }
                else
                {
                    var devSubType = (EplanDevice.DeviceSubType)Enum
                        .Parse(typeof(EplanDevice.DeviceSubType),
                        devType.ToString());
                    r.Tag = devSubType;
                }
                root.Nodes.Add(r);
            }
        }

        /// <summary>
        /// Генерация словаря с количеством устройств каждого типа
        /// </summary>
        /// <param name="nodes">Коллекция узлов с названиями типов и подтипов
        /// </param>
        /// <returns></returns>
        private Dictionary<string, int> MakeDevicesCounterDictionary(
            IEnumerable<Node> nodes)
        {
            var devicesText = new Dictionary<string, int>();
            foreach (var node in nodes)
            {
                int defaultValue = 0;
                devicesText.Add(node.Text, defaultValue);
            }

            return devicesText;
        }

        /// <summary>
        /// Заполнить узел для типа устройства
        /// </summary>
        /// <param name="dev">Устройство</param>
        /// <param name="root">Главный узел</param>
        /// <param name="deviceDescription">Описание устройства</param>
        /// <param name="countDev">Словарь для подсчета количества</param>
        private void FillTypeNode(EplanDevice.IODevice dev, Node root,
            string deviceDescription, Dictionary<string, int> countDev)
        {
            Node devTypeNode = FindDevTypeNode(root, dev);
            if (devTypeNode == null)
            {
                return;
            }

            Node devObjectNode = MakeObjectNode(dev.ObjectName,
                dev.ObjectNumber, devTypeNode);
            Node devNode = MakeDeviceNode(devTypeNode, devObjectNode,
                dev, deviceDescription);

            bool isDevChannelsVisible = AddDevChannels(devNode, dev);
            bool isDevParametersVisible = displayParamsBtn.Checked && AddDevParametersAndProperties(devNode, dev);

            HideIncorrectDeviceTypeSubType(devNode, isDevChannelsVisible || isDevParametersVisible, countDev, 
                dev);
        }

        /// <summary>
        /// Заполнить узел для подтипа устройства
        /// </summary>
        /// <param name="dev">Устройство</param>
        /// <param name="root">Главный узел</param>
        /// <param name="deviceDescription">Описание устройства</param>
        /// <param name="countDev">Словарь для подсчета количества</param>
        private void FillSubTypeNode(EplanDevice.IODevice dev, Node root, 
            string deviceDescription, Dictionary<string, int> countDev)
        {
            Node devSubTypeNode = FindDevSubTypeNode(root, dev);
            if (devSubTypeNode == null)
            {
                return;
            }
            Node subDevObjectNode = MakeObjectNode(dev.ObjectName,
                dev.ObjectNumber, devSubTypeNode);
            Node subDevNode = MakeDeviceNode(devSubTypeNode,
                subDevObjectNode, dev, deviceDescription);
            bool isDevVisible = noAssigmentBtn.Checked ? false : true;
            HideIncorrectDeviceTypeSubType(subDevNode, isDevVisible, countDev, 
                dev);
        }

        /// <summary>
        /// Поиск узла типа устройства
        /// </summary>
        /// <param name="root">главный узел</param>
        /// <param name="dev">Устройство</param>
        /// <returns></returns>
        private Node FindDevTypeNode(Node root, EplanDevice.IODevice dev)
        {
            foreach (Node node in root.Nodes)
            {
                if (node.Tag is EplanDevice.DeviceType && 
                    (EplanDevice.DeviceType)node.Tag == dev.DeviceType)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// Поиск узла подтипа устройства
        /// </summary>
        /// <param name="dev">Устройство</param>
        /// <param name="root">Главный узел</param>
        /// <returns></returns>
        private Node FindDevSubTypeNode(Node root, EplanDevice.IODevice dev)
        {
            foreach (Node node in root.Nodes)
            {
                string devSubType = dev.GetDeviceSubTypeStr(dev.DeviceType, 
                    dev.DeviceSubType);
                if (node.Tag is EplanDevice.DeviceSubType && 
                    node.Text == devSubType)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// Генерация описания устройства
        /// </summary>
        /// <param name="dev">Устройство</param>
        /// <returns></returns>
        private string GenerateDeviceDescription(EplanDevice.IODevice dev)
        {
            string result = "";
            if (dev.Description.Contains('\n'))
            {
                string[] devDescr = dev.Description.Split('\n');
                foreach (string str in devDescr)
                {
                    result += str + " ";
                }
            }
            else
            {
                result = dev.Description;
            }

            return result;
        }

        /// <summary>
        /// Сделать и заполнить узел объекта
        /// </summary>
        /// <param name="devTypeNode">Узел для добавления объекта</param>
        /// <param name="objectName">Имя объекта</param>
        /// <param name="objectNumber">Номер объекта</param>
        /// <returns></returns>
        private Node MakeObjectNode(string objectName, long objectNumber, 
            Node devTypeNode)
        {
            Node devObjectNode = null;
            string fullObjectName = objectName + objectNumber;
            if (objectName != "")
            {
                foreach (Node node in devTypeNode.Nodes)
                {
                    if ((node.Tag is string) && 
                        (string)node.Tag == fullObjectName)
                    {
                        return node;
                    }
                }

                if (devObjectNode == null)
                {
                    devObjectNode = new Node(fullObjectName);
                    devObjectNode.Tag = fullObjectName;
                    devTypeNode.Nodes.Add(devObjectNode);
                    return devObjectNode;
                }
            }

            return devObjectNode;
        }

        /// <summary>
        /// Сделать узел устройства
        /// </summary>
        /// <param name="devTypeNode">Узел типа устройства</param>
        /// <param name="dev">Устройство</param>
        /// <param name="deviceDescription">Описание устройства</param>
        /// <param name="devObjectNode">Узел объекта или null</param>
        /// <returns>Заполненный узел</returns>
        private Node MakeDeviceNode(Node devTypeNode, Node devObjectNode, 
            EplanDevice.IODevice dev, string deviceDescription)
        {
            Node devNode;
            if (dev.ObjectName != "")
            {
                string devName = $"{dev.EplanName.Split('-').Last()}\t {deviceDescription}";
                devNode = new Node(devName);
            }
            else
            {
                devNode = new Node(dev.Name + "\t  " + deviceDescription);
            }
            devNode.Tag = dev;

            if (devObjectNode != null)
            {
                devObjectNode.Nodes.Add(devNode);
            }
            else
            {
                devTypeNode.Nodes.Add(devNode);
            }

            return devNode;
        }

        /// <summary>
        /// Отображение каналов устройств
        /// </summary>
        /// <param name="devNode">Узел устройства</param>
        /// <param name="dev">Устройство</param>
        /// <returns></returns>
        private bool AddDevChannels(Node devNode, EplanDevice.IODevice dev)
        {
            bool isDevVisible = false;
            if (prevShowChannels)
            {
                Node channelNode;
                //Показываем каналы.
                foreach (EplanDevice.IODevice.IOChannel ch in dev.Channels)
                {
                    if (!ch.IsEmpty())
                    {
                        channelNode = new Node(ch.Name + " " + ch.Comment +
                            $" (A{ch.FullModule}:" + ch.PhysicalClamp + ")");
                        channelNode.Tag = ch;
                        devNode.Nodes.Add(channelNode);

                        if (noAssigmentBtn.Checked)
                        {
                            channelNode.IsHidden = true;
                        }
                        else
                        {
                            isDevVisible = true;
                        }
                    }
                    else
                    {
                        channelNode = new Node(ch.Name + " " + ch.Comment);
                        channelNode.Tag = ch;
                        devNode.Nodes.Add(channelNode);

                        isDevVisible = true;
                    }
                }
            }

            return isDevVisible;
        }

        public static bool AddDevParametersAndProperties(Node devNode, IODevice dev)
        {
            AddDeviceGroupNodes(devNode, "Данные",
            [
                new DeviceSubTypeNode(dev.DeviceSubType.ToString()),
                new DeviceDescriptionNode(dev.Description),
                new DeviceArticleNode(dev.ArticleName),
            ]);

            int padding = dev.Parameters.Select(x => x.Key.Name)
                .DefaultIfEmpty("")
                .Max(parNmae => parNmae.Length);
            AddDeviceGroupNodes(devNode, "Параметры",
                dev.Parameters.Select(p => new ParameterNode(
                    $"{p.Key.Name.PadRight(padding)} {p.Key.Description}",
                    p.Value?.ToString() ?? "") { Tag = p.Key }));

            AddDeviceGroupNodes(devNode, "Свойства",
                dev.Properties.Select(p => new PropertyNode(
                    p.Key, p.Value?.ToString() ?? "")));

            AddDeviceGroupNodes(devNode, "Рабочие параметры", 
                dev.RuntimeParameters.Select(p => new RuntimeParameterNode(
                    p.Key, p.Value?.ToString() ?? "")));

            return true;
        }

        
        /// <summary>
        /// Добавить группу с узлами
        /// </summary>
        /// <param name="parent">Родительский узел</param>
        /// <param name="nodeName">Имя группы</param>
        /// <param name="nodes">Узлы</param>
        private static void AddDeviceGroupNodes(Node parent, string nodeName, IEnumerable<Node> nodes)
        {
            if (!nodes.Any())
                return;

            var parametersNode = new Node(nodeName);
            parent.Nodes.Add(parametersNode);

            foreach(var node in nodes)
            {
                parametersNode.Nodes.Add(node);
            }
        }

        /// <summary>
        /// Скрываем устройства ненужных подтипов и типов
        /// </summary>
        /// <param name="dev">Устройство</param>
        /// <param name="countDev">Словарь для подсчета количества устройств
        /// </param>
        /// <param name="devTypeSubTypeNode">Узел типа или подтипа</param>
        /// <param name="isDevVisible">Видимость устройства</param>
        private void HideIncorrectDeviceTypeSubType(Node devTypeSubTypeNode,
            bool isDevVisible, Dictionary<string,int> countDev, 
            EplanDevice.IODevice dev)
        {
            if (devTypesLastSelected != null &&
                !devTypesLastSelected.Contains(dev.DeviceType))
            {
                devTypeSubTypeNode.IsHidden = true;
            }
            else
            {
                if (devSubTypesLastSelected != null &&
                    !devSubTypesLastSelected.Contains(dev.DeviceSubType))
                {
                    devTypeSubTypeNode.IsHidden = true;
                }
                else
                {
                    if (prevShowChannels && !isDevVisible)
                    {
                        devTypeSubTypeNode.IsHidden = true;
                    }
                    else
                    {
                        string subTypeName = dev.GetDeviceSubTypeStr(
                            dev.DeviceType, dev.DeviceSubType);
                        if(subTypeName != "" && 
                            countDev.ContainsKey(subTypeName))
                        {
                            countDev[subTypeName]++;
                        }
                        else
                        {
                            countDev[dev.DeviceType.ToString()]++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обновление количества устройств шапках их типов
        /// </summary>
        /// <param name="deviceEnum">Названия</param>
        /// <param name="root">Узел</param>
        /// <param name="countDev">Словарь с количеством по индексам</param>
        private void UpdateDevicesCountInHeaders(object[] deviceEnum, Node root, 
            Dictionary<string, int> countDev)
        {
            //Обновляем названия строк (добавляем количество устройств).
            int total = 0;
            foreach (var dev in deviceEnum)
            {
                Node node = root.Nodes
                    .Where(x => x.Text == dev.ToString())
                    .FirstOrDefault();
                total += countDev[dev.ToString()];
                if (node != null)
                {
                    node.Text = dev.ToString() +
                        " (" + countDev[dev.ToString()] + ")  ";
                }
            }

            root.Text = "Устройства проекта (" + total + ")";
        }

        /// <summary>
        /// Сортировка модели дерева устройств
        /// </summary>
        private void SortTreeView(TreeModel treeModel)
        {
            // Сортировка узлов
            List<Node> rootNodes = treeModel.Nodes.ToList();
            // Сортируем узлы внутри каждого устройства Device
            foreach (Node node in rootNodes)
            {
                TreeSort(node.Nodes.ToList(), node);
            }
        }

        /// <summary>
        /// Сортировка дерева с помощью List.Sort
        /// </summary>
        /// <param name="nodes">Список Nodes в узле</param>
        /// <param name="parent">Родительский узел, где лежат Nodes</param>
        public static void TreeSort(List<Node> nodes, Node parent)
        {
            // Если меньше или 1, то нет смысла сортировать
            if (nodes.Count <= 1)
                return;

            // Собственный Comparer для List
            nodes.Sort((frst, scnd) =>
            {
                if (frst.Tag is IODevice.IOChannel frstCh &&
                    scnd.Tag is IODevice.IOChannel scndCh)
                    return IODevice.IOChannel.Compare(frstCh, scndCh);

                if (frst.Nodes.FirstOrDefault()?.Tag is Device frstChildDev &&
                    scnd.Nodes.FirstOrDefault()?.Tag is Device scndChildDev)
                    return Device.Compare(frstChildDev, scndChildDev);

                if (frst.Tag is Device frstDev &&
                    scnd.Tag is Device scndDev)
                    return Device.Compare(frstDev, scndDev);

                return frst.Text.CompareTo(scnd.Text);
            });

            // Очищаем Nodes
            parent.Nodes.Clear();
            // Записываем отсортированные и сортируем сразу следующие
            foreach (Node node in nodes)
            {
                parent.Nodes.Add(node);
                TreeSort(node.Nodes.ToList(), node);
            }
        }
        #endregion

        /// <summary>
        /// Построение дерева на основе определенных объектов проекта.
        /// </summary>
        /// <param name="selectedItem">Выбранный элемент в редакторе</param>
        /// <param name="fn">Делегат для установки нового значения в поле
        /// </param>
        /// <param name="isRebuiltTree">Нужно ли перестраивать дерево</param>
        public bool ShowDisplayObjects(Editor.ITreeViewItem selectedItem,
            OnSetNewValue fn, bool isRebuiltTree = false)
        {
            bool enabledEdit = EProjectManager.GetInstance().EnabledEditMode;
            prevShowChannels = !enabledEdit;
            prevShowCheckboxes = enabledEdit;
            treeViewItemLastSelected = selectedItem;

            GetItemDataForShowObjects(treeViewItemLastSelected,
                out EplanDevice.DeviceType[] devTypes,
                out EplanDevice.DeviceSubType[] devSubTypes,
                out bool displayParameters, 
                out int techObjectIndex,
                out string checkedObjects);

            if (fn != null)
            {
                functionAfterCheck = fn;
            }

            if (prevShowCheckboxes)
            {
                displayParamsBtn.Checked = false;
                devicesTreeViewAdv.NodeControls.Insert(0, nodeCheckBox);
            }
            else
            {
                devicesTreeViewAdv.NodeControls.Remove(nodeCheckBox);
            }

            //Проверяем на изменение типов отображаемых устройств.
            if (Equals(devTypes, devTypesLastSelected) &&
                Equals(devSubTypes, devSubTypesLastSelected) &&
                isRebuiltTree == false)
            {
                SelectDisplayObjects(checkedObjects, fn);
                ShowDlg();
                return true;
            }

            devTypesLastSelected = devTypes;
            devSubTypesLastSelected = devSubTypes;
            displayParametersLastSelected = displayParameters;
            techObjectIndexLastSelected = techObjectIndex;

            Refresh(checkedObjects, techObjectIndexLastSelected);
            ShowDlg();
            return true;
        }

        /// <summary>
        /// Получить информацию из элемента для отображения объектов
        /// </summary>
        /// <param name="item">Элемент из редактора</param>
        /// <param name="devTypes">Отображаемые типы устройств</param>
        /// <param name="devSubTypes">Отображаемые подтипы устройств</param>
        /// <param name="displayParameters">Отображать параметры объекта</param>
        /// <param name="techObjectIndex">Индекс технологического объекта
        /// </param>
        /// <param name="checkedObjects">Выбранные объекты в поле</param>
        private void GetItemDataForShowObjects(
            Editor.ITreeViewItem item, out EplanDevice.DeviceType[] devTypes,
                out EplanDevice.DeviceSubType[] devSubTypes, 
                out bool displayParameters, out int techObjectIndex,
                out string checkedObjects)
        {
            if(item != null)
            {
                item.GetDisplayObjects(out devTypes, out devSubTypes,
                    out displayParameters);
                checkedObjects = item.EditText[1];

                techObjectIndex = -1;
                var mainObject = Editor.Editor.GetInstance()
                    .EditorForm.GetParentBranch(item);
                if (mainObject != null)
                {
                    var techObjectManager = TechObject.TechObjectManager
                        .GetInstance();
                    techObjectIndex = techObjectManager
                        .TechObjects
                        .IndexOf(
                        mainObject as TechObject.TechObject);
                }
            }
            else
            {
                devTypes = null; // Отобразить все типы
                devSubTypes = null; // Отобразить все подтипы
                displayParameters = false;
                checkedObjects = string.Empty;
                techObjectIndex = -1;
            }
        }

        /// <summary>
        /// По двойному клику на узле вставляем в поле описания 
        /// функционального текста(EPlan) строку с информацией 
        /// о канале устройства.
        /// </summary>
        /// 
        private void treeView_DoubleClick(object sender, 
            TreeNodeAdvMouseEventArgs e)
        {
            IApiHelper apiHelper = new ApiHelper();
            IProjectHelper projectHelper = new ProjectHelper(apiHelper);
            IIOHelper ioHelper = new IOHelper(projectHelper);
            var deviceBinder = new DeviceBinder(this, apiHelper, ioHelper);
            deviceBinder.Bind();
        }

        /// <summary>
        /// Выбор устройств, участвующих в операции.
        /// </summary>         
        static class OnCheckOperationTree
        {
            static string res = "";

            public static void ResetResStr()
            {
                res = "";
            }

            public static string GetResStr()
            {
                return res;
            }

            public static void Execute(TreeNodeAdv treeNode)
            {
                if (treeNode.IsHidden)
                {
                    return;
                }

                var node = treeNode.Tag as Node;

                if (node.CheckState == CheckState.Checked)
                {
                    switch(node.Tag)
                    {
                        case EplanDevice.Device dev:
                            res += $"{dev.Name} ";
                            break;

                        case TechObject.Param par:
                            res += $"{par.GetNameLua()} ";
                            break;
                    }
                }

                if (treeNode.Children.Count > 0)
                {
                    List<TreeNodeAdv> childs = treeNode.Children.ToList();
                    foreach (TreeNodeAdv child in childs)
                    {
                        Execute(child);
                    }
                }
            }
        }

        /// <summary>
        /// Обработка выбора устройства для добавления его к шагу.
        /// После выбора устройства мы передаем новую строку с устройствами
        /// через делегат functionAfterCheck.
        /// </summary>        
        private void treeItem_AfterCheck(object sender, TreePathEventArgs e)
        {
            if (functionAfterCheck != null)
            {
                OnCheckOperationTree.ResetResStr();
                string res = "";

                treeItem_ChangeCheckBoxState(sender, e);

                int mainNodesLevel = 1;
                List<TreeNodeAdv> treeNodes = devicesTreeViewAdv.AllNodes
                    .Where(x => x.Level == mainNodesLevel).ToList();

                // Если выбранный эелемент редактора - параметр действия,
                // то можем выбрать только одно значение для него, остальные сбрасываем
                if (treeViewItemLastSelected is ActionParameter)
                {
                    devicesTreeViewAdv.AllNodes.Select(n => n.Tag as Node)
                        .Where(n => n != e.Path.LastNode && n.IsChecked)
                        .ToList().ForEach(n => n.CheckState = CheckState.Unchecked);
                }

                treeNodes.ForEach(OnCheckOperationTree.Execute);

                devicesTreeViewAdv.Refresh();

                res = OnCheckOperationTree.GetResStr();
                functionAfterCheck(res.Trim());
            }
        }

        /// <summary>
        /// Функция обновления состояний чекбоксов
        /// </summary>
        /// <param name="sender">Объект, который вызвал функцию</param>
        /// <param name="e">Контекст переданный вызывающим кодом</param>
        private void treeItem_ChangeCheckBoxState(object sender, 
            TreePathEventArgs e)
        {
            // Нажатый узел дерева
            object nodeObject = e.Path.LastNode;
            Node checkedNode = nodeObject as Node;
            StaticHelper.GUIHelper.CheckCheckState(checkedNode);
        }

        /// <summary>
        /// Обработка нажатий на кнопки уровней.
        /// </summary>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            devicesTreeViewAdv.BeginUpdate();

            int level = Convert.ToInt32((sender as ToolStripButton).Tag) + 
                correctionValue;
            devicesTreeViewAdv.CollapseAll();
            ExpandToLevel(level);

            devicesTreeViewAdv.EndUpdate();
        }

        /// <summary>
        /// Развертывание дерева до определенного уровня
        /// </summary>       
        /// <param name="level">Уровень развертывания дерева</param>
        private void ExpandToLevel(int level)
        {
            foreach (TreeNodeAdv node in devicesTreeViewAdv.AllNodes)
            {
                if (node.Level <= level)
                {
                    node.Expand();
                }
            }
        }

        /// <summary>
        /// Соответствующие шрифты узлов дерева.
        /// </summary>
        private readonly Font itemFontNoDevice = new Font(FontFamily.GenericMonospace, 8);
        private readonly Font itemFontIsDevice = new Font(FontFamily.GenericMonospace, 8, 
            FontStyle.Strikeout);
        private readonly Font boldFont = new Font(FontFamily.GenericMonospace, 8, FontStyle.Bold);

        private void devicesTreeViewAdv_DrawNodeColumn1(object sender,
            DrawTextEventArgs e)
        {
            e.TextColor = Color.Black;
            Node currentNode = (Node)e.Node.Tag;
            bool notParameter = 
                currentNode.Tag?.GetType() != typeof(TechObject.Param);
            bool needBoldFont = e.Node.Level == 1 ||
                (e.Node.Level == 2 && notParameter);
            if (needBoldFont)
            {
                e.Font = boldFont;
                return;
            }

            if (currentNode.Tag is IODevice.IOChannel ch)
            {
                e.Font = itemFontNoDevice;
                if (!ch.IsEmpty())
                {
                    e.Font = itemFontIsDevice;
                    e.Text = ch.Name + " " + ch.Comment +
                        $" (A{ch.FullModule}:" + ch.PhysicalClamp + ")";
                }
                else
                {
                    e.Text = ch.Name + " " + ch.Comment;
                }
            }
            else e.Font = itemFontNoDevice;
        }

        private void devicesTreeViewAdv_DrawNodeColumn2(object sender,
            DrawTextEventArgs e)
        {
            e.TextColor = Color.Black;
            Node currentNode = (Node)e.Node.Tag;
            var parameter = currentNode.Tag as IODevice.Parameter;
            if (parameter != null)
            {
                e.Text = IODevice.Parameter.GetFormatValue(parameter,
                    (currentNode as ColumnNode).Value, currentNode.Parent.Parent.Tag as IODevice);
            }
        }

        private void devicesTreeViewAdv_EditNodeColumn2(object sender,
            LabelEventArgs e)
        {
            var device = (e.Subject as Node).Parent.Parent.Tag as IODevice;
            var node = (e.Subject as ColumnNode);

            node.Update(device, e.NewLabel, e.OldLabel);
        }

        private void noAssigmentBtn_Click(object sender, EventArgs e)
        {
            if (prevShowChannels == true)
            {
                if (noAssigmentBtn.Checked)
                {
                    noAssigmentBtn.Checked = false;
                }
                else
                {
                    noAssigmentBtn.Checked = true;
                }

                OnSetNewValue onSetNewValue = null;
                bool isRebuiltTree = true;
                ShowDisplayObjects(treeViewItemLastSelected, onSetNewValue, 
                    isRebuiltTree);
            }
        }

        private void DisplayParamsBtn_Click(object sender, EventArgs e)
        {
            if (prevShowChannels == true)
            {
                if (displayParamsBtn.Checked)
                {
                    displayParamsBtn.Checked = false;
                }
                else
                {
                    displayParamsBtn.Checked = true;
                }

                OnSetNewValue onSetNewValue = null;
                bool isRebuiltTree = true;
                ShowDisplayObjects(treeViewItemLastSelected, onSetNewValue,
                    isRebuiltTree);
            }
            ChangeUISize();
        }

        private void synchBtn_Click(object sender, EventArgs e)
        {
            bool saveDescrSilentMode = false;
            EProjectManager.GetInstance().SyncAndSave(saveDescrSilentMode);

            Editor.Editor.GetInstance().EditorForm.RefreshTree();
            IOViewControl.Instance?.RebuildTree();
            EasyEPlanner.Devices.View.DevicesViewControl.Instance?.RebuildTree();

            RefreshTree();
        }

        /// <summary>
        /// Обновление дерева на основе текущих устройств.
        /// </summary>
        public void RefreshTree()
        {
            Refresh("", -1);
        }

        /// <summary>
        /// Обновление дерева после привязки устройства к каналу при
        /// двойном клике
        /// </summary>
        public void RefreshTreeAfterBinding()
        {
            if (noAssigmentBtn.Checked && deviceIsShown)
            {
                devicesTreeViewAdv.BeginUpdate();
                List<TreeNodeAdv> treeNodes = devicesTreeViewAdv.AllNodes
                    .ToList();
                TreeNodeAdv treeNode = treeNodes[0];
                RefreshOperationTree.Execute(treeNode);
                treeNodes.Clear();
                treeNodes = devicesTreeViewAdv.AllNodes.ToList();
                treeNode = treeNodes[0];
                OnHideOperationTree.Execute(treeNode);
                devicesTreeViewAdv.EndUpdate();
                devicesTreeViewAdv.Refresh();
            }
            else
            {
                devicesTreeViewAdv.Refresh();
            }
        }

        /// <summary>
        /// Последний выбранный элемент в форме
        /// </summary>
        /// <returns>Узел с информацией</returns>
        public TreeNodeAdv GetLastSelectedNode()
        {
            return devicesTreeViewAdv.SelectedNodes.Last();
        }

        private void devicesTreeViewAdv_SizeChanged(object sender, EventArgs e)
        {
            foreach(var column in devicesTreeViewAdv.Columns)
            {
                devicesTreeViewAdv.AutoSizeColumn(column);
            }
        }
    }
}
