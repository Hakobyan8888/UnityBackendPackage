#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace BackendPackage.Editor
{
    public sealed class BackendPackageSetupWindow : EditorWindow
    {
        private const string UnityIapPackageId = "com.unity.purchasing";

        private static readonly string[] IntegrationSymbols =
        {
            "BACKENDPACKAGE_ENABLE_AUTH",
            "BACKENDPACKAGE_ENABLE_IAP",
            "BACKENDPACKAGE_ENABLE_LEVELPLAY",
            "BACKENDPACKAGE_ENABLE_TIKTOK"
        };

        [MenuItem("Backend Package/Setup Integrations")]
        public static void Open()
        {
            GetWindow<BackendPackageSetupWindow>("Backend Package Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Backend Package Integrations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The core package imports without vendor SDKs. Install an SDK first, then enable its symbol so the matching integration assembly compiles.",
                MessageType.Info);

            DrawUpmInstaller("Unity IAP", UnityIapPackageId, "BACKENDPACKAGE_ENABLE_IAP");

            DrawExternalSdk("Google Play Games", "BACKENDPACKAGE_ENABLE_AUTH", "https://github.com/playgameservices/play-games-plugin-for-unity");
            DrawExternalSdk("Unity LevelPlay SDK", "BACKENDPACKAGE_ENABLE_LEVELPLAY", "https://docs.unity.com/grow/levelplay/sdk/unity/intro");
            DrawExternalSdk("TikTok Business SDK", "BACKENDPACKAGE_ENABLE_TIKTOK", "https://ads.tiktok.com/marketing_api/docs?id=1739584855420929");

            EditorGUILayout.Space();
            if (GUILayout.Button("Disable All Backend Package Integration Symbols"))
            {
                UpdateSymbols(symbols => symbols.Except(IntegrationSymbols).ToArray());
            }
        }

        private static void DrawUpmInstaller(string label, string packageId, string symbol)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"Install {label}"))
                {
                    Client.Add(packageId);
                }

                DrawSymbolButton(symbol);
            }
        }

        private static void DrawExternalSdk(string label, string symbol, string url)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open SDK Page"))
                {
                    Application.OpenURL(url);
                }

                DrawSymbolButton(symbol);
            }
        }

        private static void DrawSymbolButton(string symbol)
        {
            var enabled = HasSymbol(symbol);
            var label = enabled ? $"Disable {symbol}" : $"Enable {symbol}";

            if (GUILayout.Button(label))
            {
                SetSymbol(symbol, !enabled);
            }
        }

        private static bool HasSymbol(string symbol)
        {
            return GetSymbols().Contains(symbol);
        }

        private static void SetSymbol(string symbol, bool enabled)
        {
            UpdateSymbols(symbols =>
            {
                var set = new HashSet<string>(symbols);
                if (enabled)
                {
                    set.Add(symbol);
                }
                else
                {
                    set.Remove(symbol);
                }

                return set.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            });
        }

        private static void UpdateSymbols(Func<string[], string[]> update)
        {
            var symbols = update(GetSymbols());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", symbols));
        }

        private static string[] GetSymbols()
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            return symbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
#endif
