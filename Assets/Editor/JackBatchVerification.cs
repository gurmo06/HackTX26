#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;

// Command-line verification helper. Regular setup is JackDemoBuilder.Build.
public static class JackBatchVerification
{
    public static void ImportEssentials()
    {
        if(!UnityEngine.Application.isBatchMode) throw new System.InvalidOperationException("This helper is for command-line verification only. Use the TextMeshPro import menu in the Editor.");
        if(TMP_Settings.GetFontAsset()) { EditorApplication.Exit(0); return; }
        var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
        var path=Path.Combine(package.resolvedPath,"Package Resources/TMP Essential Resources.unitypackage");
        if(!File.Exists(path)) throw new FileNotFoundException("TMP Essentials package unavailable",path);
        // Import is asynchronous: a command-line -quit would exit before the assets exist.
        AssetDatabase.importPackageCompleted += _ => { AssetDatabase.Refresh(); EditorApplication.Exit(0); };
        AssetDatabase.importPackageFailed += (_, error) => { UnityEngine.Debug.LogError(error); EditorApplication.Exit(1); };
        UnityEditor.AssetPackage.Package.Import(path,false);
    }
}
#endif
