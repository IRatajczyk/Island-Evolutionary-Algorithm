using IEA.ProblemInstance;

namespace IEA.EA.Abstraction
{
    internal interface ICrossoverParameters
    {
        double CrossoverPropability { get; }

    }

    internal interface ICrossover
    {
        public abstract List<Solution> Cross(List<Solution> solutions);
    }


}
