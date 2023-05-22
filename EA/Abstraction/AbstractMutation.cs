using IEA.ProblemInstance;

namespace IEA.EA.Abstraction
{
    internal interface IMutationParameters
    {
        public double MutationPropability { get; }
    }
    internal interface IMutation
    {
        public abstract void Mutate(List<Solution> solutions);
    }
}
