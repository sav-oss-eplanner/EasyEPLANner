using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Editor;
using Moq;
using NUnit.Framework;
using TechObject;
using TechObject.AttachedObjectStrategy;

namespace EasyEplannerTests.TechObjectTest.ObjectsTreeTest.UniversalObjecsTest
{
    public class TechObjectTest
    {
        [Test]
        public void SaveAsLuaTable_CheckResultString()
        {
            string name = "name_to";
            int globalNum = 1;
            TechObject.GetN getN = (x) => globalNum;
            int techNumber = 2;
            int techType = 3;
            string eplanName = "eplanName_to";
            int cooperParamNumber = 4;
            string BCName = "BC_name_to";
            string prefix = "\t\t";

            string modeName = "name_mode";
            string baseOperationName = "baseOperationName";

            var techObject = new TechObject.TechObject(name, getN, techNumber,
                techType, eplanName, cooperParamNumber, BCName, string.Empty, null);
            techObject.ModesManager.AddMode(modeName, baseOperationName);

            var expected = new StringBuilder()
                .Append($"\t[ {globalNum} ] =\n")
                .Append(prefix).Append("{\n")
                .Append(prefix).Append($"n          = {techNumber},\n")
                .Append(prefix).Append($"tech_type  = {techType},\n")
                .Append(prefix).Append($"name       = '{name}',\n")
                .Append(prefix).Append($"name_eplan = '{eplanName}',\n")
                .Append(prefix).Append($"name_BC    = '{BCName}',\n")
                .Append(prefix).Append($"cooper_param_number = {cooperParamNumber},\n\n")
                .Append(prefix).Append($"modes =\n")
                .Append(prefix).Append($"\t{{\n")
                .Append(prefix).Append($"\t[ 1 ] =\n")
                .Append(prefix).Append($"\t\t{{\n")
                .Append(prefix).Append($"\t\tname = '{modeName}',\n")
                .Append(prefix).Append($"\t\t}},\n")
                .Append(prefix).Append($"\t}},\n")
                .Append(prefix).Append($"}},\n");

            Assert.AreEqual(expected.ToString(), techObject.SaveAsLuaTable(prefix, globalNum));
        }

        [Test]
        public void SaveAsLuaTable_WritesAttachedObjectsRefsForExport()
        {
            string prefix = "\t\t";
            int globalNum = 1;
            var techObject = new TechObject.TechObject("name_to", x => globalNum,
                2, 3, "eplanName_to", 4, "BC_name_to", "5", null);

            string result = techObject.SaveAsLuaTable(prefix, globalNum,
                "MIX_NODE:2 PUMP:1");

            StringAssert.Contains(
                $"{prefix}attached_objects_refs = 'MIX_NODE:2 PUMP:1',\n",
                result);
        }

        [Test]
        public void SaveAsLuaTable_OmitsAttachedObjectsRefsWhenExportValueIsEmpty()
        {
            int globalNum = 1;
            var techObject = new TechObject.TechObject("name_to", x => globalNum,
                2, 3, "eplanName_to", 4, "BC_name_to", string.Empty, null);

            string result = techObject.SaveAsLuaTable("\t\t", globalNum);

            Assert.IsFalse(result.Contains("attached_objects_refs"));
        }


        [Test]
        public void QuickMultiSelect()
        {
            var item1 = new TechObject.TechObject("", GetN => 1, 1, 2, "", -1, "", "", null);
            var item2 = new TechObject.TechObject("", GetN => 1, 1, 2, "", -1, "", "", null);
            var item3 = new TechObject.TechObject("", GetN => 1, 1, 3, "", -1, "", "", null);
            var parentMock = new Mock<ITreeViewItem>();
            parentMock.Setup(o => o.Items).Returns(new[] { item1, item2, item3 });

            item1.Parent = parentMock.Object;
            item2.Parent = parentMock.Object;
            item3.Parent = parentMock.Object;

            Assert.Multiple(() =>
            {
                var multiselect = item2.QuickMultiSelect();
                CollectionAssert.AreEqual(new List<ITreeViewItem>() { item1, item2 }, multiselect);

                multiselect = item3.QuickMultiSelect();
                CollectionAssert.AreEqual(new List<ITreeViewItem>() { item3 }, multiselect);
            });

        }


        [Test]
        public void Replace_AttachedObjects()
        {
            var baseTechObject = new BaseTechObject()
            {
                Name = "BTO",
                LuaModuleName = "BTO",
            };

            var techObject1 = new TechObject.TechObject("", GetN => 1, 1, 2, "", -1, "", "", baseTechObject);
            var techObject2 = new TechObject.TechObject("", GetN => 1, 1, 2, "", -1, "", "", baseTechObject);

            var replaced = techObject1.Replace(techObject1.AttachedObjects, techObject2.AttachedObjects);

            // Заменяется не поле привязанных агрегатов а его значение
            Assert.AreSame(replaced, techObject1.AttachedObjects);
        }

        [Test]
        public void Replace_Empty()
        {
            var techObject = new TechObject.TechObject("", GetN => 1, 1, 2, "", -1, "", "", null);

            Assert.IsNull(techObject.Replace(0, 0));
        }
    }
}
