using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyEPlanner;
using InterprojectExchange;
using Moq;
using NUnit.Framework;

namespace EasyEplannerTests.InterprojectExchangeTest
{
    public class InterprojectExchangeTest
    {
        [TearDown]
        public void TearDown()
        {
            ResetSingleton();
        }

        [Test]
        public void CheckBindingSignalsTest_CheckErrorString()
        {
            string expected = "remote_gateways: adv_prj - AI\nshared_devices: adv_prj - AI\n";

            var mainReceiver = new DeviceSignalsInfo();
            mainReceiver.AI.Add("AI1");

            var mainSource = new DeviceSignalsInfo();
            var advancedSource = new DeviceSignalsInfo();

            var advancedReceiver = new DeviceSignalsInfo();
            advancedReceiver.AI.Add("AI2");

            var mainModel = new Mock<CurrentProjectModel>();

            mainModel.Setup(obj => obj.ReceiverSignals).Returns(mainReceiver);
            mainModel.Setup(obj => obj.SourceSignals).Returns(mainSource);
            mainModel.Setup(obj => obj.ProjectName).Returns("main_prj");

            var advancedModel = new Mock<IProjectModel>();

            advancedModel.Setup(obj => obj.ReceiverSignals).Returns(advancedReceiver);
            advancedModel.Setup(obj => obj.SourceSignals).Returns(advancedSource);
            advancedModel.Setup(obj => obj.ProjectName).Returns("adv_prj");
            advancedModel.Setup(obj => obj.Loaded).Returns(true);
            advancedModel.SetupProperty(obj => obj.HasBindingError);

            var interprojectExchangeMock = new Mock<InterprojectExchange.InterprojectExchange>();

            interprojectExchangeMock.Setup(obj => obj.MainModel).Returns(mainModel.Object);

            var interprojectExchange = interprojectExchangeMock.Object;

            interprojectExchange.AddModel(mainModel.Object);
            interprojectExchange.AddModel(advancedModel.Object);

            Assert.Multiple(() =>
            {
            Assert.AreEqual(expected, interprojectExchange.CheckBindingSignals());
                Assert.IsTrue(advancedModel.Object.HasBindingError);
                Assert.IsTrue(advancedModel.Object.Loaded);
                CollectionAssert.AreEqual(
                    new[] { "AI1" }, mainReceiver.AI);
                CollectionAssert.AreEqual(
                    new[] { DeviceSignalsInfo.UnpairedSignal },
                    advancedSource.AI);
                CollectionAssert.AreEqual(
                    new[] { DeviceSignalsInfo.UnpairedSignal },
                    mainSource.AI);
                CollectionAssert.AreEqual(
                    new[] { "AI2" }, advancedReceiver.AI);
            });
        }

        [Test]
        public void CheckBindingSignals_EqualCounts_KeepsLoadedWithoutError()
        {
            var mainReceiver = new DeviceSignalsInfo();
            mainReceiver.DI.Add("DI1");
            var mainSource = new DeviceSignalsInfo();
            mainSource.DO.Add("DO1");

            var advancedSource = new DeviceSignalsInfo();
            advancedSource.DI.Add("DI2");
            var advancedReceiver = new DeviceSignalsInfo();
            advancedReceiver.DO.Add("DO2");

            var mainModel = new Mock<CurrentProjectModel>();
            mainModel.Setup(obj => obj.ReceiverSignals).Returns(mainReceiver);
            mainModel.Setup(obj => obj.SourceSignals).Returns(mainSource);
            mainModel.Setup(obj => obj.ProjectName).Returns("main_prj");

            var advancedModel = new Mock<IProjectModel>();
            advancedModel.Setup(obj => obj.ReceiverSignals).Returns(advancedReceiver);
            advancedModel.Setup(obj => obj.SourceSignals).Returns(advancedSource);
            advancedModel.Setup(obj => obj.ProjectName).Returns("adv_prj");
            advancedModel.Setup(obj => obj.Loaded).Returns(true);
            advancedModel.SetupProperty(obj => obj.HasBindingError, true);

            var interprojectExchangeMock =
                new Mock<InterprojectExchange.InterprojectExchange>();
            interprojectExchangeMock.Setup(obj => obj.MainModel)
                .Returns(mainModel.Object);
            var interprojectExchange = interprojectExchangeMock.Object;
            interprojectExchange.AddModel(mainModel.Object);
            interprojectExchange.AddModel(advancedModel.Object);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(string.Empty,
                    interprojectExchange.CheckBindingSignals());
                Assert.IsFalse(advancedModel.Object.HasBindingError);
                Assert.IsTrue(advancedModel.Object.Loaded);
            });
        }

        [Test]
        public void GetModelsWithBindingErrors_ReturnsOnlyLoadedErrorProjects()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var errorModel, out var okModel);

            errorModel.HasBindingError = true;
            okModel.HasBindingError = false;

            var markedError = new AdvancedProjectModel
            {
                ProjectName = "marked",
                Loaded = true,
                HasBindingError = true,
                MarkedForDelete = true
            };
            exchange.AddModel(markedError);

            CollectionAssert.AreEqual(
                new[] { errorModel.ProjectName },
                exchange.GetModelsWithBindingErrors());
        }

        [Test]
        public void RefreshSelectedModelBindingError_UpdatesFlagByUnpairedSignals()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "-" });
            advancedModel.ReceiverSignals.DO.AddRange(new[] { "DI1", "DI2" });
            advancedModel.HasBindingError = false;

            exchange.RefreshSelectedModelBindingError();
            Assert.IsTrue(advancedModel.HasBindingError);

            mainModel.SourceSignals.DO.Clear();
            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "DO2" });
            exchange.RefreshSelectedModelBindingError();
            Assert.IsFalse(advancedModel.HasBindingError);
        }

        [Test]
        public void PairUnpairedSignals_RestoresPairAndClearsError()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "-", "-" });
            advancedModel.ReceiverSignals.DO.AddRange(new[] { "-", "DI1", "DI2" });
            advancedModel.HasBindingError = true;

            bool paired = exchange.PairUnpairedSignals("DO", "DO1", "DI1");

            Assert.Multiple(() =>
            {
                Assert.IsTrue(paired);
                CollectionAssert.AreEqual(
                    new[] { "DO1", "-" }, mainModel.SourceSignals.DO);
                CollectionAssert.AreEqual(
                    new[] { "DI1", "DI2" },
                    advancedModel.ReceiverSignals.DO);
                Assert.IsTrue(advancedModel.HasBindingError);
            });

            bool filled = exchange.FillUnpairedSignal(
                "DO", "DI2", "DO2", fillAdvancedSide: false);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(filled);
                CollectionAssert.AreEqual(
                    new[] { "DO1", "DO2" }, mainModel.SourceSignals.DO);
                CollectionAssert.AreEqual(
                    new[] { "DI1", "DI2" },
                    advancedModel.ReceiverSignals.DO);
                Assert.IsFalse(advancedModel.HasBindingError);
            });
        }

        [Test]
        public void PairUnpairedSignals_WithUnpairedName_ReturnsFalse()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "-" });
            advancedModel.ReceiverSignals.DO.AddRange(new[] { "-", "DI1" });

            Assert.IsFalse(exchange.PairUnpairedSignals(
                "DO", DeviceSignalsInfo.UnpairedSignal, "DI1"));
            Assert.IsFalse(exchange.PairUnpairedSignals(
                "DO", "DO1", DeviceSignalsInfo.UnpairedSignal));
        }

        [Test]
        public void FillUnpairedSignal_FillAdvancedSide_RemovesDuplicateUnpairedRow()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "-" });
            advancedModel.ReceiverSignals.DO.AddRange(new[] { "-", "DI1" });
            advancedModel.HasBindingError = true;

            bool filled = exchange.FillUnpairedSignal(
                "DO", "DO1", "DI1", fillAdvancedSide: true);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(filled);
                CollectionAssert.AreEqual(
                    new[] { "DO1" }, mainModel.SourceSignals.DO);
                CollectionAssert.AreEqual(
                    new[] { "DI1" }, advancedModel.ReceiverSignals.DO);
                Assert.IsFalse(advancedModel.HasBindingError);
            });
        }

        [Test]
        public void FillUnpairedSignal_AlreadyBoundDevice_ReturnsFalse()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "DO2", "-" });
            advancedModel.ReceiverSignals.DO.AddRange(new[] { "DI1", "DI2", "-" });

            Assert.IsFalse(exchange.FillUnpairedSignal(
                "DO", "DO2", "DI1", fillAdvancedSide: true));
        }

        [Test]
        public void BindSignals_WhenHasBindingError_ReturnsFalse()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);
            advancedModel.HasBindingError = true;

            Assert.IsFalse(exchange.BindSignals("DO", "DO1", "DI1"));
            Assert.Multiple(() =>
            {
                Assert.IsEmpty(mainModel.SourceSignals.DO);
                Assert.IsEmpty(advancedModel.ReceiverSignals.DO);
            });
        }

        [Test]
        public void BindSignals_RejectsUnpairedAndAlreadyBound()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.Add("DO1");
            advancedModel.ReceiverSignals.DO.Add("DI1");

            Assert.Multiple(() =>
            {
                Assert.IsFalse(exchange.BindSignals(
                    "DO", DeviceSignalsInfo.UnpairedSignal, "DI2"));
                Assert.IsFalse(exchange.BindSignals(
                    "DO", "DO2", DeviceSignalsInfo.UnpairedSignal));
                Assert.IsFalse(exchange.BindSignals("DO", "DO1", "DI3"));
                Assert.IsTrue(exchange.BindSignals("DO", "DO2", "DI2"));
                CollectionAssert.AreEqual(
                    new[] { "DO1", "DO2" }, mainModel.SourceSignals.DO);
                CollectionAssert.AreEqual(
                    new[] { "DI1", "DI2" },
                    advancedModel.ReceiverSignals.DO);
            });
        }

        [Test]
        public void DeleteSignalsBind_RemovesUnpairedRowByPair()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "-" });
            advancedModel.ReceiverSignals.DO.AddRange(new[] { "-", "DI1" });
            advancedModel.HasBindingError = true;

            Assert.IsTrue(exchange.DeleteSignalsBind(
                "DO", DeviceSignalsInfo.UnpairedSignal, "DI1"));

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEqual(
                    new[] { "DO1" }, mainModel.SourceSignals.DO);
                CollectionAssert.AreEqual(
                    new[] { DeviceSignalsInfo.UnpairedSignal },
                    advancedModel.ReceiverSignals.DO);
                Assert.IsTrue(advancedModel.HasBindingError);
            });
        }

        [Test]
        public void MoveSignalsBind_MovesPairIncludingUnpairedPlaceholder()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "-" });
            advancedModel.ReceiverSignals.DO.AddRange(new[] { "DI1", "DI2" });

            Assert.IsTrue(exchange.MoveSignalsBind(
                "DO", "-", "DI2", -1));

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEqual(
                    new[] { "-", "DO1" }, mainModel.SourceSignals.DO);
                CollectionAssert.AreEqual(
                    new[] { "DI2", "DI1" },
                    advancedModel.ReceiverSignals.DO);
                Assert.IsFalse(exchange.MoveSignalsBind(
                    "DO", "-", "DI2", -1));
            });
        }

        [Test]
        public void UpdateProjectBinding_WhenHasBindingError_ReturnsFalse()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.Add("DO1");
            advancedModel.ReceiverSignals.DO.Add("DI1");
            advancedModel.HasBindingError = true;

            Assert.IsFalse(exchange.UpdateProjectBinding(
                "DO", "DO1", "DO2", true, out _));
            CollectionAssert.AreEqual(
                new[] { "DO1" }, mainModel.SourceSignals.DO);
        }

        [Test]
        public void UpdateProjectBinding_RejectsUnpairedValues()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "-" });
            advancedModel.ReceiverSignals.DO.AddRange(new[] { "DI1", "DI2" });

            Assert.Multiple(() =>
            {
                Assert.IsFalse(exchange.UpdateProjectBinding(
                    "DO", DeviceSignalsInfo.UnpairedSignal, "DO2",
                    true, out _));
                Assert.IsFalse(exchange.UpdateProjectBinding(
                    "DO", "DO1", DeviceSignalsInfo.UnpairedSignal,
                    true, out _));
                Assert.IsTrue(exchange.UpdateProjectBinding(
                    "DO", "DO1", "DO3", true, out bool needSwap));
                Assert.IsFalse(needSwap);
                CollectionAssert.AreEqual(
                    new[] { "DO3", "-" }, mainModel.SourceSignals.DO);
            });
        }

        [Test]
        public void GetBindedSignals_ReturnsExpandedUnpairedPairs()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.Add("DO1");
            advancedModel.ReceiverSignals.DO.AddRange(new[] { "DI1", "DI2" });

            var pairs = exchange.GetBindedSignals()["DO"];

            Assert.Multiple(() =>
            {
                Assert.AreEqual(3, pairs.Count);
                CollectionAssert.AreEqual(
                    new[] { "DO1", DeviceSignalsInfo.UnpairedSignal },
                    pairs[0]);
                CollectionAssert.AreEqual(
                    new[] { DeviceSignalsInfo.UnpairedSignal, "DI1" },
                    pairs[1]);
                CollectionAssert.AreEqual(
                    new[] { DeviceSignalsInfo.UnpairedSignal, "DI2" },
                    pairs[2]);
            });
        }

        [Test]
        public void GetBindedSignals_PutsUnpairedPairsBeforePairedOnes()
        {
            var exchange = CreateExchangeWithModels(
                out var mainModel, out var advancedModel, out _);

            mainModel.SourceSignals.DO.AddRange(new[] { "DO1", "DO2", "DO3" });
            advancedModel.ReceiverSignals.DO.AddRange(
                new[] { "DI1", DeviceSignalsInfo.UnpairedSignal, "DI3" });

            var pairs = exchange.GetBindedSignals()["DO"];

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEqual(
                    new[] { "DO2", DeviceSignalsInfo.UnpairedSignal },
                    pairs[0]);
                CollectionAssert.AreEqual(new[] { "DO1", "DI1" }, pairs[1]);
                CollectionAssert.AreEqual(new[] { "DO3", "DI3" }, pairs[2]);
            });
        }

        [TestCaseSource(nameof(GetSignalsPairs_Test_CaseSrc))]
        public void GetSignalsPairs_Test(List<string> currSignals,
            List<string> advSignals, List<string[]> expected)
        {
            var result = typeof(InterprojectExchange.InterprojectExchange)
                .GetMethod("GetSignalsPairs", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { currSignals, advSignals }) as List<string[]>;

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expected.Count, result.Count,
                    "Количество пар сигналов");
                for (int i = 0; i < expected.Count; i++)
                {
                    CollectionAssert.AreEqual(expected[i], result[i],
                        $"Пара сигналов [{i}]");
                }
            });
        }

        public static object[] GetSignalsPairs_Test_CaseSrc = new object[]
        {
            new object[]
            {
                new List<string>{ "S1", "S2" },
                new List<string>{ "S1", "S2" },
                new List<string[]>{ new []{"S1", "S1"}, new []{"S2", "S2"}, },
            },
            new object[]
            {
                new List<string>{ "S1", "S2" },
                new List<string>{ "S1" },
                new List<string[]>{
                    new []{"S1", DeviceSignalsInfo.UnpairedSignal},
                    new []{"S2", DeviceSignalsInfo.UnpairedSignal},
                    new []{DeviceSignalsInfo.UnpairedSignal, "S1"},
                },
            },
            new object[]
            {
                new List<string>{ "S1", },
                new List<string>{ "S1", "S2" },
                new List<string[]>{
                    new []{"S1", DeviceSignalsInfo.UnpairedSignal},
                    new []{DeviceSignalsInfo.UnpairedSignal, "S1"},
                    new []{DeviceSignalsInfo.UnpairedSignal, "S2"},
                },
            },
            new object[]
            {
                new List<string>{ "S1", },
                new List<string>{ },
                new List<string[]>{
                    new []{"S1", DeviceSignalsInfo.UnpairedSignal},
                },
            },
            // Повторяющиеся "-" не должны схлопываться в одно имя
            new object[]
            {
                new List<string>{ "DI1", "DI2", "-", "-" },
                new List<string>{ "-", "-", "DO1", "DO2" },
                new List<string[]>{
                    new []{"DI1", DeviceSignalsInfo.UnpairedSignal},
                    new []{"DI2", DeviceSignalsInfo.UnpairedSignal},
                    new []{DeviceSignalsInfo.UnpairedSignal, "DO1"},
                    new []{DeviceSignalsInfo.UnpairedSignal, "DO2"},
                },
            }
        };


        [Test]
        public void GetMainModel()
        {
            var mainModel = Mock.Of<ICurrentProjectModel>(m => m.ProjectName == "T1-MAIN_PROJECT");
            var altModel = Mock.Of<IProjectModel>(m => m.ProjectName == "T1-ALT_PROJECT");

            typeof(InterprojectExchange.InterprojectExchange).GetField("eProjectManager", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, Mock.Of<IEProjectManager>(m => m.GetModifyingCurrentProjectName() == "T1-MAIN_PROJECT"));

            var interprojectExchange = InterprojectExchange.InterprojectExchange.GetInstance();

            interprojectExchange.AddModel(mainModel);
            interprojectExchange.AddModel(altModel);

            Assert.AreSame(mainModel, interprojectExchange.MainModel);
        }

        [Test]
        public void SelectModel()
        {
            var mainModel = Mock.Of<ICurrentProjectModel>(m => m.ProjectName == "T1-MAIN_PROJECT");
            var altModel = Mock.Of<IProjectModel>(m => m.ProjectName == "T1-ALT_PROJECT");

            typeof(InterprojectExchange.InterprojectExchange).GetField("eProjectManager", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, Mock.Of<IEProjectManager>(m => m.GetModifyingCurrentProjectName() == "T1-MAIN_PROJECT"));

            var interprojectExchange = InterprojectExchange.InterprojectExchange.GetInstance();

            interprojectExchange.AddModel(mainModel);
            interprojectExchange.AddModel(altModel);

            interprojectExchange.SelectModel(altModel);

            Assert.Multiple(() =>
            {
                Mock.Get(altModel).VerifySet(m => m.Selected = true);
                Mock.Get(mainModel).VerifySet(m => m.Selected = false);
                Mock.Get(mainModel).VerifySet(m => m.SelectedAdvancedProject = altModel.ProjectName);
            });
        }

        private static IInterprojectExchange CreateExchangeWithModels(
            out CurrentProjectModel mainModel,
            out AdvancedProjectModel advancedModel,
            out AdvancedProjectModel secondAdvancedModel)
        {
            ResetSingleton();

            typeof(InterprojectExchange.InterprojectExchange)
                .GetField("eProjectManager",
                    BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, Mock.Of<IEProjectManager>(
                    m => m.GetModifyingCurrentProjectName() == "main"));

            var exchange = InterprojectExchange.InterprojectExchange
                .GetInstance();

            mainModel = new CurrentProjectModel
            {
                ProjectName = "main",
                SelectedAdvancedProject = "adv",
                Loaded = true
            };
            advancedModel = new AdvancedProjectModel
            {
                ProjectName = "adv",
                Loaded = true,
                Selected = true
            };
            secondAdvancedModel = new AdvancedProjectModel
            {
                ProjectName = "adv2",
                Loaded = true
            };

            exchange.AddModel(mainModel);
            exchange.AddModel(advancedModel);
            exchange.AddModel(secondAdvancedModel);
            exchange.ChangeEditMode((int)EditMode.SourceReciever);

            return exchange;
        }

        private static void ResetSingleton()
        {
            typeof(InterprojectExchange.InterprojectExchange)
                .GetField("interprojectExchange",
                    BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, null);
        }
    }
}
