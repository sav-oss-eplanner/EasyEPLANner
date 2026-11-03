using EasyEPlanner.Devices.ViewModel;
using Editor;
using EplanDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TechObject;

namespace EasyEPlanner.Binding.ViewModel
{
    public class BindingViewModel : IBindingViewModel
    {
        private const string EmptyTreeTitle = "Привязка";

        private readonly List<IBindingRoot> roots = [];
        private DeviceType[] lastDeviceTypes;
        private DeviceSubType[] lastDeviceSubTypes;
        private BindingContentKind lastContentKind = BindingContentKind.None;

        public BindingViewModel(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
            SearchContext = new DevicesSearchContext();
            ShowSignalBinding();
        }

        public IBindingRoot Root => roots.FirstOrDefault();

        public IEnumerable<IBindingRoot> Roots => roots;

        public DeviceManager DeviceManager { get; }

        public DevicesGroupingMode GroupingMode { get; set; } =
            DevicesGroupingMode.TypeThenObject;

        public BindingMode Mode { get; private set; } =
            BindingMode.SignalBinding;

        public BindingContentKind ContentKind { get; private set; } =
            BindingContentKind.None;

        public DevicesSearchContext SearchContext { get; }

        public ITreeViewItem SelectedItem { get; private set; }

        public bool CheckBoxesEnabled => Mode is BindingMode.ObjectBinding &&
            ContentKind is not BindingContentKind.None;

        public bool SingleSelect { get; private set; }

        public bool GroupingToggleVisible => Mode is BindingMode.SignalBinding;

        public bool HideBoundChannelsVisible => Mode is BindingMode.SignalBinding;

        public bool HideBoundChannels { get; set; } = true;

        public BindingAttachedEditType AttachedEditType { get; private set; }

        public Action<string> OnSetStringValue { get; set; }

        public Action<IDictionary<int, List<int>>> OnSetDictValue { get; set; }

        public TechObject.TechObject ParentTechObject { get; private set; }

        public void RebuildTree()
        {
            switch (Mode)
            {
                case BindingMode.SignalBinding:
                    SetRoots(BindingTreeBuilder.BuildSignalTree(this));
                    break;
                case BindingMode.ObjectBinding:
                    BuildEditorTree();
                    ApplyCheckedValues();
                    break;
                default:
                    SetEmptyRoot();
                    break;
            }
        }

        public void ShowSignalBinding()
        {
            Mode = BindingMode.SignalBinding;
            ContentKind = BindingContentKind.None;
            SelectedItem = null;
            ParentTechObject = null;
            SingleSelect = false;
            AttachedEditType = BindingAttachedEditType.None;
            lastDeviceTypes = null;
            lastDeviceSubTypes = null;
            lastContentKind = BindingContentKind.None;
            SetRoots(BindingTreeBuilder.BuildSignalTree(this));
        }

        public bool IsShowingEmptyEditorTree =>
            Mode is BindingMode.ObjectBinding &&
            ContentKind is BindingContentKind.None;

        public void ShowEmpty()
        {
            if (IsShowingEmptyEditorTree)
                return;

            Mode = BindingMode.ObjectBinding;
            ContentKind = BindingContentKind.None;
            SelectedItem = null;
            SingleSelect = false;
            AttachedEditType = BindingAttachedEditType.None;
            lastContentKind = BindingContentKind.None;
            SetEmptyRoot();
        }

        public void ShowEditorBinding(ITreeViewItem item, bool rebuildTree)
        {
            Mode = BindingMode.ObjectBinding;
            SelectedItem = item;
            ParentTechObject = ResolveParentTechObject(item);

            var kind = ResolveContentKind(item);
            DeviceType[] types = null;
            DeviceSubType[] subTypes = null;
            item?.GetDisplayObjects(out types, out subTypes, out _);
            bool typesEqual = TypesEqual(types, lastDeviceTypes) &&
                TypesEqual(subTypes, lastDeviceSubTypes);

            ContentKind = kind;
            AttachedEditType = ResolveAttachedEditType(item);
            SingleSelect = item is ActionParameter ||
                AttachedEditType is BindingAttachedEditType.AttachedObjectToStep;

            bool needRebuild = rebuildTree ||
                kind != lastContentKind ||
                !typesEqual;

            lastDeviceTypes = types;
            lastDeviceSubTypes = subTypes;
            lastContentKind = kind;

            if (kind is BindingContentKind.None)
            {
                SetEmptyRoot();
                return;
            }

            if (needRebuild)
                BuildEditorTree();

            ApplyCheckedValues();
        }

        public void ApplyCheckedValues()
        {
            BindingCheckHelper.UncheckAll(roots);
            if (SelectedItem is null)
                return;

            switch (ContentKind)
            {
                case BindingContentKind.Devices:
                case BindingContentKind.Parameters:
                case BindingContentKind.DevicesAndParameters:
                    ApplyDeviceAndParameterChecks(SelectedItem.EditText?[1]);
                    break;
                case BindingContentKind.Operations:
                    ApplyRestrictionChecks(SelectedItem as Restriction);
                    break;
                case BindingContentKind.AttachedObjects:
                    ApplyAttachedObjectChecks(SelectedItem as AttachedObjects);
                    break;
            }
        }

        public void SetItemCheckState(BindingFilterableViewItemBase item,
            CheckState state)
        {
            if (!CheckBoxesEnabled || item is null)
                return;

            var current = item.CheckState;
            if (current is CheckState.Unchecked &&
                state is CheckState.Indeterminate)
            {
                item.SetCheckStateInternal(CheckState.Indeterminate);
                BindingCheckHelper.UpdateParents(item);
                return;
            }

            if (current is CheckState.Indeterminate &&
                state is not CheckState.Unchecked)
                state = CheckState.Checked;
            else if (state is CheckState.Indeterminate)
                state = CheckState.Unchecked;

            if (SingleSelect && state is CheckState.Checked)
                BindingCheckHelper.UncheckAll(roots);

            if (SingleSelectInGroup && state is CheckState.Checked)
            {
                if (item.Items.OfType<BindingFilterableViewItemBase>().Any())
                    return;

                BindingCheckHelper.UncheckSiblings(item);
            }

            BindingCheckHelper.SetRecursive(item, state);
            BindingCheckHelper.UpdateParents(item);
            NotifyEditor();
        }

        public bool SingleSelectInGroup =>
            AttachedEditType is BindingAttachedEditType.AttachedAgregatesToUnit;

        public static BindingContentKind ResolveContentKind(ITreeViewItem item)
        {
            if (item is null)
                return BindingContentKind.None;
            if (item is Restriction)
                return BindingContentKind.Operations;
            if (item is AttachedObjects)
                return BindingContentKind.AttachedObjects;
            if (!item.IsUseDevList)
                return BindingContentKind.None;

            item.GetDisplayObjects(out var types, out _, out bool displayParameters);
            bool hasDeviceTypes = types is null || types.Length > 0;
            if (displayParameters && hasDeviceTypes && types is { Length: > 0 })
                return BindingContentKind.DevicesAndParameters;
            if (displayParameters && types is { Length: 0 })
                return BindingContentKind.Parameters;
            if (displayParameters)
                return BindingContentKind.DevicesAndParameters;
            return BindingContentKind.Devices;
        }

        private void BuildEditorTree()
        {
            switch (ContentKind)
            {
                case BindingContentKind.Devices:
                    SetRoots(BuildDevicesRoot());
                    break;
                case BindingContentKind.Parameters:
                    SetRoots(BuildParametersRoot());
                    break;
                case BindingContentKind.DevicesAndParameters:
                    SetRoots(BuildDevicesRoot(), BuildParametersRoot());
                    break;
                case BindingContentKind.Operations:
                    SetRoots(BindingTreeBuilder.BuildOperationsTree(
                        this, TechObjectManager.GetInstance(), ParentTechObject,
                        SelectedItem as Restriction,
                        SelectedItem?.IsLocalRestrictionUse == true));
                    break;
                case BindingContentKind.AttachedObjects:
                    SetRoots(BindingTreeBuilder.BuildAttachedObjectsTree(
                        this, TechObjectManager.GetInstance(), ParentTechObject,
                        false));
                    break;
                default:
                    SetEmptyRoot();
                    break;
            }
        }

        private BindingRoot BuildDevicesRoot()
        {
            SelectedItem.GetDisplayObjects(out var types, out var subTypes, out _);
            var boundNames = ParseBoundNames(SelectedItem.EditText?[1]);
            return BindingTreeBuilder.BuildDevicesTree(
                this,
                DeviceManager?.Devices ?? [],
                types,
                subTypes,
                ParentTechObject?.NameEplan,
                ParentTechObject?.TechNumber ?? 0,
                boundNames);
        }

        private BindingRoot BuildParametersRoot()
        {
            var parameters = ParentTechObject?.GetParamsManager()?.Float;
            return BindingTreeBuilder.BuildParametersTree(this, parameters);
        }

        private void ApplyDeviceAndParameterChecks(string checkedObjects)
        {
            var names = ParseBoundNames(checkedObjects);
            if (names.Count == 0)
                return;

            foreach (var root in roots.OfType<BindingFilterableViewItemBase>())
            {
                foreach (var node in BindingCheckHelper.Enumerate(root))
                {
                    bool shouldCheck = node switch
                    {
                        BindingDeviceNode device =>
                            names.Contains(device.Device.Name),
                        BindingParameterNode parameter =>
                            names.Contains(parameter.LuaName) ||
                            names.Contains(parameter.Param.GetParameterNumber.ToString()),
                        _ => false,
                    };

                    if (!shouldCheck)
                        continue;

                    node.SetCheckStateInternal(CheckState.Checked);
                    BindingCheckHelper.UpdateParents(node);
                }
            }
        }

        private void ApplyRestrictionChecks(Restriction restriction)
        {
            if (restriction?.RestrictDictionary is null)
                return;

            foreach (var root in roots.OfType<BindingFilterableViewItemBase>())
            {
                foreach (var node in BindingCheckHelper.Enumerate(root)
                    .OfType<BindingModeNode>())
                {
                    if (restriction.RestrictDictionary.TryGetValue(
                            node.ObjectNumber, out var modes) &&
                        modes.Contains(node.ModeNumber))
                    {
                        node.SetCheckStateInternal(CheckState.Checked);
                        BindingCheckHelper.UpdateParents(node);
                    }
                }
            }
        }

        private void ApplyAttachedObjectChecks(AttachedObjects attached)
        {
            if (attached is null || string.IsNullOrWhiteSpace(attached.Value))
                return;

            var numbers = attached.Value.Split([' '],
                    StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();

            foreach (var root in roots.OfType<BindingFilterableViewItemBase>())
            {
                foreach (var node in BindingCheckHelper.Enumerate(root)
                    .OfType<BindingTechObjectNode>())
                {
                    if (!numbers.Contains(node.ObjectNumber.ToString()))
                        continue;

                    node.SetCheckStateInternal(CheckState.Checked);
                    BindingCheckHelper.UpdateParents(node);
                }
            }
        }

        private void NotifyEditor()
        {
            switch (ContentKind)
            {
                case BindingContentKind.Devices:
                case BindingContentKind.Parameters:
                case BindingContentKind.DevicesAndParameters:
                    OnSetStringValue?.Invoke(
                        BindingSelectionCollector.CollectDevicesAndParameters(roots));
                    break;
                case BindingContentKind.Operations:
                    OnSetDictValue?.Invoke(
                        BindingSelectionCollector.CollectRestrictions(roots));
                    break;
                case BindingContentKind.AttachedObjects:
                    OnSetDictValue?.Invoke(
                        BindingSelectionCollector.CollectAttachedObjects(roots));
                    break;
            }
        }

        private void SetEmptyRoot() =>
            SetRoots(BindingTreeBuilder.BuildEmpty(this, EmptyTreeTitle));

        private void SetRoots(params BindingRoot[] newRoots)
        {
            roots.Clear();
            roots.AddRange(newRoots.Where(r => r is not null));
        }

        public static HashSet<string> ParseBoundNames(string checkedObjects)
        {
            if (string.IsNullOrWhiteSpace(checkedObjects))
                return [];

            return checkedObjects
                .Split([' '], StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();
        }

        private static TechObject.TechObject ResolveParentTechObject(ITreeViewItem item)
        {
            try
            {
                return Editor.Editor.GetInstance().EditorForm
                    ?.GetParentBranch(item) as TechObject.TechObject;
            }
            catch
            {
                return WalkToTechObject(item);
            }
        }

        internal static TechObject.TechObject WalkToTechObject(ITreeViewItem item)
        {
            while (item is not null)
            {
                if (item is TechObject.TechObject techObject)
                    return techObject;
                item = item.Parent;
            }

            return null;
        }

        private static BindingAttachedEditType ResolveAttachedEditType(
            ITreeViewItem item)
        {
            if (item is Restriction)
                return BindingAttachedEditType.Restriction;
            if (item is not AttachedObjects attached)
                return BindingAttachedEditType.None;

            bool aggregatesAttaching = attached.WorkStrategy.UseInitialization;
            if (aggregatesAttaching)
            {
                bool toUnit = attached.Owner?.BaseTechObject?.S88Level ==
                    (int)BaseTechObjectManager.ObjectType.Unit;
                return toUnit
                    ? BindingAttachedEditType.AttachedAgregatesToUnit
                    : BindingAttachedEditType.AttachedAggregatesToAggregates;
            }

            return attached.Owner is null
                ? BindingAttachedEditType.AttachedObjectToStep
                : BindingAttachedEditType.AttachedUnitsToObjectGroup;
        }

        private static bool TypesEqual<T>(T[] left, T[] right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return left.SequenceEqual(right);
        }
    }
}
