using System.Collections.Generic;

public static class CollectionExt
{
    #region ILIST
    public static void Enqueue<T>(this IList<T> list, T t)
    {
        list.Add(t);
    }

    public static T Dequeue<T>(this IList<T> list)
    {
        T t = list[0];
        list.RemoveAt(0);
        return t;
    }

    public static T Peek<T>(this IList<T> list)
    {
        return list[0];
    }

    public static bool IsNullOrEmpty<T>(this IList<T> list)
    {
        return list == null || list.Count == 0;
    }

    public static T Last<T>(this IList<T> list)
    {
        return list[list.Count - 1];
    }

    public static void Swap<T>(this IList<T> list, int i1, int i2)
    {
        (list[i1], list[i2]) = (list[i2], list[i1]);
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        System.Random rng = new();
        int n = list.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static void ShuffleRange<T>(this IList<T> ts, int startIndex, int endIndex)
    {
        System.Random rng = new();

        for (int i = endIndex; i > startIndex; i--)
        {
            int j = rng.Next(startIndex, i + 1);
            (ts[i], ts[j]) = (ts[j], ts[i]);
        }
    }

    public static void Rotate<T>(this List<T> list, int position)
    {
        int count = list.Count;
        position = position % count;
        if (position == 0) return;
        if (position < 0) position += count;
        list.Reverse();
        list.Reverse(0, position);
        list.Reverse(position, count - position);
    }

    public static int SelectWeightedRandomIndex(this IList<int> probabilities)
    {
        int totalWeight = 0;

        for (int i = 0; i < probabilities.Count; i++)
        {
            totalWeight += probabilities[i];
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        for (int i = 0; i < probabilities.Count; i++)
        {
            cumulativeWeight += probabilities[i];

            if (randomValue < cumulativeWeight)
            {
                return i;
            }
        }

        return UnityEngine.Random.Range(0, probabilities.Count);
    }
    #endregion

    #region LIST COMBINATIONS
    public static List<List<T>> GetCombinations_1<T>(this List<T> list, int k)
    {
        List<List<T>> result = new List<List<T>>();
        List<T> currentCombination = new List<T>();

        GenerateCombinations_1(list, k, 0, currentCombination, result);
        return result;
    }

    private static void GenerateCombinations_1<T>(List<T> list, int k, int startIndex, List<T> currentCombination, List<List<T>> result)
    {
        if (currentCombination.Count == k)
        {
            result.Add(new List<T>(currentCombination));
            return;
        }

        for (int i = startIndex; i < list.Count; i++)
        {
            currentCombination.Add(list[i]);
            GenerateCombinations_1(list, k, i + 1, currentCombination, result);
            currentCombination.RemoveAt(currentCombination.Count - 1);
        }
    }

    public static IEnumerable<List<T>> GetCombinations_2<T>(this List<T> list, int k)
    {
        return GenerateCombinations_2(list, k, 0, new List<T>());
    }

    static IEnumerable<List<T>> GenerateCombinations_2<T>(List<T> list, int k, int startIndex, List<T> currentCombination)
    {
        if (currentCombination.Count == k)
        {
            yield return new List<T>(currentCombination);
            yield break;
        }

        for (int i = startIndex; i < list.Count; i++)
        {
            currentCombination.Add(list[i]);
            foreach (var combination in GenerateCombinations_2(list, k, i + 1, currentCombination))
            {
                yield return combination;
            }
            currentCombination.RemoveAt(currentCombination.Count - 1);
        }
    }
    #endregion

    public static bool IsNullOrEmpty<T>(this Queue<T> queue)
    {
        return queue == null || queue.Count == 0;
    }

    public static bool IsNullOrEmpty<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
    {
        return dictionary == null || dictionary.Count == 0;
    }
}
