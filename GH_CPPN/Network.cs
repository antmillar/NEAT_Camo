using NumSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TopolEvo.NEAT;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics;

namespace TopolEvo.Architecture
{

    public interface Model
    {
        Matrix<double> ForwardPass(Matrix<double> input);
    }

    public class Network : Model
    {
        int _inputCount;
        int _outputNode;
        Genome _genome;
        public List<List<NEAT.ConnectionGene>> _layers;
        public List<List<int>> _layersEndNodes;
        public List<Matrix<double>> _layersMatrices;
        public List<List<int>> _layersStartNodes;
        public Dictionary<int, Vector<double>> activations;

        public Network(NEAT.Genome genome)
        {
            _inputCount = 0;
            _genome = genome;


            foreach(NEAT.NodeGene nodeGene in _genome.Nodes)
            {
                if(nodeGene._type == "input")
                {
                    _inputCount += 1;
                }
            }

            GenerateLayers();
        }

        /// <summary>
        /// calculates the layers for the network based on nodes and connections and generates them
        /// </summary>
        public void GenerateLayers()
        {
            //use hashset as don't want repeated nodes
            var inputNodes = new HashSet<int>();
            _layers = new List<List<NEAT.ConnectionGene>>();
            _layersEndNodes = new List<List<int>>();
            _layersStartNodes = new List<List<int>>();
            _layersMatrices = new List<Matrix<double>>(); 
            //initialise the currentNodes with the input nodes
            //keep track of the output node (only one allowed)
            foreach (NEAT.NodeGene nodeGene in _genome.Nodes)
            {
                if (nodeGene._type == "input")
                {
                    inputNodes.Add(nodeGene._id);
                }

                else if (nodeGene._type == "output")
                {
                    _outputNode = nodeGene._id;
                }

            }

            //recursively creates new layers
            var output = MakeLayer(inputNodes);

        }

        /// <summary>
        /// 
        /// </summary>
        public HashSet<int> MakeLayer(HashSet<int> visitedNodes)
        {
            //could probably cache in here somehow?

            //add bias to each layer
            visitedNodes.Add(9999);

            var currentLayer = new List<NEAT.ConnectionGene>();
            var potentialNodes = new HashSet<int>();
            var nextNodes = new HashSet<int>();

            //find all connections starting from current nodes and their output nodes
            foreach (NEAT.ConnectionGene connectionGene in _genome.Connections)
            {
                if (visitedNodes.Contains(connectionGene.InputNode) & !visitedNodes.Contains(connectionGene.OutputNode))
                {
                    currentLayer.Add(connectionGene);

                    //add node ID to potential next layer, unless it's a bias connection
                    if (connectionGene.InputNode != 9999)
                    {
                        potentialNodes.Add(connectionGene.OutputNode);
                    }
                }
            }

            //finds nodes where all inputs are coming from the current nodes
            foreach (var id in potentialNodes)
            {
                var inputs = _genome.GetNodeByID(id)._inputs;

                //ensure all inputs into node are from the current nodes only
                if (visitedNodes.Intersect(inputs).Count() == inputs.Count())
                {
                    nextNodes.Add(id);
                }
            }

            //for each node, if ANY input is not in the visitedNodes, remove all of the connections to it.

            //keep only connections with inputs from current nodes
            currentLayer.RemoveAll(x => !nextNodes.Contains(x.OutputNode));


            //recursively traverse the network until next nodes is empty
            if (nextNodes.Count == 0)
            {
                return nextNodes;
            }
            else
            {

                //create a Matrix to represent the layer of connections
                //each column represents an output node
                //each node can have a variable number of inputs, so matrix will be sparse
                //for the height of columns will use the number of nodes in previous layer, lazy upper bound

                var previousNodes = currentLayer.Select(x => x.InputNode).Distinct().Count();

                var layerMatrix = Matrix<double>.Build.Dense(previousNodes, nextNodes.Count, 0);

                var nextNodeList = nextNodes.ToList();
                var nodeCountList = new int[nextNodeList.Count];

                foreach (var connection in currentLayer)
                {
                    var index = nextNodeList.IndexOf(connection.OutputNode);
                    layerMatrix[nodeCountList[index], index] = connection.Weight;
                    nodeCountList[index]++;
                }

                var startNodes = currentLayer.Select(x => x.InputNode).Distinct();

                _layersMatrices.Add(layerMatrix);
                _layers.Add(currentLayer);
                _layersStartNodes.Add(startNodes.ToList());
                _layersEndNodes.Add(nextNodes.ToList());

                //combine all visited nodes together for next iteration
                visitedNodes.UnionWith(nextNodes);
                return MakeLayer(visitedNodes);
            }
        }

        /// <summary>
        /// Single Forward Propagation through the network.
        /// Returns a set of outputs 
        /// </summary>
        /// <param name="input">Set of coordinates</param>
        /// <returns></returns>
        public Matrix<double> ForwardPass(Matrix<double> inputs)
        {
            if (_inputCount != inputs.ColumnCount) throw new IncorrectShapeException($"Network has {_inputCount} inputs, input data has shape {inputs.ColumnCount}");

            activations = new Dictionary<int, Vector<double>>();

            //copy x, y, (z) columns
            for (int i = 0; i < inputs.ColumnCount; i++)
            {
                activations[i] = inputs.Column(i);
            }

            var bias = Vector<double>.Build.Dense(inputs.RowCount, 1.0);

            //populate bias
            activations[9999] = bias;

            Matrix<double> layerOutputs = null;

            //loop over layers and calculate activations
            for (int i = 0; i < _layers.Count; i++)
            {
                layerOutputs = CalculateLayer(i, inputs.RowCount);
            }

            //last iteration of loop returns the outputs from the final layer
            var output = layerOutputs;

            if(output.ColumnCount == 2)
            {
                var test = "asd";
            }

            return output;

            #region code without matrices
            

            ////don't add the bias node twice
            //for (int i = 0; i < _genome.Nodes.Count - 1; i++)
            //{
            //    inputs[i] = np.zeros(input.shape[0]);
            //}

            ////x coords
            //inputs[0] = input[":,0"].Clone();
            ////y coords
            //inputs[1] = input[":,1"].Clone();

            //if (input.shape[1] == 3 )
            //{
            //    //z coords
            //    inputs[2] = input[":,2"].Clone();
            //}

            ////bias
            //inputs[9999] = np.ones(inputs[0].shape);



            ////loop over each layer
            //for(int i = 0; i < _layers.Count; i++)
            //{
            //    //apply the weights for each connection in layer
            //    for (int j = 0; j < _layers[i].Count; j++)
            //    {
            //        ConnectionGene connection = _layers[i][j];
            //        inputs[connection._outputNode] += connection.Weight * inputs[connection._inputNode];
            //    }


            //    //apply activation to each node and add bias
            //    //foreach(int num in _layersNodes[i])
            //   for (int j = 0; j < _layersNodes[i].Count; j++)
            //    {
            //        int num = _layersNodes[i][j];

            //        IActivation act;

            //        if (_genome.GetNodeByID(num)._activationType == "sigmoid")
            //        {
            //            act = sigmoid;
            //        }
            //        else
            //        {
            //            //act = new Tanh();
            //            act = tanh;

            //        }
            //        //seems to apply to the bias node too, do i need to stop that?
            //        inputs[num] = act.Apply(inputs[num]);
            //    }
            //}

            //var output2 = np.expand_dims(inputs[_outputNode], 1);


            //var diff = output2 - output;
            //what about more than one output?

            #endregion

        }

        private Matrix<double> CalculateLayer(int layerNum, int rows)
        {
            Matrix<double> layerOutputs = null;

            //get incoming nodes
            var matrixInputs = Matrix<double>.Build.Dense(rows, _layersStartNodes[layerNum].Count, 0);

            //create matrix of inputs for the layer
            for (int j = 0; j < _layersStartNodes[layerNum].Count; j++)
            {
                int nodeNum = _layersStartNodes[layerNum][j];
                matrixInputs.SetColumn(j, activations[nodeNum]);
            }

            //layer w*x + b
            layerOutputs = matrixInputs * _layersMatrices[layerNum];

            //apply activation functions to each column (node)

            Parallel.For(0, layerOutputs.ColumnCount, (j) =>
            {
                var nodeNum = _layersEndNodes[layerNum][j];
                var actFunction = _genome.GetNodeByID(nodeNum).Activation.Function;

                var newVals = layerOutputs.Column(j);
                layerOutputs.Column(j).Map(actFunction, newVals, Zeros.Include);
                layerOutputs.SetColumn(j, newVals);


            });

            //have to populate dictionary outside the parallel for because not thread safe
            for (int j = 0; j < layerOutputs.ColumnCount; j++)
            {
                var nodeNum = _layersEndNodes[layerNum][j];
                activations[nodeNum] = layerOutputs.Column(j);
            }

            return layerOutputs;
        }



    }


    
    //LAYERS
    public interface ILayer
    {
        NDArray Apply(NDArray input);

    }

    public class Linear : ILayer
    {
        NDArray weights;
        NDArray biases;

        public Linear(int _input_feats, int _output_feats, bool _bias = true)
        {
            //initialise weights and biases
            weights = np.random.uniform(-1.0, 1.0, (_input_feats, _output_feats));
            biases = np.random.uniform(-1.0, 1.0, (1, _output_feats));// : np.zeros((1 , _output_feats)); //need to initialise these at some point
  
        }
        public NDArray Apply(NDArray input)
        {
 
            return np.matmul(input, weights) + biases;
        }
    }

    //ACTIVATIONS
    //interface for activation functions
    public interface IActivation
    {
        NDArray Apply (NDArray input);
    }

    public class Activation
    {
        public Func<double, double> Function { get; set; }
        public string Name { get; set; }

        public Activation(Func<double, double> function, string name)
         {
            Function = function;
            Name = name;
          }
    }


    public static class Activations
    {
        public static Activation Sigmoid()
        {
            return new Activation(SpecialFunctions.Logistic, "Sigmoid"); ;
        }

        public static Activation Tanh()
        {
            return new Activation(Trig.Tanh, "Tanh");
        }

        public static Activation Sin()
        {
            return new Activation(Trig.Sin, "Sin");
        }
        public static Activation Fract()
        {
            return new Activation((value) => (value - Math.Truncate(value)), "Fract");
        }

        internal static Activation Rescale()
        {
            return new Activation((value) => (10.0 * value), "Rescale");
        }

        internal static Activation Gaussian()
        {
            return new Activation((value) => Math.Exp(-(value * value)), "Gaussian");
        }
    }

    public class Sigmoid : IActivation
    {
        public NDArray Apply(NDArray input)
        {
            return 1.0 / (1.0 + np.exp(-1.0 * input));

        }
    }

    public class Tanh : IActivation
    {
        public NDArray Apply(NDArray input)
        {
            return (np.exp(input) - np.exp(-1.0 * input)) / (np.exp(input) + np.exp(-1.0 * input));
        }
    }

    public class Sin : IActivation
    {
        public NDArray Apply(NDArray input)
        {
            return np.sin(input);
        }
    }

    public class Cos : IActivation
    {
        public NDArray Apply(NDArray input)
        {
            return np.cos(input);
        }
    }

    public class Relu : IActivation
    {
        public NDArray Apply(NDArray input)
        {
            return np.maximum(input, 0);
        }
    }

}
