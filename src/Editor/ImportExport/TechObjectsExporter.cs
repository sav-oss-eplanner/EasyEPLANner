using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TechObject;

namespace Editor
{
    /// <summary>
    /// Класс, экспортирующий описание объектов проекта.
    /// </summary>
    public class TechObjectsExporter
    {
        private TechObjectsExporter() 
        {
            techObjectManager = TechObjectManager.GetInstance();
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static TechObjectsExporter GetInstance()
        {
            if (techObjectsExporter == null)
            {
                techObjectsExporter = new TechObjectsExporter();
            }
            return techObjectsExporter;
        }

        /// <summary>
        /// Глобальный список объектов.
        /// </summary>
        public List<ITreeViewItem> Objects
        {
            get
            {
                var items = new List<ITreeViewItem>();
                foreach(var item in techObjectManager.TechObjects)
                {
                    items.Add(item);
                }

                return items;
            }
        }

        /// <summary>
        /// Список объектов корня дерева объектов в редакторе.
        /// </summary>
        public ITreeViewItem[] RootItems
        {
            get
            {
                return (techObjectManager as ITreeViewItem).Items;
            }
        }

        /// <summary>
        /// Название проекта
        /// </summary>
        public string ProjectName
        {
            get
            {
                return (techObjectManager as ITreeViewItem).DisplayText[0];
            }
        }

        /// <summary>
        /// Экспорт описания проекта в файл.
        /// </summary>
        /// <param name="path">Место сохранения</param>
        /// <param name="objectsNums">Список (номера) экспортируемых
        /// объектов</param>
        public void Export(string path, List<int> objectsNums)
        {
            var objectsDescription = new StringBuilder();
            List<TechObject.TechObject> objects = techObjectManager.TechObjects;
            objectsDescription.Append("init_tech_objects_modes = function()\n");
            objectsDescription.Append("\treturn\n");
            objectsDescription.Append("\t{\n");
            foreach (var obj in objects)
            {
                int globalNum = techObjectManager.GetTechObjectN(obj);
                bool needExporting = objectsNums.Contains(globalNum);
                if (needExporting)
                {
                    string attachedObjectsRefs =
                        AttachedObjectReferences.FromIndices(techObjectManager,
                            obj.AttachedObjects.Value);

                    TechObject.TechObject exportingObject = obj.Clone(
                        techObjectManager.GetTechObjectN, globalNum, globalNum);
                    exportingObject.AttachedObjects.SetValue("");

                    string description = exportingObject.SaveAsLuaTable("\t\t",
                        globalNum, attachedObjectsRefs);
                    description = description
                        .Replace("[ 0 ]", $"[ {globalNum} ]");
                    objectsDescription.Append(description);
                }
                else
                {
                    objectsDescription.Append($"\t[ {globalNum} ] = {{0}},\n");
                }
            }
            objectsDescription.Append("\t}\n");
            objectsDescription.Append("end");

            objectsDescription = objectsDescription.Replace("\t", "    ");

            try
            {
                var fileWriter = new StreamWriter(path, false, 
                    EasyEPlanner.EncodingDetector.DetectFileEncoding(path));
                
                fileWriter.Write(objectsDescription);
                
                fileWriter.Flush();
                fileWriter.Close();
            }
            catch
            {
                throw new Exception("Ошибка записи в файл при экспорте");
            }            
        }

        private static TechObjectsExporter techObjectsExporter;
        private ITechObjectManager techObjectManager;
    }
}
