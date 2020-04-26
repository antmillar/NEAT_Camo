using MathNet.Numerics.LinearAlgebra;
using NumSharp;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TopolEvo.Display
{

    public class Drawing
    {

        //constructor
        public Drawing(int subdivisions, int xCenter = 0, int yCenter = 0)
        {
            System.Random rand = new System.Random();

            for (int i = xCenter - subdivisions / 2; i < xCenter + subdivisions / 2; i++)
            {
                for (int j = yCenter - subdivisions / 2; j < yCenter + subdivisions / 2; j++)
                {
                    var cell = new Mesh();

                    cell.Vertices.Add(i, j, 0.0);
                    cell.Vertices.Add(i + 1, j, 0.0);
                    cell.Vertices.Add(i, j + 1, 0.0);
                    cell.Vertices.Add(i + 1, j + 1, 0.0);

                    cell.Faces.AddFace(0, 1, 3, 2);

                    Meshes.Add(cell);
                }
            }
        }

        //properties
        public List<Mesh> Meshes { get; set; } = new List<Mesh>();

        //methods
        public Mesh Paint(NDArray output)
        {
            //paint the meshes
            for (int i = 0; i < Meshes.Count; i++)

            {
                double col = output[i, 0];
                if (col > 1.0) col = 1.0;
                int intCol = (int)(col * 255);
                Color color = Color.FromArgb(intCol, intCol, intCol);

                Color[] colors = Enumerable.Repeat(color, 4).ToArray();
                Meshes[i].VertexColors.AppendColors(colors);
            }

            //combine the meshes
            Mesh combinedMesh = new Mesh();
            combinedMesh.Append(Meshes);

            return combinedMesh;
        }
    }

    
    public class Volume
    {

        //properties
        public List<Mesh> Meshes { get; set; } = new List<Mesh>();

        
        //methods
        public Mesh Create(Matrix<double> output, int subdivisions, int xCenter = 0, int yCenter = 0, int zCenter = 0)
        {

            double size = 10.0 / subdivisions;
            int counter = 0;

            for (int i = 0; i < subdivisions; i++)
            {
                for (int j = 0; j < subdivisions; j++)
                {
                    for (int k = 0; k < subdivisions; k++)
                    {
                        double col = output[counter, 0];

                        if (col > 0.5) {
                            var cube = new Mesh();

                            cube.Vertices.Add(k * size, j * size, i * size);
                            cube.Vertices.Add((k + 1) * size, j * size, i * size);
                            cube.Vertices.Add(k * size, (j + 1) * size, i * size);
                            cube.Vertices.Add((k + 1) * size, (j + 1) * size, i * size);


                            cube.Vertices.Add(k * size, j * size, (i + 1) * size);
                            cube.Vertices.Add((k + 1) * size, j * size, (i + 1) * size);
                            cube.Vertices.Add(k * size, (j + 1) * size, (i + 1) * size);
                            cube.Vertices.Add((k + 1) * size, (j + 1) * size, (i + 1) * size);

                            cube.Faces.AddFace(0, 1, 3, 2);
                            cube.Faces.AddFace(1, 5, 7, 3);
                            cube.Faces.AddFace(5, 4, 6, 7);
                            cube.Faces.AddFace(4, 0, 2, 6);
                            cube.Faces.AddFace(0, 1, 5, 4);
                            cube.Faces.AddFace(2, 3, 7, 6);

                            cube.Translate(new Vector3d(xCenter, yCenter, zCenter));

                            Meshes.Add(cube);
                        }

                        counter++;
                    }
                }
            }
          
            //combine the meshes
            Mesh combinedMesh = new Mesh();
            combinedMesh.Append(Meshes);

            return combinedMesh;
        }


    }
}
