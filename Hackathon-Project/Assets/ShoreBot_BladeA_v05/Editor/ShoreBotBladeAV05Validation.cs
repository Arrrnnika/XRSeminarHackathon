// Asset-only check: no crawler mesh or crawler behaviour is created.
using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ShoreBotBladeAV05Validation
{
    const string Root = "Assets/ShoreBot_BladeA_v05";
    [Serializable] public class Report
    {
        public string unityVersion;
        public float widthMetres, lengthMetres, thicknessMetres, oneMetreReference;
        public float testFootprintWidth = 1.0f, testFootprintLength = 1.2f;
        public bool dimensionsCorrect, collisionPresent, corridorTwoMetresClear;
        public bool footprintSupported, threeIndependentDamageZones, targetsPreserved, passed;
        public int testedRouteSamples;
        public string note = "Virtual dimensions only. No robot created; confirm Kurt's final footprint, suction contacts and tool reach. This is not a headset performance test.";
    }

    [MenuItem("Tools/ShoreBot/Blade A v05/Validate width, collision and damage")]
    public static void MenuValidate()
    {
        var r = Validate();
        EditorUtility.DisplayDialog("ShoreBot Blade A validation", JsonUtility.ToJson(r, true), "OK");
    }

    public static Report Validate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Generated/ShoreBot_RH_BladeA_v05.prefab");
        if(prefab == null) throw new Exception("Run Create or refresh test scenes first.");
        var previous = SceneManager.GetActiveScene();
        var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(temporary);
        try
        {
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            var ts=go.GetComponentsInChildren<Transform>(true);
            var blade=ts.First(t=>t.name=="ENV_WorkingBlade").GetComponent<MeshRenderer>();
            var collider=go.GetComponentsInChildren<MeshCollider>(true).FirstOrDefault(c=>c.name=="COL_WorkBlade");
            var a=ts.First(t=>t.name=="Scale_0m");var b=ts.First(t=>t.name=="Scale_1m");
            var samples=ts.Where(t=>t.name.StartsWith("Route_Sample_")).OrderBy(t=>t.name).ToArray();
            var size=blade.bounds.size;
            var r=new Report{unityVersion=Application.unityVersion,widthMetres=size.x,lengthMetres=size.z,
                thicknessMetres=size.y,oneMetreReference=Vector3.Distance(a.position,b.position),
                collisionPresent=collider!=null && collider.sharedMesh!=null,testedRouteSamples=samples.Length};
            r.dimensionsCorrect=Mathf.Abs(size.x-4.5f)<.01f && Mathf.Abs(size.z-27.2f)<.01f &&
                Mathf.Abs(size.y-.86f)<.01f && Mathf.Abs(r.oneMetreReference-1)<.001f;
            Physics.SyncTransforms();
            r.corridorTwoMetresClear=r.collisionPresent && samples.Length==23;
            r.footprintSupported=r.corridorTwoMetresClear;
            if(r.collisionPresent && samples.Length>=2)
            {
                Vector3 forward=(samples[1].position-samples[0].position);forward.y=0;forward.Normalize();
                Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
                foreach(var sample in samples)
                {
                    foreach(float x in new[]{-1f,0f,1f})
                        r.corridorTwoMetresClear &= Supported(collider,sample.position+side*x);
                    foreach(float x in new[]{-.5f,.5f})
                        foreach(float y in new[]{-.6f,.6f})
                            r.footprintSupported &= Supported(collider,sample.position+side*x+forward*y);
                }
            }
            var visuals=ts.Where(t=>t.name.StartsWith("Damage_") && t.name.EndsWith("_Visuals")).ToArray();
            r.threeIndependentDamageZones=visuals.Length==3;
            r.targetsPreserved=true;
            foreach(var v in visuals)
            {
                v.gameObject.SetActive(false);
                r.threeIndependentDamageZones &= visuals.Where(t=>t!=v).All(t=>t.gameObject.activeInHierarchy);
                string targetName=v.name.Replace("_Visuals","_Target");
                r.targetsPreserved &= ts.First(t=>t.name==targetName).gameObject.activeInHierarchy;
                v.gameObject.SetActive(true);
            }
            r.passed=r.dimensionsCorrect && r.collisionPresent && r.corridorTwoMetresClear &&
                r.footprintSupported && r.threeIndependentDamageZones && r.targetsPreserved;
            Directory.CreateDirectory(Root+"/Generated");
            File.WriteAllText(Root+"/Generated/BladeA_Validation.json",JsonUtility.ToJson(r,true));
            Debug.Log("SHOREBOT_BLADEA_VALIDATION "+JsonUtility.ToJson(r));
            return r;
        }
        finally
        {
            EditorSceneManager.CloseScene(temporary,true);
            if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
        }
    }
    static bool Supported(MeshCollider collider,Vector3 p)
    {
        RaycastHit hit;
        return collider.Raycast(new Ray(p+Vector3.up*2,Vector3.down),out hit,4) && hit.normal.y>.9f;
    }
    public static void BatchBuildAndValidate()
    {
        if(!Application.isBatchMode)throw new Exception("Batch entry point requires batch mode. Use the menu in an interactive editor.");
        try
        {
            ShoreBotBladeAV05Setup.Build();
            var report=Validate();
            EditorApplication.Exit(report.passed?0:1);
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
