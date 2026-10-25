using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JackGame
{
    // Story content composes reusable motion primitives; transport, skipping and state are centralized.
    public static class StorySequences
    {
        static readonly Color Paper=new Color(.91f,.81f,.62f);
        static Vector3 P(float x,float y=-.7f) => new Vector3(x,y,0);
        public static string Title(StoryCutscene scene)
        {
            switch(scene) {
                case StoryCutscene.Prologue: return "PROLOGUE · The Worst Trade in History";
                case StoryCutscene.BeginClimb: return "Jack Begins Climbing";
                case StoryCutscene.CloudKingdom: return "The Cloud Kingdom";
                case StoryCutscene.TreasureRoom: return "The Giant's Treasure";
                case StoryCutscene.Awakening: return "The Giant Awakens";
                default: return "EPILOGUE · Happily Ever After?";
            }
        }
        public static List<StoryStep> Build(CutsceneDirector d,StoryCutscene scene)
        {
            var steps=new List<StoryStep>();
            void Step(string line,System.Func<IEnumerator> animation=null,System.Action enter=null) => steps.Add(new StoryStep(line,animation,enter));
            switch(scene) {
                case StoryCutscene.Prologue:
                    Step("Jack's family had an empty cupboard, an overdue rent bill, and one cow. Naturally, Jack was put in charge of finance.",()=>d.Fade(0),()=> {
                        d.SetBackdrop(null,Paper); d.Add("cottage","cottage",P(-5),2.5f,0);
                        d.Add("jack","jack_idle",P(-8.5f),1.8f); d.Add("cow","cow",P(-10),1.5f);
                    });
                    Step("At the market, a merchant offered five magical beans. The cow would have preferred a second opinion.",()=>ApproachMarket(d),()=>d.Add("merchant","merchant",P(8.5f),2.1f));
                    Step("Jack traded the cow for the beans. An inspired move, if the goal was to disappoint everyone who knew him.",()=>Trade(d),()=> {
                        d.game.story.ReceiveBeans(d.game.inventory); d.Add("beans","BeanIcon",P(-.6f,.6f),.45f,15); d.game.ui.Refresh();
                    });
                    Step("Home again, Jack planted his entire investment portfolio in the dirt.",()=>Plant(d));
                    Step("Night fell. The beans grew into a magnificent beanstalk. I may have loosened a few important bits. For dramatic tension.",()=>Growth(d));
                    break;
                case StoryCutscene.BeginClimb:
                    Step("A viable climbing route! Jack approached it with the confidence of a man who had never read a safety notice.",()=>ApproachRoute(d),()=> {
                        d.SetBackdrop("01_Merchant_Village_Post_Beanstalk",Color.white); d.Add("jack","jack_idle",P(-3),1.8f);
                        var plant=d.Add("beanstalk","beanstalk_segment",P(1),4,3); plant.swayDegrees=1.2f; plant.bobAmount=0;
                        var route=d.game.world.entities.Find(e=>e.visible && e.climbable && e.stability>=.7f && e.id!="beanstalk");
                        if(route) d.Add("route",d.game.world.SpriteKey(route.archetype)=="" ? "wooden_platform" : d.game.world.SpriteKey(route.archetype),P(.3f),2.4f,4);
                    });
                    Step("Up he went. I would tell him to watch his step, but I have already removed the next one.",()=>Ascend(d));
                    break;
                case StoryCutscene.CloudKingdom:
                    Step("Beyond the gap lay the cloud kingdom: excellent views, terrible public transport.",()=>CloudPan(d),()=> {
                        d.SetBackdrop(null,new Color(.66f,.81f,.91f));
                        for(int i=0;i<4;i++) d.Add("cloud"+i,"cloud_platform",P(-6+i*4,-1.4f+i*.06f),.65f,1);
                        d.Add("jack","jack_idle",P(-3),1.8f); d.Add("castle","cloud_castle",P(5,1.3f),1.1f,2);
                    });
                    Step("Jack spotted a castle built for someone with very large feet and very small regard for visitors.",()=>CastleApproaches(d));
                    break;
                case StoryCutscene.TreasureRoom:
                    Step("Through the entrance: treasure, a sleeping giant, and a goose whose feathers were worth more than Jack's entire village.",()=>TreasurePan(d),()=>TreasureStage(d));
                    Step("Jack noticed the golden goose. Its bell was warded. Quiet hands, Jack. The snoring is doing us a favour.",()=>NoticeGoose(d));
                    break;
                case StoryCutscene.Awakening:
                    Step("The goose was safely in Jack's arms. Unfortunately, it regarded silence as an optional feature.",()=>GooseReacts(d),()=> {
                        TreasureStage(d); d.Actor("jack").FootPosition=P(-1.2f); d.Actor("goose").FootPosition=P(-.6f,.4f);
                    });
                    Step("The giant awoke. Jack suddenly discovered an impressive enthusiasm for running.",()=>AwakenAndRun(d),()=>d.game.story.Complete(StoryCutscene.Awakening,d.game.inventory));
                    break;
                case StoryCutscene.Ending:
                    Step("At sunrise Jack returned home with the golden goose. Against my better judgement, he had actually succeeded.",()=>Homecoming(d),()=> {
                        d.SetBackdrop(null,new Color(.26f,.29f,.47f)); d.Add("cottage","cottage",P(3),2.8f,1);
                        d.Add("jack","jack_idle",P(-7),1.8f); d.Add("goose","golden_goose",P(-6.4f,.15f),.75f,12);
                    });
                    Step("The family would never go hungry again. The goose's first golden egg arrived with an invoice for premium feed.",()=>FinalTwist(d));
                    break;
            }
            steps.Add(new StoryStep(null,()=>d.Fade(1),wait:false));
            return steps;
        }
        static IEnumerator ApproachMarket(CutsceneDirector d)
        {
            var jack=d.Actor("jack"); var cow=d.Actor("cow"); var merchant=d.Actor("merchant"); jack.walking=true;
            yield return StoryMotion.Tween(d.Seconds(3),t=> { jack.FootPosition=Vector3.Lerp(P(-8.5f),P(-1.5f),t); cow.FootPosition=Vector3.Lerp(P(-10),P(-3.5f),t); merchant.FootPosition=Vector3.Lerp(P(8.5f),P(1.6f),t); });
            jack.walking=false;
        }
        static IEnumerator Trade(CutsceneDirector d)
        {
            var cow=d.Actor("cow"); var merchant=d.Actor("merchant");
            yield return StoryMotion.Tween(d.Seconds(3),t=> { cow.FootPosition=Vector3.Lerp(P(-3.5f),P(9),t); merchant.FootPosition=Vector3.Lerp(P(1.6f),P(11),t); });
            cow.gameObject.SetActive(false); merchant.gameObject.SetActive(false);
        }
        static IEnumerator Plant(CutsceneDirector d)
        {
            yield return d.Move("jack",P(-3.5f),2,true);
            var beans=d.Actor("beans"); beans.FootPosition=P(-3,.6f);
            yield return StoryMotion.Move(beans.transform,P(-3,-.7f),d.Seconds(1));
            yield return StoryMotion.Scale(beans.transform,Vector3.zero,d.Seconds(.4f));
            d.game.story.PlantBeans(d.game.inventory); d.game.ui.Refresh();
        }
        static IEnumerator Growth(CutsceneDirector d)
        {
            yield return d.Tint(new Color(.22f,.26f,.42f),1.5f);
            var plant=d.Add("grown_beanstalk","beanstalk_segment",P(-2.5f),.05f,3); plant.swayDegrees=1.4f; plant.bobAmount=0;
            yield return StoryMotion.Tween(d.Seconds(3),t=>plant.height=Mathf.Lerp(.05f,4,t));
            d.game.story.GrowBeanstalk(d.game.inventory); d.game.ui.Refresh();
        }
        static IEnumerator ApproachRoute(CutsceneDirector d) { yield return d.Fade(0); yield return d.Move("jack",P(.5f),2.5f,true); }
        static IEnumerator Ascend(CutsceneDirector d)
        {
            var jack=d.Actor("jack"); jack.climbing=true;
            var camera=d.Camera.transform.position; float size=d.Camera.orthographicSize;
            yield return StoryMotion.Tween(d.Seconds(5),t=> { jack.FootPosition=Vector3.Lerp(P(.5f),P(.5f,3),t); d.Camera.transform.position=camera+Vector3.up*t*1.6f; d.Camera.orthographicSize=Mathf.Lerp(size,size*.92f,t); });
        }
        static IEnumerator CloudPan(CutsceneDirector d) { yield return d.Fade(0); yield return d.Pan(new Vector3(1,.3f,-10),d.Camera.orthographicSize,4); }
        static IEnumerator CastleApproaches(CutsceneDirector d)
        {
            var castle=d.Actor("castle");
            yield return StoryMotion.Tween(d.Seconds(4),t=> { castle.height=Mathf.Lerp(1.1f,2.6f,t); castle.FootPosition=Vector3.Lerp(P(5,1.3f),P(3,.5f),t); });
            d.Actor("jack").hopping=true; yield return d.Wait(.5f); d.Actor("jack").hopping=false;
        }
        static void TreasureStage(CutsceneDirector d)
        {
            d.SetBackdrop("03_Giant_Castle",new Color(.57f,.63f,.8f)); d.Add("jack","jack_idle",P(-4.5f),1.8f);
            var giant=d.Add("giant","giant",P(3),3.5f); giant.sleeping=true; giant.breathing=.035f;
            var goose=d.Add("goose","golden_goose",P(.6f),1.1f); goose.hopping=true;
            d.Add("treasure","golden_egg_sack",P(-1),1,4);
        }
        static IEnumerator TreasurePan(CutsceneDirector d) { yield return d.Fade(0); yield return d.Pan(new Vector3(.8f,.2f,-10),d.Camera.orthographicSize*.94f,4); }
        static IEnumerator NoticeGoose(CutsceneDirector d) { yield return d.Move("jack",P(-1.8f),3,true); yield return d.Wait(1); }
        static IEnumerator GooseReacts(CutsceneDirector d) { yield return d.Fade(0); d.Actor("goose").bobAmount=.12f; yield return d.Wait(2.5f); }
        static IEnumerator AwakenAndRun(CutsceneDirector d)
        {
            var giant=d.Actor("giant"); giant.sleeping=false; giant.visual.color=new Color(1,.65f,.55f);
            var camera=d.Camera.transform.position;
            yield return StoryMotion.Tween(d.Seconds(1.5f),t=>d.Camera.transform.position=camera+new Vector3(Mathf.Sin(t*100),Mathf.Cos(t*87),0)*d.shakeStrength*(1-t));
            d.Camera.transform.position=camera; var jack=d.Actor("jack"); var goose=d.Actor("goose"); jack.walking=true; jack.visual.flipX=true;
            yield return StoryMotion.Tween(d.Seconds(4),t=> { giant.FootPosition=Vector3.Lerp(P(3),P(-1),t); jack.FootPosition=Vector3.Lerp(P(-1.2f),P(-9),t); goose.FootPosition=jack.FootPosition+new Vector3(-.55f,.7f,0); });
        }
        static IEnumerator Homecoming(CutsceneDirector d)
        {
            yield return d.Fade(0); var jack=d.Actor("jack"); var goose=d.Actor("goose"); jack.walking=true; var start=d.Backdrop.color;
            yield return StoryMotion.Tween(d.Seconds(5),t=> { d.Backdrop.color=Color.Lerp(start,Paper,t); jack.FootPosition=Vector3.Lerp(P(-7),P(1),t); goose.FootPosition=jack.FootPosition+new Vector3(.6f,.7f,0); }); jack.walking=false;
        }
        static IEnumerator FinalTwist(CutsceneDirector d)
        {
            var egg=d.Add("egg","golden_egg",P(1.9f),.05f,12);
            yield return StoryMotion.Tween(d.Seconds(1),t=>egg.height=Mathf.Lerp(.05f,.65f,t));
            d.Actor("goose").hopping=true; yield return d.Wait(2);
        }
    }
}
