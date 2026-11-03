using System.Linq;
using TechObject;
using NUnit.Framework;
using Moq;
using Editor;
using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;

namespace EasyEplanner.Tests
{
    class StateTest
    {
        [TestCase(false, new string[] { "st1", "st2", "st3" }, 4)]
        [TestCase(true, new string[] { "st1", "st2", "st3" }, 4)]
        public void AddStep_EmptyState_AddNewStepsFirstMain(bool needMainStep,
            string[] stepNames, int expectedStepsCount)
        {
            var state = new State(State.StateType.STOP, null, needMainStep);

            foreach (var stepName in stepNames)
            {
                state.AddStep(stepName, string.Empty);
            }

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expectedStepsCount, state.Steps.Count);
                Assert.IsTrue(state.Steps.First().GetStepName()
                    .Equals(Step.MainStepName));
                foreach (var stepName in stepNames)
                {
                    Assert.IsTrue(state.Steps
                        .Any(x => x.GetStepName() == stepName));
                }
            });
        }

        [Test]
        public void UpdateOnGenericTechObject()
        {
            bool setNewValueMethodCalled = false;
            bool setNewBaseStepMethodCalled = false;

            string baseStep = "BaseStep";

            var mode = new Mode("operation", GetN => 1, null,
                new BaseOperation("baseOperation", "baseOperation", new List<BaseParameter>() { },
                new Dictionary<string, List<BaseStep>>()
                {
                    { "IDLE", new List<BaseStep>() { new BaseStep(baseStep, baseStep) } },
                }));


            var state = new State(State.StateType.IDLE, mode);
            var genericState = new State(State.StateType.IDLE, mode);

            genericState.AddStep("Шаг 1", "Step 1");
            genericState.AddStep("Шаг 2", "Step 2");

            genericState.Steps[1].SetNewValue(baseStep, true);

            var stepMock = new Mock<Step>("Шаг 1", new GetN((getN) => 1), state, false);
            stepMock.Setup(x => x.SetNewValue(It.IsAny<string>()))
                .Callback<string>((newValue) =>
                {
                    Assert.AreEqual("Шаг 1", newValue);
                    setNewValueMethodCalled = true;
                });
            stepMock.Setup(x => x.SetNewValue(It.IsAny<string>(), true))
                .Callback<string, bool>((newValue, _) =>
                {
                    Assert.AreEqual(baseStep, newValue);
                    setNewBaseStepMethodCalled = true;
                });


            state.AddStep("Шаг 1", "Step 1");

            state.Steps[1] = stepMock.Object;

            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, state.Steps.Count);
                state.UpdateOnGenericTechObject(genericState);
                Assert.AreEqual(3, state.Steps.Count);
                Assert.IsTrue(setNewValueMethodCalled);
                Assert.IsTrue(setNewBaseStepMethodCalled);
            });
        }

        [Test]
        public void UpdateOnGenericTechObject_NullValue()
        {
            bool stepUpdateNullCalled = false;

            var state = new State(State.StateType.IDLE, null);
            state.AddStep("", "");

            var stepMock = new Mock<Step>("Шаг 1", new GetN((getN) => 1), state, false);
            stepMock.Setup(s => s.UpdateOnGenericTechObject(null))
                .Callback<ITreeViewItem>((_) => { stepUpdateNullCalled = true; });

            state.Steps[1] = stepMock.Object;

            state.UpdateOnGenericTechObject(null);

            Assert.IsTrue(stepUpdateNullCalled);
        }

        [Test]
        public void Move()
        {
            var state = new State(State.StateType.IDLE, null, true);
            var step1 = state.Insert();
            var step2 = state.Insert();

            Assert.Multiple(() =>
            {
                Assert.AreSame(step2, state.MoveUp(step2));
                Assert.IsNull(state.MoveUp(step2));
                Assert.IsNull(state.MoveUp(0));

                Assert.AreSame(step2, state.MoveDown(step2));
                Assert.IsNull(state.MoveDown(step2));
                Assert.IsNull(state.MoveDown(0));
            });
        }

        [Test]
        public void InsertCopyAndReplace()
        {
            var techObject = new TechObject.TechObject("", getN => 1, 1, 2, "TANK", -1, "", "", null);
            var baseOperation = new BaseOperation(
                    "операция", "operation",
                    new List<BaseParameter>() { },
                    new Dictionary<string, List<BaseStep>>()
                    {
                        { "RUN", new List<BaseStep>() { new BaseStep("", ""), new BaseStep("шаг_1", "step_1"), new BaseStep("шаг_2", "step_2") } },
                    }
                );

            var operation = Mock.Of<IMode>(m =>
                m.TechObject == techObject &&
                m.BaseOperation == baseOperation);


            var state = new State(State.StateType.RUN, operation, true);

            var copied_step_1 = new Step("шаг_1", getN => 1, state);
            copied_step_1.SetNewValue("step_1", false);
            var copied_step_2 = new Step("шаг_2", getN => 1, state);
            copied_step_2.SetNewValue("step_2", false);
            var copied_step_no_base = new Step("шаг", getN => 1, state);

            Assert.Multiple(() =>
            {
                // Вставка базового шага 1
                var insertedStep_1 = state.InsertCopy(copied_step_1) as Step;
                Assert.IsNotNull(insertedStep_1);
                Assert.AreNotSame(copied_step_1, insertedStep_1);
                Assert.AreEqual(copied_step_1.GetBaseStepLuaName(), insertedStep_1.GetBaseStepLuaName());

                // Повторная вставка базового шага 1
                insertedStep_1 = state.InsertCopy(copied_step_1) as Step;
                Assert.IsNotNull(insertedStep_1);
                Assert.AreNotSame(copied_step_1, insertedStep_1);
                Assert.AreEqual(string.Empty, insertedStep_1.GetBaseStepLuaName());

                // no step
                var insertedStep_null = state.InsertCopy(null) as Step;
                Assert.IsNull(insertedStep_null);

                // Вставка не базового шага
                var insertedStep_no_base = state.InsertCopy(copied_step_no_base) as Step;
                Assert.IsNotNull(insertedStep_no_base);
                Assert.AreNotSame(copied_step_no_base, insertedStep_no_base);
                Assert.AreEqual(copied_step_no_base.GetBaseStepLuaName(), insertedStep_no_base.GetBaseStepLuaName());

                // Замена не базового шага базовым шагом 2
                var replacedStep_2 = state.Replace(insertedStep_no_base, copied_step_2) as Step;
                Assert.IsNotNull(replacedStep_2);
                Assert.AreNotSame(copied_step_2, replacedStep_2);
                Assert.AreEqual(copied_step_2.GetBaseStepLuaName(), replacedStep_2.GetBaseStepLuaName());

                // Замена предыдущего шага на уже имеющийся шаг 1
                var replacedStep_1 = state.Replace(replacedStep_2, copied_step_1) as Step;
                Assert.IsNotNull(replacedStep_1);
                Assert.AreNotSame(copied_step_1, replacedStep_1);
                Assert.AreEqual(string.Empty, replacedStep_1.GetBaseStepLuaName());

                // no step
                var replacedStep_null = state.Replace(null, null) as Step;
                Assert.IsNull(replacedStep_null);
            });
        }


        [TestCase(State.StateType.IDLE,State.StateType.RUN)]
        [TestCase(State.StateType.STARTING,State.StateType.RUN, State.StateType.STOP)]
        [TestCase(State.StateType.RUN,State.StateType.COMPLETE, State.StateType.PAUSE, State.StateType.STOP)]
        [TestCase(State.StateType.COMPLETING,State.StateType.COMPLETE, State.StateType.STOP)]
        [TestCase(State.StateType.COMPLETE,State.StateType.IDLE)]
        [TestCase(State.StateType.PAUSING,State.StateType.PAUSE, State.StateType.STOP)]
        [TestCase(State.StateType.PAUSE,State.StateType.RUN, State.StateType.STOP)]
        [TestCase(State.StateType.UNPAUSING,State.StateType.RUN, State.StateType.STOP)]
        [TestCase(State.StateType.STOPPING,State.StateType.STOP)]
        [TestCase(State.StateType.STOP,State.StateType.IDLE)]
        public void TransitionsTest(State.StateType state, params State.StateType[] expectedMap)
        {
            CollectionAssert.AreEqual(expectedMap, state.StateTransition());
        }

        [Test]
        public void SetNewValue()
        {
            var modesManager = new ModesManager(null);
            var mode = Mock.Of<IMode>(m => m.Owner == modesManager);
            var RUN = new State(State.StateType.RUN, mode, true);
            var IDLE = new State(State.StateType.IDLE, mode, true);

            Assert.Multiple(() =>
            {
                RUN.SetNewValue(State.RUNPOINT, true);
                Assert.AreSame(RUN, modesManager.RunPointState);

                IDLE.SetNewValue(State.RUNPOINT, true);
                Assert.AreSame(IDLE, modesManager.RunPointState);
                Assert.IsFalse(RUN.IsRunPoint);

                IDLE.SetNewValue("", true);
                Assert.IsNull(modesManager.RunPointState);
                Assert.IsFalse(IDLE.IsRunPoint);
            });
        }

        [Test]
        public void TestProperties()
        {
            var modesManager = new ModesManager(null);
            var mode = Mock.Of<IMode>(m => m.Owner == modesManager);
            var RUN = new State(State.StateType.RUN, mode, true);

            RUN.SetNewValue(State.RUNPOINT, true);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(RUN.IsEditable);
                Assert.IsTrue(RUN.IsBoolParameter);

                CollectionAssert.AreEqual(new string[] { State.RUNPOINT, "" }, RUN.BaseObjectsList);
                CollectionAssert.AreEqual(new int[] { -1, 1 }, RUN.EditablePart);
                CollectionAssert.AreEqual(new string[] { $"{RUN.Name}", State.RUNPOINT}, RUN.DisplayText);

                Assert.AreEqual(ImageIndexEnum.Run, RUN.DescriptionImageIndex);
                Assert.AreEqual(ImageIndexEnum.ModesManager, modesManager.ImageIndex);
                Assert.AreEqual(ImageIndexEnum.Run, modesManager.DescriptionImageIndex);
            });
        }
    }
}
