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

            if (!init)
            {
                init = true;
                int popSize = 10;
                pop = new Population(popSize);





                int width = 20;

                //populate coords
                NDArray coords = np.ones((width * width, 2));

                for (int i = -width / 2; i < width / 2; i++)
                {
                    for (int j = -width / 2; j < width / 2; j++)
                    {
                        //coords are in range [-0.5, 0.5]
                        coords[(i + width / 2) * width + j + width / 2, 0] = 1.0 * i / width;
                        coords[(i + width / 2) * width + j + width / 2, 1] = 1.0 * j / width;
                    }
                }

                fits = Fitness.Function(pop, coords);


                List<Network> nets = new List<Network>();


                for (int i = 0; i < popSize; i++)
                {
                    nets.Add(new Network(pop.Genomes[i]));
                }
                //var network = new Network(genome);

                var outputs = new List<NDArray>();
                meshes.Clear();
                //var meshes = new List<Mesh>();

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

            //paint mesh using outputs

            if(button)
            {

                pop.NextGeneration();


                int popSize = 10;
                //redraw them
                List<Network> nets = new List<Network>();

                for (int i = 0; i < popSize; i++)
                {
                    nets.Add(new Network(pop.Genomes[i]));
                }

                int width = 20;

                //populate coords
                NDArray coords = np.ones((width * width, 2));

                for (int i = -width / 2; i < width / 2; i++)
                {
                    for (int j = -width / 2; j < width / 2; j++)
                    {
                        //coords are in range [-0.5, 0.5]
                        coords[(i + width / 2) * width + j + width / 2, 0] = 1.0 * i / width;
                        coords[(i + width / 2) * width + j + width / 2, 1] = 1.0 * j / width;
                    }
                }

                var outputs = new List<NDArray>();
                meshes.Clear();
                //var meshes = new List<Mesh>();

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

                fits = Fitness.Function(pop, coords);
            }


            //output data from GH component
            DA.SetDataList(0, meshes);
            DA.SetDataList(1, fits);
        }
    }
}
