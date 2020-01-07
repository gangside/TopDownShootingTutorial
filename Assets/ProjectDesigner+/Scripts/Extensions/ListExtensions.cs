using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

public static class IListExtensions
{
    public static void Swap<T>(
        this IList<T> list,
        int firstIndex,
        int secondIndex
    )
    {
        Contract.Requires(list != null);
        Contract.Requires(firstIndex >= 0 && firstIndex < list.Count);
        Contract.Requires(secondIndex >= 0 && secondIndex < list.Count);
        if (firstIndex == secondIndex)
        {
            return;
        }
        T temp = list[firstIndex];
        list[firstIndex] = list[secondIndex];
        list[secondIndex] = temp;
    }

    public static T LastItem<T>(this IList<T> list)
    {
        return list[list.Count - 1];
    }

    public static List<Vector3> AddAll(this IList<Vector3> list,Vector3[] arr)
    {
        Contract.Requires(list != null);
        Contract.Requires(arr != null);

        Contract.Requires(arr.Length>0);


        List<Vector3> lst = new List<Vector3>(list);
        foreach (var loc in arr)
            lst.Add(loc);
        return lst;
    }

   
}
