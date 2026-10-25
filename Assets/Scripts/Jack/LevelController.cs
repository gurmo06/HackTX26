using UnityEngine;

namespace JackGame
{
    public class LevelController : MonoBehaviour
    {
        public WorldRegistry world;
        public Inventory inventory;
        public SpriteRenderer background;
        public JackController jack;
        public Camera sceneCamera;
        public int Beat { get; private set; }
        public int Revision { get; private set; }
        public LevelDefinition Current => LevelDefinition.Beats[Beat];
        public bool GoalMet
        {
            get {
                switch(Current.id) {
                    case "merchant": return inventory.Has("beans");
                    case "grow": var plant=world.Find("beanstalk"); return plant && plant.visible && plant.climbable && plant.size>=1.5f && plant.stability>=.7f;
                    case "climb": return HasBridge();
                    case "castle": var door=world.Find("door"); return (door && !door.locked && door.visible) || HasBridge();
                    case "escape": var exit=world.Find("exit"); return inventory.Has("goose") && exit && !exit.locked && exit.visible;
                    default: return false;
                }
            }
        }
        bool HasBridge() => world.entities.Exists(e => e && e.visible && e.climbable && e.stability>=.7f && e.size>=1.5f && (e.archetype=="ladder" || e.archetype=="rope" || e.archetype=="platform" || e.archetype=="generic"));
        public void Load(int beat)
        {
            Beat=Mathf.Clamp(beat,0,4); Revision++; world.Clear();
            background.sprite=world.art.Get(LevelDefinition.Environments[Current.environment].background);
            foreach(var actor in Current.actors) {
                var e=world.Spawn(actor.id,actor.archetype,actor.label,new Vector2(actor.x,actor.y<1 ? actor.y+.8f : actor.y),actor.height,actor.sprite,true);
                e.locked=actor.id=="door" || actor.id=="exit";
                if(actor.id=="merchant") e.state="selling_beans";
                e.Refresh();
            }
            jack.transform.position=new Vector3(-2,.15f,0);
            jack.StopAscent(); FrameCamera();
        }
        void LateUpdate() => FrameCamera();
        void FrameCamera()
        {
            if (!background.sprite) return;
            // Fit the real artwork, letterbox instead of hiding actors on narrow displays.
            var size=background.sprite.bounds.size;
            sceneCamera.orthographicSize=Mathf.Max(size.y/2,size.x/(2*Mathf.Max(.3f,sceneCamera.aspect)));
        }
    }
}
