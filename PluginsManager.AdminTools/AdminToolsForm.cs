using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace PluginsManager.AdminTools
{
    public class AdminToolsForm : Form
    {
        private DataGridView _dataGridView;
        private Button _btnSave;
        private Button _btnCancel;
        private Label _statusLabel;
        private CommandManager _commandManager;

        /// <summary>
        /// Хранит исходные (дефолтные) значения команды, полученные из DLL или пустые.
        /// </summary>
        private class CommandDefaults
        {
            public string Tab { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public Image OriginalImage { get; set; }
        }

        /// <summary>
        /// Словарь дефолтных значений по FullName команды.
        /// </summary>
        private Dictionary<string, CommandDefaults> _originalValues = new Dictionary<string, CommandDefaults>();

        public AdminToolsForm(CommandManager commandManager)
        {
            _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
            SetupForm();
            SetupControls();
            LoadDataToGrid();
        }

        private void SetupForm()
        {
            Text = "PluginsManager - Конфигурация команд";
            Size = new Size(1050, 700);
            StartPosition = FormStartPosition.CenterScreen;
            ShowIcon = false;
            MinimumSize = new Size(850, 500);
            FormBorderStyle = FormBorderStyle.Sizable;
        }

        private void SetupControls()
        {
            _dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = true,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders
            };
            EnableDoubleBuffering(_dataGridView);


            SetupDataGridViewColumns();

            _dataGridView.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    var col = _dataGridView.Columns[e.ColumnIndex];
                    _dataGridView.Cursor = (col.Name == "Preview" || col.Name == "Reset") ? Cursors.Hand : Cursors.Default;
                }
            };
            _dataGridView.CellMouseLeave += (s, e) => _dataGridView.Cursor = Cursors.Default;

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            _btnCancel = new Button { Text = "Отмена", Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            _btnCancel.Click += BtnCancel_Click;

            _btnSave = new Button { Text = "Сохранить", Width = 120, Height = 30 };
            _btnSave.Click += BtnSave_Click;

            buttonPanel.Controls.Add(_btnCancel);
            buttonPanel.Controls.Add(_btnSave);

            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 25,
                Padding = new Padding(5, 3, 0, 0),
                BackColor = Color.LightYellow,
                ForeColor = Color.DarkBlue,
                Text = "Готов",
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.Add(_dataGridView);
            Controls.Add(_statusLabel);
            Controls.Add(buttonPanel);

            CancelButton = _btnCancel;
            AcceptButton = _btnSave;
        }

        private void SetupDataGridViewColumns()
        {
            var colFullName = new DataGridViewTextBoxColumn { Name = "FullName", Visible = false, ReadOnly = true };

            var colAssembly = new DataGridViewTextBoxColumn
            {
                Name = "Assembly",
                HeaderText = "Сборка",
                FillWeight = 12,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            };

            var colClass = new DataGridViewTextBoxColumn
            {
                Name = "Class",
                HeaderText = "Класс",
                FillWeight = 15,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            };

            var colTab = new DataGridViewTextBoxColumn
            {
                Name = "Tab",
                HeaderText = "Вкладка",
                FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            };

            var colName = new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Имя команды",
                FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            };

            var colDescription = new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Описание",
                FillWeight = 22,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            };

            var colImageFileName = new DataGridViewTextBoxColumn { Name = "ImageFileName", Visible = false, ReadOnly = true };

            var colPreview = new DataGridViewImageColumn
            {
                Name = "Preview",
                HeaderText = "Изображение",
                FillWeight = 8,
                ImageLayout = DataGridViewImageCellLayout.Normal,
                Width = 60,
                MinimumWidth = 60,
                ValueType = typeof(Image),  // ← добавлено
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            var colReset = new DataGridViewButtonColumn
            {
                Name = "Reset",
                HeaderText = "Сброс",
                Text = "↺ Дефолт",
                UseColumnTextForButtonValue = true,
                FillWeight = 8,
                Width = 80,
                MinimumWidth = 80,
                FlatStyle = FlatStyle.Flat
            };

            // Источник данных (многострочный текст)
            var colSource = new DataGridViewTextBoxColumn
            {
                Name = "Source",
                HeaderText = "Источник",
                FillWeight = 10,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True,
                    Alignment = DataGridViewContentAlignment.TopLeft,
                    Font = new Font("Consolas", 8.5f)
                }
            };

            _dataGridView.Columns.Add(colFullName);
            _dataGridView.Columns.Add(colAssembly);
            _dataGridView.Columns.Add(colClass);
            _dataGridView.Columns.Add(colTab);
            _dataGridView.Columns.Add(colName);
            _dataGridView.Columns.Add(colDescription);
            _dataGridView.Columns.Add(colImageFileName);
            _dataGridView.Columns.Add(colPreview);
            _dataGridView.Columns.Add(colReset);
            _dataGridView.Columns.Add(colSource);

            _dataGridView.CellClick += DataGridView_CellClick;
            _dataGridView.CellBeginEdit += DataGridView_CellBeginEdit;
            _dataGridView.DataError += DataGridView_DataError;
        }
        /// <summary>
        /// Обработчик ошибок DataGridView. Подавляет стандартное окно и логирует ошибку.
        /// </summary>
        private void DataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Logger.Warning($"[AdminToolsForm] DataGridView DataError | Row={e.RowIndex} | Col={e.ColumnIndex} | {e.Exception?.Message}");
            e.ThrowException = false; // Подавляем окно ошибки
        }

        private Image ResizeImage(Image source, int targetSize)
        {
            if (source == null) return null;
            try
            {
                var resized = new Bitmap(targetSize, targetSize);
                using (var g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.DrawImage(source, 0, 0, targetSize, targetSize);
                }
                return resized;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "[AdminToolsForm] Ошибка масштабирования изображения");
                return source;
            }
        }

        /// <summary>
        /// Извлекает дефолтные значения для типа команды.
        /// Если команда есть в AllCommands — берёт данные оттуда.
        /// Если нет — возвращает пустые значения (пользователь заполнит вручную).
        /// </summary>
        private CommandDefaults GetDllDefaults(Type type)
        {
            var fullName = type.FullName;

            // Берём чистые данные из DLL (без учёта XML)
            if (_commandManager.DllMetadata.ContainsKey(fullName))
            {
                var dllMeta = _commandManager.DllMetadata[fullName];
                return new CommandDefaults
                {
                    Tab = dllMeta.Tab ?? string.Empty,
                    Name = dllMeta.Name ?? string.Empty,
                    Description = dllMeta.Description ?? string.Empty,
                    OriginalImage = dllMeta.Image
                };
            }

            // Команда не в DllMetadata — пустые дефолты
            Logger.Debug($"[AdminToolsForm] Команда {fullName} не найдена в DllMetadata, используются пустые дефолты");
            return new CommandDefaults();
        }

        private void LoadDataToGrid()
        {
            var allTypes = _commandManager.AllTypes;
            Logger.Info($"[AdminToolsForm] Загрузка данных в таблицу | Всего типов: {allTypes.Count}");
            _statusLabel.Text = "Загрузка данных...";
            _dataGridView.SuspendLayout();
            _dataGridView.Rows.Clear();
            _originalValues.Clear();

            foreach (var type in allTypes)
            {
                var fullName = type.FullName;
                if (string.IsNullOrEmpty(fullName)) continue;

                // Дефолтные значения (из AllCommands или пустые)
                var defaults = GetDllDefaults(type);
                _originalValues[fullName] = defaults;

                // Текущие значения для отображения
                var currentTab = defaults.Tab;
                var currentName = defaults.Name;
                var currentDescription = defaults.Description;
                var currentImageFileName = string.Empty;
                var currentImage = defaults.OriginalImage;

                // Переопределения из XML
                if (CommandConfig.CommamdConfigDictionary != null && CommandConfig.CommamdConfigDictionary.ContainsKey(fullName))
                {
                    var config = CommandConfig.CommamdConfigDictionary[fullName];

                    var xmlTab = config.ContainsKey("CmdTab") ? config["CmdTab"] : string.Empty;
                    if (!string.IsNullOrEmpty(xmlTab)) currentTab = xmlTab;

                    var xmlName = config.ContainsKey("CmdName") ? config["CmdName"] : string.Empty;
                    if (!string.IsNullOrEmpty(xmlName)) currentName = xmlName;

                    var xmlDescription = config.ContainsKey("CmdDescription") ? config["CmdDescription"] : string.Empty;
                    if (!string.IsNullOrEmpty(xmlDescription)) currentDescription = xmlDescription;

                    var xmlImage = config.ContainsKey("CmdImage") ? config["CmdImage"] : string.Empty;
                    if (!string.IsNullOrEmpty(xmlImage))
                    {
                        currentImageFileName = xmlImage;
                        var imgPath = Path.Combine(PathManager.sourceDir, PluginsManager.Const.CmdConfigFile.ImageFolderName, xmlImage);
                        if (File.Exists(imgPath))
                        {
                            try
                            {
                                using (var stream = new MemoryStream(File.ReadAllBytes(imgPath)))
                                {
                                    currentImage = Image.FromStream(stream);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"[AdminToolsForm] Не удалось загрузить изображение: {imgPath} | {ex.Message}");
                            }
                        }
                    }
                }

                // Определение источника каждого поля
                var tabSource = "DLL";
                var nameSource = "DLL";
                var descSource = "DLL";
                var imgSource = "DLL";

                if (CommandConfig.CommamdConfigDictionary != null && CommandConfig.CommamdConfigDictionary.ContainsKey(fullName))
                {
                    var config = CommandConfig.CommamdConfigDictionary[fullName];

                    if (config.ContainsKey("CmdTab") && !string.IsNullOrWhiteSpace(config["CmdTab"]))
                        tabSource = "XML";

                    if (config.ContainsKey("CmdName") && !string.IsNullOrWhiteSpace(config["CmdName"]))
                        nameSource = "XML";

                    if (config.ContainsKey("CmdDescription") && !string.IsNullOrWhiteSpace(config["CmdDescription"]))
                        descSource = "XML";

                    if (config.ContainsKey("CmdImage") && !string.IsNullOrWhiteSpace(config["CmdImage"]))
                        imgSource = "XML";
                }

                var sourceText = $"Tab: {tabSource}\nName: {nameSource}\nDesc: {descSource}\nImage: {imgSource}";

                var assemblyName = string.Empty;
                var className = string.Empty;
                ParseFullName(fullName, out assemblyName, out className);

                var imageToDisplay = currentImage != null ? ResizeImage(currentImage, 50) : new Bitmap(1, 1);

                _dataGridView.Rows.Add(
                    fullName,
                    assemblyName,
                    className,
                    currentTab,
                    currentName,
                    currentDescription,
                    currentImageFileName,
                    imageToDisplay, 
                    "↺ Дефолт",
                    sourceText
                );
            }
            _dataGridView.ResumeLayout();
            _statusLabel.Text = $"Загружено: {allTypes.Count} команд";
            Logger.Info($"[AdminToolsForm] Данные загружены в таблицу | Строк: {_dataGridView.Rows.Count}");
        }
        /// <summary>
        /// Включает двойную буферизацию для DataGridView, устраняя мерцание и зависания при прокрутке.
        /// </summary>
        private static void EnableDoubleBuffering(DataGridView dgv)
        {
            var propertyInfo = dgv.GetType().GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            propertyInfo?.SetValue(dgv, true, null);
        }

        private void ParseFullName(string fullName, out string assemblyName, out string className)
        {
            assemblyName = string.Empty;
            className = fullName ?? string.Empty;
            if (string.IsNullOrEmpty(fullName)) return;

            var dotIndex = fullName.IndexOf('.');
            if (dotIndex > 0)
            {
                assemblyName = fullName.Substring(0, dotIndex);
                className = fullName.Substring(dotIndex + 1);
            }
            else
            {
                assemblyName = fullName;
                className = string.Empty;
            }
        }

        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var columnName = _dataGridView.Columns[e.ColumnIndex].Name;

            if (columnName == "Preview")
            {
                SelectImageForRow(e.RowIndex);
            }
            else if (columnName == "Reset")
            {
                ResetRowToDefaults(e.RowIndex);
            }
        }

        /// <summary>
        /// Сбрасывает значения строки к дефолтным (из DLL или пустым).
        /// </summary>
        private void ResetRowToDefaults(int rowIndex)
        {
            var fullName = _dataGridView.Rows[rowIndex].Cells["FullName"].Value?.ToString();
            if (string.IsNullOrEmpty(fullName) || !_originalValues.ContainsKey(fullName))
            {
                Logger.Warning($"[AdminToolsForm] Не удалось найти дефолты для {fullName}");
                return;
            }

            var defaults = _originalValues[fullName];
            _dataGridView.Rows[rowIndex].Cells["Tab"].Value = defaults.Tab;
            _dataGridView.Rows[rowIndex].Cells["Name"].Value = defaults.Name;
            _dataGridView.Rows[rowIndex].Cells["Description"].Value = defaults.Description;
            _dataGridView.Rows[rowIndex].Cells["ImageFileName"].Value = string.Empty;

            var resizedImg = defaults.OriginalImage != null ? ResizeImage(defaults.OriginalImage, 50) : null;
            _dataGridView.Rows[rowIndex].Cells["Preview"].Value = (object)resizedImg ?? DBNull.Value;
            var sourceText = "Tab: DLL\nName: DLL\nDesc: DLL\nImage: DLL";
            _dataGridView.Rows[rowIndex].Cells["Source"].Value = sourceText;
            Logger.Info($"[AdminToolsForm] Строка сброшена к дефолтам: {fullName}");
            _statusLabel.Text = $"Сброшено к дефолтам: {_dataGridView.Rows[rowIndex].Cells["Name"].Value}";
        }

        private void DataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var columnName = _dataGridView.Columns[e.ColumnIndex].Name;

            if (columnName == "FullName" || columnName == "Assembly" || columnName == "Class" ||
                columnName == "ImageFileName" || columnName == "Reset")
                return;

            if (columnName == "Description" || columnName == "Tab" || columnName == "Name")
            {
                var currentValue = _dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;

                using (var editForm = new MultilineEditForm(currentValue, columnName))
                {
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        _dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = editForm.EditedText;
                    }
                }
                e.Cancel = true;
            }
        }

        private void SelectImageForRow(int rowIndex)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Выберите изображение для команды";
                dialog.Filter = "Изображения|*.png;*.jpg;*.jpeg;*.ico|Все файлы|*.*";
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var sourcePath = dialog.FileName;
                    var fileName = Path.GetFileName(sourcePath);

                    var imgDir = Path.Combine(PathManager.sourceDir, PluginsManager.Const.CmdConfigFile.ImageFolderName);
                    if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);

                    var destPath = Path.Combine(imgDir, fileName);
                    try
                    {
                        // 1. Читаем байты оригинала без блокировки файла
                        byte[] originalBytes = File.ReadAllBytes(sourcePath);
                        System.Drawing.Imaging.ImageFormat format;

                        // 2. Открываем, определяем формат, масштабируем и сохраняем
                        using (var ms = new MemoryStream(originalBytes))
                        using (var originalImg = Image.FromStream(ms))
                        {
                            format = GetImageFormat(originalImg, sourcePath);

                            using (var resizedImg = ResizeImage(originalImg, 50))
                            {
                                if (format.Equals(System.Drawing.Imaging.ImageFormat.Jpeg))
                                {
                                    // Для JPEG обязательно задаем качество, чтобы GDI+ не сломал заголовки
                                    var encoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                                        .First(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                                    var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                                    encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                                    resizedImg.Save(destPath, encoder, encoderParams);
                                }
                                else
                                {
                                    // PNG, GIF, BMP сохраняем в их нативных форматах (сохраняя прозрачность для PNG/GIF)
                                    resizedImg.Save(destPath, format);
                                }
                            }
                        }

                        Logger.Info($"[AdminToolsForm] Изображение добавлено и масштабировано (50x50): {fileName}");

                        // 3. Обновляем UI
                        _dataGridView.Rows[rowIndex].Cells["ImageFileName"].Value = fileName;

                        using (var savedStream = new MemoryStream(File.ReadAllBytes(destPath)))
                        {
                            var displayImg = Image.FromStream(savedStream);
                            _dataGridView.Rows[rowIndex].Cells["Preview"].Value = displayImg;
                        }

                        _statusLabel.Text = $"Изображение добавлено (50x50): {fileName}";
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex, $"[AdminToolsForm] Ошибка копирования изображения: {sourcePath}");
                        MessageBox.Show($"Не удалось скопировать изображение:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private System.Drawing.Imaging.ImageFormat GetImageFormat(Image img, string path)
        {
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png)) return System.Drawing.Imaging.ImageFormat.Png;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg)) return System.Drawing.Imaging.ImageFormat.Jpeg;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Gif)) return System.Drawing.Imaging.ImageFormat.Gif;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Bmp)) return System.Drawing.Imaging.ImageFormat.Bmp;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Icon)) return System.Drawing.Imaging.ImageFormat.Icon;

            // Фоллбэк по расширению файла, если RawFormat не распознан
            var ext = Path.GetExtension(path)?.ToLower();
            return ext == ".png" ? System.Drawing.Imaging.ImageFormat.Png : System.Drawing.Imaging.ImageFormat.Jpeg;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _statusLabel.Text = "Сохранение...";
            try
            {
                SaveConfiguration();
                _statusLabel.Text = "Конфигурация сохранена успешно";
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "[AdminToolsForm] Ошибка сохранения конфигурации");
                _statusLabel.Text = "Ошибка сохранения";
                MessageBox.Show($"Не удалось сохранить конфигурацию:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            _statusLabel.Text = "Отменено";
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Сравнивает текущее значение с дефолтным. Пустая строка и null считаются эквивалентными.
        /// </summary>
        private bool IsChanged(string current, string original)
        {
            var c = current?.Trim() ?? string.Empty;
            var o = original?.Trim() ?? string.Empty;
            return !string.Equals(c, o, StringComparison.Ordinal);
        }

        private void SaveConfiguration()
        {
            var xmlPath = PathManager.sourceComandConfigFile;
            var root = new XElement("Commands");
            var savedCount = 0;
            var skippedCount = 0;

            foreach (DataGridViewRow row in _dataGridView.Rows)
            {
                var fullName = row.Cells["FullName"].Value?.ToString();
                var currentTab = row.Cells["Tab"].Value?.ToString() ?? string.Empty;
                var currentName = row.Cells["Name"].Value?.ToString() ?? string.Empty;
                var currentDescription = row.Cells["Description"].Value?.ToString() ?? string.Empty;
                var currentImageFileName = row.Cells["ImageFileName"].Value?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(fullName) || !_originalValues.ContainsKey(fullName))
                {
                    Logger.Warning($"[AdminToolsForm] Пропущена строка без FullName или дефолтов");
                    continue;
                }

                var defaults = _originalValues[fullName];

                var tabChanged = IsChanged(currentTab, defaults.Tab);
                var nameChanged = IsChanged(currentName, defaults.Name);
                var descChanged = IsChanged(currentDescription, defaults.Description);
                var imageChanged = !string.IsNullOrWhiteSpace(currentImageFileName);

                if (!tabChanged && !nameChanged && !descChanged && !imageChanged)
                {
                    skippedCount++;
                    Logger.Debug($"[AdminToolsForm] Команда пропущена (все поля дефолтные): {fullName}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(currentTab))
                {
                    Logger.Warning($"[AdminToolsForm] Пропущена команда с пустой вкладкой: {fullName}");
                    skippedCount++;
                    continue;
                }

                var commandElement = new XElement("Command", new XElement("CmdCode", fullName));

                if (tabChanged) commandElement.Add(new XElement("CmdTab", currentTab));
                if (nameChanged) commandElement.Add(new XElement("CmdName", currentName));
                if (descChanged) commandElement.Add(new XElement("CmdDescription", currentDescription));
                if (imageChanged) commandElement.Add(new XElement("CmdImage", currentImageFileName));

                root.Add(commandElement);
                savedCount++;

                Logger.Debug($"[AdminToolsForm] Сохранена команда {fullName} | tab={tabChanged}, name={nameChanged}, desc={descChanged}, img={imageChanged}");
            }

            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
            doc.Save(xmlPath);
            Logger.Info($"[AdminToolsForm] Конфигурация сохранена | Записано: {savedCount} | Пропущено (дефолт): {skippedCount} | Файл: {xmlPath}");
        }
    }

    public class MultilineEditForm : Form
    {
        private TextBox _textBox;
        private Button _btnOk;
        private Button _btnCancel;

        public string EditedText => _textBox.Text;

        public MultilineEditForm(string initialValue, string fieldName)
        {
            SetupForm(fieldName);
            SetupControls();
            _textBox.Text = initialValue;
        }

        private void SetupForm(string fieldName)
        {
            Text = $"Редактирование: {fieldName}";
            Size = new Size(500, 400);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
        }

        private void SetupControls()
        {
            _textBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                AcceptsReturn = true,
                Font = new Font("Segoe UI", 10f)
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };

            _btnCancel = new Button { Text = "Отмена", Width = 80, Height = 30, DialogResult = DialogResult.Cancel };
            _btnOk = new Button { Text = "OK", Width = 80, Height = 30, DialogResult = DialogResult.OK };

            buttonPanel.Controls.Add(_btnCancel);
            buttonPanel.Controls.Add(_btnOk);

            Controls.Add(_textBox);
            Controls.Add(buttonPanel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }
    }
}