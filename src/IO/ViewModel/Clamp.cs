using EasyEPlanner;
using EplanDevice;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.ViewModel
{
    public class Clamp(IModule owner, int clamp) : IClamp, IHasIcon, IHasDescriptionIcon, IDeletable, IToolTip, IEditable
    {
        public string Name => clamp.ToString();

        string IToolTip.Description
        {
            get
            {
                var description = ClampBindingText.FormatForTooltip(
                    RawDescription);
                if (HasBindingError)
                {
                    description = $"Ошибка{Environment.NewLine}{description}";
                }

                return description;
            }
        }

        public string Description =>
            ClampBindingText.FormatForCell(RawDescription);

        private string RawDescription
        {
            get
            {
                var bindings = Module.GetClampBinding(clamp)?
                    .Select(g => ChannelBinding(g.Item1, g.Item2))
                    .Where(binding => !string.IsNullOrEmpty(binding))
                    .ToList();
                if (bindings is { Count: > 0 })
                {
                    return string.Join(ClampBindingText.Separator, bindings);
                }

                return Value ?? string.Empty;
            }
        }

        /// <summary>
        /// Генерация текста привязки канала устройства к клемме
        /// </summary>
        /// <param name="device">Устройство</param>
        /// <param name="channel">Канал</param>
        /// <returns>Текст привязки канала</returns>
        private string ChannelBinding(IIODevice device, IODevice.IIOChannel channel)
        {
            var type = (Module.Info.AddressSpaceType is
                IOModuleInfo.ADDRESS_SPACE_TYPE.AOAIDODI or
                IOModuleInfo.ADDRESS_SPACE_TYPE.AOAI or
                IOModuleInfo.ADDRESS_SPACE_TYPE.DODI) ? $":{channel.Name}" : "";

            var ioLinkProperties = device.IOLinkProperties;
            var ioLinkSize = channel.Name switch
            {
                "AI" or "DI" => FormatIOLinkSize(
                    ioLinkProperties.SizeIn,
                    ioLinkProperties.SizeInFromFile),
                "AO" or "DO" => FormatIOLinkSize(
                    ioLinkProperties.SizeOut,
                    ioLinkProperties.SizeOutFromFile),
                _ => string.Empty
            };


            var comment = (string.IsNullOrEmpty(channel.Comment)) ? "" : $":{channel.Comment}";

            return $"{device.Name}{type}{ioLinkSize}{comment}"; 
        }

        private static string FormatIOLinkSize(int roundedSize, float sizeFromFile)
        {
            float size = sizeFromFile > 0 ? sizeFromFile : roundedSize;
            return size > 0 ? $"({size * 16})" : string.Empty;
        }

        public IEplanFunction ClampFunction =>
            Module.ClampFunctions != null &&
            Module.ClampFunctions.TryGetValue(clamp, out var function)
                ? function
                : null;

        public IIOModule Module => owner.IOModule;

        public IIONode Node => owner.IONode;

        Icon IHasIcon.Icon => Icon.Clamp;

        Icon IHasDescriptionIcon.Icon
        {
            get
            {
                if (HasBindingError)
                    return Icon.Error;
                if (Bound)
                    return Icon.Cable;
                return Icon.None;
            }
        }

        public string Value => ClampFunction?.FunctionalText;

        public bool Bound =>
            clamp >= 0 &&
            clamp < Module.Devices.Length &&
            (Module.Devices[clamp]?.Any() ?? false);

        public bool HasBindingError =>
            !Bound &&
            !string.IsNullOrEmpty(Value) &&
            !Value.Contains(CommonConst.Reserve);

        public void Reset()
        {
            Module.ClearBind(clamp);
        }

        public void Delete()
        {
            foreach (var (dev, channel) in Module.GetClampBinding(clamp) ?? [])
            {
                if (dev is null || channel is null)
                    continue;

                dev.ClearChannel(Module.Info.AddressSpaceType, channel.Comment, channel.Name);
            }
            ClampFunction?.FunctionalText = "Резерв";
            Reset();
        }

        public bool SetValue(string value)
        {
            if (ClampFunction is null || ClampFunction.FunctionalText == value)
                return false;

            ClampFunction.FunctionalText = value;
            Reset();

            return true;
        }
    }
}
