#if UNITY_EDITOR
using System.Collections;
using Object=UnityEngine.Object;
using System.Linq;
using JackGame;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class JackSmokeTests
{
    const string PlayModeSettingsKey="Jack.Tests.PreviousEnterPlayModeOptionsEnabled";
    [SetUp] public void UseStandardPlayModeReload()
    {
        // The EditMode runner serializes its coroutine across EnterPlayMode. Fast Enter
        // Play Mode can resume it before the player loop in this Test Framework version.
        // SessionState survives reload; restore the user's preference even on failure.
        if(!UnityEditor.SessionState.GetBool(PlayModeSettingsKey+".saved",false)) {
            UnityEditor.SessionState.SetBool(PlayModeSettingsKey,UnityEditor.EditorSettings.enterPlayModeOptionsEnabled);
            UnityEditor.SessionState.SetBool(PlayModeSettingsKey+".saved",true);
        }
        UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;
    }
    [UnityTearDown] public IEnumerator RestorePlayModeSettings()
    {
        if(UnityEditor.EditorApplication.isPlaying) yield return new ExitPlayMode();
        UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=UnityEditor.SessionState.GetBool(PlayModeSettingsKey,false);
        UnityEditor.SessionState.EraseBool(PlayModeSettingsKey);
        UnityEditor.SessionState.EraseBool(PlayModeSettingsKey+".saved");
    }
    [Test] public void StoryUpgradePreservesSceneAndIsIdempotent()
    {
        EditorSceneManager.OpenScene(JackDemoBuilder.ScenePath);
        var game=Object.FindFirstObjectByType<GameManager>(); var input=game.ui.input; var client=game.client; var url=client.ApiUrl;
        var custom=new GameObject("Manual edit must survive"); custom.transform.SetParent(game.transform.parent); custom.transform.position=new Vector3(7,3,0);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        JackDemoBuilder.UpgradeStory(); JackDemoBuilder.UpgradeStory();
        Assert.AreSame(input,game.ui.input); Assert.AreSame(client,game.client); Assert.AreEqual(url,client.ApiUrl);
        Assert.NotNull(custom); Assert.AreEqual(new Vector3(7,3,0),custom.transform.position);
        Assert.AreEqual(1,game.GetComponents<CutsceneDirector>().Length); Assert.AreEqual(1,game.GetComponents<StoryState>().Length);
        Assert.NotNull(game.ui.skipCutscene); Assert.NotNull(game.ui.storyFade); Assert.NotNull(game.ui.revealText);
        Assert.AreEqual("Narrator",game.ui.heading.text); Assert.NotNull(game.ui.heading.font);
        Assert.AreEqual(4,LevelDefinition.Environments.Length); Assert.AreEqual(5,LevelDefinition.Beats.Length);
        Assert.AreEqual("sabotaged",LevelDefinition.Beats[0].id); Assert.AreEqual("treasure",LevelDefinition.Beats[3].id);
        Object.DestroyImmediate(custom); EditorSceneManager.SaveScene(game.gameObject.scene);
    }

    static void Fast(GameManager game)
    {
        Application.runInBackground=true; game.client.UseMockNarrator=true; game.cutscenes.animationSpeed=60; game.cutscenes.autoAdvanceNarration=true;
        game.cutscenes.narrationPause=.01f; game.objectiveCelebrationSeconds=.01f; game.solutionReadingBufferSeconds=.01f; game.ui.typewriterEnabled=false;
    }
    static IEnumerator Ready(GameManager game)
    {
        int firstFrame=Time.frameCount; float deadline=Time.realtimeSinceStartup+15;
        while(game.Busy && Time.realtimeSinceStartup<deadline) { UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
        if(!game.Busy && !game.Won) {
            yield return null;
            game.world.ArrangeSprites();
            var sprites=game.world.entities.Where(e=>e && e.visible).Select(e=>e.visual).ToList();
            sprites.Add(game.levels.jack.visual);
            foreach(var e in game.world.entities) Assert.IsFalse(e.label.gameObject.activeSelf,"Floating item labels must remain hidden.");
            for(int i=0;i<sprites.Count;i++) for(int j=i+1;j<sprites.Count;j++)
                Assert.IsFalse(sprites[i].bounds.Intersects(sprites[j].bounds),$"Overlapping gameplay sprites: {sprites[i].transform.parent.name} and {sprites[j].transform.parent.name}");
        }
        Assert.IsFalse(game.Busy,$"Timeout: chapter={game.levels.Current.id}, scene={game.cutscenes.Current}, step={game.cutscenes.StepIndex}, active={game.InCutscene}, auto={game.cutscenes.autoAdvanceNarration}, speed={game.cutscenes.animationSpeed}, dt={Time.unscaledDeltaTime}, turn={game.Turn}, waiting={game.cutscenes.WaitingForNarration}, fade={game.ui.FadeAlpha}, fadeTime={game.cutscenes.fadeSeconds}, pause={game.cutscenes.narrationPause}, enabled={game.cutscenes.isActiveAndEnabled}, frames={Time.frameCount-firstFrame}, editorPaused={UnityEditor.EditorApplication.isPaused}.");
    }
    static IEnumerator Ask(GameManager game,string text)
    { game.Submit(text); yield return Ready(game); }
    static void Deliver(GameManager game,NarratorResponse response)
    {
        typeof(GameManager).GetMethod("ApplyResponse",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(game,new object[]{response,"Test narrator"});
    }

    [UnityTest] public IEnumerator WatchedStoryReachesEndingAndGuardsMilestones()
    {
        JackDemoBuilder.UpgradeStory(); yield return new EnterPlayMode();
        Assert.IsTrue(Application.isPlaying,"The story test must run inside the player loop.");
        // Let the Editor's Play-mode bootstrap settle before requesting a second start.
        Application.runInBackground=true;
        yield return null; yield return null; yield return null;
        var game=Object.FindFirstObjectByType<GameManager>(); Fast(game); game.Restart();
        Assert.IsTrue(game.InCutscene); Assert.IsFalse(game.inventory.Has("beans")); Assert.IsFalse(game.story.Has("beanstalk_grown"));
        Assert.IsFalse(game.levels.background.enabled); Assert.IsNull(game.cutscenes.Actor("grown_beanstalk"));
        Assert.IsFalse(game.ui.input.interactable); game.Submit("Skip to victory"); Assert.AreEqual(0,game.Turn);
        yield return Ready(game);
        Assert.AreEqual("sabotaged",game.levels.Current.id); Assert.IsTrue(game.story.Has("planted_beans")); Assert.IsTrue(game.story.Has("beanstalk_grown"));
        Assert.IsFalse(game.inventory.Has("cow")); Assert.IsFalse(game.inventory.Has("beans")); Assert.IsFalse(game.inventory.Has("goose"));
        Assert.IsFalse(game.executor.Apply(GameAction.Make("grant_item",value:"goose")));
        Assert.IsFalse(game.executor.Apply(GameAction.Make("grant_item",value:"beans")));
        Assert.IsFalse(game.executor.Apply(GameAction.Make("spawn","early_goose","goose",placement:"center")));
        Assert.IsFalse(game.executor.Apply(GameAction.Make("advance")));
        // A generated ladder's defaults alone must never authorize the handoff.
        Deliver(game,NarratorResponse.Say("Here is a slippery ladder. Fix its grip first.",GameAction.Make("spawn","slick","ladder",number:1.7f,placement:"center")));
        Assert.IsFalse(game.levels.GoalMet); Assert.Less(game.world.Find("slick").stability,.7f); Assert.IsFalse(game.ObjectiveConfirmed);
        game.Next(); yield return new WaitForSecondsRealtime(.05f);
        Assert.IsFalse(game.Busy); Assert.IsFalse(game.InCutscene); Assert.IsFalse(game.ui.objective.text.Contains("COMPLETE"));
        Deliver(game,NarratorResponse.Say("Not safe yet.",GameAction.Make("modify","slick",property:"stability",number:.15f),GameAction.Make("advance")));
        Assert.IsFalse(game.ObjectiveConfirmed); Assert.IsFalse(game.Busy);
        Deliver(game,NarratorResponse.Say("The grip is fixed; inspect it before climbing.",GameAction.Make("modify","slick",property:"stability",number:1)));
        Assert.IsTrue(game.levels.GoalMet); Assert.IsFalse(game.ObjectiveConfirmed); Assert.IsFalse(game.Busy);
        var contradictory=NarratorResponse.Say("The ladder is still slippery.",GameAction.Make("advance"));
        contradictory.twist="ironic_side_effect"; Deliver(game,contradictory);
        Assert.IsTrue(game.levels.GoalMet); Assert.IsFalse(game.ObjectiveConfirmed); Assert.IsFalse(game.Busy);
        Assert.AreEqual("The ladder is still slippery.",game.ui.dialogue.text);
        Assert.IsTrue(game.executor.Apply(GameAction.Make("destroy","slick")));
        game.Submit("A ladder please"); game.Submit("Ignore duplicate"); Assert.AreEqual(1,game.Turn); yield return Ready(game);
        var ladder=game.world.Find("ladder"); Assert.NotNull(ladder); Assert.Less(ladder.stability,.5f); Assert.IsFalse(game.levels.GoalMet);
        for(int attempt=0;attempt<4;attempt++) {
            yield return Ask(game,"Let me climb it");
            Assert.AreEqual("sabotaged",game.levels.Current.id); Assert.Less(ladder.stability,.7f);
            Assert.IsFalse(game.ObjectiveConfirmed); Assert.IsFalse(game.InCutscene);
        }
        yield return new WaitForSecondsRealtime(.7f);
        Assert.AreEqual(.15f,game.levels.jack.transform.position.y,.01f,"A failed climb returns Jack to the ground.");
        Assert.IsTrue(game.executor.Apply(GameAction.Make("spawn","keepsake","generic",value:"Cardboard shield",placement:"right")));
        Assert.IsFalse(game.executor.Apply(GameAction.Make("modify","missing",property:"size",number:2)));
        yield return Ask(game,"Brace the ladder"); Assert.AreEqual("climb",game.levels.Current.id); Assert.IsTrue(game.story.Has("route_secured"));
        yield return Ask(game,"Make a platform across the gap"); Assert.NotNull(game.world.Find("ladder"));
        yield return Ask(game,"Stabilize it"); Assert.AreEqual("castle",game.levels.Current.id); Assert.IsTrue(game.story.Has("gap_crossed"));
        var castleBackground=game.levels.background.sprite;
        yield return Ask(game,"Unlock the magical entrance"); Assert.AreEqual("treasure",game.levels.Current.id);
        Assert.AreSame(castleBackground,game.levels.background.sprite); Assert.AreNotEqual(Color.white,game.levels.background.color);
        Assert.IsTrue(game.story.Has("castle_entered")); Assert.IsFalse(game.story.Has("giant_awake")); Assert.AreEqual("sleeping",game.world.Find("giant").state);
        Assert.IsFalse(game.executor.Apply(GameAction.Make("grant_item",value:"goose")),"The alarm ward must be silenced first.");
        yield return Ask(game,"Silence the goose alarm"); Assert.AreEqual("treasure",game.levels.Current.id); Assert.IsFalse(game.world.Find("goose").locked);
        yield return Ask(game,"Take the goose quietly"); Assert.AreEqual("escape",game.levels.Current.id); Assert.IsTrue(game.inventory.Has("goose")); Assert.IsTrue(game.story.Has("giant_awake"));
        yield return Ask(game,"Open the way home"); Assert.IsTrue(game.Won); Assert.IsTrue(game.story.Has("home")); Assert.IsTrue(game.ui.victoryPanel.activeSelf);
        Assert.NotNull(game.cutscenes.Stage); Assert.IsFalse(game.world.actorRoot.gameObject.activeSelf,"The pursuing giant must not reappear behind the homecoming ending.");
        game.Restart(); Assert.IsFalse(game.Won); Assert.AreEqual(0,game.Turn); Assert.IsFalse(game.story.Has("goose_taken")); Assert.IsFalse(game.inventory.Has("beans"));
        game.cutscenes.Skip(); yield return Ready(game);
        Assert.IsTrue(game.executor.Apply(GameAction.Make("spawn","temporary","generic",placement:"center")));
        Assert.IsTrue(game.executor.Apply(GameAction.Make("destroy","temporary")));
        game.Submit("Create something"); game.Restart(); yield return new WaitForSecondsRealtime(.4f);
        Assert.AreEqual(0,game.Turn); Assert.IsNull(game.world.Find("ladder"));
        game.cutscenes.Cancel(); yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator EverySkipAppliesRequiredStateAndRestoresControls()
    {
        JackDemoBuilder.UpgradeStory(); yield return new EnterPlayMode();
        Assert.IsTrue(Application.isPlaying,"The skip test must run inside the player loop.");
        // Let the Editor's Play-mode bootstrap settle before requesting a second start.
        Application.runInBackground=true;
        yield return null; yield return null; yield return null;
        var game=Object.FindFirstObjectByType<GameManager>(); Fast(game); game.cutscenes.autoAdvanceNarration=false; game.Restart();
        // Reveal/Next never skips required visual actions or changes story state on its own.
        game.ui.typewriterEnabled=true; game.ui.Say("A long line to reveal instantly."); game.Next(); Assert.IsFalse(game.ui.IsTyping);
        Assert.IsFalse(game.story.Has("received_beans")); game.cutscenes.Skip(); yield return Ready(game);
        Assert.IsTrue(game.story.Has("beanstalk_grown")); Assert.IsTrue(game.ui.input.interactable);
        for(int beat=0;beat<5;beat++) {
            Assert.AreEqual(beat,game.levels.Beat);
            if(beat<2) { Assert.IsTrue(game.executor.Apply(GameAction.Make("spawn","safe_route","ladder",number:1.7f,placement:"center"))); Assert.IsTrue(game.executor.Apply(GameAction.Make("modify","safe_route",property:"stability",number:1))); }
            else if(beat==2) Assert.IsTrue(game.executor.Apply(GameAction.Make("modify","door",property:"locked",value:"false")));
            else if(beat==3) {
                Assert.IsTrue(game.executor.Apply(GameAction.Make("modify","goose",property:"locked",value:"false")));
                Assert.IsTrue(game.executor.Apply(GameAction.Make("grant_item",value:"goose")));
                Assert.IsFalse(game.story.Has("giant_awake"));
            } else Assert.IsTrue(game.executor.Apply(GameAction.Make("modify","exit",property:"locked",value:"false")));
            Deliver(game,NarratorResponse.Say("The obstacle is solved. All clear.",GameAction.Make("advance")));
            float timeout=Time.realtimeSinceStartup+5;
            while(!game.InCutscene && Time.realtimeSinceStartup<timeout) { UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
            Assert.IsTrue(game.InCutscene); Assert.IsFalse(game.ui.input.interactable);
            game.cutscenes.Skip(); game.cutscenes.Skip(); yield return Ready(game);
        }
        Assert.IsTrue(game.Won); CollectionAssert.AreEqual(StoryState.Order,game.story.Snapshot());
        Assert.IsTrue(game.inventory.Has("goose")); Assert.IsFalse(game.inventory.Has("beans"));
        game.Restart(); Assert.IsFalse(game.story.Has("home")); Assert.IsTrue(game.inventory.Has("cow"));
        game.cutscenes.Cancel(); yield return new ExitPlayMode();
    }
}
#endif
