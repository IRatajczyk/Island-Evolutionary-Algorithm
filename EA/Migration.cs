using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IEA.EA
{

    internal class MigrationParameters : IMigrationParameters
    {
        public double MigrationPropability { get; }
        public int MigrantsCount { get; }
        public double ChoicePopabilityFactor { get; }
        public bool SendCopy { get; }

        public static MigrationParameters FromJSON(string json)
        {
            JsonElement parameters = JsonDocument
                            .Parse(json)
                            .RootElement
                            .GetProperty("MigrationParameters");
            return new(
                parameters.GetProperty(nameof(MigrationPropability)).GetDouble(),
                parameters.GetProperty(nameof(MigrantsCount)).GetInt32(),
                parameters.GetProperty(nameof(ChoicePopabilityFactor)).GetDouble(),
                parameters.GetProperty(nameof(SendCopy)).GetBoolean()
                );
        }
        public MigrationParameters(double migrationPropability, int migrantsCount, double choicePopabilityFactor, bool sendCopy)
        {
            MigrationPropability = migrationPropability;
            MigrantsCount = migrantsCount;
            ChoicePopabilityFactor = choicePopabilityFactor;
            SendCopy = sendCopy;
            Check();
        }

        private void Check()
        {
            Debug.Assert(MigrationPropability > 0 && MigrationPropability < 1);
            Debug.Assert(ChoicePopabilityFactor > 0 && ChoicePopabilityFactor < 1);
            Debug.Assert(MigrantsCount > 0);
        }
    }
    internal class Migration : IMigration
    {

        private readonly MigrationParameters Parameters;

        public Migration(MigrationParameters parameters)
        {
            Parameters = parameters;
        }
        public List<Solution> Migrate(List<Solution> population)
        {
            Random random = new();
            List<int> solutionsToSend = new();
            
            if (random.NextDouble() > Parameters.MigrationPropability) return new();
            
            double p = Parameters.ChoicePopabilityFactor;
            for (int i = 0; i < population.Count; i++)
            {

                if (new Random().NextDouble() < p) solutionsToSend.Add(i);
                if (solutionsToSend.Count == Parameters.MigrantsCount) break;

            }

            List<Solution> migrants = new();
            foreach (int i in solutionsToSend)
            {
                migrants.Add(population[i]);
            }
            if (!Parameters.SendCopy){
                for (int i = migrants.Count - 1; i >= 0; i--)
                {
                    population.Remove(migrants[i]);
                }
            }
            return migrants;
        }
    }
}
