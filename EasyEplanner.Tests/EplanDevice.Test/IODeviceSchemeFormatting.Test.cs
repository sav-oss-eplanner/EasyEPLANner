using EplanDevice;
using Moq;
using NUnit.Framework;
using StaticHelper;
using System.Globalization;

namespace Tests.EplanDevices
{
    public class IODeviceSchemeFormattingTest
    {
        [Test]
        public void UpdateParametersAndRuntimeParameters_UseInvariantDecimalSeparator()
        {
            IODevice device = new AO("KOAG4AO1", "+KOAG4-AO1",
                "Test device", 1, "KOAG", 4);
            device.SetSubType("AO");
            device.SetParameter("P_MIN_V", 1.5);
            device.RuntimeParameters[IODevice.RuntimeParameter.R_EY_NUMBER.Name] = 2.5;

            var function = new Mock<IEplanFunction>();
            device.Function = function.Object;

            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                device.UpdateParameters();
                device.UpdateRuntimeParameters();
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }

            function.VerifySet(f => f.Parameters = "P_MIN_V=1.5");
            function.VerifySet(f => f.RuntimeParameters = "R_EY_NUMBER=2.5");
        }
    }
}
