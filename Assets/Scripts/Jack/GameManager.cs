using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JackGame
{
    public class GameManager : MonoBehaviour
    {
        public LevelController levels;
        public NarratorClient client;
        public NarratorUI ui;
        public ActionExecutor executor;
        public Inventory inventory;
        public WorldRegistry world;
        public ChallengeManager challenges;
        public StoryState story;
        public CutsceneDirector cutscenes;
        [Min(0)] public float objectiveCelebrationSeconds=1.8f;
        public bool InCutscene => cutscenes && cutscenes.IsPlaying;
        public bool Busy { get; private set; }
        public bool RequestInFlight { get; private set; }
        public bool Won { get; private set; }
        public int Turn { get; private set; }
        public int Setbacks { get; private set; }
        readonly List<HistoryEntry> history=new List<HistoryEntry>();
        string session;
        int generation;
        bool initialized;
        void Awake()
        {
            if(!story) story=GetComponent<StoryState>() ?? gameObject.AddComponent<StoryState>();
            if(!cutscenes) cutscenes=GetComponent<CutsceneDirector>() ?? gameObject.AddComponent<CutsceneDirector>();
            cutscenes.game=this; if(levels) levels.story=story;
        }
        void Start()
        {
            if(!ui.storyFade || !ui.skipCutscene) { Debug.LogError("Upgrade this scene with Tools > Jack > Upgrade Story and Cutscenes before playing."); Busy=true; enabled=false; return; }
            if(!initialized) Restart();
        }
        public void Restart()
        {
            initialized=true; generation++; StopAllCoroutines(); client.Cancel(); cutscenes.Cancel(); RequestInFlight=false; Busy=true; Won=false; Turn=0; Setbacks=0;
            history.Clear(); session="demo-"+Guid.NewGuid().ToString("N"); inventory.ResetItems(); story.ResetStory(); challenges.ResetChallenge(); levels.PreparePrologue();
            ui.input.text=""; ui.input.DeactivateInputField(); ui.lastRequest.text="Next: reveal / continue narration · Skip: continue to gameplay";
            cutscenes.Play(StoryCutscene.Prologue,()=>StartCoroutine(EnterChapter(0)));
        }
        public void Interact(WorldEntity entity)
        {
            if(Busy || Won) return;
            string prompt=entity.id=="goose" ? "Narrator, help me silence the goose alarm and take it without waking the giant." : "Narrator, help me use the "+entity.displayName+" to complete my goal.";
            ui.input.text=prompt;
            ui.Say("Ask about "+entity.displayName+". Edit the request below, then press Enter or Ask.");
            ui.input.ActivateInputField();
        }
        NarratorRequest Snapshot(string text) => new NarratorRequest { sessionId=session,levelId=levels.Current.id,playerText=text,turnNumber=Turn,consecutiveSetbacks=Setbacks,inventory=inventory.Snapshot(),worldEntities=world.Snapshot(),recentHistory=history.ToArray(),activeChallengeId=challenges.ActiveId,challengeSolved=challenges.Solved,storyMilestones=story.Snapshot() };
        void Remember(string role,string text) { history.Add(new HistoryEntry(role,text.Substring(0,Math.Min(350,text.Length)))); while(history.Count>6) history.RemoveAt(0); }
        public void Submit(string text)
        {
            if(Busy || Won || !story.CanEnter(levels.Beat) || string.IsNullOrWhiteSpace(text)) return;
            text=text.Trim(); if(text.Length>400) text=text.Substring(0,400);
            if(Turn>=200) { ui.Say("This edition has reached its 200-turn limit. Restart for a fresh tale."); return; }
            Turn++; ui.lastRequest.text="You: "+text;
            if(challenges.ActiveId!="") {
                bool correct=challenges.Answer(text);
                Remember("player",text);
                if(!correct) { ui.Say(ChallengeManager.Question(challenges.ActiveId)+" Try again; mercy arrives after three attempts."); ui.Refresh("Riddle attempt "+challenges.Attempts+" / 3"); return; }
                var reward=NarratorMock.Respond(Snapshot(text),true); ApplyResponse(reward,"Riddle cleared · helpful effect unlocked"); return;
            }
            var request=Snapshot(text); Remember("player",text);
            RequestInFlight=true; Busy=true; ui.Refresh(); int revision=levels.Revision, stamp=generation;
            StartCoroutine(client.Request(request,(response,status)=> {
                if(stamp!=generation || revision!=levels.Revision) return;
                RequestInFlight=false; Busy=false; ApplyResponse(response,status);
            }));
        }
        void ApplyResponse(NarratorResponse response,string status)
        {
            string before=JsonUtility.ToJson(new NarratorRequest { inventory=inventory.Snapshot(),worldEntities=world.Snapshot() });
            int rejected=0;
            if(response.actions!=null) foreach(var action in response.actions) if(!executor.Apply(action)) {
                // Setting an already-satisfied property is harmless, but suppress unverified claims conservatively.
                rejected++;
            }
            if(!string.IsNullOrEmpty(response.challengeId) && challenges.ActiveId=="") challenges.Begin(response.challengeId);
            string after=JsonUtility.ToJson(new NarratorRequest { inventory=inventory.Snapshot(),worldEntities=world.Snapshot() });
            Setbacks=before!=after ? 0 : Setbacks+1;
            string narration=rejected>0 ? "The stage crew refused part of that request. Only the changes you can see took effect. Try a different approach." : response.narration;
            if(challenges.ActiveId!="") narration+="\n"+ChallengeManager.Question(challenges.ActiveId);
            if(Setbacks>=3 && challenges.ActiveId=="" && !levels.GoalMet) {
                if(world.entities.Count>=WorldRegistry.MaxEntities) { var removable=world.entities.Find(e=>e && !e.essential && e.id!="ladder"); if(removable) world.Remove(removable); }
                var help=NarratorMock.Respond(Snapshot("Please help me progress"),true);
                int applied=0; foreach(var action in help.actions) if(executor.Apply(action)) applied++;
                if(applied>0) { narration=help.narration; Setbacks=0; status+=" · mercy rule applied"; }
            } else if(Setbacks>=2) narration+="\nHint: "+Hint();
            ui.Say(narration,response.mood); Remember("narrator",narration); ui.Refresh(status);
            if(levels.GoalMet) BeginTransition();
        }
        string Hint()
        {
            switch(levels.Current.id) {
                case "sabotaged": return "Ask for a ladder, then brace it; or stabilize the beanstalk itself.";
                case "climb": return "Create a stable, climbable ladder or platform across the gap.";
                case "castle": return "Ask to unlock the sealed castle door.";
                case "treasure": return "Silence the goose ward (unlock goose), then ask to take it.";
                default: return "Open the exit or build a stable crossing home.";
            }
        }
        public void Next()
        {
            if(InCutscene) { cutscenes.NextNarration(); return; }
            if(!Busy && !Won && levels.GoalMet) BeginTransition();
        }
        void BeginTransition()
        {
            if(Busy || Won || !levels.GoalMet) return;
            Busy=true; ui.input.DeactivateInputField(); ui.Refresh("Objective complete · turning the page...");
            StartCoroutine(Transition());
        }
        IEnumerator Transition()
        {
            yield return new WaitForSecondsRealtime(objectiveCelebrationSeconds);
            ui.RevealDialogue();
            var sequence=new[]{StoryCutscene.BeginClimb,StoryCutscene.CloudKingdom,StoryCutscene.TreasureRoom,StoryCutscene.Awakening,StoryCutscene.Ending}[levels.Beat];
            int nextBeat=levels.Beat+1;
            cutscenes.Play(sequence,()=> {
                if(nextBeat<5) StartCoroutine(EnterChapter(nextBeat));
                else { Won=story.Has("home"); Busy=false; ui.Say("Happily ever after. Subject to the goose's terms and conditions."); ui.Refresh("The end · Restart to tell the tale again"); }
            });
        }
        IEnumerator EnterChapter(int beat)
        {
            levels.Load(beat); challenges.ResetChallenge(); Setbacks=0;
            ui.lastRequest.text="A/D or arrows · approach and press E, or click an actor";
            ui.SetFade(1); ui.Say(levels.Current.introduction);
            yield return StoryMotion.Tween(cutscenes.Seconds(cutscenes.fadeSeconds),t=>ui.SetFade(1-t));
            Busy=false; ui.Refresh(client.UseMockNarrator ? "Offline narrator" : "Live narrator · offline fallback available");
        }
    }
}
