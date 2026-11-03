using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner.Binding.ViewModel
{
    public static class BindingSelectionCollector
    {
        public static string CollectDevicesAndParameters(
            IEnumerable<IBindingRoot> roots)
        {
            var names = new List<string>();
            foreach (var node in EnumerateChecked(roots))
            {
                switch (node)
                {
                    case BindingDeviceNode device:
                        names.Add(device.Device.Name);
                        break;
                    case BindingParameterNode parameter:
                        names.Add(parameter.LuaName);
                        break;
                }
            }

            return string.Join(" ", names);
        }

        public static IDictionary<int, List<int>> CollectRestrictions(
            IEnumerable<IBindingRoot> roots)
        {
            var dict = new Dictionary<int, List<int>>();
            foreach (var mode in EnumerateChecked(roots).OfType<BindingModeNode>())
                AddUniqueMode(dict, mode.ObjectNumber, mode.ModeNumber);

            foreach (var modes in dict.Values)
                modes.Sort();

            return dict;
        }

        public static IDictionary<int, List<int>> CollectAttachedObjects(
            IEnumerable<IBindingRoot> roots)
        {
            var dict = new Dictionary<int, List<int>>();
            foreach (var techObject in EnumerateChecked(roots)
                .OfType<BindingTechObjectNode>())
            {
                dict[techObject.ObjectNumber] = [];
            }

            return dict;
        }

        private static IEnumerable<BindingFilterableViewItemBase> EnumerateChecked(
            IEnumerable<IBindingRoot> roots)
        {
            foreach (var root in roots.OfType<BindingFilterableViewItemBase>())
            {
                foreach (var node in BindingCheckHelper.Enumerate(root))
                {
                    if (node.CheckState == CheckState.Checked)
                        yield return node;
                }
            }
        }

        private static void AddUniqueMode(
            IDictionary<int, List<int>> dict, int objectNumber, int modeNumber)
        {
            if (!dict.TryGetValue(objectNumber, out var modes))
            {
                modes = [];
                dict[objectNumber] = modes;
            }

            if (!modes.Contains(modeNumber))
                modes.Add(modeNumber);
        }
    }
}
