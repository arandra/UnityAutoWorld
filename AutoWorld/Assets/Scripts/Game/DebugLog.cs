using System;
using AutoWorld.Core;
using UnityEngine;

namespace AutoWorld.Game
{
    public sealed class DebugLog : MonoBehaviour, IDebugLog
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Log(string message)
        {
            Debug.Log(message);
        }
    }
}
