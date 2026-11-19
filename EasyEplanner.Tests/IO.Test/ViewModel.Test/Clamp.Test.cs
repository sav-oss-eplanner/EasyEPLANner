using EplanDevice;
using IO;
using IO.ViewModel;
using Moq;
using NUnit.Framework;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EplanDevice.IODevice;

namespace IOTests
{
    public class ClampTest
    {
        [Test]
        public void Getters()
        {
            var node = Mock.Of<IIONode>();
            var module = Mock.Of<IModule>(m => 
                m.IONode == node &&
                m.IOModule.Devices == new List<IIODevice>[] 
                { 
                    new List<IIODevice>() { },
                    new List<IIODevice>() { Mock.Of<IIODevice>() } 
                });
            var clamp = new Clamp(module, 1);

            Assert.Multiple(() =>
            {
                Assert.AreEqual("1", clamp.Name);
                Assert.AreSame(node, clamp.Node);
                Assert.AreEqual(Icon.Clamp, (clamp as IHasIcon).Icon);
                Assert.AreEqual(Icon.Cable, (clamp as IHasDescriptionIcon).Icon);
            });
        }

        [Test]
        public void DeleteAndReset()
        {
            var device = Mock.Of<IIODevice>();
            var channel = Mock.Of<IIOChannel>(c =>
                c.Comment == "Открыть" &&
                c.Name == "AO");

            var clampFunction = Mock.Of<IEplanFunction>();
            var ioModule = Mock.Of<IIOModule>(m => 
                m.ClampFunctions == new Dictionary<int, IEplanFunction>() 
                { 
                    { 1, clampFunction }
                } &&
                m.GetClampBinding(1) == new List<(IIODevice, IIOChannel)>() 
                { 
                    new Tuple<IIODevice, IIOChannel>(device, channel).ToValueTuple() 
                } &&
                m.Info == Mock.Of<IIOModuleInfo>(
                    i => i.AddressSpaceType == IOModuleInfo.ADDRESS_SPACE_TYPE.AOAI));

            var module = Mock.Of<IModule>(m => m.IOModule == ioModule);

            var clamp = new Clamp(module, 1);

            clamp.Delete();

            Mock.Get(device).Verify(d => d.ClearChannel(IOModuleInfo.ADDRESS_SPACE_TYPE.AOAI, "Открыть", "AO"));
            Mock.Get(clampFunction).VerifySet(f => f.FunctionalText = "Резерв");
            Mock.Get(ioModule).Verify(m => m.ClearBind(1));
        }


        [Test]
        public void DeleteWithoutBinding()
        {
            var clampFunction = Mock.Of<IEplanFunction>();
            var ioModule = Mock.Of<IIOModule>(m =>
                m.ClampFunctions == new Dictionary<int, IEplanFunction>()
                {
                    { 1, clampFunction }
                } &&
                m.GetClampBinding(1) == null);

            var module = Mock.Of<IModule>(m => m.IOModule == ioModule);

            var clamp = new Clamp(module, 1);

            Assert.DoesNotThrow(() => clamp.Delete());

            Mock.Get(clampFunction).VerifySet(f => f.FunctionalText = "Резерв");
            Mock.Get(ioModule).Verify(m => m.ClearBind(1));
        }

        [Test]
        public void DeleteWithUndefinedChannel()
        {
            var device = Mock.Of<IIODevice>();
            var clampFunction = Mock.Of<IEplanFunction>();
            var ioModule = Mock.Of<IIOModule>(m =>
                m.ClampFunctions == new Dictionary<int, IEplanFunction>()
                {
                    { 1, clampFunction }
                } &&
                m.GetClampBinding(1) == new List<(IIODevice, IIOChannel)>()
                {
                    new Tuple<IIODevice, IIOChannel>(device, null).ToValueTuple()
                } &&
                m.Info == Mock.Of<IIOModuleInfo>(
                    i => i.AddressSpaceType == IOModuleInfo.ADDRESS_SPACE_TYPE.AOAI));

            var module = Mock.Of<IModule>(m => m.IOModule == ioModule);

            var clamp = new Clamp(module, 1);

            Assert.DoesNotThrow(() => clamp.Delete());

            Mock.Get(device).Verify(
                d => d.ClearChannel(It.IsAny<IOModuleInfo.ADDRESS_SPACE_TYPE>(),
                    It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            Mock.Get(clampFunction).VerifySet(f => f.FunctionalText = "Резерв");
            Mock.Get(ioModule).Verify(m => m.ClearBind(1));
        }

        [Test]
        public void InvalidText_ShowsErrorIcon()
        {
            var clampFunction = Mock.Of<IEplanFunction>(f =>
                f.FunctionalText == "+OBJ1-V1");
            var ioModule = Mock.Of<IIOModule>(m =>
                m.Devices == new List<IIODevice>[] { null, null } &&
                m.ClampFunctions == new Dictionary<int, IEplanFunction>()
                {
                    { 1, clampFunction },
                });
            var module = Mock.Of<IModule>(m =>
                m.IOModule == ioModule &&
                m.IONode == Mock.Of<IIONode>());

            var clamp = new Clamp(module, 1);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(clamp.HasBindingError);
                Assert.AreEqual(Icon.Error, (clamp as IHasDescriptionIcon).Icon);
            });
        }

        [Test]
        public void Reserve_DoesNotShowErrorIcon()
        {
            var clampFunction = Mock.Of<IEplanFunction>(f =>
                f.FunctionalText == CommonConst.Reserve);
            var ioModule = Mock.Of<IIOModule>(m =>
                m.Devices == new List<IIODevice>[] { null, null } &&
                m.ClampFunctions == new Dictionary<int, IEplanFunction>()
                {
                    { 1, clampFunction },
                });
            var module = Mock.Of<IModule>(m =>
                m.IOModule == ioModule &&
                m.IONode == Mock.Of<IIONode>());

            var clamp = new Clamp(module, 1);

            Assert.Multiple(() =>
            {
                Assert.IsFalse(clamp.HasBindingError);
                Assert.AreEqual(Icon.None, (clamp as IHasDescriptionIcon).Icon);
            });
        }

        [Test]
        public void Value()
        {
            var clampFunction = Mock.Of<IEplanFunction>(c => c.FunctionalText == "1234");
            var ioModule = Mock.Of<IIOModule>(m =>
                m.ClampFunctions == new Dictionary<int, IEplanFunction>() { { 1, clampFunction } });

            var module = Mock.Of<IModule>(m => m.IOModule == ioModule);

            var clamp = new Clamp(module, 1);

            Assert.Multiple(() =>
            {
                Assert.AreEqual("1234", clamp.Value);

                Assert.IsTrue(clamp.SetValue("qwerty"));
                Mock.Get(clampFunction).VerifySet(f => f.FunctionalText = "qwerty");
            });
        }


        [Test]
        public void Description()
        {
            var ioLinkProps = new IOLinkSize
            {
                SizeOut = 1,
                SizeIn = 2
            };

            var ioModule = Mock.Of<IIOModule>(m =>
                m.GetClampBinding(1) == new List<(IIODevice, IIOChannel)>() 
                {
                    new Tuple<IIODevice, IIOChannel>(
                        Mock.Of<IIODevice>(d => d.Name == "DEV1" && d.IOLinkProperties == ioLinkProps),
                        Mock.Of<IIOChannel>(c => c.Name == "AO")).ToValueTuple(),
                    new Tuple<IIODevice, IIOChannel>(
                        Mock.Of<IIODevice>(d => d.Name == "DEV2" && d.IOLinkProperties == ioLinkProps),
                        Mock.Of<IIOChannel>(c => c.Name == "AI")).ToValueTuple(),
                } &&
                m.Info == Mock.Of<IIOModuleInfo>(i => i.AddressSpaceType == IOModuleInfo.ADDRESS_SPACE_TYPE.AOAI)
                );

            var module = Mock.Of<IModule>(m => m.IOModule == ioModule);

            var clamp = new Clamp(module, 1);

            Assert.Multiple(() =>
            {
                Assert.AreEqual("DEV1:AO(16) · DEV2:AI(32)", clamp.Description);
                Assert.AreEqual("DEV1:AO(16)\r\nDEV2:AI(32)", (clamp as IToolTip).Description);
            });
        }

        [Test]
        public void Description_UsesUnroundedIOLinkSizeFromFile()
        {
            var roundedIoLinkProps = new IOLinkSize
            {
                SizeOut = 1,
                SizeIn = 1,
                SizeOutFromFile = 0.5f,
                SizeInFromFile = 0.5f
            };
            var fileOnlyIoLinkProps = new IOLinkSize
            {
                SizeOutFromFile = 0.5f,
                SizeInFromFile = 0.5f
            };

            var ioModule = Mock.Of<IIOModule>(m =>
                m.GetClampBinding(1) == new List<(IIODevice, IIOChannel)>()
                {
                    new Tuple<IIODevice, IIOChannel>(
                        Mock.Of<IIODevice>(d => d.Name == "DEV1" && d.IOLinkProperties == roundedIoLinkProps),
                        Mock.Of<IIOChannel>(c => c.Name == "AO")).ToValueTuple(),
                    new Tuple<IIODevice, IIOChannel>(
                        Mock.Of<IIODevice>(d => d.Name == "DEV2" && d.IOLinkProperties == roundedIoLinkProps),
                        Mock.Of<IIOChannel>(c => c.Name == "AI")).ToValueTuple(),
                    new Tuple<IIODevice, IIOChannel>(
                        Mock.Of<IIODevice>(d => d.Name == "DEV3" && d.IOLinkProperties == fileOnlyIoLinkProps),
                        Mock.Of<IIOChannel>(c => c.Name == "AO")).ToValueTuple(),
                    new Tuple<IIODevice, IIOChannel>(
                        Mock.Of<IIODevice>(d => d.Name == "DEV4" && d.IOLinkProperties == fileOnlyIoLinkProps),
                        Mock.Of<IIOChannel>(c => c.Name == "AI")).ToValueTuple(),
                } &&
                m.Info == Mock.Of<IIOModuleInfo>(i => i.AddressSpaceType == IOModuleInfo.ADDRESS_SPACE_TYPE.AOAI)
                );

            var module = Mock.Of<IModule>(m => m.IOModule == ioModule);
            var clamp = new Clamp(module, 1);

            Assert.Multiple(() =>
            {
                Assert.AreEqual("DEV1:AO(8) · DEV2:AI(8) · DEV3:AO(8) · DEV4:AI(8)", clamp.Description);
                Assert.AreEqual("DEV1:AO(8)\r\nDEV2:AI(8)\r\nDEV3:AO(8)\r\nDEV4:AI(8)", (clamp as IToolTip).Description);
            });
        }

        [Test]
        public void Description_WithMultilineFunctionalText_FormatsForCell()
        {
            var clampFunction = Mock.Of<IEplanFunction>(f =>
                f.FunctionalText == "line1\u00B6line2");
            var ioModule = Mock.Of<IIOModule>(m =>
                m.Devices == new List<IIODevice>[] { null, null } &&
                m.ClampFunctions == new Dictionary<int, IEplanFunction>()
                {
                    { 1, clampFunction },
                });
            var module = Mock.Of<IModule>(m =>
                m.IOModule == ioModule &&
                m.IONode == Mock.Of<IIONode>());

            var clamp = new Clamp(module, 1);

            Assert.AreEqual("line1 · line2", clamp.Description);
        }
    }
}
