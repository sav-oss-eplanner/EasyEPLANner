using EasyEPlanner.FileSavers.XML;
using EplanDevice;
using IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using TechObject;

namespace EasyEPlanner
{
    /// <summary>
    /// Класс, собирающий данные для экспорта в Excel
    /// </summary>
    public static class ExcelDataCollector
    {
        /// <summary>
        /// Сохранить операции и устройства технологических устройств 
        /// в виде дерева.
        /// </summary>
        /// <returns></returns>
        static public TreeView SaveTechObjectOperationsAndActionsAsTree()
        {
            var tree = new TreeView();
            foreach (var techObj in techObjectManager.TechObjects)
            {
                string techName = techObjectManager.GetTechObjectN(techObj)
                    .ToString() + ". " + techObj.EditText[0] + " " + 
                    techObj.TechNumber.ToString();
                var objectNode = new TreeNode(techName);
                objectNode.Tag = techObj;
                
                foreach (var mode in techObj.ModesManager.Modes)
                {
                    string modeName = techObj.ModesManager.GetModeN(mode)
                        .ToString() + ". " + mode.EditText[0];
                    var modeNode = new TreeNode();
                    SaveModesToTreeNodeForExcelExport(mode, modeName, modeNode);
                    objectNode.Nodes.Add(modeNode);
                }
                tree.Nodes.Add(objectNode);
            }
            return tree;
        }

        /// <summary>
        /// Сохранить операции для страницы 
        /// "Технологические операции и устройства"
        /// </summary>
        /// <param name="mode">Операция</param>
        /// <param name="modeName">Имя операции</param>
        /// <param name="modeNode">Узел дерева для сохранения</param>
        private static void SaveModesToTreeNodeForExcelExport(Mode mode,
            string modeName, TreeNode modeNode)
        {
            var res = new string[]
            {
                modeName,
                "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""
            };
            modeNode.Tag = res;

            foreach (var state in mode.States)
            {
                FillState(state, ref modeNode);
                FillSteps(state, ref modeNode);
            }
        }

        /// <summary>
        /// Заполнение информации о состоянии операции
        /// (шаг - Во время операции).
        /// </summary>
        /// <param name="state">Состояние операции</param>
        /// <param name="modeNode">узел дерева операции</param>
        private static void FillState(State state, ref TreeNode modeNode)
        {
            if (state.Empty == false)
            {
                modeNode.Nodes.Add(new TreeNode { Tag = state.SaveAsExcel() });
            }
        }

        /// <summary>
        /// Заполнение шагов операции (кроме шага "Во время операции").
        /// </summary>
        /// <param name="state">Состояние операции</param>
        /// <param name="modeNode">Узел дерева операции</param>
        private static void FillSteps(State state, ref TreeNode modeNode)
        {
            for (int i = 1; i < state.Steps.Count; i++)
            {
                Step step = state.Steps[i]; 
                modeNode.Nodes.Add(new TreeNode { Tag = step.SaveAsExcel() });
            }
        }

        /// <summary>
        /// Сохранить параметры технологических объектов в виде дерева.
        /// </summary>
        /// <returns></returns>
        static public TreeView SaveParamsAsTree()
        {
            var tree = new TreeView();
            foreach (var techObj in techObjectManager.TechObjects)
            {
                string techName = techObjectManager.GetTechObjectN(techObj)
                    .ToString() + ". " + techObj.EditText[0] + " " +
                    techObj.TechNumber.ToString();
                var objectNode = new TreeNode(techName);
                objectNode.Tag = techObj;

                foreach(Params paramsGroup in techObj.GetParamsManager().Items)
                {
                    var parTypeNode = new TreeNode(paramsGroup
                        .NameForChannelBase);
                    parTypeNode.Tag = paramsGroup.NameForChannelBase;
                    objectNode.Nodes.Add(parTypeNode);

                    int counter = 1;
                    foreach(Param param in paramsGroup.Items)
                    {
                        string parName = $"{counter}. {param.EditText[0]}";
                        var parNode = new TreeNode(parName);
                        counter++;
                        parNode.Tag = new string[]
                        {
                            parName,
                            param.GetValue(),
                            param.GetMeter(),
                            param.Operations,
                            param.GetNameLua()
                        };

                        parTypeNode.Nodes.Add(parNode);
                    }
                }

                tree.Nodes.Add(objectNode);
            }
            return tree;
        }

        /// <summary>
        /// Сохранить технологические объекты без действий в виде дерева.
        /// </summary>
        /// <returns></returns>
        static public TreeView SaveObjectsWithoutActionsAsTree()
        {
            var tree = new TreeView();

            bool objectTitleIsWrited = false;
            foreach (var techObj in techObjectManager.TechObjects)
            {
                if (objectTitleIsWrited == false)
                {
                    var titleNode = new TreeNode();
                    titleNode.Tag = new string[] 
                    { 
                        "Технологический объект",
                        "Номер", 
                        "ОУ", 
                        "Имя объекта Monitor" 
                    };
                    tree.Nodes.Add(titleNode);
                    objectTitleIsWrited = true;
                }

                string techName = techObjectManager.GetTechObjectN(techObj)
                    .ToString() + ". " + techObj.EditText[0] + " " + 
                    techObj.TechNumber.ToString();
                var objNode = new TreeNode(techName);
                objNode.Tag = new string[] 
                { 
                    techName, 
                    techObj.TechNumber.ToString(), 
                    techObj.NameEplan, 
                    techObj.GetNameForChannelBase() 
                };

                var modesNodes = new TreeNode("Операции");
                WriteObjectsOperationsInNode(ref modesNodes, techObj);

                var parametersNodes = new TreeNode("Параметры");
                WriteObjectParametersInNode(ref parametersNodes, techObj);

                if (modesNodes.Nodes.Count > 0)
                {
                    objNode.Nodes.Add(modesNodes);
                }

                if (parametersNodes.Nodes.Count > 0)
                {
                    objNode.Nodes.Add(parametersNodes);
                }

                tree.Nodes.Add(objNode);
            }
            return tree;
        }

        /// <summary>
        /// Записать операции объекта в узел дерева.
        /// </summary>
        /// <param name="modesNodes">Узел с операциями объекта</param>
        /// <param name="techObject">Объект</param>
        private static void WriteObjectsOperationsInNode(
            ref TreeNode modesNodes, TechObject.TechObject techObject)
        {
            foreach (var mode in techObject.ModesManager.Modes)
            {
                var modeNode = new TreeNode(mode.DisplayText[0]);
                modeNode.Tag = mode;
                WriteObjectOperationStepsInNode(ref modeNode, mode);

                var operationParameterNodes = new TreeNode("Параметры");
                WriteObjectOperationParametersInNode(
                    ref operationParameterNodes, mode);

                if (operationParameterNodes.Nodes.Count > 1)
                {
                    modeNode.Nodes.Add(operationParameterNodes);
                }

                modesNodes.Nodes.Add(modeNode);
            }
        }

        /// <summary>
        /// Записать шаги операции объекта
        /// </summary>
        /// <param name="modeNode">Узел операции</param>
        /// <param name="mode">Операция</param>
        private static void WriteObjectOperationStepsInNode(
            ref TreeNode modeNode, Mode mode)
        {
            foreach (var state in mode.States)
            {
                var stateNodes = new TreeNode(state.DisplayText[0]);
                stateNodes.Tag = state;

                if (state.Steps.Count > 1)
                {
                    bool stepsTitleIsWrited = false;
                    if (stepsTitleIsWrited == false)
                    {
                        stepsTitleIsWrited = true;
                        stateNodes.Nodes.Add("Шаги");
                    }
                    foreach (var step in state.Steps)
                    {
                        if (!step.DisplayText.Contains(Step.MainStepName))
                        {
                            stateNodes.Nodes.Add(step.DisplayText[0]);
                        }
                    }

                    modeNode.Nodes.Add(stateNodes);
                }
            }
        }

        /// <summary>
        /// Записать параметры операции в узел дерева.
        /// </summary>
        /// <param name="operationParameterNodes">Узел параметров операции
        /// </param>
        /// <param name="mode">Операции</param>
        private static void WriteObjectOperationParametersInNode(
            ref TreeNode operationParameterNodes, Mode mode)
        {
            bool operationParameterTitleIsWrited = false;
            for (int i = 0; i < mode.GetOperationParams().Items.Length; i++)
            {
                if (operationParameterTitleIsWrited == false)
                {
                    var titleNode = new TreeNode();
                    titleNode.Tag = new string[]
                    {
                        "Описание параметра",
                        "Lua имя"
                    };
                    operationParameterNodes.Nodes.Add(titleNode);
                    operationParameterTitleIsWrited = true;
                }

                string name = mode.GetOperationParams().Items[i].DisplayText[0];
                OperationParam operationParameter = mode
                    .GetOperationParams().Items[i] as OperationParam;
                var operationParameterNode = new TreeNode(name);
                operationParameterNode.Tag = new string[]
                {
                    name,
                    operationParameter.Param.GetNameLua()
                };
                operationParameterNodes.Nodes.Add(operationParameterNode);
            }
        }

        /// <summary>
        /// Записать параметры технологического объекта в узел дерева
        /// </summary>
        /// <param name="parameters">Узел дерева параметров</param>
        /// <param name="techObject">Технологический объект</param>
        private static void WriteObjectParametersInNode(ref TreeNode parameters,
            TechObject.TechObject techObject)
        {
            bool parameterTitleisWrited = false;
            var floatParameters = techObject.GetParamsManager().Float.Items;
            for (int i = 0; i < floatParameters.Length; i++)
            {
                var parameter = floatParameters[i] as Param;
                if (parameterTitleisWrited == false)
                {
                    var titleNode = new TreeNode();
                    titleNode.Tag = new string[]
                    {
                            "Имя параметра",
                            "Значение",
                            "Размерность",
                            "Операция" ,
                            "Lua имя"
                    };
                    parameters.Nodes.Add(titleNode);
                    parameterTitleisWrited = true;
                }

                string parameterName = (i + 1).ToString() + ". " + 
                    parameter.EditText[0];
                var parameterNode = new TreeNode();
                parameterNode.Tag = new string[] 
                { 
                    parameterName,
                    parameter.GetValue(), 
                    parameter.GetMeter(),
                    parameter.GetOperationN(),
                    parameter.GetNameLua() 
                };
                parameters.Nodes.Add(parameterNode);
            }
        }

        /// <summary>
        /// Сохранить подключение устройств (привязку) в виде дерева.
        /// </summary>
        /// <returns></returns>
        public static TreeView SaveDeviceConnectionAsTree()
        {
            var tree = new TreeView();
            foreach (DeviceType devType in Enum.GetValues(typeof(DeviceType)))
            {
                var typeNode = new TreeNode(devType.ToString());
                typeNode.Tag = devType;
                tree.Nodes.Add(typeNode);
            }

            foreach (var dev in deviceManager.Devices)
            {
                TreeNode parent = null;
                TreeNode devNode = null;

                foreach (TreeNode node in tree.Nodes)
                {
                    if ((DeviceType)node.Tag == dev.DeviceType)
                    {
                        parent = node;
                        break;
                    }
                }

                //Не найден тип устройства.
                if (parent == null)
                {
                    break;
                }

                if (dev.ObjectName != "")
                {
                    string objectName = dev.ObjectName + dev.ObjectNumber;
                    TreeNode devParent = null;

                    foreach (TreeNode node in parent.Nodes)
                    {
                        if ((node.Tag is String) &&
                            (string)node.Tag == objectName)
                        {
                            devParent = node;
                            break;
                        }
                    }

                    if (devParent == null)
                    {
                        devParent = new TreeNode(objectName);
                        devParent.Tag = objectName;
                        parent.Nodes.Add(devParent);
                    }

                    devNode = new TreeNode(dev.Name + " - " + dev.Description);
                    devNode.Tag = dev;
                    devParent.Nodes.Add(devNode);

                }
                else
                {

                    devNode = new TreeNode(dev.Name + " - " + dev.Description);
                    devNode.Tag = dev;
                    parent.Nodes.Add(devNode);
                }

                foreach (var ch in dev.Channels)
                {
                    string chNodeName = "";
                    if (!ch.IsEmpty())
                    {
                        chNodeName = ch.Name + " " + ch.Comment +
                            $" (A{ch.FullModule}:" + ch.PhysicalClamp + ")";
                    }
                    else
                    {
                        chNodeName = ch.Name + " " + ch.Comment;
                    }
                    devNode.Nodes.Add(chNodeName);
                }

            }

            return tree;
        }

        /// <summary>
        /// Сохранение в виде таблицы (массива строк, чисел и т.д.) информации
        /// об устройствах.
        /// </summary>
        public static object[,] SaveDevicesInformationAsArray()
        {
            const int MAX_SUBTYPE_CNT = 20;
            const int MAX_COL = 20;

            int MAX_TYPE_CNT = Enum.GetValues(typeof(DeviceType)).Length;
            var countDev = new int[MAX_TYPE_CNT];
            var countSubDev = new int[MAX_TYPE_CNT, MAX_SUBTYPE_CNT];

            int RowsCnt = deviceManager.Devices.Count;
            var res = new object[RowsCnt, MAX_COL];

            int idx = 0;
            foreach (var dev in deviceManager.Devices)
            {
                idx += dev.SaveAsArray(ref res, idx, MAX_COL);
            }

            return res;
        }

        public static Dictionary<string, Dictionary<string, int>> GetTypesCount()
            => deviceManager.Summary.NumberUsedTypes();
        
        public static int[] GetChannelsCount(string subtype)
            => deviceManager.ConterChannelsCounter.GetChannelsCount(subtype);
        

        /// <summary>
        /// Сохранение подключение устройств к устройствам ввода-вывода.
        /// </summary>
        /// <param name="prjName">Имя проекта</param>
        /// <param name="modulesCount">Количество модулей</param>
        /// <param name="modulesColor">Цвет</param>
        /// <param name="asInterfaceConnection">AS-интерфейс подключения</param>
        /// <returns></returns>
        public static object[,] SaveIOAsConnectionArray(string prjName, 
            Dictionary<string, int> modulesCount, 
            Dictionary<string, Color> modulesColor, 
            Dictionary<string, object[,]> asInterfaceConnection)
        {
            const int MAX_COL = 7;
            int MAX_ROW = ioManager.IONodes.Count;

            int IndexPCMain = 0;
            int offsetA1 = 0;


            foreach (var ioNode in ioManager.IONodes)
            {
                MAX_ROW += ioNode.IOModules.Count;
                if (ioNode.Type is 
                    IONode.TYPES.T_PHOENIX_CONTACT_1152 or 
                    IONode.TYPES.T_PHOENIX_CONTACT_2152 or 
                    IONode.TYPES.T_PHOENIX_CONTACT_3152)
                {
                    IndexPCMain = ioNode.N - 1;
                }
                if (ioNode.Name == "A1")
                    offsetA1 = 1;
            }

            MAX_ROW *= 16;
            var res = new object[MAX_ROW, MAX_COL];

            bool PCMainSaved = false;

            int idx = 0;
            for (int i = IndexPCMain; i < ioManager.IONodes.Count; i++)
            {
                if (i == IndexPCMain && PCMainSaved)
                {
                    continue;
                }

                IIONode currentNode = ioManager.IONodes[i];

                res[idx, 3] = prjName;
                idx++;
                res[idx, 3] = 
                    $"'{DateTime.Now.ToString(new CultureInfo("RU-ru"))}";

                string nodeName = "";
                if (currentNode.Type is not (
                    IONode.TYPES.T_PHOENIX_CONTACT_1152 or 
                    IONode.TYPES.T_PHOENIX_CONTACT_2152 or 
                    IONode.TYPES.T_PHOENIX_CONTACT_3152))
                {
                    nodeName = $"Узел №{currentNode.N - offsetA1}. {currentNode.Location}-{currentNode.Name}." +
                        $" Адрес: {currentNode.IP}";
                }
                else
                {
                    nodeName = $"Контроллер {currentNode.Location}-{currentNode.Name}. Адрес: {currentNode.IP}";
                    i = -1;
                    PCMainSaved = true;
                }

                res[idx, 4] = "Вход, бит";
                res[idx, 5] = "Выход, бит";
                res[idx, 6] = "Изделие";
                res[idx, 0] = nodeName;
                idx++;

                res[idx, 0] = 0;
                nodeName = currentNode.TypeStr.Replace("750-", "");
                res[idx, 1] = nodeName;

                if (!modulesColor.ContainsKey(nodeName))
                {
                    modulesColor.Add(nodeName, Color.Gray);
                }
                idx++;
                currentNode.SaveAsConnectionArray(ref res, ref idx, 
                    modulesCount, modulesColor, currentNode.N - offsetA1, asInterfaceConnection);
            }

            res = DeleteNullObjects(res); 

            return res;
        }

        /// <summary>
        /// Сохранение устройств, в которых контролируются изделия
        /// </summary>
        /// <returns></returns>
        public static object[,] SaveDevicesArticlesInfoAsArray()
        {
            const int MaxLength = 1000;
            const int MaxWidth = 3;
            object[,] devicesWithArticles = new object[MaxLength, MaxWidth];

            var ignoringDevicesTypes = new List<string>()
            {
                "AI",
                "AO",
                "DI",
                "DO",
            };

            var ignoringDevicesSubTypes = new List<string>()
            {
                "M",
                "M_FREQ",
                "M_REV",
                "M_REV_FREQ",
                "M_REV_2",
                "M_REV_FREQ_2",
                "M_REV_2_ERROR",
                "M_REV_FREQ_2_ERROR",
                "FQT_VIRT",
                "LS_VIRT",
                "LT_VIRT",
                "VC",
            };

            var devices = new List<IODevice>();
            foreach (var device in deviceManager.Devices)
            {
                string devType = device.DeviceType.ToString();
                string devSubType = device.GetDeviceSubTypeStr(
                    device.DeviceType, device.DeviceSubType);
                bool ignoreDevice = ignoringDevicesTypes.Contains(devType) ||
                    ignoringDevicesSubTypes.Contains(devSubType);
                if (!ignoreDevice)
                {
                    devices.Add(device);
                }
            }

            devices.Sort((x, y) =>
            {
                int res = 0;
                res = Device.Compare(x, y);
                return res;
            });

            devicesWithArticles[0, 0] = "ОУ устройства";
            devicesWithArticles[0, 1] = "Подтип устройства";
            devicesWithArticles[0, 2] = "Изделие";

            int counter = 1;
            foreach(var device in devices)
            {         
                devicesWithArticles[counter, 0] = device.EplanName;
                devicesWithArticles[counter, 1] = device.GetDeviceSubTypeStr(
                    device.DeviceType, device.DeviceSubType);
                devicesWithArticles[counter, 2] = device.ArticleName;
                counter++;
            }

            devicesWithArticles = DeleteNullObjects(devicesWithArticles);
            return devicesWithArticles;
        }

        /// <summary>
        /// Удалить пустые объекты в конце массива
        /// </summary>
        /// <param name="res">Массив</param>
        /// <returns></returns>
        private static object[,] DeleteNullObjects(object[,] res)
        {
            int firstDimensionLength = res.GetLength(0);
            int secondDimensionLength = res.GetLength(1);
            int countOfFilledElements = 0;
            for(int i = firstDimensionLength - 1; i >= 0; i--)
            {
                for(int j = 0; j < secondDimensionLength; j++)
                {
                    if (res[i,j] != null)
                    {
                        countOfFilledElements++;
                        break;
                    }
                }
            }

            var newResult = new object[countOfFilledElements, 
                secondDimensionLength];
            for(int i = 0; i < newResult.GetLength(0); i++)
            {
                for(int j = 0; j < newResult.GetLength(1); j++)
                {
                    newResult[i, j] = res[i, j];
                }
            }

            return newResult;

        }

        const string spaceStr = " ";

        /// <summary>
        /// Менеджер устройств
        /// </summary>
        static DeviceManager deviceManager = DeviceManager.GetInstance();

        /// <summary>
        /// Менеджер технологических объектов.
        /// </summary>
        static ITechObjectManager techObjectManager = TechObjectManager
            .GetInstance();

        /// <summary>
        /// Менеджер устройств ввода-вывода.
        /// </summary>
        static IOManager ioManager = IOManager.GetInstance();
    }
}
