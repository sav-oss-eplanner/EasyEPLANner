using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechObject;
using NUnit.Framework;
using Moq;
using System.Threading;
using Editor;
using System.Windows.Forms;
using EplanDevice;
using Tests.TechObject;
using NUnit.Framework.Constraints;

namespace TechObjectTests
{
    public class BaseObjectTest
    {
        [Test]
        public void AddObjectWhenLoadFromLua()
        {
            var baseTechObject = new BaseTechObject();
            baseTechObject.EplanName = "BaseTechObjectName";

            var genericTechObject = new GenericTechObject("", 2, "", -1, "", string.Empty, baseTechObject);


            var techObjects = new List<TechObject.TechObject>();

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);
            techObjectManagerMock.Setup(obj => obj.GetGenericTObject(It.IsAny<int>()))
                .Returns((GenericTechObject)null);
            techObjectManagerMock.Setup(obj => obj.GetGenericTObject(1))
                .Returns(genericTechObject);

            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);
            genericTechObject.GenericGroup = new GenericGroup(genericTechObject, baseObject, techObjectManagerMock.Object);

            var techObject = new TechObject.TechObject("", GetN => 1, 1, 2, "TANK", -1, "", string.Empty, baseTechObject);

            Assert.Multiple(() =>
            {
                baseObject.AddObjectWhenLoadFromLua(techObject, 1);
                Assert.AreEqual(techObject, genericTechObject.GenericGroup.InheritedTechObjects.First());

                baseObject.AddObjectWhenLoadFromLua(techObject, 2);
                Assert.IsTrue(baseObject.TechObjects.Contains(techObject));
            });
        }

        [Test]
        public void AddGenericObjectWhenLoadFromLua()
        {
            var baseTechObject = new BaseTechObject();
            baseTechObject.EplanName = "BaseTechObjectName";

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(new List<TechObject.TechObject>());
            techObjectManagerMock.Setup(obj => obj.GetGenericTObject(It.IsAny<int>()))
                .Returns((GenericTechObject)null);

            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);

            var genericTechObject = new GenericTechObject("", 2, "", -1, "", string.Empty, baseTechObject);
            baseObject.AddGenericObjectWhenLoadFromLua(genericTechObject);

            Assert.IsTrue(baseObject.GenericGroups.Any());
        }

        [Test]
        public void GetItems()
        {
            var techObjects = new List<TechObject.TechObject>()
            {
                new TechObject.TechObject("", GetN => 1, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject())
            };

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);

            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);
            baseObject.AddObjectWhenLoadFromLua(techObjects[0], 0);

            Assert.IsTrue(baseObject.Items.Any());
        }

        [Test]
        public void AddObject()
        {
            var techObjects = new List<TechObject.TechObject>();

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);

            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);

            var techObject = new TechObject.TechObject("", GetN => 1, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());

            baseObject.AddObject(techObject);

            Assert.IsTrue(baseObject.TechObjects.Any() is false && techObjects.Any());
        }

        [Test]
        public void CreateGenericGroup()
        {
            var techObjects = new List<TechObject.TechObject>();
            var genericTechObjects = new List<TechObject.GenericTechObject>();
            var techObject = new TechObject.TechObject("", GetN => 1, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);
            techObjectManagerMock.Setup(obj => obj.GenericTechObjects).Returns(genericTechObjects);

            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);

            var genericGroup = baseObject.CreateGenericGroup(techObject);

            Assert.IsTrue(baseObject.TechObjects.Any() is false &&
                genericGroup != null && genericTechObjects.Any());
        }

        [Test]
        public void AddAndRemoveLocalObjects()
        {
            var techObject = new TechObject.TechObject("", GetN => 1, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());
            var techObjectManagerMock = new Mock<ITechObjectManager>();
            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);

            Assert.Multiple(() =>
            {
                baseObject.AddLocalObject(techObject);
                Assert.AreEqual(1, baseObject.Count);

                baseObject.RemoveLocalObject(techObject);
                Assert.AreEqual(0, baseObject.Count);
            });
        }

        [TestCase(DialogResult.Yes)]
        [TestCase(DialogResult.No)]
        public void Delete(DialogResult deleteTechObjectInGroupDialogResult)
        {
            var techObjects = new List<TechObject.TechObject>();
            var genericTechObjects = new List<TechObject.GenericTechObject>();
            var techObject = new TechObject.TechObject("", GetN => 1, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());
            var techObjectOther = new TechObject.TechObject("", GetN => 2, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);
            techObjectManagerMock.Setup(obj => obj.GenericTechObjects).Returns(genericTechObjects);

            var editorMock = new Mock<IEditor>();
            editorMock.Setup(obj => obj.DialogDeletingGenericGroup())
                .Returns(deleteTechObjectInGroupDialogResult);

            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);
            var editorProperty = typeof(BaseObject).GetProperty("editor",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            editorProperty.SetValue(baseObject, editorMock.Object);

            baseObject.AddObject(techObject);
            baseObject.AddObject(techObjectOther);

            var genericGroup = baseObject.CreateGenericGroup(techObject);

            baseObject.Delete(genericGroup);

            if (deleteTechObjectInGroupDialogResult == DialogResult.Yes)
            {
                Assert.AreEqual(1, baseObject.Count);
            }
            else
            {
                Assert.AreEqual(2, baseObject.Count);
            }
        }

        [Test]
        public void MoveTest()
        {
            var techObjects = new List<TechObject.TechObject>();
            var genericTechObjects = new List<TechObject.GenericTechObject>();

            var techObject1 = new TechObject.TechObject("1", GetN => 1, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());
            var techObject2 = new TechObject.TechObject("2", GetN => 2, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());
            var techObject3 = new TechObject.TechObject("3", GetN => 3, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());
            var techObject4 = new TechObject.TechObject("4", GetN => 4, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);
            techObjectManagerMock.Setup(obj => obj.GenericTechObjects).Returns(genericTechObjects);

            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);

            baseObject.AddObject(techObject1);
            baseObject.AddObject(techObject2);

            baseObject.TechObjects.Add(techObject1);
            baseObject.TechObjects.Add(techObject2);

            baseObject.AddObject(techObject3);
            baseObject.AddObject(techObject4);

            var genericGroup_1 = baseObject.CreateGenericGroup(techObject3);
            var genericGroup_2 = baseObject.CreateGenericGroup(techObject4);

            Assert.Multiple(() =>
            {
                // UP
                var globlaNumMovedTO = techObject2.GlobalNum;
                var globalNumOtherTO = techObject1.GlobalNum;

                var movedUpTO = baseObject.MoveUp(techObject2);
                Assert.AreSame(techObject2, movedUpTO);
                Assert.AreEqual(globalNumOtherTO, techObject2.GlobalNum);

                var movedUpTOnull = baseObject.MoveUp(techObject2);
                Assert.IsNull(movedUpTOnull);
                Assert.AreEqual(globalNumOtherTO, techObject2.GlobalNum);

                var movedUpGG = baseObject.MoveUp(genericGroup_2);
                Assert.AreSame(genericGroup_2, movedUpGG);

                var movedUpGGnull = baseObject.MoveUp(genericGroup_2);
                Assert.IsNull(movedUpGGnull);

                var NoTypeMove = baseObject.MoveUp(1);
                Assert.IsNull(NoTypeMove);

                // DOWN
                globlaNumMovedTO = techObject2.GlobalNum;
                globalNumOtherTO = techObject1.GlobalNum;

                var movedDownTO = baseObject.MoveDown(techObject2);
                Assert.AreSame(techObject2, movedDownTO);
                Assert.AreEqual(globalNumOtherTO, techObject2.GlobalNum);

                var movedDownTOnull = baseObject.MoveDown(techObject2);
                Assert.IsNull(movedDownTOnull);
                Assert.AreEqual(globalNumOtherTO, techObject2.GlobalNum);

                var movedDownGG = baseObject.MoveDown(genericGroup_2);
                Assert.AreSame(genericGroup_2, movedDownGG);

                var movedDownGGnull = baseObject.MoveDown(genericGroup_2);
                Assert.IsNull(movedDownGGnull);

                NoTypeMove = baseObject.MoveDown(1);
                Assert.IsNull(NoTypeMove);
            });
        }

        [Test]
        public void Cut()
        {
            var techObjects = new List<TechObject.TechObject>();
            var genericTechObjects = new List<TechObject.GenericTechObject>();

            var techObject1 = new TechObject.TechObject("1", GetN => 1, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());
            var techObject2 = new TechObject.TechObject("2", GetN => 2, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);
            techObjectManagerMock.Setup(obj => obj.GenericTechObjects).Returns(genericTechObjects);

            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);

            baseObject.AddObject(techObject1);
            baseObject.AddObject(techObject2);

            baseObject.TechObjects.Add(techObject1);
            baseObject.TechObjects.Add(techObject2);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(baseObject.IsCuttable);

                baseObject.CreateGenericGroup(techObject2);
                Assert.IsTrue(baseObject.IsCuttable);

                var cuttedObject = baseObject.Cut(techObject1);
                Assert.AreSame(techObject1, cuttedObject);
                Assert.IsEmpty(baseObject.TechObjects);

                Assert.IsNull(baseObject.Cut(baseObject));
            });
        }

        [Test]
        public void Replace()
        {
            var techObjects = new List<TechObject.TechObject>();

            var techObject1 = new TechObject.TechObject("1", GetN => 1, 1, 2, "TANK", -1, "", string.Empty, new BaseTechObject());
            var techObject2 = new TechObject.TechObject("2", GetN => 2, 2, 3, "TANK_COPY", -1, "", string.Empty, new BaseTechObject());

            techObjects.Add(techObject1);
            techObjects.Add(techObject2);

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);

            var baseObject = new BaseObject("bto", techObjectManagerMock.Object);

            var baseTechObjectField = typeof(BaseObject).GetField("baseTechObject",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            baseTechObjectField.SetValue(baseObject, new BaseTechObject());

            baseObject.AddObject(techObject1);
            baseObject.TechObjects.Add(techObject1);

            baseObject.Replace(techObject1, techObject2);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(techObject1.TechType, baseObject.TechObjects[0].TechType);
                Assert.AreEqual(techObject1.NameEplan, baseObject.TechObjects[0].NameEplan);
            });
        }

        [OneTimeTearDown]
        public void CreateGenericTearDown()
        {
            var baseTechObjectManagerInstanceField = typeof(BaseTechObjectManager).GetField("baseTechObjectManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            baseTechObjectManagerInstanceField.SetValue(null, null);
        }

        [Test]
        public void CreateNewGenericGroup()
        {
            BaseTechObjectManager.GetInstance().AddBaseObject("BaseTechObjectName", "BaseTechObjectName", 2,
                "basicName", "bindingName", false, "luaModuleName", "monitorName", false, true);

            var techObjects = new List<TechObject.TechObject>();
            var genericTechObjects = new List<TechObject.GenericTechObject>();

            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);
            techObjectManagerMock.Setup(obj => obj.GenericTechObjects).Returns(genericTechObjects);

            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);

            var newGenericGroup = baseObject.CreateNewGenericGroup();

            Assert.Multiple(() =>
            {
                Assert.AreSame((newGenericGroup as GenericGroup).GenericTechObject, genericTechObjects[0]);
            });



            var baseTechObjectManagerInstanceField = typeof(BaseTechObjectManager).GetField("baseTechObjectManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            baseTechObjectManagerInstanceField.SetValue(null, null);
        }

        [TearDown]
        public void ResetBaseParameterDeviceManagerToDefault()
        {
            typeof(BaseParameter).GetField("deviceManager",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .SetValue(null, DeviceManager.GetInstance());
        }

        [Test]
        public void CreateGenericGroup_FromManyTechObjects()
        {
            SetupDeviceManagerMock();

            var baseTechObject = BaseTechObjectManager.GetInstance().AddBaseObject("BaseTechObjectName", "BaseTechObjectName", 2,
                "basicName", "bindingName", false, "luaModuleName", "monitorName", false, true);

            var equipParameter = new EquipmentParameter("equip_D", "оборудование", "LS1");

            baseTechObject.Equipment.Add(equipParameter);

            var techObjects = new List<TechObject.TechObject>();
            var genericTechObjects = new List<TechObject.GenericTechObject>();

            // Определяем тех. объекты
            var name = "Танк";
            var techType = 2;
            var nameEplan = "TANK";
            var cooperParamNumber = 1;
            var nameBC = "Tank";

            var techObject1 = new TechObject.TechObject(name, GetN => 1, 1, techType, nameEplan, cooperParamNumber, $"{nameBC}1",
                string.Empty, BaseTechObjectManager.GetInstance().GetTechObjectCopy("BaseTechObjectName"));

            var techObject2 = new TechObject.TechObject(name, GetN => 2, 2, techType, nameEplan, cooperParamNumber, $"{nameBC}2",
                string.Empty, BaseTechObjectManager.GetInstance().GetTechObjectCopy("BaseTechObjectName"));

            // Параметры тех. объектов
            var expT1Par1 =  techObject1.GetParamsManager().AddFloatParam("параметр 1", 1, "g", "par_1");
            var expT1Par2 = techObject1.GetParamsManager().AddFloatParam("параметр 2", 2, "g", "par_2");
            techObject1.GetParamsManager().AddFloatParam("параметр 3", 1, "g", "WRONG_LUA_NAME");

            var expT2Par1 = techObject2.GetParamsManager().AddFloatParam("параметр 1", 1, "g", "par_1");
            var expT2Par2 = techObject2.GetParamsManager().AddFloatParam("параметр 2", 1, "othermeter", "par_2");
            techObject2.GetParamsManager().AddFloatParam("параметр 3", 1, "g", "par_3");

            // Оборудование тех. объектов
            techObject1.Equipment.SetEquipmentValue("equip_d", "TANK1V1");
            techObject2.Equipment.SetEquipmentValue("equip_D", "TANK2V1");

            // Операции
            var expT1Mode =  techObject1.ModesManager.AddMode("Операция 1", "");
            techObject1.ModesManager.AddMode("Операция 2", "");

            var expT2Mode = techObject2.ModesManager.AddMode("Операция 1", "");

            // Шаги
            var expT1Step = expT1Mode.States[0].AddStep("Шаг 1", "");
            expT1Mode.States[0].AddStep("Шаг 2", "");
            
            var expT2Step = expT2Mode.States[0].AddStep("Шаг 1", "");

            // Действия
            expT1Step.AddDev("opened_devices", "TANK1V1", 0, "");
            expT2Step.AddDev("opened_devices", "TANK2V1", 0, "");
            expT1Step.AddDev("opened_devices", "DO1", 0, "");
            expT2Step.AddDev("opened_devices", "DO1", 0, "");

            expT1Step.AddDev("delay_opened_devices", "TANK1V1", 0, "A_0");
            expT1Step.AddDev("delay_opened_devices", "DO1", 0, "A_0");
            expT2Step.AddDev("delay_opened_devices", "TANK2V1", 0, "A_0");
            expT1Step.AddDev("delay_opened_devices", "TANK1V1", 1, "A_0");
            expT2Step.AddDev("delay_opened_devices", "TANK2V1", 1, "A_0");
            expT2Step.AddDev("delay_opened_devices", "DO1", 1, "A_0");


            // Мок менеджера тех. объектов
            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(obj => obj.TechObjects).Returns(techObjects);
            techObjectManagerMock.Setup(obj => obj.GenericTechObjects).Returns(genericTechObjects);

            // Создание базового объекта и тестирование метода
            var baseObject = new BaseObject("BaseTechObjectName", techObjectManagerMock.Object);

            var newGenericGroup = baseObject.CreateGenericGroup(new List<TechObject.TechObject>() { techObject1, techObject2 }) as GenericGroup;
            var newGenericTechObject = newGenericGroup.GenericTechObject;



            Assert.Multiple(() =>
            {
                // Данные объекта
                Assert.IsTrue(newGenericGroup.DisplayText[0].Contains(name));
                Assert.AreEqual(name, newGenericTechObject.Name);
                Assert.AreEqual(techType, newGenericTechObject.TechType);
                Assert.AreEqual(nameEplan, newGenericTechObject.NameEplan);
                Assert.AreEqual(cooperParamNumber, newGenericTechObject.CooperParamNumber);
                Assert.AreEqual(nameBC, newGenericTechObject.NameBC);

                // Параметры
                var parameters = (newGenericTechObject.GetParamsManager().Items[0] as Params).Items.Cast<Param>().ToList();
                Assert.AreEqual(2, parameters.Count());
                Assert.AreEqual(expT1Par1.GetName(), parameters[0].GetName());
                Assert.AreEqual(expT1Par2.GetName(), parameters[1].GetName());
                Assert.AreEqual(expT1Par1.GetNameLua(), parameters[0].GetNameLua());
                Assert.AreEqual(expT1Par2.GetNameLua(), parameters[1].GetNameLua());
                Assert.AreEqual(expT1Par1.GetValue(), parameters[0].GetValue());
                Assert.AreEqual("-", parameters[1].GetValue());
                Assert.AreEqual(expT1Par1.GetMeter(), parameters[0].GetMeter());
                Assert.AreEqual("", parameters[1].GetMeter());
                Assert.AreEqual(expT1Par1.GetOperationN(), parameters[0].GetOperationN());
                Assert.AreEqual("-1", parameters[1].GetOperationN());

                // Оборудование
                Assert.AreEqual("TANK1V1", (newGenericTechObject.Equipment.Items[0] as EquipmentParameter).Value);

                // Операции
                var modes = newGenericTechObject.ModesManager.Modes;
                var steps = modes[0].States[0].Steps;
                var actionOnDevices = steps[1].GetActions[1];
                var actionDelayOnDevices = steps[1].GetActions[2];

                Assert.AreEqual(1, modes.Count);
                Assert.AreEqual(expT1Mode.Name, modes[0].Name);
                Assert.AreEqual(2, steps.Count);
                Assert.AreEqual("Во время операции", steps[0].GetStepName());
                Assert.AreEqual(expT1Step.GetStepName(), steps[1].GetStepName());
                CollectionAssert.AreEqual(new[] { "TANK1V1", "DO1" }, actionOnDevices.DevicesNames);
                CollectionAssert.AreEqual(new[] { "TANK1V1" }, actionDelayOnDevices.SubActions[0].SubActions[0].DevicesNames);
            });
        }

        private void SetupDeviceManagerMock()
        {
            var deviceManagerMock = new Mock<IDeviceManager>();

            var capDevice = new LS("", "", StaticHelper.CommonConst.Cap, 0, "", 0, "");
            var DO1 = new DO("DO1", "DO1", "desc", 1, "", 0);
            var LS1 = new LS("LS1", "LS1", "desc", 1, "", 0, "");
            var TANK1V1 = new V("TANK1V1", "+TANK1-V1", "desc", 1, "TANK", 1, "");
            var TANK2V1 = new V("TANK2V1", "+TANK2-V1", "desc", 1, "TANK", 2, "");

            deviceManagerMock.Setup(m => m.GetDeviceByEplanName(It.IsAny<string>())).Returns(capDevice);
            deviceManagerMock.Setup(m => m.GetDeviceByEplanName("LS1")).Returns(LS1);
            deviceManagerMock.Setup(m => m.GetDeviceByEplanName("TANK1V1")).Returns(TANK1V1);
            deviceManagerMock.Setup(m => m.GetDeviceByEplanName("TANK2V1")).Returns(TANK2V1);
            deviceManagerMock.Setup(m => m.GetDeviceByEplanName("DO1")).Returns(DO1);

            deviceManagerMock.Setup(m => m.GetDeviceIndex("LS1")).Returns(0);
            deviceManagerMock.Setup(m => m.GetDeviceIndex("TANK1V1")).Returns(1);
            deviceManagerMock.Setup(m => m.GetDeviceIndex("TANK2V1")).Returns(2);
            deviceManagerMock.Setup(m => m.GetDeviceIndex("DO1")).Returns(3);

            deviceManagerMock.Setup(m => m.GetDeviceIndex(LS1)).Returns(0);
            deviceManagerMock.Setup(m => m.GetDeviceIndex(TANK1V1)).Returns(1);
            deviceManagerMock.Setup(m => m.GetDeviceIndex(TANK2V1)).Returns(2);
            deviceManagerMock.Setup(m => m.GetDeviceIndex(DO1)).Returns(3);

            deviceManagerMock.Setup(m => m.GetDeviceByIndex(0)).Returns(LS1);
            deviceManagerMock.Setup(m => m.GetDeviceByIndex(1)).Returns(TANK1V1);
            deviceManagerMock.Setup(m => m.GetDeviceByIndex(2)).Returns(TANK2V1);
            deviceManagerMock.Setup(m => m.GetDeviceByIndex(3)).Returns(DO1);

            deviceManagerMock.Setup(m => m.GetModifiedDevice(TANK1V1, It.IsAny<IDevModifyOptions>())).Returns(TANK1V1);
            deviceManagerMock.Setup(m => m.GetModifiedDevice(TANK2V1, It.IsAny<IDevModifyOptions>())).Returns(TANK1V1);
            deviceManagerMock.Setup(m => m.GetModifiedDevice(LS1, It.IsAny<IDevModifyOptions>())).Returns(LS1);
            deviceManagerMock.Setup(m => m.GetModifiedDevice(DO1, It.IsAny<IDevModifyOptions>())).Returns(DO1);

            typeof(BaseParameter).GetField("deviceManager",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .SetValue(null, deviceManagerMock.Object);
            typeof(Equipment).GetProperty("deviceManager",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .SetValue(null, deviceManagerMock.Object);
            typeof(Step).GetProperty("deviceManager",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .SetValue(null, deviceManagerMock.Object);
            typeof(TechObject.Action).GetField("deviceManager",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .SetValue(null, deviceManagerMock.Object);
        }

        [Test]
        public void InsertCuttedCopy()
        {
            var techObjectManagerMock = new Mock<ITechObjectManager>();
            var techObjectParentMock = new Mock<ITreeViewItem>();

            techObjectParentMock.Setup(o => o.Cut(It.IsAny<TechObject.TechObject>())).Returns<TechObject.TechObject>(to => to);

            var userObject = new BaseObject("", techObjectManagerMock.Object);
            var techObject = new TechObject.TechObject("", GetN => 1, 1, 2, "", -1, "", "", null);
            techObject.Parent = techObjectParentMock.Object;

            var method = typeof(BaseObject).GetMethod("InsertCuttedCopy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            method.Invoke(userObject, new object[] { techObject });

            Assert.AreSame(techObject, userObject.Items.SingleOrDefault());
        }

        [Test]
        public void Insert_SingleInstanceObject_BlocksSecondInsert()
        {
            var techObjects = new List<TechObject.TechObject>();
            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(tom => tom.TechObjects).Returns(techObjects);

            var baseObject = new SingleInstanceBaseObject(techObjectManagerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(baseObject.IsInsertable);

                var first = baseObject.Insert();
                Assert.IsNotNull(first);
                Assert.AreEqual(1, baseObject.Count);
                Assert.AreEqual(1, techObjects.Count);
                Assert.IsFalse(baseObject.IsInsertable);
                Assert.IsFalse(baseObject.MessageShown);

                var second = baseObject.Insert();
                Assert.IsNull(second);
                Assert.AreEqual(1, baseObject.Count);
                Assert.AreEqual(1, techObjects.Count);
                Assert.IsTrue(baseObject.MessageShown);
            });
        }

        [Test]
        public void InsertCopy_SingleInstanceObject_BlocksDuplicationButAllowsMove()
        {
            var techObjects = new List<TechObject.TechObject>();
            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(tom => tom.TechObjects).Returns(techObjects);
            var techObjectParentMock = new Mock<ITreeViewItem>();
            techObjectParentMock.Setup(o => o.Cut(It.IsAny<TechObject.TechObject>()))
                .Returns<TechObject.TechObject>(to => to);

            var baseObject = new SingleInstanceBaseObject(techObjectManagerMock.Object);
            baseObject.Insert();

            var techObject = new TechObject.TechObject("Главный модуль мойки",
                GetN => 1, 1, 2, "CIP_MODULE", -1, "", "", baseObject.BaseTechObject);
            techObject.Parent = techObjectParentMock.Object;

            Assert.Multiple(() =>
            {
                Assert.IsFalse(baseObject.IsInsertableCopy);

                var copyResult = baseObject.InsertCopy(techObject);
                Assert.IsNull(copyResult);
                Assert.AreEqual(1, baseObject.Count);

                techObject.MarkToCut = true;
                var cutResult = baseObject.InsertCopy(techObject);
                Assert.AreSame(techObject, cutResult);
                Assert.AreEqual(2, baseObject.Count);
            });
        }

        [Test]
        public void CreateNewGenericGroup_SingleInstanceObject_BlocksSecond()
        {
            var techObjects = new List<TechObject.TechObject>();
            var genericTechObjects = new List<GenericTechObject>();
            var techObjectManagerMock = new Mock<ITechObjectManager>();
            techObjectManagerMock.Setup(tom => tom.TechObjects).Returns(techObjects);
            techObjectManagerMock.Setup(tom => tom.GenericTechObjects).Returns(genericTechObjects);

            var baseObject = new SingleInstanceBaseObject(techObjectManagerMock.Object);

            Assert.Multiple(() =>
            {
                var first = baseObject.CreateNewGenericGroup();
                Assert.IsNotNull(first);
                Assert.AreEqual(1, genericTechObjects.Count);

                var second = baseObject.CreateNewGenericGroup();
                Assert.IsNull(second);
                Assert.AreEqual(1, genericTechObjects.Count);
                Assert.IsTrue(baseObject.MessageShown);
            });
        }

        /// <summary>
        /// Базовый объект-заглушка с признаком единственного экземпляра.
        /// Заглушка переопределяет показ сообщения, чтобы не блокировать тесты.
        /// </summary>
        private class SingleInstanceBaseObject : BaseObject
        {
            public SingleInstanceBaseObject(ITechObjectManager techObjectManager)
                : base("main_cip_module", techObjectManager)
            {
                typeof(BaseObject).GetField("baseTechObject",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                    .SetValue(this, new BaseTechObject
                    {
                        Name = "Главный модуль мойки",
                        EplanName = "main_cip_module",
                        S88Level = 2,
                        IsSingleInstance = true
                    });
            }

            public bool MessageShown { get; private set; }

            protected override void ShowSingleInstanceMessage()
            {
                MessageShown = true;
            }
        }
    }
}
