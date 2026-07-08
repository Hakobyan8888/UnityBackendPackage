#if UNITY_EDITOR
using UnityEditor;

namespace BackendPackage.Editor
{
    [InitializeOnLoad]
    internal static class BackendPackageImportPrompt
    {
        private const string PromptKey = "BackendPackage.ImportPrompt.0.1.0";

        static BackendPackageImportPrompt()
        {
            EditorApplication.delayCall += ShowPromptOnce;
        }

        private static void ShowPromptOnce()
        {
            if (EditorPrefs.GetBool(PromptKey, false))
            {
                return;
            }

            EditorPrefs.SetBool(PromptKey, true);

            var openSetup = EditorUtility.DisplayDialog(
                "Backend Package",
                "Backend Package was imported. Optional integrations need their SDK packages before they can compile. Open the setup window now?",
                "Open Setup",
                "Later");

            if (openSetup)
            {
                BackendPackageSetupWindow.Open();
            }
        }
    }
}
#endif
