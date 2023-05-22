using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using System.Diagnostics;
using System.Text.Json;

namespace IEA.EA
{

    internal class TournamentSelectionParameters : ISelectionParameters
    {
        public int TournamentSize { get; }
        public double BestPropability { get; }

        public static TournamentSelectionParameters FromJSON(string json)
        {
            JsonElement parameters = JsonDocument
                            .Parse(json)
                            .RootElement
                            .GetProperty("TournamentSelectionParameters");
            return new
                (
                parameters.GetProperty(nameof(TournamentSize)).GetInt32(),
                parameters.GetProperty(nameof(BestPropability)).GetDouble()
                );
        }

        public TournamentSelectionParameters(
            int tournamentSize,
            double bestPropability
            )
        {
            TournamentSize = tournamentSize;
            BestPropability = bestPropability;
            Check();
        }

        private void Check()
        {
            Debug.Assert(TournamentSize > 0);
            Debug.Assert(BestPropability > 0 && BestPropability < 1);
        }
    }

    internal class TournamentSelection : ISelection
    {

        private readonly TournamentSelectionParameters Parameters;

        public TournamentSelection(TournamentSelectionParameters SelectionParameters)
        {
            Parameters = SelectionParameters;
        }

        public List<Solution> SelectPopulation(List<Solution> population, int populationSize)
        {
            List<Solution> selectedPopulation = new List<Solution>();
            Random random = new Random();
            while (selectedPopulation.Count < populationSize)
            {
                List<Solution> participants = population.OrderBy(x => random.Next()).Take(Parameters.TournamentSize).ToList();
                double p = Parameters.BestPropability;
                foreach (Solution participant in participants.OrderBy(x => x.Fitness).ToList())
                {
                    if (random.NextDouble() < p)
                    {
                        selectedPopulation.Add(participant);
                        break;
                    }
                    p *= (1 - Parameters.BestPropability);
                }

            }
            return selectedPopulation;
        }

        public List<Solution> SelectElite(List<Solution> population, int eliteCount)
        {
            return population.OrderBy(x => x.Fitness).Take(eliteCount).ToList();
        }
    }
}
