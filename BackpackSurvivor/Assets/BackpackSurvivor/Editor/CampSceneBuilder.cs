using BS.GamePlay.Quest;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BackpackSurvivor.EditorTools
{
    public static class CampSceneBuilder
    {
        public const string ScenePath="Assets/BackpackSurvivor/Scenes/Camp/Camp.unity";
        [MenuItem("Tools/Backpack Survivor/Quest/S7 Build Camp Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas=new GameObject("CampCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c=canvas.GetComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var title=Label(canvas.transform,"Title","调度营地",48,new Vector2(0,300),new Vector2(800,80));
            var contract=Label(canvas.transform,"ContractText","",28,new Vector2(-280,80),new Vector2(580,300)); contract.alignment=TextAlignmentOptions.Center;
            var facts=Label(canvas.transform,"FactsText","",24,new Vector2(330,80),new Vector2(520,300)); facts.alignment=TextAlignmentOptions.Center;
            var button=new GameObject("LaunchButton",typeof(RectTransform),typeof(Image),typeof(Button)); button.transform.SetParent(canvas.transform,false); var b=button.GetComponent<Button>();
            var rect=button.GetComponent<RectTransform>(); rect.sizeDelta=new Vector2(320,70); rect.anchoredPosition=new Vector2(0,-260); var image=button.GetComponent<Image>(); image.color=new Color(.15f,.3f,.4f); b.targetGraphic=image; Label(button.transform,"Caption","进入封锁区",26,Vector2.zero,new Vector2(300,60));
            var go=new GameObject("CampController"); var ctl=go.AddComponent<CampController>(); ctl.Configure(contract,facts,b);
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var dir="Assets/BackpackSurvivor/Scenes/Camp"; if(!AssetDatabase.IsValidFolder(dir)) { AssetDatabase.CreateFolder("Assets/BackpackSurvivor/Scenes","Camp"); }
            EditorSceneManager.SaveScene(scene,ScenePath); EditorBuildSettings.scenes=AppendBuild(EditorBuildSettings.scenes,ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("[Quest S7] camp scene built: "+ScenePath);
        }
        static TMP_Text Label(Transform p,string n,string text,float size,Vector2 pos,Vector2 dim){var g=new GameObject(n);g.transform.SetParent(p,false);var r=g.AddComponent<RectTransform>();r.sizeDelta=dim;r.anchoredPosition=pos;var t=g.AddComponent<TextMeshProUGUI>();t.text=text;t.fontSize=size;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;return t;}
        static EditorBuildSettingsScene[] AppendBuild(EditorBuildSettingsScene[] a,string path){foreach(var x in a)if(x.path==path)return a;var l=new System.Collections.Generic.List<EditorBuildSettingsScene>(a);l.Add(new EditorBuildSettingsScene(path,true));return l.ToArray();}
    }
}
