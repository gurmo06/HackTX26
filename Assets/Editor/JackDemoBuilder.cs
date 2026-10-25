#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JackGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class JackDemoBuilder
{
    public const string ScenePath = "Assets/Scenes/JackGame.unity";
    const string OwnedRoot = "__JackDemo";
    const string Generated = "Assets/JackGenerated";
    static readonly Color Ink = new Color(.22f,.14f,.09f);
    public static readonly string[] ArtPaths = {
        "Backgrounds/01_Merchant_Village_Post_Beanstalk.png", "Backgrounds/02_Beanstalk_Climbing.png",
        "Backgrounds/03_Giant_Castle.png", "Backgrounds/04_NightEscape.png",
        "Characters/cow.png", "Characters/giant.png", "Characters/golden_goose.png", "Characters/jack_climb.png",
        "Characters/jack_idle.png", "Characters/jack_walk1.png", "Characters/jack_walk2.png", "Characters/merchant.png",
        "Icons/BeanIcon.png", "Icons/EggIcon.png", "Props/beanstalk_segment.png", "Props/cloud_castle.png",
        "Props/cloud_platform.png", "Props/cottage.png", "Props/crate.png", "Props/golden_egg_sack.png",
        "Props/golden_egg.png", "Props/ladder.png", "Props/market_stall.png", "Props/rope.png",
        "Props/stone_block.png", "Props/wooden_platform.png", "UI/ParchmentButton.png",
        "UI/ParchmentDialoguePanel.png", "UI/ParchmentInput.png"
    };

    [MenuItem("Tools/Jack/Create or Refresh Demo Scene")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode before rebuilding JackGame."); return; }
        var missing=ArtPaths.Select(p=>"Assets/Art/"+p).Where(p=>!ExactFileExists(p)).ToArray();
        WriteAssetReport(missing);
        foreach(var path in missing) Debug.LogError("Jack demo missing exact asset: "+path);
        if(missing.Length>0) throw new InvalidOperationException("Restore the listed art files and run Tools > Jack > Create or Refresh Demo Scene again.");
        var body=TMP_Settings.GetFontAsset();
        if(!body) throw new InvalidOperationException("TMP Essentials are missing. Use Window > TextMeshPro > Import TMP Essential Resources, click Import, then run Tools > Jack > Create or Refresh Demo Scene again.");
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BackupScene();
        Directory.CreateDirectory(Generated); Directory.CreateDirectory("Assets/Scenes"); AssetDatabase.Refresh();
        var art=LoadOrCreate<ArtCatalog>(Generated+"/ArtCatalog.asset");
        var entries=new List<ArtEntry>();
        foreach(var relative in ArtPaths) {
            var path="Assets/Art/"+relative;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer) throw new InvalidOperationException("No TextureImporter for "+path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100; importer.alphaIsTransparency=!relative.StartsWith("Backgrounds/");
            importer.alphaSource=relative.StartsWith("Backgrounds/") ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled=false; importer.filterMode=FilterMode.Bilinear; importer.maxTextureSize=2048;
            // Borders inspected against supplied rectangular parchment edges, not guessed sheet slicing.
            if(relative.StartsWith("UI/")) importer.spriteBorder=relative.Contains("Dialogue") ? new Vector4(36,36,36,36) : new Vector4(22,22,22,22);
            importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(!sprite) throw new InvalidOperationException("Sprite import failed: "+path);
            entries.Add(new ArtEntry { key=Path.GetFileNameWithoutExtension(path),sprite=sprite });
        }
        art.entries=entries.ToArray(); art.bodyFont=body; art.headingFont=HeadingFont(body);
        var materialPath=Generated+"/PaperUnlit.mat";
        art.worldMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!art.worldMaterial) {
            var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if(!shader) throw new InvalidOperationException("Cannot find a sprite shader in the current render pipeline.");
            art.worldMaterial=new Material(shader); AssetDatabase.CreateAsset(art.worldMaterial,materialPath);
        }
        EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
        Scene scene=File.Exists(ScenePath) ? EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==OwnedRoot);
        bool mock=true; string api="/api/narrate";
        if(old) { var oldClient=old.GetComponentInChildren<NarratorClient>(); if(oldClient) { mock=oldClient.UseMockNarrator; api=oldClient.ApiUrl; } UnityEngine.Object.DestroyImmediate(old); }
        var root=new GameObject(OwnedRoot);
        var cameraGO=Child("Main Camera",root.transform); cameraGO.tag="MainCamera";
        var camera=cameraGO.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=4.705f;
        camera.transform.position=new Vector3(0,0,-10); camera.backgroundColor=new Color(.09f,.12f,.17f); camera.clearFlags=CameraClearFlags.SolidColor;
        cameraGO.AddComponent<AudioListener>(); cameraGO.AddComponent<UniversalAdditionalCameraData>();
        // Sprites deliberately use an unlit URP 2D material: no Global Light is required.
        var managerGO=Child("GameManager",root.transform);
        var game=managerGO.AddComponent<GameManager>();
        game.levels=managerGO.AddComponent<LevelController>(); game.client=managerGO.AddComponent<NarratorClient>();
        game.ui=managerGO.AddComponent<NarratorUI>(); game.executor=managerGO.AddComponent<ActionExecutor>();
        game.inventory=managerGO.AddComponent<Inventory>(); game.world=managerGO.AddComponent<WorldRegistry>(); game.challenges=managerGO.AddComponent<ChallengeManager>();
        game.client.UseMockNarrator=mock; game.client.ApiUrl=api;
        game.world.art=art; game.world.game=game; game.world.actorRoot=Child("LevelActors",root.transform).transform;
        game.levels.world=game.world; game.levels.inventory=game.inventory; game.levels.sceneCamera=camera;
        var background=Child("Background",root.transform).AddComponent<SpriteRenderer>(); background.sprite=art.Get(LevelDefinition.Environments[0].background); background.sortingOrder=-100; background.sharedMaterial=art.worldMaterial; game.levels.background=background;
        var jack=Child("Jack",root.transform).AddComponent<JackController>(); jack.game=game;
        jack.visual=Child("Art",jack.transform).AddComponent<SpriteRenderer>(); jack.visual.sharedMaterial=art.worldMaterial; jack.visual.sortingOrder=12;
        jack.idle=art.Get("jack_idle"); jack.walk1=art.Get("jack_walk1"); jack.walk2=art.Get("jack_walk2"); jack.climb=art.Get("jack_climb");
        jack.visual.sprite=jack.idle; jack.visual.transform.localScale=Vector3.one*(1.8f/jack.idle.bounds.size.y); jack.transform.position=new Vector3(-2,.15f,0); game.levels.jack=jack;
        game.executor.world=game.world; game.executor.levels=game.levels; game.executor.inventory=game.inventory; game.executor.challenges=game.challenges;
        game.ui.game=game;
        BuildUI(root.transform,game,art);
        WireStory(game);
        var events=Child("EventSystem",root.transform); events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath);
        var scenes=EditorBuildSettings.scenes.ToList(); if(!scenes.Any(s=>s.path==ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath,true)); EditorBuildSettings.scenes=scenes.ToArray(); }
        Selection.activeGameObject=managerGO;
        Debug.Log("JackGame created/refreshed. Runtime actors populate on Play. Art: 29/29. UseMockNarrator="+mock+". See Documentation/UNITY_MANUAL_STEPS.md.");
    }
    [MenuItem("Tools/Jack/Upgrade Story and Cutscenes")]
    public static void UpgradeStory()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode before upgrading the story."); return; }
        if(!File.Exists(ScenePath)) throw new InvalidOperationException("JackGame is missing. Use Tools > Jack > Create or Refresh Demo Scene first.");
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BackupScene();
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var game=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<GameManager>(true)).FirstOrDefault();
        if(!game || !game.ui || !game.world || !game.world.art) throw new InvalidOperationException("Existing GameManager wiring is incomplete; restore the scene backup or run Create or Refresh Demo Scene.");
        WireStory(game);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath);
        Selection.activeGameObject=game.gameObject;
        Debug.Log("Story upgrade complete. Existing gameplay/UI objects and API settings were preserved. A scene backup is in Documentation/SceneBackups.");
    }
    static void BackupScene()
    {
        if(!File.Exists(ScenePath)) return;
        Directory.CreateDirectory("Documentation/SceneBackups");
        var path="Documentation/SceneBackups/JackGame-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+".unity";
        File.Copy(ScenePath,path,false);
        Debug.Log("Before scene changes, saved backup: "+path);
    }
    static void WireStory(GameManager game)
    {
        game.story=game.GetComponent<StoryState>() ?? game.gameObject.AddComponent<StoryState>();
        game.cutscenes=game.GetComponent<CutsceneDirector>() ?? game.gameObject.AddComponent<CutsceneDirector>();
        game.cutscenes.game=game; game.levels.story=game.story;
        var ui=game.ui; var art=game.world.art;
        foreach(var path in ArtPaths) if(!art.Get(Path.GetFileNameWithoutExtension(path)))
            throw new InvalidOperationException("Missing sprite reference in ArtCatalog: Assets/Art/"+path+". Restore the sprite/reference before upgrading.");
        var canvas=ui.input.GetComponentInParent<Canvas>();
        if(!canvas) throw new InvalidOperationException("Narrator input must belong to the existing Canvas.");
        if(!ui.storyFade) {
            var existing=canvas.transform.Find("Story fade");
            ui.storyFade=existing ? existing.GetComponent<Image>() : Panel("Story fade",canvas.transform,null,0,0,1,1,Color.clear);
            ui.storyFade.raycastTarget=false; ui.storyFade.transform.SetAsFirstSibling();
        }
        if(!ui.skipCutscene) ui.skipCutscene=FindOrAddButton("Skip cutscene",canvas.transform,"Skip scene",art,.56f,.39f,.19f,.065f);
        if(!ui.revealText) ui.revealText=FindOrAddButton("Reveal narration",canvas.transform,"Show text",art,.56f,.39f,.19f,.055f);
        ui.skipCutscene.gameObject.SetActive(false); ui.revealText.gameObject.SetActive(false);
        ui.submit.GetComponentInChildren<TMP_Text>(true).text="Ask";
        ui.next.GetComponentInChildren<TMP_Text>(true).text="Next"; ui.next.gameObject.SetActive(false);
        ui.objective.text="PROLOGUE · The Worst Trade in History";
        ui.lastRequest.text="Next continues narration · Skip scene preserves story progress";
        ui.dialogue.text="Jack's family had an empty cupboard, an overdue rent bill, and one cow.";
        EditorUtility.SetDirty(game); EditorUtility.SetDirty(game.levels); EditorUtility.SetDirty(ui); EditorUtility.SetDirty(game.cutscenes);
    }
    static Button FindOrAddButton(string name,Transform canvas,string label,ArtCatalog art,float x,float y,float w,float h)
    { var existing=canvas.Find(name); return existing ? existing.GetComponent<Button>() : Button(name,canvas,label,art,x,y,w,h); }

    static bool ExactFileExists(string path)
    {
        string current=".";
        foreach(var part in path.Split('/')) { if(!Directory.Exists(current) || !Directory.EnumerateFileSystemEntries(current).Any(p=>Path.GetFileName(p)==part)) return false; current=Path.Combine(current,part); }
        return File.Exists(current);
    }
    static void WriteAssetReport(string[] missing)
    {
        const string path="Documentation/UNITY_MANUAL_STEPS.md";
        Directory.CreateDirectory("Documentation");
        string text=File.Exists(path) ? File.ReadAllText(path) : "# Unity manual steps\n";
        const string marker="<!-- ASSET_REPORT -->";
        int start=text.IndexOf(marker,StringComparison.Ordinal); if(start>=0) text=text.Substring(0,start);
        File.WriteAllText(path,text.TrimEnd()+"\n\n"+marker+"\n## Latest builder asset audit\n"+(missing.Length==0 ? "All 29 exact supplied PNG paths found. No missing art.\n" : string.Join("\n",missing.Select(p=>"- Missing: `"+p+"`"))+"\n"));
    }
    static T LoadOrCreate<T>(string path) where T:ScriptableObject
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);
        if(!asset) { asset=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,path); }
        return asset;
    }
    static TMP_FontAsset HeadingFont(TMP_FontAsset fallback)
    {
        string path=Generated+"/PixelifySans SDF.asset";
        var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); if(asset) return asset;
        var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PixelifySans/PixelifySans.ttf");
        if(!font) { Debug.LogWarning("Pixelify Sans unavailable; using TMP Essentials font. See manual font instructions."); return fallback; }
        asset=TMP_FontAsset.CreateFontAsset(font);
        if(!asset) { Debug.LogWarning("Pixelify Sans TMP generation failed; using readable TMP fallback."); return fallback; }
        asset.name="PixelifySans SDF"; asset.TryAddCharacters("Narrator");
        AssetDatabase.CreateAsset(asset,path);
        AssetDatabase.AddObjectToAsset(asset.material,asset);
        foreach(var texture in asset.atlasTextures) if(texture) AssetDatabase.AddObjectToAsset(texture,asset);
        EditorUtility.SetDirty(asset); return asset;
    }
    static GameObject Child(string name,Transform parent) { var go=new GameObject(name); go.transform.SetParent(parent,false); return go; }
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform)); var rect=(RectTransform)go.transform; rect.SetParent(parent,false);
        rect.anchorMin=new Vector2(x,y); rect.anchorMax=new Vector2(x+w,y+h); rect.offsetMin=rect.offsetMax=Vector2.zero; return rect;
    }
    static Image Panel(string name,Transform parent,Sprite sprite,float x,float y,float w,float h,Color color)
    {
        var img=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>(); img.sprite=sprite; img.color=color;
        if(sprite && sprite.border!=Vector4.zero) img.type=Image.Type.Sliced;
        return img;
    }
    static TMP_Text Text(string name,Transform parent,string value,TMP_FontAsset font,float size,float x,float y,float w,float h,Color color)
    {
        var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>(); text.font=font; text.text=value; text.fontSize=size;
        text.enableAutoSizing=true; text.fontSizeMin=size*.8f; text.fontSizeMax=size; text.color=color; text.richText=false; text.raycastTarget=false;
        text.alignment=TextAlignmentOptions.MidlineLeft; text.overflowMode=TextOverflowModes.Ellipsis; return text;
    }
    static Button Button(string name,Transform parent,string value,ArtCatalog art,float x,float y,float w,float h)
    {
        var image=Panel(name,parent,art.Get("ParchmentButton"),x,y,w,h,Color.white); var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        var colors=button.colors; colors.highlightedColor=new Color(1,.9f,.65f); colors.pressedColor=new Color(.8f,.65f,.4f); colors.disabledColor=new Color(.65f,.65f,.65f); button.colors=colors;
        Text("Label",image.transform,value,art.bodyFont,26,.07f,.1f,.86f,.8f,Ink).alignment=TextAlignmentOptions.Center;
        return button;
    }
    static void BuildUI(Transform root,GameManager game,ArtCatalog art)
    {
        var canvasGO=Child("Canvas",root); var canvas=canvasGO.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasGO.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=.5f;
        canvasGO.AddComponent<GraphicRaycaster>(); var ui=game.ui;
        var top=Panel("Title bar",canvas.transform,null,0,.87f,1,.13f,new Color(.08f,.1f,.13f,.94f));
        Text("Title",top.transform,"JACK AND THE UNRELIABLE NARRATOR",art.bodyFont,32,.025f,.5f,.95f,.43f,new Color(1,.88f,.6f));
        ui.objective=Text("Objective",top.transform,LevelDefinition.Beats[0].objective,art.bodyFont,25,.025f,.08f,.67f,.38f,Color.white);
        ui.inventoryText=Text("Inventory",top.transform,"Satchel: cow",art.bodyFont,22,.69f,.08f,.24f,.38f,Color.white);
        ui.beanIcon=Panel("Bean inventory",canvas.transform,art.Get("BeanIcon"),.92f,.88f,.028f,.045f,Color.white); ui.beanIcon.preserveAspect=true;
        ui.eggIcon=Panel("Goose treasure inventory",canvas.transform,art.Get("EggIcon"),.957f,.88f,.028f,.045f,Color.white); ui.eggIcon.preserveAspect=true;
        var dialoguePanel=Panel("Parchment dialogue",canvas.transform,art.Get("ParchmentDialoguePanel"),.02f,.115f,.96f,.255f,Color.white);
        ui.heading=Text("Narrator",dialoguePanel.transform,"Narrator",art.headingFont,40,.03f,.73f,.35f,.23f,Ink);
        ui.status=Text("Status",dialoguePanel.transform,"Offline narrator",art.bodyFont,20,.4f,.76f,.57f,.17f,Ink);
        var narrationViewport=Rect("Dialogue viewport",dialoguePanel.transform,.03f,.23f,.94f,.49f);
        narrationViewport.gameObject.AddComponent<RectMask2D>();
        var narrationHitArea=narrationViewport.gameObject.AddComponent<Image>(); narrationHitArea.color=Color.clear;
        ui.dialogueScroll=narrationViewport.gameObject.AddComponent<ScrollRect>(); ui.dialogueScroll.viewport=narrationViewport;
        ui.dialogueScroll.horizontal=false; ui.dialogueScroll.vertical=true; ui.dialogueScroll.movementType=ScrollRect.MovementType.Clamped;
        ui.dialogue=Text("Dialogue",narrationViewport,LevelDefinition.Beats[0].introduction,art.bodyFont,28,0,0,1,1,Ink);
        ui.dialogue.enableAutoSizing=false; ui.dialogue.alignment=TextAlignmentOptions.TopLeft; ui.dialogue.overflowMode=TextOverflowModes.Overflow;
        ui.dialogue.rectTransform.anchorMin=new Vector2(0,1); ui.dialogue.rectTransform.anchorMax=Vector2.one; ui.dialogue.rectTransform.pivot=new Vector2(.5f,1);
        ui.dialogueScroll.content=ui.dialogue.rectTransform;
        ui.lastRequest=Text("Last request",dialoguePanel.transform,"A/D or arrows · click a character, or approach and press E",art.bodyFont,19,.03f,.05f,.94f,.15f,new Color(.4f,.29f,.21f));
        var field=Panel("Player request",canvas.transform,art.Get("ParchmentInput"),.02f,.02f,.75f,.085f,Color.white);
        ui.input=field.gameObject.AddComponent<TMP_InputField>(); ui.input.targetGraphic=field; ui.input.characterLimit=400; ui.input.lineType=TMP_InputField.LineType.SingleLine;
        var viewport=Rect("Viewport",field.transform,.025f,.15f,.95f,.7f); viewport.gameObject.AddComponent<RectMask2D>(); ui.input.textViewport=viewport;
        ui.input.textComponent=(TMP_Text)Text("Text",viewport,"",art.bodyFont,27,0,0,1,1,Ink); ui.input.textComponent.enableAutoSizing=false;
        var placeholder=Text("Placeholder",viewport,"Narrator, could you...",art.bodyFont,27,0,0,1,1,new Color(.45f,.35f,.25f)); ui.input.placeholder=placeholder;
        ui.input.fontAsset=art.bodyFont; ui.input.pointSize=27;
        ui.submit=Button("Submit",canvas.transform,"Submit",art,.79f,.02f,.19f,.085f);
        ui.next=Button("Next",canvas.transform,"Next chapter",art,.77f,.39f,.21f,.065f); ui.next.gameObject.SetActive(false);
        ui.restart=Button("Restart",canvas.transform,"Restart",art,.02f,.79f,.13f,.055f); ui.restart.gameObject.SetActive(false);
        var victory=Panel("Victory",canvas.transform,art.Get("ParchmentDialoguePanel"),.15f,.42f,.7f,.36f,Color.white);
        ui.victoryPanel=victory.gameObject; ui.victoryText=Text("Ending",victory.transform,"",art.bodyFont,32,.06f,.08f,.88f,.84f,Ink); ui.victoryText.alignment=TextAlignmentOptions.Center; victory.gameObject.SetActive(false);
    }
}
#endif
