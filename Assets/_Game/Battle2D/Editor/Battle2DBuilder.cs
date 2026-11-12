using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using GrandNavigation.Gameplay;
namespace GrandNavigation.Battle2D.Editor
{
    public static class Battle2DBuilder
    {
        public const string ScenePath="Assets/_Game/Scenes/Battle2D.unity";
        const string Root="Assets/_Game/Battle2D/";
        static Sprite sprite;
        [MenuItem("Grand Navigation/Create Battle 2D (new only)")]
        public static void Create()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty||System.IO.File.Exists(ScenePath))throw new System.Exception("Save scene, stop Play; destination must be new.");
            System.IO.Directory.CreateDirectory(Root+"Sprites");
            var texture=new Texture2D(16,16);var pixels=Enumerable.Repeat(Color.white,256).ToArray();texture.SetPixels(pixels);texture.Apply();
            System.IO.File.WriteAllBytes(Root+"Sprites/White.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"Sprites/White.png");importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.SaveAndReimport();sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Sprites/White.png");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var player=Ship("Player",new Vector2(0,-130),ShipKind2D.Player,600,new Color(.85f,.9f,.94f));
            player.gameObject.AddComponent<PlayerInput2D>().ship=player;
            Ship("Battleship",new Vector2(-110,90),ShipKind2D.Battleship,250,new Color(1,.3f,.15f));
            Ship("Destroyer A",new Vector2(100,70),ShipKind2D.Destroyer,125,new Color(1,.6f,.15f));
            Ship("Destroyer B",new Vector2(0,145),ShipKind2D.Destroyer,125,new Color(1,.6f,.15f));
            Box("Island A",new Vector2(-65,5),new Vector2(35,50),new Color(.25f,.5f,.25f));
            Box("Island B",new Vector2(70,25),new Vector2(40,35),new Color(.25f,.5f,.25f));
            Box("Island C",new Vector2(-10,90),new Vector2(28,25),new Color(.25f,.5f,.25f));
            Box("West Boundary",new Vector2(-205,0),new Vector2(10,420),Color.gray);Box("East Boundary",new Vector2(205,0),new Vector2(10,420),Color.gray);
            Box("North Boundary",new Vector2(0,205),new Vector2(400,10),Color.gray);Box("South Boundary",new Vector2(0,-205),new Vector2(400,10),Color.gray);
            var camera=new GameObject("Camera 2D",typeof(Camera),typeof(AudioListener),typeof(Camera2D));camera.tag="MainCamera";camera.transform.position=new Vector3(0,-130,-10);
            var c=camera.GetComponent<Camera>();c.orthographic=true;c.orthographicSize=115;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.025f,.13f,.23f);camera.GetComponent<Camera2D>().target=player.transform;
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();AssetDatabase.SaveAssets();
        }
        public static ShipActor2D Ship(string name,Vector2 position,ShipKind2D kind,int hp,Color color)
        {
            var go=new GameObject(name,typeof(Rigidbody2D),typeof(BoxCollider2D),typeof(HealthSystem),typeof(ShipMovement2D),typeof(ShipActor2D));go.transform.position=position;
            var actor=go.GetComponent<ShipActor2D>();actor.kind=kind;actor.team=kind==ShipKind2D.Player?0:1;go.GetComponent<HealthSystem>().maximumHealth=hp;
            bool small=kind==ShipKind2D.Destroyer;float width=small?4:6,length=small?12:18;go.GetComponent<BoxCollider2D>().size=new Vector2(width,length);
            Visual("Hull",go.transform,Vector2.zero,new Vector2(width,length),color,1);
            Visual("Bow",go.transform,new Vector2(0,length*.5f),new Vector2(width*.55f,2),color,1);
            Visual("Bridge",go.transform,new Vector2(0,-1),new Vector2(width*.6f,4),Color.gray,2);
            foreach(float y in new[]{5f,2.5f,-5f})Visual("Turret",go.transform,new Vector2(0,y),new Vector2(2,2),Color.white,3);
            return actor;
        }
        static void Visual(string name,Transform parent,Vector2 position,Vector2 size,Color color,int order)
        {
            var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;
        }
        static void Box(string name,Vector2 position,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(BoxCollider2D));go.layer=6;go.transform.position=position;go.GetComponent<BoxCollider2D>().size=size;Visual("Visual",go.transform,Vector2.zero,size,color,0);
        }
    }
}
