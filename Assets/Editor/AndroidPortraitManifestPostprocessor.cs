#if UNITY_ANDROID
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace CubeChallenge3D.Editor
{
    public sealed class AndroidPortraitManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning($"[AndroidManifest] Manifest not found for sensor portrait check: {manifestPath}");
                return;
            }

            string manifest = File.ReadAllText(manifestPath);
            string patched = manifest
                .Replace("android:screenOrientation=\"portrait\"", "android:screenOrientation=\"sensorPortrait\"")
                .Replace("android:screenOrientation=\"reversePortrait\"", "android:screenOrientation=\"sensorPortrait\"")
                .Replace("android:screenOrientation=\"fullSensor\"", "android:screenOrientation=\"sensorPortrait\"")
                .Replace("android:resizeableActivity=\"true\"", "android:resizeableActivity=\"false\"")
                .Replace("android:value=\"portrait|landscape\"", "android:value=\"portrait\"");

            if (manifest == patched)
            {
                Debug.Log("[AndroidManifest] Sensor portrait orientation already fixed.");
                return;
            }

            File.WriteAllText(manifestPath, patched);
            Debug.Log("[AndroidManifest] Forced UnityPlayerGameActivity screenOrientation=sensorPortrait.");
        }
    }
}
#endif
