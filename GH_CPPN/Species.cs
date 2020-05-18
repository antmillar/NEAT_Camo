using System;
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
        public double MinFitness { get; set; }
        public Genome Representative {get; set;}
        public List<double> FitnessHistory { get; set; } = new List<double>();

        public int GenerationsSinceImprovement { get; set; } = 0;
        public bool Stagnant { get; internal set; }

        public Species()
        {
            ID = Config.speciesID++;
        }
        public void UpdateFitness()
        {
            //if (Stagnant == true) return;

            Fitness = Genomes.Count > 0 ? Math.Round(Genomes.Select(x => x.Fitness).Average(), 2) : 0.0;
            MinFitness = Genomes.Count > 0 ? Math.Round(Genomes.Select(x => x.Fitness).Min(), 2) : 0.0; //how to do with max?
        }

        public void UpdateRepresentative()
        {
            //if (Stagnant == true) return;

            Representative = Genomes.Count > 0 ? Genomes[0] : Representative;
        }

        public void Add(Genome genome)
        {
            Genomes.Add(genome);
            genome.SpeciesID = this.ID;
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

                    //should have a max number of species maybe
                    if (distances.Min() > 5)
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

            var emptySpecies = new List<Species>();
            //calc fitness of each
            foreach (var species in SpeciesList)
            {
                if (species.Genomes.Count == 0)
                {
                    emptySpecies.Add(species);

                }
                else
                {

                    //re-assign the representative to the fittest genome in each generation
                    species.UpdateRepresentative();

                    var prevFitness = species.Fitness;
                    species.FitnessHistory.Add(prevFitness);
                    var bestFitness = species.FitnessHistory.Min();

                    species.UpdateFitness();
                    var currFitness = species.Fitness;

                    if (currFitness < bestFitness)
                    {
                        species.GenerationsSinceImprovement = 0;
                    }
                    else
                    {
                        species.GenerationsSinceImprovement++;
                    }

                    if (species.GenerationsSinceImprovement > 50)
                    {
                        species.Stagnant = true;
                    }
                }
            }

            foreach (var s in emptySpecies)
            {
                SpeciesList.Remove(s);
            }

            SpeciesList = SpeciesList.OrderBy(x => x.MinFitness).ToList();

            if (SpeciesList.Count > 5)
            {
                //take the remainig species
                var weakerSpecies = SpeciesList.Skip(5).Take(SpeciesList.Count - 5).ToList();

                //replacing weaker species with new blank genomes
                foreach(var spec in weakerSpecies)
                {
                    if(spec.GenerationsSinceImprovement > 50)
                    {
                        for (int i = 0; i < spec.Genomes.Count; i++)
                        {
                           spec.Genomes[i] = new Genome();
                        }
                        SpeciesList.Remove(spec);
                    }
                }
            }

            return SpeciesList;
        }
    }
}
