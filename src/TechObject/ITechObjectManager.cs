using EasyEPlanner.FileSavers.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TechObject
{
    /// <summary>
    /// Интерфейс класса TechObjectManager
    /// </summary>
    public interface ITechObjectManager
    {
        /// <summary>
        /// Загрузка описания проекта из строки Lua
        /// </summary>
        /// <param name="LuaStr">Строка с описанием</param>
        /// <param name="projectName">Имя проекта</param>
        void LoadDescription(string LuaStr, string projectName);

        /// <summary>
        /// Загрузка ограничений проекта
        /// </summary>
        /// <param name="LuaStr">Описание ограничений</param>
        void LoadRestrictions(string LuaStr);

        /// <summary>
        /// Сохранить описание проекта
        /// </summary>
        /// <param name="prefixStr">Отступ</param>
        /// <returns></returns>
        string SaveAsLuaTable(string prefixStr);

        /// <summary>
        /// Сохранить ограничения проекта
        /// </summary>
        /// <param name="prefixStr">Отступ</param>
        /// <returns></returns>
        string SaveRestrictionAsLua(string prefixStr);

        /// <summary>
        /// Список типовых объектов
        /// </summary>
        List<GenericTechObject> GenericTechObjects { get; }

        /// <summary>
        /// Глобальный список объектов
        /// </summary>
        List<TechObject> TechObjects { get; }

        /// <summary>
        /// Получить объект по его глобальному номеру
        /// </summary>
        /// <param name="globalNum">Глобальный номер</param>
        /// <returns></returns>
        TechObject GetTObject(int globalNum);

        /// <summary>
        /// Получить типовой объект по номеру
        /// </summary>
        /// <param name="globalNum">Номер типового объекта</param>
        GenericTechObject GetGenericTObject(int globalNum);

        int GetGenericObjectN(object techObject);

        /// <summary>
        /// Получить глобальный номер объекта
        /// </summary>
        /// <param name="techObject">Объект</param>
        /// <returns></returns>
        int GetTechObjectN(object techObject);

        /// <summary>
        /// Получить номер объекта по его отображаемому имени в дереве.
        /// </summary>
        /// <param name="displayText">Отображаемый текст</param>
        /// <returns></returns>
        int GetTechObjectN(string displayText);

        /// <summary>
        /// Получить номер объекта по названию базового объекта, ОУ и тех. номеру
        /// </summary>
        /// <param name="baseObjectName"> Название базового объекта </param>
        /// <param name="nameEplan"> ОУ </param>
        /// <param name="techNumber"> тех. номер </param>
        int GetTechObjectN(string baseObjectName, string nameEplan, int techNumber);

        /// <summary>
        /// Получить номер объекта по названию базового объекта и тех. номеру
        /// </summary>
        /// <param name="baseObjectName"> Название базового объекта </param>
        /// <param name="techNumber"> тех. номер </param>
        int GetTechObjectN(string baseObjectName, int techNumber);

        /// <summary>
        /// Получить номер объекта по названию базового объекта, тех. типу и тех. номеру
        /// </summary>
        /// <param name="baseObjectName"> Название базового объекта </param>
        /// <param name="techType"> тех. тип </param>
        /// <param name="techNumber"> тех. номер </param>
        int GetTechObjectN(string baseObjectName, int techType, int techNumber);

        /// <summary>
        /// Получить номер смежного объекта: по типу целевого объекта и искомому тех.номеру
        /// </summary>
        /// <param name="targetObjectIndex">Целевой объект</param>
        /// <param name="techNumber">Технологический номер</param>
        /// <returns>
        /// Смежный объект, если таковой имеется, иначе целевой объект
        /// </returns>
        int TypeAdjacentTObjectIdByTNum(int targetObjectIndex, int techNumber);

        /// <summary>
        /// Импорт объекта в проект.
        /// </summary>
        /// <param name="importingObject">Объект для импорта</param>
        void ImportObject(TechObject importingObject);

        /// <summary>
        /// Восстановить привязанные агрегаты по переносимым ссылкам.
        /// </summary>
        void ApplyAttachedObjectsReferences(
            IEnumerable<TechObject> objects = null);

        /// <summary>
        /// Получить описание объектов для базы каналов
        /// </summary>
        /// <param name="root">Дерево</param>
        /// <param name="combineTags">Группировать тэги в один подтип</param>
        /// <param name="useNewNames">Использовать имена объектов вместо OBJECT
        /// </param>
        void GetObjectForXML(IDriver root, bool combineTags, bool useNewNames);

        /// <summary>
        /// Вставить базовый объект в редактор
        /// </summary>
        /// <param name="luaName">Lua-имя объекта</param>
        void InsertBaseObject(string luaName);

        /// <summary>
        /// Количество аппаратов в проекте
        /// </summary>
        int UnitsCount { get; }

        /// <summary>
        /// Количество агрегатов в проекте
        /// </summary>
        int EquipmentModulesCount { get; }

        /// <summary>
        /// Проверка объектов
        /// </summary>
        /// <returns></returns>
        string Check();

        /// <summary>
        /// Синхронизация устройств в объектах;
        /// </summary>
        /// <param name="indexArray">Индексная таблица</param>
        void Synch(int[] indexArray);

        /// <summary>
        /// Проверка и исправление ограничений при удалении/перемещении объекта
        /// </summary>
        void CheckRestriction(int oldNum, int newNum);

        /// <summary>
        /// Изменение привязки объектов при удалении объекта из дерева
        /// </summary>
        /// <param name="deletedObjectNum">Номер удаленного объекта</param>
        void ChangeAttachedObjectsAfterDelete(int deletedObjectNum);

        /// <summary>
        /// Изменение привязки объектов при перемещении объекта по дереву
        /// </summary>
        /// <param name="newNum">Новый глобальный номер объекта</param>
        /// <param name="oldNum">Старый глобальный номер объекта</param>
        void ChangeAttachedObjectsAfterMove(int oldNum, int newNum);

        /// <summary>
        /// Удалить этот объект из привязки объектов
        /// </summary>
        /// <param name="techObject">Объект</param>
        void RemoveAttachingToObjects(TechObject techObject);

        /// <summary>
        /// Изменить базовый объект (первая реализация - сбросить)
        /// </summary>
        /// <param name="techObject">Базовый объект который надо изменить
        /// </param>
        /// <returns>Результат изменения, true - успех, false - нет</returns>
        bool ChangeBaseObject(TechObject techObject);
    }
}
