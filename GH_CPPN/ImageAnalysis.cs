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

        public static Bitmap BitmapFromOccupancy(Matrix<double> target, int subdivisions)
        {
            //USE BYTES INSTEAD??
            Bitmap output;
            var arrToBitmap = new Accord.Imaging.Converters.ArrayToImage(subdivisions, subdivisions);
            arrToBitmap.Convert(target.ToColumnMajorArray(), out output);
            //output.Save(@"C:\Users\antmi\pictures\testing.jpg");
            return output;
        }

        public static double GetFeatureDistance(Matrix<double> output, Matrix<double> target, int subdivisions)
        {

            var detector = new Accord.Imaging.FastCornersDetector()
            {
                Suppress = true, // suppress non-maximum points
                Threshold = 100   // less leads to more corners
            };

            //var bow = Accord.Imaging.BagOfVisualWords.Create(numberOfWords: 10);
            //var bow = Accord.Imaging.BagOfVisualWords.Create(new Accord.Imaging.FastRetinaKeypointDetector(detector), new Accord.MachineLearning.BinarySplit(10));
            var bow = Accord.Imaging.BagOfVisualWords.Create(new Accord.Imaging.HistogramsOfOrientedGradients(), numberOfWords: 10);

            var a = ImageAnalysis.BitmapFromOccupancy(output, subdivisions);
            var b = ImageAnalysis.BitmapFromOccupancy(target, subdivisions * 2);

            Bitmap[] images = new Bitmap[2] { a , b};

            bow.Learn(images);
            var test = bow.Statistics.TotalNumberOfDescriptors;
            double[][] features = bow.Transform(images);

            var dist = ImageAnalysis.FeatureDistance(features[0], features[1]);

            return dist;
        }

        public static void GaborFilter()
        {
            var gaborFilter = new GaborFilter();

        }
    }
}
