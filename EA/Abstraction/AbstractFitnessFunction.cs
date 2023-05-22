using IEA.ProblemInstance;

namespace IEA.EA.Abstraction
{
    internal interface IFitnessFunctionParameters
    {
        public int PenaltyCoefficient { get; }
    }
    internal interface IFitnessFunction
    {
        public abstract void Evaluate(List<Solution> solutions);
    }
}
