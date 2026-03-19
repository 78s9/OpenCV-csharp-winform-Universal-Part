using System;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Demo7
{
    public partial class Form1 : Form
    {

        private Dictionary<string, ImageInfo> processedImages = new Dictionary<string, ImageInfo>();
        private string _currentDirectory = string.Empty;
        private int thresholdValue = 127;
        private int maxValue = 255;
        public Form1()
        {
            InitializeComponent();
            _currentDirectory = "C:\\Users\\Lenovo\\Desktop\\work\\Test3";
            LoadFiles(_currentDirectory);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 从文本框获取阈值参数
            thresholdValue = int.Parse(textBox1.Text); 
            maxValue = int.Parse(textBox2.Text);
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
        //增加图片亮度
        public Mat IncreaseBrightness(Mat image, double brightnessValue)
        {
            Mat result = new Mat();
            // brightnessValue > 0 增加亮度，< 0 降低亮度
            image.ConvertTo(result, MatType.CV_8UC3, 1.0, brightnessValue);
            return result;
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
                Mat test1 = new Mat();
                test1 = LoadAndResizeImage(fullPath);
                //Cv2.ImShow("原图", test1);
                Mat originalImage =new Mat();
                originalImage = IncreaseBrightness(test1,115);
                Cv2.ImShow("原图加亮（增加115）",originalImage);

                Mat test = new Mat();
                test = ScharrMat(originalImage);



                Mat grad_x = new Mat();
                Mat grad_y = new Mat();
                Mat abs_grad_x = new Mat();
                Mat abs_grad_y = new Mat();
                Mat ds1 = new Mat();

                //使用Sobel方法
                Cv2.Sobel(originalImage, grad_x, MatType.CV_16S, 1, 0, 3, 1, 1,BorderTypes.Default);
                Cv2.ConvertScaleAbs(grad_x, abs_grad_x);
                //Cv2.ImShow("X方向sobel",abs_grad_x);

                Cv2.Sobel(originalImage, grad_y, MatType.CV_16S, 1, 0, 3, 1, 1, BorderTypes.Default);
                Cv2.ConvertScaleAbs(grad_y, abs_grad_y);
                //Cv2.ImShow("Y方向sobel", abs_grad_y);

                Cv2.AddWeighted(abs_grad_x, 0.5, abs_grad_y, 0.5, 0, ds1);
                //Cv2.ImShow("整体图片",ds1);

                Mat binary1 = new Mat();


                // 1. 确保ds1是单通道8位图像
                if (ds1.Type() != MatType.CV_8UC1)
                {
                    // 如果是其他类型，转换为8位单通道
                    if (ds1.Type() == MatType.CV_8UC3)
                    {
                        // 如果是三通道，转换为灰度图
                        Cv2.CvtColor(ds1, binary1, ColorConversionCodes.BGR2GRAY);
                    }
                    else
                    {
                        // 如果是其他类型（如16位），转换为8位
                        ds1.ConvertTo(binary1, MatType.CV_8UC1);
                    }
                }
                else
                {
                    ds1.CopyTo(binary1);
                }

                if (ds1.Type() == MatType.CV_8UC3)
                {
                    // 如果是三通道，转换为灰度图
                    Cv2.CvtColor(ds1, binary1, ColorConversionCodes.BGR2GRAY);
                }

                Mat binary2 = new Mat();
                Cv2.Threshold(binary1, binary2, 50, 255, ThresholdTypes.Binary);
                Console.WriteLine($"二值化后图像类型: {binary2.Type()}, 通道数: {binary2.Channels()}");

                // 2. 查找轮廓
                Point[][] contours2;
                HierarchyIndex[] hierarchy3;
                Cv2.FindContours(binary2, out contours2, out hierarchy3, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                // 3. 在原图上绘制轮廓
                Mat result = binary2.Clone();
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

                for (int i = 0; i < contours2.Length; i++)
                {
                    // 可以过滤太小的轮廓（根据面积）
                    double area = Cv2.ContourArea(contours2[i]);
                    if (area > 100) // 只绘制面积大于100的轮廓
                    {
                        // 随机颜色或固定颜色
                        Scalar color = new Scalar(0, 255, 0); // 绿色
                        Cv2.DrawContours(result, contours2, i, color, 2);

                        // 或者绘制轮廓的外接矩形
                        Rect boundingRect = Cv2.BoundingRect(contours2[i]);
                        //Cv2.Rectangle(result, boundingRect, new Scalar(255, 0, 0), 2); // 蓝色矩形
                    }
                }

                Cv2.ImShow("用Sobel轮廓检测结果", result);


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
                //Cv2.ImShow("gray",gray);

                // 高斯模糊
                Mat blurred = new Mat();
                Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(7, 7), 2.0);
                //Cv2.ImShow("blurred", blurred);

                // 二值化处理
                Mat binary = new Mat();
                Cv2.Threshold(blurred, binary, thresholdValue, maxValue, ThresholdTypes.Otsu);
                 //Cv2.ImShow("binary", binary);


                // 反转二值图像
                Mat dst = new Mat();
                Cv2.BitwiseNot(binary, dst);
                //Cv2.ImShow("dst", dst);


                //创建结构元素（核）
                // 使用3x3的矩形核
                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                // 创建不同形状的核
                Mat rectKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));               //按照矩形进行膨胀、侵蚀，适合去除大噪声
                Mat ellipseKernel3 = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(30, 30));      //按照圆来进行膨胀、侵蚀，适合保留形状
                Mat crossKernel = Cv2.GetStructuringElement(MorphShapes.Cross, new OpenCvSharp.Size(5, 5));             //按照十字进行膨胀、侵蚀，适合保留笔画特征


                //膨胀
                Mat dilated1 = new Mat();
                Cv2.Dilate(binary, dilated1, kernel);
                //Cv2.ImShow("膨胀1", dilated1);
                //Cv2.ImShow("膨胀7", dilated7);

                //侵蚀
                Mat eroded1 = new Mat();
                Cv2.Erode(binary, eroded1, kernel);
                //Cv2.ImShow("侵蚀1", eroded1);

                //Mat dst1 = new Mat();
                //Cv2.BitwiseNot(blurred, dst1);
                //Cv2.ImShow("dst", dst1);

                // 边缘检测
                Mat edges = new Mat();
                Cv2.Canny(binary, edges, 50, 150);
                //Cv2.ImShow("edges",edges);

                Cv2.FindContours(edges, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] hierarchy,
                 RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                //Console.WriteLine($"找到 {contours.Length} 个轮廓");


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

        //调用Scharr函数
        public Mat ScharrMat(Mat image)
        {
            Mat src = image.Clone();
            Mat grad_x = new Mat();
            Mat grad_y = new Mat();
            Mat abs_grad_x = new Mat();
            Mat abs_grad_y = new Mat();
            Mat dst = new Mat();

            //Cv2.ImShow("原图",src);

            //用scharr函数求X方向梯度
            Cv2.Scharr(src, grad_x, MatType.CV_16S, 1, 0, 1, 0, BorderTypes.Default);
            Cv2.ConvertScaleAbs(grad_x, abs_grad_x);
            //Cv2.ImShow("[效果图]Xf方向Scharr", abs_grad_x);

            //用scharr函数求X方向梯度
            Cv2.Scharr(src, grad_y, MatType.CV_16S, 0, 1, 1, 0, BorderTypes.Default);
            Cv2.ConvertScaleAbs(grad_y, abs_grad_y);
            //Cv2.ImShow("[效果图]Yf方向Scharr", abs_grad_y);

            //合并梯度
            Cv2.AddWeighted(abs_grad_x, 0.5, abs_grad_y, 0.5, 0, dst);
            Cv2.ImShow("[效果图]合并后的Scharr", dst);

            Mat gray = new Mat();

            //确保ds1是单通道8位图像
            if (dst.Type() != MatType.CV_8UC1)
            {
                //如果是其他类型，转换为8位单通道
                if (dst.Type() == MatType.CV_8UC3)
                {
                    //如果是三通道，转换为灰度图
                    Cv2.CvtColor(dst, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    //如果是其他类型（如16位），转换为8位
                    dst.ConvertTo(gray, MatType.CV_8UC1);
                }
            }
            else
            {
                dst.CopyTo(gray);
            }

            if (dst.Type() == MatType.CV_8UC3)
            {
                //如果是三通道，转换为灰度图
                Cv2.CvtColor(dst, gray, ColorConversionCodes.BGR2GRAY);
            }

            //二值化
            Mat binary = new Mat();
            Cv2.Threshold(gray, binary, thresholdValue, maxValue, ThresholdTypes.Otsu);
            //Console.WriteLine($"二值化后图像类型: {binary.Type()}, 通道数: {binary.Channels()}");


            return src;
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
