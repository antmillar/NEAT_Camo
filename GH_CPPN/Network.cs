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
            
            //recursively traverse the network
            if (nextNodes.Count == 0)
            {
                return nextNodes;
            }
            else
            {
                _layers.Add(currentLayer);
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
            if (_inputCount != input.shape[1]) throw new IncorrectShapeException($"Network has {_inputCount} inputs, input data has shape {input.shape}");

            var inputs = new Dictionary<int, NDArray>();
            //x coords
            inputs[0] = input[":,0"].Clone();
            //y coords
            inputs[1] = input[":,1"].Clone();

            //bias
            inputs[9999] = np.ones(inputs[0].shape);
            
            //loop over each layer
            foreach (var layer in _layers)
            {
                var layerNodes = new List<int>();

                //apply the weights
                foreach (var connection in layer)
                {
                    //create new key if not present
                    if (inputs.ContainsKey(connection._outputNode)) 
                    { 
                        inputs[connection._outputNode] += connection.Weight * inputs[connection._inputNode];
                    }
                    else
                    {
                        inputs[connection._outputNode] = connection.Weight * inputs[connection._inputNode];
                        layerNodes.Add(connection._outputNode);
                    }
                }

                //apply activation to each node and add bias
                foreach(int n in layerNodes)
                {
                    IActivation act;
                    var node = _genome.GetNodeByID(n);

                    if (node._activationType == "sigmoid")
                    {
                        act = new Sigmoid();
                    }
                    else
                    {
                        //act = new Tanh();
                        act = new Sigmoid();

                    }

                    inputs[n] = act.Apply(inputs[n]);
                }
            }

            //what about more than one output?
            return np.expand_dims(inputs[_outputNode],1) ;
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
