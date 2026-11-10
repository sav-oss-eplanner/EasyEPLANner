using LuaInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using EasyEPlanner;

namespace TechObject
{
    public interface IBaseTechObjectsLoader
    {
        /// <summary>
        /// Загрузить описание базовых объектов в менеджер базовых объектов
        /// </summary>
        /// <param name="baseTechObjectManager">Менеджер, в который будет
        /// прозводиться загрузка объектов</param>
        void LoadTo(IBaseTechObjectManager baseTechObjectManager);
    }

    /// <summary>
    /// Загрузчик описания базовых объектов
    /// </summary>
    public class BaseTechObjectLoader : IBaseTechObjectsLoader
    {
        public BaseTechObjectLoader()
        {
            string addObjectMethodName = "AddBaseObject";
            string addPackageMethotdName = "AddPackage";
            lua = new Lua();
            lua.RegisterFunction(addObjectMethodName, this,
                GetType().GetMethod(addObjectMethodName));
            lua.RegisterFunction(addPackageMethotdName, this,
                GetType().GetMethod(addPackageMethotdName));
        }

        public void LoadTo(IBaseTechObjectManager baseTechObjectManager)
        {
            this.baseTechObjectManager = baseTechObjectManager;
            LoadBaseTechObjectsFromDescription();
        }

        /// <summary>
        /// Загрузка базовых объектов из их файлов описания
        /// </summary>
        private void LoadBaseTechObjectsFromDescription()
        {
            string sysFilesPath = ProjectManager.GetInstance()
                .OriginalSystemFilesPath;
            InitBaseTechObjectsInitializer(sysFilesPath);

            string pathToDir = Path.Combine(sysFilesPath, defaultDirName);
            IList<string> fileNames = GetDescriptionFilesNames(pathToDir);
            if (fileNames.Count > 0)
            {
                foreach(var fileName in fileNames)
                {
                    string descriptionFilePath = Path.Combine(pathToDir,
                        fileName);
                    string description = LoadBaseTechObjectsDescription(
                        descriptionFilePath);
                    InitBaseObjectsFromLuaScript(description);
                }
            }
            else
            {
                WriteDefaultObjectsDescriptionTemplate(pathToDir);
                MessageBox.Show("Файлы с описанием базовых объектов не " +
                    "найдены. Будет создан пустой файл с шаблоном описания. " +
                    $"Путь к каталогу: {pathToDir}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Инициализация читателя базовых объектов.
        /// </summary>
        private void InitBaseTechObjectsInitializer(string pathToSystemFiles)
        {
            string fileName = "sys_base_objects_initializer.lua";
            string pathToFile = Path.Combine(pathToSystemFiles, fileName);
            lua.DoFile(pathToFile);
        }

        /// <summary>
        /// Получить имена файлов с описанием объектов
        /// </summary>
        /// <param name="pathToCatalog">Путь к каталогу с файлами</param>
        /// <returns></returns>
        private string[] GetDescriptionFilesNames(string pathToCatalog)
        {
            if (Directory.Exists(pathToCatalog))
            {
                return Directory.GetFiles(pathToCatalog);
            }
            else
            {
                Directory.CreateDirectory(pathToCatalog);
                return new string[0];
            }
        }

        /// <summary>
        /// Загрузить описание базовых объектов из файла
        /// </summary>
        /// <param name="pathToFile">Путь к файлу с базовыми объектами</param>
        /// <returns>Описание</returns>
        private string LoadBaseTechObjectsDescription(string pathToFile)
        {
            using (var reader = new StreamReader(pathToFile,
                EncodingDetector.DetectFileEncoding(pathToFile)))
            {
                string readedDescription = reader.ReadToEnd();
                return readedDescription;
            }
        }

        /// <summary>
        /// Инициализация загруженных базовых объектов через Lua
        /// </summary>
        /// <param name="luaString">Скрипт с описанием</param>
        private void InitBaseObjectsFromLuaScript(string luaString)
        {
            lua.DoString("base_tech_objects = nil");
            bool scriptLoaded = LoadBaseObjectsDescriptionToLua(luaString);
            if (scriptLoaded)
            {
                InitBaseObjectsDescription();
            }
        }

        /// <summary>
        /// Загрузка скрипта с базовыми объектами с экземпляр Lua
        /// </summary>
        /// <param name="descriptionScript">Скрипт с описанием</param>
        /// <returns></returns>
        private bool LoadBaseObjectsDescriptionToLua(string descriptionScript)
        {
            try
            {
                lua.DoString(descriptionScript);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ". Исправьте скрипт вручную.",
                    "Ошибка обработки Lua-скрипта с описанием " +
                    "базовых объектов.");
                return false;
            }
        }

        /// <summary>
        /// Инициализация загруженных базовых объектов из Lua
        /// </summary>
        private void InitBaseObjectsDescription()
        {
            try
            {
                string script = "if init_base_objects ~= nil " +
                    "then init_base_objects() end";
                lua.DoString(script);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обработки Lua-скрипта с " +
                    " инициализацией базовых объектов: " + ex.Message + ".\n" +
                    "Source: " + ex.Source);
            }
        }

        /// <summary>
        /// Записать стандартный файл описания.
        /// </summary>
        /// <param name="pathToDir">Путь к каталогу, где хранятся
        /// файлы описания</param>
        private void WriteDefaultObjectsDescriptionTemplate(string pathToDir)
        {
            string templateDescriptionFilePath = Path.Combine(pathToDir,
                defaultFileName);
            string template = EasyEPlanner.Properties.Resources
                .ResourceManager
                .GetString("SysBaseObjectsDescriptionPattern");
            File.WriteAllText(templateDescriptionFilePath, template,
                EncodingDetector.UTF8);
        }

        /// <summary>
        /// Добавить базовый объект в менеджер объектов, делегирует.
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="eplanName">Имя в eplan</param>
        /// <param name="s88Level">Уровень по ISA88</param>
        /// <param name="basicName">Базовое имя для функциональности</param>
        /// <param name="bindingName">Имя для привязки к объекту</param>
        /// <param name="isPID">Является ли объект ПИД-регулятором</param>
        /// <param name="luaModuleName">Имя модуля Lua для объекта</param>
        /// <param name="monitorName">Имя объекта Monitor (SCADA)</param>
        /// <param name="deprecated">Устарел объект, или нет</param>
        /// <returns>Базовый объект в экземпляр LUA</returns>
        public BaseTechObject AddBaseObject(string name, string eplanName,
            int s88Level, string basicName, string bindingName, bool isPID,
            string luaModuleName, string monitorName, bool deprecated,
            bool denyBindingToUnit, int techType = 2,
            string defaultNameEplan = "TANK", bool isSingleInstance = false)
        {
            return baseTechObjectManager.AddBaseObject(name, eplanName,
                s88Level, basicName, bindingName, isPID, luaModuleName,
                monitorName, deprecated, denyBindingToUnit, techType,
                defaultNameEplan, isSingleInstance);
        }

        public void AddPackage(string package)
        {
            ProjectDescriptionSaver.AddPackage(package);
        }

        const string defaultFileName = "DescriptionTemplate.lua";
        const string defaultDirName = "BaseObjectsDescriptionFiles";
        private Lua lua;
        IBaseTechObjectManager baseTechObjectManager;
    }
}
