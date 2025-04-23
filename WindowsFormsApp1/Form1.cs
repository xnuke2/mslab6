using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{


        public partial class Form1 : Form
        {
            public Form1()
            {
                InitializeComponent();
                SetupDataGridView();
            }

            private void SetupDataGridView()
            {
                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            private void btnCalculate_Click(object sender, EventArgs e)
            {
            dataGridView1.DataSource=null;
            panel1.Visible = false;
            groupBoxResult.Visible = false;
            try
                {
                double[,] P = new double[6, 6];
                for (int i = 0; i < 6; i++)
                    for (int j = 0; j < 6; j++)
                        P[i, j] = Convert.ToDouble(dataGridView2[j, i].Value);
                for (int k = 0; k < 6; k++)
                {
                    double sum = 0;
                    for (int l = 0; l < 6; l++)
                        sum += P[k, l];
                    if (Math.Round(sum, 8) != 1)
                    {
                        MessageBox.Show("Ошибка в матрице вероятностей в строке " + (k + 1));
                        return;
                    }
                }
                double tmpV = Convert.ToDouble(numericUpDown2.Value);
                double[] V = new double[5];
                for (int i = 0; i < 5; i++)
                    V[i] = tmpV;
                double lambda0 = Convert.ToDouble(numericUpDown1.Value);
                int[] K = new int[5];
                for (int i = 0; i < 5; i++)
                    K[i] = Convert.ToInt32(dataGridView3[i, 0].Value);
                ////Параметры варианта
                //int N1 = 9;  // число букв фамилии
                //int N2 = 6;  // число букв имени
                //int N3 = 13; // другое число
                //int N4 = N1 + N2;
                //int N5 = N1 + N3;

                //double lambda0 = 1; // интенсивность входного потока
                //double[] V = { 3, 3, 3, 3, 3 }; // средние времена обслуживания
                //int[] K = { 1, 1, 2, 3, 3 }; // количество каналов в СМО1

                //// Создание матрицы вероятностей передач


                //// Заполнение матрицы P
                //P[0, 1] = 1;
                //P[1, 2] = 1.0 / N1;
                //P[2, 1] = 1.0 / N2;
                //P[3, 2] = 1.0 / N3;
                //P[4, 2] = 1.0 / N4;
                //P[5, 4] = 1.0 / N5;

                //P[1, 5] = 1 - P[1, 2];
                //P[2, 4] = 1 - P[2, 1];
                //P[3, 3] = 1 - P[3, 2];
                //P[4, 4] = 1 - P[4, 2];
                //P[5, 0] = 1 - P[5, 4];

                // Расчет характеристик
                CalculateNetworkCharacteristics(P, lambda0, V, K);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void CalculateNetworkCharacteristics(double[,] P, double lambda0, double[] V, int[] K)
            {
                int nc = P.GetLength(0); // число СМО + источник (S0)
                int n = nc - 1;          // число СМО

                // Создаем матрицу системы уравнений для нахождения λj
                double[,] lambdaSystem = new double[n, n];
                double[] b = new double[n];

                // Заполняем матрицу системы уравнений
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        lambdaSystem[i, j] = P[j + 1, i + 1] - (i == j ? 1 : 0);
                    }
                    b[i] = -P[0, i + 1] * lambda0;
                }

                // Решаем систему уравнений (упрощенный метод - в реальном проекте используйте библиотеку)
                double[] lambdas = SolveLinearSystem(lambdaSystem, b);

                // Расчет характеристик для каждой СМО
                DataTable dt = new DataTable();
                dt.Columns.Add("СМО");
                dt.Columns.Add("λj(Интенсивность входящего потока)", typeof(double));
                dt.Columns.Add("βj(Среднее число занятых каналов)", typeof(double));
                dt.Columns.Add("ρj(Загрузка системы)", typeof(double));
                dt.Columns.Add("Стационарность", typeof(string));
                dt.Columns.Add("P0j(Вероятность отсутсвия заявок)", typeof(string));
                dt.Columns.Add("Средняя длина очереди", typeof(string));
                dt.Columns.Add("Средние число заявок", typeof(string));
                dt.Columns.Add("Время ожидания в очереди", typeof(string));
                dt.Columns.Add("Время пребывания в системе", typeof(string));
                

                double[] pArr = new double[n];
                double[] lArr = new double[n];
                double[] mArr = new double[n];
                double[] wArr = new double[n];
                double[] tArr = new double[n];

                for (int j = 0; j < n; j++)
                {
                    double a = lambdas[j] / lambda0;
                    double beta = lambdas[j] * V[j];
                    double rho = beta / K[j];

                    bool isStationary = rho <= 1;
                    double p0 = isStationary ? CalculateP0(beta, K[j]) : double.NaN;
                    double l = isStationary ? CalculateAvgQueue(beta, K[j], p0) : double.NaN;
                    double m = isStationary ? l + beta : double.NaN;
                    double w = isStationary ? l / lambdas[j] : double.NaN;
                    double T = isStationary ? w + V[j] : double.NaN;

                    pArr[j] = rho;
                    lArr[j] = l;
                    mArr[j] = m;
                    wArr[j] = a * w;
                    tArr[j] = a * T;

                    dt.Rows.Add(
                        $"S{j + 1}",
                        Math.Round(lambdas[j], 4),
                        Math.Round(beta, 4),
                        Math.Round(rho, 4),
                        isStationary ? "Да" : "Нет",
                        isStationary ? Math.Round(p0, 4)+ "" : "-",
                        isStationary ? Math.Round(l, 4) + "" : "-",
                        isStationary ? Math.Round(m, 4) + "" : "-",
                        isStationary ? Math.Round(w, 4) + "" : "-",
                        isStationary ? Math.Round(T, 4) + "" : "-"
                        
                    );
                }

                dataGridView1.DataSource = dt;

                // Расчет характеристик всей сети
                bool networkStationary = true;
                foreach (double rho in pArr)
                {
                    if (rho > 1) networkStationary = false;
                }

                if (networkStationary)
                {
                    double L = 0, N = 0, W = 0, T = 0;
                    for (int j = 0; j < n; j++)
                    {
                        L += lArr[j];
                        N += mArr[j];
                        W += wArr[j];
                        T += tArr[j];
                    }
                    groupBoxResult.Visible = true;
                    txtTotalQueue.Text = Math.Round(L, 4).ToString();
                    txtTotalRequests.Text = Math.Round(N, 4).ToString();
                    txtTotalWaitTime.Text = Math.Round(W, 4).ToString();
                    txtTotalTimeInSystem.Text = Math.Round(T, 4).ToString();
                    lblNetworkStatus.Text = "Сеть стационарна";
                    panel1.Visible = true;
                    lblNetworkStatus.ForeColor = System.Drawing.Color.Green;
            }
            else
                {
                    groupBoxResult.Visible = true;
                    txtTotalQueue.Text = "N/A";
                    txtTotalRequests.Text = "N/A";
                    txtTotalWaitTime.Text = "N/A";
                    txtTotalTimeInSystem.Text = "N/A";
                    lblNetworkStatus.Text = "Сеть не стационарна!";
                    lblNetworkStatus.ForeColor = System.Drawing.Color.Red;
            }
            }

            private double[] SolveLinearSystem(double[,] A, double[] b)
            {
                // В реальном проекте используйте библиотеку для решения СЛАУ, например Math.NET
                // Здесь упрощенная реализация для демонстрации

                int n = b.Length;
                double[] x = new double[n];

                // Простая реализация метода Гаусса (без выбора главного элемента)
                for (int k = 0; k < n - 1; k++)
                {
                    for (int i = k + 1; i < n; i++)
                    {
                        double factor = A[i, k] / A[k, k];
                        for (int j = k; j < n; j++)
                        {
                            A[i, j] -= factor * A[k, j];
                        }
                        b[i] -= factor * b[k];
                    }
                }

                // Обратный ход
                for (int i = n - 1; i >= 0; i--)
                {
                    double sum = 0;
                    for (int j = i + 1; j < n; j++)
                    {
                        sum += A[i, j] * x[j];
                    }
                    x[i] = (b[i] - sum) / A[i, i];
                }

                return x;
            }

            private double CalculateP0(double beta, int K)
            {
                if (K == 1) 
                {
                    return 1 - beta;
                }
                else 
                {
                    double sum = 0;
                    for (int m = 0; m < K; m++)
                    {
                        sum += Math.Pow(beta, m) / Factorial(m);
                    }
                    sum += Math.Pow(beta, K) / (Factorial(K) * (1 - beta / K));
                    return 1 / sum;
                }
            }

            private double CalculateAvgQueue(double beta, int K, double P0)
            {

                return (Math.Pow(beta, K + 1) * P0 /
                        (Factorial(K) * K * Math.Pow(1 - (beta / K), 2)));
            }

            private int Factorial(int n)
            {
                if (n <= 1) return 1;
                int result = 1;
                for (int i = 2; i <= n; i++)
                {
                    result *= i;
                }
                return result;
            }

        private void Form1_Load(object sender, EventArgs e)
        {
            int rang = 6;
            for (int i = 0;i<rang;i++)
                dataGridView2.Columns.Add("S" + i, "S" + i);
            for (int i = 0; i < rang; i++)
            {
                dataGridView2.Rows.Add();
                dataGridView2.Rows[i].HeaderCell.Value = "S" + i;
            }
            for (int i = 0;i< rang;i++)
                for (int j = 0;j < rang;j++)
                    dataGridView2[i,j].Value = 0;
            for (int i = 1;i < rang;i++)
                dataGridView3.Columns.Add("K" + i, "K" + i);
            dataGridView3.Rows.Add();
            for (int i = 0; i < rang-1; i++)
                dataGridView3[i,0].Value = 1;
        }
    }
    
}
