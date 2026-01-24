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
            window.Text = $"Менеджер плагинов";
            window.groupBox1.Text = $"Информация";
            window.tabControl.Font = new Font("Microsoft Sans Serif", 10, FontStyle.Regular);
            window.toolStripButton1.Click += (s, e) => ChangeFolder();
            window.toolStripButton1.Text = "Выбрать папку";
            window.toolStripButton2.Click += (s, e) => OpenGitHub();
            window.toolStripButton2.Text = "GitHub";
            window.toolStripButton3.Click += (s, e) => { Refrash(); };
            window.toolStripButton3.Text = "Обновить";
            window.richTextBox1.LinkClicked += RichTextBox1_LinkClicked;
            CreateTabs();
            window.ShowDialog();
        }

        private void ChangeFolder()
        {
            Logger.Separator();
            Logger.Info("ChangeFolder", $"Изменение пути к корневой папке...");
            var dialogResult = UserConfig.SetPathToUserConfigFileDialog();
            if (dialogResult)
            {
                UserConfig.GetUserSettings();
                CommandConfig.CreateConfigFile();
                CommandConfig.GetDllSettings();
                Dllmanager.CopyDll();
                Command_manager.Refresh(PathManager.tempDllDir);
                Window.tabControl.TabPages.Clear();
                CreateTabs();
            }
        }

        private void Refrash()
        {
            Logger.Separator();
            Logger.Info("ChangeFolder", $"Обновление...");
            TempFiles.CopyToTemp(PathManager.sourceDir);
            CommandConfig.GetDllSettings();
            Dllmanager.CopyDll();
            Command_manager.Refresh(PathManager.tempDllDir);
            Window.tabControl.TabPages.Clear();
            CreateTabs();
        }

        void table_CellClickRunCommand(object sender, DataGridViewCellEventArgs e, DataGridView dataGridView)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dataGridView.Columns["Выбор"].Index) return;
            GlobComandName.Name = dataGridView["id", e.RowIndex].Value.ToString();
            External_event.Raise();
            Window.Close();
        }

        void table_CellClickDescription(object sender, DataGridViewCellEventArgs e, DataGridView dataGridView)
        {
            if (e.RowIndex < 0) return;
            string code = dataGridView["id", e.RowIndex].Value.ToString();
            var command = Command_manager.AllCommands.FirstOrDefault(cmd => cmd.CmdCode == code);
            Window.richTextBox1.Text = command.CmdDescription;
        }

        private void RichTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.LinkText)
            {
                UseShellExecute = true
            });
        }

        private void OpenGitHub()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/i-savelev/PluginsManager",
                    UseShellExecute = true // ← обязательно!
                });
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Не удалось открыть браузер:\n{ex.Message}");
            }
        }

        public void CreateTabs()
        {
            Logger.Info("CreateTabs", $"Отрисовка вкладок...");
            if (Command_manager.CommandsDictionary != null)
            {
                foreach (var tab in Command_manager.CommandsDictionary)
                {
                    Logger.Info("CreateTabs", $"Вкладка {tab}");
                    if (string.Equals(UserConfig.post, UserConfigFile.ManagerPost, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Info("CreateTabs", $"Роль пользователя {UserConfig.post}, добавляем вкладки с '#'");
                        if (!UserConfig.exceptionTabs.Contains(tab.Key))
                        {
                            CreateNewTab(tab.Key);
                        }
                    }
                    else
                    {
                        if (!UserConfig.exceptionTabs.Contains(tab.Key) & !tab.Key.Contains('#'))
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
                AddRowToDataGridView(dataGridView, "", command.CmdCode, command.CmdName, command.CmdImage);
            }
            dataGridView.CellClick += (s, e) => {
                table_CellClickRunCommand(s, e, dataGridView);
                table_CellClickDescription(s, e, dataGridView);
            };

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
            DataGridViewTextBoxColumn NameColumn = new DataGridViewTextBoxColumn
            {
                Name = "Имя",
                HeaderText = "Имя",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                Width = 200
            };
            dataGridView.Columns.Add(imageColumn);
            dataGridView.Columns.Add(idColumn);
            dataGridView.Columns.Add(NameColumn);
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
