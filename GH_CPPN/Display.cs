using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using NumSharp;
using System.Drawing;

namespace CPPN.Display
{
   
    public class Drawing
    {
        List<Mesh> _meshes;

        public Drawing(int width)
        {
            System.Random rand = new System.Random();

            //create mesh of meshes to display CPPN
            _meshes = new List<Mesh>();

            for (int i = -width / 2; i < width / 2; i++)
            {
                for (int j = -width / 2; j < width / 2; j++)
                {
                    var cell = new Mesh();

                    cell.Vertices.Add(i, j, 0.0);
                    cell.Vertices.Add(i + 1, j, 0.0);
                    cell.Vertices.Add(i, j + 1, 0.0);
                    cell.Vertices.Add(i + 1, j + 1, 0.0);

                    cell.Faces.AddFace(0, 1, 3, 2);

                    _meshes.Add(cell);
                }
            }
        }

        public Mesh Paint(NDArray output)
        {
            //paint the meshes
            for (int i = 0; i < _meshes.Count; i++)

            {
                double col = output[i, 0];
                int intCol = (int)(col * 255);
                Color color = Color.FromArgb(intCol, intCol, intCol);

                Color[] colors = Enumerable.Repeat(color, 4).ToArray();
                _meshes[i].VertexColors.AppendColors(colors);
            }

            //combine the meshes
            Mesh combinedMesh = new Mesh();
            combinedMesh.Append(_meshes);

            return combinedMesh;
        }


    }
}
