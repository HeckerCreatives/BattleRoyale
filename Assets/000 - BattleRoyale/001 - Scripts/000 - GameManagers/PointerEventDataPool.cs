using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PointerEventDataPool
{
    private Stack<PointerEventData> pool = new Stack<PointerEventData>();

    // Get a PointerEventData instance from the pool or create a new one if empty
    public PointerEventData GetPointerEventData()
    {
        if (pool.Count > 0)
        {
            return pool.Pop();
        }
        else
        {
            return new PointerEventData(EventSystem.current); // Create a new one if pool is empty
        }
    }

    // Return a PointerEventData instance back to the pool for reuse
    public void ReturnPointerEventData(PointerEventData pointerEventData)
    {
        pool.Push(pointerEventData);
    }
}

