using System;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace Demo7
{
    public partial class Form1 : Form
    {

        private Dictionary<string, ImageInfo> processedImages = new Dictionary<string, ImageInfo>();
        private string _currentDirectory = string.Empty;
        public Form1()
        {
            InitializeComponent();
            _currentDirectory = "C:\\Users\\Lenovo\\Desktop\\work\\Test3";
            LoadFiles(_currentDirectory);
        }

        //读取文件
        private void LoadFiles(string folderPath)
        {
            try
            {
                comboBox1.Items.Clear();

                string[] files = Directory.GetFiles(folderPath);

                if(files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        comboBox1.Items.Add(Path.GetFileName(file));
                    }
                    comboBox1.SelectedIndex = 1;
                }
                else
                {
                    comboBox1.Items.Add("当前目录没有文件");
                    comboBox1.SelectedIndex = 0;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件失败：{ex.Message}","错误");

                //出错是恢复提示状态
                comboBox1.Items.Clear();
                comboBox1.Items.Add("读取失败，请重试");
                comboBox1.SelectionLength = 0;
            }
        }

        //下拉选择
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null &&
                !comboBox1.SelectedItem.ToString().Contains("请先选择文件夹") &&
                !comboBox1.SelectedItem.ToString().Contains("文件夹中没有文件") &&
                !comboBox1.SelectedItem.ToString().Contains("读取失败"))
            {
                //获取选中的文件夹名
                string seletedFile = comboBox1.SelectedItem.ToString();

                //显示当前文件名
                label1.Text = $"当前选择：{seletedFile}";

                //加载图片到PictureBox
                string fullPath = Path.Combine(_currentDirectory, seletedFile);
                Mat originalImage = new Mat();
                originalImage = LoadAndResizeImage(fullPath);

                // 灰度转换
                Mat gray = new Mat();
                if (originalImage.Channels() == 3)
                {
                    Cv2.CvtColor(originalImage, gray, ColorConversionCodes.BGR2GRAY);
                }
                else if (originalImage.Channels() == 4)
                {
                    Cv2.CvtColor(originalImage, gray, ColorConversionCodes.BGRA2GRAY);
                }
                else
                {
                    gray = originalImage.Clone(); // 已经是灰度图
                }

                // 高斯模糊
                Mat blurred = new Mat();
                Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 2.0);
                //Cv2.ImShow("blurred", blurred);

                // 二值化处理
                Mat binary = new Mat();
                Cv2.Threshold(blurred, binary, 0, 255, ThresholdTypes.Otsu);//255可修改
                //Cv2.ImShow("binary", binary);

                // 反转二值图像
                Mat dst = new Mat();
                Cv2.BitwiseNot(binary, dst);
                //Cv2.ImShow("dst", dst);

                //Mat dst1 = new Mat();
                //Cv2.BitwiseNot(blurred, dst1);
                //Cv2.ImShow("dst", dst1);

                // 边缘检测
                Mat edges = new Mat();
                Cv2.Canny(dst, edges, 50, 150);
                Cv2.ImShow("edges",edges);









                ////尝试霍夫圆解决问题
                //CircleSegment[] circles = Cv2.HoughCircles(gray, HoughModes.Gradient, 1, 30, 100, 30, 5, 50);


                //Mat op = new Mat();
                //Cv2.CvtColor(gray, op, ColorConversionCodes.GRAY2BGR);
                //foreach (CircleSegment circle in circles)
                //{
                //    // 直接从这里获取圆心坐标
                //    Point2f center = circle.Center;
                //    float radius = circle.Radius;

                //    Console.WriteLine($"找到一个圆，圆心在 ({center.X}, {center.Y})，半径为 {radius}");

                //    // 在新图像上绘制圆心（红色）
                //    Cv2.Circle(op, (int)center.X, (int)center.Y, 1, new Scalar(0, 0, 255), 2);

                //    // 可选：绘制圆轮廓（绿色）
                //    Cv2.Circle(op, (int)center.X, (int)center.Y, (int)radius, new Scalar(0, 255, 0), 2);
                //}
                //Cv2.ImShow("检测的圆", op);


                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                    pictureBox1.Image = null;
                }
                if (pictureBox2.Image != null)
                {
                    pictureBox2.Image.Dispose();
                    pictureBox2.Image = null;
                }
                if (pictureBox3.Image != null)
                {
                    pictureBox3.Image.Dispose();
                    pictureBox3.Image = null;
                }
                if (pictureBox4.Image != null)
                {
                    pictureBox4.Image.Dispose();
                    pictureBox4.Image = null;
                }
                if (pictureBox5.Image != null)
                {
                    pictureBox5.Image.Dispose();
                    pictureBox5.Image = null;
                }
                try
                {
                    //加载图片
                    pictureBox1.Image = System.Drawing.Image.FromFile(fullPath);        //加载原图
                    pictureBox2.Image = BitmapConverter.ToBitmap(gray);                 //加载灰度图
                    pictureBox3.Image = BitmapConverter.ToBitmap(binary);               //加载二值化
                    pictureBox4.Image = BitmapConverter.ToBitmap(blurred);              //加载高斯模糊
                    pictureBox5.Image = BitmapConverter.ToBitmap(edges);                //加载轮廓
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"加载图片失败：{ex.Message}","错误");
                }

            }
        }



        //分界线
        //图片处理
        public class ImageInfo
        {
            public string FilePath { get; set; }
            public int OriginalWidth { get; set; }
            public int OriginalHeight { get; set; }
            public double ScaleX { get; set; }
            public double ScaleY { get; set; }
            public Mat DisplayImage { get; set; }

            //计算比例
            public void CalculateScale(int targetwidth = 800 , int targetheight = 600)
            {
                ScaleX = (double)OriginalWidth / targetwidth;
                ScaleY = (double)OriginalHeight / targetheight;
            }
        }
        //图像尺寸处理
        public Mat LoadAndResizeImage(string imagePath)
        {
            int targetWidth = 800;
            int targetHeight = 600;
            double ImageX, ImageY = 0.0;
            //读取图片
            Mat originalImage = Cv2.ImRead(imagePath);
            //创建图像信息
            var imageInfo = new ImageInfo
            {
                FilePath = imagePath,
                OriginalWidth = originalImage.Width,
                OriginalHeight = originalImage.Height
            };

            label7.Text = $"图片尺寸：{imageInfo.OriginalWidth}x{imageInfo.OriginalHeight}";

            // 计算缩放比例
            imageInfo.CalculateScale(targetWidth, targetHeight);
            ImageX = imageInfo.ScaleX;
            ImageY = imageInfo.ScaleY;

            // 缩放到统一大小
            Mat resizedImage = new Mat();
            Cv2.Resize(originalImage, resizedImage, new OpenCvSharp.Size(targetWidth, targetHeight));

            imageInfo.DisplayImage = resizedImage.Clone();

            // 存储信息
            processedImages[imagePath] = imageInfo;

            return resizedImage;

        }
    }
}
