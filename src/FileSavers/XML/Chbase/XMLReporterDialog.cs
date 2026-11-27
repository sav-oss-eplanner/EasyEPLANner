using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyEPlanner
{
    [ExcludeFromCodeCoverage]
    public partial class XMLReporterDialog : Form
    {
        public XMLReporterDialog()
        {
            InitializeComponent();

            path = string.Empty;
            projectName = string.Empty;
            ExportBut.Visible = false;
            analysisLabel.Text = "Подготовка предварительного анализа...";
        }

        /// <summary>
        /// Установить имя проекта.
        /// </summary>
        /// <param name="name"></param>
        public void SetProjectName(string name)
        {
            projectName = name;
            UpdatePathFromMode();
        }

        /// <summary>
        /// Установить каталог chbase проекта как путь по умолчанию.
        /// </summary>
        /// <param name="chBaseDirectoryPath">Путь к папке chbase проекта</param>
        public void SetDefaultPath(string chBaseDirectoryPath)
        {
            chBaseDirectoryPath = chBaseDirectoryPath ?? string.Empty;
            this.chBaseDirectoryPath = chBaseDirectoryPath;
            UpdatePathFromMode();
        }

        private void UpdatePathFromMode()
        {
            if (string.IsNullOrEmpty(chBaseDirectoryPath))
            {
                return;
            }

            if (oldChBaseBut.Checked)
            {
                path = string.IsNullOrEmpty(projectName)
                    ? chBaseDirectoryPath
                    : Path.Combine(chBaseDirectoryPath, $"{projectName}{chBaseFormat}");
            }
            else
            {
                path = chBaseDirectoryPath;
            }

            pathTextBox.Text = path;
        }

        /// <summary>
        /// Кнопка "отменить".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelBut_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Событие после закрытия формы.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportChannelBaseDialog_FormClosed(object sender, 
            FormClosedEventArgs e)
        {
            this.Dispose();
        }

        /// <summary>
        /// Кнопка "Обзор".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void reviewPathBut_Click(object sender, EventArgs e)
        {
            if (oldChBaseBut.Checked)
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = $"Файлы базы каналов|*{chBaseFormat}";
                DialogResult openFileResult = openFileDialog.ShowDialog();

                if (openFileResult == DialogResult.OK)
                {
                    path = openFileDialog.FileName;
                }
            }

            if (newChBaseBut.Checked)
            {
                var browseFolderDialog = new FolderBrowserDialog();
                DialogResult browseDialog = browseFolderDialog.ShowDialog();

                if (browseDialog == DialogResult.OK)
                {
                    path = browseFolderDialog.SelectedPath;
                }
            }

            pathTextBox.Text = path;
        }

        private void newChBaseBut_CheckedChanged(object sender, EventArgs e)
        {
            if (newChBaseBut.Checked)
            {
                pathTextBoxLabel.Text = "Укажите каталог для сохранения:";
                oldChBaseBut.Checked = false;
                UpdatePathFromMode();
            }
        }

        private void oldChBaseBut_CheckedChanged(object sender, EventArgs e)
        {
            if (oldChBaseBut.Checked)
            {
                if (splitDatabasesYesBut.Checked)
                {
                    newChBaseBut.Checked = true;
                    return;
                }

                pathTextBoxLabel.Text = "Укажите путь к базе каналов:";
                newChBaseBut.Checked = false;
                UpdatePathFromMode();
            }
        }

        private void NonCombineTagBut_CheckedChanged(object sender, EventArgs e)
        {
            if (nonCombineTagBut.Checked)
            {
                combineTagBut.Checked = false;
                UpdateExportAnalysis();
            }
        }

        private void combineTagBut_CheckedChanged(object sender, EventArgs e)
        {
            if (combineTagBut.Checked)
            {
                nonCombineTagBut.Checked = false;
                UpdateExportAnalysis();
            }
        }

        private void splitDatabasesNoBut_CheckedChanged(object sender, EventArgs e)
        {
            if (splitDatabasesNoBut.Checked)
            {
                UpdateExportAnalysis();
            }
        }

        private void splitDatabasesYesBut_CheckedChanged(object sender, EventArgs e)
        {
            if (splitDatabasesYesBut.Checked)
            {
                if (oldChBaseBut.Checked)
                {
                    newChBaseBut.Checked = true;
                }

                UpdateExportAnalysis();
            }
        }

        private async void XMLReporterDialog_Shown(object sender, EventArgs e)
        {
            await RefreshExportAnalysisAsync(true);
        }

        private async void UpdateExportAnalysis()
        {
            if (isApplyingDefaultSettings || analysisRunning || IsDisposed)
            {
                return;
            }

            await RefreshExportAnalysisAsync(false);
        }

        private async Task RefreshExportAnalysisAsync(bool chooseDefaults)
        {
            if (analysisRunning || IsDisposed)
            {
                return;
            }

            analysisRunning = true;
            SetAnalysisInProgress(true);
            analysisLabel.Text = "Выполняется предварительный анализ...";

            try
            {
                var reporter = new XmlReporter();
                bool combineTags = combineTagBut.Checked;
                bool splitDatabases = splitDatabasesYesBut.Checked;
                var analysis = await Task.Run(() =>
                    ChannelBaseAnalysisRunner.Run(() =>
                        reporter.AnalyzeExport(combineTags, splitDatabases)));

                if (IsDisposed || Disposing)
                {
                    return;
                }

                if (chooseDefaults && analysis.ExceedsSubtypeLimit && !combineTags)
                {
                    isApplyingDefaultSettings = true;
                    combineTagBut.Checked = true;
                    nonCombineTagBut.Checked = false;
                    isApplyingDefaultSettings = false;
                    combineTags = true;
                    analysis = await Task.Run(() =>
                        ChannelBaseAnalysisRunner.Run(() =>
                        reporter.AnalyzeExport(combineTags, splitDatabases)));
                }

                if (IsDisposed || Disposing)
                {
                    return;
                }

                if (chooseDefaults && analysis.ExceedsSubtypeLimit && !splitDatabases)
                {
                    isApplyingDefaultSettings = true;
                    splitDatabasesYesBut.Checked = true;
                    newChBaseBut.Checked = true;
                    isApplyingDefaultSettings = false;
                    splitDatabases = true;
                    analysis = await Task.Run(() =>
                        ChannelBaseAnalysisRunner.Run(() =>
                        reporter.AnalyzeExport(combineTags, splitDatabases)));
                }

                if (IsDisposed || Disposing)
                {
                    return;
                }

                currentAnalysis = analysis;
                UpdateAnalysisLabel();
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                {
                    currentAnalysis = null;
                    analysisLabel.Text = $"Не удалось выполнить предварительный анализ: {ex.Message}";
                    ExportBut.Enabled = false;
                }
            }
            finally
            {
                analysisRunning = false;
                if (!IsDisposed && !Disposing)
                {
                    SetAnalysisInProgress(false);
                    ExportBut.Visible = true;
                }
            }
        }

        private void SetAnalysisInProgress(bool inProgress)
        {
            combineTagsGroup.Enabled = !inProgress;
            splitDatabasesGroup.Enabled = !inProgress;
            ExportBut.Visible = !inProgress;
            ExportBut.Enabled = !inProgress && currentAnalysis != null &&
                currentAnalysis.IsWithinLimits;
        }

        private void UpdateAnalysisLabel()
        {
            if (currentAnalysis == null)
            {
                return;
            }

            var lines = new System.Collections.Generic.List<string>
            {
                "Лимиты:"
            };

            if (splitDatabasesYesBut.Checked)
            {
                foreach (var baseStatistics in currentAnalysis.Bases)
                {
                    string baseFileName = baseStatistics.BaseName == "Устройства"
                        ? $"{projectName}_Devices"
                        : $"{projectName}_TechObjectsAndSystem";
                    lines.Add($"  * {baseStatistics.BaseName} [{baseFileName}]:");
                    lines.Add($"    - Подтипы: {baseStatistics.SubtypeCount}/{XmlReporter.MaximumSubtypeCount};");
                    lines.Add($"    - Каналы: {baseStatistics.TotalChannelCount} (max. " +
                        $"{baseStatistics.MaxChannelsPerSubtype} [{baseStatistics.MaxChannelsSubtypeName}])");
                }
            }
            else
            {
                var baseStatistics = currentAnalysis.Bases[0];
                lines.Add($"  - Подтипы: {baseStatistics.SubtypeCount}/{XmlReporter.MaximumSubtypeCount};");
                lines.Add($"  - Каналы: {baseStatistics.TotalChannelCount} (max. " +
                    $"{baseStatistics.MaxChannelsPerSubtype} [{baseStatistics.MaxChannelsSubtypeName}])");
            }

            if (currentAnalysis.ExceedsSubtypeLimit || currentAnalysis.ExceedsChannelLimit)
            {
                lines.Add("Превышен допустимый диапазон.");
            }

            analysisLabel.Text = string.Join(Environment.NewLine, lines);
            ExportBut.Enabled = currentAnalysis.IsWithinLimits;
        }

        private void oldFormatBut_CheckedChanged(object sender, EventArgs e)
        {
            if (oldFormatBut.Checked)
            {
                newFormatBut.Checked = false;
            }
        }

        private void newFormatBut_CheckedChanged(object sender, EventArgs e)
        {
            if (newFormatBut.Checked)
            {
                oldFormatBut.Checked = false;
            }
        }

        /// <summary>
        /// Экспортировать базу каналов.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportBut_Click(object sender, EventArgs e)
        {
            bool pathIsValid = CheckPath();
            bool combineTag = combineTagBut.Checked;
            bool useNewNames = newFormatBut.Checked;
            bool splitDatabases = splitDatabasesYesBut.Checked;

            if (currentAnalysis == null || !currentAnalysis.IsWithinLimits)
            {
                MessageBox.Show("Количество подтипов или каналов превышает допустимый диапазон. " +
                    "Измените настройки экспорта.", "Ошибка", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (pathIsValid)
            {
                var reporter = new XmlReporter();
                if (newChBaseBut.Checked)
                {
                    Directory.CreateDirectory(path);
                    string filePath = Path.Combine(path, $"{projectName}{chBaseFormat}");
                    bool rewrite = true;
                    reporter.SaveAsCDBX(filePath, combineTag, useNewNames, rewrite,
                        splitDatabases);
                }
                else
                {
                    reporter.SaveAsCDBX(path, combineTag, useNewNames,
                        splitDatabases: splitDatabases);
                }

                Close();
            }
        }

        /// <summary>
        /// Проверить выбранный путь.
        /// </summary>
        /// <returns></returns>
        private bool CheckPath()
        {
            if (newChBaseBut.Checked && string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Не указан каталог для сохранения.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (oldChBaseBut.Checked)
            {
                if (!File.Exists(path))
                {
                    MessageBox.Show("Не найден файл базы каналов", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (!path.Contains(chBaseFormat))
                {
                    MessageBox.Show("Указан неправильный файл базы каналов.",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        private string path;
        private string projectName;
        private bool isApplyingDefaultSettings;
        private bool analysisRunning;
        private ChannelBaseExportAnalysis currentAnalysis;
        private string chBaseDirectoryPath = string.Empty;
        private string chBaseFormat = ".cdbx";
    }
}
