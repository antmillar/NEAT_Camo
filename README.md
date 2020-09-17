# Evolving Camouflage with [NEAT](https://www.cs.ucf.edu/~kstanley/neat.html) and [Compositional Pattern Producing Networks](https://en.wikipedia.org/wiki/Compositional_pattern-producing_network) in Rhino Grasshopper

C# component for Grasshopper that implements a CPPN from scratch to evolve camouflage designs

Camouflage designs are evolved to blend in with their background, given a fitness functions determined by the following three metrics:

* Luminance Comparison

* Edge Detectability using Gabor Filters

* Feature Matching using SURF features



## Example Images

![Generated Designs](/images/designs.png)

![Generated Designs and Habitat used for learning](/images/designs_habitat.png)
