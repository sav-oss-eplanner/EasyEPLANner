using BrightIdeasSoftware;
using EasyEPlanner;
using Editor;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.Scripting;
using IO.ViewModel;
using IO.ViewModel.ViewInterface;
using PInvoke;
using StaticHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IO.View
{
    public partial class IOViewControl : Form
    {
        private bool cancelChanges = false;

        private bool isCellEditing = false;

        private bool cellEditUsesMultiline = false;

        private bool cellEditUsesComboBox = false;

        private readonly PlcCellEditKeyEngine cellEditKeyEngine = new();

        private IEditable cellEditItem;

        private bool isDraggingDeletedModule = false;

        private object dragHoverRowObject;

        private ToolStripMenuItem shiftModulesToolStripMenuItem;

        private ToolStripMenuItem deleteUndefinedModuleToolStripMenuItem;

        private ToolStripMenuItem goToFasToolStripMenuItem;

        private ToolStripMenuItem restoreDeletedModulesToolStripMenuItem;

        private ToolStripMenuItem reserveErrorClampsToolStripMenuItem;

        private sealed class DraggedModule
        {
            public DeletedModule DeletedModule { get; set; }
        }

        [ExcludeFromCodeCoverage]
        private sealed class ModuleDropTarget
        {
            public IIONode Node { get; set; }

            public IModule Module { get; set; }

            public bool IsNodeEnd => Module == null;
        }

        public static IOViewControl Instance { get; private set; }

        /// <summary>
        /// Модель представления
        /// </summary>
        public static IIOViewModel DataContext { get; private set; }

        /// <summary>
        /// Запуск окна из меню
        /// </summary>
        public static void Start()
        {
            if (DataContext?.IOManager == null)
            {
                DataContext = new IOViewModel(IOManager.GetInstance());
            }

            Instance ??= new IOViewControl(DataContext);

            Instance.InitDataStructPLC();
            Instance.ShowDlg();
        }

        public void Clear()
        {
            DataContext = null;
            StructPLC.BeginUpdate();
            StructPLC.ClearObjects();
            StructPLC.EndUpdate();
        }

        private IOViewControl(IIOViewModel viewModel)
        {
            InitializeComponent();
            InitStructPLC();
            InitContextMenu();
        }

        private void InitStructPLC()
        {
            StructPLC.CanExpandGetter = obj => obj is IExpandable exp && (exp.Items?.Any() ?? false);
            StructPLC.ChildrenGetter = GetChildren;
            StructPLC.CellToolTipGetter = (col, obj) => (col.Index) switch
            {
                0 => (obj as IToolTip)?.Name ?? (obj as IViewItem).Name,
                1 => (obj as IToolTip)?.Description ?? (obj as IViewItem).Description,
                _ => ""
            };


            var firstColumn = new OLVColumn("Название", nameof(IViewItem.Name))
            {
                ImageGetter = GetIconIndex,
                AspectGetter = obj => (obj as IViewItem).Name,
                IsEditable = true,
                Sortable = false,
            };

            var secondColumn = new OLVColumn("Описание", nameof(IViewItem.Description))
            {
                ImageGetter = obj => (obj is IHasDescriptionIcon item) ? (int)item.Icon : -1,
                AspectGetter = obj => (obj as IViewItem).Description,
                IsEditable = true,
                Sortable = false,
            };


            StructPLC.Columns.Add(firstColumn);
            StructPLC.Columns.Add(secondColumn);
            StructPLC.CellEditKeyEngine = cellEditKeyEngine;

            EnsureAddModuleIcon();
            EnsureErrorIcon();
        }

        [ExcludeFromCodeCoverage]
        private IEnumerable GetChildren(object obj)
        {
            if (obj is Node node && isDraggingDeletedModule)
            {
                return node.Items.Concat(new IViewItem[]
                {
                    new AppendModuleTarget(node.IONode)
                });
            }

            return (obj as IExpandable)?.Items;
        }

        [ExcludeFromCodeCoverage]
        private object GetIconIndex(object obj)
        {
            if (isDraggingDeletedModule &&
                obj is IModule module &&
                IsUndefinedModule(module.IOModule))
            {
                return (int)IO.ViewModel.Icon.AddModule;
            }

            return obj is IHasIcon item ? (int)item.Icon : -1;
        }

        [ExcludeFromCodeCoverage]
        private void EnsureErrorIcon()
        {
            const string imageKey = "error.png";
            if (ViewItemImageList.Images.ContainsKey(imageKey))
            {
                return;
            }

            ViewItemImageList.Images.Add(imageKey, CreateErrorIcon());
        }

        [ExcludeFromCodeCoverage]
        private static Bitmap CreateErrorIcon()
        {
            const int size = 16;
            var bitmap = new Bitmap(
                size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var yellow = Color.FromArgb(255, 255, 204, 0);
            const float center = 7.5f;
            const float fillRadius = 4.5f;
            const float borderRadius = 5.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    Color color;
                    if (IsErrorIconExclamationPixel(x, y))
                    {
                        color = Color.Black;
                    }
                    else if (distance <= fillRadius)
                    {
                        color = yellow;
                    }
                    else if (distance <= borderRadius)
                    {
                        color = Color.Black;
                    }
                    else
                    {
                        color = Color.Transparent;
                    }

                    bitmap.SetPixel(x, y, color);
                }
            }

            return bitmap;
        }

        private static bool IsErrorIconExclamationPixel(int x, int y) =>
            (x == 7 || x == 8) && (y >= 5 && y <= 8 || y == 10);

        [ExcludeFromCodeCoverage]
        private void EnsureAddModuleIcon()
        {
            const string imageKey = "add_module.png";
            if (ViewItemImageList.Images.ContainsKey(imageKey))
            {
                return;
            }

            ViewItemImageList.Images.Add(imageKey, CreateAddModuleIcon());
        }

        [ExcludeFromCodeCoverage]
        private static Bitmap CreateAddModuleIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            using (var fillBrush = new SolidBrush(Color.White))
            using (var borderPen = new Pen(Color.Black, 1))
            using (var plusPen = new Pen(Color.Black, 2))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.None;
                graphics.FillRectangle(fillBrush, 2, 2, 11, 11);
                graphics.DrawRectangle(borderPen, 2, 2, 11, 11);
                graphics.DrawLine(plusPen, 8, 5, 8, 11);
                graphics.DrawLine(plusPen, 5, 8, 11, 8);
            }

            return bitmap;
        }

        [ExcludeFromCodeCoverage]
        private static Bitmap CreateReserveErrorClampsIcon()
        {
            const int size = 16;
            var bitmap = new Bitmap(
                size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            using (var errorIcon = CreateErrorIcon())
            using (var basketIcon = new Bitmap(
                global::EasyEPlanner.Properties.Resources.delete,
                new Size(10, 10)))
            {
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(errorIcon, 0, 0, 11, 11);
                graphics.DrawImage(basketIcon, 6, 6, 10, 10);
            }

            return bitmap;
        }

        private void InitContextMenu()
        {
            var contextMenuStrip = new ContextMenuStrip(components);

            shiftModulesToolStripMenuItem =
                new ToolStripMenuItem("Сдвинуть модули")
                {
                    Image = global::EasyEPlanner.Properties.Resources.shift_modules
                };
            shiftModulesToolStripMenuItem.Click += ShiftModules_Click;

            goToFasToolStripMenuItem =
                new ToolStripMenuItem(FasNavigationTexts.MenuItem)
                {
                    Image = global::EasyEPlanner.Properties.Resources.go_to_fas
                };
            goToFasToolStripMenuItem.Click += GoToFas_Click;

            reserveErrorClampsToolStripMenuItem =
                new ToolStripMenuItem("Очистить клеммы с ошибкой")
                {
                    Image = CreateReserveErrorClampsIcon()
                };
            reserveErrorClampsToolStripMenuItem.Click +=
                ReserveErrorClamps_Click;

            restoreDeletedModulesToolStripMenuItem =
                new ToolStripMenuItem("Восстановить")
                {
                    Image = CreateAddModuleIcon()
                };
            restoreDeletedModulesToolStripMenuItem.Click +=
                RestoreDeletedModules_Click;

            deleteUndefinedModuleToolStripMenuItem =
                new ToolStripMenuItem("Удалить")
                {
                    Image = global::EasyEPlanner.Properties.Resources.delete,
                    ShortcutKeys = Keys.Delete
                };
            deleteUndefinedModuleToolStripMenuItem.Click +=
                DeleteUndefinedModule_Click;

            contextMenuStrip.Items.Add(shiftModulesToolStripMenuItem);
            contextMenuStrip.Items.Add(goToFasToolStripMenuItem);
            contextMenuStrip.Items.Add(reserveErrorClampsToolStripMenuItem);
            contextMenuStrip.Items.Add(restoreDeletedModulesToolStripMenuItem);
            contextMenuStrip.Items.Add(deleteUndefinedModuleToolStripMenuItem);
            contextMenuStrip.Opening += StructPLCContextMenu_Opening;

            StructPLC.ContextMenuStrip = contextMenuStrip;
            StructPLC.MouseDown += StructPLC_MouseDown;
            StructPLC.AllowDrop = true;
            StructPLC.ItemDrag += StructPLC_ItemDrag;
            StructPLC.DragOver += StructPLC_DragOver;
            StructPLC.DragDrop += StructPLC_DragDrop;
            StructPLC.DragLeave += StructPLC_DragLeave;
        }

        [ExcludeFromCodeCoverage]
        private void StructPLC_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if ((e.Item as OLVListItem)?.RowObject is DeletedModule
                deletedModule &&
                IsOnlySelectedObject(deletedModule) &&
                deletedModule.IOModule.Function?.IsValid == true)
            {
                isDraggingDeletedModule = true;
                RefreshDragDropTargets();

                try
                {
                    DoDragDrop(new DataObject(typeof(DraggedModule).FullName,
                        new DraggedModule { DeletedModule = deletedModule }),
                        DragDropEffects.Move);
                }
                finally
                {
                    isDraggingDeletedModule = false;
                    SetDragHoverRowObject(null);
                    RefreshDragDropTargets();
                }
            }
        }

        [ExcludeFromCodeCoverage]
        private void RefreshDragDropTargets()
        {
            StructPLC.BeginUpdate();
            StructPLC.RebuildAll(true);
            StructPLC.EndUpdate();
            AutoResizeColumns(StructPLC);
        }

        [ExcludeFromCodeCoverage]
        private void StructPLC_DragOver(object sender, DragEventArgs e)
        {
            bool canDrop = CanDropModule(e);

            SetDragHoverRowObject(canDrop ? GetDragRowObject(e) : null);

            e.Effect = canDrop ? DragDropEffects.Move : DragDropEffects.None;
        }

        [ExcludeFromCodeCoverage]
        private void StructPLC_DragLeave(object sender, EventArgs e)
        {
            SetDragHoverRowObject(null);
        }

        [ExcludeFromCodeCoverage]
        private object GetDragRowObject(DragEventArgs e)
        {
            var point = StructPLC.PointToClient(new Point(e.X, e.Y));
            return (StructPLC.GetItemAt(point.X, point.Y) as OLVListItem)
                ?.RowObject;
        }

        [ExcludeFromCodeCoverage]
        private void SetDragHoverRowObject(object rowObject)
        {
            if (ReferenceEquals(dragHoverRowObject, rowObject))
            {
                return;
            }

            dragHoverRowObject = rowObject;
            StructPLC.Invalidate();
        }

        private void StructPLC_DragDrop(object sender, DragEventArgs e)
        {
            if (!TryGetDragDropModules(e, out var draggedModule,
                out var dropTarget))
            {
                return;
            }

            try
            {
                InsertDeletedModule(draggedModule.DeletedModule.IOModule,
                    dropTarget);

                EProjectManager.GetInstance().SyncAndSave(false);

                Editor.Editor.GetInstance().EditorForm.RefreshTree();
                DFrm.GetInstance().RefreshTree();

                RebuildTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Перемещение модуля",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool CanDropModule(DragEventArgs e)
        {
            return TryGetDragDropModules(e, out var draggedModule,
                out var dropTarget) &&
                CanDropModule(draggedModule, dropTarget);
        }

        private bool TryGetDragDropModules(DragEventArgs e,
            out DraggedModule draggedModule, out ModuleDropTarget dropTarget)
        {
            draggedModule = null;
            dropTarget = null;

            if (!e.Data.GetDataPresent(typeof(DraggedModule).FullName))
            {
                return false;
            }

            draggedModule = e.Data.GetData(typeof(DraggedModule).FullName)
                as DraggedModule;
            var point = StructPLC.PointToClient(new Point(e.X, e.Y));
            var rowObject = (StructPLC.GetItemAt(point.X, point.Y) as
                OLVListItem)?.RowObject;

            if (rowObject is IModule targetModule)
            {
                dropTarget = new ModuleDropTarget
                {
                    Node = targetModule.IONode,
                    Module = targetModule
                };
            }
            else if (rowObject is AppendModuleTarget appendModuleTarget)
            {
                dropTarget = new ModuleDropTarget
                {
                    Node = appendModuleTarget.IONode
                };
            }

            return draggedModule != null && dropTarget != null;
        }

        private static bool CanDropModule(DraggedModule draggedModule,
            ModuleDropTarget dropTarget)
        {
            return draggedModule.DeletedModule?.IOModule.Function?.IsValid ==
                true &&
                CanDropToTarget(dropTarget);
        }

        private static bool CanDropToTarget(ModuleDropTarget dropTarget)
        {
            if (dropTarget?.Node == null)
            {
                return false;
            }

            if (dropTarget.IsNodeEnd)
            {
                return CanAppendToNode(dropTarget.Node);
            }

            return dropTarget.Module.IOModule.Function?.IsValid != true;
        }

        private static bool CanAppendToNode(IIONode targetNode)
        {
            if (targetNode.Type is IONode.TYPES.T_EMPTY)
            {
                return false;
            }

            try
            {
                GetNextPhysicalNumber(targetNode);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private bool IsOnlySelectedObject(object rowObject)
        {
            var selectedObjects = StructPLC.SelectedObjects?.Cast<object>()
                .ToList() ?? new List<object>();

            return selectedObjects.Count == 1 &&
                ReferenceEquals(selectedObjects[0], rowObject);
        }

        private void StructPLC_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle ||
                e.Button == MouseButtons.Left &&
                ModifierKeys.HasFlag(Keys.Control))
            {
                GoToFasAt(e.Location);
                return;
            }

            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var item = StructPLC.GetItemAt(e.X, e.Y) as OLVListItem;
            if (item != null && !item.Selected)
            {
                StructPLC.SelectedObject = item.RowObject;
            }
        }

        private void GoToFasAt(Point location)
        {
            var rowObject = (StructPLC.GetItemAt(location.X, location.Y) as
                OLVListItem)?.RowObject;
            if (!TryGetEplanFunction(rowObject, out var function))
            {
                return;
            }

            EplanNavigateHelper.OpenFunctionPageWithError(function);
        }

        private void StructPLCContextMenu_Opening(object sender, CancelEventArgs e)
        {
            var selectedModules = GetSelectedModules().ToList();

            shiftModulesToolStripMenuItem.Enabled =
                selectedModules.Count == 1 &&
                selectedModules[0].IOModule.Function?.IsValid == true;
            goToFasToolStripMenuItem.Enabled =
                TryGetSelectedEplanFunction(out _);
            reserveErrorClampsToolStripMenuItem.Enabled =
                BindingErrorClampCollector.Collect(GetSelectedViewObjects())
                    .Any();
            restoreDeletedModulesToolStripMenuItem.Enabled =
                GetRestorableDeletedModules(GetSelectedDeletedIOModules())
                    .Any();
            deleteUndefinedModuleToolStripMenuItem.Enabled =
                selectedModules.Any();
        }

        private void GoToFas_Click(object sender, EventArgs e)
        {
            if (!TryGetSelectedEplanFunction(out var function))
            {
                return;
            }

            EplanNavigateHelper.OpenFunctionPageWithError(function);
        }

        private void ReserveErrorClamps_Click(object sender, EventArgs e)
        {
            var errorClamps = BindingErrorClampCollector
                .Collect(GetSelectedViewObjects())
                .ToList();
            if (!errorClamps.Any())
            {
                return;
            }

            foreach (var clamp in errorClamps)
            {
                clamp.Delete();
            }

            RefreshTree();
            DFrm.GetInstance().RefreshTreeAfterBinding();
            EasyEPlanner.Binding.View.BindingViewControl.Instance?.RefreshTreeAfterBinding();
        }

        private void ShiftModules_Click(object sender, EventArgs e)
        {
            if (StructPLC.SelectedObject is not IModule module ||
                !TryGetShiftValue(out int shiftValue))
            {
                return;
            }

            try
            {
                ShiftModulesFrom(module, shiftValue);

                EProjectManager.GetInstance().SyncAndSave(false);

                Editor.Editor.GetInstance().EditorForm.RefreshTree();
                DFrm.GetInstance().RefreshTree();

                RebuildTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Сдвиг модулей",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteUndefinedModule_Click(object sender, EventArgs e)
        {
            DeleteSelectedUndefinedModule();
        }

        private void RestoreDeletedModules_Click(object sender, EventArgs e)
        {
            var restoreTargets = GetRestorableDeletedModules(
                GetSelectedDeletedIOModules()).ToList();
            if (!restoreTargets.Any())
            {
                return;
            }

            try
            {
                RestoreDeletedModules(restoreTargets);

                EProjectManager.GetInstance().SyncAndSave(false);

                Editor.Editor.GetInstance().EditorForm.RefreshTree();
                DFrm.GetInstance().RefreshTree();

                RebuildTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Восстановление модулей",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelectedUndefinedModule()
        {
            var modules = GetSelectedModules().ToList();
            if (!modules.Any())
            {
                return;
            }

            try
            {
                DeleteModules(modules);

                EProjectManager.GetInstance().SyncAndSave(false);

                Editor.Editor.GetInstance().EditorForm.RefreshTree();
                DFrm.GetInstance().RefreshTree();

                RebuildTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Удаление модуля",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IEnumerable<IModule> GetSelectedModules()
        {
            return StructPLC.SelectedObjects?.OfType<IModule>() ??
                Enumerable.Empty<IModule>();
        }

        private IEnumerable<object> GetSelectedViewObjects()
        {
            var selectedObjects = StructPLC.SelectedObjects?.Cast<object>()
                .ToList();
            if (selectedObjects != null && selectedObjects.Count > 0)
            {
                return selectedObjects;
            }

            return StructPLC.SelectedObject != null
                ? new[] { StructPLC.SelectedObject }
                : Enumerable.Empty<object>();
        }

        private IEnumerable<DeletedModule> GetSelectedDeletedModules()
        {
            var selectedObjects = StructPLC.SelectedObjects?.Cast<object>() ??
                Enumerable.Empty<object>();

            foreach (var deletedModule in selectedObjects
                .OfType<DeletedModule>())
            {
                yield return deletedModule;
            }

            foreach (var deletedModulesGroup in selectedObjects
                .OfType<DeletedModulesGroup>())
            {
                foreach (var deletedModule in deletedModulesGroup.Items
                    .OfType<DeletedModule>())
                {
                    yield return deletedModule;
                }
            }
        }

        private IEnumerable<IIOModule> GetSelectedDeletedIOModules()
        {
            return GetSelectedDeletedModules()
                .Select(module => module.IOModule);
        }

        private bool TryGetSelectedEplanFunction(out Function function)
        {
            function = null;

            var selectedObjects = StructPLC.SelectedObjects?.Cast<object>()
                .ToList() ?? new List<object>();
            if (selectedObjects.Count != 1)
            {
                return false;
            }

            return TryGetEplanFunction(selectedObjects[0], out function);
        }

        private static bool TryGetEplanFunction(object viewObject,
            out Function function)
        {
            function = null;

            IEplanFunction eplanFunction = viewObject switch
            {
                INode node => node.IONode.Function,
                IModule module => module.IOModule.Function,
                IClamp clamp => clamp.ClampFunction,
                DeletedModule deletedModule => deletedModule.IOModule.Function,
                _ => null
            };

            return EplanNavigateHelper.TryGetFunction(eplanFunction, out function);
        }

        private static bool TryGetShiftValue(out int shiftValue)
        {
            const int DefaultShiftValue = 1;
            const int MaxShiftValue = 99;

            using var form = new Form
            {
                Text = "Сдвинуть модули",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(260, 100)
            };

            var label = new System.Windows.Forms.Label
            {
                Text = "Сдвинуть на:",
                AutoSize = true,
                Location = new Point(12, 15)
            };

            var countInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = MaxShiftValue,
                Value = DefaultShiftValue,
                Location = new Point(145, 12),
                Width = 95
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(84, 60),
                Width = 75
            };

            var cancelButton = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new Point(165, 60),
                Width = 75
            };

            form.Controls.AddRange(new Control[]
            {
                label,
                countInput,
                okButton,
                cancelButton
            });

            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            bool accepted = form.ShowDialog() == DialogResult.OK;
            shiftValue = accepted ? (int)countInput.Value : 0;

            return accepted;
        }

        private static void ShiftModulesFrom(IModule selectedModule,
            int shiftValue)
        {
            var modules = selectedModule.IONode.IOModules;
            int selectedIndex = modules.IndexOf(selectedModule.IOModule);
            if (selectedIndex < 0)
            {
                throw new InvalidOperationException(
                    "Выбранный модуль не найден в узле.");
            }

            var modulesToShift = modules
                .Skip(selectedIndex)
                .Where(module => module?.Function?.IsValid == true)
                .OrderByDescending(module => module.PhysicalNumber)
                .ToList();

            if (!modulesToShift.Any())
            {
                throw new InvalidOperationException(
                    "Нет модулей для сдвига.");
            }

            foreach (var physicalNumber in modulesToShift
                .Select(module => module.PhysicalNumber))
            {
                int newPhysicalNumber = physicalNumber + shiftValue;
                ValidateModuleNumber(physicalNumber, newPhysicalNumber);
            }

            foreach (var module in modulesToShift)
            {
                int newPhysicalNumber = module.PhysicalNumber + shiftValue;
                RenameModuleWithClamps(module,
                    $"-A{module.PhysicalNumber}",
                    $"-A{newPhysicalNumber}");
            }
        }

        private static void InsertDeletedModule(IIOModule deletedModule,
            ModuleDropTarget dropTarget)
        {
            if (dropTarget?.Node == null ||
                deletedModule?.Function?.IsValid != true)
            {
                throw new InvalidOperationException(
                    "Исключенный модуль можно переместить только в узел PLC.");
            }

            int targetPhysicalNumber = dropTarget.IsNodeEnd
                ? GetNextPhysicalNumber(dropTarget.Node)
                : GetTargetPhysicalNumber(dropTarget.Module);

            RenameDeletedModule(deletedModule, $"-A{targetPhysicalNumber}");
        }

        private IEnumerable<DeletedModuleRestoreTarget>
            GetRestorableDeletedModules(IEnumerable<IIOModule> modules)
        {
            return DeletedModuleRestorePlanner.GetRestorableModules(modules,
                DataContext?.IOManager?.IONodes);
        }

        private static void RestoreDeletedModules(
            IEnumerable<DeletedModuleRestoreTarget> restoreTargets)
        {
            foreach (var target in restoreTargets)
            {
                RenameDeletedModule(target.Module,
                    $"-A{target.TargetPhysicalNumber}");
            }
        }

        private static int GetTargetPhysicalNumber(IModule targetModule)
        {
            var modules = targetModule.IONode.IOModules;
            int targetIndex = modules.IndexOf(targetModule.IOModule);
            if (targetIndex < 0)
            {
                throw new InvalidOperationException(
                    "Целевой модуль не найден в узле.");
            }

            if (targetModule.IOModule.Function?.IsValid == true)
            {
                throw new InvalidOperationException(
                    "Исключенный модуль можно переместить только в " +
                    "неопределенный модуль.");
            }

            return GetPhysicalNumberByIndex(targetModule.IONode, targetIndex);
        }

        private static int GetNextPhysicalNumber(IIONode targetNode)
        {
            int physicalNumber = targetNode.NodeNumber +
                targetNode.IOModules.Count + 1;

            ValidateModuleNumber(targetNode.NodeNumber + 1, physicalNumber);

            return physicalNumber;
        }

        private static void RenameDeletedModule(IIOModule deletedModule,
            string newName)
        {
            if (deletedModule.Function is not StaticHelper.EplanFunction
                eplanFunction)
            {
                throw new InvalidOperationException(
                    "Не найдена функция для переименования исключенного " +
                    "модуля.");
            }

            var actualOldName = GetDeletedFunctionNamePart(
                eplanFunction.Function);
            if (actualOldName == string.Empty)
            {
                throw new InvalidOperationException(
                    $"Не удалось найти имя исключенного модуля " +
                    $"\"{eplanFunction.Function.VisibleName}\".");
            }

            var functionsToRename = GetDeletedModuleRenameFunctions(
                eplanFunction.Function, actualOldName);
            int renamedFunctionsCount = functionsToRename.Count(function =>
                RenameFunctionNamePart(function, actualOldName, newName));

            if (renamedFunctionsCount == 0)
            {
                throw new InvalidOperationException(
                    $"Не удалось заменить имя исключенного модуля " +
                    $"\"{eplanFunction.Function.VisibleName}\".");
            }
        }

        private static string GetDeletedFunctionNamePart(Function function)
        {
            return Regex.Match(function.Name, @"-DEL\d+(?=$|\D)",
                RegexOptions.None, RegexDefaults.Timeout).Value;
        }

        private static List<Function> GetDeletedModuleRenameFunctions(
            Function moduleFunction, string oldName)
        {
            var functions = new List<Function>();

            AddRenameCandidate(functions, moduleFunction, oldName);

            foreach (var subFunction in moduleFunction.SubFunctions ?? [])
            {
                AddRenameCandidate(functions, subFunction, oldName);
            }

            foreach (var pageFunction in (moduleFunction.Page?.Functions ?? [])
                .Where(function => IsRelatedModuleFunction(moduleFunction,
                    function)))
            {
                AddRenameCandidate(functions, pageFunction, oldName);
            }

            return functions;
        }

        private static bool IsRelatedModuleFunction(Function moduleFunction,
            Function function)
        {
            return function == moduleFunction ||
                function.ParentFunction == moduleFunction;
        }

        private static int GetPhysicalNumberByIndex(IIONode node,
            int moduleIndex)
        {
            return node.NodeNumber + moduleIndex + 1;
        }

        private static void DeleteModules(IEnumerable<IModule> selectedModules)
        {
            var modulesByNode = selectedModules
                .GroupBy(module => module.IONode);

            foreach (var modulesGroup in modulesByNode)
            {
                DeleteModulesFromNode(modulesGroup.Key,
                    modulesGroup.ToList());
            }
        }

        private static void DeleteModulesFromNode(IIONode node,
            List<IModule> selectedModules)
        {
            var modules = node.IOModules;
            var selectedIndexes = selectedModules
                .Select(module => modules.IndexOf(module.IOModule))
                .OrderBy(index => index)
                .ToList();

            if (selectedIndexes.Any(index => index < 0))
            {
                throw new InvalidOperationException(
                    "Один из выбранных модулей не найден в узле.");
            }

            var selectedDefinedModules = selectedModules
                .Select(module => module.IOModule)
                .Where(module => module?.Function?.IsValid == true)
                .OrderByDescending(module => module.PhysicalNumber)
                .ToList();

            var selectedUndefinedIndexes = selectedModules
                .Where(module => IsUndefinedModule(module.IOModule))
                .Select(module => modules.IndexOf(module.IOModule))
                .OrderBy(index => index)
                .ToList();

            if (!selectedDefinedModules.Any() &&
                !selectedUndefinedIndexes.Any())
            {
                throw new InvalidOperationException(
                    "Нет модулей для удаления.");
            }

            foreach (var module in selectedDefinedModules)
            {
                RenameModuleWithClamps(module,
                    $"-A{module.PhysicalNumber}",
                    GetDeletedModuleName(module));
            }

            if (!selectedUndefinedIndexes.Any())
            {
                return;
            }

            int firstDeletedIndex = selectedUndefinedIndexes[0];
            var modulesToShift = modules
                .Skip(firstDeletedIndex + 1)
                .Where(module => module?.Function?.IsValid == true)
                .Where(module => !selectedDefinedModules.Contains(module))
                .OrderBy(module => module.PhysicalNumber)
                .ToList();

            if (!modulesToShift.Any())
            {
                throw new InvalidOperationException(
                    "После выбранного модуля нет модулей для сдвига.");
            }

            foreach (var module in modulesToShift)
            {
                int moduleIndex = modules.IndexOf(module);
                int shiftValue = selectedUndefinedIndexes.Count(index =>
                    index < moduleIndex);
                int newPhysicalNumber = module.PhysicalNumber - shiftValue;
                ValidateModuleNumber(module.PhysicalNumber, newPhysicalNumber);
            }

            foreach (var module in modulesToShift)
            {
                int moduleIndex = modules.IndexOf(module);
                int shiftValue = selectedUndefinedIndexes.Count(index =>
                    index < moduleIndex);
                int newPhysicalNumber = module.PhysicalNumber - shiftValue;
                RenameModuleWithClamps(module,
                    $"-A{module.PhysicalNumber}",
                    $"-A{newPhysicalNumber}");
            }
        }

        private static bool IsUndefinedModule(IIOModule module)
        {
            return module is not null &&
                module.Info?.Name == IOModuleInfo.Stub.Name &&
                module.Function is null;
        }

        private static string GetDeletedModuleName(IIOModule module)
        {
            return $"-DEL{module.PhysicalNumber}";
        }

        private static void RenameModuleWithClamps(IIOModule module,
            string oldName, string newName)
        {
            var functionsToRename = GetModuleRenameFunctions(module, oldName);
            foreach (var function in functionsToRename)
            {
                RenameFunctionNamePart(function, oldName, newName);
            }
        }

        private static bool RenameFunctionNamePart(Function function,
            string oldName, string newName)
        {
            var renamedFunctionName = Regex.Replace(function.Name,
                $@"{Regex.Escape(oldName)}(?=$|\D)", newName,
                RegexOptions.None, RegexDefaults.Timeout);
            if (renamedFunctionName != function.Name)
            {
                function.Name = renamedFunctionName;
                return true;
            }

            return false;
        }

        private static List<Function> GetModuleRenameFunctions(
            IIOModule module, string oldModuleName)
        {
            var functions = new List<Function>();

            AddRenameCandidate(functions, module.Function, oldModuleName);

            foreach (var clampFunction in module.ClampFunctions.Values
                .Where(function => function?.IsValid == true)
                .Distinct())
            {
                AddRenameCandidate(functions, clampFunction, oldModuleName);
            }

            AddProjectRenameCandidates(functions, module, oldModuleName);

            foreach (var function in functions.ToArray())
            {
                AddRenameCandidate(functions, function.ParentFunction,
                    oldModuleName);

                var pageFunctions = function.Page?.Functions ?? [];
                foreach (var pageFunction in pageFunctions)
                {
                    AddRenameCandidate(functions, pageFunction, oldModuleName);
                }
            }

            if (!functions.Any())
            {
                throw new InvalidOperationException(
                    $"Не найдены функции для переименования {oldModuleName}.");
            }

            return functions;
        }

        private static void AddProjectRenameCandidates(List<Function> functions,
            IIOModule module, string oldModuleName)
        {
            if (module.Function is not StaticHelper.EplanFunction moduleFunction)
            {
                return;
            }

            var project = moduleFunction.Function.Page?.Project;
            if (project is null)
            {
                return;
            }

            var objectFinder = new DMObjectsFinder(project);
            AddProjectRenameCandidates(functions, objectFinder, oldModuleName,
                Function.Enums.Category.PLCBox);
            AddProjectRenameCandidates(functions, objectFinder, oldModuleName,
                Function.Enums.Category.PLCTerminal);
        }

        private static void AddProjectRenameCandidates(List<Function> functions,
            DMObjectsFinder objectFinder,
            string oldModuleName, Function.Enums.Category category)
        {
            var functionsFilter = new FunctionsFilter
            {
                Category = category
            };

            foreach (var function in objectFinder.GetFunctions(functionsFilter))
            {
                AddRenameCandidate(functions, function, oldModuleName);
            }
        }

        private static void AddRenameCandidate(List<Function> functions,
            StaticHelper.IEplanFunction function, string oldModuleName)
        {
            if (function is StaticHelper.EplanFunction eplanFunction)
            {
                AddRenameCandidate(functions, eplanFunction.Function,
                    oldModuleName);
            }
        }

        private static void AddRenameCandidate(List<Function> functions,
            Function function, string oldModuleName)
        {
            if (function is null ||
                !function.IsValid ||
                functions.Contains(function) ||
                !CanRenameModuleFunction(function) ||
                !ContainsModuleName(function.Name, oldModuleName))
            {
                return;
            }

            functions.Add(function);
        }

        private static bool CanRenameModuleFunction(Function function)
        {
            return function.Category is Function.Enums.Category.PLCBox or
                Function.Enums.Category.PLCTerminal;
        }

        private static bool ContainsModuleName(string functionName,
            string moduleName)
        {
            return Regex.IsMatch(functionName,
                $@"{Regex.Escape(moduleName)}(?=$|\D)", RegexOptions.None,
                RegexDefaults.Timeout);
        }

        private static void ValidateModuleNumber(int currentPhysicalNumber,
            int newPhysicalNumber)
        {
            bool movedToAnotherNode =
                currentPhysicalNumber / 100 != newPhysicalNumber / 100;
            bool becameNodeNumber = newPhysicalNumber % 100 == 0;
            if (movedToAnotherNode || becameNodeNumber)
            {
                throw new InvalidOperationException(
                    $"Невозможно переименовать модуль -A{currentPhysicalNumber} " +
                    $"в -A{newPhysicalNumber}: номер выходит за пределы узла.");
            }
        }

        private void InitDataStructPLC()
        {
            StructPLC.BeginUpdate();

            StructPLC.Roots = DataContext.Roots;

            StructPLC.Columns[0].Width = 100;
            StructPLC.Columns[1].Width = 200;

            StructPLC.Expand(DataContext.Root);

            /// Востановление развертки дерева
            RestoreExpanded(DataContext.Roots);

            StructPLC.SelectedIndex = 0;
            StructPLC.SelectedItem.EnsureVisible();

            StructPLC.EndUpdate();

            AutoResizeColumns(StructPLC);
        }

        private void RestoreExpanded(IEnumerable items)
        {
            foreach (var item in items.OfType<IExpandable>())
            {
                if (item.Expanded)
                {
                    StructPLC.Expand(item);
                }

                RestoreExpanded(item.Items);
            }
        }

        private void Expand_Click(object sender, EventArgs e)
        {
            int level = Convert.ToInt32((sender as ToolStripMenuItem).Tag);

            StructPLC.BeginUpdate();

            // Убираем события для оптимизации множественного вызова
            StructPLC.Expanded -= ItemExpanded;
            StructPLC.Collapsed -= ItemCollapsed;

            StructPLC.SelectedIndex = 0;

            ExpandToLevel(level, StructPLC.Objects);

            StructPLC.Expanded += ItemExpanded;
            StructPLC.Collapsed += ItemCollapsed;
            
            StructPLC.EnsureModelVisible(StructPLC.SelectedObject);
            StructPLC.EndUpdate();

            AutoResizeColumns(StructPLC);
        }

        private void ExpandToLevel(int level, IEnumerable items)
        {
            foreach (var item in items.OfType<IExpandable>())
            {
                if (level > 0 && !StructPLC.IsExpanded(item))
                {
                    StructPLC.Expand(item);
                    item?.Expanded = true;
                }
                
                if (level == 0 && StructPLC.IsExpanded(item))
                {
                    StructPLC.Collapse(item);
                    item?.Expanded = false;
                }

                if (item is not null)
                {
                    ExpandToLevel(level - 1, item.Items);
                }
            }
        }

        private TextBox textBoxCellEditor;

        private ComboBox comboBoxCellEditor;

        private void CellEditStarting(object sender, CellEditEventArgs e)
        {
            if (e.Column.Index != 1)
            {
                e.Cancel = true;
                return;
            }

            if (StructPLC.SelectedObject is IComboBoxEditable comboItem)
            {
                isCellEditing = true;
                cellEditUsesComboBox = true;
                cellEditUsesMultiline = false;
                cellEditItem = comboItem;
                InitComboBoxCellEditor(comboItem.ComboBoxItems);
                string value = e.Value?.ToString() ?? comboItem.Value;
                int index = comboBoxCellEditor.FindStringExact(value);
                if (index >= 0)
                    comboBoxCellEditor.SelectedIndex = index;
                comboBoxCellEditor.Bounds = GetCellEditorBounds(StructPLC, e.CellBounds);
                e.Control = comboBoxCellEditor;
                comboBoxCellEditor.Focus();
                cellEditKeyEngine.MultilineEditActive = false;
                InstallKeyboardHook();
                StructPLC.Freeze();
                return;
            }

            if (StructPLC.SelectedObject is not IEditable item)
            {
                e.Cancel = true;
                return;
            }

            isCellEditing = true;
            cellEditUsesComboBox = false;
            cellEditItem = item;
            cellEditUsesMultiline = item is IClamp;

            InitTextBoxCellEditor(cellEditUsesMultiline);
            textBoxCellEditor.Text = cellEditUsesMultiline
                ? EplanMultilineText.FormatForEditor(item.Value)
                : item.Value;
            textBoxCellEditor.Bounds = cellEditUsesMultiline
                ? GetMultilineEditorBounds(StructPLC, e.CellBounds, textBoxCellEditor.Text)
                : GetCellEditorBounds(StructPLC, e.CellBounds);

            e.Control = textBoxCellEditor;
            textBoxCellEditor.Focus();
            cellEditKeyEngine.MultilineEditActive = cellEditUsesMultiline;
            InstallKeyboardHook();

            StructPLC.Freeze();
        }

        private void CellEditFinishing(object sender, CellEditEventArgs e)
        {
            try
            {
                isCellEditing = false;
                cellEditKeyEngine.MultilineEditActive = false;
                var editable = cellEditItem;
                cellEditItem = null;

                if (cancelChanges || editable is null)
                {
                    RemoveCellEditorControls();
                    e.Cancel = true;
                    cancelChanges = false;
                    cellEditUsesComboBox = false;
                    cellEditUsesMultiline = false;

                    StructPLC.Unfreeze();

                    return;
                }

                bool modified;
                if (cellEditUsesComboBox)
                {
                    var combo = comboBoxCellEditor ?? e.Control as ComboBox;
                    e.NewValue = combo?.Text ?? string.Empty;
                    modified = editable.SetValue(e.NewValue?.ToString() ?? string.Empty);
                }
                else
                {
                    string text = textBoxCellEditor?.Text
                        ?? e.NewValue?.ToString()
                        ?? string.Empty;
                    if (cellEditUsesMultiline)
                        text = EplanMultilineText.ParseFromEditor(text,
                            CommonConst.NewLineWithCarriageReturn);

                    modified = editable is IClamp clamp
                        ? ClampFunctionalTextBinder.TryApply(clamp, text)
                        : editable.SetValue(text);
                }

                RemoveCellEditorControls();
                cellEditUsesComboBox = false;
                cellEditUsesMultiline = false;

                if (modified)
                {
                    RefreshTree();
                    DFrm.GetInstance().RefreshTreeAfterBinding();
                    EasyEPlanner.Binding.View.BindingViewControl.Instance?.RefreshTreeAfterBinding();
                }

                e.Cancel = true;
                StructPLC.Unfreeze();
            }
            finally
            {
                MaybeReleaseKeyboardHook();
            }
        }

        private void RemoveCellEditorControls()
        {
            if (comboBoxCellEditor is not null)
            {
                comboBoxCellEditor.LostFocus -= ComboBoxCellEditor_LostFocus;
                comboBoxCellEditor.KeyDown -= CellEditor_KeyDown;
                if (StructPLC.Controls.Contains(comboBoxCellEditor))
                    StructPLC.Controls.Remove(comboBoxCellEditor);
                comboBoxCellEditor = null;
            }

            if (textBoxCellEditor is not null)
            {
                textBoxCellEditor.LostFocus -= CellEditor_LostFocus;
                textBoxCellEditor.KeyDown -= CellEditor_KeyDown;
                if (StructPLC.Controls.Contains(textBoxCellEditor))
                    StructPLC.Controls.Remove(textBoxCellEditor);
                textBoxCellEditor = null;
            }
        }

        private static Rectangle GetCellEditorBounds(TreeListView tree, Rectangle bounds)
        {
            int height = tree.RowHeight > 0 ? tree.RowHeight : 20;
            return new Rectangle(bounds.X, bounds.Y, bounds.Width, height);
        }

        private static Rectangle GetMultilineEditorBounds(
            TreeListView tree,
            Rectangle bounds,
            string text)
        {
            int lineHeight = TextRenderer.MeasureText("Xg", tree.Font).Height;
            int lineCount = string.IsNullOrEmpty(text)
                ? 2
                : text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).Length;
            int height = Math.Max(tree.RowHeight > 0 ? tree.RowHeight : 20,
                lineCount * lineHeight + 6);
            height = Math.Min(height, 200);
            return new Rectangle(bounds.X, bounds.Y, bounds.Width, height);
        }

        private void InitTextBoxCellEditor(bool multiline = false)
        {
            textBoxCellEditor = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = StructPLC.Font,
                Multiline = multiline,
                AcceptsReturn = multiline,
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
                WordWrap = multiline,
            };

            textBoxCellEditor.LostFocus += CellEditor_LostFocus;
            textBoxCellEditor.KeyDown += CellEditor_KeyDown;

            StructPLC.Controls.Add(textBoxCellEditor);
        }

        private void InitComboBoxCellEditor(IEnumerable<string> items)
        {
            comboBoxCellEditor = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = StructPLC.Font,
                IntegralHeight = false,
                FlatStyle = FlatStyle.Flat,
            };
            comboBoxCellEditor.Items.AddRange(items.Cast<object>().ToArray());
            comboBoxCellEditor.LostFocus += ComboBoxCellEditor_LostFocus;
            comboBoxCellEditor.KeyDown += CellEditor_KeyDown;
            StructPLC.Controls.Add(comboBoxCellEditor);
        }

        private void ComboBoxCellEditor_LostFocus(object sender, EventArgs e)
        {
            if (!isCellEditing || sender is not ComboBox combo || combo.DroppedDown)
                return;

            StructPLC.FinishCellEdit();
        }

        private void CellEditor_LostFocus(object sender, EventArgs e)
        {
            if (isCellEditing)
                StructPLC.FinishCellEdit();
        }

       
        private void CellEditor_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (cellEditUsesMultiline && !e.Control)
                    {
                        InsertNewLineInCellEditor();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        return;
                    }

                    StructPLC.FinishCellEdit();
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    cancelChanges = true;
                    StructPLC.FinishCellEdit();
                    e.Handled = true;
                    break;

                default:
                    return;
            }
        }

        private void InsertNewLineInCellEditor()
        {
            if (textBoxCellEditor is null)
                return;

            int selectionStart = textBoxCellEditor.SelectionStart;
            int selectionLength = textBoxCellEditor.SelectionLength;
            string text = textBoxCellEditor.Text ?? string.Empty;
            string newLine = Environment.NewLine;

            textBoxCellEditor.Text = text
                .Remove(selectionStart, selectionLength)
                .Insert(selectionStart, newLine);
            textBoxCellEditor.SelectionStart = selectionStart + newLine.Length;
        }


        public void RefreshTree()
        {
            if (StructPLC.Items.Count == 0)
            {
                StructPLC.BeginUpdate();
                StructPLC.ClearObjects();
                StructPLC.EndUpdate();
            }
            else
            {
                StructPLC.FocusedObject = StructPLC.SelectedObject;
                StructPLC.RefreshObjects(DataContext.Roots.ToList());
            }
        }

        public void RefreshTreeAfterBinding()
        {
            RefreshTree();
        }

        public void RebuildTree()
        {
            DataContext.RebuildTree();
            InitDataStructPLC();
        }

        private void ItemExpanded(object sender, TreeBranchExpandedEventArgs e)
        {
            (e.Model as IExpandable).Expanded = true;
            AutoResizeColumns(sender as TreeListView);
        }


        private void ItemCollapsed(object sender, TreeBranchCollapsedEventArgs e)
        {
            (e.Model as IExpandable).Expanded = false;
            AutoResizeColumns(sender as TreeListView);
        }


        private static void AutoResizeColumns(TreeListView treeListView)
        {
            if (treeListView is null)
                return;

            foreach (ColumnHeader column in treeListView.Columns)
            {
                column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
        }

        private void SelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if ((sender as TreeListView)?.SelectedObject is IClamp clamp)
            {
                DataContext.SelectedClampFunction = clamp.ClampFunction;
                DataContext.SelectedClamp = clamp;
            }
            else
            {
                DataContext.SelectedClampFunction = null;
                DataContext.SelectedClamp = null;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            EProjectManager.GetInstance().SyncAndSave(false);

            
            Editor.Editor.GetInstance().EditorForm.RefreshTree();
            DFrm.GetInstance().RefreshTree();
            EasyEPlanner.Devices.View.DevicesViewControl.Instance?.RebuildTree();
            EasyEPlanner.Binding.View.BindingViewControl.Instance?.RebuildTree();

            RebuildTree();
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            var tlv = sender as TreeListView;
           
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    var selectedModules = GetSelectedModules().ToList();
                    if (selectedModules.Any())
                    {
                        DeleteSelectedUndefinedModule();
                        e.Handled = true;
                        return;
                    }

                    (tlv.SelectedObject as IDeletable)?.Delete();
                    break;

                default:
                    return;
            }

            e.Handled = true;
            RefreshTree();
            DFrm.GetInstance().RefreshTreeAfterBinding();
            EasyEPlanner.Devices.View.DevicesViewControl.Instance?.RefreshTreeAfterBinding();
            EasyEPlanner.Binding.View.BindingViewControl.Instance?.RefreshTreeAfterBinding();
        }

        private void StructPLC_FormatCell(object sender, FormatCellEventArgs e)
        {
            if (isDraggingDeletedModule &&
                ReferenceEquals(e.Model, dragHoverRowObject))
            {
                foreach (ListViewItem.ListViewSubItem subItem in
                    e.Item.SubItems)
                {
                    subItem.BackColor = Color.LightSkyBlue;
                }
            }

            if (e.Model is IClamp clamp && !clamp.Bound)
            {
                e.Item.SubItems[1].ForeColor = Color.LightSlateGray;
            }

            if (e.Model is Node node && node.IsNtypeDisabled)
            {
                e.Item.ForeColor = Color.Gray;
                foreach (ListViewItem.ListViewSubItem subItem in e.Item.SubItems)
                    subItem.ForeColor = Color.Gray;
            }
        }

        /// <summary>
        /// Пропускает Enter в TextBox при многострочном редактировании.
        /// </summary>
        private sealed class PlcCellEditKeyEngine : CellEditKeyEngine
        {
            internal bool MultilineEditActive { get; set; }

            public override bool HandleKey(ObjectListView olv, Keys keyData)
            {
                if (MultilineEditActive &&
                    (keyData == Keys.Enter || keyData == Keys.Return))
                {
                    return false;
                }

                return base.HandleKey(olv, keyData);
            }
        }
    }
}
