using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CombatExperimentTools
{
    [MenuItem("TheGame/Combat v1/Create isolated experiment scene")]
    public static void CreateScene()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Create outside Play Mode only");
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(active.isDirty)throw new System.InvalidOperationException("Current scene has unsaved changes; preserve them before switching");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Experiment camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=14;
        camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.015f,.025f,.06f);camera.clearFlags=CameraClearFlags.SolidColor;
        new GameObject("ISOLATED combat experiment — no production progress").AddComponent<CombatExperiment>();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CombatArchetypesV1.unity");
        // Deliberately not added to EditorBuildSettings or a Build Profile.
    }
}