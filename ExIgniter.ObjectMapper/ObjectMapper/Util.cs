using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExIgniter.ObjectMapper.ObjectMapper
{
    public static class Util
    {
        public static double CalculateSimilarity(string source, string target)
        {
            if (source == null || target == null) return 0.0;
            if (source.Length == 0 || target.Length == 0) return 0.0;
            if (source == target) return 1.0;

            var stepsToSame = ComputeLevenshteinDistance(source, target);
            return 1.0 - stepsToSame / (double) Math.Max(source.Length, target.Length);
        }

        private static int ComputeLevenshteinDistance(string source, string target)
        {
            const int MaxLength = 256; // Adjust as needed for performance

            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return 0;

            if (source == target)
                return 0;

            if (source.Length > MaxLength || target.Length > MaxLength)
                return int.MaxValue; // consider them too dissimilar to match

            int sourceLen = source.Length;
            int targetLen = target.Length;

            var distance = new int[sourceLen + 1, targetLen + 1];

            for (int i = 0; i <= sourceLen; i++) distance[i, 0] = i;
            for (int j = 0; j <= targetLen; j++) distance[0, j] = j;

            for (int i = 1; i <= sourceLen; i++)
            {
                for (int j = 1; j <= targetLen; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceLen, targetLen];
        }


        internal static IGrouping<double, MapEngineObj> CalculateSimilarity(string name, IEnumerable<string> enumerable)
        {
            var pointList = new List<MapEngineObj>();
            foreach (var st in enumerable)
                pointList.Add(new MapEngineObj
                {
                    Point = CalculateSimilarity(name, st),
                    DestPropertName = st,
                    SrcPropertName = name
                });

            var max = pointList.GroupBy(e => e.Point).Max();

            return max;
        }

        public static IEnumerable<MapEngineObj> CalculateSimilarity(string propertyName,
            List<PropertyInfo> destProperties)
        {
            var pointList = new List<MapEngineObj>();
            foreach (var st in destProperties)
                pointList.Add(new MapEngineObj
                {
                    Point = CalculateSimilarity(propertyName, st.Name),
                    DestPropertName = st.Name,
                    SrcPropertName = propertyName
                });

            var max = pointList.Where(e => e.Point == pointList.Max(d => d.Point));

            return max;
        }
    }
}