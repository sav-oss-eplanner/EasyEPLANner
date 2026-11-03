using EasyEPlanner.Devices.ViewModel;
using Editor;
using EplanDevice;
using System;
using System.Collections.Generic;

namespace EasyEPlanner.Binding.ViewModel
{
    public interface IBindingViewModel
    {
        IBindingRoot Root { get; }

        IEnumerable<IBindingRoot> Roots { get; }

        DeviceManager DeviceManager { get; }

        DevicesGroupingMode GroupingMode { get; set; }

        BindingMode Mode { get; }

        BindingContentKind ContentKind { get; }

        DevicesSearchContext SearchContext { get; }

        ITreeViewItem SelectedItem { get; }

        bool CheckBoxesEnabled { get; }

        bool SingleSelect { get; }

        bool GroupingToggleVisible { get; }

        bool HideBoundChannelsVisible { get; }

        bool HideBoundChannels { get; set; }

        bool IsShowingEmptyEditorTree { get; }

        Action<string> OnSetStringValue { get; set; }

        Action<IDictionary<int, List<int>>> OnSetDictValue { get; set; }

        void RebuildTree();

        void ShowSignalBinding();

        void ShowEditorBinding(ITreeViewItem item, bool rebuildTree);

        void ShowEmpty();

        void ApplyCheckedValues();

        void SetItemCheckState(BindingFilterableViewItemBase item,
            System.Windows.Forms.CheckState state);
    }
}
