/*
 * @create:  李锦俊 
 * @email: mybios@qq.com
 * 全局唯一的Mono状态派发
 * */
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;


///Singleton. Automatically added when needed, collectively calls methods that needs updating amongst other things relevant to MonoBehaviours
public class MonoManager : MonoBehaviour
{

    //These can be used by the user, or when un/subscribe is not regular.
    public UnityEvent onUpdate = new UnityEvent();
    public UnityEvent onLateUpdate = new UnityEvent();
    public UnityEvent onFixedUpdate = new UnityEvent();
    public UnityEvent onApplicationQuit = new UnityEvent();

    private static bool isQuiting;

    private static MonoManager _current;
    public static MonoManager Instance
    {
        get
        {
            if (_current == null && !isQuiting)
            {
                _current = FindObjectOfType<MonoManager>();
                if (_current == null)
                {
                    _current = new GameObject("_MonoManager").AddComponent<MonoManager>();
                }
            }
            return _current;
        }
    }


    ///Creates the MonoManager singleton
    public static void Create() { _current = Instance; }

    void OnApplicationQuit()
    {
        isQuiting = true;
        onApplicationQuit.Invoke();
    }

    void Awake()
    {
        if (_current != null && _current != this)
        {
            DestroyImmediate(this.gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        _current = this;
    }

    public void Update()
    {
        onUpdate.Invoke();
    }

    public void LateUpdate()
    {
        onLateUpdate.Invoke();
    }

    public void FixedUpdate()
    {
        onFixedUpdate.Invoke();
    }
}
