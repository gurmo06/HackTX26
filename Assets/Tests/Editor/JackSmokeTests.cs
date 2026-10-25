#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using JackGame;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class JackSmokeTests
{
    [Test] public void SceneBuilderIsIdempotentAndWired()
    {
        JackDemoBuilder.Build(); JackDemoBuilder.Build();
        var roots=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        Assert.AreEqual(1,roots.Count(g=>g.name=="__JackDemo"));
        var game=Object.FindFirstObjectByType<GameManager>();
        Assert.NotNull(game); Assert.NotNull(game.world.art); Assert.AreEqual(29,game.world.art.entries.Length);
        Assert.AreEqual("Narrator",game.ui.heading.text); Assert.NotNull(game.ui.heading.font);
        Assert.NotNull(game.ui.input.textComponent); Assert.NotNull(game.ui.submit); Assert.NotNull(game.levels.jack.idle);
        Assert.AreEqual(4,LevelDefinition.Environments.Length); Assert.AreEqual(5,LevelDefinition.Beats.Length);
    }

    [UnityTest] public IEnumerator OfflineStoryReachesVictoryAndRestarts()
    {
        JackDemoBuilder.Build();
        yield return new EnterPlayMode();
        var game=Object.FindFirstObjectByType<GameManager>(); game.client.UseMockNarrator=true; game.Restart();
        yield return null;
        Assert.IsFalse(game.levels.GoalMet);
        game.Submit("Give me some beans, please");
        game.Submit("This double click must be ignored");
        Assert.AreEqual(1,game.Turn);
        yield return new WaitUntil(()=>!game.Busy);
        Assert.NotNull(game.world.Find("jellybeans")); Assert.IsFalse(game.inventory.Has("beans"));
        game.Submit("Trade my cow for real magic beans"); yield return new WaitUntil(()=>!game.Busy);
        Assert.IsTrue(game.inventory.Has("beans")); Assert.IsFalse(game.world.Find("cow").visible);
        Assert.IsTrue(game.levels.GoalMet); game.Next(); yield return new WaitUntil(()=>!game.Busy);
        var background=game.levels.background.sprite;
        game.Submit("Grow a sturdy beanstalk to the clouds"); yield return new WaitUntil(()=>!game.Busy);
        Assert.IsTrue(game.levels.GoalMet); Assert.GreaterOrEqual(game.world.Find("beanstalk").size,1.5f);
        game.Next(); yield return new WaitUntil(()=>!game.Busy);
        Assert.AreEqual(background,game.levels.background.sprite);
        game.Submit("Give me a ladder across the gap"); yield return new WaitUntil(()=>!game.Busy);
        Assert.Less(game.world.Find("ladder").stability,.5f); Assert.IsFalse(game.levels.GoalMet);
        Assert.IsFalse(game.executor.Apply(GameAction.Make("modify","missing",property:"size",number:2)));
        game.Submit("Brace that ladder and stand it upright"); yield return new WaitUntil(()=>!game.Busy);
        Assert.IsTrue(game.levels.GoalMet); game.Next(); yield return new WaitUntil(()=>!game.Busy);
        game.Submit("Compliment the hinges and unlock the castle door"); yield return new WaitUntil(()=>!game.Busy);
        Assert.IsTrue(game.levels.GoalMet); game.Next(); yield return new WaitUntil(()=>!game.Busy);
        game.Submit("Take the goose and quietly open the escape gate"); yield return new WaitUntil(()=>!game.Busy);
        Assert.IsTrue(game.levels.GoalMet); game.Next(); Assert.IsTrue(game.Won);
        game.Restart(); Assert.AreEqual(0,game.Turn); Assert.IsFalse(game.Won); Assert.IsTrue(game.inventory.Has("cow")); Assert.IsFalse(game.inventory.Has("goose"));
        game.Submit("Give me something"); game.Restart();
        yield return new WaitForSecondsRealtime(.4f);
        Assert.AreEqual(0,game.Turn); Assert.IsNull(game.world.Find("jellybeans"));
        yield return new ExitPlayMode();
    }
}
#endif
