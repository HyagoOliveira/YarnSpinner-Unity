namespace Yarn.Unity.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;
    using UnityEditor.Compilation;

    class YarnSpinnerProjectSettingsProvider : SettingsProvider
    {
        private YarnSpinnerProjectSettings baseSettings;
        private YarnSpinnerProjectSettings unsavedSettings;

        public YarnSpinnerProjectSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            baseSettings = YarnSpinnerProjectSettings.GetOrCreateSettings();
            unsavedSettings = YarnSpinnerProjectSettings.GetOrCreateSettings();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Automatic Recompilation and Asset Association");
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Update Localised Assets", GUILayout.Width(290), GUILayout.ExpandWidth(false));
                    var localisedAssetUpdate = EditorGUILayout.Toggle(unsavedSettings.autoRefreshLocalisedAssets, GUILayout.ExpandWidth(false));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Generate linkings for Functions and Commands", GUILayout.Width(290), GUILayout.ExpandWidth(false));
                    var linkingAttributedFuncs = EditorGUILayout.Toggle(unsavedSettings.automaticallyLinkAttributedYarnCommandsAndFunctions, GUILayout.ExpandWidth(false));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Generate YSLS file for attributed methods", GUILayout.Width(290), GUILayout.ExpandWidth(false));
                    var generateYSLS = EditorGUILayout.Toggle(unsavedSettings.generateYSLSFile, GUILayout.ExpandWidth(false));
                    EditorGUILayout.EndHorizontal();

                    if (changeCheck.changed)
                    {   
                        unsavedSettings.autoRefreshLocalisedAssets = localisedAssetUpdate;
                        unsavedSettings.automaticallyLinkAttributedYarnCommandsAndFunctions = linkingAttributedFuncs;
                        unsavedSettings.generateYSLSFile = generateYSLS;
                    }

                    bool disabledReimportButton = true;
                    if
                    (
                        unsavedSettings.automaticallyLinkAttributedYarnCommandsAndFunctions != baseSettings.automaticallyLinkAttributedYarnCommandsAndFunctions ||
                        unsavedSettings.autoRefreshLocalisedAssets != baseSettings.autoRefreshLocalisedAssets ||
                        unsavedSettings.generateYSLSFile != baseSettings.generateYSLSFile
                    )
                    {
                        disabledReimportButton = false;
                    }

                    EditorGUILayout.Space();
                    using (new EditorGUI.DisabledScope(disabledReimportButton))
                    {
                        if (GUILayout.Button("Apply Changes", GUILayout.Width(200)))
                        {
                            // we need to know which parts we will need to reimport
                            // we check and change the setting first
                            // because the settings are used in the reimports that are about to be run
                            // and only then can we do the appropriate reimports
                            bool needsCSharpRecompilation = false;
                            bool needsYarnProjectReimport = false;

                            if (baseSettings.autoRefreshLocalisedAssets != unsavedSettings.autoRefreshLocalisedAssets)
                            {
                                needsYarnProjectReimport = true;
                            }
                            if (baseSettings.automaticallyLinkAttributedYarnCommandsAndFunctions != unsavedSettings.automaticallyLinkAttributedYarnCommandsAndFunctions)
                            {
                                needsCSharpRecompilation = true;
                            }
                            if (baseSettings.generateYSLSFile != unsavedSettings.generateYSLSFile)
                            {
                                needsCSharpRecompilation = true;
                            }

                            // saving the changed settings out to disk
                            baseSettings.autoRefreshLocalisedAssets = unsavedSettings.autoRefreshLocalisedAssets;
                            baseSettings.automaticallyLinkAttributedYarnCommandsAndFunctions = unsavedSettings.automaticallyLinkAttributedYarnCommandsAndFunctions;
                            baseSettings.generateYSLSFile = unsavedSettings.generateYSLSFile;
                            baseSettings.WriteSettings();

                            // now we can reimport
                            if (needsCSharpRecompilation)
                            {
                                // need to do a full build because if they have DISABLED this then we want to turf the already linked code
                                // this is BIG but I can't see any other way around this
                                // I am assuming people aren't doing this so often as to be a huge headache
                                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
                            }
                            if (needsYarnProjectReimport)
                            {
                                // here we just reimport the yarn projects
                                var yarnProjects = AssetDatabase.FindAssets($"t:{nameof(YarnProject)}");
                                foreach (var guid in yarnProjects)
                                {
                                    var path = AssetDatabase.GUIDToAssetPath(guid);
                                    AssetDatabase.ImportAsset(path);
                                }
                            }
                        }
                    }
                }
            }
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateYarnSpinnerProjectSettingsProvider()
        {
            var provider = new YarnSpinnerProjectSettingsProvider("Project/Yarn Spinner", SettingsScope.Project);

            var keywords = new List<string>() { "yarn", "spinner", "localisation", "codegen", "attribute" };
            provider.keywords = keywords;
            return provider;
        }
    }
}
