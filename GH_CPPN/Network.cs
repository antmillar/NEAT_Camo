using NumSharp;
using System.Collections.Generic;
using System.Linq;
using TopolEvo.NEAT;

namespace TopolEvo.Architecture
{

    public interface Model
    {
        NDArray ForwardPass(NDArray input);
    }

    public class Network : Model
    {
        int _inputCount;
        int _outputNode;
        Genome _genome;
        public List<List<NEAT.ConnectionGene>> _layers;
        public List<List<int>> _layersNodes;
        public List<NDArray> _layersMatrices;

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
            _layersMatrices = new List<NDArray>();

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
                if (currentNodes.Contains(connectionGene._inputNode))
                {
                    currentLayer.Add(connectionGene);

                    //add node ID to potential next layer, unless it's a bias connection
                    if (connectionGene._inputNode != 9999)
                    {
                        potentialNodes.Add(connectionGene._outputNode);
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
            currentLayer.RemoveAll(x => !nextNodes.Contains(x._outputNode));

            //create an NDArray to represent the layer of connections
            //each column represents an output node
            //each node can have a variable number of inputs, so matrix will be sparse
            //for the height of columns will use the number of nodes in previous layer, lazy upper bound

            NDArray layerMatrix = np.zeros((currentNodes.Count, nextNodes.Count));

            var nextNodeList = nextNodes.ToList();
            var nodeCountList = new int[nextNodeList.Count];

            foreach (var connection in currentLayer)
            {
                var index = nextNodeList.IndexOf(connection._outputNode);
                layerMatrix[nodeCountList[index], index] = connection.Weight;
                nodeCountList[index]++;
            }




            //recursively traverse the network
            if (nextNodes.Count == 0)
            {
                return nextNodes;
            }
            else
            {
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
        public NDArray ForwardPass(NDArray input)
        {
            if (_inputCount != input.shape[1]) throw new IncorrectShapeException($"Network has {_inputCount} inputs, input data has shape {input.shape[1]}");

            var sigmoid = new Sigmoid();
            var tanh = new Tanh();

            var inputs = new Dictionary<int, NDArray>();
            var valuesMatrices = new Dictionary<int, NDArray>();

            var firstMatrix = np.zeros(input.shape[0], 4);
            firstMatrix[Slice.All, 0] = input[":,0"].Clone();
            firstMatrix[Slice.All, 1] = input[":,1"].Clone();
            firstMatrix[Slice.All, 2] = input[":,2"].Clone();
            firstMatrix[Slice.All, 3] = np.ones(input.shape[0]);

            var test = np.dot(firstMatrix,_layersMatrices[0]);
            test = tanh.Apply(test);
            test = np.concatenate((test, np.ones((input.shape[0], 1))),1 );

            var output = np.dot(test, _layersMatrices[1]);
            output = sigmoid.Apply(output);

            //don't add the bias node twice
            for (int i = 0; i < _genome.Nodes.Count - 1; i++)
            {
                inputs[i] = np.zeros(input.shape[0]);
            }

            //x coords
            inputs[0] = input[":,0"].Clone();
            //y coords
            inputs[1] = input[":,1"].Clone();

            if (input.shape[1] == 3 )
            {
                //z coords
                inputs[2] = input[":,2"].Clone();
            }

            //bias
            inputs[9999] = np.ones(inputs[0].shape);


            
            //loop over each layer
            for(int i = 0; i < _layers.Count; i++)
            {
                //apply the weights for each connection in layer
                for (int j = 0; j < _layers[i].Count; j++)
                {
                    ConnectionGene connection = _layers[i][j];
                    inputs[connection._outputNode] += connection.Weight * inputs[connection._inputNode];
                }


                //apply activation to each node and add bias
                //foreach(int num in _layersNodes[i])
               for (int j = 0; j < _layersNodes[i].Count; j++)
                {
                    int num = _layersNodes[i][j];

                    IActivation act;

                    if (_genome.GetNodeByID(num)._activationType == "sigmoid")
                    {
                        act = sigmoid;
                    }
                    else
                    {
                        //act = new Tanh();
                        act = tanh;

                    }
                    //seems to apply to the bias node too, do i need to stop that?
                    inputs[num] = act.Apply(inputs[num]);
                }
            }

            var output2 = np.expand_dims(inputs[_outputNode], 1);

            var diff = output2 - output;
            //what about more than one output?
            return output;
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
