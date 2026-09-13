using System;
using BS.GamePlay.Npc;
using BS.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace BackpackSurvivor.EditorTools
{
    public static class RunNpcFeedbackBuilder
    {
        [MenuItem("Tools/Backpack Survivor/Quest/S10 Build Run Radio Subtitle")]
        public static void BuildMenu()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first.");
            var hud=UnityEngine.Object.FindAnyObjectByType<RunHudView>();
            if(!hud)throw new InvalidOperationException("Open a Run scene with HUD.");
            BuildOn((RectTransform)hud.transform);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);EditorSceneManager.SaveScene(hud.gameObject.scene);
        }
        public static void BuildOn(RectTransform root)
        {
            // Only this generated block is replaced: retain the user's tracker and supplies layout.
            V04HudArtBuilder.Remove(root,"V04NpcRadio");
            var panel=V04HudArtBuilder.Box("V04NpcRadio",root,0,0,680,84);
            V04HudArtBuilder.Pin(panel,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-135));
            panel.GetComponent<UpgradeRoundedGraphic>().raycastTarget=false;
            var group=panel.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.interactable=false;group.blocksRaycasts=false;
            var text=V04HudArtBuilder.Label("Subtitle",panel,"",0,0,648,68,24,V04HudArtBuilder.TextColor,TextAlignmentOptions.Midline);
            text.richText=false;text.raycastTarget=false;text.enableAutoSizing=true;text.fontSizeMin=20;text.fontSizeMax=24;
            var view=panel.gameObject.AddComponent<RadioPulseReplyView>();
            var data=new SerializedObject(view);data.FindProperty("subtitle").objectReferenceValue=text;data.FindProperty("group").objectReferenceValue=group;data.ApplyModifiedPropertiesWithoutUndo();
            var service=root.GetComponent<WavePulseService>()??root.gameObject.AddComponent<WavePulseService>();
            data=new SerializedObject(service);data.FindProperty("view").objectReferenceValue=view;data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
