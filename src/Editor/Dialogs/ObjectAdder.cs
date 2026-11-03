using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace Editor
{
    [ExcludeFromCodeCoverage]
    public partial class ObjectsAdder : Form
    {
        public ObjectsAdder()
        {
            InitializeComponent();
            baseTechObjectsManager = TechObject.BaseTechObjectManager
                .GetInstance();
            LastSelectedType = null;
            LastSelectedSubType = null;
            FillObjectTypesListView();
        }

        /// <summary>
        /// Конструктор, если известен выбранный тип объекта.
        /// </summary>
        /// <param name="typeName">тип объекта, имя</param>
        public ObjectsAdder(string typeName)
        {
            InitializeComponent();
            baseTechObjectsManager = TechObject.BaseTechObjectManager
                .GetInstance();
            LastSelectedType = typeName;
            LastSelectedSubType = null;
            FillObjectTypesListView();
            SetUpObjectTypesListView();
        }
        
        /// <summary>
        /// Заполнить список с базовыми объектами (типами объектов)
        /// </summary>
        private void FillObjectTypesListView()
        {
            var s88Levels = baseTechObjectsManager.Objects
                .Select(x => x.S88Level).Distinct().ToList();
            var baseObjectsTypes = new List<string>();
            foreach (var s88level in s88Levels)
            {
                baseObjectsTypes
                    .Add(baseTechObjectsManager.GetS88Name(s88level));
            }

            objectTypes.Items.AddRange(baseObjectsTypes.ToArray());
        }

        /// <summary>
        /// Настройка дерева с типами объектов.
        /// </summary>
        private void SetUpObjectTypesListView()
        {
            int index = objectTypes.FindString(LastSelectedType);
            objectTypes.SetSelected(index, true);
            objectTypes.Enabled = false;
        }

        private void acceptButton_Click(object sender, EventArgs e)
        {
            bool incorrectInput =
                (objectSubTypes.Enabled && objectSubTypes.SelectedIndex == -1 && objectSubTypes.Items.Count != 1) ||
                (objectTypes.Enabled && objectTypes.SelectedIndex == -1);
            if (incorrectInput)
            {
                string message = "Ошибка. Выберите значения в списке(-ах)";
                MessageBox.Show(message, "Внимание", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                LastSelectedSubType = objectSubTypes.SelectedItem?.ToString() 
                    ?? objectSubTypes.Items.Cast<object>().First().ToString();
                LastSelectedType = objectTypes.SelectedItem.ToString();
                Close();
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            LastSelectedType = null;
            LastSelectedSubType = null;
            Close();
        }

        private void ObjectTypes_SelectedValueChanged(object sender, 
            EventArgs e)
        {
            objectSubTypes.ClearSelected();
            objectSubTypes.Items.Clear();

            if (objectTypes.SelectedItem is not null)
            {
                string selectedType = objectTypes.SelectedItem.ToString();
                int level = baseTechObjectsManager.GetS88Level(selectedType);
                var subTypes = baseTechObjectsManager.Objects
                    .Where(x => x.S88Level == level && !x.Deprecated)
                    .Select(x => x.Name).ToArray();
                objectSubTypes.Items.AddRange(subTypes);
            }
        }

        /// <summary>
        /// Сброс выбранных типов и подтипов.
        /// </summary>
        public static void Reset()
        {
            LastSelectedSubType = null;
            LastSelectedType = null;
        }

        /// <summary>
        /// Установленное значение типа (Ячейка процесса, Аппарат, Агрегат)
        /// </summary>
        public static string LastSelectedType { get; set; }

        /// <summary>
        /// Установленное значение подтипа (Танк и др.)
        /// </summary>
        public static string LastSelectedSubType { get; set; }

        TechObject.IBaseTechObjectManager baseTechObjectsManager;
    }
}
