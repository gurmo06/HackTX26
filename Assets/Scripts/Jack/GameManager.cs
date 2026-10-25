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
        [Tooltip("Reading time after the successful reply finishes typing, before the scripted cutscene begins. Also applies when Show text is clicked.")]
        [Min(0)] public float solutionReadingBufferSeconds=4f;
        public bool InCutscene => cutscenes && cutscenes.IsPlaying;
        public bool Busy { get; private set; }
        public bool RequestInFlight { get; private set; }
        public bool Won { get; private set; }
        public bool ObjectiveConfirmed { get; private set; }
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
            initialized=true; ObjectiveConfirmed=false; generation++; StopAllCoroutines(); client.Cancel(); cutscenes.Cancel(); RequestInFlight=false; Busy=true; Won=false; Turn=0; Setbacks=0;
            history.Clear(); session="demo-"+Guid.NewGuid().ToString("N"); inventory.ResetItems(); story.ResetStory(); challenges.ResetChallenge(); levels.PreparePrologue();
            ui.input.text=""; ui.input.DeactivateInputField(); ui.lastRequest.text="Next: reveal / continue narration · Skip: continue to gameplay";
            cutscenes.Play(StoryCutscene.Prologue,()=>StartCoroutine(EnterChapter(0)));
        }
        public void Interact(WorldEntity entity)
        {
            if(Busy || Won) return;
            string prompt=entity.id=="goose" ? "Narrator, I approach the golden goose." : "Narrator, help me use the "+entity.displayName+" to complete my goal.";
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
                RequestInFlight=false; Busy=false; ApplyResponse(NarratorMock.UnsafeTraversal(request) ?? response,status);
            }));
        }
        void ApplyResponse(NarratorResponse response,string status)
        {
            ObjectiveConfirmed=false;
            string before=JsonUtility.ToJson(new NarratorRequest { inventory=inventory.Snapshot(),worldEntities=world.Snapshot() });
            int rejected=0; bool approved=false;
            if(response.actions!=null) foreach(var action in response.actions) {
                // A twist cannot also authorize a successful handoff, even if the model sends advance.
                if(action!=null && action.type=="advance" && response.twist!="helpful") continue;
                if(action!=null && action.type=="advance" && action!=response.actions[response.actions.Length-1]) rejected++;
                else if(!executor.Apply(action)) rejected++;
                else if(action.type=="advance") approved=true;
            }
            if(!string.IsNullOrEmpty(response.challengeId) && challenges.ActiveId=="") challenges.Begin(response.challengeId);
            string after=JsonUtility.ToJson(new NarratorRequest { inventory=inventory.Snapshot(),worldEntities=world.Snapshot() });
            Setbacks=before!=after ? 0 : Setbacks+1;
            string narration=rejected>0 ? "The stage crew refused part of that request. Only the changes you can see took effect. Try a different approach." : response.narration;
            if(challenges.ActiveId!="") narration+="\n"+ChallengeManager.Question(challenges.ActiveId);
            if(response.explanation=="unsafe_traversal") levels.jack.PlayFailedClimb();
            ObjectiveConfirmed=approved && rejected==0 && levels.GoalMet && challenges.ActiveId=="";
            ui.Say(narration,response.mood); Remember("narrator",narration); ui.Refresh(status);
            if(ObjectiveConfirmed) BeginTransition();
        }
        public void Next()
        {
            if(InCutscene) { cutscenes.NextNarration(); return; }
            if(!Busy && !Won && ObjectiveConfirmed) BeginTransition();
        }
        void BeginTransition()
        {
            if(Busy || Won || !ObjectiveConfirmed || !levels.GoalMet) return;
            Busy=true; ui.input.DeactivateInputField(); ui.Refresh("Objective complete · a moment to read...");
            StartCoroutine(Transition());
        }
        IEnumerator Transition()
        {
            // Preserve the successful AI reply: the reading pause starts only once
            // the typewriter finishes (or the player chooses Show text).
            while(ui.IsTyping) yield return null;
            yield return new WaitForSecondsRealtime(Mathf.Max(objectiveCelebrationSeconds,solutionReadingBufferSeconds));
            var sequence=new[]{StoryCutscene.BeginClimb,StoryCutscene.CloudKingdom,StoryCutscene.TreasureRoom,StoryCutscene.Awakening,StoryCutscene.Ending}[levels.Beat];
            int nextBeat=levels.Beat+1;
            cutscenes.Play(sequence,()=> {
                if(nextBeat<5) StartCoroutine(EnterChapter(nextBeat));
                else { Won=story.Has("home"); Busy=false; ui.Say("Happily ever after. Subject to the goose's terms and conditions."); ui.Refresh("The end · Restart to tell the tale again"); }
            });
        }
        IEnumerator EnterChapter(int beat)
        {
            ObjectiveConfirmed=false;
            levels.Load(beat); challenges.ResetChallenge(); Setbacks=0;
            ui.lastRequest.text="A/D or arrows · approach and press E, or click an actor";
            ui.SetFade(1); ui.Say(levels.Current.introduction);
            yield return StoryMotion.Tween(cutscenes.Seconds(cutscenes.fadeSeconds),t=>ui.SetFade(1-t));
            Busy=false; ui.Refresh(client.StatusText);
        }
    }
}
