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
using System.Threading;
using OpenCvSharp.Features2D;
using OpenCvSharp.Flann;
using Nancy.Responses.Negotiation;

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
        public static Mat IncreaseBrightness(Mat image, double brightnessValue)
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

                //均值滤波
                Mat mean = new Mat();
                Cv2.Blur(gray, mean, new OpenCvSharp.Size(3, 3));
                //Cv2.ImShow("均值滤波", mean);

                //双边滤波
                Mat bilateral = new Mat();
                Cv2.BilateralFilter(gray, bilateral, 9, 75, 75);
                //Cv2.ImShow("双边滤波", bilateral);

                //中值滤波(组合去噪)钝化
                Mat median = new Mat();
                Cv2.MedianBlur(gray, median, 5);
                //Cv2.ImShow("中值滤波", median);

                // 高斯模糊
                Mat blurred = new Mat();
                Cv2.GaussianBlur(median, blurred, new OpenCvSharp.Size(7, 7), 2.0);
                //Cv2.ImShow("高斯滤波", blurred);

                //拉普拉斯算子 锐化
                //Console.WriteLine($"原图像图像类型: {gray.Type()}, 通道数: {gray.Channels()}");
                Mat laplacian = new Mat();
                Cv2.Laplacian(blurred, laplacian, MatType.CV_8U, 3);
                //Cv2.ImShow("拉普拉斯算子", laplacian);
                Mat res = new Mat();
                Cv2.AddWeighted(blurred, 1.0, laplacian, 1.0, 0, res);
                //Cv2.ImShow("res", res);

                Mat io = new Mat();
                Cv2.AdaptiveThreshold(blurred, io, 255,
                AdaptiveThresholdTypes.GaussianC,
                ThresholdTypes.BinaryInv,
                21, 
                3);

                Mat ip = new Mat();
                Cv2.Threshold(blurred, ip, 50, 255, ThresholdTypes.BinaryInv);

                //形态学闭运算
                Mat closed = new Mat();
                var kernelClose = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(9, 9));
                Cv2.MorphologyEx(blurred, closed, MorphTypes.Close, kernelClose, null, iterations: 2);
                //Cv2.ImShow("closed", closed);

                //形态学开运算
                Mat opening = new Mat();
                var kernelOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
                Cv2.MorphologyEx(closed, opening, MorphTypes.Open, kernelOpen, null, iterations: 1);
                //Cv2.ImShow("opening", opening);

                //形态学梯度
                Mat gradient = new Mat();
                Cv2.MorphologyEx(opening, gradient, MorphTypes.Gradient, kernelOpen, null, iterations: 1);
                //Cv2.ImShow("gradient", gradient);

                //test1 = ScharrMat(opening);
                test2 = SobelMat(opening);

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

                string imagePath = @"C:\Users\Lenovo\Desktop\work\Test3\8.bmp";
                string templatePath = @"C:\Users\Lenovo\Desktop\work\Test3\CNN\13.Png";
                string outputPath = @"D:\test\result.jpg";

                Point2f? center = DetectCircleFromMat(test2);

                if (center.HasValue)
                {
                    Console.WriteLine($"找到圆心: X={center.Value.X}, Y={center.Value.Y}");

                    Mat displayImage;
                    if (test.Channels() == 1)
                    {
                        // 灰度图转BGR彩色图
                        displayImage = new Mat();
                        Cv2.CvtColor(test, displayImage, ColorConversionCodes.GRAY2BGR);
                    }
                    else
                    {
                        displayImage = test.Clone();
                    }

                    Cv2.Circle(displayImage,
                       (int)center.Value.X, (int)center.Value.Y,
                       5,                          // 半径5像素
                       new Scalar(0, 0, 255),      // 红色 (BGR格式)
                       -1);
                    int crossSize = 15;
                    Cv2.Line(displayImage,
                             new Point((int)center.Value.X - crossSize, (int)center.Value.Y),
                             new Point((int)center.Value.X + crossSize, (int)center.Value.Y),
                             new Scalar(0, 255, 0), 2);    // 绿色横线

                    Cv2.Line(displayImage,
                             new Point((int)center.Value.X, (int)center.Value.Y - crossSize),
                             new Point((int)center.Value.X, (int)center.Value.Y + crossSize),
                             new Scalar(0, 255, 0), 2);    // 绿色竖线

                    // 在圆心旁边显示坐标文字
                    string text = $"Circle Center: ({center.Value.X:F1}, {center.Value.Y:F1})";
                    Cv2.PutText(displayImage, text,
                                new Point((int)center.Value.X + 10, (int)center.Value.Y - 10),
                                HersheyFonts.HersheySimplex, 0.6,
                                new Scalar(255, 255, 255), 2);

                    // 显示图像
                    //Cv2.ImShow("展示圆心", displayImage);
                }
                else
                {
                    Console.WriteLine("未找到圆心");
                }

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

        //找出圆心
        //分界线
        public struct Point2D
        {
            public double X { get; set; }
            public double Y { get; set; }
            public Point2D(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
        //拟合结果结构
        public struct CircleResult
        {
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public double Radius { get; set; }
            public bool Success { get; set; }
        }

        public class CircleFitter
        {
            /// <summary>
            /// 使用 Kåsa 方法 (最小二乘法) 拟合圆
            /// </summary>
            /// <param name="points">轮廓点集合 (至少需要3个点)</param>
            /// <returns>包含圆心和半径的结果</returns>
            public static CircleResult FitCircle(Point2D[] points)
            {
                if(points == null || points.Length < 3)
                {
                    return new CircleResult { Success = false };
                }
                int n  = points.Length;
                double sumX = 0, sumY = 0;
                double sumX2 = 0, sumY2 = 0;
                double sumX3 = 0, sumY3 = 0;
                double sumXY = 0, sumX2Y = 0, sumXY2 = 0;

                foreach (var p in points)
                {
                    double x = p.X;
                    double y = p.Y;
                    double x2 = x * x;
                    double y2 = y * y;


                    sumX += x;
                    sumY += y;
                    sumX2 += x2;
                    sumY2 += y2;
                    sumX3 += x * x2;
                    sumY3 += y * y2;
                    sumXY += x * y;
                    sumX2Y += x2 * y;
                    sumXY2 += x * y2;
                }

                double A11 = sumX2;
                double A12 = sumXY;
                double A13 = sumX;
                double B1 = -sumX3 - sumXY2;


                double A21 = sumXY;
                double A22 = sumY2;
                double A23 = sumY;
                double B2 = -sumX2Y - sumY3;


                double A31 = sumX;
                double A32 = sumY;
                double A33 = n;
                double B3 = -sumX2 - sumY2;

                double detA = A11 * (A22 * A33 - A23 * A32)

                        - A12 * (A21 * A33 - A23 * A31)

                        + A13 * (A21 * A32 - A22 * A31);


                if (Math.Abs(detA) < 1e-10)
                {
                    // 行列式接近0，点共线或分布不佳，无法拟合
                    return new CircleResult { Success = false };

                }

                // 计算 D, E, F

                double detD = B1 * (A22 * A33 - A23 * A32)
                            - A12 * (B2 * A33 - A23 * B3)
                            + A13 * (B2 * A32 - A22 * B3);


                double detE = A11 * (B2 * A33 - A23 * B3)
                            - B1 * (A21 * A33 - A23 * A31)
                            + A13 * (A21 * B3 - B2 * A31);


                double detF = A11 * (A22 * B3 - B2 * A32)
                            - A12 * (A21 * B3 - B2 * A31)
                            + B1 * (A21 * A32 - A22 * A31);


                double D = detD / detA;
                double E = detE / detA;
                double F = detF / detA;

                // 4. 计算圆心和半径
                double centerX = -D / 2.0;
                double centerY = -E / 2.0;

                double radiusSquared = (D * D + E * E) / 4.0 - F;

                if (radiusSquared < 0)
                {
                    // 数值误差导致半径平方为负，设为0或视为失败
                    radiusSquared = 0;
                }
                double radius = Math.Sqrt(radiusSquared);

                return new CircleResult
                {
                    CenterX = centerX,
                    CenterY = centerY,
                    Radius = radius,
                    Success = true

                };
            }
        }

        //分界线
        public Point2f? DetectCircleFromMat(Mat image)
        {
            // 1. 确保图像不为空
            if (image == null || image.Empty())
                return null;

            // 2. 如果图像不是二值图，先进行二值化
            Mat binaryImage;
            if (image.Channels() > 1)
            {
                // 彩色图转灰度
                Mat gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                binaryImage = new Mat();
                Cv2.Threshold(gray, binaryImage, 127, 255, ThresholdTypes.Binary);
                gray.Dispose();
            }
            else
            {
                // 已经是灰度图或二值图
                binaryImage = image.Clone();
                // 如果不是二值图，进行二值化
                if (!IsBinaryImage(binaryImage))
                {
                    Cv2.Threshold(binaryImage, binaryImage, 127, 255, ThresholdTypes.Binary);
                }
            }

            // 3. 查找轮廓
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(binaryImage, out contours, out hierarchy,
                RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            //在原图上绘制轮廓
            Mat result = binaryImage.Clone();
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

            //Cv2.ImShow("kkok", result);

            if (contours == null || contours.Length == 0)
            {
                binaryImage.Dispose();
                return null;
            }

            // 4. 过滤轮廓（去除太小或太大的）
            var filteredContours = FilterContours(contours, binaryImage);

            if (filteredContours.Length == 0)
            {
                binaryImage.Dispose();
                return null;
            }

            // 5. 提取所有轮廓点作为点集
            Point2f[] allPoints = ExtractAllPoints(filteredContours);

            // 6. 使用RANSAC拟合圆
            var ransac = new RansacCircleDetector();
            var circle = ransac.FitCircleRansac(allPoints, iterations: 1000, distanceThreshold: 3.0f);

            binaryImage.Dispose();

            return circle?.Center;
        }

        /// <summary>
        /// 确认圆的位置
        /// </summary>
        public Point2f? DetectCircleWithVisualization(Mat image, out Mat resultImage)
        {
            resultImage = image.Clone();

            // 转换为彩色图以便绘制
            if (resultImage.Channels() == 1)
            {
                Cv2.CvtColor(resultImage, resultImage, ColorConversionCodes.GRAY2BGR);
            }

            // 1. 确保图像不为空
            if (image == null || image.Empty())
                return null;

            // 2. 预处理：二值化
            Mat binaryImage;
            if (image.Channels() > 1)
            {
                Mat gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                binaryImage = new Mat();
                Cv2.Threshold(gray, binaryImage, 127, 255, ThresholdTypes.Binary);
                gray.Dispose();
            }
            else
            {
                binaryImage = image.Clone();
                if (!IsBinaryImage(binaryImage))
                {
                    Cv2.Threshold(binaryImage, binaryImage, 127, 255, ThresholdTypes.Binary);
                }
            }

            // 3. 查找轮廓
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(binaryImage, out contours, out hierarchy,
                RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            if (contours == null || contours.Length == 0)
            {
                binaryImage.Dispose();
                return null;
            }

            // 4. 过滤轮廓并绘制
            var filteredContours = FilterContours(contours, binaryImage);

            // 在原图上绘制所有轮廓（绿色）
            Cv2.DrawContours(resultImage, filteredContours, -1, new Scalar(0, 255, 0), 2);

            // 5. 提取所有轮廓点
            Point2f[] allPoints = ExtractAllPoints(filteredContours);

            // 6. 使用RANSAC拟合圆
            var ransac = new RansacCircleDetector();
            var circle = ransac.FitCircleRansac(allPoints, iterations: 1000, distanceThreshold: 3.0f);

            if (circle.HasValue)
            {
                // 绘制拟合的圆（红色）
                Cv2.Circle(resultImage, (int)circle.Value.Center.X, (int)circle.Value.Center.Y,
                          (int)circle.Value.Radius, new Scalar(0, 0, 255), 2);

                // 绘制圆心（蓝色）
                Cv2.Circle(resultImage, (int)circle.Value.Center.X, (int)circle.Value.Center.Y,
                          5, new Scalar(255, 0, 0), -1);

                // 在图像上显示圆心坐标
                string text = $"Center: ({circle.Value.Center.X:F1}, {circle.Value.Center.Y:F1})";
                Cv2.PutText(resultImage, text, new Point(10, 30),
                           HersheyFonts.HersheySimplex, 0.8, new Scalar(255, 255, 255), 2);
            }

            binaryImage.Dispose();

            return circle?.Center;
        }

        public class RansacCircleDetector
        {
            /// <summary>
            /// 使用RANSAC算法拟合圆，适合有噪声的情况
            /// </summary>
            public CircleSegment? FitCircleRansac(Point2f[] points, int iterations = 1000, float distanceThreshold = 5.0f)
            {
                if (points.Length < 3) return null;

                CircleSegment? bestCircle = null;
                int bestInlierCount = 0;

                Random rand = new Random();

                for (int i = 0; i < iterations; i++)
                {
                    // 随机选择3个点
                    int idx1 = rand.Next(points.Length);
                    int idx2 = rand.Next(points.Length);
                    int idx3 = rand.Next(points.Length);

                    if (idx1 == idx2 || idx2 == idx3 || idx1 == idx3)
                        continue;

                    // 计算这3个点确定的圆
                    var circle = GetCircleFromThreePoints(points[idx1], points[idx2], points[idx3]);
                    if (!circle.HasValue) continue;

                    // 计算内点数量
                    int inlierCount = 0;
                    foreach (var point in points)
                    {
                        float distance = Math.Abs(GetDistanceToCircle(point, circle.Value));
                        if (distance <= distanceThreshold)
                            inlierCount++;
                    }

                    // 更新最佳结果
                    if (inlierCount > bestInlierCount)
                    {
                        bestInlierCount = inlierCount;
                        bestCircle = (CircleSegment)circle;
                    }
                }

                // 如果有足够多的内点，用所有内点重新拟合更精确的圆
                if (bestCircle.HasValue && bestInlierCount > points.Length * 0.5)
                {
                    var inliers = new List<Point2f>();
                    foreach (var point in points)
                    {
                        if (Math.Abs(GetDistanceToCircle(point, bestCircle.Value)) <= distanceThreshold)
                            inliers.Add(point);
                    }

                    if (inliers.Count >= 3)
                    {
                        // 用所有内点重新拟合
                        Point2f refinedCenter;
                        float refinedRadius;
                        Cv2.MinEnclosingCircle(inliers, out refinedCenter, out refinedRadius);
                        return new CircleSegment(refinedCenter, refinedRadius);
                    }
                }

                return bestCircle;
            }

            private CircleSegment? GetCircleFromThreePoints(Point2f p1, Point2f p2, Point2f p3)
            {
                // 计算两线段的中垂线交点
                float ma = (p2.Y - p1.Y) / (p2.X - p1.X);
                float mb = (p3.Y - p2.Y) / (p3.X - p2.X);

                // 处理垂直线的情况
                if (Math.Abs(ma - mb) < 1e-6) return null;

                float centerX = (ma * mb * (p1.Y - p3.Y) + mb * (p1.X + p2.X) - ma * (p2.X + p3.X)) / (2 * (mb - ma));
                float centerY = -1 / ma * (centerX - (p1.X + p2.X) / 2) + (p1.Y + p2.Y) / 2;

                float radius = (float)Math.Sqrt(Math.Pow(p1.X - centerX, 2) + Math.Pow(p1.Y - centerY, 2));

                return new CircleSegment(new Point2f(centerX, centerY), radius);
            }

            private float GetDistanceToCircle(Point2f point, CircleSegment circle)
            {
                float dx = point.X - circle.Center.X;
                float dy = point.Y - circle.Center.Y;
                return (float)Math.Sqrt(dx * dx + dy * dy) - circle.Radius;
            }
        }

        public struct CircleSegment
        {
            public Point2f Center { get; }
            public float Radius { get; }

            public CircleSegment(Point2f center, float radius)
            {
                Center = center;
                Radius = radius;
            }
        }

        private Point[][] FilterContours(Point[][] contours, Mat referenceImage)
        {
            double imageArea = referenceImage.Width * referenceImage.Height;
            double minArea = imageArea * 0.001;  // 最小面积为图像面积的1%
            double maxArea = imageArea * 0.8;   // 最大面积为图像面积的80%

            var filtered = new List<Point[]>();

            foreach (var contour in contours)
            {
                double area = Cv2.ContourArea(contour);
                if (area >= minArea && area <= maxArea && contour.Length > 10)
                {
                    filtered.Add(contour);
                }
            }

            return filtered.ToArray();
        }

        /// <summary>
        /// 从轮廓中提取所有点
        /// </summary>
        private Point2f[] ExtractAllPoints(Point[][] contours)
        {
            var points = new List<Point2f>();

            foreach (var contour in contours)
            {
                foreach (var point in contour)
                {
                    points.Add(new Point2f(point.X, point.Y));
                }
            }

            return points.ToArray();
        }

        /// <summary>
        /// 判断图像是否为二值图
        /// </summary>
        private bool IsBinaryImage(Mat image)
        {
            // 简单判断：检查像素值是否只有0和255
            if (image.Channels() != 1) return false;

            double minVal, maxVal;
            Cv2.MinMaxIdx(image, out minVal, out maxVal);

            // 如果最小值和最大值分别是0和255，可能是二值图
            // 但这不是绝对的，这里简化处理
            return (minVal == 0 && maxVal == 255);
        }

        //匹配器(形状匹配)
        public class IndustrialShapeMatcher
        {
            private class MatchResult
            {
                public Point Location { get; set; }             // 匹配位置
                public double Score { get; set; }               // 匹配得分（越小越好）
                public double Scale { get; set; }               // 缩放比例
                public Rect BoundingBox { get; set; }           // 边界框
                public Point[] MatchedContour { get; set; }     // 匹配到的轮廓
            }

            /// <summary>
            /// 主检测方法
            /// </summary>
            /// <param name="imagePath">待检测图像路径</param>
            /// <param name="templatePath">模板图像路径（包含圆和钩子）</param>
            /// <param name="outputPath">结果输出路径</param>
            public static void DetectByShapeMatching(string imagePath, string templatePath, string outputPath)
            {
                Console.WriteLine("=== 工业形状匹配开始 ===");

                // 1. 读取图像
                Mat src = Cv2.ImRead(imagePath, ImreadModes.Color);
                Mat template = Cv2.ImRead(templatePath, ImreadModes.Color);

                //Mat k1 = new Mat();
                //Mat k2 = new Mat();

                //k1 = Cv2.ImRead(imagePath, ImreadModes.Color);
                //k2 = Cv2.ImRead(templatePath, ImreadModes.Color);

                //src = LoadAndResizeImage2(k1);
                //template = LoadAndResizeImage2(k2);


                if (src.Empty() || template.Empty())
                {
                    Console.WriteLine("错误：无法加载图像");
                    return;
                }

                // 2. 预处理：提取模板轮廓
                var templateContour = ExtractCleanContour(template, "模板");
                if (templateContour == null)
                {
                    Console.WriteLine("错误：无法提取模板轮廓");
                    return;
                }

                // 3. 多尺度形状匹配
                var bestMatch = FindBestMatchByShape(src, templateContour);

                // 4. 绘制结果
                Mat result = src.Clone();
                if (bestMatch != null)
                {
                    DrawMatchResult(result, bestMatch);
                    Console.WriteLine($"✓ 检测成功！位置：{bestMatch.Location}, 得分：{bestMatch.Score:F4}");
                }
                else
                {
                    Console.WriteLine("✗ 未找到匹配的目标");
                }

                // 5. 保存和显示
                Cv2.ImWrite(outputPath, result);
                Cv2.ImShow("形状匹配结果", result);
                Cv2.WaitKey(0);
                Cv2.DestroyAllWindows();
            }

            /// <summary>
            /// 从图像中提取干净轮廓（针对电磁铁干扰优化）
            /// </summary>
            private static Point[] ExtractCleanContour(Mat img, string name)
            {
                Mat gray = new Mat();
                Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);

                // 显示灰度图
                //Cv2.ImShow($"{name} - 灰度图", gray);

                // 步骤1：中值滤波去除电磁干扰
                Mat median = new Mat();
                Cv2.MedianBlur(gray, median, 5);
                //Cv2.ImShow($"{name} - 中值滤波", median);

                // 步骤2：自适应阈值（抗光照不均）
                Mat thresh = new Mat();
                Cv2.AdaptiveThreshold(median, thresh, 255,
                    AdaptiveThresholdTypes.GaussianC,
                    ThresholdTypes.BinaryInv, 25, 3);
                //Cv2.ImShow($"{name} - 自适应阈值", thresh);

                // 步骤3：形态学修复（关键步骤！）
                Mat closed = new Mat();

                // 先用大核闭运算连接断开的轮廓
                var kernelLarge = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5));
                Cv2.MorphologyEx(thresh, closed, MorphTypes.Close, kernelLarge, iterations: 2);
                //Cv2.ImShow($"{name} - 大核闭运算", closed);

                // 再用小核开运算去除噪点
                Mat opened = new Mat();
                var kernelSmall = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                Cv2.MorphologyEx(closed, opened, MorphTypes.Open, kernelSmall, iterations: 1);
                //Cv2.ImShow($"{name} - 小核开运算", opened);

                // 步骤4：提取轮廓
                Cv2.FindContours(opened, out Point[][] contours, out _,
                    RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0)
                {
                    Console.WriteLine($"{name} 未找到轮廓");
                    return null;
                }

                // 步骤5：筛选最大轮廓（通常是目标）
                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                // 步骤6：轮廓近似（减少点数，保留形状特征）
                double epsilon = 0.02 * Cv2.ArcLength(largestContour, true);
                var approxContour = Cv2.ApproxPolyDP(largestContour, epsilon, true);

                // 显示轮廓
                Mat contourImg = img.Clone();
                Cv2.DrawContours(contourImg, new[] { approxContour }, -1, Scalar.Green, 2);
                Cv2.ImShow($"{name} - 最终轮廓", contourImg);

                Console.WriteLine($"{name} 轮廓提取完成，点数：{approxContour.Length}");
                return approxContour;
            }

            /// <summary>
            /// 在目标图像中通过形状匹配找最佳位置
            /// </summary>
            private static MatchResult FindBestMatchByShape(Mat src, Point[] templateContour)
            {
                // 预处理源图像，得到二值图
                Mat gray = new Mat();
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Mat orign = new Mat();
                orign = IncreaseBrightness(gray, 100);
                Mat median = new Mat();
                Cv2.MedianBlur(orign, median, 5);
                Mat thresh = new Mat();
                Cv2.AdaptiveThreshold(median, thresh, 255,
                    AdaptiveThresholdTypes.GaussianC,
                    ThresholdTypes.BinaryInv, 25, 3);

                Cv2.ImShow("图形特征中的中值滤波", thresh);

                // 形态学修复
                Mat morph = new Mat();
                var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5));
                Cv2.MorphologyEx(thresh, morph, MorphTypes.Close, kernel, iterations: 2);

                Cv2.ImShow("源图像预处理", morph);

                // 提取所有轮廓
                Cv2.FindContours(morph, out Point[][] contours, out _,
                    RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0)
                {
                    Console.WriteLine("源图像未找到轮廓");
                    return null;
                }

                Console.WriteLine($"找到 {contours.Length} 个候选轮廓");

                // 存储所有匹配结果
                var matches = new List<MatchResult>();

                foreach (var contour in contours)
                {
                    double area = Cv2.ContourArea(contour);

                    // 过滤太小的轮廓（噪点）
                    if (area < 500) continue;

                    // 轮廓近似
                    double epsilon = 0.02 * Cv2.ArcLength(contour, true);
                    var approxContour = Cv2.ApproxPolyDP(contour, epsilon, true);

                    // 形状匹配（关键！）
                    double matchScore = Cv2.MatchShapes(templateContour, approxContour, ShapeMatchModes.I1);

                    // 计算边界框
                    var bbox = Cv2.BoundingRect(approxContour);

                    matches.Add(new MatchResult
                    {
                        Location = new Point(bbox.X + bbox.Width / 2, bbox.Y + bbox.Height / 2),
                        Score = matchScore,
                        BoundingBox = bbox,
                        MatchedContour = approxContour
                    });

                    Console.WriteLine($"  候选轮廓：面积={area:F0}, 匹配得分={matchScore:F4}");
                }

                // 筛选最佳匹配（得分越低越好）
                var bestMatches = matches.Where(m => m.Score < 0.3)  // 阈值可调，越小越严格
                                         .OrderBy(m => m.Score)
                                         .ToList();

                if (bestMatches.Count == 0)
                {
                    // 如果没找到，放宽阈值
                    bestMatches = matches.Where(m => m.Score < 0.5)
                                         .OrderBy(m => m.Score)
                                         .ToList();
                }

                return bestMatches.FirstOrDefault();
            }

            /// <summary>
            /// 绘制匹配结果
            /// </summary>
            private static void DrawMatchResult(Mat img, MatchResult match)
            {
                // 绘制边界框
                Cv2.Rectangle(img, match.BoundingBox, Scalar.Green, 3);

                // 绘制中心点
                Cv2.Circle(img, match.Location, 5, Scalar.Red, -1);

                // 绘制匹配到的轮廓（半透明）
                var overlay = img.Clone();
                Cv2.DrawContours(overlay, new[] { match.MatchedContour }, -1, Scalar.Yellow, 2);
                Cv2.AddWeighted(overlay, 0.3, img, 0.7, 0, img);

                // 添加文字标注
                string text = $"Score: {match.Score:F3}";
                Cv2.PutText(img, text,
                    new Point(match.BoundingBox.X, match.BoundingBox.Y - 10),
                    HersheyFonts.HersheySimplex, 0.6, Scalar.White, 2);
            }
        }

        //匹配器(SIFT特征点匹配)
        public class FeatureMatcher
        {
            private class MatchResult
            {
                public Point2f[] SrcPoints { get; set; }
                public Point2f[] TmplPoints { get; set; }
                public DMatch[] GoodMatches { get; set; }
                public Mat Homography { get; set; }
                public Point2f[] TemplateCorners { get; set; }
                public Point2f[] TransformedCorners { get; set; }
            }

            /// <summary>
            ///SIFT特征点匹配
            /// </summary>
            public static void DetectByFeatureMatching(string imagePath, string templatePath, string outputPath)
            {
                Console.WriteLine("=== SIFT特征点匹配开始 ===");

                // 1. 读取图像（灰度图）
                Mat src = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                Mat template = Cv2.ImRead(templatePath, ImreadModes.Grayscale);

                if (src.Empty() || template.Empty())
                {
                    Console.WriteLine("错误：无法加载图像");
                    return;
                }

                // 显示原图
                Cv2.ImShow("1. 待检测图像", src);
                Cv2.ImShow("2. 模板图像", template);

                // 2. 图像预处理（增强特征）
                Mat srcProcessed = PreprocessForFeatures(src);
                Mat tmplProcessed = PreprocessForFeatures(template);

                // 3. 创建SIFT检测器
                var sift = SIFT.Create(0, 3, 0.04, 10, 1.6); // 参数：nfeatures, nOctaveLayers, contrastThreshold, edgeThreshold, sigma

                // 4. 检测关键点和计算描述子
                KeyPoint[] keypointsSrc, keypointsTmpl;
                Mat descriptorsSrc = new Mat();
                Mat descriptorsTmpl = new Mat();

                sift.DetectAndCompute(srcProcessed, null, out keypointsSrc, descriptorsSrc);
                sift.DetectAndCompute(tmplProcessed, null, out keypointsTmpl, descriptorsTmpl);

                Console.WriteLine($"模板图像特征点：{keypointsTmpl.Length}个");
                Console.WriteLine($"待检测图像特征点：{keypointsSrc.Length}个");

                // 5. FLANN特征匹配（高效近邻搜索）
                var matches = FlannMatch(descriptorsTmpl, descriptorsSrc);

                // 6. 筛选优质匹配点
                var goodMatches = FilterGoodMatches(matches);
                Console.WriteLine($"优质匹配对：{goodMatches.Length}个");

                if (goodMatches.Length < 4)
                {
                    Console.WriteLine("错误：优质匹配点太少，无法定位");
                    return;
                }

                // 7. 计算单应性矩阵
                var matchResult = ComputeHomography(keypointsTmpl, keypointsSrc, goodMatches, template);

                if (matchResult == null)
                {
                    Console.WriteLine("错误：无法计算单应性矩阵");
                    return;
                }

                // 8. 绘制结果
                DrawFinalResult(imagePath, templatePath, matchResult, outputPath);

                Console.WriteLine("✓ 检测完成！");
            }

            /// <summary>
            /// 特征点增强预处理
            /// </summary>
            private static Mat PreprocessForFeatures(Mat gray)
            {
                // 直方图均衡化增强对比度
                Mat equalized = new Mat();
                Cv2.EqualizeHist(gray, equalized);

                // 轻微去噪但保留边缘
                Mat blurred = new Mat();
                Cv2.GaussianBlur(equalized, blurred, new Size(3, 3), 0);

                // 锐化增强边缘
                Mat sharp = new Mat();
                Mat kernel = new Mat(3, 3, MatType.CV_32F);

                kernel.Set(0, 0, 0); kernel.Set(0, 1, -1); kernel.Set(0, 2, 0);
                kernel.Set(1, 0, -1); kernel.Set(1, 1, 5); kernel.Set(1, 2, -1);
                kernel.Set(2, 0, 0); kernel.Set(2, 1, -1); kernel.Set(2, 2, 0);

                Cv2.Filter2D(blurred, sharp, -1, kernel);

                return sharp.Clone();
            }
            /// <summary>
            /// FLANN快速近邻匹配
            /// </summary>
            private static DMatch[] FlannMatch(Mat queryDescriptors, Mat trainDescriptors)
            {
                // 转换描述子类型
                if (queryDescriptors.Type() != MatType.CV_32F)
                {
                    queryDescriptors.ConvertTo(queryDescriptors, MatType.CV_32F);
                }
                if (trainDescriptors.Type() != MatType.CV_32F)
                {
                    trainDescriptors.ConvertTo(trainDescriptors, MatType.CV_32F);
                }

                // FLANN参数
                var indexParams = new KDTreeIndexParams(5);
                var searchParams = new SearchParams(50);

                var flannMatcher = new FlannBasedMatcher(indexParams, searchParams);
                var knnMatches = flannMatcher.KnnMatch(queryDescriptors, trainDescriptors, 2);

                // Lowe's 比率测试
                var goodMatches = new List<DMatch>();
                foreach (var matchPair in knnMatches)
                {
                    if (matchPair.Length >= 2)
                    {
                        var m = matchPair[0];
                        var n = matchPair[1];

                        // 如果最佳匹配距离显著小于次佳匹配
                        if (m.Distance < 0.75 * n.Distance)
                        {
                            goodMatches.Add(m);
                        }
                    }
                }

                return goodMatches.ToArray();
            }
            /// <summary>
            /// 进一步筛选优质匹配点
            /// </summary>
            private static DMatch[] FilterGoodMatches(DMatch[] matches)
            {
                if (matches.Length == 0) return matches;

                // 按距离排序
                var sorted = matches.OrderBy(m => m.Distance).ToArray();

                // 取距离最小的前30%，但至少保留10个
                int takeCount = Math.Max(10, (int)(sorted.Length * 0.3));
                return sorted.Take(takeCount).ToArray();
            }
            /// <summary>
            /// 计算单应性矩阵
            /// </summary>
            private static MatchResult ComputeHomography(KeyPoint[] keypointsTmpl, KeyPoint[] keypointsSrc,
                DMatch[] goodMatches, Mat template)
            {
                // 提取匹配点坐标
                var srcPts = new List<Point2f>();
                var dstPts = new List<Point2f>();

                foreach (var match in goodMatches)
                {
                    srcPts.Add(keypointsSrc[match.TrainIdx].Pt);
                    dstPts.Add(keypointsTmpl[match.QueryIdx].Pt);
                }

                // 将 Point2f[] 转换为 InputArray
                InputArray srcPtsArray = InputArray.Create(srcPts.ToArray());
                InputArray dstPtsArray = InputArray.Create(dstPts.ToArray());

                // 计算单应性矩阵
                var homography = Cv2.FindHomography(dstPtsArray, srcPtsArray,
                    HomographyMethods.Ransac, 5.0);

                if (homography.Empty())
                    return null;

                // 获取模板四个角
                var corners = new Point2f[]
                {
                new Point2f(0, 0),
                new Point2f(template.Width, 0),
                new Point2f(template.Width, template.Height),
                new Point2f(0, template.Height)
                };

                // 透视变换
                var transformedCorners = Cv2.PerspectiveTransform(corners, homography);

                return new MatchResult
                {
                    SrcPoints = srcPts.ToArray(),
                    TmplPoints = dstPts.ToArray(),
                    GoodMatches = goodMatches,
                    Homography = homography,
                    TemplateCorners = corners,
                    TransformedCorners = transformedCorners
                };
            }
            /// <summary>
            /// 绘制最终结果
            /// </summary>
            private static void DrawFinalResult(string imagePath, string templatePath,
                MatchResult matchResult, string outputPath)
            {
                Mat src = Cv2.ImRead(imagePath, ImreadModes.Color);
                Mat template = Cv2.ImRead(templatePath, ImreadModes.Color);
                Mat result = src.Clone();

                // 绘制模板在源图像中的位置
                for (int i = 0; i < 4; i++)
                {
                    var p1 = matchResult.TransformedCorners[i];
                    var p2 = matchResult.TransformedCorners[(i + 1) % 4];
                    Cv2.Line(result, (Point)p1, (Point)p2, Scalar.Green, 3);
                }

                // 绘制中心点
                var center = new Point2f(
                    matchResult.TransformedCorners.Average(p => p.X),
                    matchResult.TransformedCorners.Average(p => p.Y)
                );
                Cv2.Circle(result, (Point)center, 8, Scalar.Red, -1);

                // 绘制所有匹配点
                foreach (var pt in matchResult.SrcPoints)
                {
                    Cv2.Circle(result, (Point)pt, 3, Scalar.Yellow, -1);
                }

                // 创建匹配可视化图
                Mat matchVisual = new Mat();
                Cv2.DrawMatches(template, GetKeyPointsFromPoints(matchResult.TmplPoints),
                    src, GetKeyPointsFromPoints(matchResult.SrcPoints),
                    matchResult.GoodMatches, matchVisual, Scalar.Green, Scalar.Red);

                // 显示结果
                Cv2.ImShow("3. 特征点匹配可视化", matchVisual);
                Cv2.ImShow("4. 最终检测结果", result);

                // 保存结果
                Cv2.ImWrite(outputPath, result);
                Cv2.ImWrite(outputPath.Replace(".jpg", "_matches.jpg"), matchVisual);
            }

            private static KeyPoint[] GetKeyPointsFromPoints(Point2f[] points)
            {
                return points.Select(p => new KeyPoint(p, 1)).ToArray();
            }

            private static MatchResult FindCirclePosition(Mat src, Mat template)
            {
                Mat graySrc = new Mat();
                Mat grayTmpl = new Mat();
                Cv2.CvtColor(src, graySrc, ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(template, grayTmpl, ColorConversionCodes.BGR2GRAY);
                Mat descriptorsSrc = new Mat();
                Mat descriptorsTmpl = new Mat();

                var sift = SIFT.Create();
                sift.DetectAndCompute(graySrc, null, out KeyPoint[] keypointsSrc, descriptorsSrc);
                sift.DetectAndCompute(grayTmpl, null, out KeyPoint[] keypointsTmpl, descriptorsTmpl);

                var matches = FlannMatch(descriptorsTmpl, descriptorsSrc);
                var goodMatches = FilterGoodMatches(matches);

                if (goodMatches.Length < 4) return null;

                return ComputeHomography(keypointsTmpl, keypointsSrc, goodMatches, template);
            }
            private static Rect GetHookROI(Point2f center, double radius, Size imgSize)
            {
                // 钩子可能在圆的周边，扩展ROI区域
                int roiSize = (int)(radius * 1.5);
                int x = Math.Max(0, (int)center.X - roiSize / 2);
                int y = Math.Max(0, (int)center.Y - roiSize / 2);
                int width = Math.Min(roiSize, imgSize.Width - x);
                int height = Math.Min(roiSize, imgSize.Height - y);

                return new Rect(x, y, width, height);
            }
            private static Point DetectHookInROI(Mat roi)
            {
                // 这里可以添加专门的钩子检测逻辑
                // 例如：霍夫直线检测、轮廓分析等

                // 简单示例：找最突出的凸起
                Mat gray = new Mat();
                Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);
                    Mat edges = new Mat();
                Cv2.Canny(gray, edges, 50, 150);

                Cv2.FindContours(edges, out Point[][] contours, out _, 
                    RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                //if (contours.Length == 0) return Point.Empty;

                // 找最长的轮廓（可能是钩子）
                var longest = contours.OrderByDescending(c => Cv2.ArcLength(c, false)).First();

                // 返回轮廓中心
                var rect = Cv2.BoundingRect(longest);
                return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            }



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
            Cv2.Threshold(gray, binary, 50, 255, ThresholdTypes.Tozero);
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
            Cv2.Threshold(gray, binary, thresholdValue, maxValue, ThresholdTypes.Tozero);
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
        //图像尺寸处理
        public static Mat LoadAndResizeImage2(Mat image)
        {
            int targetWidth = 800;
            int targetHeight = 600;
            double ImageX, ImageY = 0.0;
            //读取图片
            Mat originalImage = new Mat();
            originalImage = image.Clone();
            //创建图像信息
            var imageInfo = new ImageInfo
            {
                OriginalWidth = originalImage.Width,
                OriginalHeight = originalImage.Height
            };


            // 计算缩放比例
            imageInfo.CalculateScale(targetWidth, targetHeight);
            ImageX = imageInfo.ScaleX;
            ImageY = imageInfo.ScaleY;

            // 缩放到统一大小
            Mat resizedImage = new Mat();
            Cv2.Resize(originalImage, resizedImage, new OpenCvSharp.Size(targetWidth, targetHeight));

            imageInfo.DisplayImage = resizedImage.Clone();

            return resizedImage;

        }
        private void button2_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.ShowDialog();
        }
    }
}
