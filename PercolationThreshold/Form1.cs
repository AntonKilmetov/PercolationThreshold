using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace PercolationThreshold
{ 
    public partial class Form1 : Form
    {
        private bool[,] field; // Заполнение ячеек
        private int[,] markedField; // Маркировка заполненых ячеек
        private List<int> np; // Ссылки на коллизии
        private HashSet<int> marckerOfConnectingCluster;
        private Dictionary<int, int> distribution;
        private static double p = 0.1; // Вероятность заполнить ячейку
        private static int sideLength = 16; // Длина стороны L
        public Form1()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Рисуется перколяционная картина + определяется наличие соединяющего кластера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = p.ToString();
            FillField();
            DetectConnectingCluster();
            PInfinity();
            FillDistribution();
            MediumSizeDistribution();
            Draw();
        }
        /// <summary>
        /// Заполнения ячеек в соответсвии с вероятностью P
        /// </summary>
        private void FillField() 
        {
            var random = new Random();
            field = new bool[sideLength, sideLength];
            for (int i = 0; i < sideLength; i++)
                for (int j = 0; j < sideLength; j++)
                    field[i, j] = random.NextDouble() < p;
        }
        /// <summary>
        /// Увеличить вероятность заполнения Р и перерисовать перколяционную картину
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            p += 0.025;
            textBox1.Text = p.ToString();
            FillField();          
            DetectConnectingCluster();
            PInfinity();
            FillDistribution();
            MediumSizeDistribution();
            Draw();
        }
        /// <summary>
        /// Отрисовка перколяционной картины
        /// </summary>
        private void Draw()
        {
            var bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            var graph = Graphics.FromImage(bmp);
            var whiteBrush = new SolidBrush(Color.White);
            var width = pictureBox1.Width / sideLength;
            var height = pictureBox1.Height / sideLength;
            for (int i = 0; i < sideLength; i++)
            {
                for (int j = 0; j < sideLength; j++)
                {
                    if (field[i, j])
                        graph.FillRectangle(
                            new SolidBrush(Color.FromArgb(markedField[i, j] * 13 % 254, markedField[i, j] * 53 % 254, markedField[i, j] * 2 % 254)),
                            j * width,
                            i * height,
                            width - 1,
                            height - 1);
                    else
                        graph.FillRectangle(whiteBrush, j * width, i * height, width - 1, height - 1);
                }
            }
            pictureBox1.Image = bmp;
        }
        /// <summary>
        /// Информирует о наличие соединяющего кластера 
        /// </summary>
        private void DetectConnectingCluster()
        {
            marckerOfConnectingCluster = new HashSet<int>();
            HoshenKopelmanAlgorithm();
            for (int i = 0; i < sideLength; i++)
                for (int j = 0; j < sideLength; j++)
                    if (markedField[0, i] == markedField[sideLength - 1, j] && markedField[0, i] != -1)
                        marckerOfConnectingCluster.Add(markedField[0, i]);
            if (marckerOfConnectingCluster.Count == 0) textBox2.Text = "Нет";
            else textBox2.Text = "Есть";
        }
        /// <summary>
        /// Алгоритм Хошена - Копельмана
        /// </summary>
        private void HoshenKopelmanAlgorithm()
        {
            markedField = new int[sideLength, sideLength];
            np = new List<int>();
            var marker = 0;
            for (int i = 0; i < sideLength; i++)
                for (int j = 0; j < sideLength; j++)
                    markedField[i, j] = -1;
            for (int i = sideLength - 1; i >= 0; i--)
            {
                for (int j = 0; j < sideLength; j++)
                {
                    if (i == sideLength - 1 && j == 0) // начальный узел
                    {
                        if (field[i, j])
                        {
                            markedField[i, j] = marker;
                            np.Add(-1);
                            marker++;
                        }
                    }
                    else if (i == sideLength - 1) // нижние узлы
                    {
                        if (field[i, j])
                        {
                            if (field[i, j - 1])
                                markedField[i, j] = markedField[i, j - 1];
                            else
                            {
                                markedField[i, j] = marker;
                                np.Add(-1);
                                marker++;
                            }
                        }
                    }
                    else if (j == 0) // левые узлы
                    {
                        if (field[i, j])
                        {
                            if (field[i + 1, j])
                                markedField[i, j] = markedField[i + 1, j];
                            else
                            {
                                markedField[i, j] = marker;
                                np.Add(-1);
                                marker++;
                            }
                        }
                    }
                    else // остальные узлы
                    {
                        if (field[i, j])
                        {
                            if (field[i + 1, j] && field[i, j - 1])
                            {
                                if (markedField[i + 1, j] == markedField[i, j - 1])
                                    markedField[i, j] = markedField[i + 1, j];
                                else
                                {
                                    markedField[i, j] = Math.Min(markedField[i + 1, j], markedField[i, j - 1]);
                                    np[Math.Max(markedField[i + 1, j], markedField[i, j - 1])]
                                        = Math.Min(markedField[i + 1, j], markedField[i, j - 1]);
                                    Relabel(); // Если есть коллизия происходит перемаркировка
                                }
                            }
                            else if (field[i + 1, j])
                                markedField[i, j] = markedField[i + 1, j];
                            else if (field[i, j - 1])
                                markedField[i, j] = markedField[i, j - 1];
                            else
                            {
                                markedField[i, j] = marker;
                                np.Add(-1);
                                marker++;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Перемаркировка в случае коллизии
        /// </summary>
        private void Relabel()
        {
            for (int i = np.Count - 1; i >= 0; i--)
                if (np[i] != -1)
                {
                    for (int j = 0; j < sideLength; j++)
                        for (int k = 0; k < sideLength; k++)
                            if (markedField[j, k] == i)
                                markedField[j, k] = np[i];
                    np[i] = -1; 
                }
        }
        private void PInfinity()
        {
            var count = 0;
            foreach (var item in markedField)
            {
                if (marckerOfConnectingCluster.Contains(item))
                    count++;
            }
            if (count != 0)
                textBox3.Text = ((double)count / (sideLength * sideLength)).ToString();
            else
                textBox3.Text = "0";
        }
        private void FillDistribution()
        {
            distribution = new Dictionary<int, int>();
            foreach (var item in markedField)
            {
                if (item < 0) continue;
                if (distribution.ContainsKey(item))
                    distribution[item]++;
                else
                    distribution.Add(item, 1);
            }
        }
        /// <summary>
        /// Распределение среденего размера кластеров = ns
        /// </summary>
        private void MediumSizeDistribution()
        {
            var n = sideLength * sideLength;
            var s = new int[n]
                .Select((x, y) => y + 1)
                .ToArray();
            var ns = new double[n];
            for (int i = 0; i < n; i++)
                foreach (var item in distribution.Values)
                    if (s[i] == item)
                        ns[i]++;
            ns = ns
                .Select(x => x / n)
                .ToArray();
            textBox4.Text = AverageClusterSize(s,ns).ToString();
            //ToFile(s, ns);
        } 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s">Возможное количество ячеек кластера</param>
        /// <param name="ns">Распределение среденего размера кластеров</param>
        /// <returns>Средний размер кластера</returns>
        private double AverageClusterSize(int[] s, double[] ns)
        {
            var numerator = 0.0;
            var denominator = 0.0;
            for (int i = 0; i < ns.Length; i++)
            {
                numerator += s[i] * s[i] * ns[i];
                denominator += s[i] * ns[i];
            }
            return numerator / denominator;
        }
        private void ToFile(int[] s, double[] ns)
        {
            var newS = s.Select(x => x.ToString()).ToArray();
            var newNS = ns.Select(x => x.ToString()).ToArray();
            File.WriteAllLines(@"path", newS);
            File.WriteAllLines(@"path", newNS);
        }
    }
}
