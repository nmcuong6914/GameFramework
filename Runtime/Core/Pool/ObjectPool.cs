using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialSize = 5;
    private Queue<GameObject> pool = new Queue<GameObject>();
    private HashSet<GameObject> activeObjects = new HashSet<GameObject>(); // Track active objects
    private Transform poolParent;

    public void Init(GameObject prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.initialSize = initialSize;
        poolParent = parent;
        for (int i = 0; i < initialSize; i++)
        {
            var go = Object.Instantiate(prefab, poolParent);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    public GameObject Get()
    {
        GameObject go = pool.Count > 0 ? pool.Dequeue() : Object.Instantiate(prefab, poolParent);
        go.SetActive(true);
        activeObjects.Add(go); // Track as active
        return go;
    }

    public void Release(GameObject go)
    {
        if (go == null) return;
        
        activeObjects.Remove(go); // Remove from active tracking
        
        // Re-parent to pool parent to keep hierarchy clean (before deactivating)
        if (poolParent != null)
        {
            go.transform.SetParent(poolParent, false); // false = keep local position/rotation
        }
        
        go.SetActive(false);
        pool.Enqueue(go);
    }

    /// <summary>
    /// Returns all currently active objects back to the pool
    /// </summary>
    public void ReturnAllActive()
    {
        var activeList = new List<GameObject>(activeObjects); // Create a copy to avoid modification during iteration
        foreach (var activeObj in activeList)
        {
            if (activeObj != null)
            {
                Release(activeObj);
            }
        }
    }

    public int Count => pool.Count;
    public int ActiveCount => activeObjects.Count;
}
