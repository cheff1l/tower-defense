using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TowerDefenseProjectBuilder
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string WebGlBuildPath = "Builds/WebGL";

    [MenuItem("Tower Defense/Build Complete Scene")]
    public static void BuildProject()
    {
        ImportSprites();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Main";

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.04f);

        GameObject gameObject = new GameObject("CompleteTowerDefenseGame");
        CompleteTowerDefenseGame game = gameObject.AddComponent<CompleteTowerDefenseGame>();
        SerializedObject serializedGame = new SerializedObject(game);
        AssignSprite(serializedGame, "mapSprite", "Assets/Art/Maps/map_1.png");
        AssignSprite(serializedGame, "goblinSprite", "Assets/Art/Processed/Enemy_Goblin.png");
        AssignSprite(serializedGame, "orcSprite", "Assets/Art/Processed/Enemy_Orc.png");
        AssignSprite(serializedGame, "ghostSprite", "Assets/Art/Processed/Enemy_Ghost.png");
        AssignSprite(serializedGame, "archerTowerSprite", "Assets/Art/Processed/Tower_Archer.png");
        AssignSprite(serializedGame, "mageTowerSprite", "Assets/Art/Processed/Tower_Mage.png");
        AssignSprite(serializedGame, "freezerTowerSprite", "Assets/Art/Processed/Tower_Freezer.png");
        AssignSprite(serializedGame, "cannonTowerSprite", "Assets/Art/Processed/Tower_Cannon.png");
        AssignSprite(serializedGame, "arrowSprite", "Assets/Art/Processed/Projectile_Arrow.png");
        AssignSprite(serializedGame, "magicSprite", "Assets/Art/Processed/Projectile_Magic.png");
        AssignSprite(serializedGame, "goldSprite", "Assets/Art/Processed/Icon_Gold.png");
        AssignSprite(serializedGame, "panelSprite", "Assets/Art/UserProvided/photo_4_2026-04-30_17-15-20.jpg");
        AssignSprite(serializedGame, "buttonSprite", "Assets/Art/UserProvided/photo_14_2026-04-30_17-15-20.jpg");
        serializedGame.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Tower Defense complete scene generated at " + ScenePath);
    }

    public static void BuildWebGL()
    {
        BuildProject();
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = WebGlBuildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);
        Debug.Log("Tower Defense WebGL build generated at " + WebGlBuildPath);
    }

    private static void ImportSprites()
    {
        string[] paths =
        {
            "Assets/Art/Maps/map_1.png",
            "Assets/Art/Enemies/Goblin.jpg",
            "Assets/Art/Enemies/Ork.jpg",
            "Assets/Art/Enemies/Ghost.jpg",
            "Assets/Art/UserProvided/photo_1_2026-04-30_17-15-20.jpg",
            "Assets/Art/UserProvided/photo_2_2026-04-30_17-15-20.jpg",
            "Assets/Art/UserProvided/photo_4_2026-04-30_17-15-20.jpg",
            "Assets/Art/UserProvided/photo_10_2026-04-30_17-15-20.jpg",
            "Assets/Art/UserProvided/photo_11_2026-04-30_17-15-20.jpg",
            "Assets/Art/UserProvided/photo_14_2026-04-30_17-15-20.jpg",
            "Assets/Art/UserProvided/photo_15_2026-04-30_17-15-20.jpg",
            "Assets/Art/UserProvided/photo_16_2026-04-30_17-15-20.jpg",
            "Assets/Art/UserProvided/photo_18_2026-04-30_17-15-20.jpg"
            ,"Assets/Art/Processed/Enemy_Goblin.png"
            ,"Assets/Art/Processed/Enemy_Orc.png"
            ,"Assets/Art/Processed/Enemy_Ghost.png"
            ,"Assets/Art/Processed/Tower_Archer.png"
            ,"Assets/Art/Processed/Tower_Mage.png"
            ,"Assets/Art/Processed/Tower_Freezer.png"
            ,"Assets/Art/Processed/Tower_Cannon.png"
            ,"Assets/Art/Processed/Projectile_Arrow.png"
            ,"Assets/Art/Processed/Projectile_Magic.png"
            ,"Assets/Art/Processed/Icon_Gold.png"
        };

        foreach (string path in paths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("Sprite asset not found: " + path);
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static void AssignSprite(SerializedObject target, string propertyName, string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning("Could not assign sprite: " + path);
        }

        target.FindProperty(propertyName).objectReferenceValue = sprite;
    }
}
