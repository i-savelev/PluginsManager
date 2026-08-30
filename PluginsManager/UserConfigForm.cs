using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PluginsManager
{
    /// <summary>
    /// Форма для управления настройками пользователя (user_config.xml).
    /// Позволяет изменить исходную папку, роль и видимость вкладок.
    /// </summary>
    public class UserConfigForm : Form
    {
        private TextBox _txtFolderPath;
        private Button _btnSelectFolder;
        private TextBox _txtPost;
        private CheckedListBox _chkTabs;
        private Button _btnSave;
        private Button _btnCancel;

        private string _exceptionTabs;

        /// <summary>
        /// Выбранный путь к исходной папке.
        /// </summary>
        public string SelectedFolderPath { get; private set; }

        /// <summary>
        /// Выбранная роль пользователя.
        /// </summary>
        public string SelectedPost { get; private set; }

        /// <summary>
        /// Строка с исключёнными вкладками (через точку с запятой).
        /// </summary>
        public string ExcludedTabsString { get; private set; }

        public UserConfigForm(string folderPath, string exceptionTabs, string post, IEnumerable<string> allTabs)
        {
            _exceptionTabs = exceptionTabs ?? string.Empty;
            SelectedFolderPath = folderPath ?? string.Empty;
            SelectedPost = post ?? Const.UserConfigFile.DefaultPost;

            SetupForm();
            SetupControls(allTabs);
        }

        private void SetupForm()
        {
            Text = "Плагины - Настройки";
            Size = new Size(600, 500);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            MinimumSize = new Size(500, 400);
            FormBorderStyle = FormBorderStyle.Sizable;
            TopMost = true;
        }

        private void SetupControls(IEnumerable<string> allTabs)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 5,
                Padding = new Padding(10)
            };

            // Настройка колонок
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

            // Настройка строк
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));  // Папка
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));  // Роль
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));  // Заголовок вкладок
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Список вкладок
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Кнопки

            // --- Строка 0: Исходная папка ---
            var lblFolder = new Label
            {
                Text = "Исходная папка:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _txtFolderPath = new TextBox
            {
                Text = SelectedFolderPath,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control
            };

            _btnSelectFolder = new Button
            {
                Text = "Выбрать",
                Dock = DockStyle.Fill,
                Height = 20
            };
            _btnSelectFolder.Click += BtnSelectFolder_Click;

            layout.Controls.Add(lblFolder, 0, 0);
            layout.Controls.Add(_txtFolderPath, 1, 0);
            layout.Controls.Add(_btnSelectFolder, 2, 0);

            // --- Строка 1: Роль ---
            var lblPost = new Label
            {
                Text = "Роль:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _txtPost = new TextBox
            {
                Text = SelectedPost,
                Dock = DockStyle.Fill
            };

            layout.Controls.Add(lblPost, 0, 1);
            layout.Controls.Add(_txtPost, 1, 1);

            // --- Строка 2: Заголовок вкладок ---
            var lblTabs = new Label
            {
                Text = "Вкладки (отметьте видимые):",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            layout.Controls.Add(lblTabs, 0, 2);
            layout.SetColumnSpan(lblTabs, 3);

            // --- Строка 3: Список вкладок ---
            _chkTabs = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                Font = new Font("Segoe UI", 10f)
            };

            // Заполняем список вкладок
            var excludedList = _exceptionTabs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var tab in allTabs.OrderBy(t => t))
            {
                var isVisible = !excludedList.Contains(tab);
                _chkTabs.Items.Add(tab, isVisible);
            }

            layout.Controls.Add(_chkTabs, 0, 3);
            layout.SetColumnSpan(_chkTabs, 3);

            // --- Строка 4: Кнопки ---
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };

            _btnCancel = new Button
            {
                Text = "Отмена",
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.Click += BtnCancel_Click;

            _btnSave = new Button
            {
                Text = "Сохранить",
                Width = 120,
                Height = 30
            };
            _btnSave.Click += BtnSave_Click;

            buttonPanel.Controls.Add(_btnCancel);
            buttonPanel.Controls.Add(_btnSave);

            layout.Controls.Add(buttonPanel, 0, 4);
            layout.SetColumnSpan(buttonPanel, 3);

            Controls.Add(layout);

            CancelButton = _btnCancel;
            AcceptButton = _btnSave;
        }

        private void BtnSelectFolder_Click(object sender, EventArgs e)
        {
            Logger.Debug("[UserConfigForm] Открытие диалога выбора папки");

            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Выберите папку с файлами .dll";
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.DereferenceLinks = true;
                dialog.FileName = "Выбор папки";
                dialog.Filter = "Все папки|*.*";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var folderPath = Path.GetDirectoryName(dialog.FileName);
                    _txtFolderPath.Text = folderPath;
                    Logger.Info($"[UserConfigForm] Выбрана папка: {folderPath}");
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Logger.Info("[UserConfigForm] Начало сохранения настроек");

            try
            {
                // Собираем результаты
                SelectedFolderPath = _txtFolderPath.Text;
                SelectedPost = _txtPost.Text.Trim();

                // Собираем исключённые вкладки (те, у которых снят чекбокс)
                var excludedTabs = new List<string>();
                for (int i = 0; i < _chkTabs.Items.Count; i++)
                {
                    if (!_chkTabs.GetItemChecked(i))
                    {
                        excludedTabs.Add(_chkTabs.Items[i].ToString());
                    }
                }
                ExcludedTabsString = string.Join(";", excludedTabs);

                Logger.Info($"[UserConfigForm] Настройки собраны | FolderPath={SelectedFolderPath} | Post={SelectedPost} | ExcludedTabs={ExcludedTabsString}");

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "[UserConfigForm] Ошибка сохранения настроек");
                MessageBox.Show($"Не удалось сохранить настройки:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Logger.Info("[UserConfigForm] Отмена изменений");
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}