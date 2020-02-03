using System;
namespace System.Collections.Generic
{
    public static class ListExtensions
    {
        public static void Shuffle<T>(this List<T> list, Random random = null)
        {
            if (random == null)
            {
                random = new Random((int)DateTime.UtcNow.Ticks);
                int n = list.Count;
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    T value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
            }
        }

        public static void Update<T>(this List<T> list, Action<T> updateFunction)
        {

        }
    }
}
