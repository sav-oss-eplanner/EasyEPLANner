using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EasyEPlanner;

namespace InterprojectExchange
{
    /// <summary>
    /// Межконтроллерный обмен сигналами. Обмен с формами
    /// </summary>
    public class InterprojectExchange : IInterprojectExchange
    {
        protected InterprojectExchange()
        {
            interprojectExchangeModels = new List<IProjectModel>();
        }

        public void Clear()
        {
            interprojectExchangeStarter = null;
            interprojectExchangeModels.Clear();
            InterprojectProjectCatalog.Invalidate();
        }

        public void AddModel(IProjectModel model)
        {
            interprojectExchangeModels.Add(model);
        }

        public bool LoadProjectData(string pathToProjectDir, out string errors)
        {
            if (!MainIoProjectNameReader.TryReadFromFolder(pathToProjectDir,
                out string projName, out string readError))
            {
                errors = readError;
                return false;
            }

            InterprojectProjectCatalog.Register(pathToProjectDir, projName);
            return Owner.LoadProjectData(pathToProjectDir, projName, out errors);
        }

        public bool CheckPathToProjectFiles(string path)
        {
            return Owner.CheckProjectData(path);
        }

        public IProjectModel GetModel(string projName)
        {
            IProjectModel model = interprojectExchangeModels
                .Where(x => x.ProjectName == projName)
                .FirstOrDefault();
            return model;
        }

        public void SelectModel(IProjectModel selectingModel)
        {
            var currentProjectModel = MainModel;

            foreach (var model in Models)
            {
                if (model.ProjectName == selectingModel.ProjectName)
                {
                    model.Selected = true;
                    currentProjectModel.SelectedAdvancedProject = model
                        .ProjectName;
                }
                else
                {
                    model.Selected = false;
                }
            }
        }

        public void DeleteExchangeWithProject(string projectName)
        {
            IProjectModel model = GetModel(projectName);
            if (model != null)
            {
                model.MarkedForDelete = true;
            }
            else
            {
                throw new Exception("Ошибка при удалении связи с проектом");
            }
        }

        public void ChangeEditMode(int selectedModeIndex)
        {
            editMode = (EditMode)selectedModeIndex;
        }

        public Dictionary<string, List<string[]>> GetBindedSignals()
        {
            var signals = new Dictionary<string, List<string[]>>();

            foreach (var channelName in DeviceChannelsNames)
            {
                List<string[]> channelSignals = GetSignalsPairs(
                    GetCurrentProjectSignals(channelName),
                    GetAdvancedProjectSignals(channelName));
                signals.Add(channelName, SortBindedSignalPairs(channelSignals));
            }

            return signals;
        }

        /// <summary>
        /// Ошибочные строки (с "-") — в начале, связанные пары — в конце.
        /// </summary>
        private static List<string[]> SortBindedSignalPairs(List<string[]> pairs)
        {
            return [.. pairs.OrderBy(pair =>
                pair.Any(DeviceSignalsInfo.IsUnpaired) ? 0 : 1)];
        }

        public string CheckBindingSignals()
        {
            var err = new StringBuilder();
            var mainModel = MainModel;

            foreach (var model in Models.Where(m => m.Loaded && m != MainModel))
            {
                mainModel.SelectedAdvancedProject = model.ProjectName;
                model.HasBindingError = false;

                mainModel.ReceiverSignals.StripUnpairedSignals();
                model.SourceSignals.StripUnpairedSignals();
                mainModel.SourceSignals.StripUnpairedSignals();
                model.ReceiverSignals.StripUnpairedSignals();

                string receiverErr = mainModel.ReceiverSignals
                    .CountCompare(model.SourceSignals);
                if (!string.IsNullOrEmpty(receiverErr))
                {
                    err.Append(
                        $"remote_gateways: {model.ProjectName} - {receiverErr}\n");
                    DeviceSignalsInfo.ExpandMismatchedChannels(
                        mainModel.ReceiverSignals, model.SourceSignals);
                    model.HasBindingError = true;
                }

                string sourceErr = mainModel.SourceSignals
                    .CountCompare(model.ReceiverSignals);
                if (!string.IsNullOrEmpty(sourceErr))
                {
                    err.Append(
                        $"shared_devices: {model.ProjectName} - {sourceErr}\n");
                    DeviceSignalsInfo.ExpandMismatchedChannels(
                        mainModel.SourceSignals, model.ReceiverSignals);
                    model.HasBindingError = true;
                }
            }

            return err.ToString();
        }

        /// <summary>
        /// Обновить флаг ошибки привязки у выбранной модели
        /// </summary>
        public void RefreshSelectedModelBindingError()
        {
            IProjectModel model = SelectedModel;
            if (model == null || ReferenceEquals(model, MainModel))
            {
                return;
            }

            bool hasError =
                MainModel.ReceiverSignals.ContainsUnpairedSignals() ||
                MainModel.SourceSignals.ContainsUnpairedSignals() ||
                model.SourceSignals.ContainsUnpairedSignals() ||
                model.ReceiverSignals.ContainsUnpairedSignals();

            model.HasBindingError = hasError;
        }

        /// <summary>
        /// Имена проектов с ошибкой привязки сигналов
        /// </summary>
        public string[] GetModelsWithBindingErrors()
        {
            return Models
                .Where(m => m != MainModel &&
                    m.Loaded &&
                    m.HasBindingError &&
                    !m.MarkedForDelete)
                .Select(m => m.ProjectName)
                .ToArray();
        }

        /// <summary>
        /// Получить связанные сигналы
        /// </summary>
        /// <param name="currentProjectSignals">Список сигналов текущего проекта
        /// </param>
        /// <param name="advancedProjectSignals">Список сигналов альтернативного
        /// проекта</param>
        /// <returns></returns>
        private static List<string[]> GetSignalsPairs(
            List<string> currentProjectSignals,
            List<string> advancedProjectSignals)
        {
            if (currentProjectSignals.Count == 0 &&
                advancedProjectSignals.Count == 0)
            {
                return new List<string[]>();
            }

            if (currentProjectSignals.Count != advancedProjectSignals.Count)
            {
                var unpaired = new List<string[]>();
                foreach (var signal in currentProjectSignals)
                {
                    unpaired.Add(new[]
                    {
                        signal,
                        DeviceSignalsInfo.UnpairedSignal
                    });
                }

                foreach (var signal in advancedProjectSignals)
                {
                    unpaired.Add(new[]
                    {
                        DeviceSignalsInfo.UnpairedSignal,
                        signal
                    });
                }

                return unpaired;
            }

            // Индексное сопоставление: IndexOf нельзя использовать при
            // повторяющихся именах (в т.ч. плейсхолдерах "-")
            var pairs = new List<string[]>(currentProjectSignals.Count);
            for (int i = 0; i < currentProjectSignals.Count; i++)
            {
                pairs.Add(new[]
                {
                    currentProjectSignals[i],
                    advancedProjectSignals[i]
                });
            }

            return pairs;
        }

        public bool BindSignals(string signalType, string currentProjectDevice,
            string advancedProjectDevice)
        {
            if (SelectedModel?.HasBindingError is true ||
                DeviceSignalsInfo.IsUnpaired(currentProjectDevice) ||
                DeviceSignalsInfo.IsUnpaired(advancedProjectDevice))
            {
                return false;
            }

            List<string> currentProjSignals = GetCurrentProjectSignals(
                signalType);
            List<string> advancedProjSignals = GetAdvancedProjectSignals(
                signalType);

            if (currentProjSignals.Contains(currentProjectDevice) ||
                advancedProjSignals.Contains(advancedProjectDevice))
            {
                return false;
            }

            currentProjSignals.Add(currentProjectDevice);
            advancedProjSignals.Add(advancedProjectDevice);
            RefreshSelectedModelBindingError();

            return true;
        }

        public bool DeleteSignalsBind(string signalType,
            string currentProjectDevice, string advancedProjectDevice)
        {
            List<string> currentProjSignals = GetCurrentProjectSignals(
                signalType);
            List<string> advancedProjSignals = GetAdvancedProjectSignals(
                signalType);

            int index = FindSignalPairIndex(currentProjSignals,
                advancedProjSignals, currentProjectDevice,
                advancedProjectDevice);
            if (index < 0)
            {
                return false;
            }

            currentProjSignals.RemoveAt(index);
            advancedProjSignals.RemoveAt(index);
            RefreshSelectedModelBindingError();

            return true;
        }

        public bool MoveSignalsBind(string signalType, string currProjSignal,
            string advProjSignal, int move)
        {
            List<string> currentProjSignals = GetCurrentProjectSignals(
                signalType);
            List<string> advancedProjSignals = GetAdvancedProjectSignals(
                signalType);

            int pairIndex = FindSignalPairIndex(currentProjSignals,
                advancedProjSignals, currProjSignal, advProjSignal);
            if (pairIndex < 0)
            {
                return false;
            }

            bool blockMoveUp = pairIndex == 0 && move == -1;
            bool blockMoveDown =
                pairIndex == currentProjSignals.Count - 1 && move == 1;
            if (blockMoveDown || blockMoveUp)
            {
                return false;
            }

            string curr = currentProjSignals[pairIndex];
            string adv = advancedProjSignals[pairIndex];
            currentProjSignals.RemoveAt(pairIndex);
            advancedProjSignals.RemoveAt(pairIndex);
            currentProjSignals.Insert(pairIndex + move, curr);
            advancedProjSignals.Insert(pairIndex + move, adv);

            return true;
        }

        public bool UpdateProjectBinding(string signalType,
            string oldValue, string newValue, bool mainProject,
            out bool needSwap)
        {
            needSwap = false;

            if (SelectedModel?.HasBindingError is true ||
                oldValue == newValue ||
                DeviceSignalsInfo.IsUnpaired(oldValue) ||
                DeviceSignalsInfo.IsUnpaired(newValue))
            {
                return false;
            }

            List<string> signals = mainProject ?
                GetCurrentProjectSignals(signalType) :
                GetAdvancedProjectSignals(signalType);

            void RemoveAndInsert<T>(List<T> collection, int index, T value)
            {
                collection.RemoveAt(index);
                collection.Insert(index, value);
            }

            int oldValueIndex = signals.IndexOf(oldValue);
            if (oldValueIndex < 0)
            {
                return false;
            }

            int newValueIndex = signals.IndexOf(newValue);
            if (newValueIndex >= 0)
            {
                RemoveAndInsert(signals, oldValueIndex, newValue);
                RemoveAndInsert(signals, newValueIndex, oldValue);

                needSwap = true;
            }
            else
            {
                RemoveAndInsert(signals, oldValueIndex, newValue);
            }

            RefreshSelectedModelBindingError();
            return true;
        }

        /// <summary>
        /// Связать два несвязанных сигнала из ошибочных строк
        /// </summary>
        public bool PairUnpairedSignals(string signalType,
            string currentProjectSignal, string advancedProjectSignal)
        {
            if (DeviceSignalsInfo.IsUnpaired(currentProjectSignal) ||
                DeviceSignalsInfo.IsUnpaired(advancedProjectSignal))
            {
                return false;
            }

            List<string> currentProjSignals = GetCurrentProjectSignals(
                signalType);
            List<string> advancedProjSignals = GetAdvancedProjectSignals(
                signalType);

            int currentRow = FindSignalPairIndex(currentProjSignals,
                advancedProjSignals, currentProjectSignal,
                DeviceSignalsInfo.UnpairedSignal);
            int advancedRow = FindSignalPairIndex(currentProjSignals,
                advancedProjSignals, DeviceSignalsInfo.UnpairedSignal,
                advancedProjectSignal);

            if (currentRow < 0 || advancedRow < 0 || currentRow == advancedRow)
            {
                return false;
            }

            int first = Math.Max(currentRow, advancedRow);
            int second = Math.Min(currentRow, advancedRow);
            currentProjSignals.RemoveAt(first);
            advancedProjSignals.RemoveAt(first);
            currentProjSignals.RemoveAt(second);
            advancedProjSignals.RemoveAt(second);

            currentProjSignals.Insert(0, currentProjectSignal);
            advancedProjSignals.Insert(0, advancedProjectSignal);
            RefreshSelectedModelBindingError();

            return true;
        }

        /// <summary>
        /// Заполнить сторону "-" в ошибочной строке устройством из списка
        /// </summary>
        public bool FillUnpairedSignal(string signalType,
            string knownSignal, string newDevice, bool fillAdvancedSide)
        {
            if (DeviceSignalsInfo.IsUnpaired(knownSignal) ||
                DeviceSignalsInfo.IsUnpaired(newDevice))
            {
                return false;
            }

            List<string> currentProjSignals = GetCurrentProjectSignals(
                signalType);
            List<string> advancedProjSignals = GetAdvancedProjectSignals(
                signalType);

            int rowIndex = FindUnpairedRowForFill(currentProjSignals,
                advancedProjSignals, knownSignal, fillAdvancedSide);
            if (rowIndex < 0)
            {
                return false;
            }

            int duplicateUnpairedRow = FindDuplicateUnpairedRow(
                currentProjSignals, advancedProjSignals, newDevice,
                fillAdvancedSide);
            if (IsDeviceAlreadyBound(currentProjSignals, advancedProjSignals,
                newDevice, fillAdvancedSide, duplicateUnpairedRow))
            {
                return false;
            }

            if (fillAdvancedSide)
            {
                advancedProjSignals[rowIndex] = newDevice;
            }
            else
            {
                currentProjSignals[rowIndex] = newDevice;
            }

            RemoveDuplicateUnpairedRow(currentProjSignals, advancedProjSignals,
                duplicateUnpairedRow, rowIndex);
            RefreshSelectedModelBindingError();
            return true;
        }

        private static int FindUnpairedRowForFill(List<string> currentSignals,
            List<string> advancedSignals, string knownSignal,
            bool fillAdvancedSide)
        {
            if (fillAdvancedSide)
            {
                return FindSignalPairIndex(currentSignals, advancedSignals,
                    knownSignal, DeviceSignalsInfo.UnpairedSignal);
            }

            return FindSignalPairIndex(currentSignals, advancedSignals,
                DeviceSignalsInfo.UnpairedSignal, knownSignal);
        }

        private static int FindDuplicateUnpairedRow(List<string> currentSignals,
            List<string> advancedSignals, string newDevice,
            bool fillAdvancedSide)
        {
            for (int i = 0; i < currentSignals.Count; i++)
            {
                bool isDuplicate = fillAdvancedSide
                    ? advancedSignals[i] == newDevice &&
                        DeviceSignalsInfo.IsUnpaired(currentSignals[i])
                    : currentSignals[i] == newDevice &&
                        DeviceSignalsInfo.IsUnpaired(advancedSignals[i]);
                if (isDuplicate)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsDeviceAlreadyBound(List<string> currentSignals,
            List<string> advancedSignals, string newDevice,
            bool fillAdvancedSide, int duplicateUnpairedRow)
        {
            if (duplicateUnpairedRow >= 0)
            {
                return false;
            }

            return fillAdvancedSide
                ? advancedSignals.Contains(newDevice)
                : currentSignals.Contains(newDevice);
        }

        private static void RemoveDuplicateUnpairedRow(
            List<string> currentSignals, List<string> advancedSignals,
            int duplicateUnpairedRow, int filledRowIndex)
        {
            if (duplicateUnpairedRow < 0 ||
                duplicateUnpairedRow == filledRowIndex)
            {
                return;
            }

            currentSignals.RemoveAt(duplicateUnpairedRow);
            advancedSignals.RemoveAt(duplicateUnpairedRow);
        }

        private static int FindSignalPairIndex(List<string> currentSignals,
            List<string> advancedSignals, string currentSignal,
            string advancedSignal)
        {
            int count = Math.Min(currentSignals.Count, advancedSignals.Count);
            for (int i = 0; i < count; i++)
            {
                if (currentSignals[i] == currentSignal &&
                    advancedSignals[i] == advancedSignal)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Получить сигналы текущего проекта
        /// </summary>
        /// <param name="signalType">Тип сигнала</param>
        /// <returns></returns>
        private List<string> GetCurrentProjectSignals(string signalType)
        {
            IProjectModel currentProjectModel = MainModel;
            DeviceSignalsInfo currentProjectSignals;
            if (editMode == EditMode.SourceReciever)
            {
                currentProjectSignals = currentProjectModel.SourceSignals;
            }
            else
            {
                currentProjectSignals = currentProjectModel.ReceiverSignals;
            }

            switch (signalType)
            {
                case "AI":
                    return currentProjectSignals.AI;

                case "AO":
                    return currentProjectSignals.AO;

                case "DI":
                    return currentProjectSignals.DI;

                case "DO":
                    return currentProjectSignals.DO;
            }

            return new List<string>();
        }

        /// <summary>
        /// Получить сигналы альтернативного проекта
        /// </summary>
        /// <param name="signalType">Тип сигнала</param>
        /// <returns></returns>
        private List<string> GetAdvancedProjectSignals(string signalType)
        {
            IProjectModel advancedProjectModel = SelectedModel;
            DeviceSignalsInfo advancedProjectSignals;
            if (editMode == EditMode.SourceReciever)
            {
                advancedProjectSignals = advancedProjectModel?.ReceiverSignals ?? new DeviceSignalsInfo();
            }
            else
            {
                advancedProjectSignals = advancedProjectModel?.SourceSignals ?? new DeviceSignalsInfo();
            }

            switch (signalType)
            {
                case "AI":
                    return advancedProjectSignals.AI;

                case "AO":
                    return advancedProjectSignals.AO;

                case "DI":
                    return advancedProjectSignals.DI;

                case "DO":
                    return advancedProjectSignals.DO;
            }

            return new List<string>();
        }

        public void Save()
        {
            Owner.Save();
        }

        public bool RestoreModel(string projectName)
        {
            var canRestore = false;

            foreach (var model in Models)
            {
                bool foundMarkedModel =
                    projectName == model.ProjectName && model.MarkedForDelete;
                if (foundMarkedModel)
                {
                    canRestore = true;
                    model.MarkedForDelete = false;
                }
            }

            return canRestore;
        }

        public string[] LoadedAdvancedModelNames
        {
            get
            {
                return interprojectExchange.Models
                    .Where(x => x.ProjectName != interprojectExchange
                    .MainProjectName &&
                    x.MarkedForDelete == false)
                    .Select(x => x.ProjectName)
                    .ToArray();
            }
        }

        public virtual ICurrentProjectModel MainModel
        {
            get
            {
                return GetModel(MainProjectName) as ICurrentProjectModel;
            }
        }

        public string MainProjectName
        {
            get
            {
                if (ProjectManager.GetInstance().IsStandalone ||
                    eProjectManager == null)
                {
                    return ResolveStandaloneMainProjectName();
                }

                return eProjectManager.GetModifyingCurrentProjectName();
            }
        }

        private static string ResolveStandaloneMainProjectName()
        {
            var context = ProjectContextHolder.Current;
            if (context == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(context.ProjectFolderPath) &&
                MainIoProjectNameReader.TryReadFromFolder(
                    context.ProjectFolderPath, out string pacName, out _))
            {
                return pacName;
            }

            return context.ProjectName ?? string.Empty;
        }

        public string DefaultPathWithProjects
        {
            get
            {
                return ProjectManager.GetInstance().GetPtusaProjectsPath("");
            }
        }

        public InterprojectExchangeStarter Owner
        {
            get
            {
                return interprojectExchangeStarter;
            }
            set
            {
                interprojectExchangeStarter = value;
            }
        }

        public List<IProjectModel> Models
        {
            get
            {
                return interprojectExchangeModels;
            }
        }

        public IProjectModel SelectedModel
        {
            get
            {
                return Models.Where(x => x.Selected == true).FirstOrDefault();
            }
        }

        public string[] DeviceChannelsNames
        {
            get
            {
                return new string[] { "AI", "AO", "DI", "DO" };
            }
        }

        public EditMode EditMode => editMode;

        /// <summary>
        /// Получить экземпляр класса. Singleton
        /// </summary>
        /// <returns></returns>
        public static IInterprojectExchange GetInstance()
        {
            if (interprojectExchange == null)
            {
                if (eProjectManager is null)
                {
                    try
                    {
                        eProjectManager = EProjectManager.GetInstance();
                    }
                    catch
                    {
                        // skip EProjectManager int TESTS
                    }
                }

                interprojectExchange = new InterprojectExchange();
            }

            return interprojectExchange;
        }

        private static IEProjectManager eProjectManager = null;
        private EditMode editMode;
        private InterprojectExchangeStarter interprojectExchangeStarter;
        private static InterprojectExchange interprojectExchange;
        private List<IProjectModel> interprojectExchangeModels;
    }

    /// <summary>
    /// Режим редактирования связей
    /// </summary>
    public enum EditMode
    {
        SourceReciever, // Источник >> Приемник
        RecieverSource  // Приемник >> Источник
    }
}