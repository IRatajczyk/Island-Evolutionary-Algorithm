namespace IEA.EA.Abstraction
{
    internal interface IEvolutionaryAlgorithmParameters
    {
        public int PopulationSize { get; }
        public int EliteCount { get; }

        public bool AllowElitism { get; }

    }
    internal interface IEvolutionaryAlgorithm
    {
        public void Run();
    }
}
