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
using TopolEvo.NEAT;

namespace ImageInfo
{
    public static class ImageAnalysis
    {
        //would be better to move these static variables into another class, currently they're not reinitialised unless app is restarted
        public static Bitmap targetBitmap = null;
        public static Bitmap targetBitmapScaled = null;
        public static double scaledFeatureCount;

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

                    if(grayscales.ColumnCount == 3)
                    {

                        var rgb = new RGB((byte)(grayscales[i, 0] * 255), (byte)(grayscales[i, 1] * 255), (byte)(grayscales[i, 2] * 255));
                        var lum = HSL.FromRGB(rgb).Luminance;
                        values[h * i + j] = lum;
                    }

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
            return MathNet.Numerics.Distance.Euclidean(targetFeat, feat);
        }

        internal static double GetHue(Matrix<double> values, int subdivisions)
        {
            var hueCounter = 0;
            for (int i = 0; i < values.RowCount; i++)
            {
                var rgb = new RGB((byte) (values[i, 0] * 255), (byte) (values[i, 1] * 255), (byte) (values[i, 2] * 255));
                var hue = HSL.FromRGB(rgb).Hue;
                hueCounter += hue;
            }

            var meanHue = hueCounter / values.RowCount;
            return meanHue;
        }

        internal static double GetHueDist(Matrix<double> values, Matrix<double> targets, int subdivisions)
        {
            var hueCounter = 0;
            var valDist = new double[10];
            var tarDist = new double[10];

            var sizeRatio = targets.RowCount / values.RowCount; 

            for (int i = 0; i < values.RowCount; i++)
            {
                var rgb = new RGB((byte)(values[i, 0] * 255), (byte)(values[i, 1] * 255), (byte)(values[i, 2] * 255));
                var hue = HSL.FromRGB(rgb).Hue;

                int bucket = (int) hue / 36;
                if (bucket > 9) bucket = 9;

                valDist[bucket]++;
               // hueCounter += hue;
            }

            for (int i = 0; i < targets.RowCount; i++)
            {
                var rgb = new RGB((byte)(targets[i, 0] * 255), (byte)(targets[i, 1] * 255), (byte)(targets[i, 2] * 255));
                var hue = HSL.FromRGB(rgb).Hue;

                int bucket = (int)hue / 36;
                if (bucket > 9) bucket = 9;

                tarDist[bucket]++;
                //hueCounter += hue;
            }

            valDist = valDist.Select(x => x / (subdivisions * subdivisions)).ToArray();
            tarDist = tarDist.Select(x => x / (sizeRatio * (subdivisions * subdivisions))).ToArray() ;

            var distDist = FeatureDistance(valDist, tarDist) ;
            return distDist;
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


        internal static void SaveImages(Dictionary<int, Matrix<double>> outputs, int subdivisions)
        {
            var root = @"C:\Users\antmi\pictures\camo\";

            foreach (var output in outputs)
            {
                var bitmap = ImageAnalysis.BitmapFromPixels(output.Value, subdivisions);
                bitmap.Save(root + output.Key.ToString()  + ".png");
            }
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

            //Color[] colors = new Color[pixels.RowCount];

            //for (int i = 0; i < pixels.RowCount; i++)
            //{
            //    if (pixels.ColumnCount == 3)
            //    {
            //        colors[i] = Color.FromArgb((int)(pixels[i, 0] * 255.0), (int)(pixels[i, 1] * 255.0), (int)(pixels[i, 2] * 255.0));
            //    }
            //    else
            //    {
            //        colors[i] = Color.FromArgb((int)(pixels[i, 0] * 255.0), (int)(pixels[i, 0] * 255.0), (int)(pixels[i, 0] * 255.0));
            //    }
            //}
            var colors = pixels.ToColumnMajorArray();

            var arrToBitmap = new Accord.Imaging.Converters.ArrayToImage(subdivisions, subdivisions);
            arrToBitmap.Convert(colors, out bitmap);
            //bitmap.Save(@"C:\Users\antmi\pictures\testing.png");
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
            //bitmap.Save(@"C:\Users\antmi\pictures\testing.png");
            return bitmap;
        }


        /// <summary>
        /// Initialise a Visual Bag of Words Model
        /// </summary>
        /// <returns></returns>
        public static BagOfVisualWords<Accord.IFeatureDescriptor<double[]>, double[], Accord.MachineLearning.KMeans, Accord.Imaging.FastRetinaKeypointDetector> CreateBowModel()
        {
            if (ImageAnalysis.targetBitmap == null)
            {
                System.Drawing.Image imageTarget = System.Drawing.Image.FromFile(@"C:\Users\antmi\Pictures\grass.jpg");
                Size size = new Size(200, 200);
                ImageAnalysis.targetBitmap = new Bitmap(imageTarget, size);
            }


            var bow = BagOfVisualWords.Create(new FastRetinaKeypointDetector(), numberOfWords: 10);
            Bitmap[] images = new Bitmap[1] { ImageAnalysis.targetBitmap };

            bow.Learn(images);


            if (ImageAnalysis.targetBitmapScaled == null)
            {
                System.Drawing.Image imageTarget = System.Drawing.Image.FromFile(@"C:\Users\antmi\Pictures\grass.jpg");
                Size size = new Size(100, 100);
                ImageAnalysis.targetBitmapScaled = new Bitmap(imageTarget, size);

                ImageAnalysis.scaledFeatureCount = bow.Transform(new Bitmap[1] { ImageAnalysis.targetBitmapScaled })[0].Sum();

            }
            return bow;
        }

        /// <summary>
        /// Gets the feature distance between the habitat and prey image when passed through the BoVW Model
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, double> GetFeatureDistances(Dictionary<int, Matrix<double>> outputs, int subdivisions, BagOfVisualWords<Accord.IFeatureDescriptor<double[]>, double[], Accord.MachineLearning.KMeans, Accord.Imaging.FastRetinaKeypointDetector> bowModel)
        {
            var bow = bowModel;

            Bitmap[] images = new Bitmap[1 + outputs.Count];

            images[0] = ImageAnalysis.targetBitmap;

            var count = 1;

            var mappings = new Dictionary<int, int>();

            //convert each of the outputs into a bitmap
            foreach (var key in outputs.Keys)
            {
                var outputBitmap = ImageAnalysis.BitmapFromPixels(outputs[key], subdivisions);
                images[count] = outputBitmap;
                mappings[count] = key;
                count++;
            }

            //extract features for each image
            double[][] features = bow.Transform(images);

            var rescaler = features[0].Sum() / ImageAnalysis.scaledFeatureCount;
            //rescaler = 1.0;

            var dists = new Dictionary<int, double>();

            for (int i = 1; i < outputs.Count + 1; i++)
            {
                var dist = ImageAnalysis.FeatureDistance(features[0].Select(x => x / rescaler).ToArray(), features[i]);
                dists[mappings[i]] = dist;
            }

            return dists;
        }

        /// <summary>
        /// Pass a vertical and horizontal gabor filter over the superimposed images
        /// </summary>
        /// <param name="combinedPixels"></param>
        /// <returns></returns>
        public static double GaborFilter(Matrix<double> combinedPixels)
        {
            //convert to grayscale
            var bm = BitmapFromPixels(combinedPixels, 200);
            //bm = ToGrayscale(bm);

            var gaborFilter = new GaborFilter();

            //get the vertical gabor edges
            gaborFilter.Theta = 0.0;
            var vertBitmap = gaborFilter.Apply(bm);
            var pixels = Array1DFromBitmap(vertBitmap).Select(x => x / 255.0).ToArray(); 
            var vert = GetVerticalEdges(pixels);

            //get the horizontal gabor edges
            gaborFilter.Theta = Math.PI / 2;
            var horizBitmap = gaborFilter.Apply(bm);
            pixels = Array1DFromBitmap(horizBitmap).Select(x => x / 255.0).ToArray(); ;
            var horiz = GetHorizontalEdges(pixels);

            var sum = vert.Sum() + horiz.Sum();// + left.Sum();

            return sum;
        }

        //https://stackoverflow.com/questions/12168654/image-processing-with-lockbits-alternative-to-getpixel
        public static byte[] Array1DFromBitmap(Bitmap bmp)
        {
            if (bmp == null) throw new NullReferenceException("Bitmap is null");

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            IntPtr ptr = data.Scan0;

            //declare an array to hold the bytes of the bitmap
            int numBytes = data.Stride * bmp.Height;
            byte[] bytes = new byte[numBytes];

            //copy the RGB values into the array
            System.Runtime.InteropServices.Marshal.Copy(ptr, bytes, 0, numBytes);

            bmp.UnlockBits(data);

            return bytes;
        }

        //get horizontal edge pixels from Gabor Filters
        //ideally wouldn't be hard coded for a 100 pixel image
        public static List<double> GetHorizontalEdges(double[] pixels)
        {
            
            var leftEdge = new List<double>();
            int start = 200 * 50 + 50 + 12;
            var mean = pixels.Average();

            for (int i = 0; i < 100; i++)
            {
                leftEdge.Add(Math.Abs(mean - pixels[start + i * 200]));
                leftEdge.Add(Math.Abs(mean - pixels[start + i * 200 + 100]));
            }

            return leftEdge;
        }

        //get vertical edge pixels from Gabor Filters
        //ideally wouldn't be hard coded for a 100 pixel image
        public static List<double> GetVerticalEdges(double[] pixels)
        {
            var topEdge = new List<double>();
            var start = 200 * 50 + 12 * 200 + 50 - 12;
            var mean = pixels.Average();

            for (int i = 0; i < 100; i++)
            {
                topEdge.Add(Math.Abs(mean - pixels[start + i ]));

                topEdge.Add(Math.Abs(mean - pixels[start + i + 200 * 100]));
            }

            return topEdge;
        }

        //https://stackoverflow.com/questions/2265910/convert-an-image-to-grayscale
        public static Bitmap ToGrayscale(Bitmap bmp)
        {
            var result = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);

            BitmapData data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

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
