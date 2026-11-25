using NUnit.Framework;
using TechObject;
using Editor;
using System.Collections.Generic;
using System;
using System.Linq;
using Moq;
using EplanDevice;
using TechObject.ActionProcessingStrategy;

namespace EasyEplanner.Tests
{
    class StepTest
    {
        private static State RUN  = new State(State.StateType.RUN, null);
        private static State STOP = new State(State.StateType.STOP, null);
        private static State PAUSE = new State(State.StateType.PAUSE, null);
        private static State IDLE = new State(State.StateType.IDLE, null);

        [TestCase(true, new int[] { 0, -1 })]
        [TestCase(false, new int[] { 0, 1 })]
        public void EditablePart_StepFromCaseSource_ReturnsArray(
            bool isMainStep, int[] expectedArray)
        {
            var step = new Step(string.Empty, null, RUN, isMainStep);

            Assert.AreEqual(expectedArray, step.EditablePart);
        }

        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("Name", "Name")]
        [TestCase("Имя", "Имя")]
        public void GetStepName_NewStep_ReturnExpectedName(string defaultName,
            string expectedName)
        {
            var step = new Step(defaultName, null, null);

            var actualName = step.GetStepName();

            Assert.AreEqual(actualName, expectedName);
        }

        static object[] Constructor_NewStep_CheckActionsCountCases =
        {
            new object[] { RUN, true, 15 },
            new object[] { RUN, false, 15 },
            new object[] { STOP, true, 15 },
            new object[] { STOP, false, 15 },
            new object[] { PAUSE, true, 15 },
            new object[] { PAUSE, false, 15 },
            new object[] { IDLE, true, 15 },
            new object[] { IDLE, false, 15 },
        };

        [TestCaseSource(nameof(Constructor_NewStep_CheckActionsCountCases))]
        public void Constructor_NewStep_CheckActionsCount(State state,
            bool isMainStep, int expectedCount)
        {
            var step = new Step(string.Empty, null, state, isMainStep);

            var actualCount = step.GetActions.Count;

            Assert.AreEqual(expectedCount, actualCount);
        }

        private static readonly int NotMainStepActionsCount = 19;
        private static readonly int MainStepActionsCount = 15;
        private static readonly int MainStepActionsWithToStateCount = 15;

        static object[] Constructor_NewStep_CheckItemsCountCases =
        {
            new object[] { RUN, true, MainStepActionsWithToStateCount },
            new object[] { RUN, false, NotMainStepActionsCount },
            new object[] { STOP, true, MainStepActionsCount },
            new object[] { STOP, false, NotMainStepActionsCount },
            new object[] { PAUSE, true,  MainStepActionsWithToStateCount},
            new object[] { PAUSE, false, NotMainStepActionsCount },
            new object[] { IDLE, true, MainStepActionsWithToStateCount },
            new object[] { IDLE, false, NotMainStepActionsCount },
        };

        [TestCaseSource(nameof(Constructor_NewStep_CheckItemsCountCases))]
        public void Constructor_NewStep_CheckItemsCount(State state,
            bool isMainStep, int expectedCount)
        {
            var step = new Step(string.Empty, null, state, isMainStep);

            var actualCount = step.Items.Length;

            Assert.AreEqual(expectedCount, actualCount);
        }

        [TestCaseSource(nameof(CheckActionsSequenceByLuaNameTestSource))]
        public void Constructor_NewStep_CheckActionsSequenceByLuaname(
            bool isMainStep, string[] expectedLuaNames)
        {
            var step = new Step(string.Empty, null, RUN, isMainStep);

            var actions = step.GetActions;

            for (int i = 0; i < actions.Count; i++)
            {
                Assert.AreEqual(expectedLuaNames[i], actions[i].LuaName);
            }
        }

        private static object[] CheckActionsSequenceByLuaNameTestSource()
        {
            var checkedDevices = "checked_devices";
            var openedDevices = "opened_devices";
            var delayOpenedDevices = "delay_opened_devices";
            var openedReverseDevices = "opened_reverse_devices";
            var closedDevices = "closed_devices";
            var delayClosedDevices = "delay_closed_devices";
            var openedUpperSeats = "opened_upper_seat_v";
            var openedLowerSeats = "opened_lower_seat_v";
            var requiredFB = "required_FB";
            var devices = "devices_data";
            var pairsDIDO = "DI_DO";
            var pairsInvertedDIDO = "inverted_DI_DO";
            var pairsAIAO = "AI_AO";
            var enableStepBySignal = "enable_step_by_signal";
            var toStepIfDevicesInSpecificState =
                "jump_if";
            var toStateIfDevicesInSpecificState = "jump_if";

            object[] sequenceIfNoMainStep = new object[]
            {
                false,
                new string[]
                {
                    checkedDevices,
                    openedDevices,
                    delayOpenedDevices,
                    openedReverseDevices,
                    closedDevices,
                    delayClosedDevices,
                    openedUpperSeats,
                    openedLowerSeats,
                    requiredFB,
                    devices,
                    pairsDIDO,
                    pairsInvertedDIDO,
                    pairsAIAO,
                    enableStepBySignal,
                    toStepIfDevicesInSpecificState,
                }
            };

            object[] sequenceIfMainStep = new object[]
            {
                true,
                new string[]
                {
                    checkedDevices,
                    openedDevices,
                    delayOpenedDevices,
                    openedReverseDevices,
                    closedDevices,
                    delayClosedDevices,
                    openedUpperSeats,
                    openedLowerSeats,
                    requiredFB,
                    devices,
                    pairsDIDO,
                    pairsInvertedDIDO,
                    pairsAIAO,
                    enableStepBySignal,
                    toStateIfDevicesInSpecificState,
                }
            };

            return new object[]
            {
                sequenceIfMainStep,
                sequenceIfNoMainStep
            };
        }

        [TestCaseSource(nameof(CheckActionsDrawStyleTestSource))]
        public void Constructor_NewStep_CheckActionsDrawStyle(bool isMainStep,
            DrawInfo.Style[] drawStyles)
        {
            var step = new Step(string.Empty, null, RUN, isMainStep);

            var actions = step.GetActions;

            for (int i = 0; i < actions.Count; i++)
            {
                Assert.AreEqual(actions[i].DrawStyle, drawStyles[i]);
            }
        }

        private static object[] CheckActionsDrawStyleTestSource()
        {
            var greenBox = DrawInfo.Style.GREEN_BOX;
            var redBox = DrawInfo.Style.GRAY_BOX;
            var greenUpBox = DrawInfo.Style.GREEN_UPPER_BOX;
            var greenLowBox = DrawInfo.Style.GREEN_LOWER_BOX;

            DrawInfo.Style checkedDevices = greenBox;
            DrawInfo.Style openedDevices = greenBox;
            DrawInfo.Style delayOpenedDevices = greenBox;
            DrawInfo.Style openedReverseDevices = greenBox;
            DrawInfo.Style closedDevices = redBox;
            DrawInfo.Style delayClosedDevices = redBox;
            DrawInfo.Style openedUpperSeats = greenUpBox;
            DrawInfo.Style openedLowerSeats = greenLowBox;
            DrawInfo.Style requiredFB = greenBox;
            DrawInfo.Style devices = greenBox;
            DrawInfo.Style pairsDIDO = greenBox;
            DrawInfo.Style pairsInvertedDIDO = greenBox;
            DrawInfo.Style pairsAIAO = greenBox;
            DrawInfo.Style enableStepBySignal = greenBox;
            DrawInfo.Style toStepByCondition = greenBox;
            DrawInfo.Style toStateIfDevicesInSpecificState = greenBox;

            var noMainStep = new object[]
            {
                false,
                new DrawInfo.Style[]
                {
                    checkedDevices,
                    openedDevices,
                    delayOpenedDevices,
                    openedReverseDevices,
                    closedDevices,
                    delayClosedDevices,
                    openedUpperSeats,
                    openedLowerSeats,
                    requiredFB,
                    devices,
                    pairsDIDO,
                    pairsInvertedDIDO,
                    pairsAIAO,
                    enableStepBySignal,
                    toStepByCondition,
                }
            };

            var mainStep = new object[]
            {
                true,
                new DrawInfo.Style[]
                {
                    checkedDevices,
                    openedDevices,
                    delayOpenedDevices,
                    openedReverseDevices,
                    closedDevices,
                    delayClosedDevices,
                    openedUpperSeats,
                    openedLowerSeats,
                    requiredFB,
                    devices,
                    pairsDIDO,
                    pairsInvertedDIDO,
                    pairsAIAO,
                    enableStepBySignal,
                    toStateIfDevicesInSpecificState,
                }
            };

            return new object[]
            {
                noMainStep,
                mainStep
            };
        }

        [TestCaseSource(nameof(CheckActionsAllowedDevTypes))]
        public void Constructor_NewStep_CheckActionsAllowedDevTypes(
            bool isMainStep, List<EplanDevice.DeviceType[]> devTypesList)
        {
            var step = new Step(string.Empty, null, RUN, isMainStep);

            var actions = step.GetActions;

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                action.GetDisplayObjects(out EplanDevice.DeviceType[] actualDevTypes,
                    out _, out _);
                EplanDevice.DeviceType[] expectedDevTypes = devTypesList[i];

                Assert.AreEqual(expectedDevTypes, actualDevTypes);
            }
        }

        private static object[] CheckActionsAllowedDevTypes()
        {
            // null - allowed all devices in action.
            // But action can contain other actions and it means, that main
            // action can't be changed itself, inner can. Null for main if u
            // have inner = it's ok.
            EplanDevice.DeviceType[] allTypesAllowed = null;

            var openDevice = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.V,
                EplanDevice.DeviceType.DO,
                EplanDevice.DeviceType.M
            };

            var delayOpenDevice = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.V,
                EplanDevice.DeviceType.DO,
                EplanDevice.DeviceType.M
            };

            var openReverse = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.M
            };

            var closeDevice = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.V,
                EplanDevice.DeviceType.DO,
                EplanDevice.DeviceType.M
            };

            var delayCloseDevice = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.V,
                EplanDevice.DeviceType.DO,
                EplanDevice.DeviceType.M
            };

            var valveSeats = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.V,
                EplanDevice.DeviceType.DO
            };
            EplanDevice.DeviceType[] openUpperSeats = valveSeats;
            EplanDevice.DeviceType[] openLowerSeats = valveSeats;

            var requiredFb = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.DI,
                EplanDevice.DeviceType.GS
            };

            var groupDiDo = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.DI,
                EplanDevice.DeviceType.SB,
                EplanDevice.DeviceType.DO,
                EplanDevice.DeviceType.HL,
                EplanDevice.DeviceType.GS,
                EplanDevice.DeviceType.LS,
                EplanDevice.DeviceType.FS
            };

            var groupInvertedDIDO = groupDiDo;

            var groupAiAo = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.AI,
                EplanDevice.DeviceType.AO,
                EplanDevice.DeviceType.M,
                EplanDevice.DeviceType.PT,
                EplanDevice.DeviceType.LT,
                EplanDevice.DeviceType.FQT,
                EplanDevice.DeviceType.QT,
                EplanDevice.DeviceType.TE,
                EplanDevice.DeviceType.VC
            };

            var enableStepBySignal = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.DI,
                EplanDevice.DeviceType.LS,
                EplanDevice.DeviceType.GS,
                EplanDevice.DeviceType.SB,
                EplanDevice.DeviceType.FS,
                EplanDevice.DeviceType.TS,
                EplanDevice.DeviceType.PDS,
            };

            var byCondition = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.V,
                EplanDevice.DeviceType.GS,
                EplanDevice.DeviceType.DI,
                EplanDevice.DeviceType.DO,
                EplanDevice.DeviceType.SB,
                EplanDevice.DeviceType.LS,
            };

            var mainStep = new object[]
            {
                true,
                new List<EplanDevice.DeviceType[]>()
                {
                    allTypesAllowed,
                    openDevice,
                    delayOpenDevice,
                    openReverse,
                    closeDevice,
                    delayCloseDevice,
                    openUpperSeats,
                    openLowerSeats,
                    requiredFb,
                    allTypesAllowed,
                    groupDiDo,
                    groupInvertedDIDO,
                    groupAiAo,
                    enableStepBySignal,
                    byCondition,
                }
            };

            var noMainStep = new object[]
            {
                true,
                new List<EplanDevice.DeviceType[]>()
                {
                    allTypesAllowed,
                    openDevice,
                    delayOpenDevice,
                    openReverse,
                    closeDevice,
                    delayCloseDevice,
                    openUpperSeats,
                    openLowerSeats,
                    requiredFb,
                    allTypesAllowed,
                    groupDiDo,
                    groupInvertedDIDO,
                    groupAiAo,
                    enableStepBySignal,
                    byCondition,
                }
            };

            return new object[]
            {
                mainStep,
                noMainStep
            };
        }

        [TestCaseSource(nameof(CheckActionsAllowedDevSubTypesSource))]
        public void Constructor_NewStep_CheckActionsAllowedDevSubTypes(
            bool isMainStep, List<EplanDevice.DeviceSubType[]> devSubTypesList)
        {
            var step = new Step(string.Empty, null, RUN, isMainStep);

            var actions = step.GetActions;

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                action.GetDisplayObjects(out _,
                    out EplanDevice.DeviceSubType[] actualDevSubTypes, out _);
                EplanDevice.DeviceSubType[] expectedDevTypes = devSubTypesList[i];

                Assert.AreEqual(expectedDevTypes, actualDevSubTypes);
            }
        }

        private static object[] CheckActionsAllowedDevSubTypesSource()
        {
            // null - it means, that we can use any subtype from defined types.
            EplanDevice.DeviceSubType[] allSubTypesAllowed = null;

            var openReverse = new EplanDevice.DeviceSubType[]
            {
                EplanDevice.DeviceSubType.M_REV_FREQ,
                EplanDevice.DeviceSubType.M_REV_FREQ_2,
                EplanDevice.DeviceSubType.M_REV_FREQ_2_ERROR,
                EplanDevice.DeviceSubType.M_ATV,
                EplanDevice.DeviceSubType.M_ATV_LINEAR,
                EplanDevice.DeviceSubType.M,
                EplanDevice.DeviceSubType.M_VIRT,
            };

            var openUpperSeats = new EplanDevice.DeviceSubType[]
            {
                EplanDevice.DeviceSubType.V_MIXPROOF,
                EplanDevice.DeviceSubType.V_AS_MIXPROOF,
                EplanDevice.DeviceSubType.V_IOLINK_MIXPROOF,
                EplanDevice.DeviceSubType.V_IOL_TERMINAL_MIXPROOF_DO3,
                EplanDevice.DeviceSubType.V_VIRT,
                EplanDevice.DeviceSubType.DO,
                EplanDevice.DeviceSubType.DO_VIRT
            };
            var openLowerSeats = new EplanDevice.DeviceSubType[]
            {
                EplanDevice.DeviceSubType.V_MIXPROOF,
                EplanDevice.DeviceSubType.V_AS_MIXPROOF,
                EplanDevice.DeviceSubType.V_IOLINK_MIXPROOF,
                EplanDevice.DeviceSubType.V_MINI_FLUSHING,
                EplanDevice.DeviceSubType.V_IOL_TERMINAL_MIXPROOF_DO3,
                EplanDevice.DeviceSubType.V_VIRT,
                EplanDevice.DeviceSubType.DO,
                EplanDevice.DeviceSubType.DO_VIRT
            };
            var mainStep = new object[]
            {
                true,
                new List<EplanDevice.DeviceSubType[]>()
                {
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    openReverse,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    openUpperSeats,
                    openLowerSeats,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                }
            };

            var noMainStep = new object[]
            {
                true,
                new List<EplanDevice.DeviceSubType[]>()
                {
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    openReverse,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    openUpperSeats,
                    openLowerSeats,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed,
                    allSubTypesAllowed
                }
            };

            return new object[]
            {
                mainStep,
                noMainStep
            };
        }

        [TestCaseSource(nameof(ImageIndexInActionsTest))]
        public void ImageIndex_SetOfActions_ReturnCorrectImageIndexSequence(
            bool isMainStep, List<ImageIndexEnum> expectedImageIndexes)
        {
            var step = new Step(string.Empty, null, RUN, isMainStep);

            var actualImageIndexes = new List<ImageIndexEnum>();
            foreach (var action in step.GetActions)
            {
                var treeViewItem = (ITreeViewItem)action;
                ImageIndexEnum imageIndex = treeViewItem.ImageIndex;
                actualImageIndexes.Add(imageIndex);
            }

            Assert.AreEqual(expectedImageIndexes, actualImageIndexes);
        }

        private static object[] ImageIndexInActionsTest()
        {
            var checkedDevices = ImageIndexEnum.NONE;
            var openedDevices = ImageIndexEnum.ActionON;
            var delayOpenedDevices = ImageIndexEnum.NONE;
            var openedReverseDevices = ImageIndexEnum.NONE;
            var closedDevices = ImageIndexEnum.ActionOFF;
            var delayClosedDevices = ImageIndexEnum.NONE;
            var openedUpperSeats = ImageIndexEnum.ActionWashUpperSeats;
            var openedLowerSeats = ImageIndexEnum.ActionWashLowerSeats;
            var requiredFB = ImageIndexEnum.ActionSignals;
            var devices = ImageIndexEnum.ActionWash;
            var pairsDIDO = ImageIndexEnum.ActionDIDOPairs;
            var pairsInvertedDIDO = pairsDIDO;
            var pairsAIAO = ImageIndexEnum.ActionDIDOPairs;
            var toStepIfDevicesInSpecificState = ImageIndexEnum.NONE;
            var enableStepBySignal = ImageIndexEnum.NONE;
            var toStateIfDevicesInSpecificState = ImageIndexEnum.NONE;

            object[] notMainStepImageIndexes = new object[]
            {
                false,
                new List<ImageIndexEnum>()
                {
                    checkedDevices,
                    openedDevices,
                    delayOpenedDevices,
                    openedReverseDevices,
                    closedDevices,
                    delayClosedDevices,
                    openedUpperSeats,
                    openedLowerSeats,
                    requiredFB,
                    devices,
                    pairsDIDO,
                    pairsInvertedDIDO,
                    pairsAIAO,
                    enableStepBySignal,
                    toStepIfDevicesInSpecificState
                }
            };

            object[] mainStepImageIndexes = new object[]
            {
                true,
                new List<ImageIndexEnum>()
                {
                    checkedDevices,
                    openedDevices,
                    delayOpenedDevices,
                    openedReverseDevices,
                    closedDevices,
                    delayClosedDevices,
                    openedUpperSeats,
                    openedLowerSeats,
                    requiredFB,
                    devices,
                    pairsDIDO,
                    pairsInvertedDIDO,
                    pairsAIAO,
                    enableStepBySignal,
                    toStateIfDevicesInSpecificState,
                }
            };

            return new object[]
            {
                mainStepImageIndexes,
                notMainStepImageIndexes
            };
        }

        [TestCaseSource(nameof(ActionsTypesSequenceTest))]
        public void Constructor_NewStep_ReturnsCorrectTypesSequence(
            bool isMainStep, Type[] expectedTypes)
        {
            var step = new Step(string.Empty, null, RUN, isMainStep);

            var actualTypes = new List<Type>();
            foreach (var action in step.GetActions)
            {
                actualTypes.Add(action.GetType());
            }

            Assert.AreEqual(expectedTypes, actualTypes.ToArray());
        }

        public static object[] ActionsTypesSequenceTest()
        {
            Type action = typeof(TechObject.Action);
            Type actionGroup = typeof(ActionGroup);
            Type actionGroupWash = typeof(ActionGroupWash);
            Type actionToStepByCondition = typeof(ActionGroupCustom);
            Type actionGroupCustom = typeof(ActionGroupCustom);

            Type checkedDevices = action;
            Type openedDevices = action;
            Type openedReverseDevices = action;
            Type closedDevices = action;
            Type openedUpperSeats = actionGroup;
            Type openedLowerSeats = actionGroup;
            Type requiredFB = action;
            Type devices = actionGroupWash;
            Type pairsDIDO = actionGroupCustom;
            Type pairsInvertedDIDO = actionGroupCustom;
            Type pairsAIAO = actionGroup;
            Type enableStepBySignal = actionGroupCustom;

            var mainStepSequence = new Type[]
            {
                checkedDevices,
                openedDevices,
                actionGroupCustom,
                openedReverseDevices,
                closedDevices,
                actionGroupCustom,
                openedUpperSeats,
                openedLowerSeats,
                requiredFB,
                devices,
                pairsDIDO,
                pairsInvertedDIDO,
                pairsAIAO,
                enableStepBySignal,
                actionGroupCustom,
            };

            var notMainStepSequence = new Type[]
            {
                checkedDevices,
                openedDevices,
                actionGroupCustom,
                openedReverseDevices,
                closedDevices,
                actionGroupCustom,
                openedUpperSeats,
                openedLowerSeats,
                requiredFB,
                devices,
                pairsDIDO,
                pairsInvertedDIDO,
                pairsAIAO,
                enableStepBySignal,
                actionToStepByCondition
            };

            return new object[]
            {
                new object[] { true, mainStepSequence },
                new object[] { false, notMainStepSequence },
            };
        }

        [Test]
        public void UpdateOnGenericTechObject()
        {
            var step = new Step("Шаг", GetN => 1, null);
            var genericStep = new Step("Шаг 1", GetN => 1, null);

            var techObject = new TechObject.TechObject("", GetN => 1, 1, 2, "", -1, "", "", null);

            techObject.GetParamsManager().Float.Insert();
            techObject.GetParamsManager().Float.Insert();

            step.AddParent(techObject);
            genericStep.AddParent(techObject);

            var genericActionMock = new Mock<IAction>();
            genericActionMock.Setup(a => a.Empty).Returns(false);

            var deviceManagerMock = new Mock<IDeviceManager>();

            genericStep.GetActions[0] = genericActionMock.Object;
            
            genericStep.SetPar(2, 3, 1);


            var actionMock = new Mock<IAction>();
            actionMock.Setup(a => a.UpdateOnGenericTechObject(It.IsAny<IAction>()))
                .Callback<IAction>((action) => Assert.AreSame(genericActionMock.Object, action));
            actionMock.Setup(a => a.UpdateOnGenericTechObject(null))
                .Callback<IAction>((action) => Assert.IsNull(action));

            step.GetActions[0] = actionMock.Object;

            Assert.Multiple(() =>
            {
                step.UpdateOnGenericTechObject(genericStep);

                step.GetActions.RemoveRange(1, step.GetActions.Count - 1);

                step.UpdateOnGenericTechObject(null);

                Assert.AreEqual("2", step.TimeParam);
                Assert.AreEqual("3", step.NextStepN);
                Assert.AreEqual("1", step.MaxDurationParam);
            });
        }

        [Test]
        public void GetBaseObjectList()
        {
            var mode = new Mode("", getN => 1, null, new BaseOperation("", "", new List<BaseParameter>() { },
                new Dictionary<string, List<BaseStep>>() 
                {
                    { "RUN", new List<BaseStep>() { new BaseStep("", ""), new BaseStep("шаг 1", "step_1"), new BaseStep("шаг 2", "step_2") } }
                }));
            var state = new State(State.StateType.RUN, mode);
            var step = state.AddStep("", "");


            Assert.Multiple(() =>
            {
                CollectionAssert.AreEqual(new List<string>() { "", "шаг 1", "шаг 2" }, step.BaseObjectsList);
                
                state.AddStep("шаг 1", "step_1");
                CollectionAssert.AreEqual(new List<string>() { "", "шаг 2" }, step.BaseObjectsList);
            });

        }

        [Test]
        public void Replace_Null()
        {
            var step = new Step("", getN => 1, null);

            Assert.IsNull(step.Replace(null, null));
        }

        [Test]
        public void Replace_DifferentActions()
        {
            var step = new Step("", getN => 1, null);

            Assert.IsNull(step.Replace(Mock.Of<IAction>(a => a.LuaName == "LuaName_1"), Mock.Of<IAction>(a => a.LuaName == "LuaName_2")));
        }


        [Test]
        public void GetObjectsToDraw_RED_BOX()
        {
            var dev = Mock.Of<IDevice>(d => d.Name == "DEV1");

            var actions = new List<IAction>() 
            {
                Mock.Of<IAction>(a => a.GetObjectToDrawOnEplanPage() == new List<DrawInfo>() { new DrawInfo(DrawInfo.Style.GREEN_BOX, dev) { Action = DrawInfo.ActionType.ON_DEVICE} }),
                Mock.Of<IAction>(a => a.GetObjectToDrawOnEplanPage() == new List<DrawInfo>() { new DrawInfo(DrawInfo.Style.GRAY_BOX, dev) { Action = DrawInfo.ActionType.OFF_DEVICE} }),
                Mock.Of<IAction>(a => a.GetObjectToDrawOnEplanPage() == new List<DrawInfo>() { new DrawInfo(DrawInfo.Style.GRAY_BOX, dev) { Action = DrawInfo.ActionType.OTHER} }),
            };

            var step = new Step("", getN => 1, null);
            typeof(Step)
                .GetField("actions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(step, actions);

            var draw = step.GetObjectToDrawOnEplanPage();

            Assert.AreEqual(1, draw.Count);
            Assert.AreEqual(DrawInfo.Style.RED_BOX, draw[0].DrawingStyle);
        }

        [Test]
        public void GetObjectsToDraw_GREEN_GRAY_BOX()
        {
            var dev = Mock.Of<IDevice>(d => d.Name == "DEV1");

            var actions = new List<IAction>()
            {
                Mock.Of<IAction>(a => a.GetObjectToDrawOnEplanPage() == new List<DrawInfo>() { new DrawInfo(DrawInfo.Style.GREEN_BOX, dev) { Action = DrawInfo.ActionType.ON_DEVICE} }),
                Mock.Of<IAction>(a => a.GetObjectToDrawOnEplanPage() == new List<DrawInfo>() { new DrawInfo(DrawInfo.Style.GRAY_BOX, dev) { Action = DrawInfo.ActionType.OTHER} }),
                Mock.Of<IAction>(a => a.GetObjectToDrawOnEplanPage() == new List<DrawInfo>() { new DrawInfo(DrawInfo.Style.GRAY_BOX, dev) { Action = DrawInfo.ActionType.OTHER} }),
            };

            var step = new Step("", getN => 1, null);
            typeof(Step)
                .GetField("actions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(step, actions);

            var draw = step.GetObjectToDrawOnEplanPage();

            Assert.AreEqual(1, draw.Count);
            Assert.AreEqual(DrawInfo.Style.GREEN_GRAY_BOX, draw[0].DrawingStyle);
        }

        [Test]
        public void GetObjectsToDraw_GREEN_BOX()
        {
            var dev = Mock.Of<IDevice>(d => d.Name == "DEV1");

            var actions = new List<IAction>()
            {
                Mock.Of<IAction>(a => a.GetObjectToDrawOnEplanPage() == new List<DrawInfo>() { new DrawInfo(DrawInfo.Style.GREEN_BOX, dev) { Action = DrawInfo.ActionType.ON_DEVICE} }),
                Mock.Of<IAction>(a => a.GetObjectToDrawOnEplanPage() == new List<DrawInfo>() { new DrawInfo(DrawInfo.Style.GREEN_BOX, dev) { Action = DrawInfo.ActionType.OTHER} }),
                Mock.Of<IAction>(a => a.GetObjectToDrawOnEplanPage() == new List<DrawInfo>() { new DrawInfo(DrawInfo.Style.NO_DRAW, dev) { Action = DrawInfo.ActionType.OTHER} }),
            };

            var step = new Step("", getN => 1, null);
            typeof(Step)
                .GetField("actions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(step, actions);

            var draw = step.GetObjectToDrawOnEplanPage();

            Assert.AreEqual(1, draw.Count);
            Assert.AreEqual(DrawInfo.Style.GREEN_BOX, draw[0].DrawingStyle);
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void Check_ExistingNextStepNumber_ReturnsNoError(int nextStepNumber)
        {
            var step = CreateStepForCheck();
            step.SetPar(1, nextStepNumber, 1);

            var errors = step.Check();

            StringAssert.DoesNotContain("Номер следующего шага", errors);
        }

        [Test]
        public void Check_NonExistingNextStepNumber_ReturnsError()
        {
            var step = CreateStepForCheck();
            step.SetPar(1, 2, 1);

            var errors = step.Check();

            StringAssert.Contains("Номер следующего шага", errors);
            StringAssert.Contains("- \"2\".", errors);
            StringAssert.Contains(
                "\"1. Объект №1 (#0)\" -> операция \"Операция\" -> " +
                "шаг \"Шаг 1\" -> св-во \"Номер следующего шага\"",
                errors);
        }

        [Test]
        public void Check_CurrentStepAsNextStep_ReturnsError()
        {
            var step = CreateStepForCheck();
            step.SetPar(1, 0, 1);

            var errors = step.Check();

            StringAssert.Contains("текущий шаг недопустим", errors);
            StringAssert.Contains("- \"0\".", errors);
        }

        [Test]
        public void Check_NonNumericNextStepNumber_ReturnsError()
        {
            var step = CreateStepForCheck();
            var nextStepNumber = (ObjectProperty)typeof(Step)
                .GetField("nextStepN", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(step);
            nextStepNumber.SetValue("abc");

            var errors = step.Check();

            StringAssert.Contains("Номер следующего шага", errors);
            StringAssert.Contains("- \"abc\".", errors);
        }

        private static Step CreateStepForCheck()
        {
            var techObject = new TechObject.TechObject("Объект", getN => 1,
                1, 2, "TANK", -1, "", "", null);
            var mode = new Mode("Операция", getN => 1,
                techObject.ModesManager);
            var state = new State(State.StateType.RUN, mode, true);
            state.AddStep("Шаг 1", string.Empty);
            state.AddStep("Шаг 2", string.Empty);

            return state.Steps[1];
        }

        [Test]
        public void CheckInOutGroupActions()
        {
            var dev = Mock.Of<IDevice>(d => d.Name == "DEV1");

            var actions = new List<IAction>()
            {
                Mock.Of<IAction>(a =>
                    a.Name == "Группы AI -> AO AO ..." &&
                    a.SubActions == new List<IAction>()
                    {
                        Mock.Of<IAction>(sa => sa.Empty == true),

                        Mock.Of<IAction>(sa =>
                            sa.GetDeviceProcessingStrategy() == Mock.Of<IDeviceProcessingStrategy>(ps =>
                                ps.Check(It.IsAny<IDeviceManager>()) == "error 1\n")),

                        Mock.Of<IAction>(sa =>
                            sa.GetDeviceProcessingStrategy() == Mock.Of<IDeviceProcessingStrategy>(ps =>
                                ps.Check(It.IsAny<IDeviceManager>()) == "error 2\n"))
                    }
                ),
            };

            var step = new Step("", getN => 1, null);
            
            typeof(Step)
                .GetField("actions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(step, actions);

            var error = typeof(Step)
                .GetMethod("CheckInOutGroupActions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(step, null);

            Assert.AreEqual("error 1\nerror 2\n", error);
        }

        [Test]
        public void Clone_ActionsOwnerPointsToClonedTechObject()
        {
            var baseTechObject = new BaseTechObject();
            var sourceTechObject = new TechObject.TechObject("Танк", getN => 1, 1, 2,
                "CREAM_TANK", -1, "", "", baseTechObject);
            var targetTechObject = new TechObject.TechObject("Танк", getN => 2, 3, 2,
                "CREAM_TANK", -1, "", "", baseTechObject);

            var sourceMode = new Mode("Операция", getN => 1, sourceTechObject.ModesManager);
            var targetMode = new Mode("Операция", getN => 1, targetTechObject.ModesManager);
            var sourceState = new State(State.StateType.RUN, sourceMode, true);
            var targetState = new State(State.StateType.RUN, targetMode, true);

            var sourceStep = sourceState.Steps[0];
            var clonedStep = sourceStep.Clone(targetState, getN => 1);
            var openDevicesAction = clonedStep.GetActions
                .First(action => action.LuaName == "opened_devices");

            Assert.Multiple(() =>
            {
                Assert.AreSame(targetTechObject, openDevicesAction.Owner.TechObject);
                Assert.AreNotSame(sourceTechObject, openDevicesAction.Owner.TechObject);
                Assert.AreSame(clonedStep, openDevicesAction.Owner);
                Assert.AreEqual(3, openDevicesAction.Owner.TechObject.TechNumber);
            });
        }
    }
}
