using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if BAKERY_INCLUDED

public class BakeryLightManagerWindow : EditorWindow
{
    private List<Light> directionalLights = new List<Light>();
    private List<Light> pointLights = new List<Light>();
    private List<Light> spotLights = new List<Light>();
    private List<Light> areaLights = new List<Light>();

    private Vector2 scrollPosition;

    private bool showDirectional = true;
    private bool showPoint = true;
    private bool showSpot = true;
    private bool showArea = true;

    [MenuItem("ValenVRC/Tools/Bakery Light Manager")]
    public static void ShowWindow()
    {
        GetWindow<BakeryLightManagerWindow>("Bakery Light Manager");
    }

    private void OnEnable()
    {
        RefreshLights();
    }

    private void RefreshLights()
    {
        directionalLights.Clear();
        pointLights.Clear();
        spotLights.Clear();
        areaLights.Clear();

        Light[] sceneLights = FindObjectsOfType<Light>(true);
        foreach (Light light in sceneLights)
        {
            switch (light.type)
            {
                case LightType.Directional: directionalLights.Add(light); break;
                case LightType.Point:       pointLights.Add(light);       break;
                case LightType.Spot:        spotLights.Add(light);        break;
                case LightType.Area:        areaLights.Add(light);        break;
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh"))
                RefreshLights();

            if (GUILayout.Button("Convert All to Bakery Lights"))
                ConvertAll();
        }

        EditorGUILayout.Space(4);

        int total = directionalLights.Count + pointLights.Count + spotLights.Count + areaLights.Count;
        EditorGUILayout.LabelField($"Total lights found: {total}", EditorStyles.boldLabel);

        EditorGUILayout.Space(4);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawLightSection("Directional Lights", directionalLights, ref showDirectional, ConvertDirectional);
        DrawLightSection("Point Lights",       pointLights,       ref showPoint,       ConvertPoint);
        DrawLightSection("Spot Lights",        spotLights,        ref showSpot,        ConvertSpot);
        DrawLightSectionReadOnly("Area Lights (manual setup required)", areaLights, ref showArea);

        EditorGUILayout.EndScrollView();
    }

    private void DrawLightSection(string title, List<Light> lights, ref bool foldout, System.Action<Light> convertAction)
    {
        foldout = EditorGUILayout.Foldout(foldout, $"{title} ({lights.Count})", true, EditorStyles.foldoutHeader);
        if (!foldout) return;

        EditorGUI.indentLevel++;

        if (lights.Count == 0)
        {
            EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
        }
        else
        {
            foreach (Light light in lights)
            {
                if (light == null) continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool alreadyConverted = light.gameObject.GetComponent<BakeryDirectLight>() != null
                                        || light.gameObject.GetComponent<BakeryPointLight>() != null;

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(light, typeof(Light), true);
                    EditorGUI.EndDisabledGroup();

                    string status = alreadyConverted ? "Converted" : (light.enabled ? "Active" : "Disabled");
                    GUIStyle statusStyle = alreadyConverted
                        ? EditorStyles.miniLabel
                        : (light.enabled ? EditorStyles.miniLabel : EditorStyles.miniLabel);
                    Color prevColor = GUI.color;
                    GUI.color = alreadyConverted ? Color.green : (light.enabled ? Color.white : Color.gray);
                    GUILayout.Label(status, EditorStyles.miniLabel, GUILayout.Width(60));
                    GUI.color = prevColor;

                    EditorGUI.BeginDisabledGroup(alreadyConverted);
                    if (GUILayout.Button("Convert", GUILayout.Width(64)))
                        convertAction(light);
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2);
    }

    private void DrawLightSectionReadOnly(string title, List<Light> lights, ref bool foldout)
    {
        foldout = EditorGUILayout.Foldout(foldout, $"{title} ({lights.Count})", true, EditorStyles.foldoutHeader);
        if (!foldout) return;

        EditorGUI.indentLevel++;

        if (lights.Count == 0)
        {
            EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.HelpBox("Area lights require a BakeryLightMesh component on a mesh renderer. Convert manually via the Bakery menu.", MessageType.Info);
            foreach (Light light in lights)
            {
                if (light == null) continue;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(light, typeof(Light), true);
                EditorGUI.EndDisabledGroup();
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2);
    }

    private void ConvertAll()
    {
        int converted = 0;

        foreach (Light light in directionalLights)
        {
            if (light == null) continue;
            if (light.gameObject.GetComponent<BakeryDirectLight>() == null)
            {
                ConvertDirectional(light);
                converted++;
            }
        }

        foreach (Light light in pointLights)
        {
            if (light == null) continue;
            if (light.gameObject.GetComponent<BakeryPointLight>() == null)
            {
                ConvertPoint(light);
                converted++;
            }
        }

        foreach (Light light in spotLights)
        {
            if (light == null) continue;
            if (light.gameObject.GetComponent<BakeryPointLight>() == null)
            {
                ConvertSpot(light);
                converted++;
            }
        }

        RefreshLights();
        Debug.Log($"[BakeryLightManager] Converted {converted} light(s) to Bakery lights.");
    }

    private void ConvertDirectional(Light light)
    {
        Undo.RecordObject(light.gameObject, "Convert to Bakery Directional Light");

        BakeryDirectLight bakeryLight = Undo.AddComponent<BakeryDirectLight>(light.gameObject);
        bakeryLight.color     = light.color;
        bakeryLight.intensity = light.intensity;

        ApplyEditorOnly(light);

        EditorUtility.SetDirty(light.gameObject);
    }

    private void ConvertPoint(Light light)
    {
        Undo.RecordObject(light.gameObject, "Convert to Bakery Point Light");

        BakeryPointLight bakeryLight  = Undo.AddComponent<BakeryPointLight>(light.gameObject);
        bakeryLight.projMode          = BakeryPointLight.ftLightProjectionMode.Omni;
        bakeryLight.color             = light.color;
        bakeryLight.intensity         = light.intensity;
        bakeryLight.cutoff            = light.range;

        ApplyEditorOnly(light);

        EditorUtility.SetDirty(light.gameObject);
    }

    private void ConvertSpot(Light light)
    {
        Undo.RecordObject(light.gameObject, "Convert to Bakery Spot Light");

        BakeryPointLight bakeryLight  = Undo.AddComponent<BakeryPointLight>(light.gameObject);
        bakeryLight.projMode          = BakeryPointLight.ftLightProjectionMode.Cone;
        bakeryLight.color             = light.color;
        bakeryLight.intensity         = light.intensity;
        bakeryLight.cutoff            = light.range;
        bakeryLight.angle             = light.spotAngle;
        bakeryLight.innerAngle        = light.innerSpotAngle;

        ApplyEditorOnly(light);

        EditorUtility.SetDirty(light.gameObject);
    }

    private static void ApplyEditorOnly(Light light)
    {
        Undo.RecordObject(light, "Disable realtime light");
        light.enabled = false;
        light.gameObject.tag = "EditorOnly";
    }
}

#else

public class BakeryLightManagerWindow : EditorWindow
{
    [MenuItem("ValenVRC/Tools/Bakery Light Manager")]
    public static void ShowWindow()
    {
        GetWindow<BakeryLightManagerWindow>("Bakery Light Manager");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Bakery is not detected in this project.", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox("Please install Bakery from the Asset Store and ensure it's properly set up to use the Bakery Light Manager tool.", MessageType.Warning);
    }
}

#endif