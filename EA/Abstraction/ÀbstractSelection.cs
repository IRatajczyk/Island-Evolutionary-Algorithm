using IEA.ProblemInstance;

namespace IEA.EA.Abstraction
{
    internal interface ISelectionParameters
    {

    }

    internal interface ISelection
    {
        public abstract List<Solution> SelectPopulation(List<Solution> population, int populationSize);

        public abstract List<Solution> SelectElite(List<Solution> population, int eliteCount);

    }
}
