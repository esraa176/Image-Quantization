using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace ImageQuantization
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        RGBPixel[,] ImageMatrix;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
        }

        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            //double sigma = double.Parse(txtGaussSigma.Text); 
            //int maskSize = (int)nudMaskSize.Value ;
            //ImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
            int k = int.Parse(textBox1.Text);
            //####################################################//
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //####################################################//
            ImageOperations.getDistinctColors(ImageMatrix, k);
            ImageMatrix = ImageOperations.replacingColors(ImageMatrix);
            //##################################################//
            stopwatch.Stop();
            Console.WriteLine("==============================================");
            Console.WriteLine("Total Execution Time in Seconds: {0} s", stopwatch.ElapsedMilliseconds * 0.001);
            Console.WriteLine("Total Execution Time in MilliSeconds: {0} ms", stopwatch.ElapsedMilliseconds);
            //##################################################//
            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
            textBox2.Text = ImageOperations.GetMST().ToString();
            txt_Distinct.Text = ImageOperations.GetDistinct().ToString();
            timeSecTxt.Text = (stopwatch.ElapsedMilliseconds * 0.001).ToString();
            timeMSecTxt.Text = stopwatch.ElapsedMilliseconds.ToString();

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }
    }
}