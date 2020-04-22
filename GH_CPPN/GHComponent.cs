using Grasshopper.Kernel;
using MathNet.Numerics.LinearAlgebra;
using NumSharp;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
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
        private List<double> fits;
        private Matrix<double> coords;
        private Dictionary<int, Matrix<double>> outputs;
        private Mesh targetMesh;
        private Mesh inputMesh;
        private int width = 10;
        private int popSize = 50;
        private Matrix<double> occupancy;


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
            pManager.AddIntegerParameter("width", "width", "width", GH_ParamAccess.item);
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
            DA.GetData(3, ref width);
            DA.GetData(4, ref popSize);

            //if (button == null) { return; }
            //if (cutoff == null) { return; }
            //if (inputMesh == null) { return; }



            //var linear = new temp.CustomModel();
            //NDArray activations = linear.ForwardPass(coords);

            //var mlp = new temp.MLP(2, 1, 8);
            //NDArray activations = mlp.ForwardPass(coords);
            //int width = 10;
            //int popSize = 50;

            //coords = np.ones((width * width * width, 3));
            coords = Matrix<double>.Build.Dense(width * width * width, 3);

            coords = PopulateCoords(width, 3);



            var bbox = inputMesh.GetBoundingBox(true);
            var box = new Box(bbox);

            var longestDim = Math.Max(Math.Max(box.X.Length, box.Y.Length), box.Z.Length);
            inputMesh.Translate(new Vector3d(-box.Center));
            inputMesh.Scale(1.0 / longestDim);
            occupancy = Fitness.CreateOccupancy(width * width * width, 1, coords, inputMesh);


            if (!init)
            {
                init = true;

                //initialise globals

                pop = new Population(popSize);


                outputs = new Dictionary<int, Matrix<double>>();

                outputs = pop.Evaluate(coords);

                //var target = Fitness.CreateTarget(width * width * width, 1, coords);
                fits = Fitness.Function(pop, outputs, coords, occupancy);
                pop.SortByFitness();

   

                targetMesh = GenerateTargetMesh(occupancy, width);
                meshes = GenerateMeshes(pop, outputs, width, popSize);

            }

            //paint mesh using outputs

            if (button)
            {
                Config.survivalCutoff = cutoff;
                Run(10, width, popSize);
            }

            //output data from GH component
            DA.SetDataList(0, meshes);
            DA.SetDataList(1, fits);
            DA.SetData(2, fits.Average());
            DA.SetData(3, targetMesh);
        }



        private List<Mesh> Run(int generations, int width, int popSize)
        {
            for (int i = 0; i < generations; i++)
            {
                pop.NextGeneration();

                outputs = pop.Evaluate(coords);
                fits = Fitness.Function(pop, outputs, coords, occupancy);

                pop.SortByFitness();

            }


            meshes = GenerateMeshes(pop, outputs, width, popSize);

            return meshes;
        }

        private Mesh GenerateTargetMesh(Matrix<double> target, int width)
        {
            var volume = new Volume();
            Mesh mesh = volume.Create(target, width, -60, -60);

            return mesh;
        }

        private List<Mesh> GenerateMeshes(Population pop, Dictionary<int, Matrix<double>> outputs, int width, int popSize)
        {
            meshes.Clear();
            //draw a grid of results
            var gridSide = (int) Math.Ceiling(Math.Sqrt(popSize));

            var count = 0;

            for (int i = 0; i < gridSide; i++)
            {
                for (int j = 0; j < gridSide; j++)
                {
                    if(count < popSize)
                    {
                        var volume = new Volume();
                        Mesh combinedMesh = volume.Create(outputs[pop.Genomes[i * gridSide + j].ID], width, -gridSide / 2 * (width + 3) + i * (width + 3), -gridSide / 2 * (width + 3) + j * (width + 3));
                        meshes.Add(combinedMesh);
                    }

                    count++;
                }
            }

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

        private Matrix<double> PopulateCoords(int width, int dims)
        {
            //populate coords

            var shift = width / 2;

            if (dims == 2)
            {
                for (int i = -shift; i < shift; i++)
                {
                    for (int j = -shift; j < shift; j++)
                    {
                        //coords are in range [-0.5, 0.5]
                        coords[(i + shift) * width + j + shift, 0] = 1.0 * i / width;
                        coords[(i + shift) * width + j + shift, 1] = 1.0 * j / width;
                    }
                }
            }

            else if(dims == 3)
            {
                for (int i = -shift; i < shift; i++)
                {
                    for (int j = -shift; j < shift; j++)
                    {
                        for (int k = -shift; k < shift; k++)
                        {
                            //coords are in range [-0.5, 0.5]
                            coords[(i + shift) * width*width + (j + shift) * width + k + shift, 0] = 1.0 * i / width;
                            coords[(i + shift) * width*width + (j + shift) * width + k + shift, 1] = 1.0 * j / width;
                            coords[(i + shift) * width*width + (j + shift) * width + k + shift, 2] = 1.0 * k / width;
                        }
                    }
                }

            }

            return coords;
        }
    }
}
