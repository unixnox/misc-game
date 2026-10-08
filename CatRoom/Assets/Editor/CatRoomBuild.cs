using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatRoom.EditorTools
{
    /// <summary>
    /// One-click project setup and WebGL build for YouTube Playables.
    /// Menu: Cat Room/...  — or batch mode: -executeMethod CatRoom.EditorTools.CatRoomBuild.BuildFromCommandLine
    /// </summary>
    [InitializeOnLoad]
    public static class CatRoomBuild
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string BuildDir = "Builds/YouTubePlayables";

        static CatRoomBuild()
        {
            // First time the project is opened: create the scene and apply settings automatically.
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Setup();
                    EditorSceneManager.OpenScene(ScenePath);
                }
            };
        }

        [MenuItem("Cat Room/1. Setup Project (scene + WebGL settings)")]
        public static void Setup()
        {
            EnsureScene();
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[CatRoom] Project set up. Press Play to test, or use 'Cat Room/2. Build WebGL'.");
        }

        [MenuItem("Cat Room/2. Build WebGL for YouTube Playables")]
        public static void BuildWebGL()
        {
            Setup();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            Directory.CreateDirectory(BuildDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[CatRoom] WebGL build OK: {Path.GetFullPath(BuildDir)} ({report.summary.totalSize / (1024 * 1024f):0.0} MB). " +
                          "Zip the CONTENTS of this folder and upload it in the YouTube Playables portal.");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(BuildDir);
            }
            else
            {
                Debug.LogError("[CatRoom] WebGL build failed: " + report.summary.result);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        /// <summary>For CI: Unity -batchmode -quit -projectPath CatRoom -executeMethod CatRoom.EditorTools.CatRoomBuild.BuildFromCommandLine</summary>
        public static void BuildFromCommandLine() => BuildWebGL();

        [MenuItem("Cat Room/Delete Local Save (PlayerPrefs)")]
        public static void DeleteSave()
        {
            PlayablesBridge.DeleteLocalSave();
            Debug.Log("[CatRoom] Local save deleted.");
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                // The game builds itself at runtime (see Bootstrap), so an empty scene is all we need.
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "CatRoom";
            PlayerSettings.productName = "Cozy Cat Room";
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.runInBackground = false;

            // WebGL / YouTube Playables
            PlayerSettings.WebGL.template = "PROJECT:YouTubePlayables";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;   // works no matter how the host serves .gz files
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            // The game uses the legacy Input Manager. If the project is set to "Input System only", switch to "Both".
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings != null && settings.Length > 0)
            {
                var so = new SerializedObject(settings[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null && prop.intValue == 1)
                {
                    prop.intValue = 2;
                    so.ApplyModifiedProperties();
                    Debug.LogWarning("[CatRoom] Active Input Handling set to 'Both'. Please restart the Unity editor.");
                }
            }
        }
    }
}
