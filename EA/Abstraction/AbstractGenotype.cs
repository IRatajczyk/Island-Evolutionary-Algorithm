using IEA.ProblemInstance;

namespace IEA.EA.Abstraction
{

    internal interface IGenotypeParameters
    {
        public bool GenerateFeasible { get; }
    }
    internal interface IGenotype
    {
        public abstract List<Solution> GeneratePopulation(int PopulationSize);
    }
}
