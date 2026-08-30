using Autodesk.Revit.UI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static PluginsManager.Const;

namespace PluginsManager
{
    public class WindowManager
    {
        public ExternalEvent External_event { get; set; }
        public PluginsManagerForm Window { get; set; }
        public CommandManager Command_manager { get; set; }
        public UIApplication UiApp { get; set; }

        public WindowManager(CommandManager command_manager, UIApplication uiApp)
        {
            Command_manager = command_manager;
            UiApp = uiApp;
            External_event = Command_manager.ExternalEvent;
            PluginsManagerForm window = new PluginsManagerForm();
            Window = window;
            window.Text = "Менеджер плагинов";
            window.groupBox1.Text = "Информация";
            window.tabControl.Font = new Font("Microsoft Sans Serif", 10, FontStyle.Regular);
            window.toolStripButton1.Click += (s, e) => ShowUserConfigForm();
            window.toolStripButton1.Text = "Выбрать папку";
            window.toolStripButton2.Click += (s, e) => OpenGitHub();
            window.toolStripButton2.Text = "GitHub";
            window.toolStripButton3.Click += (s, e) => { Refrash(); };
            window.toolStripButton3.Text = "Обновить";
            window.richTextBox1.LinkClicked += RichTextBox1_LinkClicked;
            Logger.Info("Создание и отображение основного окна");
            CreateTabs();
            Logger.Info($"Окно подготовлено | tabs = {Window.tabControl.TabPages.Count}");
            window.ShowDialog();
        }

        /// <summary>
        /// Открывает форму настроек пользователя и применяет изменения после сохранения.
        /// </summary>
        private void ShowUserConfigForm()
        {
            Logger.Separator();
            Logger.Info("[WindowManager] Открытие формы настроек пользователя");

            // Собираем список всех вкладок
            var allTabs = Command_manager.CommandsDictionary.Keys.ToList();
            Logger.Debug($"[WindowManager] Всего вкладок для настроек: {allTabs.Count}");

            using (var form = new UserConfigForm(PathManager.sourceDir, UserConfig.exceptionTabs, UserConfig.post, allTabs))
            {
                if (form.ShowDialog(Window) == DialogResult.OK)
                {
                    Logger.Info("[WindowManager] Настройки сохранены, применяем изменения");

                    // 1. Сохраняем конфиг пользователя
                    UserConfig.SaveUserSettings(form.SelectedFolderPath, form.ExcludedTabsString, form.SelectedPost);

                    // 2. Перечитываем настройки (обновляет PathManager.sourceDir)
                    UserConfig.GetUserSettings();

                    // 3. Создаём конфиг команд в исходной папке, если его нет
                    CommandConfig.CreateConfigFile();

                    // 4. Копируем все файлы (включая commands_config.xml) во временную папку
                    TempFiles.CopyToTemp(PathManager.sourceDir);

                    // 5. Читаем конфиг команд из временной папки
                    CommandConfig.GetDllSettings();

                    // 6. Готовим DLL для анализа
                    Dllmanager.CopyDll();

                    // 7. Обновляем менеджер команд (анализирует DLL с учётом XML)
                    Command_manager.Refresh(PathManager.tempDllDir);

                    // 8. Перерисовываем вкладки
                    Window.tabControl.TabPages.Clear();
                    CreateTabs();

                    Logger.Info("[WindowManager] Настройки применены успешно");
                }
                else
                {
                    Logger.Info("[WindowManager] Изменение настроек отменено");
                }
            }
        }

        private void Refrash()
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.Separator();
            Logger.Info($"Обновление UI и данных из папки [{PathManager.sourceDir}]");
            TempFiles.CopyToTemp(PathManager.sourceDir);
            CommandConfig.GetDllSettings();
            Dllmanager.CopyDll();
            Command_manager.Refresh(PathManager.tempDllDir);
            Window.tabControl.TabPages.Clear();
            CreateTabs();
            stopwatch.Stop();
            Logger.Info($"Обновление завершено за {stopwatch.ElapsedMilliseconds} ms");
        }

        void table_CellClickRunCommand(object sender, DataGridViewCellEventArgs e, DataGridView dataGridView)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dataGridView.Columns["Выбор"].Index) return;
            GlobComandName.Name = dataGridView["id", e.RowIndex].Value.ToString();
            Logger.Info($"Выбрана команда для запуска: [{GlobComandName.Name}]");
            External_event.Raise();
            Logger.Debug("ExternalEvent поднят, окно будет закрыто");
            Window.Close();
        }

        void table_CellClickDescription(object sender, DataGridViewCellEventArgs e, DataGridView dataGridView)
        {
            if (e.RowIndex < 0) return;
            string code = dataGridView["id", e.RowIndex].Value.ToString();
            var command = Command_manager.AllCommands.FirstOrDefault(cmd => cmd.CmdCode == code);
            Window.richTextBox1.Text = command?.CmdDescription ?? string.Empty;
            Logger.Debug($"Показано описание команды [{code}]");
        }

        private void RichTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Logger.Info($"Открытие ссылки из описания: [{e.LinkText}]");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.LinkText)
            {
                UseShellExecute = true
            });
        }

        private void OpenGitHub()
        {
            try
            {
                Logger.Info("Открытие страницы проекта на GitHub");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/i-savelev/PluginsManager",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Не удалось открыть GitHub");
                TaskDialog.Show("Ошибка", $"Не удалось открыть браузер:\n{ex.Message}");
            }
        }

        public void CreateTabs()
        {
            Logger.Info("Отрисовка вкладок");
            if (Command_manager.CommandsDictionary != null)
            {
                // Разделяем исключённые вкладки для точного сравнения
                var excludedTabs = UserConfig.exceptionTabs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                Logger.Debug($"[WindowManager] Исключённые вкладки: {excludedTabs.Length} шт.");

                foreach (var tab in Command_manager.CommandsDictionary)
                {
                    var isExcluded = excludedTabs.Contains(tab.Key);

                    if (string.Equals(UserConfig.post, UserConfigFile.ManagerPost, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Manager видит все вкладки, кроме исключённых
                        if (!isExcluded)
                        {
                            CreateNewTab(tab.Key);
                        }
                    }
                    else
                    {
                        // User не видит вкладки с '#' и исключённые
                        if (!isExcluded && !tab.Key.Contains('#'))
                        {
                            CreateNewTab(tab.Key);
                        }
                    }
                }
            }
        }

        private void CreateNewTab(string tabName)
        {
            TabPage tabPage = new TabPage
            {
                Text = tabName,
            };
            DataGridView dataGridView = CreateDataGrid(tabName);
            tabPage.Controls.Add(dataGridView);
            Window.tabControl.TabPages.Add(tabPage);
            Logger.Info($"Создана вкладка [{tabName}] с {dataGridView.Rows.Count} командами");
        }

        public DataGridView CreateDataGrid(string tabName)
        {
            DataGridView dataGridView = new DataGridView
            {
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                Name = $"table{tabName}",
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };

            DataGridViewCellStyle columnHeaderStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = SystemColors.Control,
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold),
                ForeColor = SystemColors.WindowText,
                SelectionBackColor = SystemColors.Highlight,
                SelectionForeColor = SystemColors.HighlightText,
                WrapMode = DataGridViewTriState.True
            };

            dataGridView.ColumnHeadersDefaultCellStyle = columnHeaderStyle;
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = SystemColors.Window,
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Regular),
                ForeColor = SystemColors.ControlText,
                SelectionBackColor = SystemColors.Highlight,
                SelectionForeColor = SystemColors.HighlightText,
                WrapMode = DataGridViewTriState.True,
            };

            dataGridView.DefaultCellStyle = cellStyle;

            DataGridViewCellStyle rowStyle = new DataGridViewCellStyle
            {
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Regular),
                WrapMode = DataGridViewTriState.True,
            };

            dataGridView.RowsDefaultCellStyle = rowStyle;
            dataGridView.ReadOnly = true;
            dataGridView.MultiSelect = false;
            dataGridView.RowHeadersDefaultCellStyle = columnHeaderStyle;
            dataGridView.RowHeadersVisible = false;
            dataGridView.RowHeadersWidth = 51;
            dataGridView.RowTemplate.Height = 50;
            dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;

            CreateColumns(dataGridView);
            foreach (var command in Command_manager.CommandsDictionary[tabName])
            {
                AddRowToDataGridView(dataGridView, string.Empty, command.CmdCode, command.CmdName, command.CmdImage);
            }
            dataGridView.CellClick += (s, e) =>
            {
                table_CellClickRunCommand(s, e, dataGridView);
                table_CellClickDescription(s, e, dataGridView);
            };

            Logger.Debug($"DataGrid для вкладки [{tabName}] создан, строк = {dataGridView.Rows.Count}");
            return dataGridView;
        }

        public void CreateColumns(DataGridView dataGridView)
        {
            DataGridViewImageColumn imageColumn = new DataGridViewImageColumn
            {
                Name = " ",
                Width = 50,
                HeaderText = " ",
            };
            DataGridViewButtonColumn buttonColumn = new DataGridViewButtonColumn
            {
                Name = "Выбор",
                UseColumnTextForButtonValue = false,
                Width = 50,
                HeaderText = " ",
            };
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn
            {
                Name = "id",
                HeaderText = "id",
                Visible = false,
            };
            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn
            {
                Name = "Имя",
                HeaderText = "Имя",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                Width = 200
            };
            dataGridView.Columns.Add(imageColumn);
            dataGridView.Columns.Add(idColumn);
            dataGridView.Columns.Add(nameColumn);
            dataGridView.Columns.Add(buttonColumn);
        }

        private void AddRowToDataGridView(DataGridView dataGridView, string buttonText, string code, string name, Image image)
        {
            int rowIndex = dataGridView.Rows.Add();
            DataGridViewRow row = dataGridView.Rows[rowIndex];

            row.Cells["Выбор"].Value = buttonText;
            row.Cells[" "].Value = image;
            row.Cells["id"].Value = code;
            row.Cells["Имя"].Value = name;
        }
    }
}
