using System;
using System.IO;
using UnityEngine;

namespace CubeChallenge3D.Save
{
    public static class SaveService
    {
        public static T LoadJson<T>(string fileName, T defaultValue)
        {
            string path = GetPath(fileName);
            if (!File.Exists(path))
            {
                return defaultValue;
            }

            try
            {
                string json = File.ReadAllText(path);
                T loaded = JsonUtility.FromJson<T>(json);
                return loaded == null ? defaultValue : loaded;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load save file '{fileName}': {exception.Message}");
                return defaultValue;
            }
        }

        public static bool SaveJson<T>(string fileName, T data)
        {
            try
            {
                string path = GetPath(fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to save file '{fileName}': {exception.Message}");
                return false;
            }
        }

        public static string GetPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
