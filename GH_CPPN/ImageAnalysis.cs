using System;
using System.Collections.Generic;
using System.Drawing;
using Accord.Imaging.Filters;
using MathNet.Numerics.LinearAlgebra;

namespace ImageInfo
{
    public static class ImageAnalysis
    {


        /// <summary>
        /// Gets an array of pixel luminances for a bitmap
        /// </summary>
        private static double[] GetLuminanceArray(Matrix<double> grayscales, int width)
        {
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

        public static double FeatureDistance(double[] feat1, double[] feat2)
        {
            return MathNet.Numerics.Distance.Euclidean(feat1, feat2);
        }

        public static Bitmap BitmapFromPixels(Matrix<double> pixels, int subdivisions)
        {
            //USE BYTES INSTEAD??
            Bitmap bitmap;
            var arrToBitmap = new Accord.Imaging.Converters.ArrayToImage(subdivisions, subdivisions);
            arrToBitmap.Convert(pixels.ToColumnMajorArray(), out bitmap);
            //output.Save(@"C:\Users\antmi\pictures\testing.jpg");
            return bitmap;
        }

        public static double GetFeatureDistance(Matrix<double> output, Matrix<double> target, int subdivisions)
        {

            //var detector = new Accord.Imaging.FastCornersDetector()
            //{
            //    Suppress = true, // suppress non-maximum points
            //    Threshold = 100   // less leads to more corners
            //};

            //var bow = Accord.Imaging.BagOfVisualWords.Create(numberOfWords: 10);
            //var bow = Accord.Imaging.BagOfVisualWords.Create(new Accord.Imaging.FastRetinaKeypointDetector(detector), new Accord.MachineLearning.BinarySplit(10));
            var bow = Accord.Imaging.BagOfVisualWords.Create(new Accord.Imaging.HistogramsOfOrientedGradients(), numberOfWords: 10);

            var a = ImageAnalysis.BitmapFromPixels(output, subdivisions);
            var b = ImageAnalysis.BitmapFromPixels(target, subdivisions * 4);

            Bitmap[] images = new Bitmap[2] { a , b};

            bow.Learn(images);

            double[][] features = bow.Transform(images);

            var dist = ImageAnalysis.FeatureDistance(features[0], features[1]);

            return dist;
        }

        public static double GetFeatureDistances(Dictionary<int, Matrix<double>> outputs, Matrix<double> target, int subdivisions)
        {

            var bow = Accord.Imaging.BagOfVisualWords.Create(new Accord.Imaging.HistogramsOfOrientedGradients(), numberOfWords: 10);

            Bitmap[] images = new Bitmap[1 + outputs.Count];

            var targetBitmap = ImageAnalysis.BitmapFromPixels(target, subdivisions * 4);

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

            bow.Learn(images);

            double[][] features = bow.Transform(images);

            for (int i = 1; i < outputs.Count; i++)
            {
                var dist = ImageAnalysis.FeatureDistance(features[0], features[i]);

            }


            return dist;
        }

        public static void GaborFilter()
        {
            var gaborFilter = new GaborFilter();

        }
    }
}
