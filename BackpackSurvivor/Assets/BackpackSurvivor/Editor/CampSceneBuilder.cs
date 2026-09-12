using System;
using System.Collections.Generic;
using BS.GamePlay.Quest;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save active scene changes before rebuilding camp.");
            var data=QuestCatalogBuilder.Build();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("CampCamera",typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.025f,.045f,.065f);
            var canvas=new GameObject("CampCanvas", typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            var root=canvas.transform;
            V04HudArtBuilder.Label("Title",root,"调度营地",0,430,1600,90,48,V04HudArtBuilder.TextColor);
            V04HudArtBuilder.Box("ContractPanel",root,-405,20,880,670);
            V04HudArtBuilder.Box("NpcPanel",root,495,20,820,670);
            var contract=Label(root,"ContractText",-405,20,810,610,27);
            var facts=Label(root,"FactsText",495,20,750,610,27);
            var status=Label(root,"StatusText",0,-365,1670,50,22);
            var launch=Button(root,"LaunchButton","出击 / 重试当前合同",-530,-450,440);
            var redraw=Button(root,"RedrawButton","重抽合同",0,-450,350);
            var menu=Button(root,"MenuButton","返回主菜单",500,-450,350);
            var controller=new GameObject("CampController").AddComponent<CampController>();
            controller.Configure(data,contract,facts,status,launch,redraw,menu);
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            if(!AssetDatabase.IsValidFolder("Assets/BackpackSurvivor/Scenes/Camp")) AssetDatabase.CreateFolder("Assets/BackpackSurvivor/Scenes","Camp");
            EditorSceneManager.SaveScene(scene,ScenePath);
            var list=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s=>s.path==ScenePath)) list.Add(new EditorBuildSettingsScene(ScenePath,true));
            else list.Find(s=>s.path==ScenePath).enabled=true;
            EditorBuildSettings.scenes=list.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Quest S7] rebuilt camp with real database, Chinese font, draw/retry buttons and new Input System.");
        }
        static TMP_Text Label(Transform parent,string name,float x,float y,float w,float h,float size)
        {
            var text=V04HudArtBuilder.Label(name,parent,"",x,y,w,h,size,V04HudArtBuilder.TextColor,TextAlignmentOptions.TopLeft);
            text.textWrappingMode=TextWrappingModes.Normal; text.overflowMode=TextOverflowModes.Overflow;
            return text;
        }
        static Button Button(Transform parent,string name,string caption,float x,float y,float width)
        {
            var button=V04HudArtBuilder.Button(name,parent,caption,width,78);
            V04HudArtBuilder.Pin((RectTransform)button.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(x,y));
            return button;
        }
    }
}
