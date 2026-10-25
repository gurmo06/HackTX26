using UnityEngine;

namespace JackGame
{
    public class LevelController : MonoBehaviour
    {
        public WorldRegistry world;
        public Inventory inventory;
        public StoryState story;
        public bool CinematicCamera { get; set; }
        public SpriteRenderer background;
        public JackController jack;
        public Camera sceneCamera;
        public int Beat { get; private set; }
        public int Revision { get; private set; }
        public LevelDefinition Current => LevelDefinition.Beats[Beat];
        public bool GoalMet
        {
            get {
                if(!story || !story.CanEnter(Beat)) return false;
                switch(Current.id) {
                    case "sabotaged": var plant=world.Find("beanstalk"); return (plant && plant.visible && plant.climbable && plant.size>=1.5f && plant.stability>=.7f) || HasBridge();
                    case "climb": return HasBridge();
                    case "castle": var door=world.Find("door"); return (door && !door.locked && door.visible) || HasBridge();
                    case "treasure": return story.Has("castle_entered") && story.Has("goose_taken") && inventory.Has("goose");
                    case "escape": var exit=world.Find("exit"); return story.Has("giant_awake") && inventory.Has("goose") && ((exit && !exit.locked && exit.visible) || HasBridge());
                    default: return false;
                }
            }
        }
        bool HasBridge() => world.entities.Exists(e => e && e.visible && e.climbable && e.stability>=.7f && e.size>=1.5f && (e.archetype=="ladder" || e.archetype=="rope" || e.archetype=="platform" || e.archetype=="generic"));
        public void Load(int beat)
        {
            if(!story || !story.CanEnter(beat)) throw new System.InvalidOperationException("This chapter requires its preceding story milestone.");
            Beat=beat; Revision++; world.Clear();
            background.enabled=true; background.color=Current.id=="treasure" ? new Color(.58f,.64f,.82f) : Color.white;
            background.sprite=world.art.Get(LevelDefinition.Environments[Current.environment].background);
            foreach(var actor in Current.actors) {
                var e=world.Spawn(actor.id,actor.archetype,actor.label,new Vector2(actor.x,actor.y<1 ? actor.y+.8f : actor.y),actor.height,actor.sprite,true);
                e.locked=actor.id=="door" || actor.id=="exit" || actor.id=="goose";
                if(actor.id=="giant") e.state=Current.id=="treasure" ? "sleeping" : "pursuing";
                if(actor.id=="beanstalk" && Current.id=="sabotaged") { e.size=1.5f; e.stability=.25f; }
                if(actor.id=="goose") e.state="warded";
                e.Refresh();
            }
            jack.transform.position=new Vector3(-2,.15f,0);
            jack.StopAscent(); FrameCamera();
        }
        public void PreparePrologue() { Beat=0; Revision++; world.Clear(); background.enabled=false; jack.StopAscent(); }
        void LateUpdate() { if(!CinematicCamera) FrameCamera(); }
        void FrameCamera()
        {
            if (!background.sprite) return;
            // Fit the real artwork, letterbox instead of hiding actors on narrow displays.
            var size=background.sprite.bounds.size;
            sceneCamera.orthographicSize=Mathf.Max(size.y/2,size.x/(2*Mathf.Max(.3f,sceneCamera.aspect)));
            sceneCamera.transform.position=Current.id=="treasure" ? new Vector3(.45f,.2f,-10) : new Vector3(0,0,-10);
            if(Current.id=="treasure") sceneCamera.orthographicSize*=.96f;
        }
    }
}
