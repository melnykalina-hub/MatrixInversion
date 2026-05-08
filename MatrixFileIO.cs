using System;
using System.IO;
using System.Text;

namespace MatrixInversion
{
    public static class MatrixFileIO
    {
        /// <summary>
        /// Зчитує матрицю з файлу.
        /// Формат: перший рядок - цiле число n (розмiр), далi n рядкiв по n чисел.
        /// </summary>
        public static double[,] Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Файл не знайдено: «{path}».");

            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.UTF8); }
            catch (IOException ex) { throw new IOException($"Помилка читання файлу: {ex.Message}"); }
            catch (UnauthorizedAccessException) { throw new UnauthorizedAccessException($"Немає доступу до файлу: «{path}»."); }

            var cl = new System.Collections.Generic.List<string>();
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;
                cl.Add(trimmed);
            }

            if (cl.Count == 0)
                throw new FormatException("Файл порожнiй або мiстить лише коментарi.");

            if (!int.TryParse(cl[0], out int n))
                throw new FormatException($"Перший рядок файлу повинен мiстити розмiр матрицi (цiле число). Знайдено: «{cl[0]}».");

            if (n < 2 || n > MatrixMath.MAX_SIZE)
                throw new FormatException($"Розмiр матрицi {n} виходить за допустимi межi [2; {MatrixMath.MAX_SIZE}].");

            if (cl.Count < n + 1)
                throw new FormatException($"Файл пошкоджений: очiкується {n} рядкiв даних, знайдено {cl.Count - 1}.");

            if (cl.Count > n + 1)
                throw new FormatException($"Файл мiстить зайвi рядки: очiкується {n}, знайдено {cl.Count - 1}.");

            double[,] A;
            try { A = new double[n, n]; }
            catch (OutOfMemoryException) { throw new OutOfMemoryException($"Недостатньо пам'ятi для матрицi {n}x{n}."); }

            for (int i = 0; i < n; i++)
            {
                string[] parts = cl[i + 1].Split(new char[] { ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != n)
                    throw new FormatException(
                        $"Рядок {i + 1}: очiкується {n} елементiв, знайдено {parts.Length}.\n" +
                        $"Вмiст рядку: «{cl[i + 1]}»");

                for (int j = 0; j < n; j++)
                {
                    string raw = parts[j].Replace(',', '.');
                    if (!double.TryParse(raw,
                        System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowLeadingSign,
                        System.Globalization.CultureInfo.InvariantCulture, out double val))
                        throw new FormatException(
                            $"Рядок {i + 1}, елемент {j + 1}: не вдалося розпiзнати число «{parts[j]}».\n" +
                            $"Допустимi формати: 3, -2.5, 1.5e-3");

                    if (double.IsInfinity(val) || double.IsNaN(val))
                        throw new FormatException($"Рядок {i + 1}, елемент {j + 1}: значення «{parts[j]}» є нескiнченним або NaN.");

                    A[i, j] = val;
                }
            }

            return A;
        }

        /// <summary>
        /// Зберiгає матрицю у файл (лише числа, можна знову завантажити).
        /// </summary>
        public static void SaveMatrix(string path, double[,] A)
        {
            int n = A.GetLength(0);
            try
            {
                using StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8);
                sw.WriteLine(n);
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (j > 0) sw.Write('\t');
                        sw.Write(A[i, j].ToString("G10", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    sw.WriteLine();
                }
            }
            catch (IOException ex) { throw new IOException($"Помилка запису файлу: {ex.Message}"); }
            catch (UnauthorizedAccessException) { throw new UnauthorizedAccessException($"Немає прав для запису у файл: «{path}»."); }
        }

        /// <summary>
        /// Зберiгає повний звiт.
        /// </summary>
        public static void SaveReport(string path, double[,] A, double[,] inv, string methodName, long ops, double elapsedMs, double error)
        {
            int n = A.GetLength(0);
            try
            {
                using StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8);
                sw.WriteLine($"  Звiт: обернення матрицi ({methodName})");
                sw.WriteLine($"  Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                sw.WriteLine();
                sw.WriteLine($"Розмiр матрицi: {n}x{n}");
                sw.WriteLine();
                sw.WriteLine("Вхiдна матриця A:");
                WriteMatrix(sw, A);
                sw.WriteLine();
                sw.WriteLine($"Обернена матриця A^(-1) [{methodName}]:");
                WriteMatrix(sw, inv);
                sw.WriteLine();
                sw.WriteLine("Перевiрка A * A^(-1) ~ E:");
                sw.WriteLine($"  Максимальна абсолютна похибка: {error:G6}");
                sw.WriteLine();
                sw.WriteLine("Практична складнiсть:");
                sw.WriteLine($"  Кiлькiсть арифметичних операцiй: {ops:N0}");
                sw.WriteLine($"  Час виконання: {elapsedMs:F3} мс");
            }
            catch (IOException ex) { throw new IOException($"Помилка запису звiту: {ex.Message}"); }
        }

        private static void WriteMatrix(StreamWriter sw, double[,] M)
        {
            int n = M.GetLength(0);
            int m = M.GetLength(1);
            string fmt = n <= 10 ? "G8" : "G6";
            for (int i = 0; i < n; i++)
            {
                sw.Write("  ");
                for (int j = 0; j < m; j++)
                {
                    if (j > 0) sw.Write("\t");
                    sw.Write(M[i, j].ToString(fmt, System.Globalization.CultureInfo.InvariantCulture).PadLeft(14));
                }
                sw.WriteLine();
            }
        }
    }
}