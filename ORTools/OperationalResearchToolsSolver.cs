using IEA.ProblemInstance;
using System;
using Google.OrTools.Sat;
using OperationsResearch.Pdlp;
using System.ComponentModel;
using System.Collections.Concurrent;

namespace IEA.ORTools
{

    internal class ORToolsSolverWrapper
    {
        private readonly OperationalResearchToolsSolver solver;
        private readonly BlockingCollection<Solution> buffer;
        public Action<Solution> OnSolutionReturned { get; set; }
        public ORToolsSolverWrapper(bool[,] Mask, Problem problem) 
        { 
            solver = new OperationalResearchToolsSolver(Mask, problem); 
            buffer = new BlockingCollection<Solution>();
        }

        public bool CheckProblemFeasibility()
        {
            return solver.SolveSatify();
        }

        public void RequestSolveMinize(Solution solution)
        {
            buffer.Add(solution);
        }

        private void SolveMinimize(Solution solution)
        {
            buffer.Add(solution);
            Solution? optimal = solver.SolveMinimize(solution);
            if (optimal != null) OnSolutionReturned(optimal);
        }

        public void Run()
        {
            while (true)
            {
                if (buffer.Any()) SolveMinimize(buffer.Take());
                
            }

        }
    }
    internal class OperationalResearchToolsSolver

    {
        readonly private bool[,] Mask;
        readonly private Problem Problem;
        public OperationalResearchToolsSolver(bool[,] mask, Problem problem)
        {
            Mask = mask;
            Problem = problem;
        }

        public bool SolveSatify()
        {
            CpSolver cpSolver = new();
            CpModel model = new();

            BoolVar[,] attend_group = InitializeSolution(model);

            AddCannotAttendExcludedGroup(model, attend_group);
            AddCannotAttendConflictedGroups(model, attend_group);
            AddLimitedGroupSize(model, attend_group);
            AddMustAttend(model, attend_group);

            CpSolverStatus status = cpSolver.Solve(model);
            return status == CpSolverStatus.Feasible || status == CpSolverStatus.Optimal;
        }

        

        public Solution? SolveMinimize(Solution solution)
        {
            CpSolver cpSolver = new();
            CpModel model = new();

            BoolVar[,] attend_group = InitializeSolution(model);

            AddCannotAttendExcludedGroup(model, attend_group);
            AddCannotAttendConflictedGroups(model, attend_group);
            AddLimitedGroupSize(model, attend_group);
            AddMustAttend(model, attend_group);

            AddFollowMask(model, attend_group, solution);

            AddObjective(model, attend_group);

            CpSolverStatus status = cpSolver.Solve(model);

            bool[,] optimalSolution = new bool[Problem.StudentsCount, Problem.GroupCount];

            if (status == CpSolverStatus.Feasible || status == CpSolverStatus.Optimal)
            {
                for (int s = 0; s < Problem.StudentsCount; s++)
                {
                    for (int g = 0; g < Problem.GroupCount; g++)
                    {
                        optimalSolution[s, g] = cpSolver.BooleanValue(attend_group[s, g]);
                    }
                }
                return Solution.FromBooleanRepresentation(optimalSolution, Problem);
            }

            return null;
        }


        private BoolVar[,] InitializeSolution(CpModel model)
        {
            BoolVar[,] attend_group = new BoolVar[Problem.StudentsCount, Problem.GroupCount];

            for (int s = 0; s < Problem.StudentsCount; s++)
            {
                for (int g = 0; g < Problem.GroupCount; g++)
                {
                    attend_group[s, g] = model.NewBoolVar($"s{s}g{g}");
                }
            }
            return attend_group;
        }

        private void AddCannotAttendExcludedGroup(CpModel model, BoolVar[,] attend_group)
        {
            for (int s = 0; s < Problem.StudentsCount; s++)
            {
                for (int g = 0; g < Problem.GroupCount; g++)
                {
                    if (Problem.GroupPreferences[s, g] == -1) model.Add(attend_group[s, g] == 0);
                }
            }
        }

        private void AddCannotAttendConflictedGroups(CpModel model, BoolVar[,] attend_group)
        {
            for (int s = 0; s < Problem.StudentsCount; s++)
            {
                for (int g1 = 0; g1 < Problem.GroupCount; g1++)
                {
                    for (int g2 = g1 + 1; g2 < Problem.GroupCount; g2++)
                    {
                        if (Problem.GroupConflict[g1, g2] || Problem.GroupClass[g1] == Problem.GroupClass[g2]) model.Add(attend_group[s, g1] + attend_group[s, g2] <= 1);

                    }
                }
            }
        }

        private void AddLimitedGroupSize(CpModel model, BoolVar[,] attend_group)
        {
            LinearExpr[] attendantsCount = new LinearExpr[Problem.GroupCount];
            for (int g = 0; g < Problem.GroupCount; g++)
            {
                attendantsCount[g] = model.NewIntVar(0, Problem.StudentsCount, $"g{g}count");
                for (int s = 0; s < Problem.StudentsCount; s++)
                {
                    attendantsCount[g] += attend_group[s, g];
                }
                model.Add(attendantsCount[g] <= Problem.ClassSize[Problem.GroupClass[g]]);

            }
        }

        private void AddMustAttend(CpModel model, BoolVar[,] attend_group)
        {
            for (int s = 0; s < Problem.StudentsCount; s++)
            {
                for (int c = 0; c < Problem.ClassesCount; c++)
                {
                    LinearExpr attendingSum = model.NewIntVar(0, 10, $"s{s}gc{c}count");
                    foreach (int g in Problem.ClassGroups[c])
                    {
                        attendingSum += attend_group[s, g];
                    }
                   
                    model.Add(attendingSum == (Problem.MustAttend[s, c] ? 1 : 0));
                }
            }
        }

        private void AddFollowMask(CpModel model, BoolVar[,] attend_group, Solution solution)
        {
            bool[,] booleanRepresentation = solution.ChannelBooleanRepresentation();
            
            for (int s = 0; s < Problem.StudentsCount; s++)
            {
                for (int g = 0; g < Problem.GroupCount; g++)
                {
                    if (Mask[s, g]) model.Add(attend_group[s, g] == (booleanRepresentation[s,g] ? 1 : 0));
                }
            }
        }

        private void AddObjective(CpModel model, BoolVar[,] attend_group)
        {
            // TODO : add objective
            LinearExpr objective = model.NewIntVar(0, long.MaxValue, "objective");
            for (int s = 0; s < Problem.StudentsCount; s++)
            {
                for (int g = 0; g < Problem.GroupCount; g++)
                {
                    objective += Problem.GroupPreferences[s, g] * attend_group[s, g];
                }
            }
            model.Minimize(objective);
        }
    }
}
