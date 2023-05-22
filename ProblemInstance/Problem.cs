using Newtonsoft.Json;
using System.Data;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IEA.ProblemInstance
{
    public class Problem
    {
        readonly public int TimeUnitsPerHour;
        readonly public int StudentsCount;
        readonly public int GroupCount;
        readonly public int[,] GroupPreferences;
        readonly public int[] BreakPreferences;
        readonly public int DaysCount;
        readonly public int ClassesCount;
        readonly public int[] ClassDuration;
        readonly public int[] ClassSize;
        readonly public int[] GroupClass;
        readonly public int[] GroupStart;
        readonly public int[] GroupDay;
        readonly public bool[,] GroupConflict;
        readonly public bool[,] MustAttend;
        readonly public List<List<int>> ClassGroups;

        public static Problem FromJSON(string json)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonDocument = JsonDocument.Parse(json);
                var problem = jsonDocument.RootElement.GetProperty("problem");

                int timeUnitsPerHour = problem.GetProperty("TimeUnitsPerHour").GetInt32();
                int studentsCount = problem.GetProperty("StudentsCount").GetInt32();
                int groupCount = problem.GetProperty("GroupCount").GetInt32();
                int[,] groupPreferences = JsonConvert.DeserializeObject<int[,]>(problem.GetProperty("GroupPreferences").GetRawText());
                int[] breakPreferences = System.Text.Json.JsonSerializer.Deserialize<int[]>(problem.GetProperty("BreakPreferences").GetRawText(), options);
                int daysCount = problem.GetProperty("DaysCount").GetInt32();
                int classesCount = problem.GetProperty("ClassesCount").GetInt32();
                int[] classDuration = System.Text.Json.JsonSerializer.Deserialize<int[]>(problem.GetProperty("ClassDuration").GetRawText(), options);
                int[] classSize = System.Text.Json.JsonSerializer.Deserialize<int[]>(problem.GetProperty("ClassSize").GetRawText(), options);
                int[] groupClass = System.Text.Json.JsonSerializer.Deserialize<int[]>(problem.GetProperty("GroupClass").GetRawText(), options);
                int[] groupStart = System.Text.Json.JsonSerializer.Deserialize<int[]>(problem.GetProperty("GroupStart").GetRawText(), options);
                int[] groupDay = System.Text.Json.JsonSerializer.Deserialize<int[]>(problem.GetProperty("GroupDay").GetRawText(), options);
                bool[,] groupConflict = JsonConvert.DeserializeObject<bool[,]>(problem.GetProperty("GroupConflict").GetRawText());

                return new Problem(
                    timeUnitsPerHour,
                    studentsCount,
                    groupCount,
                    groupPreferences,
                    breakPreferences,
                    daysCount,
                    classesCount,
                    classDuration,
                    classSize,
                    groupClass,
                    groupStart,
                    groupDay,
                    groupConflict
                );
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new ArgumentException("Invalid JSON provided", ex);
            }
        }




        public Problem(
            int timeUnitsPerHour,
            int studentsCount,
            int groupCount,
            int[,] groupPreferences,
            int[] breakPreferences,
            int daysCount,
            int classesCount,
            int[] classDuration,
            int[] classSize,
            int[] groupClass,
            int[] groupStart,
            int[] groupDay,
            bool[,] groupConflict
            )
        {
            TimeUnitsPerHour = timeUnitsPerHour;
            StudentsCount = studentsCount;
            GroupCount = groupCount;
            GroupPreferences = groupPreferences;
            BreakPreferences = breakPreferences;
            DaysCount = daysCount;
            ClassesCount = classesCount;
            ClassDuration = classDuration;
            ClassSize = classSize;
            GroupClass = groupClass;
            GroupStart = groupStart;
            GroupDay = groupDay;
            GroupConflict = groupConflict;

            MustAttend = CalculateMustAttend();
            ClassGroups = CalculateClassGroups();
        }

        private bool[,] CalculateMustAttend()
        {

            bool[,] mustAttend = new bool[StudentsCount, ClassesCount];
            for (int s = 0; s < StudentsCount; s++)
            {
                for (int c = 0; c < ClassesCount; c++)
                {
                    mustAttend[s, c] = false;
                }
            }
            for (int s = 0; s < StudentsCount; s++)
            {
                for (int g = 0; g < GroupCount; g++)
                {
                    if (GroupPreferences[s, g] > 0)
                    {
                        mustAttend[s, GroupClass[g]] = true;
                    }
                }
            }
            return mustAttend;
        }

        private List<List<int>> CalculateClassGroups()
        {
            List<List<int>> classGroups = new();
            for (int c = 0; c < ClassesCount; c++)
            {
                classGroups.Add(new List<int>());
            }
            for (int g = 0; g < GroupCount; g++)
            {
                classGroups[GroupClass[g]].Add(g);
            }
            return classGroups;
        }

    }

}
