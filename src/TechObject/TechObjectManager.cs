using EasyEPlanner;
using Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LuaInterface;
using System.Windows.Forms;
using System.Text;
using System.Reflection;
using EasyEPlanner.FileSavers.XML;

namespace TechObject
{
    /// <summary>
    /// Получение номера объекта в списке. Нумерация начинается с 1.
    /// </summary>
    public delegate int GetN(object obj);

    /// <summary>
    /// Менеджер объектов редактора.
    /// </summary>
    public class TechObjectManager : TreeViewItem, ITechObjectManager
    {
        private TechObjectManager()
        {
            InitLua();

            treeObjects = new List<ITreeViewItem>();
            genericTechObjects = new List<GenericTechObject>();
            techObjects = new List<TechObject>();
            techObjectManagerChecker = new TechObjectChecker(this);
            techObjectXMLMaker = new TechObjectXMLMaker(this);
        }

        #region Инициализация LUA-скриптов
        /// <summary>
        /// Инициализировать Lua-скрипт для чтения описания объектов
        /// </summary>
        private void InitLua()
        {
            lua = new Lua();
            InitReadDescriptionLuaScript();
            InitReadRestrictionsLuaScript();
        }

        /// <summary>
        /// Загрузка скрипта для чтения описания объектов.
        /// </summary>
        private void InitReadDescriptionLuaScript()
        {
            lua.RegisterFunction("ADD_TECH_OBJECT", this,
                GetType().GetMethod("AddObject"));
            const string sysFileName = "sys.lua";
            string sysLuaPath = Path.Combine(ProjectManager.GetInstance()
                .SystemFilesPath, sysFileName);
            lua.DoFile(sysLuaPath);
        }

        /// <summary>
        /// Загрузка скрипта для чтения ограничений объектов.
        /// </summary>
        private void InitReadRestrictionsLuaScript()
        {
            lua.RegisterFunction("Get_TECH_OBJECT", this,
                GetType().GetMethod("GetTObject"));
            lua.RegisterFunction("Get_GENERIC_OBJECT", this,
                GetType().GetMethod("GetGenericTObject"));
            string restrictionFileName = "sys_restriction.lua";
            string pathToRestrictionInitializer = Path
                .Combine(ProjectManager.GetInstance().SystemFilesPath,
                restrictionFileName);
            lua.DoFile(pathToRestrictionInitializer);
        }
        #endregion

        /// <summary>
        /// Получить экземпляр класса менеджера объектов
        /// </summary>
        /// <returns></returns>
        public static TechObjectManager GetInstance()
        {
            if (instance == null)
            {
                instance = new TechObjectManager();
            }

            return instance;
        }

        /// <summary>
        /// Получение объекта по номеру
        /// </summary>
        /// <param name="i">Номер объекта</param>
        /// <returns></returns>
        public TechObject GetTObject(int i)
        {
            if (techObjects != null && techObjects.Count >= i)
            {
                return techObjects[i - 1];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Получение типового объекта по номеру
        /// </summary>
        /// <param name="globalNum">Номер типового объекта</param>
        public GenericTechObject GetGenericTObject(int globalNum)
            => genericTechObjects.ElementAtOrDefault(globalNum - 1);


        public int GetGenericObjectN(object techObject)
            => genericTechObjects.IndexOf(techObject as GenericTechObject) + 1;

        /// <summary>
        /// Получение номера объекта в списке тех. объектов. 
        /// Нумерация начинается с 1.
        /// </summary>
        /// <param name="techObject">Тех. объект</returns>
        public int GetTechObjectN(object techObject)
        {
            return techObjects.IndexOf(techObject as TechObject) + 1;
        }

        public int GetTechObjectN(string displayText)
        {
            TechObject findedObject = TechObjects
                .Find(x => x.DisplayText[0] == displayText);

            if(findedObject != null)
            {
                return findedObject.GlobalNum;
            }
            else
            {
                return 0;
            }
        }


        public int GetTechObjectN(string baseObjectName, string nameEplan, int techNumber)
        {
            var techObject = TechObjects
                .Find(to => 
                    (to.BaseTechObject?.EplanName.Equals(baseObjectName) ?? false) &&
                    to.NameEplan.Equals(nameEplan) && to.TechNumber == techNumber);

            return techObjects.IndexOf(techObject) + 1;
        }

        public int GetTechObjectN(string baseObjectName, int techNumber)
        {
            var techObject = TechObjects
                .Find(to =>
                    (to.BaseTechObject?.EplanName.Equals(baseObjectName) ?? false) &&
                    to.TechNumber == techNumber);

            return techObjects.IndexOf(techObject) + 1;
        }

        public int GetTechObjectN(string baseObjectName, int techType, int techNumber)
        {
            var techObject = TechObjects
                .Find(to =>
                    (to.BaseTechObject?.EplanName.Equals(baseObjectName) ?? false) &&
                    to.TechType == techType && to.TechNumber == techNumber);

            return techObjects.IndexOf(techObject) + 1;
        }

        public int TypeAdjacentTObjectIdByTNum(int targetObjectIndex, int techNumber)
        {
            var techObject = GetTObject(targetObjectIndex);
            var indexByTechNumber = GetTechObjectN(techObject.BaseTechObject.EplanName, techObject.TechType, techNumber);
            if (indexByTechNumber > 0)
                return indexByTechNumber;
            else
                return targetObjectIndex;
        }

        /// <summary>
        /// Проверка и исправление ограничений при удалении/перемещении
        /// операции 
        /// </summary>
        public void ChangeModeNum(TechObject techObject, int oldNum, int newNum)
        {
            foreach (TechObject to in TechObjects)
            {
                to.ChangeModeNum(techObject, oldNum, newNum);
            }
        }

        /// <summary>
        /// Сохранение в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns>Описание в виде таблицы Lua.</returns>
        public string SaveAsLuaTable(string prefix)
        {
            var res = new StringBuilder();

            GenericTechObjects.ForEach(obj => obj.Update());

            var genericObjectsData = SaveObjects(GenericTechObjects.Cast<TechObject>().ToList(), prefix);
            var techObjectsData = SaveObjects(TechObjects, prefix);

            if (genericObjectsData != string.Empty)
            {
                res.Append("init_generic_tech_objects = function()\n")
                    .Append("\treturn\n")
                    .Append("\t{\n")
                    .Append($"{genericObjectsData}")
                    .Append("\t}\n")
                    .Append("end\n")
                    .Append($"{new string('-', 80)}\n")
                    .Append($"{new string('-', 80)}\n");
            }

            res.Append("init_tech_objects_modes = function()\n")
                .Append("\treturn\n")
                .Append("\t{\n")
                .Append($"{techObjectsData}")
                .Append("\t}\n")
                .Append("end");

            res = res.Replace("\t", "    ");
            return res.ToString();
        }

        public string SaveObjects(List<TechObject> Objects, string prefix)
        {
            var res = new StringBuilder();

            foreach (TechObject obj in Objects)
            {
                int num = Objects.IndexOf(obj) + 1;
                res.Append(obj.SaveAsLuaTable(prefix + "\t\t", num));
            }

            return res.ToString();
        }

        /// <summary>
        /// Сохранение ограничений в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns>Описание в виде таблицы Lua.</returns>
        public string SaveRestrictionAsLua(string prefix)
        {
            var res = new StringBuilder();
            var genericRstrBldr = new StringBuilder();

            foreach (GenericTechObject obj in GenericTechObjects)
            {
                int num = GenericTechObjects.IndexOf(obj) + 1;
                genericRstrBldr.Append(obj.SaveRestrictionAsLua(prefix + "\t", num));
            }

            if (genericRstrBldr.Length > 0)
            {
                res.Append("generic_restrictions =\n")
                .Append("\t{\n")
                .Append(genericRstrBldr)
                .Append("\t}\n")
                .Append($"{new string('-', 80)}\n")
                .Append($"{new string('-', 80)}\n");
            }

            res.Append("restrictions =\n")
                .Append("\t{\n");
            foreach (TechObject obj in TechObjects)
            {
                int num = TechObjects.IndexOf(obj) + 1;
                res.Append(obj.SaveRestrictionAsLua(prefix + "\t", num));
            }
            res.Append("\t}");
            res = res.Replace("\t", "    ");
            return res.ToString();
        }

        #region Загрузка описания из LUA
        /// <summary>
        /// Загрузка описания проекта из строки
        /// </summary>
        /// <param name="LuaStr">Строка с описанием</param>
        /// <param name="projectName">Имя проекта</param>
        public void LoadDescription(string LuaStr, string projectName)
        {
            ProjectName = projectName;
            treeObjects.Clear();
            techObjects.Clear();
            genericTechObjects.Clear();

            //Сброс описания объектов.
            lua.DoString("init_tech_objects_modes = nil");
            try
            {
                //Выполнения Lua скрипта с описанием объектов.
                lua.DoString(LuaStr);
            }
            catch (Exception ex)
            {
                string message = $"{ex.Message}. Ошибка обработки " +
                    $"Lua-скрипта описания проекта. " +
                    $"Данные не будут сохранены.";
                throw new Exception(message);
            }
            try
            {
                //Создание объектов C# из скрипта Lua.
                object[] res = lua.DoString("if init ~= nil then init() end");
            }
            catch (Exception ex)
            {
                string message = $"{ex.Message}. Ошибка обработки " +
                    $"Lua-скрипта для загрузки данных проекта. " +
                    $"Данные не будут сохранены.\n" +
                    $"Source: {ex.Source}";
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Загрузка ограничений объектов
        /// </summary>
        /// <param name="LuaStr">Описание ограничений объектов</param>
        public void LoadRestrictions(string LuaStr)
        {
            //Выполнения Lua скрипта с описанием объектов.
            lua.DoString(LuaStr);
            lua.DoString("init_restrictions()");
            lua.DoString("init_generic_restrictions()");
        }
        #endregion

        #region Добавление объекта из LUA
        /// <summary>
        /// Добавление технологического объекта. Вызывается из Lua.
        /// </summary>
        /// <returns>Добавленный технологический объект.</returns>
        /// <param name="globalNumber">Глобальный номер объекта, используется
        /// при импорте из файла</param>
        public TechObject AddObject(int globalNumber, int techN, string name,
            int techType, string nameEplan, int cooperParamNumber,
            string NameBC, string baseTechObjectName, string attachedObjects,
            string attachedObjectsRefs, int genericTechObjectNumber,
            bool isGeneric)
        {
            // globalNumber игнорируется в этом методе, но используется при
            // импорте описания из файла (аналогичная сигнатура, другое тело).
            // attachedObjectsRefs игнорируется при загрузке проекта;
            // обрабатывается в TechObjectsImporter.LoadObjects.
            _ = attachedObjectsRefs;

            var baseTechObject = BaseTechObjectManager.GetInstance()
                .GetTechObjectCopy(baseTechObjectName);

            if (isGeneric)
            {
                var obj = new GenericTechObject(name, techType, nameEplan,
                    cooperParamNumber, NameBC, attachedObjects, baseTechObject);

                AddGenericObject(obj);
                return obj;
            }
            else
            {
                // getN - null т.к он будет другой, ниже по функциям.
                TechObject obj = new TechObject(name, null, techN,
                    techType, nameEplan.ToUpper(), cooperParamNumber, NameBC,
                    attachedObjects, baseTechObject);

                AddObject(obj, genericTechObjectNumber);
                return obj;
            }
        }

        /// <summary>
        /// Восстановить привязанные агрегаты по переносимым ссылкам.
        /// </summary>
        public void ApplyAttachedObjectsReferences(
            IEnumerable<TechObject> objects = null)
        {
            IEnumerable<TechObject> targetObjects = objects ?? TechObjects;
            foreach (TechObject techObject in targetObjects)
                AttachedObjectReferences.ApplyTo(this, techObject, objects);
        }

        /// <summary>
        /// Добавление технологического объекта.
        /// </summary>
        /// <param name="obj">Добавляемый объект</param>
        private void AddObject(TechObject obj, int genericObjectNumber = -1)
        {
            if (obj.BaseTechObject != null)
            {
                AddIdentifiedObject(obj, false, genericObjectNumber);
            }
            else
            {
                AddUnidentifiedObject(obj);
            }

            techObjects.Add(obj);
        }

        private void AddGenericObject(GenericTechObject obj)
        {
            if (obj.BaseTechObject != null)
            {
                AddIdentifiedObject(obj, true);
            }

            genericTechObjects.Add(obj);
        }

        /// <summary>
        /// Добавить опознанный объект при загрузке из LUA
        /// </summary>
        /// <param name="obj">Объект</param>
        private void AddIdentifiedObject(TechObject obj, bool isGeneric = false, int genericObjectNumber = -1)
        {
            BaseTechObject baseTechObject = obj.BaseTechObject;
            var type = (BaseTechObjectManager.ObjectType)baseTechObject
                .S88Level;
            string name = BaseTechObjectManager.GetInstance()
                        .GetS88Name(baseTechObject.S88Level);
            switch (type)
            {
                case BaseTechObjectManager.ObjectType.ProcessCell:
                     AddProcessCell(obj);
                    break;

                case BaseTechObjectManager.ObjectType.Unit:
                    AddS88Object(obj, name, isGeneric, genericObjectNumber);
                    break;

                case BaseTechObjectManager.ObjectType.Aggregate:
                    AddS88Object(obj, name, isGeneric, genericObjectNumber);
                    break;

                case BaseTechObjectManager.ObjectType.UserObject:
                    AddUserObject(obj, isGeneric, genericObjectNumber);
                    break;
            }
        }

        /// <summary>
        /// Добавить ячейку процесса (мастер) из LUA
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <returns></returns>
        private void AddProcessCell(TechObject obj)
        {
            var masterItem = treeObjects.Where(x => x is ProcessCell)
                        .FirstOrDefault() as ProcessCell;
            if (masterItem == null)
            {
                masterItem = new ProcessCell(instance);
                treeObjects.Add(masterItem);
                SortTreeObjectsByCustomComparer();
            }

            masterItem.AddObjectWhenLoadFromLua(obj);
        }

        /// <summary>
        /// Добавить аппарат из LUA
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="name">Имя объекта</param>
        /// <returns></returns>
        private void AddS88Object(TechObject obj, string name,
            bool isGeneric = false, int genericObjectNumber = -1)
        {
            var s88Item = treeObjects
                .Where(x => x is S88Object && x.DisplayText[0].Contains(name))
                .FirstOrDefault() as S88Object;
            if (s88Item == null)
            {
                s88Item = new S88Object(name, instance);
                treeObjects.Add(s88Item);
                SortTreeObjectsByCustomComparer();
            }

            s88Item.AddObjectWhenLoadFromLua(obj, isGeneric, genericObjectNumber);
        }

        /// <summary>
        /// Добавить пользовательский объект из LUA
        /// </summary>
        /// <param name="obj">Объект</param>
        private void AddUserObject(TechObject obj, bool isGeneric, int genericObjectNumber)
        {
            var userObject = treeObjects.Where(x => x is UserObject)
                .FirstOrDefault() as UserObject;
            if(userObject == null)
            {
                userObject = new UserObject(instance);
                treeObjects.Add(userObject);
                SortTreeObjectsByCustomComparer();
            }

            if (isGeneric)
                userObject.AddGenericObjectWhenLoadFromLua(obj as GenericTechObject);
            else
                userObject.AddObjectWhenLoadFromLua(obj, genericObjectNumber);
        }

        /// <summary>
        /// Добавить неопознанный объект при добавлении из LUA
        /// </summary>
        /// <param name="obj">Объект</param>
        private void AddUnidentifiedObject(TechObject obj)
        {
            var unidentifiedObject = treeObjects
                .Where(x => x is Unidentified)
                .FirstOrDefault() as Unidentified;
            if (unidentifiedObject == null)
            {
                unidentifiedObject = new Unidentified(instance);
                unidentifiedObject.AddParent(instance);
                treeObjects.Add(unidentifiedObject);
                SortTreeObjectsByCustomComparer();
            }

            unidentifiedObject.AddUnidentifiedObject(obj);
        }
        #endregion

        public void ImportObject(TechObject importingObject)
        {
            AddObject(importingObject);
        }

        /// <summary>
        /// Проверка технологического объекта на правильность ввода и др.
        /// </summary>
        /// <returns>Строка с ошибками</returns>
        public string Check()
        {
            return techObjectManagerChecker.Check();
        }

        /// <summary>
        /// Формирование узлов для операций, шагов и параметров объектов.
        /// </summary>
        /// <param name="rootNode">корневой узел</param>
        /// <param name="combineTags">Сгруппировать тэги в один подтип</param>
        /// <param name="useNewNames">Использовать имена объектов вместо
        /// OBJECT</param>
        public void GetObjectForXML(IDriver root, bool combineTags, bool useNewNames)
        {
            techObjectXMLMaker.BuildObjectsForXML(root, combineTags, useNewNames);
        }

        private void SortTreeObjectsByCustomComparer()
        {
            string firstS88LevelName = BaseTechObjectManager.GetInstance()
                .GetS88Name((int)BaseTechObjectManager.ObjectType.Unit);
            string secondS88LevelName = BaseTechObjectManager.GetInstance()
                .GetS88Name((int)BaseTechObjectManager.ObjectType.Aggregate);

            treeObjects = treeObjects.OrderByDescending(i => i is ProcessCell)
                .ThenByDescending(i => i is S88Object && i.DisplayText[0]
                .Contains(firstS88LevelName))
                .ThenByDescending(i => i is S88Object && i.DisplayText[0]
                .Contains(secondS88LevelName))
                .ThenByDescending(i => i is UserObject)
                .ThenByDescending(i => i is Unidentified)
                .ToList();
        }

        #region Синхронизация устройств в объектах
        /// <summary>
        /// Синхронизация устройств в объектах
        /// </summary>
        /// <param name="array">Индексная таблица</param>
        public void Synch(int[] array)
        {
            foreach (TechObject obj in TechObjects)
            {
                obj.Synch(array);
            }
        }
        #endregion

        #region Реализация ITreeViewItem
        public override string[] DisplayText
        {
            get
            {
                string res = "\"" + ProjectName + "\"";
                if (treeObjects.Count > 0)
                {
                    res += " (" + treeObjects.Count + ")";
                }

                return new string[] { res, "" };
            }
        }

        public override ITreeViewItem[] Items
        {
            get
            {
                return treeObjects.ToArray();
            }
        }

        public override ImageIndexEnum ImageIndex
        {
            get
            {
                return ImageIndexEnum.TechObjectManager;
            }
        }

        public override bool IsInsertableCopy
        {
            get
            {
                return true;
            }
        }

        public override ITreeViewItem InsertCopy(object obj)
        {
            var techObj = obj as TechObject;
            if(techObj != null && techObj.MarkToCut)
            {
                ChooseObjectTypes(out string selectedType,
                    out string selectedSubType);
                if (selectedType != null && selectedSubType != null)
                {
                    ITreeViewItem insertedItem = InsertType(selectedType,
                        techObj);

                    insertedItem.AddParent(this);
                    return insertedItem;
                }
            }

            return null;
        }

        public override bool IsInsertable
        {
            get
            {
                return true;
            }
        }

        public override ITreeViewItem Insert()
        {
            ChooseObjectTypes(out string selectedType, out string selectedSubType);

            if (string.IsNullOrEmpty(selectedType) || string.IsNullOrEmpty(selectedSubType))
                return null;

            ITreeViewItem insertedItem = InsertType(selectedType);

            if (insertedItem is null)
                return null;

            insertedItem.AddParent(this);
            SortTreeObjectsByCustomComparer();
            return insertedItem;
        }

        /// <summary>
        /// Выбор типа и подтипа объекта на форме.
        /// </summary>
        /// <param name="selectedType">Тип</param>
        /// <param name="selectedSubType">Подтип</param>
        private void ChooseObjectTypes(out string selectedType,
            out string selectedSubType)
        {
            var objectsAdderForm = new ObjectsAdder();
            objectsAdderForm.ShowDialog();
            selectedType = ObjectsAdder.LastSelectedType;
            selectedSubType = ObjectsAdder.LastSelectedSubType;
        }

        private ITreeViewItem InsertType(string selectedType,
            TechObject techObj = null)
        {
            ITreeViewItem treeItem = GetTreeItem(selectedType);
            ITreeViewItem innerItem;

            bool needInsert = techObj == null;
            if (needInsert)
            {
                innerItem = treeItem.Insert();
            }
            else
            {
                innerItem = treeItem.InsertCopy(techObj);
            }

            if (innerItem != null)
            {
                return treeItem;
            }

            return null;
        }

        /// <summary>
        /// Создать объект дерева, описывающий базу по S88.
        /// </summary>
        /// <param name="selectedType">Выбранный на форме тип объекта</param>
        /// <returns></returns>
        private ITreeViewItem GetTreeItem(string selectedType)
        {
            ITreeViewItem treeItem = treeObjects
                .Where(x => x.EditText[0] == selectedType)
                .FirstOrDefault();
            if (treeItem == null)
            {
                ITreeViewItem newTreeItem;
                switch(selectedType)
                {
                    case ProcessCell.Name:
                        newTreeItem = new ProcessCell(instance);
                        break;

                    case UserObject.Name:
                        newTreeItem = new UserObject(instance);
                        break;

                    default:
                        newTreeItem = new S88Object(selectedType, instance);
                        break;
                }

                treeObjects.Add(newTreeItem);
                SortTreeObjectsByCustomComparer();
                newTreeItem.AddParent(instance);

                return newTreeItem;
            }
            else
            {
                return treeItem;
            }
        }

        public override bool Delete(object child)
        {
            var treeViewItem = child as ITreeViewItem;
            if (treeViewItem != null && treeObjects.Contains(treeViewItem))
            {
                foreach (var item in treeViewItem.Items)
                {
                    treeViewItem.Delete(item);
                }

                if (treeViewItem.Items.Count() == 0)
                {
                    treeObjects.Remove(child as ITreeViewItem);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Корректировка объектов при их удалении или перемещении
        public void CheckRestriction(int oldNum, int newNum)
        {
            foreach (TechObject techObject in TechObjects)
            {
                techObject.CheckRestriction(oldNum, newNum);
            }
        }

        public void ChangeAttachedObjectsAfterDelete(int deletedObjectNum)
        {
            foreach (var obj in TechObjects)
            {
                ChangeAttachedObjectAfterDelete(obj.AttachedObjects,
                    deletedObjectNum);

                if (obj.BaseTechObject?.ObjectGroupsList != null)
                {
                    foreach (var group in obj.BaseTechObject.ObjectGroupsList)
                    {
                        ChangeAttachedObjectAfterDelete(group,
                            deletedObjectNum);
                    }
                }
            }
        }

        /// <summary>
        /// Изменение привязки объекта при удалении объекта из дерева 
        /// </summary>
        /// <param name="attachedObjects">Элемент для обработки</param>
        /// <param name="deletedObjectNum">Номер удаленного объекта</param>
        private void ChangeAttachedObjectAfterDelete(
            AttachedObjects attachedObjects, int deletedObjectNum)
        {
            if (attachedObjects?.Value == string.Empty)
            {
                return;
            }

            string attachingObjectsStr = attachedObjects?.Value ?? "";
            int[] attachingObjectsArr = attachingObjectsStr.Split(' ')
                .Select(int.Parse).ToArray();
            for (int index = 0; index < attachingObjectsArr.Length; index++)
            {
                int attachedObjectNum = attachingObjectsArr[index];
                if (attachedObjectNum > deletedObjectNum)
                {
                    attachingObjectsArr[index] = attachedObjectNum - 1;
                }
            }
            attachedObjects?.SetValue(string.Join(" ", attachingObjectsArr));
        }

        public void ChangeAttachedObjectsAfterMove(int oldNum, int newNum)
        {
            foreach (var obj in TechObjects)
            {
                ChangeAttachedObjectAfterMove(obj.AttachedObjects, oldNum,
                    newNum);

                if (obj.BaseTechObject?.ObjectGroupsList != null)
                {
                    foreach (var group in obj.BaseTechObject.ObjectGroupsList)
                    {
                        ChangeAttachedObjectAfterMove(group, oldNum, newNum);
                    }
                }
            }
        }

        /// <summary>
        /// Изменение привязки объекта при перемещении объекта по дереву
        /// </summary>
        /// <param name="attachedObjects">Элемент с объектами</param>
        /// <param name="oldNum">Старый номер объекта</param>
        /// <param name="newNum">Новый номер объекта</param>
        private void ChangeAttachedObjectAfterMove(
            AttachedObjects attachedObjects, int oldNum, int newNum)
        {
            string attachingObjectsStr = attachedObjects.Value;
            string[] attachingObjectsArr = attachingObjectsStr.Split(' ');
            for (int index = 0; index < attachingObjectsArr.Length; index++)
            {
                if (attachingObjectsArr[index] == newNum.ToString())
                {
                    attachingObjectsArr[index] = oldNum.ToString();
                }
                else if (attachingObjectsArr[index] == oldNum.ToString())
                {
                    attachingObjectsArr[index] = newNum.ToString();
                }
            }
            attachedObjects.SetValue(string.Join(" ", attachingObjectsArr));
        }

        public void RemoveAttachingToObjects(TechObject techObject)
        {
            int objNum = TechObjectManager.GetInstance()
                .GetTechObjectN(techObject);
            foreach (var obj in TechObjects)
            {
                RemoveAttachingToObject(obj.AttachedObjects, objNum);

                if (obj.BaseTechObject?.ObjectGroupsList != null)
                {
                    foreach (var group in obj.BaseTechObject.ObjectGroupsList)
                    {
                        RemoveAttachingToObject(group, objNum);
                    }
                }
            }
        }

        /// <summary>
        /// Удалить объект из привязки объекта
        /// </summary>
        /// <param name="attachedObjects">Элемент с привязанными объектами
        /// </param>
        /// <param name="objNum">Номер удаляемого объекта</param>
        private void RemoveAttachingToObject(AttachedObjects attachedObjects,
            int objNum)
        {
            if (attachedObjects is null || attachedObjects.Value == string.Empty)
            {
                return;
            }

            List<int> attachedObjectsNums = attachedObjects.Value.Split(' ')
                .Select(int.Parse).ToList();
            if (attachedObjectsNums.Contains(objNum))
            {
                attachedObjectsNums.Remove(objNum);
                attachedObjects
                    .SetNewValue(string.Join(" ", attachedObjectsNums));
            }
        }
        #endregion

        /// <summary>
        /// Вставка базового объекта в редактор по LUA-имени базового объекта
        /// </summary>
        /// <param name="luaName"></param>
        public void InsertBaseObject(string luaName)
        {
            var baseObjectManager = BaseTechObjectManager.GetInstance();
            BaseTechObject foundBaseObject = baseObjectManager
                .GetTechObjectCopy(luaName);
            if (foundBaseObject != null)
            {
                string baseObjectTypeName = baseObjectManager
                    .GetS88Name(foundBaseObject.S88Level);
                var treeItem = GetTreeItem(baseObjectTypeName);
                if (treeItem != null)
                {
                    if (treeItem is S88Object s88Obj)
                    {
                        s88Obj.Insert(foundBaseObject.Name);
                    }
                    else
                    {
                        treeItem.Insert();
                    }

                    Editor.Editor.GetInstance().RefreshEditor();
                }
            }
        }

        /// <summary>
        /// Изменить базовый объект у объекта
        /// </summary>
        /// <param name="techObject"></param>
        public bool ChangeBaseObject(TechObject techObject)
        {
            try
            {
                techObject.ResetBaseTechObject();
                AddUnidentifiedObject(techObject);
                return true;
            }
            catch
            {
                return false;
            }

            // TODO: Создать форму, которая поможет изменить базовый объект
            // указывая различия
        }

        /// <summary>
        /// Объект мастера проекта.
        /// </summary>
        public TechObject ProcessCellObject
        {
            get
            {
                var masterObject = treeObjects.Where(x => x is ProcessCell)
                    .FirstOrDefault() as ProcessCell;
                if (masterObject != null)
                {
                    return masterObject.ProcessCellObject;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Получить количество аппаратов в проекте
        /// </summary>
        public int UnitsCount
        {
            get
            {
                int unitS88Level = (int)BaseTechObjectManager.ObjectType.Unit;
                var unitsCount = TechObjects
                    .Where(x => x.BaseTechObject != null)
                    .Where(x => x.BaseTechObject.S88Level == unitS88Level)
                    .Count();
                return unitsCount;
            }
        }

        /// <summary>
        /// Получить количество агрегатов в проекте
        /// </summary>
        public int EquipmentModulesCount
        {
            get
            {
                int aggregateS88Level = (int)BaseTechObjectManager.ObjectType
                    .Aggregate;
                var aggregatesCount = TechObjects
                    .Where(x => x.BaseTechObject != null)
                    .Where(x => x.BaseTechObject.S88Level == aggregateS88Level)
                    .Count();
                return aggregatesCount;
            }
        }

        /// <summary>
        /// Имя проекта.
        /// </summary>
        private string ProjectName { get;set; }

        /// <summary>
        /// Список типовых тех.объектов в дереве
        /// </summary>
        public List<GenericTechObject> GenericTechObjects => genericTechObjects;
        
        /// <summary>
        /// Список всех технологических объектов в дереве.
        /// </summary>
        public List<TechObject> TechObjects
        {
            get
            {
                return techObjects;
            }
        }


        /// <summary>
        /// Единственный объект менеджера объектов.
        /// </summary>
        private static TechObjectManager instance;

        /// <summary>
        /// Список объектов дерева.
        /// </summary>
        private List<ITreeViewItem> treeObjects;

        /// <summary>
        /// Список типовых тех. объектов в дереве.
        /// </summary>
        private readonly List<GenericTechObject> genericTechObjects;
        
        /// <summary>
        /// Список всех технологических объектов в дереве.
        /// </summary>
        private List<TechObject> techObjects;

        /// <summary>
        /// Экземпляр LUA.
        /// </summary>
        private Lua lua;

        /// <summary>
        /// Класс для проверки технологических объектов.
        /// </summary>
        private TechObjectChecker techObjectManagerChecker;

        /// <summary>
        /// Класс, генерирующий информацию для базы каналов по объектам
        /// </summary>
        private TechObjectXMLMaker techObjectXMLMaker;
    }
}