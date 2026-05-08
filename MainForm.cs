using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MatrixInversion
{
    public partial class MainForm : Form
    {
        private double[,]? _matrix = null;
        private double[,]? _invMatrix = null;
        private string _lastMethod = "";
        private long _lastOps;
        private double _lastMs;
        private double _lastError;

#pragma warning disable CS8618
        private NumericUpDown nudSize;
        private Button btnRandom, btnLoadFile, btnConfirm, btnHelp;
        private Button btnBordering, btnLUP, btnCompare, btnSaveReport, btnReset;
        private DataGridView dgvInput, dgvResult, dgvCompare;
        private Label lblInputTitle, lblResultTitle, lblCompareInfo;
        private Label lblDet, lblOps, lblTime, lblError, lblStatus;
        private TabControl tabMain;
#pragma warning restore CS8618

        public MainForm()
        {
            InitializeComponent();
            BuildUI();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "Обернення матрицi — Мельник А.Р.";
            this.Size = new Size(1100, 720);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9f);
            this.ResumeLayout(false);
        }

        private void BuildUI()
        {
            // ── Лiва панель ──
            var panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                Padding = new Padding(8)
            };

            int y = 8;

            // Розмiр
            AddLabel(panelLeft, "Розмiр матрицi n (2–150):", ref y);
            nudSize = new NumericUpDown
            {
                Minimum = 2,
                Maximum = MatrixMath.MAX_SIZE,
                Value = 3,
                Location = new Point(8, y),
                Width = 260,
                BorderStyle = BorderStyle.FixedSingle
            };
            panelLeft.Controls.Add(nudSize);
            nudSize.ValueChanged += (s, e) => OnSizeChanged();
            y += 28;

            // Роздiлювач
            y += 6;
            AddSep(panelLeft, ref y);

            // Введення
            AddLabel(panelLeft, "Введення матрицi:", ref y);
            btnRandom = AddBtn(panelLeft, "Генерувати випадково", ref y);
            btnLoadFile = AddBtn(panelLeft, "Завантажити з файлу (.txt)", ref y);
            btnConfirm = AddBtn(panelLeft, "Пiдтвердити введення", ref y);
            btnConfirm.Enabled = false;
            btnConfirm.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnConfirm.Click += (s, e) => { if (TryCommitManualInput()) { SetStatus("Матрицю введено", false); btnConfirm.Enabled = false; } };

            y += 4;
            AddSep(panelLeft, ref y);

            // Обчислення
            AddLabel(panelLeft, "Обчислення:", ref y);
            btnBordering = AddBtn(panelLeft, "Метод окаймлення", ref y);
            btnLUP = AddBtn(panelLeft, "LUP-розклад", ref y);
            btnCompare = AddBtn(panelLeft, "Порiвняти обидва", ref y);

            y += 4;
            AddSep(panelLeft, ref y);

            // Збереження / скидання
            btnSaveReport = AddBtn(panelLeft, "Зберегти звiт у .txt", ref y);
            btnReset = AddBtn(panelLeft, "Скинути", ref y);
            y += 4;
            btnHelp = AddBtn(panelLeft, "Iнструкцiя", ref y);

            y += 4;
            AddSep(panelLeft, ref y);

            // Статистика
            AddLabel(panelLeft, "Результати:", ref y);
            lblDet = AddInfoLabel(panelLeft, "det: —", ref y);
            lblOps = AddInfoLabel(panelLeft, "Операцiй: —", ref y);
            lblTime = AddInfoLabel(panelLeft, "Час: —", ref y);
            lblError = AddInfoLabel(panelLeft, "Похибка: —", ref y);

            y += 4;
            lblStatus = new Label
            {
                Text = "Готовий до роботи",
                Location = new Point(8, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            panelLeft.Controls.Add(lblStatus);

            // ── Роздiлювач лiвої панелi ──
            var splitter = new Splitter { Dock = DockStyle.Left, Width = 4 };

            // ── Вкладки ──
            tabMain = new TabControl { Dock = DockStyle.Fill };

            var tabInput = new TabPage("Введення");
            var tabResult = new TabPage("Результат");
            var tabCompare = new TabPage("Порiвняння");
            tabMain.TabPages.AddRange(new[] { tabInput, tabResult, tabCompare });

            // Вкладка «Введення»
            lblInputTitle = new Label
            {
                Text = "Матриця A  [не задана]",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.Gray,
                Padding = new Padding(2)
            };
            tabInput.Controls.Add(lblInputTitle);
            dgvInput = MakeGrid(editable: true);
            dgvInput.Dock = DockStyle.Fill;
            tabInput.Controls.Add(dgvInput);

            // Вкладка «Результат»
            lblResultTitle = new Label
            {
                Text = "Обернена матриця A^(-1)",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.Gray,
                Padding = new Padding(2)
            };
            tabResult.Controls.Add(lblResultTitle);
            dgvResult = MakeGrid(editable: false);
            dgvResult.Dock = DockStyle.Fill;
            tabResult.Controls.Add(dgvResult);

            // Вкладка «Порiвняння»
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 110 };
            lblCompareInfo = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Натиснiть «Порiвняти обидва» пiсля введення матрицi.",
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(4)
            };
            pnlBottom.Controls.Add(lblCompareInfo);
            tabCompare.Controls.Add(pnlBottom);

            dgvCompare = MakeGrid(editable: false);
            dgvCompare.Dock = DockStyle.Fill;
            dgvCompare.RowHeadersVisible = false;
            dgvCompare.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCompare.RowTemplate.Height = 28;
            dgvCompare.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Параметр", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 40, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvCompare.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Метод окаймлення", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvCompare.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "LUP-розклад", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvCompare.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            tabCompare.Controls.Add(dgvCompare);

            // Статус-рядок
            var statusBar = new StatusStrip();
            var statusLbl = new ToolStripStatusLabel("Статус: готовий до роботи")
            { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            statusBar.Items.Add(statusLbl);
            lblStatus.TextChanged += (s, e) => statusLbl.Text = "Статус: " + lblStatus.Text;

            // Компонування
            this.Controls.Add(tabMain);
            this.Controls.Add(splitter);
            this.Controls.Add(panelLeft);
            this.Controls.Add(statusBar);

            // Обробники
            btnRandom.Click += BtnRandom_Click;
            btnLoadFile.Click += BtnLoadFile_Click;
            btnBordering.Click += (s, e) => RunMethod("bordering");
            btnLUP.Click += (s, e) => RunMethod("lup");
            btnCompare.Click += BtnCompare_Click;
            btnSaveReport.Click += BtnSaveReport_Click;
            btnReset.Click += BtnReset_Click;
            btnHelp.Click += BtnHelp_Click;

            SetComputeEnabled(false);
            RefreshInputGrid();
        }

        // ═══════════════════════════════════════════════════════
        // Обробники
        // ═══════════════════════════════════════════════════════

        private void BtnRandom_Click(object sender, EventArgs e)
        {
            try
            {
                _matrix = MatrixMath.GenerateRandom((int)nudSize.Value);
                FillInputGrid(_matrix);
                UpdateDet();
                SetComputeEnabled(true);
                btnConfirm.Enabled = false;
                SetStatus("Матрицю згенеровано", false);
                ClearResult();
            }
            catch (OutOfMemoryException ex) { ShowError("Нестача пам'ятi", ex.Message); }
            catch (Exception ex) { ShowError("Помилка", ex.Message); }
        }

        private void BtnLoadFile_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Вибiр файлу",
                Filter = "Текстовi файли (*.txt)|*.txt|Усi файли (*.*)|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                _matrix = MatrixFileIO.Load(dlg.FileName);
                nudSize.Value = _matrix.GetLength(0);
                FillInputGrid(_matrix);
                UpdateDet();
                SetComputeEnabled(true);
                btnConfirm.Enabled = false;
                SetStatus("Файл завантажено", false);
                ClearResult();
                tabMain.SelectedIndex = 0;
            }
            catch (System.IO.FileNotFoundException ex) { ShowError("Файл не знайдено", ex.Message); }
            catch (UnauthorizedAccessException ex) { ShowError("Немає доступу", ex.Message); }
            catch (System.IO.IOException ex) { ShowError("Помилка читання", ex.Message); }
            catch (FormatException ex) { ShowError("Некоректний формат", ex.Message); }
            catch (OutOfMemoryException ex) { ShowError("Нестача пам'ятi", ex.Message); }
            catch (Exception ex) { ShowError("Помилка", ex.Message); }
        }

        private void RunMethod(string method)
        {
            if (!TryCommitManualInput()) return;
            if (_matrix == null) { ShowError("Матриця не задана", "Спочатку введiть матрицю."); return; }
            try
            {
                var sw = Stopwatch.StartNew();
                double[,] inv;
                long ops;
                if (method == "bordering")
                {
                    inv = MatrixMath.BorderingMethod(_matrix, out ops);
                    _lastMethod = "Метод окаймлення";
                }
                else
                {
                    inv = MatrixMath.LupMethod(_matrix, out ops);
                    _lastMethod = "LUP-розклад";
                }
                sw.Stop();

                _invMatrix = inv;
                _lastOps = ops;
                _lastMs = sw.Elapsed.TotalMilliseconds;
                _lastError = MatrixMath.CheckError(_matrix, inv);

                FillResultGrid(inv);
                int n = _matrix.GetLength(0);
                lblResultTitle.Text = $"A^(-1)  [{_lastMethod}, n={n}]";
                lblResultTitle.ForeColor = Color.DarkGreen;
                lblOps.Text = $"Операцiй: {ops:N0}";
                lblTime.Text = $"Час: {_lastMs:F3} мс";
                lblError.Text = $"Похибка: {_lastError:G4}";
                SetStatus("Готово", false);
                btnSaveReport.Enabled = true;
                tabMain.SelectedIndex = 1;
            }
            catch (InvalidOperationException ex) { ShowError("Матриця вироджена", ex.Message); }
            catch (DivideByZeroException ex) { ShowError("Дiлення на нуль", ex.Message); }
            catch (OverflowException ex) { ShowError("Переповнення", ex.Message); }
            catch (OutOfMemoryException ex) { ShowError("Нестача пам'ятi", ex.Message); }
            catch (Exception ex) { ShowError("Помилка", ex.Message); }
        }

        private void BtnCompare_Click(object sender, EventArgs e)
        {
            if (!TryCommitManualInput()) return;
            if (_matrix == null) { ShowError("Матриця не задана", "Спочатку введiть матрицю."); return; }

            int n = _matrix.GetLength(0);

            // Попередження для великих матриць
            if (n > 50)
            {
                var res = MessageBox.Show(
                    $"Матриця {n}x{n} — обчислення може зайняти кiлька секунд.\nПродовжити?",
                    "Увага", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;
            }

            // Перевiрка виродженостi до запуску обох методiв
            double det = MatrixMath.Determinant(_matrix);
            if (Math.Abs(det) < MatrixMath.EPSILON)
            {
                ShowError("Матриця вироджена",
                    $"det = {det:G6}\nОбернення неможливе — матриця є виродженою.");
                return;
            }

            // Блокуємо кнопки на час обчислення
            btnCompare.Enabled = false;
            btnCompare.Text = "Обчислення...";
            lblCompareInfo.Text = "Виконується порiвняння...";
            tabMain.SelectedIndex = 2;
            Application.DoEvents(); // оновити UI перед важким обчисленням

            try
            {
                var sw1 = Stopwatch.StartNew();
                var inv1 = MatrixMath.BorderingMethod(_matrix, out long ops1);
                sw1.Stop();
                var sw2 = Stopwatch.StartNew();
                var inv2 = MatrixMath.LupMethod(_matrix, out long ops2);
                sw2.Stop();

                double err1 = MatrixMath.CheckError(_matrix, inv1);
                double err2 = MatrixMath.CheckError(_matrix, inv2);

                dgvCompare.Rows.Clear();
                dgvCompare.Rows.Add("Кiлькiсть операцiй",
                    ops1.ToString("N0"), ops2.ToString("N0"));
                dgvCompare.Rows.Add("Час виконання (мс)",
                    sw1.Elapsed.TotalMilliseconds.ToString("F4"),
                    sw2.Elapsed.TotalMilliseconds.ToString("F4"));
                dgvCompare.Rows.Add("Похибка ||A*A^-1 - E||",
                    err1.ToString("G6"), err2.ToString("G6"));
                dgvCompare.Rows.Add("Теоретична складнiсть",
                    "O(n^3)", "O(n^3)");

                lblCompareInfo.Text =
                    $"Матриця: {n}x{n}   det = {det:G6}\n" +
                    $"Переможець за операцiями : {(ops1 <= ops2 ? "Метод окаймлення" : "LUP-розклад")}\n" +
                    $"Переможець за часом      : {(sw1.Elapsed <= sw2.Elapsed ? "Метод окаймлення" : "LUP-розклад")}\n" +
                    $"Краща точнiсть           : {(err1 <= err2 ? "Метод окаймлення" : "LUP-розклад")}";

                SetStatus("Порiвняння виконано", false);
            }
            catch (InvalidOperationException ex) { ShowError("Матриця вироджена", ex.Message); }
            catch (DivideByZeroException ex) { ShowError("Дiлення на нуль", ex.Message); }
            catch (OverflowException ex) { ShowError("Переповнення", ex.Message); }
            catch (OutOfMemoryException ex) { ShowError("Нестача пам'ятi", ex.Message); }
            catch (Exception ex) { ShowError("Помилка", ex.Message); }
            finally
            {
                btnCompare.Enabled = true;
                btnCompare.Text = "Порiвняти обидва";
            }
        }

        private void BtnSaveReport_Click(object sender, EventArgs e)
        {
            if (_invMatrix == null || _matrix == null)
            { ShowError("Немає результату", "Спочатку виконайте обчислення."); return; }
            using var dlg = new SaveFileDialog
            {
                Title = "Зберегти звiт",
                Filter = "Текстовi файли (*.txt)|*.txt",
                FileName = $"zvit_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                MatrixFileIO.SaveReport(dlg.FileName, _matrix, _invMatrix,
                    _lastMethod, _lastOps, _lastMs, _lastError);
                MessageBox.Show("Файл збережено!", "Готово",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError("Помилка збереження", ex.Message); }
        }

        private void BtnHelp_Click(object sender, EventArgs e)
        {
            string text =
                "IНСТРУКЦIЯ З ВИКОРИСТАННЯ\n\n" +
                "1. ВВЕДЕННЯ МАТРИЦI\n" +
                "   • Вкажiть розмiр матрицi n (вiд 2 до 150)\n" +
                "   • Оберiть спосiб введення:\n" +
                "     - Генерувати випадково — автоматичне заповнення\n" +
                "     - Завантажити з файлу — формат .txt:\n" +
                "       перший рядок: розмiр n\n" +
                "       далi n рядкiв по n чисел через пробiл\n" +
                "     - Ввести вручну — заповнiть таблицю\n" +
                "       та натиснiть «Пiдтвердити введення»\n\n" +
                "2. ОБЧИСЛЕННЯ\n" +
                "   • Метод окаймлення — послiдовне нарощування\n" +
                "     оберненої пiдматрицi вiд 1x1 до nxn\n" +
                "   • LUP-розклад — розклад A = L·U·P,\n" +
                "     розв'язання n систем лiнiйних рiвнянь\n" +
                "   • Порiвняти обидва — запускає обидва методи\n" +
                "     та порiвнює час, операцiї та точнiсть\n\n" +
                "3. РЕЗУЛЬТАТ\n" +
                "   • Вкладка «Результат» — обернена матриця A\u207B\xb9\n" +
                "   • n \u2264 10: розгорнутий вигляд (6 знакiв пiсля коми)\n" +
                "   • n > 10: наукова нотацiя (формат E4)\n" +
                "   • Похибка — максимальне вiдхилення A·A\u207B\xb9 вiд E\n\n" +
                "4. ЗБЕРЕЖЕННЯ\n" +
                "   • Зберегти звiт — зберiгає у .txt:\n" +
                "     вхiдну матрицю, обернену, похибку,\n" +
                "     кiлькiсть операцiй та час виконання\n\n" +
                "5. УМОВА ОБЕРНЕННЯ\n" +
                "   • Матриця повинна бути невиродженою (det \u2260 0)\n" +
                "   • Якщо det = 0 — програма виведе повiдомлення";

            MessageBox.Show(text, "Iнструкцiя", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            _matrix = _invMatrix = null;
            _lastMethod = "";
            RefreshInputGrid();
            dgvResult.Rows.Clear(); dgvResult.Columns.Clear();
            dgvCompare.Rows.Clear();
            lblCompareInfo.Text = "Натиснiть «Порiвняти обидва» пiсля введення матрицi.";
            lblDet.Text = "det: —";
            lblOps.Text = "Операцiй: —";
            lblTime.Text = "Час: —";
            lblError.Text = "Похибка: —";
            lblResultTitle.Text = "Обернена матриця A^(-1)";
            lblResultTitle.ForeColor = Color.Gray;
            SetComputeEnabled(false);
            SetStatus("Готовий до роботи", false);
            tabMain.SelectedIndex = 0;
        }

        private void OnSizeChanged()
        {
            _matrix = null;
            SetComputeEnabled(false);
            RefreshInputGrid();
        }

        // ═══════════════════════════════════════════════════════
        // Грид
        // ═══════════════════════════════════════════════════════

        private void RefreshInputGrid()
        {
            int n = (int)nudSize.Value;
            SetupGrid(dgvInput, n, editable: true);
            lblInputTitle.Text = $"Матриця A  [{n}x{n}]";
            lblInputTitle.ForeColor = Color.Gray;
            if (btnConfirm != null) btnConfirm.Enabled = true;
        }

        private void SetupGrid(DataGridView dgv, int n, bool editable)
        {
            dgv.SuspendLayout();
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            int colW = Math.Max(60, (dgv.Width - dgv.RowHeadersWidth - 20) / Math.Min(n, 20));
            for (int j = 0; j < n; j++)
            {
                dgv.Columns.Add($"c{j}", $"{j + 1}");
                dgv.Columns[j].Width = colW;
                dgv.Columns[j].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            for (int i = 0; i < n; i++)
            {
                dgv.Rows.Add();
                dgv.Rows[i].Height = 24;
                dgv.Rows[i].HeaderCell.Value = $"{i + 1}";
            }
            dgv.ReadOnly = !editable;
            dgv.ResumeLayout();
        }

        private void FillInputGrid(double[,] A)
        {
            int n = A.GetLength(0);
            SetupGrid(dgvInput, n, editable: false);
            string fmt = n > MatrixMath.DISPLAY_FULL_THRESHOLD ? "E4" : "F6";
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    dgvInput.Rows[i].Cells[j].Value =
                        A[i, j].ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
            lblInputTitle.Text = $"Матриця A  [{n}x{n}]";
            lblInputTitle.ForeColor = Color.Black;
        }

        private void FillResultGrid(double[,] A)
        {
            int n = A.GetLength(0);
            SetupGrid(dgvResult, n, editable: false);
            string fmt = n > MatrixMath.DISPLAY_FULL_THRESHOLD ? "E4" : "F6";
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    dgvResult.Rows[i].Cells[j].Value =
                        A[i, j].ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
        }

        private void ClearResult()
        {
            _invMatrix = null;
            dgvResult.Rows.Clear(); dgvResult.Columns.Clear();
            lblResultTitle.Text = "Обернена матриця A^(-1)";
            lblResultTitle.ForeColor = Color.Gray;
        }

        private bool TryCommitManualInput()
        {
            if (dgvInput.ReadOnly) return true;
            int n = dgvInput.RowCount;
            if (n == 0 || dgvInput.ColumnCount == 0)
            { ShowError("Матриця порожня", "Задайте розмiр та заповнiть таблицю."); return false; }

            double[,] mat;
            try { mat = new double[n, n]; }
            catch (OutOfMemoryException)
            { ShowError("Нестача пам'ятi", $"Недостатньо пам'ятi для матрицi {n}x{n}."); return false; }

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    string raw = dgvInput.Rows[i].Cells[j].Value?.ToString() ?? "";
                    raw = raw.Trim().Replace(',', '.');
                    if (raw.Length == 0)
                    {
                        ShowError("Незаповнена комiрка",
                            $"Комiрка [{i + 1},{j + 1}] порожня.\n" +
                            $"Потрiбно заповнити всi {n * n} елементiв.");
                        dgvInput.CurrentCell = dgvInput.Rows[i].Cells[j];
                        return false;
                    }
                    if (!double.TryParse(raw,
                        System.Globalization.NumberStyles.Float |
                        System.Globalization.NumberStyles.AllowLeadingSign,
                        System.Globalization.CultureInfo.InvariantCulture, out double val))
                    {
                        ShowError("Некоректне значення",
                            $"Комiрка [{i + 1},{j + 1}]: «{raw}» не є числом.\n" +
                            "Приклади: 3, -2.5, 1.5e-3");
                        dgvInput.CurrentCell = dgvInput.Rows[i].Cells[j];
                        return false;
                    }
                    if (double.IsInfinity(val) || double.IsNaN(val))
                    {
                        ShowError("Неприпустиме значення",
                            $"Комiрка [{i + 1},{j + 1}]: значення за межами дiапазону.");
                        return false;
                    }
                    mat[i, j] = val;
                }

            _matrix = mat;
            dgvInput.ReadOnly = true;
            UpdateDet();
            SetComputeEnabled(true);
            lblInputTitle.Text = $"Матриця A  [{n}x{n}]  — введено";
            lblInputTitle.ForeColor = Color.Black;
            return true;
        }

        // ═══════════════════════════════════════════════════════
        // Допомiжнi
        // ═══════════════════════════════════════════════════════

        private void UpdateDet()
        {
            if (_matrix == null) { lblDet.Text = "det: —"; return; }
            try
            {
                double det = MatrixMath.Determinant(_matrix);
                lblDet.Text = double.IsInfinity(det) ? "det: дуже велике" : $"det = {det:G6}";
                lblDet.ForeColor = Math.Abs(det) < 1e-12 ? Color.Red : Color.Black;
            }
            catch { lblDet.Text = "det: помилка"; }
        }

        private void SetComputeEnabled(bool en)
        {
            btnBordering.Enabled = en;
            btnLUP.Enabled = en;
            btnCompare.Enabled = en;
            btnSaveReport.Enabled = en && _invMatrix != null;
        }

        private void SetStatus(string text, bool isError)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = isError ? Color.Red : Color.DarkGreen;
        }

        private void ShowError(string title, string msg)
        {
            SetStatus(title, true);
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ═══════════════════════════════════════════════════════
        // Фабричнi методи
        // ═══════════════════════════════════════════════════════

        private DataGridView MakeGrid(bool editable)
        {
            var dgv = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = !editable,
                RowHeadersWidth = 36,
                ColumnHeadersHeight = 24,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ScrollBars = ScrollBars.Both,
                BorderStyle = BorderStyle.Fixed3D
            };
            dgv.DefaultCellStyle.Font = new Font("Consolas", 9f);
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            return dgv;
        }

        private void AddLabel(Panel p, string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(8, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f)
            };
            p.Controls.Add(lbl);
            y += 18;
        }

        private Label AddInfoLabel(Panel p, string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(8, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f)
            };
            p.Controls.Add(lbl);
            y += 18;
            return lbl;
        }

        private Button AddBtn(Panel p, string text, ref int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(8, y),
                Size = new Size(262, 26),
                Font = new Font("Segoe UI", 8.5f)
            };
            p.Controls.Add(btn);
            y += 30;
            return btn;
        }

        private void AddSep(Panel p, ref int y)
        {
            var sep = new Panel
            {
                Location = new Point(8, y),
                Size = new Size(262, 1),
                BackColor = Color.Silver
            };
            p.Controls.Add(sep);
            y += 8;
        }
    }
}