using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace GH_CPPN
{
    public class FEMComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public FEMComponent()
          : base("FEM", "FEM",
              "FEM analysis using Brief Finite Element Library",
              "FEM", "BFE")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddTextParameter("FEM", "FEM", "FEM", GH_ParamAccess.item);
            pManager.AddMeshParameter("pts", "pts", "pts", GH_ParamAccess.list);
            pManager.AddLineParameter("lines", "lines", "lines", GH_ParamAccess.list);

            // Sometimes you want to hide a specific parameter from the Rhino preview.
            // You can use the HideParameter() method as a quick way:
            //pManager.HideParameter(0);
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
            
            var results = FEM.Example1();
  
            var tuple = FEM.MakeFrame();
            var boxes = tuple.Item1;
            var beams = tuple.Item2;

            var test = FEM.AnalyseFrame(boxes);

            var total = test.Sum();

            var meshes = new List<Mesh>();

            for(int i = 0; i < boxes.Count; i++)
            {
                var mesh = Mesh.CreateFromBox(boxes[i], 1, 1, 1);

                Color color = Color.FromArgb((int) Math.Min(255, test[i]), 255, 0);
                Color[] colors = Enumerable.Repeat(color, 24).ToArray();


                mesh.VertexColors.AppendColors(colors);
                meshes.Add(mesh.DuplicateMesh());

            }

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, results);
            DA.SetDataList(1, meshes);
            DA.SetDataList(2, beams);
        }


        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d46a10d3-b402-45ba-9fdd-6bdee01ee377"); }
        }
    }
}
