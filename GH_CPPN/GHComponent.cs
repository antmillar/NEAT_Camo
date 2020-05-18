using Grasshopper.Kernel;
using MathNet.Numerics.LinearAlgebra;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

//custom libraries
using TopolEvo.NEAT;
using TopolEvo.Speciation;
using TopolEvo.Display;
using TopolEvo.Fitness;
using ImageInfo;

namespace GH_CPPN
{
    public class GHComponent : GH_Component
    {
        public GHComponent() : base("Camouflage Evolver", "CamoEvo", "Evolving CPPNs", "CamoEvolver", "CamoEvo")
        {
        }

        //fields
        private bool init;
        private Population pop;
        private List<string> fits;
        private List<Mesh> femModels;
        private List<Mesh> meshes = new List<Mesh>();
        private Matrix<double> coords;
        private Metrics metrics;
        private int dims = 2;
        private Dictionary<int, Matrix<double>> outputs = new Dictionary<int, Matrix<double>>();
        private Mesh voxelsTarget;
        private Mesh meshTarget = new Mesh();
        private int subdivisions = 6;
        private int popSize = 20;
        private Matrix<double> targetValues = null;
        private Stopwatch perfTimer;
        private Speciator speciator = new Speciator();
        private int count = 0;

        public override Guid ComponentGuid
        {
            // Don't copy this GUID, make a new one
            get { return new Guid("ab047315-77a4-45ef-8039-44cd6428b913"); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            //pManager.AddTextParameter("input text", "i", "string to reverse", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Generations", "Run", "Run the next N generations", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Generation Count", "gens", "Number Generations to each time", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Population Size", "popSize", "Size of the Population", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Save images", "Save", "Save images", GH_ParamAccess.item);

            //pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Prey", "mg", "grid of meshes", GH_ParamAccess.list);
            pManager.AddTextParameter("Fitnesses", "fitnesses", "output of fitnesses", GH_ParamAccess.list);
            pManager.AddTextParameter("Topologies", "topos", "topos", GH_ParamAccess.list);
            pManager.AddMeshParameter("Superimposed", "target", "target meshes", GH_ParamAccess.item);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            //defaults
            bool button = false;
            bool save = false;
            int generations = 1;
            bool targetIs2D = true;
            bool targetIs3D = false;
            bool targetIsColor = false;
            var final = new Mesh();
            subdivisions = 100;

            DA.GetData(0, ref button);
            DA.GetData(1, ref generations);
            DA.GetData(2, ref popSize);
            DA.GetData(3, ref save);

            coords = Matrix<double>.Build.Dense((int) Math.Pow(subdivisions,dims), dims );
            coords = PopulateCoords(coords, subdivisions, dims );
            metrics = Metrics.Contrast | Metrics.Luminance | Metrics.Pattern;

            //if there is a target shape, rescale to [-0.5, 0.5]
            if (targetIs3D)
            {
                var bbox = meshTarget.GetBoundingBox(true);
                var box = new Box(bbox);

                var longestDim = Math.Max(Math.Max(box.X.Length, box.Y.Length), box.Z.Length);
                meshTarget.Translate(new Vector3d(-box.Center));
                meshTarget.Scale(1.0 / longestDim);

                targetValues = Fitness.VoxelsFromMesh(subdivisions * subdivisions * subdivisions, 1, coords, meshTarget);
                var occCount = targetValues.ColumnSums().Sum();

                if (occCount == 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not generate any voxels, try a larger number of subdivisions");
            }

            //if there is a target shape, rescale to[-0.5, 0.5]
            if (targetIs2D)
            {
                Image imageTarget = Image.FromFile(@"C:\Users\antmi\Pictures\grass.jpg");
                Bitmap bmTarget = new Bitmap(imageTarget);
                targetValues = Fitness.PixelsFromImage(subdivisions * 4, bmTarget, false);
                var occCount = targetValues.ColumnSums().Sum();

                if (occCount == 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not generate any voxels, try a larger number of subdivisions");
            }

            //if there is a target shape, rescale to [-0.5, 0.5]
            if (targetIsColor)
            {
                Image imageTarget = Image.FromFile(@"C:\Users\antmi\Pictures\grass.jpg");
                Bitmap bmTarget = new Bitmap(imageTarget);
                targetValues = Fitness.PixelsFromImage(subdivisions * 4, bmTarget, true);
                var occCount = targetValues.ColumnSums().Sum();

                if (occCount == 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not generate any voxels, try a larger number of subdivisions");
            }

            //on first run, initialise population
            if (!init)
            {
                Fitness.bowModel = null;
                perfTimer = new Stopwatch();
                perfTimer.Start();

                init = true;
                pop = new Population(popSize, dims  );
                Accord.Math.Random.Generator.Seed = 0;
            }

            else if (button)
            {
                perfTimer = new Stopwatch();
                perfTimer.Start();
                
                Run(generations , subdivisions, popSize);

                meshes = GenerateMeshes(pop, outputs, subdivisions, pop.Genomes.Count);

                perfTimer.Stop();
            }

            if (save)
            {
                SaveImages(outputs, subdivisions * 4);
            }

            if (outputs.Count > 0)
            {
                final = Impose(targetValues, outputs[pop.Genomes[0].ID], 4 * subdivisions, subdivisions);
                //var temp = Fitness.Superimpose(targetValues, outputs[pop.Genomes[0].ID], 4 * subdivisions, subdivisions);
            }
            var topologies = pop.Genomes.Select(i => i.ToString()).ToList();

            //output data from GH component
            DA.SetDataList(0, meshes);
            DA.SetDataList(1, fits);
            DA.SetDataList(2, topologies);
            DA.SetData(3, final);
            //DA.SetData(4, perfTimer.Elapsed.ToString());
            //DA.SetDataList(5, femModels);
            //DA.SetData(6, pop.Genomes.Select(a => a.Fitness).Average());
        }

        private Mesh Impose(Matrix<double> habitat, Matrix<double> prey, int subdivisionsHabitat, int subdivisionsPrey)
        {

            for (int i = 0; i < subdivisionsPrey; i++)
            {
                for (int j = 0; j < subdivisionsPrey; j++)
                {
                    habitat[j + i * subdivisionsHabitat + 150 + 150 * subdivisionsHabitat, 0] = prey[j + i * subdivisionsPrey, 0];

                    if(habitat.ColumnCount == 3)
                    {
                        habitat[j + i * subdivisionsHabitat + 150 + 150 * subdivisionsHabitat, 1] = prey[j + i * subdivisionsPrey, 1];
                        habitat[j + i * subdivisionsHabitat + 150 + 150 * subdivisionsHabitat, 2] = prey[j + i * subdivisionsPrey, 2];
                    }
                }
            }

            var drawing = new Drawing();
            var mesh = drawing.Create(habitat,subdivisionsHabitat, 10.0 * 4, -10.0 * 4 - 10, 40);

            return mesh;

        }
        private void Run(int generations, int subdivisions, int popSize)
        {
            for (int i = 0; i < generations; i++)
            {
                var speciesList = speciator.GenerateSpecies(pop);
                pop.NextGen(speciesList);


                //pop.NextGeneration();

                Evaluation();
            }
        }

        private void SaveImages(Dictionary<int, Matrix<double>> outputs, int subdivisions)
        {
            var root = @"C:\Users\antmi\pictures\camo\";
            var newCoords = Matrix<double>.Build.Dense((int)Math.Pow(subdivisions, dims), dims );
            newCoords = PopulateCoords(newCoords, subdivisions, dims);

            var imoutputs = pop.Evaluate(newCoords);

            foreach (var output in imoutputs)
            {
                var bitmap = ImageAnalysis.BitmapFromPixels(output.Value, subdivisions);
                bitmap.Save(root + output.Key.ToString() + ".png");
            }
        }

        private void SaveImage(Matrix<double> output, int subdivisions, int count)
        {
            var root = @"C:\Users\antmi\pictures\video\";

            var image = Fitness.ToImposedImage(targetValues, output, 400, 100);
            image.Save(root + count.ToString() + ".png");
            
        }

        private void Evaluation()
        {
            outputs = pop.Evaluate(coords);
            
            SaveImage(outputs[pop.Genomes[0].ID], subdivisions, count++);

            fits = Fitness.Function(pop, outputs, coords, targetValues, subdivisions, metrics);

            if ((metrics & Metrics.Displacement) == Metrics.Displacement)
            {
                femModels = pop.Genomes.Select(g => FEM.MakeFrame(g.FEMModel, FEM.GetDisplacements(g.FEMModel)).Item1).ToList();
                femModels = GenerateFEMs(femModels, pop.Genomes.Count);
            }
        }

        private Mesh Draw3DTarget(Matrix<double> target, int subdivisions)
        {
            var volume = new Volume();
            Mesh mesh = volume.Create(target, subdivisions, -30, 0, 0);

            return mesh;
        }

        private Mesh Draw2DTarget(Matrix<double> target, int subdivisions)
        {
            var drawing = new Drawing();
            Mesh mesh = drawing.Create(target, subdivisions, 10.0 * 4, -10.0 * 4 - 10, 0);

            return mesh;
        }




        private List<Mesh> GenerateMeshes(Population pop, Dictionary<int, Matrix<double>> outputs, int subdivisions, int popSize)
        {
            meshes.Clear();

            //draw a grid of results
            int gridWidth = Math.Min(10, popSize);
            int gridDepth = popSize / gridWidth + 1;
            int padding = 10;
            int maxDisplay = Math.Min(50, popSize);

            var count = 0;

            for (int i = 0; i < gridDepth; i++)
            {

                for (int j = 0; j < gridWidth; j++)
                {
                    if(count < maxDisplay)
                    {
                        Mesh combinedMesh = new Mesh();

                        if (dims == 3)
                        {
                            var volume = new Volume();
                            combinedMesh = volume.Create(outputs[pop.Genomes[count].ID], subdivisions, j * (10 + padding), i * (10 + padding));
                        }
                        else
                        {
                            var drawing = new Drawing();
                            combinedMesh = drawing.Create(outputs[pop.Genomes[count].ID], subdivisions, 10.0,  j * (10 + padding), 10 +  i * (10 + padding));
                        }

                        meshes.Add(combinedMesh);
                    }

                    count++;
                }
            }

            //non grid layout, just line
            //for (int i = 0; i < pop.Genomes.Count; i++)
            //{
            //    //var drawing = new Drawing(width, -popSize / 2 * width + i * width, 0);
            //    //Mesh combinedMesh = drawing.Paint(outputs[pop.Genomes[i].ID]);
            //    //meshes.Add(combinedMesh);

            //    var volume = new Volume(width, -popSize / 2 * (width + 1) + i * (width + 1));
            //    Mesh combinedMesh = volume.Paint(outputs[pop.Genomes[i].ID]);
            //    meshes.Add(combinedMesh);
            //}
            return meshes;
        }

        private List<Mesh> GenerateFEMs(List<Mesh> femMeshes, int popSize)
        {
            meshes.Clear();

            //draw a grid of results
            int gridWidth = Math.Min(10, popSize);
            int gridDepth = popSize / gridWidth + 1;
            int padding = 10;
            int maxDisplay = Math.Min(20, popSize);
            double size = 10.0 / subdivisions;

            var count = 0;

            for (int i = 0; i < gridDepth; i++)
            {
                for (int j = 0; j < gridWidth; j++)
                {
                    if (count < maxDisplay)
                    {
                        femModels[count].Scale(10.0);
                        femModels[count].Translate(new Vector3d(5 + size /2, 5+size / 2, 5+size / 2));
                        femModels[count].Translate(new Vector3d(j * (10 + padding), i * (10 + padding), 0));
  
                    }
                    count++;
                }
            }

            return femMeshes;
        }

        private Matrix<double> PopulateCoords(Matrix<double> coords, int subdivisions, int dims)
        {
            //populate coords

            var shift = subdivisions / 2;

            if (dims == 2)
            {
                for (int i = 0; i < subdivisions; i++)
                {
                    for (int j = 0; j < subdivisions; j++)
                    {
                        //coords are in range [-0.5, 0.5]
                        coords[i * subdivisions + j, 0] = 1.0 * i / subdivisions;
                        coords[i * subdivisions + j, 1] = 1.0 * j / subdivisions;

                    }
                }
                coords -= 0.5;
            }

            else if (dims == 3)
            {
                for (int i = 0; i < subdivisions; i++)
                {
                    for (int j = 0; j < subdivisions; j++)
                    {
                        var xCoord = 1.0 * i / subdivisions - 0.5;
                        var yCoord = 1.0 * j / subdivisions - 0.5;
                        //coords are in range [-0.5, 0.5]
                        coords[i * subdivisions + j, 0] = xCoord;
                        coords[i * subdivisions + j, 1] = yCoord;
                        coords[i * subdivisions + j, 2] = Math.Sqrt(Math.Pow(xCoord, 2) + Math.Pow(yCoord, 2)); //distance from center

                    }
                }
            }

            //else if(dims == 3)
            //{
            //    for (int i = 0; i < subdivisions; i++)
            //    {
            //        for (int j = 0; j < subdivisions; j++)
            //        {
            //            for (int k = 0; k < subdivisions; k++)
            //            {
            //                //coords are in range [-0.5, 0.5]
            //                coords[i * subdivisions * subdivisions + j * subdivisions + k , 0] = 1.0 * k / subdivisions;
            //                coords[i * subdivisions * subdivisions + j * subdivisions + k , 1] = 1.0 * j / subdivisions;
            //                coords[i * subdivisions * subdivisions + j * subdivisions + k , 2] = 1.0 * i / subdivisions;
            //            }
            //        }
            //    }
               //coords -= 0.5;
            //}




            return coords;
        }
    }
}
