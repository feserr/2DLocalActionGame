using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolSystem : MonoBehaviour {

    List<GameObject>list;

    public void Init(int size, GameObject prefab)
    {
        list = new List<GameObject>();
          for(int i = 0 ; i < size; i++)
        {
            GameObject obj = (GameObject)Instantiate(prefab);
            obj.SetActive(false);
            list.Add(obj);
        }
    }

    public GameObject GetObject()
    {
         if(list.Count > 0)
        {
            GameObject obj = list[0];
            list.RemoveAt(0);
            return obj;
        }

        return null;
    }

    public void DestroyObjectPool(GameObject obj)
    {
        list.Add(obj);
        obj.SetActive(false);
    }
}
