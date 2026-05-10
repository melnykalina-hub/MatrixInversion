using System;

namespace MatrixInversion
{
    /// <summary>
    /// Реалiзацiя алгоритмiв обернення матрицi
    /// </summary>
    public static class MatrixMath
    {
        public const int MAX_SIZE = 150;
        public const int DISPLAY_FULL_THRESHOLD = 10;
        public const double EPSILON = 1e-12;

        public static double[,] LupMethod(double[,] A, out long operationCount)
        {
            operationCount = 0;
            int n = A.GetLength(0);

            double[,] U, L;
            int[] P;
            double[,] inv;
            try
            {
                U = (double[,])A.Clone();
                L = new double[n, n];
                P = new int[n];
                inv = new double[n, n];
            }
            catch (OutOfMemoryException)
            {
                throw new OutOfMemoryException(
                    $"Недостатньо пам'ятi для LUP-розкладу матрицi {n}×{n}.");
            }

            for (int i = 0; i < n; i++)
            {
                L[i, i] = 1.0;
                P[i] = i;
            }

            for (int k = 0; k < n; k++)
            {
                int pivotRow = k;
                double maxVal = Math.Abs(U[k, k]);
                for (int i = k + 1; i < n; i++)
                {
                    double val = Math.Abs(U[i, k]);
                    if (val > maxVal) { maxVal = val; pivotRow = i; }
                    operationCount++;
                }

                if (maxVal < EPSILON)
                    throw new InvalidOperationException(
                        "Матриця є виродженою (визначник дорiвнює нулю). Обернення неможливе.");

                if (pivotRow != k)
                {
                    int tmp = P[k]; P[k] = P[pivotRow]; P[pivotRow] = tmp;
                    for (int j = 0; j < n; j++)
                    {
                        double t = U[k, j]; U[k, j] = U[pivotRow, j]; U[pivotRow, j] = t;
                        operationCount++;
                    }
                    for (int j = 0; j < k; j++)
                    {
                        double t = L[k, j]; L[k, j] = L[pivotRow, j]; L[pivotRow, j] = t;
                        operationCount++;
                    }
                }

                for (int i = k + 1; i < n; i++)
                {
                    if (Math.Abs(U[k, k]) < 1e-300)
                        throw new DivideByZeroException(
                            $"Дiлення на нуль в LUP-розкладi (стовпець {k + 1}). " +
                            "Перевiрте матрицю або скористайтеся методом окаймлення.");
                    L[i, k] = U[i, k] / U[k, k];
                    operationCount++;

                    for (int j = k; j < n; j++)
                    {
                        U[i, j] -= L[i, k] * U[k, j];
                        operationCount += 2;
                    }

                    for (int j = k; j < n; j++)
                    {
                        if (double.IsInfinity(U[i, j]) || double.IsNaN(U[i, j]))
                            throw new OverflowException(
                                $"Переповнення в LUP-розкладi: елемент U[{i + 1},{j + 1}] " +
                                "виходить за межi допустимого дiапазону.");
                    }
                }
            }

            double[] b = new double[n];
            double[] y = new double[n];
            double[] x = new double[n];

            for (int col = 0; col < n; col++)
            {
                for (int i = 0; i < n; i++)
                {
                    b[i] = (P[i] == col) ? 1.0 : 0.0;
                    operationCount++;
                }

                for (int i = 0; i < n; i++)
                {
                    y[i] = b[i];
                    for (int m = 0; m < i; m++)
                    {
                        y[i] -= L[i, m] * y[m];
                        operationCount += 2;
                    }
                }

                for (int i = n - 1; i >= 0; i--)
                {
                    if (Math.Abs(U[i, i]) < EPSILON)
                        throw new DivideByZeroException(
                            $"Дiлення на нуль при зворотнiй пiдстановцi: U[{i + 1},{i + 1}] ≈ 0.");
                    x[i] = y[i];
                    for (int m = i + 1; m < n; m++)
                    {
                        x[i] -= U[i, m] * x[m];
                        operationCount += 2;
                    }
                    x[i] /= U[i, i];
                    operationCount++;

                    if (double.IsInfinity(x[i]) || double.IsNaN(x[i]))
                        throw new OverflowException(
                            $"Переповнення при зворотнiй пiдстановцi: x[{i + 1}] " +
                            $"(стовпець {col + 1} матрицi A⁻¹).");
                }

                for (int row = 0; row < n; row++)
                    inv[row, col] = x[row];
            }

            return inv;
        }

        public static double[,] BorderingMethod(double[,] A, out long operationCount)
        {
            operationCount = 0;
            int n = A.GetLength(0);

            double det = Determinant(A);
            if (Math.Abs(det) < EPSILON)
                throw new InvalidOperationException(
                    "Матриця є виродженою (det = 0). Обернення неможливе.");

            if (Math.Abs(A[0, 0]) < EPSILON)
                throw new InvalidOperationException(
                    "[BORDERING_UNSTABLE] Елемент A[1,1] = " + A[0, 0].ToString("G4") +
                    " занадто малий для методу окаймлення.\n" +
                    "Матриця не є виродженою — скористайтесь LUP-розкладом.");

            double[,] Binv;
            try { Binv = new double[n, n]; }
            catch (OutOfMemoryException)
            {
                throw new OutOfMemoryException(
                    $"Недостатньо пам'ятi для методу окаймлення (матриця {n}×{n}).");
            }

            Binv[0, 0] = 1.0 / A[0, 0];
            operationCount++;

            for (int k = 1; k < n; k++)
            {
                int km1 = k;

                double[] v = new double[km1];
                double[] u = new double[km1];

                double alpha = A[k, k];
                operationCount++;
                for (int i = 0; i < km1; i++)
                {
                    v[i] = A[i, k];
                    u[i] = A[k, i];
                    operationCount += 2;
                }

                double[] w = new double[km1];
                for (int i = 0; i < km1; i++)
                    for (int j = 0; j < km1; j++)
                    {
                        w[i] += Binv[i, j] * v[j];
                        operationCount += 2;
                    }

                double[] q = new double[km1];
                for (int i = 0; i < km1; i++)
                    for (int j = 0; j < km1; j++)
                    {
                        q[j] += u[i] * Binv[i, j];
                        operationCount += 2;
                    }

                double utw = 0.0;
                for (int i = 0; i < km1; i++) { utw += u[i] * w[i]; operationCount += 2; }

                double delta = alpha - utw;
                operationCount++;
                if (double.IsNaN(delta) || double.IsInfinity(delta))
                    throw new OverflowException(
                        $"Переповнення при обчисленнi δ = α − uᵀ·w на кроцi {k + 1}.");
                if (Math.Abs(delta) < EPSILON)
                    throw new InvalidOperationException(
                        $"[BORDERING_UNSTABLE] Метод окаймлення нестiйкий на кроцi {k + 1}.\n" +
                        "Це може бути спричинено малими елементами, а не виродженiстю.\n" +
                        "Скористайтесь LUP-розкладом.");
                double c = 1.0 / delta;
                operationCount++;

                for (int i = 0; i < km1; i++)
                    for (int j = 0; j < km1; j++)
                    {
                        Binv[i, j] += c * w[i] * q[j];
                        operationCount += 3;
                        if (double.IsInfinity(Binv[i, j]) || double.IsNaN(Binv[i, j]))
                            throw new OverflowException(
                                $"Переповнення: B⁻¹[{i + 1},{j + 1}] на кроцi {k + 1}.");
                    }

                for (int i = 0; i < km1; i++)
                {
                    Binv[i, k] = -c * w[i];
                    operationCount += 2;
                }

                for (int j = 0; j < km1; j++)
                {
                    Binv[k, j] = -c * q[j];
                    operationCount += 2;
                }

                Binv[k, k] = c;
                operationCount++;
            }

            return Binv;
        }

        public static double Determinant(double[,] A)
        {
            int n = A.GetLength(0);
            double[,] M;
            try { M = (double[,])A.Clone(); }
            catch (OutOfMemoryException)
            {
                throw new OutOfMemoryException(
                    $"Недостатньо пам'ятi для обчислення визначника матрицi {n}×{n}.");
            }

            double det = 1.0;
            int sign = 1;
            for (int col = 0; col < n; col++)
            {
                int pivotRow = -1;
                double maxVal = EPSILON;
                for (int row = col; row < n; row++)
                    if (Math.Abs(M[row, col]) > maxVal)
                    { maxVal = Math.Abs(M[row, col]); pivotRow = row; }
                if (pivotRow == -1) return 0.0;
                if (pivotRow != col)
                {
                    for (int j = 0; j < n; j++)
                    { double t = M[col, j]; M[col, j] = M[pivotRow, j]; M[pivotRow, j] = t; }
                    sign = -sign;
                }
                det *= M[col, col];
                if (double.IsInfinity(det)) return double.PositiveInfinity;
                for (int row = col + 1; row < n; row++)
                {
                    double factor = M[row, col] / M[col, col];
                    for (int j = col; j < n; j++)
                        M[row, j] -= factor * M[col, j];
                }
            }
            return det * sign;
        }

        public static double[,] Multiply(double[,] A, double[,] B)
        {
            int n = A.GetLength(0), m = B.GetLength(1), k = B.GetLength(0);
            double[,] C;
            try { C = new double[n, m]; }
            catch (OutOfMemoryException)
            { throw new OutOfMemoryException($"Недостатньо пам'ятi для перевiрки результату ({n}×{m})."); }

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    double s = 0;
                    for (int p = 0; p < k; p++) s += A[i, p] * B[p, j];
                    if (double.IsInfinity(s) || double.IsNaN(s))
                        throw new OverflowException($"Переповнення при перевiрцi результату (елемент [{i + 1},{j + 1}]).");
                    C[i, j] = s;
                }
            return C;
        }

        public static double CheckError(double[,] A, double[,] inv)
        {
            int n = A.GetLength(0);
            double[,] product = Multiply(A, inv);
            double maxErr = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    double expected = (i == j) ? 1.0 : 0.0;
                    maxErr = Math.Max(maxErr, Math.Abs(product[i, j] - expected));
                }
            return maxErr;
        }

        public static double[,] GenerateRandom(int n, double minVal = -10.0, double maxVal = 10.0)
        {
            if (n < 2 || n > MAX_SIZE)
                throw new ArgumentException($"Розмiр матрицi {n} виходить за межi [2; {MAX_SIZE}].");

            double[,] A;
            try { A = new double[n, n]; }
            catch (OutOfMemoryException)
            { throw new OutOfMemoryException($"Недостатньо пам'ятi для генерацiї матрицi {n}×{n}."); }

            Random rnd = new Random();

            if (n > 20)
            {
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        A[i, j] = Math.Round(minVal + rnd.NextDouble() * (maxVal - minVal), 2);
                for (int i = 0; i < n; i++)
                {
                    double rowSum = 0;
                    for (int j = 0; j < n; j++) if (i != j) rowSum += Math.Abs(A[i, j]);
                    if (Math.Abs(A[i, i]) <= rowSum)
                        A[i, i] = (A[i, i] >= 0 ? 1 : -1) * (rowSum + Math.Abs(minVal) + 1.0);
                }
                return A;
            }

            int attempts = 0;
            do
            {
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        A[i, j] = Math.Round(minVal + rnd.NextDouble() * (maxVal - minVal), 2);
                attempts++;
                if (attempts > 300)
                    throw new Exception("Не вдалося згенерувати невироджену матрицю за 300 спроб.");
            }
            while (Math.Abs(Determinant(A)) < EPSILON);

            return A;
        }
    }
}