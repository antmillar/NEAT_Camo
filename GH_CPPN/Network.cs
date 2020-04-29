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
        public List<List<int>> _layersNodes;
        public List<Matrix<double>> _layersMatrices;
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
            var currentNodes = new HashSet<int>();
            _layers = new List<List<NEAT.ConnectionGene>>();
            _layersNodes = new List<List<int>>();
            _layersMatrices = new List<Matrix<double>>(); 
            //initialise the currentNodes with the input nodes
            //keep track of the output node (only one allowed)
            foreach (NEAT.NodeGene nodeGene in _genome.Nodes)
            {
                if (nodeGene._type == "input")
                {
                    currentNodes.Add(nodeGene._id);
                }

                else if (nodeGene._type == "output")
                {
                    _outputNode = nodeGene._id;
                }

            }

            //recursively creates new layers
            var output = MakeLayer(currentNodes);

        }

        /// <summary>
        /// 
        /// </summary>
        public HashSet<int> MakeLayer(HashSet<int> currentNodes)
        {
            //could probably cache in here somehow?


            //add bias to each layer
            currentNodes.Add(9999);

            var currentLayer = new List<NEAT.ConnectionGene>();
            var potentialNodes = new HashSet<int>();
            var nextNodes = new HashSet<int>();

            //find all connections starting from current nodes and their output nodes
            foreach (NEAT.ConnectionGene connectionGene in _genome.Connections)
            {
                if (currentNodes.Contains(connectionGene.InputNode))
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

                //check if all inputs into node are from the current nodes
                if (currentNodes.Intersect(inputs).Count() == inputs.Count())
                {
                    nextNodes.Add(id);
                }
            }

            //keep only nodes with inputs from current nodes
            currentLayer.RemoveAll(x => !nextNodes.Contains(x.OutputNode));


            //recursively traverse the network
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

                var layerMatrix = Matrix<double>.Build.Dense(currentNodes.Count, nextNodes.Count, 0);

                var nextNodeList = nextNodes.ToList();
                var nodeCountList = new int[nextNodeList.Count];

                foreach (var connection in currentLayer)
                {
                    var index = nextNodeList.IndexOf(connection.OutputNode);
                    layerMatrix[nodeCountList[index], index] = connection.Weight;
                    nodeCountList[index]++;
                }

                _layersMatrices.Add(layerMatrix);
                _layers.Add(currentLayer);
                _layersNodes.Add(nextNodes.ToList());
                return MakeLayer(nextNodes);
            }
        }

        /// <summary>
        /// Single Forward Propagation through the network.
        /// Returns a set of outputs 
        /// </summary>
        /// <param name="input">Set of coordinates</param>
        /// <returns></returns>
        public Matrix<double> ForwardPass(Matrix<double> input)
        {
            if (_inputCount != input.ColumnCount) throw new IncorrectShapeException($"Network has {_inputCount} inputs, input data has shape {input.ColumnCount}");

            var V = Vector<double>.Build;
            var M = Matrix<double>.Build;

            //set up the input layer

            var inputs = M.Dense(input.RowCount, input.ColumnCount, 0);

            //copy x, y, (z) columns
            for (int i = 0; i < input.ColumnCount; i++)
            {
                inputs.SetColumn(i, input.Column(i));
            }

            var bias =V.Dense(input.RowCount, 1.0);
            var biasMatrix = M.Dense(input.RowCount, 1);
            biasMatrix.SetColumn(0, bias);


            var output = CalculateLayer(0, inputs);


            //var inputsWithBias = new Matrix<double>[,] { { inputs, biasMatrix } };
            //var inputs1 = M.DenseOfMatrixArray(inputsWithBias);

            ////forward propagation

            //var outputs1 = inputs1 * _layersMatrices[0]; //w*x + b

            ////apply activation function to each column
            //for(int i = 0; i < outputs1.ColumnCount; i++)
            //{
            //    var nodenum = _layersNodes[0][i];
            //    var act = _genome.GetNodeByID(nodenum)._activationType;

            //    var temp = outputs1.Column(i);
            //    outputs1.Column(i).Map(act, temp, Zeros.Include);
            //    outputs1.SetColumn(i, temp);
            //}


            ////outputs1.Map(Trig.Tanh, outputs1); //activation

            //var outputs1WithBias = new Matrix<double>[,] { { outputs1, biasMatrix } };
            //var z1 = M.DenseOfMatrixArray(outputs1WithBias);

            //var outputs2 = z1 * _layersMatrices[1]; //w*x + b

            ////apply activation function to each column
            //for (int i = 0; i < outputs2.ColumnCount; i++)
            //{
            //    var nodenum = _layersNodes[1][i];
            //    var act = _genome.GetNodeByID(nodenum)._activationType;

            //    var temp = outputs2.Column(i);
            //    outputs2.Column(i).Map(act, temp, Zeros.Include);
            //    outputs2.SetColumn(i, temp);
            //}

            //outputs2.Map(SpecialFunctions.Logistic, outputs2); // activation

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

        private Matrix<double> CalculateLayer(int layerIndex, Matrix<double> inputs)
        {
            var V = Vector<double>.Build;
            var M = Matrix<double>.Build;

            //create matrix for bias column
            var bias = V.Dense(inputs.RowCount, 1.0);
            var biasMatrix = M.Dense(inputs.RowCount, 1);
            biasMatrix.SetColumn(0, bias);

            //append to inputs
            var concat = new Matrix<double>[,] { { inputs, biasMatrix } };
            var inputsWithBias = M.DenseOfMatrixArray(concat);

            //forward propagation

            var outputs = inputsWithBias * _layersMatrices[layerIndex]; //w*x + b


            //apply activation functions
            Parallel.For(0, outputs.ColumnCount, (i) =>
             {
                 var nodenum = _layersNodes[layerIndex][i];
                 var act = _genome.GetNodeByID(nodenum)._activationType;

                 var temp = outputs.Column(i);
                 outputs.Column(i).Map(act, temp, Zeros.Include);
                 outputs.SetColumn(i, temp);
             });

            //apply activation function to each column non parallel
            //for (int i = 0; i < outputs.ColumnCount; i++)
            //{
            //    var nodenum = _layersNodes[layerIndex][i];
            //    var act = _genome.GetNodeByID(nodenum)._activationType;

            //    var temp = outputs.Column(i);
            //    outputs.Column(i).Map(act, temp, Zeros.Include);
            //    outputs.SetColumn(i, temp);
            //}

            //when get to last layer, stop iterating
            if(layerIndex + 1 < _layersMatrices.Count)
            {
                return CalculateLayer(layerIndex + 1, outputs);
            }
            else
            {
                return outputs;
            }

        }

        //had to write my own parallel dot product because the one in numsharp is incredibly slow.
        //https://github.com/SciSharp/NumSharp/issues/201
        //It's even slower than manually looping over every node and connection!!

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

    public static class Activation
    {
        public static Func<double, double> Sigmoid()
        {
            return SpecialFunctions.Logistic;
        }

        public static Func<double, double> Tanh()
        {
            return Trig.Tanh;
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
