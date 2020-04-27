using Grasshopper.Kernel;
using MathNet.Numerics.LinearAlgebra;
using NumSharp;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TopolEvo.Architecture;
using TopolEvo.Display;
using TopolEvo.Fitness;
//custom libraries
using TopolEvo.NEAT;

namespace GH_CPPN
{
    public class GHComponent : GH_Component
    {
        public GHComponent() : base("Topology Evolver", "TopolEvo", "Evolving CPPNs", "TopologyEvolver", "2D")
        {

        }

        //fields
        private bool init;
        private List<Mesh> meshes = new List<Mesh>();
        private Population pop;
        private List<string> fits;
        private List<Mesh> femModels;
        private Matrix<double> coords;
        private Dictionary<int, Matrix<double>> outputs;
        private Mesh targetMesh;
        private Mesh inputMesh;
        private int subdivisions = 10;
        private int popSize = 50;
        private Matrix<double> occupancy;
        private Stopwatch timer;


        public override Guid ComponentGuid
        {
            // Don't copy this GUID, make a new one
            get { return new Guid("ab047315-77a4-45ef-8039-44cd6428b913"); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            //pManager.AddTextParameter("input text", "i", "string to reverse", GH_ParamAccess.item);
            pManager.AddBooleanParameter("toggle generation", "toggle", "run the next generation", GH_ParamAccess.item);
            pManager.AddNumberParameter("survival cutoff", "survival cutoff", "survival cutoff", GH_ParamAccess.item);
            pManager.AddMeshParameter("input mesh", "inputMesh mesh", "inputMesh mesh", GH_ParamAccess.item);
            pManager.AddIntegerParameter("subdivisions", "subdivisions", "subdivisions", GH_ParamAccess.item);
            pManager.AddIntegerParameter("popSize", "popSize", "popSize", GH_ParamAccess.item);


            //pManager[0].Optional = true;
            pManager[1].Optional = true;
            //pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            //pManager.AddPointParameter("points", "pts", "grid points", GH_ParamAccess.list);
            pManager.AddMeshParameter("mesh grid", "mg", "grid of meshes", GH_ParamAccess.list);
            pManager.AddTextParameter("fitnesses", "fitnesses", "output of fitnesses", GH_ParamAccess.list);
            pManager.AddNumberParameter("mean fitness", "mean fitness", "means of fitnesses", GH_ParamAccess.item);
            pManager.AddMeshParameter("target mesh", "target", "target meshes", GH_ParamAccess.item);
            pManager.AddTextParameter("timer", "timer", "timer", GH_ParamAccess.item);
            pManager.AddMeshParameter("fems", "fems", "fems", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //defaults
            bool button = false;
            double cutoff = 0.5;
            inputMesh = new Mesh();
            //init = false;

            //if (!DA.GetData(0, ref button)) { return; }
            //if (!DA.GetData(1, ref cutoff)) { return; }


            DA.GetData(0, ref button);
            DA.GetData(1, ref cutoff);
            if (!DA.GetData(2, ref inputMesh)) { return; }
            DA.GetData(3, ref subdivisions);
            DA.GetData(4, ref popSize);

            //if (button == null) { return; }
            //if (cutoff == null) { return; }
            //if (inputMesh == null) { return; }

            coords = Matrix<double>.Build.Dense(subdivisions * subdivisions * subdivisions, 3);
            coords = PopulateCoords(subdivisions, 3);

            var bbox = inputMesh.GetBoundingBox(true);
            var box = new Box(bbox);

            var longestDim = Math.Max(Math.Max(box.X.Length, box.Y.Length), box.Z.Length);
            inputMesh.Translate(new Vector3d(-box.Center));
            inputMesh.Scale(1.0 / longestDim);


            occupancy = Fitness.CreateOccupancy(subdivisions * subdivisions * subdivisions, 1, coords, inputMesh);
            var occCount = occupancy.ColumnSums().Sum();

            if(occCount == 0)  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not generate any voxels, try a larger number of subdivisions");

            if (!init)
            {
                init = true;

                timer = new Stopwatch();
                timer.Start();


                //initialise globals

                pop = new Population(popSize);


                outputs = new Dictionary<int, Matrix<double>>();

                outputs = pop.Evaluate(coords);

                outputs = Fitness.OutputOccupancy(outputs);

                //var target = Fitness.CreateTarget(width * width * width, 1, coords);
                fits = Fitness.Function(pop, outputs, coords, occupancy, subdivisions);

                femModels = pop.Genomes.Select(g => FEM.MakeFrame(g.FEMModel, FEM.GetDisplacements(g.FEMModel)).Item1).ToList();
                femModels = GenerateFEMs(femModels, popSize);
                timer.Stop();

                targetMesh = GenerateTargetMesh(occupancy, subdivisions);
                meshes = GenerateMeshes(pop, outputs, subdivisions, popSize);


            }

            //paint mesh using outputs

            if (button)
            {
                timer = new Stopwatch();
                timer.Start();

                Config.survivalCutoff = cutoff;
                Run(1, subdivisions, popSize);

                timer.Stop();
                
            }

            //output data from GH component
            DA.SetDataList(0, meshes);
            DA.SetDataList(1, fits);
           // DA.SetData(2, fits.Average());
            DA.SetData(3, targetMesh);
            DA.SetData(4, timer.Elapsed.ToString());
            DA.SetDataList(5, femModels);
        }



        private List<Mesh> Run(int generations, int subdivisions, int popSize)
        {
            for (int i = 0; i < generations; i++)
            {
                pop.NextGeneration();

                outputs = pop.Evaluate(coords);
                outputs = Fitness.OutputOccupancy(outputs);
                fits = Fitness.Function(pop, outputs, coords, occupancy, subdivisions);

                femModels = pop.Genomes.Select(g => FEM.MakeFrame(g.FEMModel, FEM.GetDisplacements(g.FEMModel)).Item1).ToList();
                femModels = GenerateFEMs(femModels, popSize);
            }

            targetMesh = GenerateTargetMesh(occupancy, subdivisions);
            meshes = GenerateMeshes(pop, outputs, subdivisions, popSize);

            return meshes;
        }

        private Mesh GenerateTargetMesh(Matrix<double> target, int subdivisions)
        {
            var volume = new Volume();
            Mesh mesh = volume.Create(target, subdivisions, -30, 0, 0);

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
                        var volume = new Volume();
                        Mesh combinedMesh = volume.Create(outputs[pop.Genomes[count].ID], subdivisions, j * (10 + padding),  i * (10 + padding));
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
