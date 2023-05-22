namespace IEA.ProblemInstance
{
    internal class Solution
    {

        private int fitness;
        private int[,] assignment;
        private bool? isFeasible;
        private Problem Problem { get; }

        public int ConstraintsViolated { get; private set; }
        public int[,] Assignment
        {
            get => assignment;
            set
            {
                assignment = value;
                fitness = int.MaxValue;
                HasChanged = true;

            }
        }
        public bool? IsFeasible
        {
            get => isFeasible ??= CalculateFeasibility();
        }
        public int Fitness
        {
            get => fitness;
            set
            {
                fitness = value;
                HasChanged = false;
            }
        }

        public bool HasChanged { get; private set; }

        public static Solution FromBooleanRepresentation(bool[,] attend_group, Problem problem)
        {
            int[,] assignment = new int[problem.StudentsCount, problem.ClassesCount];
            for (int s = 0; s < problem.StudentsCount; s++)
            {
                for (int c = 0; c < problem.ClassesCount; c++)
                {
                    assignment[s, c] = -1;
                }
            }
            for (int s = 0; s < problem.StudentsCount; s++)
            {
                for (int g = 0; g < problem.GroupCount; g++)
                {
                    if (attend_group[s, g]) assignment[s, problem.GroupClass[g]] = g;
                }
            }
            return new Solution(problem, assignment);
        }

        public Solution(Problem Problem, int[,] Assignment)
        {
            this.Problem = Problem;
            this.Assignment = Assignment;
            this.assignment = Assignment;
        }

        public void CastSemiFeasible()
        {
            Random random = new();
            for (int s = 0; s < Problem.StudentsCount; s++)
            {
                for (int c = 0; c < Problem.ClassesCount; c++)
                {
                    if (Problem.MustAttend[s, c] && Assignment[s, c] < 0)
                    {
                        Assignment[s, c] = Problem.ClassGroups[c][random.Next(Problem.ClassGroups[c].Count)];
                    }
                    else if (!Problem.MustAttend[s, c] && Assignment[s, c] >= 0)
                    {
                        Assignment[s, c] = -1;
                    }
                    else if (!Problem.ClassGroups[c].Contains(Assignment[s, c]))
                    {
                        Assignment[s, c] = Problem.ClassGroups[c][random.Next(Problem.ClassGroups[c].Count)];
                    }
                }
            }
        }

        public bool[,] ChannelBooleanRepresentation()
        { 
            CastSemiFeasible();
            bool[,] booleanRepresentation = new bool[Problem.StudentsCount, Problem.GroupCount];
            for (int s = 0; s < Problem.StudentsCount; s++)
            {
                for (int g = 0; g < Problem.GroupCount; g++)
                {
                    booleanRepresentation[s, g] = false;
                }

                for (int c = 0; c < Problem.ClassesCount; c++)
                {
                    if (Assignment[s, c] > 0) booleanRepresentation[s, Assignment[s, c]] = true;
                }
            }
            return booleanRepresentation;
        }

        private bool CalculateFeasibility()
        {
            return CountConstraintViolations() == 0;
        }

        private int CountConstraintViolations()
        {
            int count = 0;

            int[] groupParticipants = new int[Problem.GroupCount];

            Array.Fill(groupParticipants, 0);


            bool[,] booleanRepresentation = ChannelBooleanRepresentation();


            for (int s = 0; s < Problem.StudentsCount; s++)
            {
                for (int c = 0; c < Problem.ClassesCount; c++)
                {
                    if (Problem.MustAttend[s, c] && Assignment[s, c] == -1) count++;
                }

                for (int g = 0; g < Problem.GroupCount; g++)
                {
                    if (booleanRepresentation[s, g]) groupParticipants[g]++;
                    if (Problem.GroupPreferences[s, g] == -1 && booleanRepresentation[s, g]) count++;


                    for (int g2 = g + 1; g2 < Problem.GroupCount; g2++)
                    {
                        if (Problem.GroupConflict[g, g2] && booleanRepresentation[s, g] && booleanRepresentation[s, g2]) count++;
                    }

                }
            }
            for (int g = 0; g < Problem.GroupCount; g++)
            {
                int c = Problem.ClassSize[Problem.GroupClass[g]] - groupParticipants[g];
                if (c < 0) count -= c;
            }

            ConstraintsViolated = count;
            return count;
        }

    }
}
