using System;
using System.Collections.Generic;

public static class ArrayExtensions {
    public static void ShiftElement<T>(this T[] array, int oldIndex, int newIndex)
    {
        // TODO: Argument validation
        if (oldIndex == newIndex)
        {
            return; // No-op
        }
        T tmp = array[oldIndex];
        if (newIndex < oldIndex) 
        {
            // Need to move part of the array "up" to make room
            Array.Copy(array, newIndex, array, newIndex + 1, oldIndex - newIndex);
        }
        else
        {
            // Need to move part of the array "down" to fill the gap
            Array.Copy(array, oldIndex + 1, array, oldIndex, newIndex - oldIndex);
        }
        array[newIndex] = tmp;
    }

    public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
    {
        var item = list[oldIndex];

        list.RemoveAt(oldIndex);

        if (newIndex > oldIndex) newIndex--;
        // the actual index could have shifted due to the removal

        list.Insert(newIndex, item);
    }

    public static void Move<T>(this List<T> list, T item, int newIndex)
    {
        if (item != null)
        {
            var oldIndex = list.IndexOf(item);
            if (oldIndex > -1)
            {
                list.RemoveAt(oldIndex);

                if (newIndex > oldIndex) newIndex--;
                // the actual index could have shifted due to the removal

                list.Insert(newIndex, item);
            }
        }

    }
}