using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>Deterministic 120 m recovery yard; preserves the original gameplay scene and art slice.</summary>
    public static class FullMapArtBuilder
    {
        public const string ScenePath="Assets/BackpackSurvivor/Scenes/Run/01-Run_ArtFull.unity";
        const string SourceScene="Assets/BackpackSurvivor/Scenes/Run/01-Run.unity";
        const string Art="Assets/BackpackSurvivor/Art/Environment/VNext";
        const string Mats=Art+"/Materials/FullMap";
        const string Meshes=Art+"/Meshes/FullMap";
        const string Prefabs="Assets/BackpackSurvivor/Prefabs/Environment/VNext";
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static readonly Dictionary<string,Transform> batches=new Dictionary<string,Transform>();
        static Transform environment, props, decoration;
        static Mesh unitQuad;

        [MenuItem("Tools/Backpack Survivor/Art/Build Full 120m Recovery Yard")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before building the full map.");
            if(!Shader.Find("BackpackSurvivor/Environment/Recovery Ground")) throw new InvalidOperationException("Recovery Ground shader has not imported.");
            foreach(string name in new[]{"ConcreteBarrier","FenceSegment","CargoCrate","SupplyTerminal","SmallGenerator","RecoveryCar"})
                if(!AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/"+name+".prefab")) throw new InvalidOperationException("Missing existing prop prefab: "+name);
            foreach(string name in new[]{"ShippingContainer","CheckpointCanopy"})
                if(!AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/"+name+".glb")) throw new InvalidOperationException("Missing imported landmark: "+name);
            PreserveUnsavedScene();
            Folder(Mats); Folder(Meshes); Folder(Prefabs+"/FullMap");
            BuildMaterials(); BuildPropVariants(); BuildLandmarkPrefabs();
            unitQuad=SaveMesh("UnitGroundQuad",new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)},new[]{0,1,2,0,2,3});
            Scene scene=EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Single);
            if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new InvalidOperationException("Could not create the full-map scene.");
            Transform map=GameObject.Find("Map").transform;
            var old=new List<GameObject>(); foreach(Transform child in map) old.Add(child.gameObject);
            foreach(var child in old) UnityEngine.Object.DestroyImmediate(child);
            environment=new GameObject("RecoveryYard_FullMap_120m").transform; environment.SetParent(map,false);
            environment.localRotation=Quaternion.Euler(0,-45,0);
            props=Group("Props",environment); decoration=Group("SurfaceDetails_By32mSector",environment); batches.Clear();
            BuildGround(); BuildCirculation(); BuildCentralServices(); BuildCheckpoint(); BuildCargo(); BuildRepair(); BuildDrainage(); BuildInfieldPockets(); BuildBoundary();
            foreach(var parent in batches.Values) Combine(parent);
            ConfigureLighting();
            NightContentBuilder.ApplyToActiveScene();
            var save=GameObject.Find("SaveService"); if(save) save.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(environment.gameObject,Prefabs+"/FullMap/RecoveryYard_120m.prefab");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("FULL_MAP_READY="+ScenePath+"; playable radius remains 60m.");
        }

        [MenuItem("Tools/Backpack Survivor/Art/Open Full 120m Recovery Yard")]
        public static void Open()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        static void BuildMaterials()
        {
            materials.Clear();
            var mask=PackedTexture("Ground_SurfaceMask"); var normal=PackedTexture("Ground_NormalDetail");
            Ground("OuterApron",new Color(.255f,.29f,.32f),new Color(.35f,.385f,.40f),4,.32f,.16f,.20f,.32f,mask,normal);
            Ground("Asphalt",new Color(.18f,.215f,.25f),new Color(.27f,.31f,.34f),4,.55f,.25f,.38f,.65f,mask,normal);
            Ground("Concrete",new Color(.37f,.41f,.445f),new Color(.465f,.49f,.51f),5,.24f,.25f,.15f,.26f,mask,normal);
            Ground("OldConcrete",new Color(.31f,.35f,.39f),new Color(.40f,.435f,.46f),5,.34f,.20f,.26f,.52f,mask,normal);
            Ground("Patch",new Color(.15f,.19f,.23f),new Color(.25f,.29f,.325f),3,.65f,.26f,.34f,.15f,mask,normal);
            Ground("WetConcrete",new Color(.21f,.26f,.305f),new Color(.32f,.365f,.40f),5,.18f,.43f,.88f,.45f,mask,normal);
            Ground("Water",new Color(.12f,.18f,.23f),new Color(.20f,.265f,.315f),8,.08f,.77f,1f,.08f,mask,normal);
            Lit("WhitePaint",new Color(.55f,.58f,.60f),0,.25f);
            Lit("AmberPaint",new Color(.58f,.415f,.18f),0,.27f);
            Ground("TireRubber",new Color(.275f,.32f,.365f),new Color(.365f,.405f,.435f),4,.15f,.30f,.12f,.2f,mask,normal);
            Lit("Joint",new Color(.18f,.22f,.26f),0,.18f);
            Lit("Steel",new Color(.25f,.32f,.385f),.7f,.45f);
            Lit("Charcoal",new Color(.095f,.14f,.19f),.35f,.35f);
            Lit("ConcreteEdge",new Color(.33f,.385f,.435f),0,.24f);
            Material amber=Lit("LampAmber",new Color(.82f,.60f,.27f),0,.3f);
            amber.SetColor("_EmissionColor",new Color(1,.67f,.25f)*2); amber.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive; amber.EnableKeyword("_EMISSION");
            Material cyan=Lit("LampCyan",new Color(.25f,.62f,.60f),0,.3f);
            cyan.SetColor("_EmissionColor",new Color(.28f,.77f,.79f)*1.6f); cyan.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive; cyan.EnableKeyword("_EMISSION");
            var original=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/SharedKit.mat");
            var kit=MaterialAsset("SharedKitSteel",original.shader); EditorUtility.CopySerialized(original,kit); kit.name="SharedKitSteel";
            string palettePath=Art+"/Textures/FullMap/ENV_Palette_BaseColor.png";
            var paletteImporter=(TextureImporter)AssetImporter.GetAtPath(palettePath);
            paletteImporter.sRGBTexture=true; paletteImporter.mipmapEnabled=false; paletteImporter.filterMode=FilterMode.Point; paletteImporter.wrapMode=TextureWrapMode.Clamp;
            paletteImporter.isReadable=false; paletteImporter.npotScale=TextureImporterNPOTScale.None; paletteImporter.textureCompression=TextureImporterCompression.Uncompressed; paletteImporter.SaveAndReimport();
            kit.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(palettePath)); kit.enableInstancing=true; EditorUtility.SetDirty(kit); materials["SharedKitSteel"]=kit;
        }

        static void Ground(string name,Color a,Color b,float metres,float bump,float smooth,float wet,float cracks,Texture2D mask,Texture2D normal)
        {
            Material m=MaterialAsset(name,Shader.Find("BackpackSurvivor/Environment/Recovery Ground"));
            m.SetColor("_BaseColor",a); m.SetColor("_SecondaryColor",b); m.SetFloat("_MetersPerTile",metres);
            m.SetFloat("_BumpScale",bump); m.SetFloat("_Smoothness",smooth); m.SetFloat("_Wetness",wet); m.SetFloat("_CrackStrength",cracks);
            m.SetFloat("_MacroScale",.117f); m.SetTexture("_SurfaceMask",mask); m.SetTexture("_NormalDetail",normal);
            EditorUtility.SetDirty(m); materials[name]=m;
        }

        static Texture2D PackedTexture(string name)
        {
            string path=Art+"/Textures/FullMap/"+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer) throw new InvalidOperationException("Run Tools/ArtPipeline/build_ground_maps.py first: "+path);
            importer.textureType=TextureImporterType.Default; importer.sRGBTexture=false; importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=false; importer.mipmapEnabled=true; importer.filterMode=FilterMode.Trilinear;
            importer.wrapMode=TextureWrapMode.Repeat; importer.anisoLevel=4; importer.maxTextureSize=512; importer.isReadable=false;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=512,format=TextureImporterFormat.BC7,compressionQuality=100});
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Lit(string name,Color color,float metal,float smooth)
        {
            var m=MaterialAsset(name,Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor",color); m.SetFloat("_Metallic",metal); m.SetFloat("_Smoothness",smooth); EditorUtility.SetDirty(m); return materials[name]=m;
        }
        static Material MaterialAsset(string name,Shader shader)
        {
            string path=Mats+"/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(shader){name=name}; AssetDatabase.CreateAsset(m,path);} else m.shader=shader;
            m.enableInstancing=true; return m;
        }

        static void BuildPropVariants()
        {
            foreach(string name in new[]{"ConcreteBarrier","FenceSegment","CargoCrate","SupplyTerminal","SmallGenerator","RecoveryCar"})
            {
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/"+name+".prefab"));
                try
                {
                    foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                    {
                        if(name!="RecoveryCar"){renderer.sharedMaterial=materials["SharedKitSteel"]; continue;}
                        var slots=renderer.sharedMaterials;
                        for(int i=0;i<slots.Length;i++)
                        {
                            var copy=MaterialAsset("CarSteel_"+i,slots[i].shader); EditorUtility.CopySerialized(slots[i],copy); copy.name="CarSteel_"+i; copy.enableInstancing=true;
                            var tint=new Color(.67f,.82f,1,1);
                            if(copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor",tint);
                            if(copy.HasProperty("_BaseColorFactor")) copy.SetColor("_BaseColorFactor",tint);
                            if(copy.HasProperty("baseColorFactor")) copy.SetColor("baseColorFactor",copy.GetColor("baseColorFactor")*tint);
                            EditorUtility.SetDirty(copy); slots[i]=copy;
                        }
                        renderer.sharedMaterials=slots;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/FullMap/"+name+".prefab");
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
        }

        static void BuildLandmarkPrefabs()
        {
            Material kit=materials["SharedKitSteel"];
            if(!kit) throw new InvalidOperationException("Existing SharedKit palette material missing.");
            foreach(string name in new[]{"ShippingContainer","CheckpointCanopy"})
            {
                var root=new GameObject(name); root.layer=7;
                try
                {
                    var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/"+name+".glb"));
                    visual.name="Visual"; visual.transform.SetParent(root.transform,false);
                    foreach(var child in visual.GetComponentsInChildren<Transform>()) child.gameObject.layer=7;
                    foreach(var r in visual.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial=kit;
                    if(name=="ShippingContainer") ColliderBox(root.transform,new Vector3(0,1.295f,0),new Vector3(6.06f,2.59f,2.44f));
                    else foreach(float x in new[]{-3.5f,3.5f}) foreach(float z in new[]{-1.1f,1.1f}) ColliderBox(root.transform,new Vector3(x,2.01f,z),new Vector3(.52f,4.02f,.52f));
                    PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/FullMap/"+name+".prefab");
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
        }

        static void BuildGround()
        {
            Transform surfaces=Group("Ground_128m_Coverage",environment);
            // Visual apron beyond the boundary prevents the normal edge camera from seeing an empty void.
            // It has no collider and does not enlarge MapBounds or the 128m gameplay floor collider.
            Plane("DistantApron_VisualOnly_224m",surfaces,new Vector3(0,-.025f,0),new Vector2(224,224),0,materials["OuterApron"]);
            var verts=new List<Vector3>(); var indices=new List<int>();
            for(int z=0;z<=8;z++) for(int x=0;x<=8;x++) verts.Add(new Vector3(x*4-16,0,z*4-16));
            for(int z=0;z<8;z++) for(int x=0;x<8;x++){int a=z*9+x; indices.AddRange(new[]{a,a+9,a+10,a,a+10,a+1});}
            Mesh tile=SaveMesh("Ground32m_4mGrid",verts.ToArray(),indices.ToArray());
            for(int z=0;z<4;z++) for(int x=0;x<4;x++) MeshObject("GroundSector_"+x+"_"+z,surfaces,tile,materials["OuterApron"],new Vector3(x*32-48,0,z*32-48));
            var collision=new GameObject("GroundCollider_DefaultLayer"); collision.transform.SetParent(surfaces,false);
            var box=collision.AddComponent<BoxCollider>(); box.size=new Vector3(128,.1f,128); box.center=new Vector3(0,-.05f,0);
            Disc("CentralRecoveryApron",surfaces,Vector3.zero,18,12,materials["Concrete"],.012f);
            Ring("CirculationRoad_10m",surfaces,29,5,materials["Asphalt"],.017f,192,false);
            for(int i=0;i<4;i++)
            {
                float yaw=i*90; Vector3 centre=Quaternion.Euler(0,yaw,0)*new Vector3(0,.025f,33);
                Plane("ApproachRoad_"+i,surfaces,centre,new Vector2(10,38),yaw,materials["Asphalt"]);
            }
            Pad("CheckpointApron",new Vector2(0,46),new Vector2(25,16),"OldConcrete",0);
            Pad("CargoApron",new Vector2(45,0),new Vector2(18,28),"Concrete",0);
            Pad("RepairApron",new Vector2(0,-46),new Vector2(28,16),"OldConcrete",0);
            Pad("DrainageApron",new Vector2(-45,0),new Vector2(18,24),"WetConcrete",0);
            for(int i=0;i<4;i++)
            {
                float angle=(45+i*90)*Mathf.Deg2Rad; Vector2 p=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*35;
                Pad("InfieldPad_"+i,p,new Vector2(9,7),"OldConcrete",i*90-8);
            }
        }

        static void BuildCirculation()
        {
            Transform ground=Group("RoadMarkingArcs",environment);
            Ring("InnerRoadEdge",ground,24.7f,.045f,materials["WhitePaint"],.044f,240,false);
            Ring("OuterRoadEdge",ground,33.4f,.055f,materials["AmberPaint"],.044f,240,true);
            Ring("CentralRecoveryMark",ground,9.5f,.065f,materials["AmberPaint"],.049f,96,true);
            for(int direction=0;direction<4;direction++)
            {
                float yaw=direction*90; var rotation=Quaternion.Euler(0,yaw,0);
                for(int j=0;j<8;j++)
                {
                    Vector3 p=rotation*new Vector3(0,0,19+j*4.5f);
                    Paint("ApproachDash",new Vector2(p.x,p.z),new Vector2(.12f,1.6f),yaw,"WhitePaint");
                }
                for(int side=-1;side<=1;side+=2)
                    for(int j=0;j<5;j++)
                    {
                        Vector3 p=rotation*new Vector3(side*4.6f,0,34+j*3.5f);
                        Paint("RoadEdge",new Vector2(p.x,p.z),new Vector2(.08f,2.3f),yaw,"WhitePaint");
                    }
                StencilNumber(direction+1,rotation*new Vector3(-2.2f,0,38.5f),yaw,1.15f);
                Arrow(rotation*new Vector3(2.3f,0,21),yaw);
            }
            // Local repairs and curved tire scuffs break the large road surfaces at metre scale.
            var random=new System.Random(120709);
            for(int i=0;i<22;i++)
            {
                float a=(float)random.NextDouble()*Mathf.PI*2; float r=22+(float)random.NextDouble()*17;
                Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                Plane("AsphaltRepair_"+i,Batch(p),new Vector3(p.x,.037f,p.y),new Vector2(1.4f+(float)random.NextDouble()*2.5f,.8f+(float)random.NextDouble()*2),i*37,materials["Patch"]);
            }
            TireTracks(new Vector2(-4,5),18,7); TireTracks(new Vector2(4,-11),-24,7);
            TireTracks(new Vector2(37,-5),85,7); TireTracks(new Vector2(-8,-42),8,8); TireTracks(new Vector2(8,43),-10,7);
        }

        static void BuildCentralServices()
        {
            Place("ConcreteBarrier",-6.5f,13.5f,0); Place("ConcreteBarrier",6,13.5f,-8);
            Place("SupplyTerminal",-10.5f,12,145); Place("CargoCrate",-12.2f,10.5f,5);
            Place("CargoCrate",-11.1f,11.1f,-4); Place("RecoveryCar",-14,0,12);
            Place("SmallGenerator",13.9f,-6.5f,-70); Place("CargoCrate",14.7f,-2.5f,10);
            Place("ConcreteBarrier",7,-13,-8);
            Hatch(new Vector2(-7,12.6f),0,4); Hatch(new Vector2(7,-12.4f),0,4);
            Lamp(new Vector2(-12.5f,12.5f),false,false); Lamp(new Vector2(12.5f,12.5f),true,false);
            Pad("CentralSupplyInset",new Vector2(-8,5),new Vector2(5.2f,3.6f),"OldConcrete",-5);
            Place("SupplyTerminal",-8.8f,5.5f,150); Place("CargoCrate",-6.7f,5.6f,-6); Place("CargoCrate",-7.1f,4.1f,7);
            Pad("CentralMaintenanceInset",new Vector2(8,-5.4f),new Vector2(5.8f,3.8f),"OldConcrete",8);
            Place("SmallGenerator",8.8f,-5.5f,-65); Place("ConcreteBarrier",6.8f,-7.0f,8);
            Drain(new Vector2(9.2f,-3.9f),2,90); Puddle(new Vector2(6.5f,-5.5f),1.4f,.45f,12);
            // Expansion joints add construction scale while the centre remains clear for movement.
            foreach(float x in new[]{-10f,-5f,5f,10f})
            {
                float length=2*Mathf.Sqrt(17*17-x*x);
                Paint("CentralSlabJoint",new Vector2(x,0),new Vector2(.021f,length),0,"Joint",.021f);
                Paint("CentralSlabJoint",new Vector2(0,x),new Vector2(length,.021f),0,"Joint",.021f);
            }
            StencilNumber(0,new Vector3(-1,0,10.7f),0,.82f);
        }

        static void BuildCheckpoint()
        {
            Zone("01_Checkpoint",new Vector2(0,46));
            Place("CheckpointCanopy",0,50,0); Place("SupplyTerminal",-6.1f,48.4f,150);
            Place("RecoveryCar",8.5f,44,12); Place("CargoCrate",-9.3f,49.5f,5); Place("CargoCrate",-10.4f,47.6f,-7);
            Place("ConcreteBarrier",-6.3f,42,0); Place("ConcreteBarrier",5.9f,42,-8);
            Place("FenceSegment",-9,53,0); Place("FenceSegment",8.8f,53,0);
            Hatch(new Vector2(-7,43.2f),0,5); Hatch(new Vector2(7,49.7f),0,4);
            Lamp(new Vector2(-10.7f,44),true,true); Lamp(new Vector2(10.7f,51.5f),true,false);
        }

        static void BuildCargo()
        {
            Zone("02_CargoLoading",new Vector2(45,0));
            Place("ShippingContainer",49,-7,90); Place("ShippingContainer",49,6.5f,90);
            Place("SupplyTerminal",41,10.5f,-90); Place("SmallGenerator",50.4f,0,90);
            Place("ConcreteBarrier",39.5f,-9.5f,90); Place("ConcreteBarrier",39.5f,7.5f,90);
            foreach(var p in new[]{new Vector2(44,7),new Vector2(43,8.6f),new Vector2(45.2f,8.8f),new Vector2(44,-6.8f),new Vector2(45.3f,-8.3f),new Vector2(44.1f,-9.7f)}) Place("CargoCrate",p.x,p.y,p.y*3);
            Place("FenceSegment",53,0,90); Place("FenceSegment",52.8f,-11,90);
            Hatch(new Vector2(44,3.8f),90,6); Hatch(new Vector2(44,-3.2f),90,6);
            Lamp(new Vector2(43,11.8f),false,true); Lamp(new Vector2(49,-12),true,false);
        }

        static void BuildRepair()
        {
            Zone("03_VehicleRepair",new Vector2(0,-46));
            Place("RecoveryCar",-8,-44,173); Place("RecoveryCar",4.8f,-45.4f,-14);
            Place("ShippingContainer",-7.5f,-51.2f,0); Place("SmallGenerator",-2.4f,-49.4f,-10); Place("SmallGenerator",10.5f,-47,80);
            Place("SupplyTerminal",8.1f,-50,0); Place("ConcreteBarrier",-9,-39.8f,0); Place("ConcreteBarrier",7,-39.8f,8);
            Place("CargoCrate",-11.2f,-48,0); Place("CargoCrate",11.8f,-44,16); Place("CargoCrate",11.7f,-42.5f,-3);
            Place("FenceSegment",1,-53.2f,0); Place("FenceSegment",7.8f,-52,0);
            for(int i=0;i<4;i++) Paint("VehicleBay",new Vector2(-12+i*7,-45),new Vector2(.1f,9),0,"WhitePaint");
            Hatch(new Vector2(4,-50.5f),0,6); Lamp(new Vector2(-12,-41.7f),true,true); Lamp(new Vector2(12,-50),true,false);
            Puddle(new Vector2(-4.5f,-43.2f),2.2f,.7f,2); Drain(new Vector2(11,-48.5f),4,0);
        }

        static void BuildDrainage()
        {
            Zone("04_DrainageAndPower",new Vector2(-45,0));
            Place("SmallGenerator",-49.4f,7.8f,-90); Place("SmallGenerator",-49.5f,3.8f,-90); Place("SupplyTerminal",-44.2f,8.8f,90);
            Place("ShippingContainer",-50,-6.8f,90); Place("ConcreteBarrier",-40,-7.5f,90); Place("ConcreteBarrier",-40,8,82);
            Place("CargoCrate",-46.2f,5.8f,-8); Place("CargoCrate",-47.5f,5.4f,0); Place("CargoCrate",-43.5f,-8.7f,17);
            Place("FenceSegment",-53,0,90); Place("FenceSegment",-51.5f,10.2f,90);
            Drain(new Vector2(-48,0),11,0); Drain(new Vector2(-42.5f,-3.5f),6,90);
            Puddle(new Vector2(-44.4f,1),3.8f,1.3f,5); Puddle(new Vector2(-48.6f,-2),2.5f,1.4f,9);
            Hatch(new Vector2(-42,8),90,5); Lamp(new Vector2(-43,-10),false,true); Lamp(new Vector2(-51,9),false,false);
        }

        static void BuildInfieldPockets()
        {
            for(int i=0;i<4;i++)
            {
                float angle=(45+i*90)*Mathf.Deg2Rad; Vector2 p=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*35;
                Place("ConcreteBarrier",p.x-2.6f,p.y-2.4f,i*90); Place("CargoCrate",p.x+2.2f,p.y+1.7f,i*33);
                Place("CargoCrate",p.x+3.1f,p.y+.2f,i*17); Place(i%2==0?"SupplyTerminal":"SmallGenerator",p.x-2.6f,p.y+1.7f,i*90+180);
                Hatch(p+new Vector2(0,-2.5f),i*90,3);
            }
        }

        static void BuildBoundary()
        {
            var parent=Group("BoundaryVisuals_Radius60",environment);
            Ring("PlayableBoundaryMark",parent,59.72f,.07f,materials["AmberPaint"],.045f,320,true);
            BuildRetainingWall(parent);
            // The low continuous wall communicates the boundary. Fence panels and stockpiles vary its silhouette.
            for(int i=0;i<8;i++)
            {
                float yaw=22.5f+i*45; Quaternion q=Quaternion.Euler(0,yaw,0);
                foreach(float x in new[]{-2.25f,2.25f})
                {
                    Vector3 p=q*new Vector3(x,0,60.7f); var fence=Place("FenceSegment",p.x,p.z,yaw); fence.transform.localPosition+=Vector3.up*.83f;
                }
                Vector3 lamp=q*new Vector3(4.7f,0,58.8f); Lamp(new Vector2(lamp.x,lamp.z),true,false);
                Vector3 stack=q*new Vector3(-6.5f,0,61.9f);
                for(int j=0;j<3;j++)
                {
                    Vector3 d=stack+q*new Vector3(j*.65f,.25f+j*.16f,0);
                    BoxVisual("BoundaryConcreteStock",Batch(new Vector2(d.x,d.z)),d,new Vector3(.95f,.5f,.72f),materials[j%2==0?"ConcreteEdge":"Charcoal"]);
                }
            }
            for(int i=0;i<4;i++)
            {float yaw=i*90; Vector3 p=Quaternion.Euler(0,yaw,0)*new Vector3(0,0,62.45f); Place("ShippingContainer",p.x,p.z,yaw);}
        }

        static void BuildRetainingWall(Transform parent)
        {
            // Inner face at radius 60m; the visible footprint is outside the original playable circle.
            const int segments=96, perSector=8;
            var profile=new[]{new Vector2(60,0),new Vector2(60,.64f),new Vector2(60.3f,.84f),new Vector2(61.25f,.84f),new Vector2(61.5f,.12f),new Vector2(61.5f,0)};
            for(int sector=0;sector<segments/perSector;sector++)
            {
                var v=new List<Vector3>(); var t=new List<int>();
                for(int i=0;i<perSector;i++) for(int j=0;j<profile.Length;j++)
                {
                    float a=(sector*perSector+i)*Mathf.PI*2/segments,b=(sector*perSector+i+1)*Mathf.PI*2/segments;
                    Vector2 p=profile[j],q=profile[(j+1)%profile.Length]; int n=v.Count;
                    v.Add(new Vector3(Mathf.Cos(a)*p.x,p.y,Mathf.Sin(a)*p.x)); v.Add(new Vector3(Mathf.Cos(b)*p.x,p.y,Mathf.Sin(b)*p.x));
                    v.Add(new Vector3(Mathf.Cos(b)*q.x,q.y,Mathf.Sin(b)*q.x)); v.Add(new Vector3(Mathf.Cos(a)*q.x,q.y,Mathf.Sin(a)*q.x));
                    t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                }
                var go=MeshObject("RetainingWallSector_"+sector,parent,SaveMesh("RetainingWallSector_"+sector,v.ToArray(),t.ToArray()),materials["ConcreteEdge"],Vector3.zero);
                go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;
            }
            for(int i=0;i<segments;i++)
            {
                float angle=(i+.5f)*Mathf.PI*2/segments;
                var go=new GameObject("BoundaryCollision_"+i); go.transform.SetParent(parent,false); go.layer=7;
                go.transform.localPosition=new Vector3(Mathf.Cos(angle)*60.75f,.42f,Mathf.Sin(angle)*60.75f);
                go.transform.localRotation=Quaternion.Euler(0,90-angle*Mathf.Rad2Deg,0);
                var box=go.AddComponent<BoxCollider>(); box.size=new Vector3(4.0f,.84f,1.50f);
                Vector2 joint=new Vector2(Mathf.Cos(angle)*60.7f,Mathf.Sin(angle)*60.7f);
                Paint("BoundaryExpansionJoint",joint,new Vector2(.025f,1.1f),90-angle*Mathf.Rad2Deg,"Joint",.852f);
                if(i%2==0)
                {
                    Vector2 p=new Vector2(Mathf.Cos(angle)*60.5f,Mathf.Sin(angle)*60.5f);
                    Paint("BoundaryReflector",p,new Vector2(.65f,.16f),90-angle*Mathf.Rad2Deg,"AmberPaint",.851f);
                }
            }
        }

        static void Pad(string name,Vector2 p,Vector2 size,string material,float yaw)
        {
            Plane(name,environment,new Vector3(p.x,.030f,p.y),size,yaw,materials[material]);
            Quaternion q=Quaternion.Euler(0,yaw,0);
            for(float x=-size.x/2+4;x<size.x/2;x+=4)
            {
                Vector3 d=q*new Vector3(x,0,0); Paint("SlabExpansionJoint",p+new Vector2(d.x,d.z),new Vector2(.025f,size.y-.12f),yaw,"Joint",.036f);
            }
            for(float z=-size.y/2+4;z<size.y/2;z+=4)
            {
                Vector3 d=q*new Vector3(0,0,z); Paint("SlabExpansionJoint",p+new Vector2(d.x,d.z),new Vector2(size.x-.12f,.025f),yaw,"Joint",.036f);
            }
        }

        static void Zone(string name,Vector2 p)
        {
            var anchor=Group(name,environment); anchor.localPosition=new Vector3(p.x,0,p.y);
        }
        static GameObject Place(string name,float x,float z,float yaw)
        {
            string path=Prefabs+"/FullMap/"+name+".prefab";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source,props); go.transform.localPosition=new Vector3(x,.035f,z); go.transform.localRotation=Quaternion.Euler(0,yaw,0); return go;
        }
        static void Paint(string name,Vector2 p,Vector2 size,float yaw,string material,float y=.051f)
        {Plane(name,Batch(p),new Vector3(p.x,y,p.y),size,yaw,materials[material]);}
        static Transform Batch(Vector2 point)
        {
            string key="Sector_"+Mathf.FloorToInt((point.x+64)/32)+"_"+Mathf.FloorToInt((point.y+64)/32);
            if(!batches.ContainsKey(key)) batches[key]=Group(key,decoration); return batches[key];
        }
        static void Hatch(Vector2 p,float yaw,int count)
        {
            Quaternion q=Quaternion.Euler(0,yaw,0);
            for(int i=0;i<count;i++){Vector3 d=q*new Vector3((i-(count-1)*.5f)*.6f,0,0); Paint("FadedHazardHatch",p+new Vector2(d.x,d.z),new Vector2(.21f,1.0f),yaw+25,"AmberPaint",.054f);}
        }
        static void TireTracks(Vector2 p,float yaw,float length)
        {
            Quaternion q=Quaternion.Euler(0,yaw,0); int n=Mathf.CeilToInt(length/.3f);
            for(int side=-1;side<=1;side+=2) for(int i=0;i<n;i++)
            {
                if(i%13==3) continue;
                Vector3 d=q*new Vector3(side*.79f+Mathf.Sin(i*.22f)*.18f,0,(i-n*.5f)*.3f);
                float width=.07f+.045f*Mathf.Sin(i*.6f)*Mathf.Sin(i*.6f);
                Paint("TireScuff",p+new Vector2(d.x,d.z),new Vector2(width,.302f),yaw,"TireRubber",.042f);
            }
        }
        static void Arrow(Vector3 p,float yaw)
        {
            Quaternion q=Quaternion.Euler(0,yaw,0);
            Paint("RouteArrowShaft",new Vector2(p.x,p.z),new Vector2(.15f,1.6f),yaw,"WhitePaint");
            for(int side=-1;side<=1;side+=2)
            {Vector3 d=p+q*new Vector3(side*.26f,0,.65f); Paint("RouteArrowHead",new Vector2(d.x,d.z),new Vector2(.13f,.83f),yaw-side*40,"WhitePaint");}
        }
        static void StencilNumber(int number,Vector3 p,float yaw,float scale)
        {
            int[][] digits={new[]{0,1,2,4,5,6},new[]{2,5},new[]{0,2,3,4,6},new[]{0,2,3,5,6},new[]{1,2,3,5}};
            Vector2[] positions={new Vector2(0,1),new Vector2(-.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(-.5f,-.5f),new Vector2(.5f,-.5f),new Vector2(0,-1)};
            Quaternion q=Quaternion.Euler(0,yaw,0);
            for(int digit=0;digit<2;digit++) foreach(int segment in digits[digit==0?0:number])
            {
                Vector2 at=positions[segment]; Vector3 d=p+q*new Vector3((at.x+digit*1.5f)*scale,0,at.y*scale);
                bool horizontal=segment==0||segment==3||segment==6;
                Paint("ZoneStencil",new Vector2(d.x,d.z),horizontal?new Vector2(.78f*scale,.12f*scale):new Vector2(.12f*scale,.78f*scale),yaw,"WhitePaint");
            }
        }
        static void Drain(Vector2 p,float length,float yaw)
        {
            Paint("DrainRecess",p,new Vector2(.65f,length),yaw,"Charcoal",.06f); Quaternion q=Quaternion.Euler(0,yaw,0);
            for(int i=0;i<Mathf.FloorToInt(length/.18f);i++)
            {Vector3 d=q*new Vector3(0,0,-length/2+i*.18f); Paint("DrainSlat",p+new Vector2(d.x,d.z),new Vector2(.59f,.045f),yaw,"Steel",.065f);}
        }
        static void Puddle(Vector2 p,float rx,float rz,int seed)
        {
            var v=new List<Vector3>{new Vector3(p.x,.048f,p.y)}; var t=new List<int>();
            for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32; float r=1+Mathf.Sin(i*.83f+seed)*.07f+Mathf.Sin(i*1.8f)*.035f; v.Add(new Vector3(p.x+Mathf.Cos(a)*rx*r,.048f,p.y+Mathf.Sin(a)*rz*r));}
            for(int i=0;i<32;i++) t.AddRange(new[]{0,(i+1)%32+1,i+1});
            MeshObject("Puddle_"+seed,Batch(p),SaveMesh("Puddle_"+seed,v.ToArray(),t.ToArray()),materials["Water"],Vector3.zero);
        }
        static void Lamp(Vector2 p,bool amber,bool realLight)
        {
            Transform parent=Batch(p);
            BoxVisual("LampFoot",parent,new Vector3(p.x,.09f,p.y),new Vector3(.52f,.18f,.52f),materials["ConcreteEdge"]);
            BoxVisual("LampPole",parent,new Vector3(p.x,1.62f,p.y),new Vector3(.12f,3.1f,.12f),materials["Charcoal"]);
            BoxVisual("LampHousing",parent,new Vector3(p.x,3.19f,p.y),new Vector3(.65f,.22f,.35f),materials["Charcoal"]);
            BoxVisual("LampLens",parent,new Vector3(p.x,3.065f,p.y),new Vector3(.5f,.025f,.26f),materials[amber?"LampAmber":"LampCyan"]);
            if(realLight)
            {
                var go=new GameObject(amber?"WarmServiceSpot_NoShadows":"CoolServiceSpot_NoShadows",typeof(Light)); go.transform.SetParent(environment,false);
                go.transform.localPosition=new Vector3(p.x,3.02f,p.y); go.transform.localRotation=Quaternion.Euler(65,180,0);
                var light=go.GetComponent<Light>(); light.type=LightType.Spot; light.color=amber?new Color(1,.73f,.39f):new Color(.40f,.81f,.9f);
                light.intensity=4; light.range=11; light.spotAngle=105; light.innerSpotAngle=62; light.shadows=LightShadows.None;
            }
        }
        static GameObject Plane(string name,Transform parent,Vector3 p,Vector2 size,float yaw,Material material)
        {
            var go=MeshObject(name,parent,unitQuad,material,p); go.transform.localScale=new Vector3(size.x,1,size.y); go.transform.localRotation=Quaternion.Euler(0,yaw,0); return go;
        }
        static void Disc(string name,Transform parent,Vector3 p,float radius,int sides,Material material,float y)
        {
            var v=new List<Vector3>{new Vector3(p.x,y,p.z)}; var t=new List<int>();
            for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides; v.Add(new Vector3(p.x+Mathf.Cos(a)*radius,y,p.z+Mathf.Sin(a)*radius));}
            for(int i=0;i<sides;i++) t.AddRange(new[]{0,(i+1)%sides+1,i+1});
            MeshObject(name,parent,SaveMesh(name,v.ToArray(),t.ToArray()),material,Vector3.zero);
        }
        static void Ring(string name,Transform parent,float r,float halfWidth,Material material,float y,int segments,bool dashed)
        {
            var v=new List<Vector3>(); var t=new List<int>();
            for(int i=0;i<segments;i++)
            {
                if(dashed&&i%12>=8) continue;
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments; int n=v.Count;
                v.Add(new Vector3(Mathf.Cos(a)*(r-halfWidth),y,Mathf.Sin(a)*(r-halfWidth))); v.Add(new Vector3(Mathf.Cos(b)*(r-halfWidth),y,Mathf.Sin(b)*(r-halfWidth)));
                v.Add(new Vector3(Mathf.Cos(b)*(r+halfWidth),y,Mathf.Sin(b)*(r+halfWidth))); v.Add(new Vector3(Mathf.Cos(a)*(r+halfWidth),y,Mathf.Sin(a)*(r+halfWidth)));
                t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            MeshObject(name,parent,SaveMesh(name,v.ToArray(),t.ToArray()),material,Vector3.zero);
        }
        static void BoxVisual(string name,Transform parent,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=p; go.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        static void ColliderBox(Transform parent,Vector3 centre,Vector3 size)
        {var go=new GameObject("Collision"); go.layer=7; go.transform.SetParent(parent,false); var box=go.AddComponent<BoxCollider>(); box.center=centre; box.size=size;}
        static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material,Vector3 position)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; var r=go.GetComponent<MeshRenderer>(); r.sharedMaterial=material; r.shadowCastingMode=ShadowCastingMode.Off; r.receiveShadows=true; return go;
        }
        static Mesh SaveMesh(string name,Vector3[] vertices,int[] triangles)
        {
            string path=Meshes+"/"+name+".asset"; var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh{name=name}; AssetDatabase.CreateAsset(mesh,path);} mesh.Clear(); mesh.vertices=vertices; mesh.triangles=triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh); return mesh;
        }
        static void Combine(Transform parent)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>(); var old=new List<GameObject>();
            foreach(var f in parent.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>(); if(!r||!f.sharedMesh) continue;
                if(!groups.ContainsKey(r.sharedMaterial)) groups[r.sharedMaterial]=new List<CombineInstance>();
                groups[r.sharedMaterial].Add(new CombineInstance{mesh=f.sharedMesh,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix}); old.Add(f.gameObject);
            }
            foreach(var pair in groups)
            {
                string name=parent.name+"_"+pair.Key.name; string path=Meshes+"/"+name+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(!mesh){mesh=new Mesh{name=name}; AssetDatabase.CreateAsset(mesh,path);} mesh.Clear(); mesh.CombineMeshes(pair.Value.ToArray(),true,true); EditorUtility.SetDirty(mesh);
                MeshObject(name,parent,mesh,pair.Key,Vector3.zero);
            }
            foreach(var go in old) UnityEngine.Object.DestroyImmediate(go);
        }
        static void ConfigureLighting()
        {
            RenderSettings.fog=false; RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.64f,.71f,.79f); RenderSettings.ambientEquatorColor=new Color(.42f,.49f,.56f); RenderSettings.ambientGroundColor=new Color(.20f,.25f,.30f); RenderSettings.reflectionIntensity=.55f;
            var sun=GameObject.Find("Directional Light").GetComponent<Light>(); sun.transform.rotation=Quaternion.Euler(42,-32,0); sun.color=new Color(.96f,.975f,1); sun.intensity=1.25f;
            sun.shadows=LightShadows.Soft; sun.shadowStrength=.74f; sun.shadowBias=.04f; sun.shadowNormalBias=.2f;
            if(Camera.main&&Camera.main.TryGetComponent<UniversalAdditionalCameraData>(out var data)) data.renderPostProcessing=false;
        }
        static Transform Group(string name,Transform parent){var t=new GameObject(name).transform; t.SetParent(parent,false); return t;}
        internal static void PreserveUnsavedScene()
        {
            if(!SceneManager.GetActiveScene().isDirty) return;
            const string folder="Assets/BackpackSurvivor/Scenes/ArtInputSnapshots"; Folder(folder);
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),folder+"/BeforeFullMap_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity",true)) throw new InvalidOperationException("Could not preserve unsaved scene.");
        }
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path)) return; string parent=Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
    }
}
