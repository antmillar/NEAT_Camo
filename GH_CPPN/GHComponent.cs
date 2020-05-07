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
        public GHComponent() : base("Topology Evolver", "TopolEvo", "Evolving CPPNs", "TopologyEvolver", "TopoEvo")
        {
        }

        //fields
        private bool init = false;
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

        public override Guid ComponentGuid
        {
            // Don't copy this GUID, make a new one
            get { return new Guid("ab047315-77a4-45ef-8039-44cd6428b913"); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            //pManager.AddTextParameter("input text", "i", "string to reverse", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Generations", "Run", "Run the next N generations", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Generation Count", "gens", "Number of Generations to each time", GH_ParamAccess.item);
            pManager.AddMeshParameter("Target Output", "target mesh", "target mesh", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Subdivisions", "subdivisions", "Number of Subdivisions", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Population Size", "popSize", "Size of the Population", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Dimension", "dims", "number of dimensions", GH_ParamAccess.item);

            //pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh grid", "mg", "grid of meshes", GH_ParamAccess.list);
            pManager.AddTextParameter("fitnesses", "fitnesses", "output of fitnesses", GH_ParamAccess.list);
            pManager.AddTextParameter("topos", "topos", "topos", GH_ParamAccess.list);
            pManager.AddMeshParameter("target mesh", "target", "target meshes", GH_ParamAccess.item);
            pManager.AddTextParameter("perfTimer", "perfTimer", "perfTimer", GH_ParamAccess.item);
            pManager.AddMeshParameter("fems", "fems", "fems", GH_ParamAccess.list);
            pManager.AddNumberParameter("av fit", "av fit", "av fit", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            perfTimer = new Stopwatch();
            perfTimer.Start();

            //defaults
            bool button = false;
            int generations = 1;
            bool targetIsShape = false;
            bool targetIsImage = true;

            DA.GetData(0, ref button);
            DA.GetData(1, ref generations);
            if (DA.GetData(2, ref meshTarget)) { targetIsShape = true; }
            DA.GetData(3, ref subdivisions);
            DA.GetData(4, ref popSize);
            DA.GetData(5, ref dims);

            coords = Matrix<double>.Build.Dense((int) Math.Pow(subdivisions,dims), dims);
            coords = PopulateCoords(subdivisions, dims);
            metrics = Metrics.Pattern | Metrics.Luminance | Metrics.Contrast;

            //if there is a target shape, rescale to [-0.5, 0.5]
            if (targetIsShape)
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

            //if there is a target shape, rescale to [-0.5, 0.5]
            if (targetIsImage)
            {
                Image imageTarget = Image.FromFile(@"C:\Users\antmi\Pictures\bark1.jpg");
                Bitmap bmTarget = new Bitmap(imageTarget);
                targetValues = Fitness.PixelsFromImage(subdivisions * 2, coords, bmTarget);
                var occCount = targetValues.ColumnSums().Sum();

                if (occCount == 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not generate any voxels, try a larger number of subdivisions");
            }

            //on first run, initialise population
            if (!init)
            {
                init = true;
                pop = new Population(popSize, dims);

                Evaluation();
            }

            //paint mesh using outputs

            if (button)
            {
                Run(generations , subdivisions, popSize);
            }

            if (targetIsImage) voxelsTarget = DrawImageTarget(targetValues, subdivisions * 2);
            if (targetIsShape) voxelsTarget = DrawVoxelsTarget(targetValues, subdivisions);

            meshes = GenerateMeshes(pop, outputs, subdivisions, pop.Genomes.Count);

            var topologies = pop.Genomes.Select(i => i.ToString()).ToList();

            perfTimer.Stop();

            //output data from GH component
            DA.SetDataList(0, meshes);
            DA.SetDataList(1, fits);
            DA.SetDataList(2, topologies);
            DA.SetData(3, voxelsTarget);
            DA.SetData(4, perfTimer.Elapsed.ToString());
            DA.SetDataList(5, femModels);
            DA.SetData(6, pop.Genomes.Select(a => a.Fitness).Average());
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

        private void Evaluation()
        {
            outputs = pop.Evaluate(coords);

            fits = Fitness.Function(pop, outputs, coords, targetValues, subdivisions, metrics);

            femModels = pop.Genomes.Select(g => FEM.MakeFrame(g.FEMModel, FEM.GetDisplacements(g.FEMModel)).Item1).ToList();
            femModels = GenerateFEMs(femModels, pop.Genomes.Count);
        }

        private Mesh DrawVoxelsTarget(Matrix<double> target, int subdivisions)
        {
            var volume = new Volume();
            Mesh mesh = volume.Create(target, subdivisions, -30, 0, 0);

            return mesh;
        }

        private Mesh DrawImageTarget(Matrix<double> target, int subdivisions)
        {
            var drawing = new Drawing();
            Mesh mesh = drawing.Create(target, subdivisions, 20.0,  -30, 0);

            return mesh;
        }

        private List<Mesh> GenerateMeshes(Population pop, Dictionary<int, Matrix<double>> outputs, int subdivisions, int popSize)
        {
            meshes.Clear();

            //draw a grid of results
            int gridWidth = Math.Min(10, popSize);
            int gridDepth = popSize / gridWidth + 1;
            int padding = 10;
            int maxDisplay = Math.Min(20, popSize);

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
                            combinedMesh = drawing.Create(outputs[pop.Genomes[count].ID], subdivisions, 10.0,  j * (10 + padding), i * (10 + padding));
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

            else if(dims == 3)
            {
                for (int i = 0; i < subdivisions; i++)
                {
                    for (int j = 0; j < subdivisions; j++)
                    {
                        for (int k = 0; k < subdivisions; k++)
                        {
                            //coords are in range [-0.5, 0.5]
                            coords[i * subdivisions * subdivisions + j * subdivisions + k , 0] = 1.0 * k / subdivisions;
                            coords[i * subdivisions * subdivisions + j * subdivisions + k , 1] = 1.0 * j / subdivisions;
                            coords[i * subdivisions * subdivisions + j * subdivisions + k , 2] = 1.0 * i / subdivisions;
                        }
                    }
                }

                coords -= 0.5;
            }

            return coords;
        }
    }
}
