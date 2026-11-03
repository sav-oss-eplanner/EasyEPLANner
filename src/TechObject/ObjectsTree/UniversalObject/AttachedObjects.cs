using BrightIdeasSoftware;
using Editor;
using IO;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using TechObject.AttachedObjectStrategy;

namespace TechObject
{
    /// <summary>
    /// Класс привязки объектов к объекту
    /// </summary>
    public class AttachedObjects : ObjectProperty
    {
        public AttachedObjects(string attachedObjects, TechObject owner,
            IAttachedObjectsStrategy strategy) :
            base(strategy.Name, attachedObjects)
        {
            this.owner = owner;
            this.Parent = owner;
            this.strategy = strategy;
            SetValue(attachedObjects);
        }

        public override bool SetNewValue(string newValue)
        {
            string oldValue = Value;
            var thisObjNum = owner?.GlobalNum ?? 0;
            var isGeneric = owner is GenericTechObject;
            List<int> newNumbers = strategy.GetValidTechObjNums(newValue, thisObjNum);

            newValue = string.Join(" ", newNumbers);
            string sortedValue = MoveNewValuesInTheEndOfString(newValue,
                oldValue);
            base.SetNewValue(sortedValue);

            if (strategy.UseInitialization)
            {
                var replacedObjects = new Dictionary<TechObject, TechObject>();

                List<TechObject> newObjects = newNumbers
                    .Select(x => owner.TechObjectManagerInstance.GetTObject(x))
                    .ToList();
                List<TechObject> oldObjects = strategy
                    .GetValidTechObjNums(oldValue, thisObjNum, isGeneric)
                    .Select(x => owner.TechObjectManagerInstance.GetTObject(x))
                    .ToList();
                bool bindToUnit = owner.BaseTechObject
                    .S88Level == (int)BaseTechObjectManager.ObjectType.Unit;
                if (bindToUnit)
                {
                    replacedObjects = FindReplacedObjects(oldObjects, newObjects);
                    ChangeOwnerInReplacedObjectsProperties(replacedObjects);
                }

                List<int> deletedAgregatesNumbers = FindDeletedObjects(
                    oldValue, newValue, replacedObjects);
                RemoveDeletedObjects(deletedAgregatesNumbers);

                InitAttachedObjects(newNumbers);
            }

            return true;
        }

        /// <summary>
        /// Переместить новые значения в конец строки
        /// </summary>
        /// <param name="newValue">Строка с новыми значениями</param>
        /// <param name="oldValue">Строка со старыми значениями</param>
        /// <returns></returns>
        private string MoveNewValuesInTheEndOfString(string newValue,
            string oldValue)
        {
            char charSeparator = ' ';
            List<string> oldValues = oldValue.Split(charSeparator).ToList();
            List<string> newValues = newValue.Split(charSeparator).ToList();
            
            if(oldValues.Count >= newValues.Count)
            {
                return newValue.Trim();
            }
            else
            {
                List<string> differenceInValues = newValues
                    .Except(oldValues).ToList();
                foreach(var diffValue in differenceInValues)
                {
                    oldValues.Add(diffValue);
                }

                string stringSeparator = " ";
                return string.Join(stringSeparator, oldValues).Trim();
            }
        }

        public override bool SetNewValue(IDictionary<int, List<int>> newDict)
        {
            return SetNewValue(string.Join(" ", newDict.Keys));
        }

        /// <summary>
        /// Проверить и найти замененные объекты
        /// </summary>
        /// <param name="oldObjects">Старый список объектов</param>
        /// <param name="newObjects">Новый список объектов</param>
        /// <returns></returns>
        private Dictionary<TechObject, TechObject> FindReplacedObjects(
            List<TechObject> oldObjects, List<TechObject> newObjects)
        {
            var replacedObjects = new Dictionary<TechObject, TechObject>();

            foreach (var newObj in newObjects)
            {
                var replacedObject = oldObjects
                    .Where(x => x.BaseTechObject.Name ==
                    newObj.BaseTechObject.Name &&
                    x.GetLocalNum != newObj.GetLocalNum)
                    .FirstOrDefault();
                if (replacedObject != null)
                {
                    replacedObjects.Add(replacedObject, newObj);
                }
            }

            return replacedObjects;
        }

        /// <summary>
        /// Заменить владельца в замененных объектах
        /// </summary>
        /// <param name="replacedObjects">Замененные объекты</param>
        private void ChangeOwnerInReplacedObjectsProperties(
            Dictionary<TechObject, TechObject> replacedObjects)
        {
            if (replacedObjects.Count == 0)
            {
                return;
            }

            foreach (var objects in replacedObjects)
            {
                TechObject oldObject = objects.Key;
                TechObject newObject = objects.Value;

                foreach (var mode in owner.ModesManager.Modes)
                {
                    mode.BaseOperation.ChangePropertiesOwner(
                        oldObject.BaseTechObject, newObject.BaseTechObject);
                }
            }
        }

        /// <summary>
        /// Найти удаленные объекты
        /// </summary>
        /// <param name="oldValue">Старое значение поля</param>
        /// <param name="newValue">Новое значение поля</param>
        /// <param name="replacedObjects">Словарь замененных объектов.
        /// Ключ - старый объект, значение - новый объект.</param>
        /// <returns></returns>
        private List<int> FindDeletedObjects(string oldValue,
            string newValue,
            Dictionary<TechObject, TechObject> replacedObjects)
        {
            var oldNumbers = new List<int>();
            var newNumbers = new List<int>();
            if (oldValue != null && oldValue != string.Empty)
            {
                oldNumbers = oldValue.Split(' ').Select(int.Parse).ToList();
            }
            if (newValue != null && newValue != string.Empty)
            {
                newNumbers = newValue.Split(' ').Select(int.Parse).ToList();
            }

            foreach (var newNum in newNumbers)
            {
                if (oldNumbers.Contains(newNum))
                {
                    oldNumbers.Remove(newNum);
                }
            }

            if (replacedObjects != null)
            {
                foreach (var replacedObj in replacedObjects.Keys)
                {
                    var replacedObjNum = TechObjectManager.GetInstance()
                        .GetTechObjectN(replacedObj);
                    if (oldNumbers.Contains(replacedObjNum))
                    {
                        oldNumbers.Remove(replacedObjNum);
                    }
                }
            }

            return oldNumbers;
        }

        /// <summary>
        /// Удалить привязку объекта к объекту.
        /// </summary>
        /// <param name="objectNumbers">Список объектов</param>
        private void RemoveDeletedObjects(List<int> objectNumbers)
        {
            if (objectNumbers.Count == 0)
            {
                return;
            }

            foreach (var number in objectNumbers)
            {
                TechObject removingObject = Owner
                    .TechObjectManagerInstance.GetTObject(number);
                BaseTechObject removingBaseTechObject = removingObject
                    .BaseTechObject;
                List<BaseParameter> properties = removingBaseTechObject
                    .AggregateParameters;

                var deletingProperties = new List<BaseParameter>();
                if (properties.Count != 0)
                {
                    deletingProperties.AddRange(properties);
                }

                if (removingBaseTechObject.MainAggregateParameter != null)
                {
                    deletingProperties.Add(removingBaseTechObject
                        .MainAggregateParameter);
                }
                TechObject thisTechObject = owner;
                List<Mode> modes = thisTechObject.ModesManager.Modes;
                foreach (var mode in modes)
                {
                    mode.BaseOperation.RemoveProperties(deletingProperties);
                }
            }
        }

        /// <summary>
        /// Инициализация данных 
        /// </summary>
        /// <param name="objectsNumbrers">Список корректных номеров
        /// привязанных объектов</param>
        private void InitAttachedObjects(List<int> objectsNumbrers)
        {
            foreach (var number in objectsNumbrers)
            {
                TechObject attachedAggregate = owner.TechObjectManagerInstance
                    .GetTObject(number);
                BaseTechObject attachedBaseTechObject = attachedAggregate
                    .BaseTechObject;
                List<BaseParameter> properties = attachedBaseTechObject
                    .AggregateParameters;

                var addingProperties = new List<BaseParameter>();
                if (attachedBaseTechObject.MainAggregateParameter != null)
                {
                    addingProperties.Add(attachedBaseTechObject
                        .MainAggregateParameter);
                }

                if (properties.Count != 0)
                {
                    addingProperties.AddRange(properties);
                }

                TechObject thisThechObject = owner;
                List<Mode> modes = thisThechObject.ModesManager.Modes;
                foreach (var mode in modes)
                {
                    if (mode.BaseOperation.Name == string.Empty)
                    {
                        continue;
                    }

                    mode.BaseOperation.AddProperties(addingProperties,
                        attachedBaseTechObject);
                    mode.BaseOperation.Check();
                }
            }
        }

        /// <summary>
        /// Проверить привязанные объекты на инициализацию и 
        /// инициализировать, если не инициализировано.
        /// </summary>
        public string CheckInit()
        {
            var res = "";
            var objNum = TechObjectManager.GetInstance().GetTechObjectN(Owner);
            List<int> numbers = strategy.GetValidTechObjNums(Value, objNum);
            string checkedValue = string.Join(" ", numbers);
            if (checkedValue != Value)
            {
                int objGlobalNumber = TechObjectManager.GetInstance()
                    .GetTechObjectN(owner);
                res += $"Проверьте {Name.ToLower()} в объекте: " +
                    $"{objGlobalNumber}." +
                    $"{owner.Name + owner.TechNumber}. " +
                    $"В поле присутствуют агрегаты, которые нельзя " +
                    $"привязывать.\n";
            }

            InitAttachedObjects(numbers);
            return res;
        }

        /// <summary>
        /// Проверка правильности привязки
        /// </summary>
        public string CheckBindingToUnit()
        {
            if (owner.BaseTechObject.S88Level != (int)BaseTechObjectManager.ObjectType.Unit)
                return string.Empty;

            var denyBindingBaseObjects = string.Join(", ",
                    strategy.GetValidTechObjNums(Value, Owner.GlobalNum)
                        .Select(objN => techObjectManager.GetTObject(objN).BaseTechObject)
                        .Where(obj => obj.DenyBindingToUnit)
                        .Select(obj => obj.Name)
                        .Distinct());

            if (string.IsNullOrEmpty(denyBindingBaseObjects))
                return string.Empty;

            return $"К аппарату [ {Owner.Name} №{Owner.TechNumber} ] нельзя привязывать базовые объекты типа: {denyBindingBaseObjects}\n";
        }


        private string GenerateAttachedObjectsString(string attachedObjects)
        {
            if (attachedObjects == string.Empty)
            {
                return attachedObjects;
            }

            var objectNums = attachedObjects.Split(' ').Select(int.Parse);
            var objectNames = new List<string>();
            foreach (var objNum in objectNums)
            {
                TechObject findedObject = techObjectManager.GetTObject(objNum);
                if (findedObject != null)
                {
                    string name = $"\"{findedObject.Name} " +
                        $"{findedObject.TechNumber}\"";
                    objectNames.Add(name);
                }
            }

            return string.Join(", ", objectNames);
        }

        /// <summary>
        /// Получить список названий привязанных объектов
        /// </summary>
        /// <returns></returns>
        public List<string> GetAttachedObjectsName()
            => [.. GetTechObjects().Select(obj => obj.NameEplanForFile.ToLower() + obj.TechNumber)];


        /// <summary>
        /// Получить список привязанных объектов
        /// </summary>
        /// <returns></returns>
        public List<TechObject> GetTechObjects()
        {
            if (Value == string.Empty)
                return [];

            return [.. Value.Split(' ')
                .Where(n => int.TryParse(n, out _))
                .Select(int.Parse)
                .Select(techObjectManager.GetTObject)];
        }

        /// <summary>
        /// Получить базовый тех. объект привязанного объекта по его названию
        /// </summary>
        /// <param name="Luaname">Lua-название базового объекта</param>
        /// <returns></returns>
        public BaseTechObject GetBaseTechObjectByLuaName(string Luaname)
        {
            return GetTechObjects().Select(to => to.BaseTechObject)
                .FirstOrDefault(bto => bto.EplanName == Luaname);
        }

        /// <summary>
        /// Установить новые значения при вставке или замене при копировании.
        /// Проверяет подходящие для технологическгого объекта агрегаты.
        /// </summary>
        /// <param name="newValues"> Список глобальных номеров привязываемых агрегатов </param>
        private void SetNewValues(List<int> newValues)
        {
            // Исключение агрегатов одинакового типа для танка
            if (Owner.BaseTechObject.S88Level == (int)BaseTechObjectManager.ObjectType.Unit)
            {
                List<TechObject> newObjects = newValues
                    .Select(x => Owner.TechObjectManagerInstance.GetTObject(x))
                    .ToList();

                var usedTypes = new HashSet<string>();
                newValues = newObjects.Where(techObject => usedTypes.Add(techObject.BaseTechObject.Name))
                    .Select(techObject => techObject.GlobalNum).ToList();
            }

            // Исключение привязки агргата на самого себя
            if (Owner.BaseTechObject.S88Level == (int)BaseTechObjectManager.ObjectType.Aggregate)
            {
                newValues.Remove(this.Owner.GlobalNum);
            }

            SetNewValue(string.Join(" ", newValues));
        }

        #region реализация ITreeViewItem
        public override ITreeViewItem InsertCopy(object obj)
        {   
            if ( obj is AttachedObjects attachedObjects)
            {
                List<int> copyValues = strategy.GetValidTechObjNums(attachedObjects.Value,
                    attachedObjects.Owner.GlobalNum, attachedObjects.Owner is GenericTechObject);
                List<int> oldValues = strategy.GetValidTechObjNums(Value,
                    this.Owner.GlobalNum, Owner is GenericTechObject);

                List<int> newValues = oldValues.Union(copyValues).ToList();

                SetNewValues(newValues);
                return this;
            }

            return null;
        }

        public override ITreeViewItem Replace(object child, object copyObject)
        {
            if (copyObject is AttachedObjects copyAttachedObjects)
            {
                List<int> newValues = strategy.GetValidTechObjNums(copyAttachedObjects.Value,
                    copyAttachedObjects.Owner.GlobalNum, copyAttachedObjects.Owner is GenericTechObject);

                SetNewValues(newValues);
                return this;
            }

            return null;
        }

        public override object Copy()
        {
            return new AttachedObjects(Value, Owner, strategy);
        }

        public void UpdateOnGenericTechObject(AttachedObjects genericAttachedObjects)
        {
            var res = new List<int>();

            var oldValue = GetValueIndexes();
            var oldGenericValue = genericValue.Split(' ').Where(num => num != string.Empty).Select(int.Parse).ToList();

            var newGenericValue = genericAttachedObjects.Value
                .Split(' ').Where(num => num != string.Empty)
                .Select(int.Parse).Select(idx => techObjectManager.TypeAdjacentTObjectIdByTNum(idx, owner.TechNumber)).ToList();
           
            genericValue = string.Join(" ", newGenericValue);

            var op1 = oldValue.Except(oldGenericValue).ToList();
            var op2 = newGenericValue.Except(op1).ToList();
                
            var value = op1.Union(op2).ToList(); 


            if (value.Count <= 0)
            {
                SetNewValues(res);
                return;
            }

            SetNewValues(value);
        }

        public List<int> GetValueIndexes() 
            => Value.Split(' ').Where(num => num != string.Empty).Select(int.Parse).ToList();

        public void CreateGenericByTechObjects(IEnumerable<ITreeViewItem> itemList)
        {
            var attachedObjectsList = itemList.Cast<AttachedObjects>().ToList();

            var refTechNumber = attachedObjectsList[0].owner.TechNumber;

            SetNewValues(attachedObjectsList.Skip(1)
                .Aggregate(new HashSet<int>(attachedObjectsList[0].GetValueIndexes()
                .Select(idx => techObjectManager.TypeAdjacentTObjectIdByTNum(idx, refTechNumber))),
                    (h, e) => 
                    { 
                        h.IntersectWith(
                            e.GetValueIndexes()
                            .Select(idx => techObjectManager.TypeAdjacentTObjectIdByTNum(idx, refTechNumber)));
                        return h; 
                    }).ToList());
        }

        /// <summary>
        /// Обновление после удаления типового объекта
        /// </summary>
        public void UpdateOnDeleteGeneric()
        {
            SetNewValues(GetValueIndexes()
                .Concat(genericValue.Split(' ').Where(num => num != string.Empty).Select(int.Parse)).ToList());
            genericValue = "";
        }

        public override string[] DisplayText
        {
            get
            {
                return new string[]
                {
                        Name,
                        GenerateAttachedObjectsString(Value ?? string.Empty)
                };
            }
        }

        public override bool NeedRebuildParent
        {
            get
            {
                return true;
            }
        }

        public override int[] EditablePart
        {
            get
            {
                return new int[] { -1, -1 };
            }
        }

        override public bool IsEditable
        {
            get
            {
                if (owner == null) return true; 

                BaseTechObject baseTechObject = owner?.BaseTechObject;
                bool editable = baseTechObject?.IsAttachable == true;
                return editable;
            }
        }

        public override bool IsCopyable => true;

        public override bool IsReplaceable => true;

        public override bool IsInsertableCopy => true;

        public override bool IsDeletable
        {
            get
            {
                return true;
            }
        }

        public override bool Delete(object child)
        {
            bool cleared = SetNewValue(string.Empty);
            return cleared;
        }

        public override bool ShowWarningBeforeDelete
        {
            get
            {
                return true;
            }
        }

        [ExcludeFromCodeCoverage]
        public override IRenderer[] CellRenderer =>
            new IRenderer[] { null, GenericAttachedObjectsRenderer };

        /// <summary>
        /// Подсветка устройств из типового объекта
        /// </summary>
        [ExcludeFromCodeCoverage]
        private HighlightTextRenderer GenericAttachedObjectsRenderer
        {
            get
            {
                genericDevicesRenderer.Filter.ContainsStrings =
                    GenerateAttachedObjectsString(genericValue ?? string.Empty).Split(',').Select(obj => obj.Trim(' ', '"'));
                return genericDevicesRenderer;
            }
        }

        private readonly HighlightTextRenderer genericDevicesRenderer = new HighlightTextRenderer()
        {
            Filter = TextMatchFilter.Contains(Editor.Editor.GetInstance().EditorForm?.editorTView, string.Empty),
            FillBrush = new SolidBrush(Color.YellowGreen),
            FramePen = new Pen(Color.White),
        };
        #endregion

        public TechObject Owner
        {
            get
            {
                return owner;
            }
            set
            {
                owner = value;
            }
        }

        public IAttachedObjectsStrategy WorkStrategy 
        { 
            get => strategy; 
        }

        private TechObject owner;
        private IAttachedObjectsStrategy strategy;

        private static readonly ITechObjectManager techObjectManager = TechObjectManager.GetInstance();

        /// <summary>
        /// Значения объектов из типового объекта,
        /// Vlue так же содержит эти значения
        /// </summary>
        private string genericValue = "";
    }

    namespace AttachedObjectStrategy
    {
        /// <summary>
        /// Интерфейс стратегии фильтрации привязываемых объектов
        /// </summary>
        public interface IAttachedObjectsStrategy
        {
            /// <summary>
            /// Название поля
            /// </summary>
            string Name { get; set; }

            /// <summary>
            /// Lua-имя стратегии
            /// </summary>
            string LuaName { get; set; }

            /// <summary>
            /// Получить корректные номера технологических объектов из
            /// входной строки
            /// </summary>
            /// <param name="value">Входная строка</param>
            /// <param name="objNum">Номер редактируемого объекта</param>
            /// <param name="isGeneric">Редактируемый объект - типовой</param>
            /// <returns></returns>
            List<int> GetValidTechObjNums(string value, int objNum, bool isGeneric = false);

            /// <summary>
            /// Нужно ли инициализировать привязанные объекты
            /// </summary>
            bool UseInitialization { get; }

            List<BaseTechObjectManager.ObjectType> AllowedObjects { get; set; }
        }

        /// <summary>
        /// Стратегия для привязки объектов с их инициализацией в объекте.
        /// </summary>
        public class AttachedWithInitStrategy : BaseStrategy,
            IAttachedObjectsStrategy
        {
            public AttachedWithInitStrategy(string name, string luaName,
                List<BaseTechObjectManager.ObjectType> allowedObjects)
                : base(name, luaName, allowedObjects)
            { }

            public bool UseInitialization
            {
                get
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Стратегия для привязки объектов без их инициализации в объекте.
        /// </summary>
        public class AttachedWithoutInitStrategy : BaseStrategy,
            IAttachedObjectsStrategy
        {
            public AttachedWithoutInitStrategy(string name, string luaName,
                List<BaseTechObjectManager.ObjectType> allowedObjects)
                : base(name, luaName, allowedObjects)
            { }

            public bool UseInitialization
            {
                get
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Базовая стратегия проверки разрешенных объектов при привязке 
        /// объектов
        /// </summary>
        public abstract class BaseStrategy
        {
            public BaseStrategy(string name, string luaName,
                List<BaseTechObjectManager.ObjectType> allowedObjects)
            {
                Name = name;
                LuaName = luaName;
                AllowedObjects = allowedObjects;
            }

            /// <summary>
            /// Получить корректные номера технологических объектов из входной строки
            /// </summary>
            /// <remarks>
            /// Проверка на двустороннюю привязку. <br/>
            /// Если к привязываемым агргатам <paramref name="value"/> уже привязан <paramref name="selectedObjNum"/>, <br/>
            /// то исключаем такие агргаты. (Не касается типовых объектов)
            /// </remarks>
            /// <param name="value">Входная строка</param>
            /// <param name="selectedObjNum">Номер редактируемого объекта</param>
            /// <param name="isGeneric"><paramref name="selectedObjNum"/> - типовой объект</param>
            public List<int> GetValidTechObjNums(string value,
                int selectedObjNum, bool isGeneric = false)
            {
                var validNumbers = new List<int>();
                List<int> numbers = value.Split(' ')
                    .Where(n => int.TryParse(n, out _))
                    .Select(int.Parse)
                    .Where(num => num > 0).ToList();
                List<int> allowedObjectsNums = AllowedObjects?
                    .Select(objType => (int)objType).ToList() ?? new List<int>();

                foreach (var attachedObjectIndex in numbers)
                {
                    var attachedObject = techObjectManager.GetTObject(attachedObjectIndex);
                    if (attachedObject is null || attachedObject.BaseTechObject is null)
                        return new List<int>();

                    if (isGeneric || 
                        attachedObject.AttachedObjects.Value.Split(' ').Where(n => int.TryParse(n, out _))
                        .Select(int.Parse).Contains(selectedObjNum))
                        continue;

                    if (allowedObjectsNums.Contains(attachedObject.BaseTechObject.S88Level))
                        validNumbers.Add(attachedObjectIndex);
                }

                return validNumbers.Distinct().ToList();
            }

            /// <summary>
            /// Отображаемое имя группы
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Lua-имя группы
            /// </summary>
            public string LuaName { get; set; }

            /// <summary>
            /// Разрешенные для добавления в группу объекты
            /// </summary>
            public List<BaseTechObjectManager.ObjectType> AllowedObjects { get; set; }

            private readonly ITechObjectManager techObjectManager = TechObjectManager.GetInstance();
        }
    }
}
