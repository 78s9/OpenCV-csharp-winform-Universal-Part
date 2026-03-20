using System;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms.VisualStyles;

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
                Mat test = new Mat();
                test = LoadAndResizeImage(fullPath);

                //Console.WriteLine($"原图像图像类型: {test.Type()}, 通道数: {test.Channels()}");

                //Cv2.ImShow("原图", test1);

                Mat originalImage =new Mat();
                originalImage = IncreaseBrightness(test, 100);

                //Cv2.ImShow("原图加亮（增加100）",originalImage);



                Mat test1 = new Mat();
                Mat test2 = new Mat();
                Mat test3 = new Mat();
                Mat test4 = new Mat();

                //test1 = ScharrMat(originalImage);
                //test2 = SobelMat(originalImage);
                //test3 = CornerHarrisone(originalImage);
                //test4 = HoughCircles(test1);

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

                //霍夫曼画圆需要输入灰度图
                //test4 = HoughCircles(gray);

                //均值滤波
                Mat mean = new Mat();
                Cv2.Blur(gray, mean, new OpenCvSharp.Size(5, 5));
                Cv2.ImShow("均值滤波", mean);

                //中值滤波
                Mat median = new Mat();
                Cv2.MedianBlur(gray, median, 1);
                Cv2.ImShow("中值滤波",median);
                

                //双边滤波
                Mat bilateral = new Mat();
                Cv2.BilateralFilter(gray, bilateral, 9, 75, 75);
                Cv2.ImShow("双边滤波", bilateral);

                // 高斯模糊
                Mat blurred = new Mat();
                Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(7, 7), 2.0);
                //Cv2.ImShow("blurred", blurred);

                //锐化
                //拉普拉斯算子
                Console.WriteLine($"原图像图像类型: {gray.Type()}, 通道数: {gray.Channels()}");
                Mat laplacian = new Mat();
                Cv2.Laplacian(mean, laplacian, MatType.CV_8U, 3);
                //Cv2.ImShow("laplacian", laplacian);
                Mat res = new Mat();
                Cv2.AddWeighted(mean, 1.0, laplacian, 1.0, 0, res);
                Cv2.ImShow("res", res);

                test1 = ScharrMat(res);



                // 二值化处理
                Mat binary = new Mat();
                Cv2.Threshold(blurred, binary, thresholdValue, maxValue, ThresholdTypes.Otsu);
                 //Cv2.ImShow("binary", binary);

                // 反转二值图像
                //Mat dst = new Mat();
                //Cv2.BitwiseNot(binary, dst);
                //Cv2.ImShow("dst", dst);

                //创建结构元素（核）
                // 使用3x3的矩形核
                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                // 创建不同形状的核
                Mat rectKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));            //按照矩形进行膨胀、侵蚀，适合去除大噪声
                Mat ellipseKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));      //按照圆来进行膨胀、侵蚀，适合保留形状
                Mat crossKernel = Cv2.GetStructuringElement(MorphShapes.Cross, new OpenCvSharp.Size(5, 5));          //按照十字进行膨胀、侵蚀，适合保留笔画特征

                Mat dilated = new Mat();
                Mat a = new Mat();
                Mat Imag = new Mat();

                //膨胀
                Cv2.Dilate(binary, Imag, ellipseKernel);
                //Cv2.Dilate(binary, dilated, kernel);
                Cv2.MorphologyEx(binary, a, MorphTypes.Close, ellipseKernel);
                //Cv2.ImShow("膨胀1", dilated1);
                //Cv2.ImShow("膨胀2", Imag);
                //Cv2.ImShow("a", a);

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

                //在原图上绘制轮廓
                Mat result = binary.Clone();
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

                for (int i = 0; i < contours.Length; i++)
                {
                    // 可以过滤太小的轮廓（根据面积）
                    double area = Cv2.ContourArea(contours[i]);
                    if (area > 100) // 只绘制面积大于100的轮廓
                    {
                        // 随机颜色或固定颜色
                        Scalar color = new Scalar(0, 255, 0); // 绿色
                        Cv2.DrawContours(result, contours, i, color, 2);

                        // 或者绘制轮廓的外接矩形
                        Rect boundingRect = Cv2.BoundingRect(contours[i]);
                        //Cv2.Rectangle(result, boundingRect, new Scalar(255, 0, 0), 2); // 蓝色矩形
                    }
                }

                //Cv2.ImShow("用轮廓检测结果", result);
                Mat ko = new Mat();
                ko = TemplateBasedCircleDetector(result);


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

        public class CircleInfo
        {
            public Point2f Center { get; set; }
            public float Radius { get; set; }
            public double Area { get; set; }
            public double Circularity { get; set; }
            public double MatchScore { get; set; }
        }
        //样板匹配
        public Mat TemplateBasedCircleDetector(Mat image)
        {
            Mat templateImage = new Mat();
            templateImage = Cv2.ImRead("C:\\Users\\Lenovo\\Desktop\\work\\Test3\\CNN\\15.Png");
            Mat op = new Mat();
            op = image;
            // 分析样板特征
            AnalyzeTemplate(templateImage);

            //Mat lp = new Mat();
            //lp = ScharrMat(op);
            //Cv2.ImShow("lp", lp);
            //Mat kk = new Mat();
            //DatectCircles(lp);

            return image;
        }
        //分析样板图
        public Mat AnalyzeTemplate(Mat templateImage)
        {
            Mat image = new Mat();
            image = templateImage.Clone();
            Mat gray = new Mat();
            if (image.Channels() == 3)
                Cv2.CvtColor(templateImage, gray, ColorConversionCodes.BGR2GRAY);
            else
                gray = image.Clone();

            Mat grad_x = new Mat();
            Mat grad_y = new Mat();
            Mat abs_grad_x = new Mat();
            Mat abs_grad_y = new Mat();
            Mat dst = new Mat();

            //用scharr函数求X方向梯度
            Cv2.Scharr(gray, grad_x, MatType.CV_16S, 1, 0, 1, 0, BorderTypes.Default);
            Cv2.ConvertScaleAbs(grad_x, abs_grad_x);

            //用scharr函数求X方向梯度
            Cv2.Scharr(gray, grad_y, MatType.CV_16S, 0, 1, 1, 0, BorderTypes.Default);
            Cv2.ConvertScaleAbs(grad_y, abs_grad_y);

            //合并梯度
            Cv2.AddWeighted(abs_grad_x, 0.5, abs_grad_y, 0.5, 0, dst);

            Mat binary = new Mat();
            Cv2.Threshold(gray, binary, thresholdValue, maxValue, ThresholdTypes.Otsu);

            //查找轮廓
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(binary, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length > 0)
            {

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                double templateArea = 0.0;
                double templateCircularity = 0.0;
                double templateRadius = 0.0;

                //计算特征
                templateArea = Cv2.ContourArea(largestContour);
                double perimeter = Cv2.ArcLength(largestContour, true);
                templateCircularity = 4 * Math.PI * templateArea / (perimeter * perimeter);

                Point2f center;
                float radius;
                Cv2.MinEnclosingCircle(largestContour, out center, out radius);
                templateRadius = radius;

                //保存边缘图用于匹配
                Mat templateEdges = new Mat();
                Cv2.Canny(templateImage, templateEdges, 50, 150);

                Console.WriteLine($"样板分析完成:");
                Console.WriteLine($"  半径: {templateRadius:F2}");
                Console.WriteLine($"  面积: {templateArea:F2}");
                Console.WriteLine($"  圆度: {templateCircularity:F3}");

            }
            gray.Dispose();
            binary.Dispose();
            return image;

        }
        //对比
        public List<CircleInfo> DatectCircles(
            Mat testImage,
            double areaTolerance = 0.3,      // 面积容差 ±30%
            double circularityTolerance = 0.1, // 圆度容差
            double radiusTolerance = 0.2)
        {
            double templateArea = 0.0;
            double templateCircularity = 0.0;
            double templateRadius = 0.0;

            List<CircleInfo> detectedCircles = new List<CircleInfo>();

            // 1. 预处理测试图像
            Mat gray = new Mat();
            if (testImage.Channels() == 3)
                Cv2.CvtColor(testImage, gray, ColorConversionCodes.BGR2GRAY);
            else
                gray = testImage.Clone();

            // 2. 边缘检测
            Mat edges = new Mat();
            Cv2.Canny(gray, edges, 50, 150);

            // 3. 查找轮廓
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(edges, out contours, out hierarchy,
                RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            // 4. 遍历轮廓，与样板特征对比
            foreach (var contour in contours)
            {
                double area = Cv2.ContourArea(contour);
                double perimeter = Cv2.ArcLength(contour, true);
                double circularity = 4 * Math.PI * area / (perimeter * perimeter);

                // 基于样板的筛选
                bool areaMatch = Math.Abs(area - templateArea) / templateArea <= areaTolerance;
                bool circularityMatch = Math.Abs(circularity - templateCircularity) <= circularityTolerance;

                if (areaMatch && circularityMatch)
                {
                    // 拟合圆
                    Point2f center;
                    float radius;
                    Cv2.MinEnclosingCircle(contour, out center, out radius);

                    // 半径检查
                    if (Math.Abs(radius - templateRadius) / templateRadius <= radiusTolerance)
                    {
                        detectedCircles.Add(new CircleInfo
                        {
                            Center = center,
                            Radius = radius,
                            Area = area,
                            Circularity = circularity,
                            MatchScore = CalculateMatchScore(area, circularity, radius)
                        });
                        Console.WriteLine($"[找到圆] 中心: ({center.X:F2}, {center.Y:F2}), " +
                        $"半径: {radius:F2}, 面积: {area:F2}, " +
                        $"圆度: {circularity:F3}, 匹配度: ");
                    }
                }
                Console.WriteLine("未找到");
            }

            gray.Dispose();
            edges.Dispose();

            return detectedCircles;


        }
        public double CalculateMatchScore(double area, double circularity, float radius)
        {
            double templateArea = 0.0;
            double templateCircularity = 0.0;
            double templateRadius = 0.0;
            // 计算综合匹配度（0-1之间，越高越匹配）
            double areaScore = 1 - Math.Abs(area - templateArea) / templateArea;
            double circScore = 1 - Math.Abs(circularity - templateCircularity);
            double radiusScore = 1 - Math.Abs(radius - templateRadius) / templateRadius;

            // 加权平均
            return areaScore * 0.3 + circScore * 0.3 + radiusScore * 0.4;
        }


        //用harris角点检测找出角点
        public Mat CornerHarrisone(Mat image)
        {
            Mat src = new Mat();
            src = image.Clone();
            Mat cornerStrength = new Mat();
            //Console.WriteLine($"二值化后图像类型: {src.Type()}, 通道数: {src.Channels()}");
            Mat prosses = new Mat();

            if (src.Channels() == 3)
            {
                Console.WriteLine("将3通道彩色图转换为灰度图");
                Cv2.CvtColor(src, prosses, ColorConversionCodes.BGR2GRAY);
            }
            if (src.Channels() == 4)
            {
                Console.WriteLine("将4通道图转换为灰度图");
                Cv2.CvtColor(src, prosses, ColorConversionCodes.BGRA2GRAY);
            }

            Cv2.CornerHarris(prosses, cornerStrength, 2, 3, 0.01);

            Mat harrisConrner = new Mat();
            Cv2.Threshold(cornerStrength, harrisConrner, 0.00001, 255, ThresholdTypes.Binary);
            Cv2.ImShow("角点检测后的二值效果图：", harrisConrner);

            return harrisConrner;
        }

        //霍夫曼画圆
        public Mat HoughCircles(Mat image)
        {
            //只收灰度图
            Mat src = new Mat();
            src = image.Clone();

            Mat gray = new Mat();

            if (src.Channels() == 3)
            {
                Console.WriteLine("将3通道彩色图转换为灰度图");
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            }
            if (src.Channels() == 4)
            {
                Console.WriteLine("将4通道图转换为灰度图");
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGRA2GRAY);
            }
            else if (src.Channels() == 1)
            {
                Console.WriteLine("图像已经是灰度图");
                gray = src.Clone();
            }


            CircleSegment[] circles = Cv2.HoughCircles(gray, HoughModes.Gradient, 1, 30, 100, 30, 5, 50);

            Mat op = new Mat();
            op = src.Clone();

            // 如果检测到圆
            if (circles.Length > 0)
            {
                Console.WriteLine($"检测到 {circles.Length} 个圆");

                foreach (CircleSegment circle in circles)
                {
                    Point2f center = circle.Center;
                    float radius = circle.Radius;

                    if (radius > 5)
                    {
                        Console.WriteLine($"找到一个圆，圆心在 ({center.X:F2}, {center.Y:F2})，半径为 {radius:F2}");

                        // 绘制圆心（红色圆点）
                        Cv2.Circle(op, (int)center.X, (int)center.Y, 3, new Scalar(0, 0, 255), -1);

                        // 绘制圆轮廓（绿色）
                        Cv2.Circle(op, (int)center.X, (int)center.Y, (int)radius, new Scalar(0, 255, 0), 2);
                    }
                    else
                    {
                        Console.WriteLine($"跳过半径过小的圆: 半径 {radius:F2} <= 5");
                    }
                }
            }
            else
            {
                Console.WriteLine("未检测到圆");
            }
            Cv2.ImShow("检测的圆", op);

            return image;
        }

        //调用Sobel函数
        public Mat SobelMat(Mat image)
        {
            Mat scr = image.Clone();
            Mat grad_x = new Mat();
            Mat grad_y = new Mat();
            Mat abs_grad_x = new Mat();
            Mat abs_grad_y = new Mat();
            Mat dst = new Mat();

            //使用Sobel
            Cv2.Sobel(scr, grad_x, MatType.CV_16S, 1, 0, 3, 1, 1, BorderTypes.Default);
            Cv2.ConvertScaleAbs(grad_x, abs_grad_x);
            //Cv2.ImShow("X方向sobel",abs_grad_x);

            Cv2.Sobel(scr, grad_y, MatType.CV_16S, 1, 0, 3, 1, 1, BorderTypes.Default);
            Cv2.ConvertScaleAbs(grad_y, abs_grad_y);
            //Cv2.ImShow("Y方向sobel", abs_grad_y);

            Cv2.AddWeighted(abs_grad_x, 0.5, abs_grad_y, 0.5, 0, dst);
            //Cv2.ImShow("整体图片",ds1);

            Mat gray = new Mat();

            // 1. 确保ds1是单通道8位图像
            if (dst.Type() != MatType.CV_8UC1)
            {
                // 如果是其他类型，转换为8位单通道
                if (dst.Type() == MatType.CV_8UC3)
                {
                    // 如果是三通道，转换为灰度图
                    Cv2.CvtColor(dst, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    // 如果是其他类型（如16位），转换为8位
                    dst.ConvertTo(gray, MatType.CV_8UC1);
                }
            }
            else
            {
                dst.CopyTo(gray);
            }

            if (dst.Type() == MatType.CV_8UC3)
            {
                // 如果是三通道，转换为灰度图
                Cv2.CvtColor(dst, gray, ColorConversionCodes.BGR2GRAY);
            }

            Mat binary = new Mat();
            Cv2.Threshold(gray, binary, 50, 255, ThresholdTypes.Binary);
            //Console.WriteLine($"二值化后图像类型: {binary2.Type()}, 通道数: {binary2.Channels()}");

            //查找轮廓
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(binary, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            //在原图上绘制轮廓
            Mat result = binary.Clone();
            Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            for (int i = 0; i < contours.Length; i++)
            {
                // 可以过滤太小的轮廓（根据面积）
                double area = Cv2.ContourArea(contours[i]);
                if (area > 100) // 只绘制面积大于100的轮廓
                {
                    // 随机颜色或固定颜色
                    Scalar color = new Scalar(0, 255, 0); // 绿色
                    Cv2.DrawContours(result, contours, i, color, 2);

                    // 或者绘制轮廓的外接矩形
                    Rect boundingRect = Cv2.BoundingRect(contours[i]);
                    //Cv2.Rectangle(result, boundingRect, new Scalar(255, 0, 0), 2); // 蓝色矩形
                }
            }

            Cv2.ImShow("用Sobel轮廓检测结果", result);

            return result;
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
            //Cv2.ImShow("[效果图]合并后的Scharr", dst);

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
            //Cv2.Threshold(gray, binary, thresholdValue, maxValue, ThresholdTypes.Otsu);
            Cv2.Threshold(gray, binary, thresholdValue, maxValue, ThresholdTypes.BinaryInv);
            //Console.WriteLine($"二值化后图像类型: {binary.Type()}, 通道数: {binary.Channels()}");

            //查找轮廓
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(binary, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            //在原图上绘制轮廓
            Mat result = binary.Clone();
            Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            for (int i = 0; i < contours.Length; i++)
            {
                // 可以过滤太小的轮廓（根据面积）
                double area = Cv2.ContourArea(contours[i]);
                if (area > 100) // 只绘制面积大于100的轮廓
                {
                    // 随机颜色或固定颜色
                    Scalar color = new Scalar(0, 255, 0); // 绿色
                    Cv2.DrawContours(result, contours, i, color, 2);

                    // 或者绘制轮廓的外接矩形
                    Rect boundingRect = Cv2.BoundingRect(contours[i]);
                    //Cv2.Rectangle(result, boundingRect, new Scalar(255, 0, 0), 2); // 蓝色矩形
                }
            }

            Cv2.ImShow("用Soharr轮廓检测结果", result);

            return result;
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

        private void button2_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.ShowDialog();
        }
    }
}
