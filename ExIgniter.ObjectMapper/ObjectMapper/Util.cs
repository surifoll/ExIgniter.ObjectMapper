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
            if (source == null || target == null) return 0;
            if (source.Length == 0 || target.Length == 0) return 0;
            if (source == target) return source.Length;

            var sourceWordCount = source.Length;
            var targetWordCount = target.Length;

            // Step 1
            if (sourceWordCount == 0)
                return targetWordCount;

            if (targetWordCount == 0)
                return sourceWordCount;

            var distance = new int[sourceWordCount + 1, targetWordCount + 1];

            // Step 2
            for (var i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (var j = 0; j <= targetWordCount; distance[0, j] = j++) ;

            for (var i = 1; i <= sourceWordCount; i++)
            for (var j = 1; j <= targetWordCount; j++)
            {
                // Step 3
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;

                // Step 4
                distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }

            return distance[sourceWordCount, targetWordCount];
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