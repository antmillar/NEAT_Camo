using System.Collections.Generic;
using System.Linq;
using TopolEvo.NEAT;

namespace TopolEvo.Speciation
{
    public class Species
    {
        public int ID { get; set; }
        public List<Genome> Genomes { get; set; } = new List<Genome>();
        public double Fitness { get; set; }
        public Genome Representative {get; set;}
        public List<double> FitnessHistory { get; set; } = new List<double>();
        public Species()
        {
            ID = Config.speciesID++;
        }
        public void WeightedFitness()
        {
            Fitness = Genomes.Count > 0 ? Genomes.Select(x => x.Fitness).Average() : 0.0;
        }

        public void Add(Genome genome)
        {
            Genomes.Add(genome);
        }

    }

    public class Speciator
    {
        public List<Species> SpeciesList { get; set; } = new List<Species>();

        public List<Species> GenerateSpecies(Population pop)
        {
            var representatives = new List<Genome>();

            foreach(var species in SpeciesList)
            {
                representatives.Add(species.Representative);
                species.Genomes.Clear();
            }

            foreach(var genome in pop.Genomes)
            {
                //if no species created yet, create one
                if(SpeciesList.Count == 0)
                {
                    var species = new Species();
                    species.Add(genome);
                    SpeciesList.Add(species);

                    species.Representative = genome;
                    representatives.Add(species.Representative);
                }
                else
                {
                    var distances = new List<double>();

                    foreach (var genomeRep in representatives)
                    {
                        var distance = genome.Distance(genomeRep);
                        distances.Add(distance);
                    }

                    var closest = distances.IndexOf(distances.Min());

                    if (distances.Min() > 0.25)
                    {
                        var species = new Species();
                        species.Add(genome);
                        SpeciesList.Add(species);

                        species.Representative = genome;
                        representatives.Add(species.Representative);

                    }
                    else
                    {
                        SpeciesList[closest].Add(genome);
                    }
                }
            }

            //calc fitness of each
            foreach (var species in SpeciesList)
            {
                species.FitnessHistory.Add(species.Fitness);
                species.WeightedFitness();
            }

            return SpeciesList;
        }
    }
}
