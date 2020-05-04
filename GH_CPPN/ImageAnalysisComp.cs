using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using MathNet.Numerics.LinearAlgebra;
using Rhino.Geometry;
using TopolEvo.Display;
using TopolEvo.Fitness;
using Accord.Imaging.Filters;
using Accord.DataSets;
using System.Drawing.Imaging;

namespace GH_CPPN
{
    public class ImageAnalysisComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ImageAnalysisComp()
          : base("ImageAnalysisComp", "Nickname",
              "Description",
              "TopologyEvolver", "imageAnalysis")
        {
        }

        public Matrix<double> coords;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("imagePath", "imagePath", "imagePath", GH_ParamAccess.item);
            pManager.AddIntegerParameter("subdivs", "subdivs", "subdivs", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddNumberParameter("mean", "mean", "mean", GH_ParamAccess.item);
            pManager.AddNumberParameter("std", "std", "std", GH_ParamAccess.item);
            pManager.AddMeshParameter("image", "image", "image", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // First, we need to retrieve all data from the input parameters.
            // We'll start by declaring variables and assigning them starting values.

            string path = "";
            int subdivs = 0;
            DA.GetData(0, ref path);
            DA.GetData(1, ref subdivs);

            Image imageTarget = Image.FromFile(path);
            Bitmap bmTarget = new Bitmap(imageTarget, 20, 20);
            coords = Matrix<double>.Build.Dense((int)Math.Pow(subdivs, 2), 2);
            coords = PopulateCoords(subdivs, 2);

            Bitmap a = Accord.Imaging.Image.Clone(new Bitmap(@"C: \Users\antmi\pictures\zebra.jpg"));
            //Accord.Imaging.Image.SetGrayscalePalette(a);


            var gaborFilter = new GaborFilter();
            var filtered = gaborFilter.Apply(a);

            var occupancyTarget = Fitness.OccupancyFromImage(subdivs, coords, filtered);
            var backgroundImage = GenerateImageTarget(occupancyTarget, subdivs);

            var mean = GetMeanBrightness(bmTarget);
            var std = GetContrast(bmTarget);
            // Finally assign the spiral to the output parameter.
            DA.SetData(0, mean);
            DA.SetData(1, std);
            DA.SetData(2, backgroundImage);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        //faster approach here : https://stackoverflow.com/questions/1068373/how-to-calculate-the-average-rgb-color-values-of-a-bitmap
        //could use local approach too
        private double[] GetBrightnessArray(Bitmap bm)
        {
            var width = bm.Width;
            var height = bm.Height;
            var values = new double[width * height];
            var colors = new Color[width * height];

            //need to resolve issue with isempty colors
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var color = bm.GetPixel(i, j);

                    colors[j * i + j] = color;
                    values[j * i + j] = bm.GetPixel(i, j).GetBrightness();
                }
            }

            return values;
        }

        //contrast is just the standard deviation of the brightness
        private double GetMeanBrightness(Bitmap bm)
        {
            var brights = GetBrightnessArray(bm);

            var contrast = MathNet.Numerics.Statistics.Statistics.Mean(brights);

            return contrast;
        }

        //contrast is just the standard deviation of the brightness
        private double GetContrast(Bitmap bm)
        {
            var brights = GetBrightnessArray(bm);

            var contrast = MathNet.Numerics.Statistics.Statistics.StandardDeviation(brights);

            return contrast;
        }

        private Mesh GenerateImageTarget(Matrix<double> target, int subdivisions)
        {
            var drawing = new Drawing();
            Mesh mesh = drawing.Create(target, subdivisions, -30, 0);

            return mesh;
        }
        private Matrix<double> PopulateCoords(int subdivisions, int dims)
        {
            //populate coords

            var shift = subdivisions / 2;

            if (dims == 2)
            {
                for (int i = -shift; i < shift; i++)
                {
                    for (int j = -shift; j < shift; j++)
                    {
                        //coords are in range [-0.5, 0.5]
                        coords[(i + shift) * subdivisions + j + shift, 0] = 1.0 * i / subdivisions;
                        coords[(i + shift) * subdivisions + j + shift, 1] = 1.0 * j / subdivisions;
                    }
                }
            }

            else if (dims == 3)
            {
                for (int i = 0; i < subdivisions; i++)
                {
                    for (int j = 0; j < subdivisions; j++)
                    {
                        for (int k = 0; k < subdivisions; k++)
                        {
                            //coords are in range [-0.5, 0.5]
                            coords[i * subdivisions * subdivisions + j * subdivisions + k, 0] = 1.0 * k / subdivisions;
                            coords[i * subdivisions * subdivisions + j * subdivisions + k, 1] = 1.0 * j / subdivisions;
                            coords[i * subdivisions * subdivisions + j * subdivisions + k, 2] = 1.0 * i / subdivisions;
                        }
                    }
                }

                coords -= 0.5;
            }

            return coords;
        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("dcf26c87-2f32-4667-bf0d-6367124e5633"); }
        }


    }
}