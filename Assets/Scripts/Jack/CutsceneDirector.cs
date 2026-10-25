using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JackGame
{
    public sealed class StoryStep
    {
        public string narration;
        public Action enter;
        public Func<IEnumerator> animation;
        public bool waitForNarration;
        public StoryStep(string narration, Func<IEnumerator> animation=null, Action enter=null, bool wait=true)
        { this.narration=narration; this.animation=animation; this.enter=enter; waitForNarration=wait && !string.IsNullOrEmpty(narration); }
    }

    public class CutsceneDirector : MonoBehaviour
    {
        public GameManager game;
        [Min(.01f)] public float animationSpeed=1;
        [Min(.1f)] public float fadeSeconds=.65f;
        [Min(.1f)] public float narrationPause=2.5f;
        public bool autoAdvanceNarration;
        [Range(0,.3f)] public float shakeStrength=.1f;
        public bool IsPlaying { get; private set; }
        public bool WaitingForNarration { get; private set; }
        public int StepIndex { get; private set; }
        public StoryCutscene Current { get; private set; }
        public string Title { get; private set; }
        public Transform Stage { get; private set; }
        public SpriteRenderer Backdrop { get; private set; }
        readonly Dictionary<string,StoryActor> actors=new Dictionary<string,StoryActor>();
        Coroutine running;
        Action completed;
        bool nextRequested;
        Vector3 originalCameraPosition;
        float originalCameraSize;
        static Sprite paper;
        public Camera Camera => game.levels.sceneCamera;
        public ArtCatalog Art => game.world.art;
        public float Seconds(float seconds) => seconds/Mathf.Max(.01f,animationSpeed);
        public StoryActor Actor(string id) => actors.TryGetValue(id,out var actor) ? actor : null;

        public void Play(StoryCutscene scene, Action onComplete)
        {
            Cancel(); Current=scene; completed=onComplete; IsPlaying=true; StepIndex=-1;
            Title=StorySequences.Title(scene);
            originalCameraPosition=Camera.transform.position; originalCameraSize=Camera.orthographicSize;
            game.levels.CinematicCamera=true;
            game.world.actorRoot.gameObject.SetActive(false); game.levels.background.enabled=false; game.levels.jack.gameObject.SetActive(false);
            Stage=new GameObject("Story cutouts").transform; Stage.SetParent(transform.parent,false);
            game.ui.SetFade(1); game.ui.Refresh();
            running=StartCoroutine(Run(StorySequences.Build(this,scene)));
        }
        IEnumerator Run(List<StoryStep> steps)
        {
            for(int i=0;i<steps.Count;i++) {
                StepIndex=i; nextRequested=false; WaitingForNarration=false;
                var step=steps[i]; step.enter?.Invoke();
                if(!string.IsNullOrEmpty(step.narration)) game.ui.Say(step.narration);
                if(step.animation!=null) yield return step.animation();
                if(step.waitForNarration) {
                    WaitingForNarration=true; game.ui.Refresh();
                    float elapsed=0;
                    while(!nextRequested && (!autoAdvanceNarration || elapsed<narrationPause)) { elapsed+=Time.unscaledDeltaTime; yield return null; }
                    game.ui.RevealDialogue();
                }
            }
            Finish();
        }
        public void NextNarration()
        {
            if(!IsPlaying) return;
            if(game.ui.IsTyping) { game.ui.RevealDialogue(); return; }
            nextRequested=true;
        }
        public void Skip() { if(IsPlaying) { if(running!=null) StopCoroutine(running); Finish(); } }
        void Finish()
        {
            if(!Application.isPlaying) { Cancel(); return; }
            if(!IsPlaying) return;
            // Same idempotent finalizer for watched and skipped sequences.
            game.story.Complete(Current,game.inventory);
            var callback=completed; completed=null;
            if(Current==StoryCutscene.Ending) {
                // Keep the warm homecoming tableau behind the ending screen until Restart.
                running=null; IsPlaying=false; WaitingForNarration=false; nextRequested=false;
                game.ui.SetFade(0); game.ui.RevealDialogue();
            } else Cleanup();
            callback?.Invoke();
        }
        void OnDisable() => Cancel();
        public void Cancel()
        {
            if(running!=null) StopCoroutine(running);
            completed=null;
            if(IsPlaying || Stage) Cleanup();
        }
        void Cleanup()
        {
            running=null; IsPlaying=false; WaitingForNarration=false; nextRequested=false;
            if(Stage) { Stage.gameObject.SetActive(false); if(Application.isPlaying) Destroy(Stage.gameObject); else DestroyImmediate(Stage.gameObject); Stage=null; }
            actors.Clear(); Backdrop=null;
            if(!game || !game.levels || !game.world || !game.ui) return;
            Camera.transform.position=originalCameraPosition; Camera.orthographicSize=originalCameraSize;
            game.levels.CinematicCamera=false;
            game.world.actorRoot.gameObject.SetActive(true); game.levels.background.enabled=true; game.levels.jack.gameObject.SetActive(true);
            game.ui.SetFade(0); game.ui.RevealDialogue();
        }
        public void SetBackdrop(string spriteKey, Color tint)
        {
            if(!Backdrop) { var go=new GameObject("Storybook backdrop"); go.transform.SetParent(Stage,false); Backdrop=go.AddComponent<SpriteRenderer>(); Backdrop.sortingOrder=-100; Backdrop.sharedMaterial=Art.worldMaterial; }
            if(string.IsNullOrEmpty(spriteKey)) {
                if(!paper) paper=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f),Texture2D.whiteTexture.height);
                Backdrop.sprite=paper; Backdrop.transform.localScale=new Vector3(50,35,1);
            } else { Backdrop.sprite=Art.Get(spriteKey); Backdrop.transform.localScale=Vector3.one; }
            Backdrop.color=tint;
        }
        public StoryActor Add(string id,string sprite,Vector3 feet,float height,int order=10)
        {
            var go=new GameObject(id); go.transform.SetParent(Stage,false); go.transform.position=feet;
            var actor=go.AddComponent<StoryActor>(); actor.height=height; actor.idle=Art.Get(sprite);
            var visual=new GameObject("Cutout"); visual.transform.SetParent(go.transform,false);
            actor.visual=visual.AddComponent<SpriteRenderer>(); actor.visual.sharedMaterial=Art.worldMaterial; actor.visual.sortingOrder=order;
            if(sprite=="jack_idle") { actor.walk1=Art.Get("jack_walk1"); actor.walk2=Art.Get("jack_walk2"); actor.climb=Art.Get("jack_climb"); }
            actors[id]=actor; actor.Pose(); return actor;
        }
        public IEnumerator Fade(float alpha) { float start=game.ui.FadeAlpha; return StoryMotion.Tween(Seconds(fadeSeconds),t=>game.ui.SetFade(Mathf.Lerp(start,alpha,t))); }
        public IEnumerator Pan(Vector3 end,float zoom,float seconds)
        {
            var start=Camera.transform.position; float size=Camera.orthographicSize;
            return StoryMotion.Tween(Seconds(seconds),t=> { Camera.transform.position=Vector3.Lerp(start,end,t); Camera.orthographicSize=Mathf.Lerp(size,zoom,t); });
        }
        public IEnumerator Move(string id,Vector3 end,float seconds,bool walk=false)
        {
            var actor=Actor(id); actor.walking=walk; actor.visual.flipX=end.x<actor.transform.position.x;
            yield return StoryMotion.Move(actor.transform,end,Seconds(seconds)); actor.walking=false;
        }
        public IEnumerator Tint(Color end,float seconds)
        { var start=Backdrop.color; return StoryMotion.Tween(Seconds(seconds),t=>Backdrop.color=Color.Lerp(start,end,t)); }
        public IEnumerator Wait(float seconds) { yield return new WaitForSecondsRealtime(Seconds(seconds)); }
    }
}
