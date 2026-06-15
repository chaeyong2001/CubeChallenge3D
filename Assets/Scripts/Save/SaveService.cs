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
                return TryLoadBackup(fileName, defaultValue);
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return TryLoadBackup(fileName, defaultValue);
                }

                T loaded = JsonUtility.FromJson<T>(json);
                return loaded == null ? TryLoadBackup(fileName, defaultValue) : loaded;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load save file '{fileName}': {exception.Message}");
                return TryLoadBackup(fileName, defaultValue);
            }
        }

        public static bool SaveJson<T>(string fileName, T data)
        {
            if (data == null)
            {
                Debug.LogWarning($"Save skipped because '{fileName}' data is null.");
                return false;
            }

            string tempPath = null;
            try
            {
                string path = GetPath(fileName);
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string backupPath = GetBackupPath(fileName);
                tempPath = path + ".tmp";
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, backupPath, true);
                }
                else
                {
                    File.Move(tempPath, path);
                    File.Copy(path, backupPath, true);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to save file '{fileName}': {exception.Message}");
                return false;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempPath) && File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Preserve the original save even if stale temp cleanup fails.
                    }
                }
            }
        }

        public static string GetPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        private static string GetBackupPath(string fileName)
        {
            return GetPath(fileName) + ".bak";
        }

        private static T TryLoadBackup<T>(string fileName, T defaultValue)
        {
            string backupPath = GetBackupPath(fileName);
            if (!File.Exists(backupPath))
            {
                return defaultValue;
            }

            try
            {
                string json = File.ReadAllText(backupPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return defaultValue;
                }

                T recovered = JsonUtility.FromJson<T>(json);
                if (recovered == null)
                {
                    return defaultValue;
                }

                Debug.LogWarning($"Recovered save file '{fileName}' from backup.");
                try
                {
                    File.Copy(backupPath, GetPath(fileName), true);
                }
                catch (Exception restoreException)
                {
                    Debug.LogWarning($"Backup loaded but primary save could not be restored for '{fileName}': {restoreException.Message}");
                }
                return recovered;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to recover backup for '{fileName}': {exception.Message}");
                return defaultValue;
            }
        }
    }
}
