#if UNITY_EDITOR
using BackendPackage.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace BackendPackage.Editor
{
    public static class BackendPackageTools
    {
        [MenuItem("Backend Package/Create Test Bootstrap In Scene")]
        public static void CreateTestBootstrapInScene()
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<BackendBootstrapper>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            var go = new GameObject("BackendPackageBootstrap");
            go.AddComponent<BackendBootstrapper>();

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
#endif
