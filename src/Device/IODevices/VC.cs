using System.Collections.Generic;

namespace EplanDevice
{
    /// <summary>
    /// Технологическое устройство - управляемый клапан.
    /// </summary>
    sealed public class VC : IODevice, ISetupTerminal
    {
        public VC(string name, string eplanName, string description,
            int deviceNumber, string objectName, int objectNumber,
            string articleName) : base(name, eplanName, description,
                deviceNumber, objectName, objectNumber)
        {
            dSubType = DeviceSubType.NONE;
            dType = DeviceType.VC;
            ArticleName = articleName;
        }

        public override string PIDUnitFormat => UnitFormat.Percentages;

        public override string SetSubType(string subType)
        {
            base.SetSubType(subType);

            string errStr = string.Empty;
            switch (subType)
            {
                case nameof(DeviceSubType.VC):
                    AO.Add(new IOChannel("AO", -1, -1, -1, ""));
                    break;

                case nameof(DeviceSubType.VC_EY):
                    AO.Add(new IOChannel("AO", -1, -1, -1, ""));
                    RuntimeParameters.Add(RuntimeParameter.R_EY_NUMBER, null);
                    properties.Add(Property.TERMINAL, null);
                    break;

                case nameof(DeviceSubType.VC_IOLINK):
                    AO.Add(new IOChannel("AO", -1, -1, -1, ""));
                    AI.Add(new IOChannel("AI", -1, -1, -1, ""));
                    parameters.Add(Parameter.P_FB, null);
                    SetIOLinkSizes(ArticleName);
                    break;

                case "VC_VIRT":
                    break;

                case "":
                    errStr = string.Format("\"{0}\" - не задан тип" +
                        " (VC, VC_IOLINK, VC_VIRT).\n", Name);
                    break;

                default:
                    errStr = string.Format("\"{0}\" - неверный тип" +
                        $" ({string.Join(", ", DeviceType.VC.SubTypeNames())}).\n", Name);
                    break;
            }

            return errStr;
        }

        public override string GetDeviceSubTypeStr(DeviceType dt,
            DeviceSubType dst)
        {
            switch (dt)
            {
                case DeviceType.VC:
                    switch (dst)
                    {
                        case DeviceSubType.VC:
                            return "VC";
                        case DeviceSubType.VC_IOLINK:
                            return "VC_IOLINK";
                        case DeviceSubType.VC_VIRT:
                            return "VC_VIRT";
                        case DeviceSubType.VC_EY:
                            return nameof(DeviceSubType.VC_EY);
                    }
                    break;
            }

            return string.Empty;
        }

        public override Dictionary<ITag, int> GetDeviceProperties(
            DeviceType dt, DeviceSubType dst)
        {
            switch (dt)
            {
                case DeviceType.VC:
                    switch (dst)
                    {
                        case DeviceSubType.VC:
                            return new Dictionary<ITag, int>()
                            {
                                {Tag.ST, 1},
                                {Tag.M, 1},
                                {Tag.V, 1},
                            };

                        case DeviceSubType.VC_EY:
                            return new Dictionary<ITag, int>()
                            {
                                {Tag.ST, 1},
                                {Tag.M, 1},
                                {Tag.V, 1},
                            };

                        case DeviceSubType.VC_IOLINK:
                            return new Dictionary<ITag, int>()
                            {
                                {Tag.ST, 1},
                                {Tag.M, 1},
                                {Tag.V, 1},
                                {Tag.BLINK, 1},
                                {Tag.NAMUR_ST, 1},
                                {Tag.OPENED, 1},
                                {Tag.CLOSED, 1},
                                {Parameter.P_FB, 1},
                            };

                        case DeviceSubType.VC_VIRT:
                            return new Dictionary<ITag, int>()
                            {
                                {Tag.ST, 1},
                                {Tag.M, 1},
                                {Tag.V, 1},
                            };
                    }
                    break;
            }

            return null;
        }

        public override string Check()
        {
            string res = base.Check();

            bool emptyArticle = ArticleName == string.Empty;
            bool needCheckArticle = DeviceSubType == DeviceSubType.VC_IOLINK;
            if (needCheckArticle && emptyArticle)
            {
                res += $"\"{name}\" - не задано изделие.\n";
            }

            return res;
        }

        public void SetupTerminal(string terminal, string action, int clamp)
        {
            if (DeviceSubType is not DeviceSubType.VC_EY)
                return;

            SetProperty(Property.TERMINAL, terminal);
            SetRuntimeParameter(RuntimeParameter.R_EY_NUMBER, clamp);
        }
    }
}
