using UnityEngine;

namespace GnosiaCustomizer.utils;
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;

    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CoroutineRunner");
                GameObject.DontDestroyOnLoad(go);
                _instance = go.AddComponent<CoroutineRunner>();
            }
            return _instance;
        }
    }
}
