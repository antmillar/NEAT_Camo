using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using NumSharp;

//custom libraries
using CPPN.NEAT;
using CPPN.Net;
using CPPN.Display;
using CPPN.Fitness;

namespace GH_CPPN
{
    public class CPPN : GH_Component
    {
        private bool init;
        private List<Mesh> meshes = new List<Mesh>();
        private Population pop;
        private List<double> fits;
        private NDArray coords;
        private List<Network> nets;
        private List<NDArray> outputs;

        public CPPN() : base("CPPN", "CPPN", "Constructing a 2d CPPN", "CPPN", "Simple")
            {

            }

        public override Guid ComponentGuid
        {
            // Don't copy this GUID, make a new one
            get { return new Guid("ab047315-77a4-45ef-8039-44cd6428b913"); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            //pManager.AddTextParameter("input text", "i", "string to reverse", GH_ParamAccess.item);
            pManager.AddBooleanParameter("toggle generation", "toggle", "run the next generation", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            //pManager.AddPointParameter("points", "pts", "grid points", GH_ParamAccess.list);
            pManager.AddMeshParameter("mesh grid", "mg", "grid of meshes", GH_ParamAccess.list);
            pManager.AddTextParameter("fitnesses", "fitnesses", "output of fitnesses", GH_ParamAccess.list);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            bool button = false;
            if (!DA.GetData(0, ref button)) { return; }
            if (button == null) { return; }

            //var linear = new temp.CustomModel();
            //NDArray activations = linear.ForwardPass(coords);

            //var mlp = new temp.MLP(2, 1, 8);
            //NDArray activations = mlp.ForwardPass(coords);
            int width = 20;
            int popSize = 10;

            if (!init)
            {
                init = true;

                //initialise globals

                pop = new Population(popSize);


                coords = np.ones((width * width, 2));

                PopulateCoords(width);
                nets = new List<Network>();
                outputs = new List<NDArray>();

                GenerateImages(width, popSize);

                fits = Fitness.Function(pop, outputs, coords);
            }

            //paint mesh using outputs

            if (button)
            {
                pop.NextGeneration();

                nets.Clear();
                outputs.Clear();
                meshes.Clear();

                //regenerate everything
                GenerateImages(width, popSize);

                //calculate the fitnesses
                fits = Fitness.Function(pop, outputs, coords);
            }

            //output data from GH component
            DA.SetDataList(0, meshes);
            DA.SetDataList(1, fits);
        }


        //updates networks with new genomes, passes coords through them, outputs results
        private void GenerateImages(int width, int popSize)
        {
            for (int i = 0; i < popSize; i++)
            {
                nets.Add(new Network(pop.Genomes[i]));
            }

            foreach (var net in nets)
            {
                var output = net.ForwardPass(coords);
                outputs.Add(output);
            }

            for (int i = 0; i < outputs.Count; i++)
            {
                var drawing = new Drawing(width, -popSize / 2 * width + i * width, 0);
                Mesh combinedMesh = drawing.Paint(outputs[i]);
                meshes.Add(combinedMesh);
            }
        }

        private void PopulateCoords(int width)
        {
            //populate coords


            for (int i = -width / 2; i < width / 2; i++)
            {
                for (int j = -width / 2; j < width / 2; j++)
                {
                    //coords are in range [-0.5, 0.5]
                    coords[(i + width / 2) * width + j + width / 2, 0] = 1.0 * i / width;
                    coords[(i + width / 2) * width + j + width / 2, 1] = 1.0 * j / width;
                }
            }
        }
    }
}
