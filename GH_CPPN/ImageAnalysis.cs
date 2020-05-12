using System;
using System.Collections.Generic;
using System.Drawing;
using Accord.Imaging.Filters;
using Accord.Imaging;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using Accord.Math;
using GH_CPPN;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TopolEvo.Fitness;

namespace ImageInfo
{
    public static class ImageAnalysis
    {


        /// <summary>
        /// Gets an array of pixel luminances for a bitmap
        /// </summary>
        private static double[] GetLuminanceArray(Matrix<double> grayscales, int width)
        {

            //WHY DO I NEED WIDTH HERE?
            var w = width;
            var h = width;
            var values = new double[w * h];

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    values[h * i + j] = grayscales[h * i + j, 0];
                }
            }

            return values;
        }

        /// <summary>
        /// Gets the Mean Luminance of a bitmap. (Luminance is referred to as Brightness in System.Drawing)
        /// </summary>
        public static double GetMeanLuminance(Matrix<double> grayscales, int width)
        {
            var brights = GetLuminanceArray(grayscales, width);

            var contrast = MathNet.Numerics.Statistics.Statistics.Mean(brights);

            return contrast;
        }

        /// <summary>
        /// Gets the Contrast of an Image. (In this case this is the variance approach to calculating contrast)
        /// </summary>
        public static double GetContrast(Matrix<double> grayscales, int width)
        {
            var brights = GetLuminanceArray(grayscales, width);

            var contrast = MathNet.Numerics.Statistics.Statistics.StandardDeviation(brights);

            return contrast;
        }

        public static double FeatureDistance(double[] targetFeat, double[] feat)
        {
            //var correctCount = targetFeat.Select((v, i) => new { v, i }).Where(x => x.v > 0).Select(x => feat[x.i]).Sum();
            return MathNet.Numerics.Distance.Euclidean(targetFeat, feat);
            //return correctCount;
        }

        internal static double GetHue(Matrix<double> outputValues, int subdivisions)
        {
            var hueCounter = 0;
            for (int i = 0; i < outputValues.RowCount; i++)
            {
                var rgb = new RGB((byte) (outputValues[i, 0] * 255), (byte) (outputValues[i, 1] * 255), (byte) (outputValues[i, 2] * 255));
                var hue = HSL.FromRGB(rgb).Hue;
                hueCounter += hue;
            }

            var meanHue = hueCounter / outputValues.RowCount;
            return meanHue;
        }

        internal static double GetColDiff(Matrix<double> outputValues, Matrix<double> outputTargets, int subdivisions)
        {
            var redCount = 0.0;
            var greenCount = 0.0;
            var blueCount = 0.0;

            for (int i = 0; i < outputValues.RowCount; i++)
            {

                redCount += Math.Abs( (outputValues[i, 0] * 255) - (outputTargets[i, 0] * 255));
                greenCount += Math.Abs((outputValues[i, 1] * 255) - (outputTargets[i, 1] * 255));
                blueCount += Math.Abs((outputValues[i, 2] * 255) - (outputTargets[i, 2] * 255));

            }

            var meanColDiff = (redCount + greenCount + blueCount) / outputValues.RowCount;

            return meanColDiff;
        }

        internal static double GetColMean(Matrix<double> outputValues, int index)
        {
            var count = 0.0;

            for (int i = 0; i < outputValues.RowCount; i++)
            {
                count += (outputValues[i, index] * 255);
            }

            var meanCol = count / outputValues.RowCount;

            return meanCol;
        }

        internal static double GetColVar(Matrix<double> outputValues, int index)
        {
            List<double> cols = new List<double>();

            for (int i = 0; i < outputValues.RowCount; i++)
            {
                cols.Add( (outputValues[i, index] * 255));
            }

            var meanVar = MathNet.Numerics.Statistics.Statistics.StandardDeviation(cols);

            return meanVar;
        }
        internal static double GetHueVar(Matrix<double> outputValues, int subdivisions)
        {
            List<double> hues = new List<double>();
            for (int i = 0; i < outputValues.RowCount; i++)
            {
                var rgb = new RGB((byte)(outputValues[i, 0] * 255), (byte)(outputValues[i, 1] * 255), (byte)(outputValues[i, 2] * 255));
                var hue = HSL.FromRGB(rgb).Hue;
                hues.Add(hue);
            }

            var variance = MathNet.Numerics.Statistics.Statistics.StandardDeviation(hues);
            return variance;
        }

        internal static double GetSat(Matrix<double> outputValues, int subdivisions)
        {
            var satCounter = 0.0;
            for (int i = 0; i < outputValues.RowCount; i++)
            {
                var rgb = new RGB((byte)(outputValues[i, 0] * 255), (byte)(outputValues[i, 1] * 255), (byte)(outputValues[i, 2] * 255));
                var sat = HSL.FromRGB(rgb).Saturation;
                satCounter += sat;
            }

            var meanSat = satCounter / outputValues.RowCount;
            return meanSat;
        }


        internal static double GetSatVar(Matrix<double> outputValues, int subdivisions)
        {
            List<double> sats = new List<double>();
            for (int i = 0; i < outputValues.RowCount; i++)
            {
                var rgb = new RGB((byte)(outputValues[i, 0] * 255), (byte)(outputValues[i, 1] * 255), (byte)(outputValues[i, 2] * 255));
                var sat = HSL.FromRGB(rgb).Saturation;
                sats.Add(sat);
            }

            var variance = MathNet.Numerics.Statistics.Statistics.StandardDeviation(sats);
            return variance;
        }

        public static Bitmap BitmapFromPixels(Matrix<double> pixels, int subdivisions)
        {
            //USE BYTES INSTEAD??
            Bitmap bitmap;

            Color[] colors = new Color[pixels.RowCount];

            for (int i = 0; i < pixels.RowCount; i++)
            {
                if (pixels.ColumnCount == 3)
                {
                    colors[i] = Color.FromArgb((int)(pixels[i, 0] * 255.0), (int)(pixels[i, 1] * 255.0), (int)(pixels[i, 2] * 255.0));
                }
                else
                {
                    colors[i] = Color.FromArgb((int)(pixels[i, 0] * 255.0), (int)(pixels[i, 0] * 255.0), (int)(pixels[i, 0] * 255.0));
                }
            }

            var arrToBitmap = new Accord.Imaging.Converters.ArrayToImage(subdivisions, subdivisions);
            arrToBitmap.Convert(colors, out bitmap);
            //bitmap.Save(@"C:\Users\antmi\pictures\testing.jpg");
            return bitmap;
        }

        public static Bitmap BitmapFromHSL(Matrix<double> hsls, int subdivisions)
        {
            //USE BYTES INSTEAD??
            Bitmap bitmap;
            Color[] colors = new Color[hsls.RowCount];

            for (int i = 0; i < hsls.RowCount; i++)
            {
                colors[i] = ColorScale.ColorFromHSL(hsls[i, 0], hsls[i, 1], hsls[i, 2]);
            }

            var arrToBitmap = new Accord.Imaging.Converters.ArrayToImage(subdivisions, subdivisions);
            arrToBitmap.Convert(colors, out bitmap);
            //bitmap.Save(@"C:\Users\antmi\pictures\testing.jpg");
            return bitmap;
        }

        public static double GetFeatureDistance(Matrix<double> output, Matrix<double> target, int subdivisions)
        {

            //var detector = new Accord.Imaging.FastCornersDetector()
            //{
            //    Suppress = true, // suppress non-maximum points
            //    Threshold = 100   // less leads to more corners
            //};

            var bow = Accord.Imaging.BagOfVisualWords.Create(numberOfWords: 10);
            //var bow = Accord.Imaging.BagOfVisualWords.Create(new Accord.Imaging.FastRetinaKeypointDetector(detector), new Accord.MachineLearning.BinarySplit(10));
            //var bow = Accord.Imaging.BagOfVisualWords.Create(new Accord.Imaging.HistogramsOfOrientedGradients(), numberOfWords: 10);

            var a = ImageAnalysis.BitmapFromPixels(output, subdivisions);
            var targetBitmap = ImageAnalysis.BitmapFromPixels(target, subdivisions * 4);

            Bitmap[] images = new Bitmap[2] { targetBitmap, a};

            bow.Learn(images);

            double[][] features = bow.Transform(images);

            var dist = ImageAnalysis.FeatureDistance(features[0], features[1]);

            return dist;
        }

        public static BagOfVisualWords<Accord.IFeatureDescriptor<double[]>, double[], Accord.MachineLearning.KMeans, Accord.Imaging.FastRetinaKeypointDetector> CreateBowModel(Bitmap target)
        {
            var bow = Accord.Imaging.BagOfVisualWords.Create(new FastRetinaKeypointDetector() , numberOfWords: 10);
            //var bow = BagOfVisualWords.Create(new HistogramsOfOrientedGradients(), numberOfWords: 10);

            Bitmap[] images = new Bitmap[1];

            var targetBitmap = target;

            images[0] = targetBitmap;
            var count = 1;

            //var mappings = new Dictionary<int, int>();

            //foreach (var key in outputs.Keys)
            //{
            //    var outputBitmap = ImageAnalysis.BitmapFromPixels(outputs[key], subdivisions);
            //    images[count] = outputBitmap;
            //    mappings[count] = key;
            //    count++;
            //}

            bow.Learn(images);

            return bow;
        }



        public static Dictionary<int, double> GetFeatureDistances(Dictionary<int, Matrix<double>> outputs, Bitmap target, int subdivisions, BagOfVisualWords<Accord.IFeatureDescriptor<double[]>, double[], Accord.MachineLearning.KMeans, Accord.Imaging.FastRetinaKeypointDetector> bowModel)
        {
            var bow = bowModel;

            Bitmap[] images = new Bitmap[1 + outputs.Count];


            var targetBitmap = target;
            var t2 = new Bitmap(target, new Size(subdivisions, subdivisions));
            images[0] = targetBitmap;
            var count = 1;

            var mappings = new Dictionary<int, int>();

            foreach(var key in outputs.Keys)
            {
                var outputBitmap = ImageAnalysis.BitmapFromPixels(outputs[key], subdivisions);
                images[count] = outputBitmap;
                mappings[count] = key;
                count++;
            }

            double[][] testfeatures = bow.Transform(new Bitmap[1] { t2 });
             
            double[][] features = bow.Transform(images);

            var rescaler = features[0].Sum() / testfeatures[0].Sum();
            //rescaler = 1.0;

            var dists = new Dictionary<int, double>();

            for (int i = 1; i < outputs.Count + 1; i++)
            {
                var dist = ImageAnalysis.FeatureDistance(features[0].Select(x => x / rescaler).ToArray(), features[i]);
                dists[mappings[i]] = dist;
            }

            var gabor = new GaborFilter();
            gabor.Theta = Math.PI / 2.0;
            var fast = new FastRetinaKeypointDetector();
            var surf = new SpeededUpRobustFeaturesDetector();

            // Use it to extract the SURF point descriptors from the Lena image:
            List<SpeededUpRobustFeaturePoint> descriptors = surf.ProcessImage(targetBitmap);

            // We can obtain the actual double[] descriptors using
            //double[][] test = descriptors.Apply(d => d.Descriptor);
            var marker = new FeaturesMarker(descriptors);
            var labelledImage = marker.Apply(targetBitmap);

            //SobelEdgeDetector filter = new SobelEdgeDetector();

            //labelledImage = filter.Apply(labelledImage);
            //labelledImage.Save(@"C:\Users\antmi\Pictures\labelledBlobs.jpg");


            return dists;
        }

        public static double GetGaborDistance(Matrix<double> output, Matrix<double> target, int subdivisions)
        {

            var valueBitmap = ImageAnalysis.BitmapFromPixels(output, subdivisions);
            var targetBitmap = ImageAnalysis.BitmapFromPixels(target, subdivisions);



            return 2.0;
        }


        public static List<double> GaborFilter(Matrix<double> output)
        {

            var bm = ImageAnalysis.BitmapFromPixels(output, 400);
            bm = ToGrayscale(bm);
            var gaborFilter = new GaborFilter();
            gaborFilter.Theta = Math.PI / 2;

            var labelledImage = gaborFilter.Apply(bm);

            var pixels = Fitness.RGBFromImage(400, labelledImage, false);
            var left = GetLeftGabor(pixels);

            labelledImage.Save(@"C:\Users\antmi\Pictures\labelledBlobs.jpg");

            return left;
        }

        public static List<double> GetLeftGabor(Matrix<double> pixels)
        {
            var leftEdge = new List<double>();
            var start = 400 * 150 + 150 + 12 + 2;
            var mean = pixels.ToRowMajorArray().Average();

            for (int i = 0; i < 100; i++)
            {
                leftEdge.Add(Math.Abs(mean - pixels[start + i * 400, 0]));
            }

            return leftEdge;
        }

        public static List<double> GetTopGabor(Matrix<double> pixels)
        {
            var topEdge = new List<double>();
            var start = 400 * 150 + 12 * 400;

            for (int i = 0; i < 100; i++)
            {
                topEdge.Add(pixels[start + i * 400, 0]);
            }

            return topEdge;
        }
        public static Bitmap ToGrayscale(Bitmap bmp)
        {
            var result = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);

            System.Drawing.Imaging.BitmapData data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            // Copy the bytes from the image into a byte array
            byte[] bytes = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    var rgb = (byte)((c.R + c.G + c.B) / 3);

                    bytes[x * data.Stride + y] = rgb;
                }
            }

            // Copy the bytes from the byte array into the image
            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);

            result.UnlockBits(data);

            return result;
        }
    }
}
