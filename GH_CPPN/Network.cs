using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumSharp;
using TopolEvo.NEAT;

namespace TopolEvo.Architecture
{
  
    //MODELS
    public interface Model
    {
        NDArray ForwardPass(NDArray input);
    }

    public class Network : Model
    {
        int _inputCount;
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

        //calculates the layers for the network based on nodes and connections
        public void GenerateLayers()
        {
            var currentNodes = new List<int>();
            _layers = new List<List<NEAT.ConnectionGene>>();

            //find input nodes
            foreach (NEAT.NodeGene nodeGene in _genome.Nodes)
            {
                if (nodeGene._type == "input")
                {
                    currentNodes.Add(nodeGene._id);
                }
            }

            var output = MakeLayer(currentNodes);

        }
        public List<int> MakeLayer(List<int> currentNodes)
        {

            var currentLayer = new List<NEAT.ConnectionGene>();
            var potentialNodes = new List<int>();
            var nextNodes = new List<int>();

            //find all connections starting from current nodes and their output nodes
            foreach (NEAT.ConnectionGene connectionGene in _genome.Connections)
            {
                if (currentNodes.Contains(connectionGene._inputNode))
                {
                    currentLayer.Add(connectionGene);
                    potentialNodes.Add(connectionGene._outputNode);
                }
            }

            //finds nodes where all inputs are from current nodes
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

        public NDArray ForwardPass(NDArray input)
        {
            if (_inputCount != input.shape[1]) throw new IncorrectShapeException($"Network has {_inputCount} inputs, input data has shape {input.shape}");

            var inputs = new Dictionary<int, NDArray>();
            inputs[0] = input[":,0"].Clone();
            inputs[1] = input[":,1"].Clone();
            var tanh = new Tanh();


            foreach (var layer in _layers)
            {
                var layerNodes = new List<int>();

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
                        act = new Tanh();
                    }
                    inputs[n] = act.Apply(inputs[n] + node._bias);
                }
            }

            //what about more than one output?
            return np.expand_dims(inputs[inputs.Count - 1],1) ;
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
