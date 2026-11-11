using EasyEPlanner;
using EasyEPlanner.FileSavers.XML;
using Eplan.EplApi.Base;
using IO.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TechObject
{
    public class TechObjectXMLMaker
    {
        public TechObjectXMLMaker(ITechObjectManager techObjectManager)
        {
            this.techObjectManager = techObjectManager;
        }

        /// <summary>
        /// Формирование узлов для операций, шагов и параметров объектов.
        /// </summary>
        /// <param name="root">корневой узел</param>
        /// <param name="useNewNames">Использовать новые имена объектов
        /// вместо OBJECT</param>
        /// <param name="combineTags">Комбинировать тэги в один подтип</param>
        public void BuildObjectsForXML(IDriver root, bool combineTags, bool useNewNames)
        {
            cdbxNewNames = useNewNames;
            cdbxTagView = combineTags;

            GenerateSystemNode(root);
            foreach (var num in Enumerable.Range(1, techObjectManager.TechObjects.Count))
            {
                TechObject item = techObjectManager.TechObjects[num - 1];
                GenerateObject(root, item, num);
            }
        }

        /// <summary>
        /// Генерировать теги для объекта
        /// </summary>
        /// <param name="root">Корень базы</param>
        /// <param name="techObject">Тех.объетк</param>
        /// <param name="num"></param>
        private void GenerateObject(IDriver root, TechObject techObject, int num)
        {
            List<Mode> modes = techObject.ModesManager.Modes;
            var stDescription = techObject.GetNameForChannelBase();
            string objName = GenerateObjectName(techObject, num);

            GenerateOperations(root, modes, stDescription, objName);
            GenerateStateSteps(root, modes, stDescription, objName);
            GenerateParamters(root, techObject, stDescription, objName);

            // Параметры ПИД (совместимость со старыми версиями)
            if (techObject.BaseTechObject != null && techObject.BaseTechObject.IsPID)
            {
                foreach (var i in Enumerable.Range(1, 2))
                {
                    root.AddChannel(SubtypeName($"PID{num}", "Параметры"), $"PID{num}.RT_PAR_F[ {i} ]");
                }

                foreach (var i in Enumerable.Range(1, 14))
                {
                    root.AddChannel(SubtypeName($"PID{num}", "Параметры"), $"PID{num}.S_PAR_F[ {i} ]");
                }
            }
        }

        /// <summary>
        /// Генерировать теги операций     <br/>
        /// .CMD;               [ ]        <br/>
        /// .ST;                [ logged ] <br/>
        /// .MODES[ i ];        [ logged ] <br/>
        /// .OPERATIONS[ i ];   [ logged ] <br/>
        /// .AVAILABILITY[ i ]; [ ]        <br/>
        /// .MODES_STEPS[ i ];  [ logged ] <br/>
        /// </summary>
        /// <param name="root">Корень базы</param>
        /// <param name="modes">Операции</param>
        /// <param name="subtype">Название подтипа</param>
        /// <param name="objName">Название объекта</param>
        private void GenerateOperations(IDriver root, List<Mode> modes, string subtype, string objName)
        {
            root.AddChannel(SubtypeName(subtype, "Операции"), $"{objName}.CMD");
            foreach (var index in Enumerable.Range(1, (modes.Count / 33) + 1))
            {
                root.AddChannel(SubtypeName(subtype, "Операции"), $"{objName}.ST[ {index} ]")
                    .Logged();
            }

            foreach (var index in Enumerable.Range(1, modes.Count))
            {
                root.AddChannel(SubtypeName(subtype, "Операции"), $"{objName}.MODES[ {index} ]")
                    .Logged();
                root.AddChannel(SubtypeName(subtype, "Состояния_Операций"), $"{objName}.OPERATIONS[ {index} ]")
                    .Logged();
                root.AddChannel(SubtypeName(subtype, "Доступность"), $"{objName}.AVAILABILITY[ {index} ]");
                root.AddChannel(SubtypeName(subtype, "Одиночные_Шаги"), $"{objName}.MODES_STEPS[ {index} ]")
                    .Logged();
            }
        }

        /// <summary>
        /// Генерировать теги .STATE_STEPS [logged]
        /// </summary>
        /// <param name="root">Корень базы</param>
        /// <param name="modes">Операции</param>
        /// <param name="subtype">Название подтипа</param>
        /// <param name="objName">Название объекта</param>
        private void GenerateStateSteps(IDriver root, List<Mode> modes, string subtype, string objName)
        {
            foreach (var modeIdx in Enumerable.Range(0, modes.Count))
            {
                foreach (var state in modes[modeIdx].States)
                {
                    foreach (var stepIdx in Enumerable.Range(0, state.Steps.Count))
                    {
                        root.AddChannel(
                            SubtypeName(subtype, "Одиночные_Шаги"),
                            $"{objName}.{state.Type}_STEPS{modeIdx + 1}[ {stepIdx + 1} ]")
                            .Logged();
                    }
                }
            }
        }

        /// <summary>
        /// Генерировать теги параметров .S_PAR_F[ index ]...
        /// </summary>
        /// <param name="root">Корень базы</param>
        /// <param name="techObject">Тех.объект</param>
        /// <param name="subtype">Название подтипа</param>
        /// <param name="objName">Название объекта</param>
        private void GenerateParamters(IDriver root, TechObject techObject, string subtype, string objName)
        {
            foreach (var paramsGroup in techObject.GetParamsManager().Items.OfType<Params>())
            {
                foreach (var param in paramsGroup.Parameters)
                {
                    var channel = root.AddChannel(
                        SubtypeName(subtype, "Параметры"),
                        cdbxNewNames ?
                        $"{objName}.{paramsGroup.NameForChannelBase}_{param.GetNameLua()}[ {param.GetParameterNumber} ]" :
                        $"{objName}.{paramsGroup.NameForChannelBase}[ {param.GetParameterNumber} ]");

                    if (param.Operations is { } operations && operations != "-1")
                    {
                        channel.WithParameter("Operations", $"[ {string.Join(", ", operations.Split(' '))} ]");
                    }
                }
            }
        }

        private string SubtypeName(string stDescription, string nonCombinedPostfix)
            => stDescription + (cdbxTagView ? "" : $"_{nonCombinedPostfix}");

        /// <summary>
        /// Генерация системных тегов 
        /// </summary>
        /// <param name="rootNode">Узловой узел</param>
        private static void GenerateSystemNode(IDriver root)
        {
            const string SYSTEM = nameof(SYSTEM);

            root.AddChannel(SYSTEM, $"{SYSTEM}.UP_TIME")
                .WithParameter("IsString", "1");
            root.AddChannel(SYSTEM, $"{SYSTEM}.WASH_VALVE_SEAT_PERIOD");
            root.AddChannel(SYSTEM, $"{SYSTEM}.P_V_OFF_DELAY_TIME"   );
            root.AddChannel(SYSTEM, $"{SYSTEM}.WASH_VALVE_UPPER_SEAT_TIME");
            root.AddChannel(SYSTEM, $"{SYSTEM}.WASH_VALVE_LOWER_SEAT_TIME");
            root.AddChannel(SYSTEM, $"{SYSTEM}.CMD");
            root.AddChannel(SYSTEM, $"{SYSTEM}.CMD_ANSWER")
                .WithParameter("IsString", "1");
            root.AddChannel(SYSTEM, $"{SYSTEM}.P_RESTRICTIONS_MODE");
            root.AddChannel(SYSTEM, $"{SYSTEM}.P_RESTRICTIONS_MANUAL_TIME"   );
            root.AddChannel(SYSTEM, $"{SYSTEM}.P_AUTO_PAUSE_OPER_ON_DEV_ERR");
            root.AddChannel(SYSTEM, $"{SYSTEM}.VERSION")
                .WithParameter("IsString", "1");
            root.AddChannel(SYSTEM, $"{SYSTEM}.NODES_COMM_ERROR");
            root.AddChannel(SYSTEM, $"{SYSTEM}.WATCHDOG_ERROR");
            root.AddChannel(SYSTEM, $"{SYSTEM}.COMMUN_ERROR");

            var nodes = IO.IOManager.GetInstance().IONodes;
            if (nodes.Count is 0 or 1)
                return;

            foreach (var nodeNumber in Enumerable.Range(1, nodes.Count - 1))
            {
                root.AddChannel(SYSTEM, $"{SYSTEM}.NODEENABLED[ {nodeNumber + 1} ]");
            }

            foreach (var nodeNumber in Enumerable.Range(1, nodes.Count - 1))
            {
                root.AddChannel(SYSTEM, $"{SYSTEM}.NODEST[ {nodeNumber + 1} ]");
            }
        }

        /// <summary>
        /// Генерация имени объекта
        /// </summary>
        /// <param name="item">Объект</param>
        /// <param name="itemNumber">Глобальный номер</param>
        /// <returns></returns>
        private string GenerateObjectName(TechObject item, int itemNumber)
        {
            if (cdbxNewNames)
            {
                return item.GetNameForChannelBase();
            }
            else
            {
                return "OBJECT" + itemNumber.ToString();
            }
        }

        private ITechObjectManager techObjectManager;

        /// <summary>
        /// Использовать имена объектов в базе каналов вместо OBJECT
        /// </summary>
        private bool cdbxNewNames;

        /// <summary>
        /// Сгруппировать тэги в один подтип
        /// </summary>
        private bool cdbxTagView;
    }
}
