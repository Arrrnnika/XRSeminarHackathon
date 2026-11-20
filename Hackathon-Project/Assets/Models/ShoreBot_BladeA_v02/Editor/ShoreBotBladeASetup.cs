// Asset-only utilities. No robot, XR, input, networking, or repair gameplay logic.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ShoreBotBladeASetup
{
    const string Root = "Assets/ShoreBot_BladeA_v02";
    const string Model = Root + "/Models/ShoreBot_RH_BladeA_v02.fbx";
    static readonly string[] MaterialNames = {
        "MAT_BladePaint", "MAT_Edge", "MAT_FoundationYellow", "MAT_Crack", "MAT_RoutePaint", "MAT_TipBand",
        "MAT_Ocean", "MAT_Sky", "MAT_DamageAtlas"
    };

    [MenuItem("Tools/ShoreBot/Blade A v02/Create or refresh test scenes")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Generated");
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            var ti = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 1024;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = path.Contains("Damage_Atlas");
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.SaveAndReimport();
        }
        bool urp = GraphicsSettings.currentRenderPipeline != null &&
            GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");
        if (GraphicsSettings.currentRenderPipeline != null && !urp)
            throw new Exception("This asset-test helper supports Built-in or URP. Map materials manually for another pipeline; project settings are unchanged.");
        var mats = new Dictionary<string, Material>();
        foreach (string name in MaterialNames)
        {
            bool unlit = name == "MAT_Ocean" || name == "MAT_Sky";
            string shaderName = urp ? (unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Simple Lit") :
                (unlit ? "Unlit/Texture" : "Standard");
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new Exception("Required shader unavailable: " + shaderName + ". Use the manual material mapping in Unity_Handoff.md.");
            string path = Root + "/Materials/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            else m.shader = shader;
            m.name = name;
            Color color = Color.white;
            string texture = null;
            switch (name)
            {
                case "MAT_BladePaint": texture = "BladePaint_BaseColor.png"; break;
                case "MAT_Edge": color = new Color(.29f,.37f,.40f); break;
                case "MAT_RoutePaint": color = new Color(.06f,.20f,.25f); break;
                case "MAT_TipBand": color = new Color(.54f,.21f,.10f); break;
                case "MAT_FoundationYellow": color = new Color(.68f,.40f,.06f); break;
                case "MAT_Crack": color = new Color(.018f,.021f,.020f); break;
                case "MAT_Ocean": texture = "Ocean_Palette.png"; break;
                case "MAT_Sky": texture = "Sky_Gradient.png"; break;
                case "MAT_DamageAtlas": texture = "Damage_Atlas_RGBA.png"; break;
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .2f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .2f);
            if (texture != null)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + texture);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (name == "MAT_DamageAtlas")
            {
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.EnableKeyword("_ALPHATEST_ON");
                if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1);
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", .15f);
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1);
                m.renderQueue = (int)RenderQueue.AlphaTest;
            }
            EditorUtility.SetDirty(m); mats[name] = m;
        }
        AssetDatabase.SaveAssets();
        var importer = AssetImporter.GetAtPath(Model) as ModelImporter;
        if (importer == null) throw new Exception("Copy the complete Assets/ShoreBot_BladeA_v02 folder into the project first.");
        importer.globalScale = 1;
        importer.useFileScale = true;
        importer.isReadable = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importAnimation = false;
        importer.addCollider = false;
        importer.preserveHierarchy = true;
        importer.importNormals = ModelImporterNormals.Import;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        foreach (var kv in mats)
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
        importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        var staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(staging);
        var go = PrefabUtility.InstantiatePrefab(model) as GameObject;
        // Work on plain objects, not a live link to the FBX, so components can be added reliably.
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = "ShoreBot_RH_BladeA_v02";
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (r.name.StartsWith("COL_"))
            {
                r.gameObject.SetActive(true);
                r.enabled = false;
                // Unity components must be null-checked with ==, not ??
                var mc = r.gameObject.GetComponent<MeshCollider>();
                if (mc == null) mc = r.gameObject.AddComponent<MeshCollider>();
                if (mc == null)
                {
                    // Fallback: an invisible child object carries the collider instead.
                    var holder = new GameObject(r.name + "_Collider");
                    holder.transform.SetParent(r.transform, false);
                    mc = holder.AddComponent<MeshCollider>();
                }
                mc.sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
                mc.convex = false;
            }
            else if (r.name.Contains("OPTIONAL")) { r.enabled = true; r.gameObject.SetActive(false); }
            else r.enabled = true;
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Generated/ShoreBot_RH_BladeA_v02.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        EditorSceneManager.CloseScene(staging, true);
        SaveScene(prefab, "ShoreBot_BladeA_CrawlTest", "View_Crawl");
        SaveScene(prefab, "ShoreBot_BladeA_DamageTest", "View_DamageCloseup");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("ShoreBot: environment prefab, materials and two asset test scenes created. No existing scene or project graphics settings changed.");
    }

    static void SaveScene(GameObject prefab, string name, string view)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        var transforms = go.GetComponentsInChildren<Transform>(true);
        var marker = transforms.First(t => t.name == view);
        var target = transforms.First(t => t.name == (view == "View_Crawl" ? "View_CrawlAim" : "Damage_01_Target"));
        var camera = new GameObject("Asset test camera").AddComponent<Camera>();
        camera.tag = "MainCamera"; camera.transform.position = marker.position;
        camera.transform.LookAt(target.position, Vector3.up);
        camera.fieldOfView = view == "View_Crawl" ? 42 : 34; camera.nearClipPlane = .03f; camera.farClipPlane = 5800;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.22f,.28f,.34f);
        var light = new GameObject("Asset test sunlight").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.2f; light.shadows = LightShadows.None;
        light.transform.rotation = Quaternion.Euler(35,-35,0);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.55f,.60f,.66f);
        RenderSettings.fog = false;
        EditorSceneManager.SaveScene(scene, Root + "/Generated/" + name + ".unity");
        EditorSceneManager.CloseScene(scene, true);
    }

    [Serializable] public class Validation
    {
        public string unityVersion;
        public string renderPipeline;
        public float scaleCheckMetres;
        public int visibleRenderers, visibleTriangles, damageZones;
        public bool texturesAssigned, damageTogglesIndependent, scaleCorrect, colliderPresent;
        public bool surfaceNormalsVerified;
        public string[] notes;
    }

    // Invoked in a disposable project via -executeMethod; not an auto-import callback.
    public static void BatchBuildAndValidate()
    {
        try
        {
            Build();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Generated/ShoreBot_RH_BladeA_v02.prefab");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            var ts = go.GetComponentsInChildren<Transform>(true);
            var a = ts.First(t => t.name == "Scale_0m"); var b = ts.First(t => t.name == "Scale_1m");
            var cracks = ts.Where(t => t.name.StartsWith("Damage_") && t.name.EndsWith("_Crack")).ToArray();
            bool independent = cracks.Length == 3;
            foreach (var crack in cracks)
            {
                crack.gameObject.SetActive(false);
                independent &= !crack.gameObject.activeInHierarchy && cracks.Where(x=>x!=crack).All(x=>x.gameObject.activeInHierarchy);
                crack.gameObject.SetActive(true);
            }
            var renderers = go.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.gameObject.activeInHierarchy).ToArray();
            bool textures = renderers.All(r=>r.sharedMaterials.All(m=>m!=null && m.shader!=null && m.shader.name!="Hidden/InternalErrorShader"));
            foreach (var n in new[]{"MAT_BladePaint","MAT_Ocean","MAT_Sky"})
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/"+n+".mat");
                textures &= m != null && m.mainTexture != null;
            }
            var collider = go.GetComponentsInChildren<MeshCollider>(true).FirstOrDefault();
            Physics.SyncTransforms();
            // Axis conventions are verified after FBX conversion, rather than assumed.
            var target = ts.First(t => t.name=="Damage_02_Target");
            bool normals = false;
            foreach (var axis in new[]{target.forward,target.up,target.right,-target.forward,-target.up,-target.right})
            {
                RaycastHit hit;
                if (collider != null && collider.Raycast(new Ray(target.position+axis*.15f,-axis),out hit,.3f))
                    normals |= Vector3.Dot(hit.normal,axis)>.75f;
            }
            var report = new Validation {
                unityVersion=Application.unityVersion,
                renderPipeline=GraphicsSettings.currentRenderPipeline==null ? "Built-in (isolated import test)" : GraphicsSettings.currentRenderPipeline.GetType().Name,
                scaleCheckMetres=Vector3.Distance(a.position,b.position),
                scaleCorrect=Mathf.Abs(Vector3.Distance(a.position,b.position)-1)<.001f,
                visibleRenderers=renderers.Length,
                visibleTriangles=renderers.Sum(r=>TriangleCount(r.GetComponent<MeshFilter>().sharedMesh)),
                damageZones=cracks.Length, texturesAssigned=textures,damageTogglesIndependent=independent,
                colliderPresent=collider!=null && collider.sharedMesh!=null,surfaceNormalsVerified=normals,
                notes=new[]{"Editor asset import and independent visibility tested.","No robot, XR interaction, URP headset rendering or Quest performance test performed."}
            };
            var output = Environment.GetEnvironmentVariable("SHOREBOT_VALIDATION_OUTPUT");
            if (!String.IsNullOrEmpty(output)) File.WriteAllText(output,JsonUtility.ToJson(report,true));
            Debug.Log("SHOREBOT_VALIDATION "+JsonUtility.ToJson(report));
            if (!report.scaleCorrect || !textures || !independent || !report.colliderPresent || !normals)
                throw new Exception("ShoreBot asset validation failed; inspect report.");
            EditorApplication.Exit(0);
        }
        catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    static int TriangleCount(Mesh mesh)
    {
        int n = 0;
        for(int i=0;i<mesh.subMeshCount;i++) n += (int)mesh.GetIndexCount(i)/3;
        return n;
    }
}
