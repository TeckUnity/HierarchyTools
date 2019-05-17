using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

namespace LoTekK.Tools.Editor
{
    [InitializeOnLoadAttribute]
    public class HierarchyTools : UnityEditor.Editor
    {
        static float padding = 1;
        static float buttonDim;
        static GUIStyle buttonStyle;
        static bool prefsLoaded;
        static bool showComponentIcons;
        const string showComponentIconsKey = "bShowComponentIcons";
        static bool showIndentRainbow;
        const string showIndentRainbowKey = "bShowIndentRainbow";
        static Color disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        static readonly string s_SearchPathPackage = "Packages/com.ltk.hierarchy/";

        static HierarchyTools()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyTools.OnHierarchyGUI;
            showComponentIcons = EditorPrefs.GetBool(showComponentIconsKey, true);
            showIndentRainbow = EditorPrefs.GetBool(showIndentRainbowKey, false);
        }

        [PreferenceItem("Hierarchy Tools")]
        public static void HierarchyToolsPreferencesGUI()
        {
            if (!prefsLoaded)
            {
                showComponentIcons = EditorPrefs.GetBool(showComponentIconsKey, true);
                showIndentRainbow = EditorPrefs.GetBool(showIndentRainbowKey, false);
                prefsLoaded = true;
            }

            using (var c = new EditorGUI.ChangeCheckScope())
            {
                showComponentIcons = EditorGUILayout.Toggle("Show Component Icons", showComponentIcons);
                showIndentRainbow = EditorGUILayout.Toggle("Show Indent Rainbow", showIndentRainbow);
                if (c.changed)
                {
                    EditorPrefs.SetBool(showComponentIconsKey, showComponentIcons);
                    EditorPrefs.SetBool(showIndentRainbowKey, showIndentRainbow);
                }
            }
        }

        public static void OnHierarchyGUI(int instanceId, Rect selectionRect)
        {
            // if(buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.label);
                buttonStyle.padding = new RectOffset();
                buttonStyle.border = new RectOffset();
            }
            buttonDim = EditorGUIUtility.singleLineHeight - 2;
            Event e = Event.current;
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            // if obj is null, then the item is a Scene (the header for each loaded scene)
            if (obj)
            {
                GameObject g = obj as GameObject;
                Rect buttonRect = new Rect(selectionRect);
                buttonRect.x = buttonRect.xMax - buttonDim;
                buttonRect.width = buttonDim;
                buttonRect.height -= 2;
                buttonRect.y += 1;

                GUI.DrawTexture(buttonRect, g.activeSelf
                    ? AssetDatabase.LoadAssetAtPath<Texture2D>(s_SearchPathPackage + "Icons/IconEnabled.png")
                    : AssetDatabase.LoadAssetAtPath<Texture2D>(s_SearchPathPackage + "Icons/IconDisabled.png"));

                if (e.isMouse && e.type == EventType.MouseDown && buttonRect.Contains(e.mousePosition))
                {
                    switch (e.button)
                    {
                        case 0:
                            g.SetActive(!g.activeSelf);
                            break;
                        case 1:
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Show Component Icons", ""), EditorPrefs.GetBool(showComponentIconsKey, true), () =>
                            {
                                EditorPrefs.SetBool(showComponentIconsKey, !EditorPrefs.GetBool(showComponentIconsKey, true));
                            });
                            menu.ShowAsContext();
                            break;
                    }
                    e.Use();
                }
                if (!EditorPrefs.GetBool(showComponentIconsKey, true))
                {
                    return;
                }
                buttonRect.x -= 3;
                buttonRect.width = 1;
                GUI.color = Color.black * 0.5f;
                GUI.DrawTexture(buttonRect, EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;
                buttonRect.x -= 3;
                buttonRect.width = buttonDim;
                Component[] components = g.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null || component.GetType() == typeof(Transform))
                    {
                        continue;
                    }
                    buttonRect.x -= buttonDim + padding;
                    GUI.color = new Color(1, 1, 1, 0.5f);
                    GUI.Box(buttonRect, "");
                    GUI.color = Color.white;
                    Type type = component.GetType();
                    GUI.Label(buttonRect, new GUIContent(EditorGUIUtility.ObjectContent(component, type).image, type.ToString()), buttonStyle);
                    var b = component as Behaviour;
                    if (b)
                    {
                        if (!b.enabled)
                        {
                            GUI.DrawTexture(new RectOffset(-(int)(buttonDim * 0.4f), 0, -(int)(buttonDim * 0.4f), 0).Add(buttonRect), AssetDatabase.LoadAssetAtPath<Texture2D>(s_SearchPathPackage + "Icons/IconRemove.png"));
                            EditorGUI.DrawRect(buttonRect, new Color(1, 0, 0, 0.25f));
                        }
                    }
                    else
                    {
                        Type t = component.GetType();
                        if (t.GetMember("get_enabled", BindingFlags.Public | BindingFlags.Instance).Length > 0)
                        {
                            if (!(bool)t.InvokeMember("get_enabled", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, component, null))
                            {
                                GUI.DrawTexture(new RectOffset(-(int)(buttonDim * 0.4f), 0, -(int)(buttonDim * 0.4f), 0).Add(buttonRect), AssetDatabase.LoadAssetAtPath<Texture2D>(s_SearchPathPackage + "Icons/IconRemove.png"));
                                EditorGUI.DrawRect(buttonRect, new Color(1, 0, 0, 0.25f));
                            }
                        }
                    }
                }
                if (!g.activeInHierarchy)
                {
                    GUI.color = new Color(1, 0, 0, 1f);
                    GUI.Box(selectionRect, "");
                    GUI.color = Color.white;
                }
                if (!showIndentRainbow)
                {
                    return;
                }
                float opacity = 0.25f;
                float luminosity = 0.666f;
                Color[] colors = new Color[] { new Color(luminosity * 1.5f, 0, luminosity / 3, opacity), new Color(luminosity, luminosity * 2 / 3, 0, opacity), new Color(luminosity / 4, luminosity, 0, opacity), new Color(0, luminosity, luminosity, opacity), new Color(0, luminosity / 2, luminosity * 1.5f, opacity), new Color(luminosity, 0, luminosity * 1.5f, opacity) };
                int pos = (int)(selectionRect.xMin - 2) / 14 - 2;
                for (int i = 0; i <= pos; i++)
                {
                    GUI.color = colors[(i) % colors.Length];
                    // EditorGUI.DrawRect(new Rect(1 + (i + 1) * 14 + (14 - width), selectionRect.yMin, width, selectionRect.height), colors[(i) % colors.Length] / opacity);
                    GUI.DrawTexture(new Rect(1 + (i + 1) * 14, selectionRect.yMin, 14, selectionRect.height), AssetDatabase.LoadAssetAtPath<Texture>(s_SearchPathPackage + "Icons/IconGradient.psd"));
                    GUI.color = Color.white;
                }
                return;
            }
            Scene scene = GetSceneFromInstanceID(instanceId);
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset();
            selectionRect.x = selectionRect.xMax - 35;
            selectionRect.width = buttonDim;
            using (new EditorGUI.DisabledScope(EditorSceneManager.sceneCount <= 1))
            {
                if (GUI.Button(selectionRect, new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>(s_SearchPathPackage + "Icons/IconRemove.png"), "Remove scene"), buttonStyle))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Remove Scene"), false, () =>
                    {
                        if (scene.isDirty)
                        {
                            if (EditorUtility.DisplayDialog("Save modified scene?", string.Format("{0} has been modified. Save before removing scene?", scene.name), "Save", "Don't Save"))
                            {
                                EditorSceneManager.SaveScene(scene);
                            }
                        }
                        EditorSceneManager.UnloadSceneAsync(scene);
                    });
                    menu.ShowAsContext();
                }
                if (EditorSceneManager.sceneCount <= 1)
                {
                    GUI.color = disabledColor;
                    GUI.DrawTexture(selectionRect, EditorGUIUtility.whiteTexture);
                    GUI.color = Color.white;
                }
                selectionRect.x -= buttonDim + padding * 4;
            }
            using (new EditorGUI.DisabledScope(scene.isLoaded && EditorSceneManager.loadedSceneCount == 1))
            {
                if (GUI.Button(selectionRect, scene.isLoaded ? new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>(s_SearchPathPackage + "Icons/IconEnabled.png"), "Unload Scene") : new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>(s_SearchPathPackage + "Icons/IconDisabled.png"), "Load Scene"), buttonStyle))
                {
                    if (scene.isLoaded)
                    {
                        if (scene.isDirty)
                        {
                            if (EditorUtility.DisplayDialog("Save modified scene?", string.Format("{0} has been modified. Save before unloading scene?", scene.name), "Save", "Don't Save"))
                            {
                                EditorSceneManager.SaveScene(scene);
                            }
                        }
                        EditorSceneManager.CloseScene(scene, false);
                    }
                    else
                    {
                        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
                        SceneView.RepaintAll();
                    }
                }
                if (scene.isLoaded && EditorSceneManager.loadedSceneCount == 1)
                {
                    GUI.color = disabledColor;
                    GUI.DrawTexture(selectionRect, EditorGUIUtility.whiteTexture);
                    GUI.color = Color.white;
                }
            }
            // selectionRect.x -= buttonDim + padding;
            // using (new EditorGUI.DisabledScope(!scene.isLoaded))
            // {
            //     if (GUI.Button(selectionRect, new GUIContent("R", "Select root GameObjects in scene"), buttonStyle))
            //     {
            //         Selection.objects = scene.GetRootGameObjects();
            //     }
            //     if (!scene.isLoaded)
            //     {
            //         GUI.color = disabledColor;
            //         GUI.DrawTexture(selectionRect, EditorGUIUtility.whiteTexture);
            //         GUI.color = Color.white;
            //     }
            // }
            selectionRect.x -= buttonDim + padding;
            using (new EditorGUI.DisabledScope(!scene.isDirty))
            {
                if (GUI.Button(selectionRect, new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>(s_SearchPathPackage + "Icons/IconSave.png"), "Save this scene"), buttonStyle))
                {
                    EditorSceneManager.SaveScene(scene, scene.path);
                }
                if (!scene.isDirty)
                {
                    GUI.color = disabledColor;
                    GUI.DrawTexture(selectionRect, EditorGUIUtility.whiteTexture);
                    GUI.color = Color.white;
                }
            }
            if (!string.IsNullOrEmpty(scene.path))
            {
                selectionRect.x -= buttonDim + padding;
                if (GUI.Button(selectionRect, new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>(s_SearchPathPackage + "Icons/IconFind.png"), "Locate Scene Asset"), buttonStyle))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scene.path));
                }
            }
        }

        public static Scene GetSceneFromInstanceID(int id)
        {
            Type type = typeof(EditorSceneManager);
            MethodInfo mi = type.GetMethod("GetSceneByHandle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            object classInstance = Activator.CreateInstance(type, null);
            return (Scene)mi.Invoke(classInstance, new object[] { id });
        }
    }
}