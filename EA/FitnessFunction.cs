using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace IEA.EA
{
    internal class FitnessFunctionParameters : IFitnessFunctionParameters
    {
        public Problem Problem { get; }

        public int PenaltyCoefficient { get; }

        public bool PenaltyFitnessFunction { get; }


        public static FitnessFunctionParameters FromJSON(string json, Problem problem)
        {
            JsonElement parameters = JsonDocument
                                        .Parse(json)
                                        .RootElement
                                        .GetProperty("FitnessFunctionParameters");
            return new(
                problem,
                parameters.GetProperty(nameof(PenaltyCoefficient)).GetInt32(),
                parameters.GetProperty(nameof(PenaltyFitnessFunction)).GetBoolean()
                );
        }

        public FitnessFunctionParameters(Problem problem, int penaltyCoefficient, bool penaltyFitnessFunction)
        {
            Problem = problem;
            PenaltyCoefficient = penaltyCoefficient;
            PenaltyFitnessFunction = penaltyFitnessFunction;
            Check();
        }

        void Check()
        {
            Debug.Assert(PenaltyCoefficient > 0);
        }
    }
    internal class FitnessFunction : IFitnessFunction
    {
        private readonly FitnessFunctionParameters Parameters;
        private readonly int[,] BestGroupPreferenceMapping;
        private readonly List<List<int>> GroupDays;
        private readonly int[] GroupFinish;

        public FitnessFunction(FitnessFunctionParameters parameters)
        {
            Parameters = parameters;
            BestGroupPreferenceMapping = GenerateBestGroupPreferenceMapping();
            GroupDays = AssignGroupsToDays();
            GroupFinish = CalculateGroupFinish();
        }
        public void Evaluate(List<Solution> solutions)
        {
            for (int i = 0; i < solutions.Count; i++)
            {
                if (solutions[i].HasChanged)
                {
                    int fitness = 0;
                    fitness += CalculateTotalDissapointment(solutions[i]);
                    if (Parameters.PenaltyFitnessFunction) fitness += CalculatePenalty(solutions[i]);
                    solutions[i].Fitness = fitness;
                }

            }
        }

        private int CalculateTotalDissapointment(Solution solution)
        {
            int objective = 0;
            int[] breakDissapointment = CalculateBreakDissapointment(solution);
            int[] preferenceDissapointment = CalculatePreferenceDisapointment(solution);
            for (int s = 0; s < Parameters.Problem.StudentsCount; s++)
            {
                int unitDissapointment = Parameters.Problem.BreakPreferences[s]*breakDissapointment[s] + (10 - Parameters.Problem.BreakPreferences[s]) * preferenceDissapointment[s];
                objective += CeilDivisionSquared(unitDissapointment, 10);
            }
            return objective;
        }

        private int[] CalculateBreakDissapointment(Solution solution)
        {
            int[] breakDissapointment = new int[Parameters.Problem.StudentsCount];

            bool[,] boolChannel = solution.ChannelBooleanRepresentation();

            for (int s = 0; s < Parameters.Problem.StudentsCount; s++)
            {
                for (int d = 0; d <  Parameters.Problem.DaysCount; d++)
                {
                    List<int> dayGropusAttened = GroupDays[d].FindAll(g => boolChannel[s, g]);
                    if (dayGropusAttened.Any())
                    {
                        int min = dayGropusAttened.Min(g => Parameters.Problem.GroupStart[g]);
                        int max = dayGropusAttened.Max(g => GroupFinish[g]);
                        int timeSpent = dayGropusAttened.Sum(g => Parameters.Problem.ClassDuration[Parameters.Problem.GroupClass[g]]);

                        breakDissapointment[s] += Math.Max(0, max - min - timeSpent);
                    }
                }
            }
            return breakDissapointment;
        }

        private int[] CalculatePreferenceDisapointment(Solution solution)
        {
            int[] preferenceDissapointment = new int[Parameters.Problem.StudentsCount];

            for (int s = 0; s < Parameters.Problem.StudentsCount; s++)
            {
                for (int c = 0; c < Parameters.Problem.ClassesCount; c++)
                {
                    if (solution.Assignment[s, c] != -1)
                    {
                        preferenceDissapointment[s] += BestGroupPreferenceMapping[s, c] - Parameters.Problem.GroupPreferences[s, solution.Assignment[s, c]];
                    }
                }
            }

            return preferenceDissapointment;
        } 



        private int CalculatePenalty(Solution solution)
        {
            return solution.ConstraintsViolated * Parameters.PenaltyCoefficient;
        }

        private int[,] GenerateBestGroupPreferenceMapping()
        {
            int[,] bestGroupPreference = new int[Parameters.Problem.StudentsCount, Parameters.Problem.ClassesCount];
            for (int s = 0; s < Parameters.Problem.StudentsCount; s++)
            {
                for (int c = 0; c < Parameters.Problem.ClassesCount; c++)
                {
                    bestGroupPreference[s, c] = Parameters.Problem.ClassGroups[c].Max(g => Parameters.Problem.GroupPreferences[s, g]);
                }
            }
            return bestGroupPreference;
        }

        private List<List<int>> AssignGroupsToDays()
        {
            List<List<int>> daysAssignment = new List<List<int>>();
            for (int d = 0; d < Parameters.Problem.DaysCount; d++)
            {
                daysAssignment.Add(new List<int>());
            }
            for (int g = 0; g < Parameters.Problem.GroupCount; g++)
            {
                daysAssignment[Parameters.Problem.GroupDay[g]].Add(g);
            }

            return daysAssignment;
        }

        private int[] CalculateGroupFinish()
        {
            int[] groupFinish = new int[Parameters.Problem.GroupCount];
            for (int g = 0; g < Parameters.Problem.GroupCount; g++)
            {
                groupFinish[g] = Parameters.Problem.GroupStart[g] + Parameters.Problem.ClassDuration[Parameters.Problem.GroupClass[g]];
            }
            return groupFinish;
        }

        private static int CeilDivisionSquared(int a, int b)
        {
            int x = (a + b - 1) / b;
            return x * x;
        }
    }
}
