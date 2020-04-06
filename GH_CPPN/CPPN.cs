using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using NumSharp;
using WIP;
using test;

namespace GH_CPPN
{
    public class CPPN : GH_Component
    {

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
            //pManager.AddNumberParameter("input number", "num", "number to input", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            //pManager.AddPointParameter("points", "pts", "grid points", GH_ParamAccess.list);
            pManager.AddMeshParameter("mesh grid", "mg", "grid of meshes", GH_ParamAccess.item);
            pManager.AddNumberParameter("outputs", "outputs", "output of linear", GH_ParamAccess.list);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            //double data = double.NaN;

            //if (!DA.GetData(0, ref data)) { return; }

            //if (data == double.NaN) { return; }



            System.Random rand = new System.Random();

            List<Mesh> meshes = new List<Mesh>();
            int width = 100;

            NDArray coords = np.ones((width * width, 2));

            for (int i = -width/2; i < width/2; i++)
            {
                for (int j = -width/2; j < width/2; j++)
                {
                    var cell = new Mesh();

                    cell.Vertices.Add(i, j, 0.0);
                    cell.Vertices.Add(i + 1, j, 0.0);
                    cell.Vertices.Add(i, j + 1, 0.0);
                    cell.Vertices.Add(i + 1, j + 1, 0.0);

                    cell.Faces.AddFace(0, 1, 3, 2);

                    meshes.Add(cell);

                    //coords are in range [-0.5, 0.5]
                    coords[(i + width/2)* width + j + width/2, 0] = 1.0 * i / width;
                    coords[(i + width/2)* width + j + width/2, 1] = 1.0 * j / width;

                }
            }

            Rhino.Geometry.Plane plane = Rhino.Geometry.Plane.WorldXY;

            var linear = new temp.CustomModel();
            //NDArray activations = linear.ForwardPass(coords);

            var mlp = new temp.MLP(2, 1, 8);
            NDArray activations = mlp.ForwardPass(coords);

            for (int i = 0; i < meshes.Count; i++)
       
            {
                double col = activations[i, 0];
                int intCol = (int) (col * 255);
                Color color = Color.FromArgb(intCol, intCol, intCol);

                Color[] colors = Enumerable.Repeat(color, 4).ToArray();
                meshes[i].VertexColors.AppendColors(colors);
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.Append(meshes);
            DA.SetData(0, combinedMesh);
            DA.SetDataList(1, activations);


            var pop = new NEAT.Population(10);
            Console.WriteLine(pop);

            var genome = new NEAT.Genome();

            Console.WriteLine(genome);

            var network = new Network(genome);

            network.GenerateLayers();

            Console.WriteLine("TEST");
        }
    }
}
