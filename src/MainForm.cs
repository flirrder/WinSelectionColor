using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace WinSelectionColor
{
    public class MainForm : Form
    {
        // State
        private SelectionColorSettings _currentSettings;
        private SelectionColorSettings _backupSettings;
        private bool _isUpdatingUi = false;
        private Image _wallpaperThumb = null;
        private List<Color> _wallpaperPalette = new List<Color>();
        private string _wallpaperPath = null;

        // UI Controls
        private Button _btnEyedropper;
        private FlowLayoutPanel _palettePanel;
        private PictureBox _picWallpaperThumb;
        private Label _lblWallpaperStatus;

        // Preview Controls
        private Panel _previewDesktopPanel;
        private Panel _previewTextPanel;
        private Label _lblRgbValue;
        private Panel _swatchCurrent;

        // Sliders & Numbers
        private TrackBar _tbRed;
        private TrackBar _tbGreen;
        private TrackBar _tbBlue;
        private NumericUpDown _numRed;
        private NumericUpDown _numGreen;
        private NumericUpDown _numBlue;
        private TextBox _txtHex;
        private CheckBox _chkAutoTextContrast;
        private CheckBox _chkAutoBorder;

        // Action Buttons
        private Button _btnApply;
        private Button _btnRestartExplorer;
        private Button _btnResetDefault;
        private Button _btnRestoreBackup;
        private Button _btnMoreColors;
        private Label _lblStatus;

        // Colors for modern theme
        private readonly Color ClrBg = Color.FromArgb(24, 24, 27);
        private readonly Color ClrCard = Color.FromArgb(34, 34, 39);
        private readonly Color ClrCardBorder = Color.FromArgb(52, 52, 60);
        private readonly Color ClrTextPrimary = Color.FromArgb(244, 244, 245);
        private readonly Color ClrTextMuted = Color.FromArgb(161, 161, 170);
        private readonly Color ClrAccent = Color.FromArgb(59, 130, 246);
        private readonly Color ClrSuccess = Color.FromArgb(34, 197, 94);

        public MainForm()
        {
            InitializeComponent();
            LoadInitialData();
        }

        private void InitializeComponent()
        {
            Text = "WinSelectionColor — Цвет выделения Windows 10";
            Size = new Size(680, 850);
            MinimumSize = new Size(660, 800);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ClrBg;
            ForeColor = ClrTextPrimary;
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            DoubleBuffered = true;
            Icon = SystemIcons.Application;

            // Main scrollable container
            Panel container = new Panel();
            container.Dock = DockStyle.Fill;
            container.AutoScroll = true;
            container.Padding = new Padding(20, 16, 20, 20);
            Controls.Add(container);

            int y = 12;

            // --- Header ---
            Label lblTitle = new Label();
            lblTitle.Text = "Настройка цвета выделения Windows 10";
            lblTitle.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            lblTitle.ForeColor = ClrTextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(18, y);
            container.Controls.Add(lblTitle);
            y += 32;

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Подберите цвет рамки и текста выделения под цвет ваших обоев с помощью пипетки";
            lblSubtitle.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblSubtitle.ForeColor = ClrTextMuted;
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(20, y);
            container.Controls.Add(lblSubtitle);
            y += 34;

            // --- Card 1: Пипетка и Обои ---
            Panel cardEyedropper = CreateCardPanel(20, y, 620, 160);
            container.Controls.Add(cardEyedropper);

            Label lblCard1Title = CreateCardHeader("1. ВЫБОР ЦВЕТА С ЭКРАНА И ОБОЕВ", 16, 12);
            cardEyedropper.Controls.Add(lblCard1Title);

            // Eyedropper Button
            _btnEyedropper = new Button();
            _btnEyedropper.Text = "🔍 Экранная пипетка (взять цвет с обоев или окна)";
            _btnEyedropper.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _btnEyedropper.ForeColor = Color.White;
            _btnEyedropper.BackColor = Color.FromArgb(79, 70, 229); // Indigo
            _btnEyedropper.FlatStyle = FlatStyle.Flat;
            _btnEyedropper.FlatAppearance.BorderSize = 0;
            _btnEyedropper.FlatAppearance.MouseOverBackColor = Color.FromArgb(99, 90, 245);
            _btnEyedropper.Cursor = Cursors.Hand;
            _btnEyedropper.SetBounds(16, 38, 410, 44);
            _btnEyedropper.Click += BtnEyedropper_Click;
            cardEyedropper.Controls.Add(_btnEyedropper);

            // Wallpaper Thumbnail
            _picWallpaperThumb = new PictureBox();
            _picWallpaperThumb.SetBounds(440, 38, 160, 104);
            _picWallpaperThumb.SizeMode = PictureBoxSizeMode.Zoom;
            _picWallpaperThumb.BackColor = Color.FromArgb(20, 20, 24);
            _picWallpaperThumb.BorderStyle = BorderStyle.FixedSingle;
            cardEyedropper.Controls.Add(_picWallpaperThumb);

            // Wallpaper Palette Label
            _lblWallpaperStatus = new Label();
            _lblWallpaperStatus.Text = "Палитра текущих обоев (кликните для выбора):";
            _lblWallpaperStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            _lblWallpaperStatus.ForeColor = ClrTextMuted;
            _lblWallpaperStatus.AutoSize = true;
            _lblWallpaperStatus.Location = new Point(16, 92);
            cardEyedropper.Controls.Add(_lblWallpaperStatus);

            // Swatches FlowPanel
            _palettePanel = new FlowLayoutPanel();
            _palettePanel.SetBounds(16, 114, 410, 36);
            _palettePanel.WrapContents = false;
            _palettePanel.AutoScroll = false;
            cardEyedropper.Controls.Add(_palettePanel);

            y += 172;

            // --- Card 2: Предпросмотр (Live Preview) ---
            Panel cardPreview = CreateCardPanel(20, y, 620, 175);
            container.Controls.Add(cardPreview);

            Label lblCard2Title = CreateCardHeader("2. ПРЕДПРОСМОТР ВЫДЕЛЕНИЯ (ПРОВОДНИК И ТЕКСТ)", 16, 12);
            cardPreview.Controls.Add(lblCard2Title);

            // Desktop Marquee Selection Preview
            _previewDesktopPanel = new Panel();
            _previewDesktopPanel.SetBounds(16, 38, 280, 122);
            _previewDesktopPanel.BackColor = Color.FromArgb(26, 30, 38);
            _previewDesktopPanel.BorderStyle = BorderStyle.FixedSingle;
            _previewDesktopPanel.Paint += PreviewDesktopPanel_Paint;
            cardPreview.Controls.Add(_previewDesktopPanel);

            // Text Selection Preview
            _previewTextPanel = new Panel();
            _previewTextPanel.SetBounds(308, 38, 296, 68);
            _previewTextPanel.BackColor = Color.White;
            _previewTextPanel.BorderStyle = BorderStyle.FixedSingle;
            _previewTextPanel.Paint += PreviewTextPanel_Paint;
            cardPreview.Controls.Add(_previewTextPanel);

            // Color Swatch
            _swatchCurrent = new Panel();
            _swatchCurrent.SetBounds(308, 118, 36, 36);
            _swatchCurrent.BorderStyle = BorderStyle.FixedSingle;
            cardPreview.Controls.Add(_swatchCurrent);

            // Hex Input TextBox
            _txtHex = new TextBox();
            _txtHex.Text = "#0078D7";
            _txtHex.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _txtHex.ForeColor = ClrTextPrimary;
            _txtHex.BackColor = Color.FromArgb(24, 24, 28);
            _txtHex.BorderStyle = BorderStyle.FixedSingle;
            _txtHex.SetBounds(352, 118, 85, 24);
            _txtHex.TextChanged += TxtHex_TextChanged;
            cardPreview.Controls.Add(_txtHex);

            // RGB Label (neatly aligned under Hex box)
            _lblRgbValue = new Label();
            _lblRgbValue.Text = "RGB: 0, 120, 215";
            _lblRgbValue.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            _lblRgbValue.ForeColor = ClrTextMuted;
            _lblRgbValue.AutoSize = true;
            _lblRgbValue.Location = new Point(352, 142);
            cardPreview.Controls.Add(_lblRgbValue);

            // Windows Palette Button (with ample spacing)
            _btnMoreColors = new Button();
            _btnMoreColors.Text = "🎨 Палитра...";
            _btnMoreColors.Font = new Font("Segoe UI", 9f);
            _btnMoreColors.ForeColor = ClrTextPrimary;
            _btnMoreColors.BackColor = Color.FromArgb(48, 48, 56);
            _btnMoreColors.FlatStyle = FlatStyle.Flat;
            _btnMoreColors.FlatAppearance.BorderColor = ClrCardBorder;
            _btnMoreColors.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 72);
            _btnMoreColors.Cursor = Cursors.Hand;
            _btnMoreColors.SetBounds(488, 118, 116, 36);
            _btnMoreColors.Click += BtnMoreColors_Click;
            cardPreview.Controls.Add(_btnMoreColors);

            y += 187;

            // --- Card 3: Точная настройка RGB и параметров ---
            Panel cardSettings = CreateCardPanel(20, y, 620, 178);
            container.Controls.Add(cardSettings);

            Label lblCard3Title = CreateCardHeader("3. ТОЧНАЯ НАСТРОЙКА КАНАЛОВ ЦВЕТА", 16, 12);
            cardSettings.Controls.Add(lblCard3Title);

            // Red Slider
            CreateColorSlider(cardSettings, "R (Красный):", Color.FromArgb(239, 68, 68), 36,
                out _tbRed, out _numRed, (s, e) => OnSliderChanged());

            // Green Slider
            CreateColorSlider(cardSettings, "G (Зеленый):", Color.FromArgb(34, 197, 94), 68,
                out _tbGreen, out _numGreen, (s, e) => OnSliderChanged());

            // Blue Slider
            CreateColorSlider(cardSettings, "B (Синий):", Color.FromArgb(59, 130, 246), 100,
                out _tbBlue, out _numBlue, (s, e) => OnSliderChanged());

            // Checkboxes (with comfortable vertical margin)
            _chkAutoTextContrast = new CheckBox();
            _chkAutoTextContrast.Text = "Авто-контраст текста (белый/черный)";
            _chkAutoTextContrast.Checked = true;
            _chkAutoTextContrast.AutoSize = false;
            _chkAutoTextContrast.ForeColor = ClrTextPrimary;
            _chkAutoTextContrast.SetBounds(16, 138, 275, 24);
            _chkAutoTextContrast.CheckedChanged += (s, e) => UpdatePreview();
            cardSettings.Controls.Add(_chkAutoTextContrast);

            _chkAutoBorder = new CheckBox();
            _chkAutoBorder.Text = "Гармоничная рамка (в тон заливке)";
            _chkAutoBorder.Checked = true;
            _chkAutoBorder.AutoSize = false;
            _chkAutoBorder.ForeColor = ClrTextPrimary;
            _chkAutoBorder.SetBounds(308, 138, 295, 24);
            _chkAutoBorder.CheckedChanged += (s, e) => UpdatePreview();
            cardSettings.Controls.Add(_chkAutoBorder);

            y += 190;

            // --- Card 4: Применение и системные действия ---
            Panel cardActions = CreateCardPanel(20, y, 620, 122);
            container.Controls.Add(cardActions);

            // Apply Button
            _btnApply = new Button();
            _btnApply.Text = "✔ Применить цвет выделения";
            _btnApply.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _btnApply.ForeColor = Color.White;
            _btnApply.BackColor = Color.FromArgb(22, 163, 74); // Vibrant Green
            _btnApply.FlatStyle = FlatStyle.Flat;
            _btnApply.FlatAppearance.BorderSize = 0;
            _btnApply.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 197, 94);
            _btnApply.Cursor = Cursors.Hand;
            _btnApply.SetBounds(16, 16, 280, 44);
            _btnApply.Click += BtnApply_Click;
            cardActions.Controls.Add(_btnApply);

            // Restart Explorer Button
            _btnRestartExplorer = new Button();
            _btnRestartExplorer.Text = "🔄 Перезапустить Проводник";
            _btnRestartExplorer.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _btnRestartExplorer.ForeColor = ClrTextPrimary;
            _btnRestartExplorer.BackColor = Color.FromArgb(51, 65, 85); // Slate
            _btnRestartExplorer.FlatStyle = FlatStyle.Flat;
            _btnRestartExplorer.FlatAppearance.BorderColor = ClrCardBorder;
            _btnRestartExplorer.FlatAppearance.MouseOverBackColor = Color.FromArgb(71, 85, 105);
            _btnRestartExplorer.Cursor = Cursors.Hand;
            _btnRestartExplorer.SetBounds(308, 16, 296, 44);
            _btnRestartExplorer.Click += BtnRestartExplorer_Click;
            cardActions.Controls.Add(_btnRestartExplorer);

            // Reset Default Button
            _btnResetDefault = new Button();
            _btnResetDefault.Text = "↩ Сбросить по умолчанию (Windows 10)";
            _btnResetDefault.Font = new Font("Segoe UI", 8.75f);
            _btnResetDefault.ForeColor = ClrTextMuted;
            _btnResetDefault.BackColor = Color.FromArgb(38, 38, 44);
            _btnResetDefault.FlatStyle = FlatStyle.Flat;
            _btnResetDefault.FlatAppearance.BorderColor = ClrCardBorder;
            _btnResetDefault.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 58);
            _btnResetDefault.Cursor = Cursors.Hand;
            _btnResetDefault.SetBounds(16, 68, 280, 36);
            _btnResetDefault.Click += BtnResetDefault_Click;
            cardActions.Controls.Add(_btnResetDefault);

            // Restore Backup Button
            _btnRestoreBackup = new Button();
            _btnRestoreBackup.Text = "📁 Из резервной копии первого запуска";
            _btnRestoreBackup.Font = new Font("Segoe UI", 8.75f);
            _btnRestoreBackup.ForeColor = ClrTextMuted;
            _btnRestoreBackup.BackColor = Color.FromArgb(38, 38, 44);
            _btnRestoreBackup.FlatStyle = FlatStyle.Flat;
            _btnRestoreBackup.FlatAppearance.BorderColor = ClrCardBorder;
            _btnRestoreBackup.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 58);
            _btnRestoreBackup.Cursor = Cursors.Hand;
            _btnRestoreBackup.SetBounds(308, 68, 296, 36);
            _btnRestoreBackup.Click += BtnRestoreBackup_Click;
            cardActions.Controls.Add(_btnRestoreBackup);

            y += 134;

            // --- Status Notification Bar ---
            _lblStatus = new Label();
            _lblStatus.Text = "Готово к работе. Нажмите «Экранная пипетка», чтобы взять цвет прямо с обоев.";
            _lblStatus.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            _lblStatus.ForeColor = Color.FromArgb(147, 197, 253);
            _lblStatus.AutoSize = false;
            _lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblStatus.SetBounds(20, y, 620, 32);
            _lblStatus.BackColor = Color.FromArgb(30, 41, 59);
            _lblStatus.BorderStyle = BorderStyle.FixedSingle;
            container.Controls.Add(_lblStatus);
        }

        private Panel CreateCardPanel(int x, int y, int w, int h)
        {
            Panel p = new Panel();
            p.SetBounds(x, y, w, h);
            p.BackColor = ClrCard;
            p.BorderStyle = BorderStyle.FixedSingle;
            return p;
        }

        private Label CreateCardHeader(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(148, 163, 184); // Slate 400
            lbl.AutoSize = true;
            lbl.Location = new Point(x, y);
            return lbl;
        }

        private void CreateColorSlider(Panel parent, string labelText, Color accentColor, int y,
            out TrackBar trackBar, out NumericUpDown numUpDn, EventHandler onChange)
        {
            Label lbl = new Label();
            lbl.Text = labelText;
            lbl.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lbl.ForeColor = accentColor;
            lbl.SetBounds(16, y + 2, 95, 22);
            parent.Controls.Add(lbl);

            trackBar = new TrackBar();
            trackBar.Minimum = 0;
            trackBar.Maximum = 255;
            trackBar.TickStyle = TickStyle.None; // Clean modern slider without distracting ticks
            trackBar.AutoSize = false;
            trackBar.SetBounds(112, y, 412, 24);
            trackBar.ValueChanged += onChange;
            parent.Controls.Add(trackBar);

            numUpDn = new NumericUpDown();
            numUpDn.Minimum = 0;
            numUpDn.Maximum = 255;
            numUpDn.BackColor = Color.FromArgb(24, 24, 28);
            numUpDn.ForeColor = ClrTextPrimary;
            numUpDn.BorderStyle = BorderStyle.FixedSingle;
            numUpDn.Font = new Font("Segoe UI", 9f);
            numUpDn.SetBounds(535, y, 68, 24);
            numUpDn.ValueChanged += onChange;
            parent.Controls.Add(numUpDn);
        }

        private void LoadInitialData()
        {
            // Ensure safe initial backup
            ColorRegistry.EnsureInitialBackup();
            _backupSettings = ColorRegistry.LoadBackup();

            // Load current system settings
            _currentSettings = ColorRegistry.LoadCurrentSettings();

            // Load wallpaper thumbnail and palette
            LoadWallpaperInfo();

            // Sync UI
            SetColorToUi(_currentSettings.Hilight);
        }

        private void LoadWallpaperInfo()
        {
            _wallpaperPath = WallpaperHelper.GetCurrentWallpaperPath();
            if (!string.IsNullOrEmpty(_wallpaperPath) && File.Exists(_wallpaperPath))
            {
                _wallpaperThumb = WallpaperHelper.GetWallpaperThumbnail(_wallpaperPath, 160, 104);
                _picWallpaperThumb.Image = _wallpaperThumb;

                _wallpaperPalette = WallpaperHelper.ExtractDominantColors(_wallpaperPath, 9);
            }
            else
            {
                _lblWallpaperStatus.Text = "Обои рабочего стола не найдены (базовая палитра):";
                _wallpaperPalette = WallpaperHelper.ExtractDominantColors(null, 9);
            }

            PopulatePaletteSwatches();
        }

        private void PopulatePaletteSwatches()
        {
            _palettePanel.Controls.Clear();
            foreach (Color c in _wallpaperPalette)
            {
                Panel swatch = new Panel();
                swatch.Size = new Size(34, 30);
                swatch.Margin = new Padding(2, 2, 7, 2);
                swatch.BackColor = c;
                swatch.Cursor = Cursors.Hand;
                swatch.BorderStyle = BorderStyle.FixedSingle;

                ToolTip tip = new ToolTip();
                tip.SetToolTip(swatch, string.Format("#{0:X2}{1:X2}{2:X2} (Кликните для выбора)", c.R, c.G, c.B));

                Color captured = c;
                swatch.Click += (s, e) =>
                {
                    SetColorToUi(captured);
                    ShowStatus(string.Format("Выбран цвет из обоев: #{0:X2}{1:X2}{2:X2}", captured.R, captured.G, captured.B), ClrSuccess);
                };

                _palettePanel.Controls.Add(swatch);
            }
        }

        private void SetColorToUi(Color c)
        {
            _isUpdatingUi = true;
            try
            {
                _tbRed.Value = c.R;
                _numRed.Value = c.R;

                _tbGreen.Value = c.G;
                _numGreen.Value = c.G;

                _tbBlue.Value = c.B;
                _numBlue.Value = c.B;

                _currentSettings.Hilight = c;
                if (_chkAutoBorder.Checked)
                {
                    _currentSettings.HotTrackingColor = ColorRegistry.GenerateBorderColor(c);
                }
                if (_chkAutoTextContrast.Checked)
                {
                    _currentSettings.HilightText = ColorRegistry.GetContrastingTextColor(c);
                }
                _currentSettings.MenuHilight = c;

                UpdatePreview();
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void OnSliderChanged()
        {
            if (_isUpdatingUi) return;

            // Sync TrackBar and NumericUpDown
            _isUpdatingUi = true;
            try
            {
                if (ActiveControl == _tbRed || ActiveControl == _tbGreen || ActiveControl == _tbBlue)
                {
                    _numRed.Value = _tbRed.Value;
                    _numGreen.Value = _tbGreen.Value;
                    _numBlue.Value = _tbBlue.Value;
                }
                else
                {
                    _tbRed.Value = (int)_numRed.Value;
                    _tbGreen.Value = (int)_numGreen.Value;
                    _tbBlue.Value = (int)_numBlue.Value;
                }

                Color c = Color.FromArgb((int)_numRed.Value, (int)_numGreen.Value, (int)_numBlue.Value);
                _currentSettings.Hilight = c;

                if (_chkAutoBorder.Checked)
                    _currentSettings.HotTrackingColor = ColorRegistry.GenerateBorderColor(c);

                if (_chkAutoTextContrast.Checked)
                    _currentSettings.HilightText = ColorRegistry.GetContrastingTextColor(c);

                _currentSettings.MenuHilight = c;

                UpdatePreview();
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void UpdatePreview()
        {
            Color c = _currentSettings.Hilight;
            if (!_txtHex.Focused)
            {
                _txtHex.Text = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
            }
            _lblRgbValue.Text = string.Format("RGB: {0}, {1}, {2}", c.R, c.G, c.B);
            _swatchCurrent.BackColor = c;

            _previewDesktopPanel.Invalidate();
            _previewTextPanel.Invalidate();
        }

        private void TxtHex_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi || !_txtHex.Focused) return;
            string text = _txtHex.Text.Trim();
            if (text.StartsWith("#")) text = text.Substring(1);
            if (text.Length == 6)
            {
                int r, g, b;
                if (int.TryParse(text.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out r) &&
                    int.TryParse(text.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out g) &&
                    int.TryParse(text.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out b))
                {
                    SetColorToUi(Color.FromArgb(r, g, b));
                }
            }
        }

        private void PreviewDesktopPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = _previewDesktopPanel.Width;
            int h = _previewDesktopPanel.Height;

            // 1. Draw wallpaper background simulation
            if (_wallpaperThumb != null)
            {
                g.DrawImage(_wallpaperThumb, new Rectangle(0, 0, w, h));
            }
            else
            {
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    new Point(0, 0), new Point(w, h), Color.FromArgb(30, 41, 59), Color.FromArgb(15, 23, 42)))
                {
                    g.FillRectangle(bgBrush, 0, 0, w, h);
                }
            }

            // 2. Draw fake desktop icons on the left side
            DrawFakeDesktopIcon(g, 14, 16, "Этот ПК");
            DrawFakeDesktopIcon(g, 14, 64, "Корзина");

            // 3. Draw Translucent Selection Marquee Box on the right side
            Rectangle selRect = new Rectangle(106, 14, 160, 88);
            Color fill = _currentSettings.Hilight;
            Color border = _chkAutoBorder.Checked ? ColorRegistry.GenerateBorderColor(fill) : _currentSettings.HotTrackingColor;

            // Translucent fill
            using (SolidBrush selFillBrush = new SolidBrush(Color.FromArgb(90, fill.R, fill.G, fill.B)))
            {
                g.FillRectangle(selFillBrush, selRect);
            }

            // Crisp border
            using (Pen selPen = new Pen(Color.FromArgb(230, border.R, border.G, border.B), 1.5f))
            {
                g.DrawRectangle(selPen, selRect);
            }

            // Selected file item inside the box
            DrawFakeSelectedFile(g, 142, 28, "Файл.txt", fill);

            // Bottom-right badge "Рабочий стол"
            Rectangle badgeRect = new Rectangle(w - 94, h - 22, 88, 18);
            using (GraphicsPath bp = GetRoundedRect(badgeRect, 4))
            using (SolidBrush bb = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            using (Font bf = new Font("Segoe UI", 7.25f, FontStyle.Regular))
            using (SolidBrush btb = new SolidBrush(Color.FromArgb(220, 220, 220)))
            {
                g.FillPath(bb, bp);
                g.DrawString("Рабочий стол", bf, btb, w - 90, h - 20);
            }
        }

        private void DrawFakeDesktopIcon(Graphics g, int x, int y, string name)
        {
            Rectangle iconRect = new Rectangle(x, y, 22, 22);
            using (GraphicsPath ipath = GetRoundedRect(iconRect, 4))
            using (SolidBrush iconBrush = new SolidBrush(Color.FromArgb(200, 70, 130, 180)))
            using (Pen ip = new Pen(Color.FromArgb(120, 255, 255, 255), 1f))
            {
                g.FillPath(iconBrush, ipath);
                g.DrawPath(ip, ipath);
            }
            using (Font f = new Font("Segoe UI", 7.5f))
            using (SolidBrush tb = new SolidBrush(Color.White))
            {
                g.DrawString(name, f, tb, x + 26, y + 4);
            }
        }

        private void DrawFakeSelectedFile(Graphics g, int x, int y, string name, Color accent)
        {
            Rectangle iconRect = new Rectangle(x, y, 22, 22);
            using (GraphicsPath ipath = GetRoundedRect(iconRect, 4))
            using (SolidBrush iconBrush = new SolidBrush(Color.FromArgb(220, 230, 230, 240)))
            using (Pen ip = new Pen(Color.White, 1f))
            {
                g.FillPath(iconBrush, ipath);
                g.DrawPath(ip, ipath);
            }
            using (Font f = new Font("Segoe UI", 7.5f))
            using (SolidBrush tb = new SolidBrush(Color.White))
            {
                g.DrawString(name, f, tb, x - 8, y + 26);
            }
        }

        private void PreviewTextPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = _previewTextPanel.Width;
            int h = _previewTextPanel.Height;

            // Draw clean white background
            g.Clear(Color.White);

            // Subtle header
            using (Font capFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (SolidBrush capBrush = new SolidBrush(Color.FromArgb(120, 120, 120)))
            {
                g.DrawString("Пример в Блокноте / текстовых полях:", capFont, capBrush, 12, 6);
            }

            using (Font font = new Font("Segoe UI", 10.5f, FontStyle.Regular))
            {
                string text1 = "Текст ";
                string textSel = " выделенный ";
                string text2 = " в окне";

                float startX = 12;
                float startY = 28;

                SizeF s1 = g.MeasureString(text1, font);
                SizeF sSel = g.MeasureString(textSel, font);

                // Before selection
                using (SolidBrush b = new SolidBrush(Color.FromArgb(20, 20, 20)))
                {
                    g.DrawString(text1, font, b, startX, startY);
                }

                // Selection background
                RectangleF selRect = new RectangleF(startX + s1.Width - 4, startY - 1, sSel.Width, sSel.Height + 2);
                using (SolidBrush selBg = new SolidBrush(_currentSettings.Hilight))
                {
                    g.FillRectangle(selBg, selRect);
                }

                // Selection text
                Color textColor = _chkAutoTextContrast.Checked
                    ? ColorRegistry.GetContrastingTextColor(_currentSettings.Hilight)
                    : _currentSettings.HilightText;

                using (SolidBrush selTextBrush = new SolidBrush(textColor))
                {
                    g.DrawString(textSel, font, selTextBrush, startX + s1.Width - 4, startY);
                }

                // After selection
                using (SolidBrush b = new SolidBrush(Color.FromArgb(20, 20, 20)))
                {
                    g.DrawString(text2, font, b, startX + s1.Width + sSel.Width - 8, startY);
                }
            }
        }

        private static GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void BtnEyedropper_Click(object sender, EventArgs e)
        {
            // Minimize or hide slightly so cursor can see underneath if needed
            WindowState = FormWindowState.Minimized;
            System.Threading.Thread.Sleep(200);

            using (EyedropperForm picker = new EyedropperForm())
            {
                picker.ColorSelected += (selectedColor) =>
                {
                    SetColorToUi(selectedColor);
                    ShowStatus(string.Format("Пипетка: выбран цвет #{0:X2}{1:X2}{2:X2} (RGB: {3}, {4}, {5})",
                        selectedColor.R, selectedColor.G, selectedColor.B,
                        selectedColor.R, selectedColor.G, selectedColor.B), ClrSuccess);
                };

                picker.ShowDialog();
            }

            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void BtnMoreColors_Click(object sender, EventArgs e)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                cd.Color = _currentSettings.Hilight;
                cd.FullOpen = true;
                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    SetColorToUi(cd.Color);
                }
            }
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            // Sync final values
            _currentSettings.Hilight = Color.FromArgb((int)_numRed.Value, (int)_numGreen.Value, (int)_numBlue.Value);
            _currentSettings.MenuHilight = _currentSettings.Hilight;

            if (_chkAutoBorder.Checked)
                _currentSettings.HotTrackingColor = ColorRegistry.GenerateBorderColor(_currentSettings.Hilight);

            if (_chkAutoTextContrast.Checked)
                _currentSettings.HilightText = ColorRegistry.GetContrastingTextColor(_currentSettings.Hilight);

            bool ok = ColorRegistry.ApplySettings(_currentSettings);
            if (ok)
            {
                ShowStatus("✔ Новый цвет успешно записан в систему! Для рамки на Рабочем столе нажмите «Перезапустить Проводник».", ClrSuccess);
                MessageBox.Show(this,
                    "Цвет выделения успешно применен в реестре и системных окнах!\n\n" +
                    "Обратите внимание: чтобы рамка выделения на самом Рабочем столе и в папках Проводника сразу приняла новый цвет, нажмите кнопку «Перезапустить Проводник» (занимает 1 секунду).",
                    "Цвет применен", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                ShowStatus("Ошибка при записи настроек в реестр Windows.", Color.FromArgb(239, 68, 68));
            }
        }

        private void BtnRestartExplorer_Click(object sender, EventArgs e)
        {
            ShowStatus("Перезапуск Проводника Windows...", Color.FromArgb(234, 179, 8));
            Refresh();

            bool ok = ColorRegistry.RestartExplorer();
            if (ok)
            {
                ShowStatus("✔ Проводник успешно перезапущен! Новый цвет активен на Рабочем столе.", ClrSuccess);
            }
            else
            {
                ShowStatus("Не удалось автоматически перезапустить explorer.exe.", Color.FromArgb(239, 68, 68));
            }
        }

        private void BtnResetDefault_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this,
                "Сбросить цвет выделения к оригинальному стандартному синему Windows 10 (#0078D7)?",
                "Сброс по умолчанию", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var def = new SelectionColorSettings();
                SetColorToUi(def.Hilight);
                ColorRegistry.ApplySettings(def);
                ShowStatus("Восстановлен стандартный цвет Windows 10. Нажмите «Перезапустить Проводник».", ClrSuccess);
            }
        }

        private void BtnRestoreBackup_Click(object sender, EventArgs e)
        {
            if (_backupSettings == null)
            {
                _backupSettings = ColorRegistry.LoadBackup();
            }

            if (_backupSettings != null)
            {
                SetColorToUi(_backupSettings.Hilight);
                ShowStatus("Восстановлены цвета из резервной копии первого запуска. Нажмите «Применить».", ClrSuccess);
            }
            else
            {
                ShowStatus("Файл резервной копии не найден.", Color.FromArgb(239, 68, 68));
            }
        }

        private void ShowStatus(string msg, Color clr)
        {
            _lblStatus.Text = msg;
            _lblStatus.ForeColor = clr;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_wallpaperThumb != null)
                {
                    _wallpaperThumb.Dispose();
                    _wallpaperThumb = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
