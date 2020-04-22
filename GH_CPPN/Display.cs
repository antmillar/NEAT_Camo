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
        public Drawing(int width, int xCenter = 0, int yCenter = 0)
        {
            System.Random rand = new System.Random();

            for (int i = xCenter - width / 2; i < xCenter + width / 2; i++)
            {
                for (int j = yCenter - width / 2; j < yCenter + width / 2; j++)
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
        public Mesh Create(Matrix<double> output, int width, int xCenter = 0, int yCenter = 0, int zCenter = 0)
        {
            int cellCount = width / 1;
            double size = 10 / cellCount;
            int counter = 0;

            for (int i = 0; i < cellCount; i++)
            {
                for (int j = 0; j < cellCount; j++)
                {
                    for (int k = 0; k < cellCount; k++)
                    {
                        double col = output[counter, 0];

                        if (col > 0.5) {
                            var cube = new Mesh();

                            cube.Vertices.Add(i * size, j * size, k * size);
                            cube.Vertices.Add((i + 1) * size, j * size, k * size);
                            cube.Vertices.Add(i * size, (j + 1) * size, k * size);
                            cube.Vertices.Add((i + 1) * size, (j + 1) * size, k * size);


                            cube.Vertices.Add(i * size, j * size, (k + 1) * size);
                            cube.Vertices.Add((i + 1) * size, j * size, (k + 1) * size);
                            cube.Vertices.Add(i * size, (j + 1) * size, (k + 1) * size);
                            cube.Vertices.Add((i + 1) * size, (j + 1) * size, (k + 1) * size);

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
