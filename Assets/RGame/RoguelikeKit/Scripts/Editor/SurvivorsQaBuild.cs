using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SurvivorsQaBuild
{
    public static void Run()
    {
        string product = PlayerSettings.productName;
        try
        {
            PlayerSettings.productName = "Survivors Artel QA 55";
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult content);
            if (!string.IsNullOrEmpty(content.Error)) throw new Exception(content.Error);
            string output = Path.GetFullPath("../qa-build/Survivors.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception($"Build failed: {report.summary.result}");
            Debug.Log($"SURVIVORS_QA_BUILD_OK={output}");
        }
        finally
        {
            PlayerSettings.productName = product;
        }
    }
}
