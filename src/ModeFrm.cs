using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using PInvoke;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EasyEPlanner
{
    public partial class ModeFrm : Form
    {
        private ModeFrm()
        {
            InitializeComponent();

            const string columnName = "Операции";
            StaticHelper.GUIHelper.SetUpAdvTreeView(modesTreeViewAdv,
                columnName, modesTreeViewAdv_DrawNode, nodeCheckBox);

            dialogCallbackDelegate =
                new PI.HookProc(DlgWndHookCallbackFunction);
        }

        /// <summary>
        /// Чекбокс для дерева
        /// </summary>
        NodeCheckBox nodeCheckBox = new NodeCheckBox();

        private static ModeFrm mFrm = null;

        /// <summary>
        /// Выбранный элемент в редакторе объектов.
        /// </summary>
        public EditType SelectedTreeItem { get; private set; }

        /// <summary>
        /// Функция для обработки завершения работы окна устройств.
        /// </summary>
        public void CloseEditor()
        {
            modesTreeViewAdv.Model = null;

            PI.UnhookWindowsHookEx(dialogHookPtr);

            PI.SetParent(modesTreeViewAdv.Handle, this.Handle);
            PI.SetParent(toolStrip.Handle, this.Handle);

            System.Threading.Thread.Sleep(1);
            modeIsShown = false;
            isLoaded = false;
        }

        public static ModeFrm GetInstance()
        {
            if (mFrm == null)
            {
                mFrm = new ModeFrm();
            }

            return mFrm;
        }

        public bool IsVisible()
        {
            return modeIsShown;
        }

        public static void CheckShown()
        {
            if (PI.IsWindowVisible(wndModeVisibilePtr) == true)
            {
                modeIsShown = true;
            }
            else
            {
                modeIsShown = false;
            }
        }

        public static void SaveCfg(bool wndState)
        {
            string path = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);
            var ini = new IniFile(path + @"\Eplan\eplan.cfg");

            if (wndState == true)
            {
                ini.WriteString("main", "show_oper_window", "true");
            }
            else
            {
                ini.WriteString("main", "show_oper_window", "false");
            }
        }

        static string caption = "Операции\0";
        static byte[] newCapt = EncodingDetector.Windows1251.GetBytes(caption);

        private PI.HookProc dialogCallbackDelegate = null;
        private IntPtr dialogHookPtr = IntPtr.Zero;

        private IntPtr dialogHandle = IntPtr.Zero;
        private IntPtr wndHandle = IntPtr.Zero;
        private IntPtr panelPtr = IntPtr.Zero;

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
                        var p = new PI.WINDOWPOS();
                        p = (PI.WINDOWPOS)
                            System.Runtime.InteropServices.Marshal
                            .PtrToStructure(lParam, typeof(PI.WINDOWPOS));

                        break;

                    case (int)PI.WM.DESTROY:
                        PI.UnhookWindowsHookEx(dialogHookPtr);
                        dialogHookPtr = IntPtr.Zero;
                        dialogHandle = IntPtr.Zero;

                        PI.SetParent(modesTreeViewAdv.Handle, this.Handle);
                        PI.SetParent(toolStrip.Handle, this.Handle);
                        this.Controls.Add(modesTreeViewAdv);
                        this.Controls.Add(toolStrip);
                        modesTreeViewAdv.Hide();
                        System.Threading.Thread.Sleep(1);
                        modeIsShown = false;
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
        /// Изменить размер UI
        /// </summary>
        private void ChangeUISize()
        {
            IntPtr dialogPtr = PI.GetParent(modesTreeViewAdv.Handle);

            PI.RECT rctDialog;
            PI.GetWindowRect(dialogPtr, out rctDialog);

            int w = rctDialog.Right - rctDialog.Left;
            int h = rctDialog.Bottom - rctDialog.Top;

            toolStrip.Location = new Point(0, 0);
            modesTreeViewAdv.Location = new Point(0, toolStrip.Height);

            toolStrip.Width = w;
            modesTreeViewAdv.Width = w;
            modesTreeViewAdv.Height = h - toolStrip.Height;
        }

        /// <summary>
        /// Дескриптор окна
        /// </summary>
        public static IntPtr wndModeVisibilePtr;

        /// <summary>
        /// Показано ли окно
        /// </summary>
        public static bool modeIsShown = false;

        /// <summary>
        /// Загружена ли форма
        /// </summary>
        public static bool isLoaded = false;

        /// <summary>
        /// Показать диалог (окно с редактором).
        /// </summary>

        public void ShowDlg()
        {
            System.Diagnostics.Process oCurrent =
                System.Diagnostics.Process.GetCurrentProcess();

            // Идентификатор команды вызова окна "Кабели"
            const int wndWmCommand = 35087;
            string windowName = "Кабели";

            if (isLoaded == true)
            {
                StaticHelper.GUIHelper.ShowHiddenWindow(oCurrent,
                    wndModeVisibilePtr, wndWmCommand);
                return;
            }

            StaticHelper.GUIHelper.SearchWindowDescriptor(oCurrent, windowName,
                wndWmCommand, ref dialogHandle, ref wndModeVisibilePtr);
            if (wndModeVisibilePtr != IntPtr.Zero)
            {
                StaticHelper.GUIHelper.ShowHiddenWindow(oCurrent,
                    wndModeVisibilePtr, wndWmCommand);
                if (isLoaded == false)
                {
                    StaticHelper.GUIHelper.ChangeWindowMainPanels(
                        ref dialogHandle, ref panelPtr);

                    Controls.Clear();

                    // Переносим на найденное окно свои элементы (SetParent) и
                    // подгоняем их размеры и позицию.
                    PI.SetParent(modesTreeViewAdv.Handle, dialogHandle);
                    PI.SetParent(toolStrip.Handle, dialogHandle);
                    ChangeUISize();

                    // Устанавливаем свой хук для найденного окна
                    // (для изменения размеров своих элементов, сохранения
                    // изменений при закрытии и отключения хука).
                    SetUpHook();

                    modeIsShown = true;
                    isLoaded = true;
                }           
            }
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

        public delegate void OnSetNewValue(IDictionary<int, List<int>> dict);
        public OnSetNewValue functionAfterCheck = null;

        #region Реализация под новый редактор в т.ч. привязка агрегатов
        /// <summary>
        /// Выбор операции на дереве, которые входят в ограничения.
        /// </summary> 
        /// <param name="checkedDev">Объект, в котором хранятся ограничения.
        /// </param>
        /// <param name="function">Делегат, который вызывается при последующих 
        /// изменениях операций в ограничениях.</param>
        public void SelectDevices(Editor.ITreeViewItem checkedDev,
            OnSetNewValue function)
        {
            modesTreeViewAdv.BeginUpdate();
            foreach (TreeNodeAdv boxedNode in modesTreeViewAdv.AllNodes)
            {
                Node node = boxedNode.Tag as Node;
                node.CheckState = CheckState.Unchecked;
            }

            nodeCheckBox.CheckStateChanged -= treeItem_AfterCheck;

            if (function != null)
            {
                functionAfterCheck = function;
            }

            var treeModel = modesTreeViewAdv.Model as TreeModel;
            List<Node> nodes = treeModel.Nodes.ToList();
            SelectedDevices(nodes, checkedDev);

            nodeCheckBox.CheckStateChanged += treeItem_AfterCheck;

            modesTreeViewAdv.EndUpdate();

            var root = (Node)modesTreeViewAdv.AllNodes.First()?.Tag;
            OnCheckOperationTree.RefreshDict(root);
        }

        /// <summary>
        /// Переключение состояния чекбоксов операции
        /// </summary>
        /// <param name="nodes">узлы</param>
        /// <param name="checkedMode">выбранные (отмеченные) операции</param>
        private void SelectedDevices(List<Node> nodes,
            Editor.ITreeViewItem checkedMode)
        {
            foreach (Node subNode in nodes)
            {
                var item = subNode.Tag as Editor.ITreeViewItem;
                if (item != null)
                {
                    if (item.IsMainObject)
                    {
                        SelectAttachedObject(subNode, checkedMode);
                    }
                    else if (item.IsMode)
                    {
                        SelectRestriction(nodes, subNode, checkedMode);
                    }
                }
                SelectedDevices(subNode.Nodes.ToList(), checkedMode);
            }
        }

        /// <summary>
        /// Отметка ограничения в дереве
        /// </summary>
        /// <param name="nodes">Список узлов</param>
        /// <param name="subNode">Текущий узел</param>
        /// <param name="checkedMode">Выбранный элемент на дереве</param>
        private void SelectRestriction(List<Node> nodes, Node subNode,
            Editor.ITreeViewItem checkedMode)
        {
            var restriction = checkedMode as TechObject.Restriction;
            if (restriction == null)
            {
                return;
            }

            Node parentNode = subNode.Parent;
            int objectNum = TechObject.TechObjectManager
                .GetInstance()
                .GetTechObjectN(parentNode.Tag);
            int modeNum = nodes.IndexOf(subNode) + 1;
            bool correctRestriction =
                restriction.RestrictDictionary != null &&
                restriction.RestrictDictionary
                .ContainsKey(objectNum) &&
                restriction.RestrictDictionary[objectNum]
                .Contains(modeNum);
            if (correctRestriction)
            {
                subNode.CheckState = CheckState.Checked;
                StaticHelper.GUIHelper.CheckCheckState(subNode);
            }
        }

        /// <summary>
        /// Отметка привязанных объектов в дереве
        /// </summary>
        /// <param name="subNode">Текущий узел</param>
        /// <param name="checkedMode">Выбранный элемент на дереве</param>
        private void SelectAttachedObject(Node subNode,
            Editor.ITreeViewItem checkedMode)
        {
            var attachedObjects = checkedMode as TechObject.AttachedObjects;
            if (attachedObjects == null)
            {
                return;
            }

            int objectNum = TechObject.TechObjectManager
                .GetInstance()
                .GetTechObjectN(subNode.Tag);
            bool correctObject = attachedObjects.Value
                .Split(' ').ToArray()
                .Contains(objectNum.ToString());
            if (correctObject)
            {
                subNode.CheckState = CheckState.Checked;
                StaticHelper.GUIHelper.CheckCheckState(subNode);
            }
        }

        /// <summary>
        /// Построение дерева на основе определенных операций проекта.
        /// </summary>   
        public bool ShowModes(TechObject.TechObjectManager techManager,
           bool showCheckBoxes, bool showOneNode, Editor.ITreeViewItem item,
           Editor.ITreeViewItem checkedMode,
           OnSetNewValue function, bool isRebuiltTree = false)
        {
            if (function != null)
            {
                functionAfterCheck = function;
            }

            if (showCheckBoxes)
            {
                modesTreeViewAdv.NodeControls.Insert(0, nodeCheckBox);
            }
            else
            {
                modesTreeViewAdv.NodeControls.Remove(nodeCheckBox);
            }

            SetEditMode(checkedMode);

            if (isRebuiltTree == true)
            {
                Refresh(techManager, checkedMode, showOneNode, item);
            }

            ShowDlg();
            return true;
        }

        /// <summary>
        /// Установить режим редактирования в зависимости от выбранного элемента
        /// на дереве объектов.
        /// </summary>
        /// <param name="checkedItem">Выбранный элемент</param>
        private void SetEditMode(Editor.ITreeViewItem checkedItem)
        {
            switch(checkedItem)
            {
                case TechObject.AttachedObjects item:
                    bool aggregatesAttaching = item.WorkStrategy.UseInitialization == true;
                    if (aggregatesAttaching)
                    {
                        

                        bool toUnit = item.Owner.BaseTechObject.S88Level == 
                            (int)TechObject.BaseTechObjectManager.ObjectType.Unit;
                        if (toUnit)
                        {
                            SelectedTreeItem = EditType.AttachedAgregatesToUnit;
                        }
                        else
                        {
                            SelectedTreeItem = EditType.AttachedAggregatesToAggregates;
                        }
                    }
                    else
                    {
                        if (item.Owner is null)
                        {
                            SelectedTreeItem = EditType.AttachedObjectToStep;
                            return;
                        }

                        SelectedTreeItem = EditType.AttachedUnitsToObjectGroup;
                    }
                    break;

                case TechObject.Restriction _:
                    SelectedTreeItem = EditType.Restriction;
                    break;

                default:
                    SelectedTreeItem = EditType.None;
                    break;
            }
        }

        /// <summary>
        /// Обновление дерева на основе текущих устройств проекта.
        /// </summary>
        /// <param name="techManager">Менеджер техустройств проекта.</param>
        /// <param name="checkedMode">Выбранные операции.</param>
        private void Refresh(TechObject.TechObjectManager techManager,
            Editor.ITreeViewItem checkedMode, bool showOneNode,
            Editor.ITreeViewItem item)
        {
            modesTreeViewAdv.BeginUpdate();

            modesTreeViewAdv.Model = null;
            modesTreeViewAdv.Refresh();
            var treeModel = new TreeModel();

            var root = new Node(techManager.DisplayText[0]);
            root.Tag = techManager;
            treeModel.Nodes.Add(root);

            int techObjectNumber = 0;
            int modeNumber = 0;
            var mainTechObject = item as TechObject.TechObject;

            FillTreeObjects(techManager.Items, root, mainTechObject,
                showOneNode, ref techObjectNumber, ref modeNumber,
                checkedMode);

            SetUpTreeVisibility(root, checkedMode);

            modesTreeViewAdv.Model = treeModel;

            List<TreeNodeAdv> nodes = modesTreeViewAdv.AllNodes.ToList();
            TreeNodeAdv treeNode = nodes[0];
            OnHideOperationTree.Execute(treeNode);

            modesTreeViewAdv.ExpandAll();
            modesTreeViewAdv.Refresh();
            modesTreeViewAdv.EndUpdate();
        }

        /// <summary>
        /// Заполнение дерева с операциями для настройки ограничений
        /// </summary>
        /// <param name="treeItems"></param>
        /// <param name="root"></param>
        /// <param name="mainTechObject"></param>
        /// <param name="showOneNode"></param>
        /// <param name="techObjNum"></param>
        /// <param name="modeNum"></param>
        /// <param name="checkedMode"></param>
        private void FillTreeObjects(Editor.ITreeViewItem[] treeItems,
            Node root, TechObject.TechObject mainTechObject,
            bool showOneNode, ref int techObjNum, ref int modeNum,
            Editor.ITreeViewItem checkedMode)
        {
            bool notShowAllOperations = checkedMode != null;
            bool notAllowedTypes = SelectedTreeItem == EditType.None ||
                (checkedMode?.IsEditable ?? false) == false;
            if (notAllowedTypes && notShowAllOperations)
            {
                ShowNoModes();
                return;
            }

            foreach (var treeItem in treeItems)
            {

                string treeItemName = $"{treeItem.DisplayText[0]}";
                int? techType = (treeItem as TechObject.TechObject)?.TechType;
                if (techType.HasValue && techType != -1)
                {
                    treeItemName += $" {{{techType}}}";
                }

                var parentNode = new Node(treeItemName);
                parentNode.Tag = treeItem;
                root.Nodes.Add(parentNode);

                if (treeItem is TechObject.TechObject techObject)
                {
                    techObjNum = TechObject.TechObjectManager.GetInstance()
                        .GetTechObjectN(techObject);
                    List<TechObject.Mode> modes = techObject.ModesManager
                        .Modes;

                    if (SelectedTreeItem == EditType.Restriction)
                    {
                        FillTreeObjectsModes(modes, parentNode, checkedMode,
                            techObject, ref techObjNum, ref modeNum);
                    }

                    SetUpTechObjectNodeVisibility(showOneNode, parentNode,
                        techObject, mainTechObject);
                }
                else
                {
                    FillTreeObjects(treeItem.Items, parentNode, mainTechObject,
                        showOneNode, ref techObjNum, ref modeNum, checkedMode);
                }
            }
        }

        /// <summary>
        /// Заполнение дерева объектов операциями.
        /// </summary>
        /// <param name="modes">Операции</param>
        /// <param name="parentNode">Родительский узел</param>
        /// <param name="checkedMode">Выбранный режим</param>
        /// <param name="techObject">Технологический объект</param>
        /// <param name="techObjNum">Номер объекта</param>
        /// <param name="modeNum">Номер операции</param>
        private void FillTreeObjectsModes(List<TechObject.Mode> modes,
            Node parentNode, Editor.ITreeViewItem checkedMode,
            TechObject.TechObject techObject, ref int techObjNum,
            ref int modeNum)
        {
            var restriction = checkedMode as TechObject.Restriction;
            foreach (var mode in modes)
            {
                modeNum = mode.GetModeNumber();
                var childNode = new Node(mode.DisplayText[0]);
                childNode.Tag = mode;
                parentNode.Nodes.Add(childNode);

                if (checkedMode != null)
                {
                    var restrictionManager = checkedMode.Parent;
                    var selectedMode = restrictionManager.Parent as
                        TechObject.Mode;
                    var modeManager = selectedMode.Parent;
                    var selectedTechObject = modeManager.Parent as
                        TechObject.TechObject;
                    bool notSameObjects =
                        techObject.DisplayText[0] == selectedTechObject
                        .DisplayText[0] && mode.Name == selectedMode.Name;
                    if (notSameObjects)
                    {
                        childNode.IsHidden = true;
                    }
                }

                if (checkedMode != null &&
                    restriction.RestrictDictionary != null &&
                    restriction.RestrictDictionary.ContainsKey(techObjNum) &&
                    restriction.RestrictDictionary[techObjNum]
                    .Contains(modeNum))
                {
                    childNode.CheckState = CheckState.Checked;
                }
                else
                {
                    childNode.CheckState = CheckState.Unchecked;
                }
            }
        }

        /// <summary>
        /// Настройка видимости узла в дереве для тех. объектов и операций
        /// </summary>
        /// <param name="showOneNode">Флаг, указывающий скрывать ли узлы,
        /// которые не равны выделенному объекту</param>
        /// <param name="parentNode">Родительский узел</param>
        /// <param name="techObject">Технологический объект</param>
        /// <param name="mainTechObject">Выбранный технологический объект
        /// </param>
        private void SetUpTechObjectNodeVisibility(bool showOneNode,
            Node parentNode, TechObject.TechObject techObject,
            TechObject.TechObject mainTechObject)
        {
            if (showOneNode == true)
            {
                if (techObject != mainTechObject)
                {
                    SetUpHiddenProperty(parentNode);
                }
            }
            else
            {
                if (techObject == mainTechObject)
                {
                    SetUpHiddenProperty(parentNode);
                }
            }
        }

        /// <summary>
        /// Установка пометки узла на скрытие для тех. объектов и операций.
        /// </summary>
        /// <param name="parentNode">Узел для установки</param>
        private void SetUpHiddenProperty(Node parentNode)
        {
            parentNode.IsHidden = true;
            foreach (Node child in parentNode.Nodes)
            {
                child.IsHidden = true;
            }
        }

        /// <summary>
        /// Обход дерева и отметка элементов, которые надо скрыть.
        /// </summary>
        /// <param name="node">Узел дерева, корень</param>
        /// <param name="checkedNode">Выбранный узел</param>
        /// <returns>Нужно ли скрыть переданные</returns>
        private bool SetUpTreeVisibility(Node node,
            Editor.ITreeViewItem checkedNode)
        {
            bool withoutChildren = node.Nodes.Count == 0;
            var item = node.Tag as Editor.ITreeViewItem;
            if (withoutChildren)
            {
                if (item != null &&
                    item.IsMainObject &&
                    SelectedTreeItem == EditType.Restriction)
                {
                    return true;
                }
                else
                {
                    return node.IsHidden;
                }
            }

            bool allChildItemsHidden = node.Nodes
                .Where(x => x.IsHidden).Count() == node.Nodes.Count;

            if (allChildItemsHidden)
            {
                return true;
            }
            else
            {
                foreach (var childTreeNode in node.Nodes)
                {
                    bool isHidden = SetUpTreeVisibility(childTreeNode,
                        checkedNode);
                    childTreeNode.IsHidden = isHidden;
                }

                allChildItemsHidden = node.Nodes
                    .Where(x => x.IsHidden).Count() == node.Nodes.Count;
                if (allChildItemsHidden)
                {
                    bool hideItem = true;
                    return hideItem;
                }
                else
                {
                    bool notHideItem = false;
                    return notHideItem;
                }
            }
        }
        #endregion

        /// <summary>
        /// Отключение видимости всех операций
        /// </summary>
        /// <returns></returns>
        public bool ShowNoModes()
        {
            modesTreeViewAdv.BeginUpdate();

            TreeModel model = new TreeModel();
            model.Nodes.Clear();

            Node root = new Node("Операции проекта");
            model.Nodes.Add(root);
            modesTreeViewAdv.Model = model;

            modesTreeViewAdv.EndUpdate();

            return true;
        }

        /// <summary>
        /// Отключение видимости операций в дереве
        /// </summary>
        static class OnHideOperationTree
        {
            public static void Execute(TreeNodeAdv treeNode)
            {
                int correctionForTreeLevelWithNewControl = 2;

                Node node = treeNode.Tag as Node;
                if (treeNode.Level == correctionForTreeLevelWithNewControl &&
                    treeNode.Children.Count < 1)
                {
                    treeNode.IsHidden = true;
                    return;
                }

                if (treeNode.Children.Count > 0)
                {
                    bool isHidden = true;
                    List<TreeNodeAdv> childs = treeNode.Children.ToList();
                    foreach (TreeNodeAdv childTree in childs)
                    {
                        Node childNode = childTree.Tag as Node;
                        if (childNode.IsHidden == false)
                        {
                            isHidden = false;
                            Execute(childTree);
                        }

                        treeNode.IsHidden = isHidden;
                    }
                }

                if (node.IsHidden == true)
                {
                    node.IsHidden = false; // Костыль, иначе не скрывает.
                    treeNode.IsHidden = true;
                }
            }
        }

        #region OnCheckOperationTree
        /// <summary>
        /// Выбор устройств, участвующих в операции/ограничении/привязке.
        /// </summary>         
        static class OnCheckOperationTree
        {
            static Dictionary<int, List<int>> resDict =
                new Dictionary<int, List<int>>();

            /// <summary>
            /// Нужно ли использовать родительский узел выбранного узла
            /// </summary>
            public static bool UseParentNode { get; set; } = false;

            /// <summary>
            /// Обновить словарь выбранных объектов
            /// </summary>
            /// <param name="root">Узел дерева с отмеченными объектами</param>
            public static void RefreshDict(Node root)
            {
                resDict.Clear();
                MakeResDictFromRootNode(root);
            }

            /// <summary>
            /// Сделать словарь отмеченных объектов из корневого узла.
            /// Рекурсивный метод.
            /// </summary>
            /// <param name="node">Корневой узел</param>
            private static void MakeResDictFromRootNode(Node node)
            {
                if (node.IsHidden ||
                    node.CheckState == CheckState.Unchecked)
                {
                    return;
                }

                if (node.Tag is Editor.TreeViewItem item)
                {
                    if (item.IsMainObject)
                    {
                        int objectIndex = TechObject.TechObjectManager
                            .GetInstance().GetTechObjectN(node.Tag);
                        AddToResDict(objectIndex);
                    }
                    else if (item.IsMode)
                    {
                        int objectIndex = TechObject.TechObjectManager
                            .GetInstance().GetTechObjectN(node.Parent.Tag);
                        int modeIndex = node.Parent.Nodes.IndexOf(node) + 1;
                        AddToResDict(objectIndex, modeIndex);
                    }
                }

                foreach(var child in node.Nodes)
                {
                    MakeResDictFromRootNode(child);
                }
            }

            /// <summary>
            /// Добавить отмеченный объект в словарь
            /// </summary>
            /// <param name="objIndex">Индекс объекта</param>
            /// <param name="modeIndex">Индекс операции (опционально)</param>
            private static void AddToResDict(int objIndex, int modeIndex = 0)
            {
                if (resDict.ContainsKey(objIndex))
                {
                    resDict[objIndex].Add(modeIndex);
                }
                else
                {
                    resDict.Add(objIndex, new List<int>());
                }
            }

            public static IDictionary<int, List<int>> GetResDict()
            {
                return resDict;
            }

            public static void Execute(Node node, EditType selectedEditType)
            {
                if (node.IsHidden)
                {
                    return;
                }

                var item = node.Tag as Editor.ITreeViewItem;
                if (item != null)
                {
                    bool attachToObjEdit = item.IsMainObject &&
                    (selectedEditType == EditType.AttachedAgregatesToUnit ||
                    selectedEditType == EditType.AttachedUnitsToObjectGroup ||
                    selectedEditType == EditType.AttachedAggregatesToAggregates ||
                    selectedEditType == EditType.AttachedObjectToStep);
                    bool restrictionEdit = item.IsMode &&
                        selectedEditType == EditType.Restriction;
                    if (attachToObjEdit)
                    {
                        ExecuteAttachedObjects(node, node.CheckState);
                    }
                    else if (restrictionEdit)
                    {
                        ExecuteRestrictions(node, node.CheckState);
                    }
                }

                if (node.Nodes.Count > 0)
                {
                    List<Node> childs = node.Nodes.ToList();
                    foreach (var child in childs)
                    {
                        Execute(child, selectedEditType);
                    }
                }
            }

            /// <summary>
            /// Обработка отметки операции в новом редакторе в ограничениях
            /// </summary>
            /// <param name="node">Узел дерева</param>
            private static void ExecuteRestrictions(Node node,
                CheckState checkState)
            {
                Node parentNode = node.Parent;
                int objectIndex = TechObject.TechObjectManager.GetInstance()
                    .GetTechObjectN(parentNode.Tag);
                int modeIndex = parentNode.Nodes.IndexOf(node) + 1;

                switch(checkState)
                {
                    case CheckState.Checked:
                        AddRestriction(objectIndex, modeIndex);
                        break;

                    case CheckState.Unchecked:
                        DeleteRestriction(objectIndex, modeIndex);
                        break;
                }
            }

            /// <summary>
            /// Добавление ограничений в словаре.
            /// </summary>
            /// <param name="objectIndex">Индекс объекта</param>
            /// <param name="modeIndex">Индекс операции</param>
            private static void AddRestriction(int objectIndex,
                int modeIndex)
            {
                if (resDict.ContainsKey(objectIndex))
                {
                    resDict[objectIndex].Add(modeIndex);
                    resDict[objectIndex].Sort();
                }
                else
                {
                    List<int> modeList = new List<int>();
                    modeList.Add(modeIndex);
                    resDict.Add(objectIndex, modeList);
                }
            }

            /// <summary>
            /// Удаление ограничений из словаря.
            /// </summary>
            /// <param name="objectIndex">Индекс объекта</param>
            /// <param name="modeIndex">Индекс операции</param>
            private static void DeleteRestriction(int objectIndex,
                int modeIndex)
            {
                if(resDict.Count > 0 &&
                    resDict.ContainsKey(objectIndex) &&
                    resDict[objectIndex].Contains(modeIndex) == true)
                {
                    resDict[objectIndex].Remove(modeIndex);

                    if(resDict[objectIndex].Count == 0)
                    {
                        resDict.Remove(objectIndex);
                    }
                }
            }

            /// <summary>
            /// Обработка отметки объекта в новом редакторе при настройке
            /// привязанных объектов
            /// </summary>
            /// <param name="node">Узел объекта</param>
            private static void ExecuteAttachedObjects(Node node,
                CheckState checkState)
            {
                int objectIndex = TechObject.TechObjectManager.GetInstance()
                    .GetTechObjectN(node.Tag);

                // Заполняем нулями список т.к правая часть не нужна.
                switch (checkState)
                {
                    case CheckState.Checked:
                        resDict.Add(objectIndex, new List<int>(0));
                        break;

                    case CheckState.Unchecked:
                        resDict.Remove(objectIndex);
                        break;
                }
            }
        }
        #endregion

        /// <summary>
        /// Обработка выбора операции для добавления в ограничение.
        /// После выбора операции мы передаем новый словарь с ограничениями
        /// через делегат functionAfterCheck.
        /// </summary>        
        private void treeItem_AfterCheck(object sender, TreePathEventArgs e)
        {
            if (functionAfterCheck != null)
            {
                treeItem_ChangeCheckBoxState(sender, e);

                Node selectedNode = GetSelectedNodeForCheckOperationTree(e);
                
                if (SelectedTreeItem == EditType.AttachedObjectToStep)
                {
                    // обход всего дерева  
                    OnCheckOperationTree.Execute(e.Path.FirstNode as Node, SelectedTreeItem);
                    functionAfterCheck(new Dictionary<int, List<int>>()
                        { [OnCheckOperationTree.GetResDict().Keys.FirstOrDefault()] = null });
                }
                else
                {
                    OnCheckOperationTree.Execute(selectedNode, SelectedTreeItem);
                    functionAfterCheck(OnCheckOperationTree.GetResDict());
                }
                
                modesTreeViewAdv.Refresh();
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
            
            if (SelectedTreeItem == EditType.AttachedAgregatesToUnit)
            {
                UnselectIncorrectValues(e, checkedNode.Text);
            }

            if (SelectedTreeItem == EditType.AttachedObjectToStep)
            {
                Unchecked(e.Path.FirstNode as Node, checkedNode);
            }

            StaticHelper.GUIHelper.CheckCheckState(checkedNode);
        }

        /// <summary>
        /// Рекурсивная функция обхода дерева для снятия всеъ чекбоксов кроме выбранного
        /// </summary>
        /// <param name="root"></param>
        /// <param name="checkedNode"></param>
        public static void Unchecked(Node root, Node checkedNode)
        {
            foreach (var node in root.Nodes)
            {
                if (node == checkedNode) continue;

                if (node.Nodes.Count > 0)
                    Unchecked(node, checkedNode);
                
                node.CheckState = CheckState.Unchecked;

                OnCheckOperationTree.UseParentNode = true;    
            }
        }

        /// <summary>
        /// Снять выделение с некорректных значений внутри узла.
        /// </summary>
        /// <param name="e">Контекст переданный вызывающим кодом</param>
        /// <param name="selectedNodeText">Выбранное значение</param>
        private void UnselectIncorrectValues(TreePathEventArgs e,
            string selectedNodeText)
        {
            var lastNode = e.Path.LastNode as Node;
            int fullPathLength = e.Path.FullPath.Length;
            if (lastNode.Nodes.Count == 0 && fullPathLength > 1)
            {
                int preLastIndexOffset = 2;
                int preLastNodeIndex = fullPathLength - preLastIndexOffset;
                var preLastNode = e.Path.FullPath[preLastNodeIndex] as Node;
                foreach(var node in preLastNode.Nodes)
                {
                    if(node.Text != selectedNodeText)
                    {
                        node.CheckState = CheckState.Unchecked;
                        OnCheckOperationTree.UseParentNode = true;
                    }
                }
            }
            else
            {
                lastNode.CheckState = CheckState.Unchecked;
            }
        }

        /// <summary>
        /// Получить выбранный узел для выбора устройств записываемых в поле.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private Node GetSelectedNodeForCheckOperationTree(TreePathEventArgs e)
        {
            Node selectedNode;
            if (!OnCheckOperationTree.UseParentNode)
            {
                selectedNode = (Node)e.Path.LastNode;
            }
            else
            {
                var path = e.Path.FullPath;
                int pathLength = path.Length;
                if (pathLength > 1)
                {
                    var lastNode = (Node)path.Last();
                    selectedNode =  lastNode.Parent;
                }
                else
                {
                    selectedNode = (Node)path.First();
                }

                OnCheckOperationTree.UseParentNode = false;
            }

            return selectedNode;
        }

        /// <summary>
        /// Обработка нажатий на кнопки уровней.
        /// </summary>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            modesTreeViewAdv.BeginUpdate();

            int level = Convert.ToInt32((sender as ToolStripButton).Tag);
            modesTreeViewAdv.CollapseAll();
            if (level >= 0)
            {
                ExpandToLevel(level);
            }

            modesTreeViewAdv.EndUpdate();
        }

        /// <summary>
        /// Развертывание дерева до определенного уровня
        /// </summary>       
        /// <param name="level">Уровень развертывания дерева</param>
        private void ExpandToLevel(int level)
        {
            foreach (TreeNodeAdv node in modesTreeViewAdv.AllNodes)
            {
                if (node.Level <= level)
                {
                    node.Expand();
                }
            }
        }

        /// <summary>
        /// Событие отрисовки дерева
        /// </summary>
        /// <param name="sender">Объект вызвавший событие</param>
        /// <param name="e">Контекст переданный событием</param>
        private void modesTreeViewAdv_DrawNode(object sender,
            DrawTextEventArgs e)
        {
            e.TextColor = Color.Black;
        }

        /// <summary>
        /// Тип редактируемого элемента
        /// </summary>
        public enum EditType
        {
            None,
            Restriction,
            AttachedAgregatesToUnit,
            AttachedUnitsToObjectGroup,
            AttachedAggregatesToAggregates,
            AttachedObjectToStep, // Привязка объекта к шагу в поле "Связанный объект"
        }
    }
}
