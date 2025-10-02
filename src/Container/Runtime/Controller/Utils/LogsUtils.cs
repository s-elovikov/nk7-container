using UnityEngine;

namespace Nk7.Container
{
    public static class LogsUtils
    {
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"<color=green>{message}</color>");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"<color=red>{message}</color>");
        }

        public static void Log(string message)
        {
            Debug.Log($"<color=green>{message}</color>");
        }
    }
}