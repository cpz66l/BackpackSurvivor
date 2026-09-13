using System;
using System.Collections.Generic;
using BS.GamePlay.Quest;
using BS.GamePlay.Npc;
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
            var facts=Label(root,"FactsText",495,245,750,170,25);
            var input=Input(root,"DialogueInput",495,-225,700,48,"输入问题后按回车");
            var output=ScrollText(root,"DialogueOutput",495,-25,750,260,25);
            var status=Label(root,"StatusText",0,-365,1670,50,22);
            var launch=Button(root,"LaunchButton","出击 / 重试当前合同",-530,-450,440);
            var redraw=Button(root,"RedrawButton","重抽合同",0,-450,350);
            var menu=Button(root,"MenuButton","返回主菜单",500,-450,350);
            var controller=new GameObject("CampController").AddComponent<CampController>();
            controller.Configure(data,contract,facts,status,launch,redraw,menu,input,output);
            const string personaPath="Assets/BackpackSurvivor/Data/Quest/NpcPersona.asset";
            var persona=AssetDatabase.LoadAssetAtPath<NpcPersona>(personaPath);
            if(!persona){persona=ScriptableObject.CreateInstance<NpcPersona>();AssetDatabase.CreateAsset(persona,personaPath);}
            var auditOpen=Button(root,"AuditOpen","开发审计",770,425,250);
            var audit=V04HudArtBuilder.Box("NpcAuditPanel",root,0,0,1750,830);
            var auditClose=Button(audit,"AuditClose","关闭",680,350,250);
            var auditText=ScrollText(audit,"AuditText",0,-35,1660,690,20);
            controller.ConfigureDialogue(persona,auditText,audit.gameObject,auditOpen,auditClose);
            audit.gameObject.SetActive(false);
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
        static TMP_InputField Input(Transform parent,string name,float x,float y,float width,float height,string placeholder)
        {
            var rect=V04HudArtBuilder.Box(name,parent,x,y,width,height);
            var background=rect.GetComponent<BS.Presentation.UpgradeRoundedGraphic>();background.raycastTarget=true;
            var field=rect.gameObject.AddComponent<TMP_InputField>();field.targetGraphic=background;
            var text=V04HudArtBuilder.Label("Text",rect,"",0,0,width-24,height-8,22,V04HudArtBuilder.TextColor,TextAlignmentOptions.Left);
            field.richText=false;text.richText=false;text.textWrappingMode=TextWrappingModes.NoWrap;
            var hint=V04HudArtBuilder.Label("Placeholder",rect,placeholder,0,0,width-24,height-8,22,V04HudArtBuilder.Muted,TextAlignmentOptions.Left);
            field.textComponent=text;field.placeholder=hint;field.textViewport=rect;field.characterLimit=1000;
            field.lineType=TMP_InputField.LineType.SingleLine;return field;
        }
        static TMP_Text ScrollText(Transform parent,string name,float x,float y,float width,float height,float size)
        {
            var viewport=V04HudArtBuilder.Box(name+"Viewport",parent,x,y,width,height);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            var text=Label(viewport,name,0,0,width-30,height,size);text.richText=false;
            var rect=text.rectTransform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(-30,height);
            var fitter=text.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.content=rect;return text;
        }
    }
}
