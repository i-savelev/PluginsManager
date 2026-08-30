using System;
using System.Drawing;
using System.Windows.Forms;
using WixSharp;
using WixSharp.UI.Forms;

namespace Build
{
    public class AdminChoiceDialog : ManagedForm, IManagedDialog
    {
        public RadioButton RbRegular { get; private set; }
        public RadioButton RbAdmin { get; private set; }
        public Label LblDescription { get; private set; }
        private Label _statusLabel;

        private Button _btnNext;
        private Button _btnCancel;

        public AdminChoiceDialog()
        {
            SetupForm();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetupInstallControls();
        }

        private void SetupForm()
        {
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            SetupStatusBar();
        }

        private void SetupStatusBar()
        {
            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                Padding = new Padding(5, 3, 0, 0),
                BackColor = Color.LightYellow,
                ForeColor = Color.DarkBlue,
                Text = ""
            };
            this.Controls.Add(_statusLabel);
        }

        private void SetupInstallControls()
        {
            this.Text = "Установка Plugins Manager";
            this.Size = new Size(450, 340);

            LblDescription = new Label
            {
                Text = "Выберите версию для установки:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold)
            };

            RbRegular = new RadioButton
            {
                Text = "Обычная версия",
                Location = new Point(20, 55), 
                AutoSize = true,
                Checked = true
            };
            RbRegular.CheckedChanged += (s, e) => UpdateStatus();

            RbAdmin = new RadioButton
            {
                Text = "Версия для администратора (AdminTools)",
                Location = new Point(20, 85),
                AutoSize = true,
                Checked = false
            };
            RbAdmin.CheckedChanged += (s, e) => UpdateStatus();

            _btnNext = new Button
            {
                Text = "Далее >",
                Location = new Point(230, 140),
                Size = new Size(90, 25)
            };
            _btnNext.Click += BtnNext_Click;

            _btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(330, 140),
                Size = new Size(90, 25)
            };
            _btnCancel.Click += BtnCancel_Click;

            this.Controls.Add(LblDescription);
            this.Controls.Add(RbRegular);
            this.Controls.Add(RbAdmin);
            this.Controls.Add(_btnNext);
            this.Controls.Add(_btnCancel);

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            _statusLabel.Text = RbAdmin.Checked
                ? "Выбрано: Версия для администратора"
                : "Выбрано: Обычная версия";
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            MsiRuntime.Session["INSTALL_ADMIN"] = RbAdmin.Checked ? "1" : "0";
            Shell.GoNext();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Shell.Cancel();
        }
    }
}