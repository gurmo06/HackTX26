using System.Collections;
using TMPro;
using UnityEngine;

namespace JackGame
{
    public class WorldEntity : MonoBehaviour
    {
        public string id, archetype, state = "ready", displayName;
        public float size = 1, stability = 1, baseHeight = 1;
        public bool climbable, locked, visible = true, essential;
        public SpriteRenderer visual;
        public TextMeshPro label;
        public BoxCollider2D hitbox;
        public WorldEntity connectedTo;
        LineRenderer connection;
        Vector3 renderScale=Vector3.one;
        Color renderColor=Color.white;
        float renderAngle;
        bool initialized;
        Coroutine appearance;
        [Range(0,.1f)] public float idleAmount=.025f;
        // Presentation settings are separate from the validated gameplay size/stability.
        [HideInInspector] public bool uprightBeanstalk;
        [HideInInspector] public float presentationHeight, unstableSwayDegrees=4, swayDistance=.08f, swaySpeed=2.4f;
        [HideInInspector] public float layoutScale=1;
        public float LayoutWidth { get {
            float h=Mathf.Max(RenderHeight,visual.sprite.bounds.size.y*renderScale.y);
            float w=h*visual.sprite.bounds.size.x/Mathf.Max(.01f,visual.sprite.bounds.size.y);
            return uprightBeanstalk ? w+2*h*Mathf.Sin(unstableSwayDegrees*Mathf.Deg2Rad)+2*swayDistance+.1f
                : stability<.5f || Mathf.Abs(renderAngle)>.1f ? Mathf.Sqrt(w*w+h*h)+.1f : w+.1f;
        } }
        float RenderHeight => uprightBeanstalk ? presentationHeight*size/1.5f : Mathf.Min(4.2f,baseHeight*size);
        public WorldSnapshot Snapshot() => new WorldSnapshot { id=id, archetype=archetype, state=state, x=transform.position.x, y=transform.position.y, size=size, stability=stability, climbable=climbable, locked=locked, visible=visible };
        public void Refresh()
        {
            var bounds = visual.sprite.bounds.size;
            Vector3 targetScale=Vector3.one*(RenderHeight/Mathf.Max(.01f,bounds.y));
            float targetAngle=stability<.5f && !uprightBeanstalk ? -68 : 0;
            Color targetColor=locked ? new Color(.7f,.49f,.35f) : stability<.5f ? new Color(1,.82f,.65f) : Color.white;
            if(appearance!=null) StopCoroutine(appearance);
            if(initialized && Application.isPlaying && visible) appearance=StartCoroutine(EaseAppearance(targetScale,targetAngle,targetColor));
            else { renderScale=targetScale; renderAngle=targetAngle; renderColor=targetColor; }
            initialized=true; visual.enabled=visible;
            hitbox.enabled = visible;
            hitbox.size = new Vector2(Mathf.Max(0.5f, bounds.x * targetScale.x), RenderHeight);
            hitbox.offset=new Vector2(0,(RenderHeight-baseHeight)/2);
            label.text = displayName + (climbable ? " · climbable" : "") + (stability < 0.5f ? (uprightBeanstalk ? " · unstable" : " · fallen") : "") + (locked ? " · locked" : "");
            label.gameObject.SetActive(false);
            label.transform.localPosition = new Vector3(0, -Mathf.Min(1f,baseHeight*size/2)+0.05f, 0);
        }
        IEnumerator EaseAppearance(Vector3 scale,float angle,Color color)
        {
            var oldScale=renderScale; var oldColor=renderColor; float oldAngle=renderAngle;
            yield return StoryMotion.Tween(.25f,t=> { renderScale=Vector3.Lerp(oldScale,scale,t); renderAngle=Mathf.LerpAngle(oldAngle,angle,t); renderColor=Color.Lerp(oldColor,color,t); });
            appearance=null;
        }
        public IEnumerator Disappear()
        {
            visible=false; hitbox.enabled=false; label.gameObject.SetActive(false);
            if(appearance!=null) StopCoroutine(appearance);
            var color=renderColor;
            yield return StoryMotion.Tween(.2f,t=>renderColor=new Color(color.r,color.g,color.b,color.a*(1-t)));
        }
        public void Appear() { if(!Application.isPlaying) return; var color=renderColor; renderColor=new Color(color.r,color.g,color.b,0); if(appearance!=null) StopCoroutine(appearance); appearance=StartCoroutine(EaseAppearance(renderScale,renderAngle,color)); }
        public void Connect(WorldEntity other, Material material)
        {
            connectedTo = other;
            if (!connection) { connection = gameObject.AddComponent<LineRenderer>(); connection.sharedMaterial=material; connection.startWidth=connection.endWidth=0.06f; connection.positionCount=2; connection.sortingOrder=6; connection.startColor=connection.endColor=new Color(0.8f,0.6f,0.3f); }
        }
        void LateUpdate() => RenderPose();
        public void RenderPose()
        {
            if(visual && initialized) {
                float wave=Mathf.Sin(Time.unscaledTime*(state=="sleeping" ? 1.4f : 2.4f));
                float breathe=state=="sleeping" ? 1+wave*idleAmount : 1;
                visual.transform.localScale=new Vector3(renderScale.x,renderScale.y*breathe,renderScale.z)*layoutScale;
                float rise=(visual.sprite.bounds.size.y*renderScale.y*layoutScale*breathe-baseHeight)/2;
                visual.transform.localPosition=new Vector3(0,rise+(archetype=="goose" ? Mathf.Abs(wave)*idleAmount*2 : 0),0);
                visual.transform.localRotation=Quaternion.Euler(0,0,renderAngle+(archetype=="beanstalk" ? wave*1.1f : 0));
                if(uprightBeanstalk) {
                    float wobble=Mathf.Sin(Time.unscaledTime*swaySpeed);
                    float unrest=1-Mathf.Clamp01(stability);
                    var rotation=Quaternion.Euler(0,0,wobble*unstableSwayDegrees*unrest);
                    // Rotate around the root, keeping the overlay attached to the painted stalk.
                    visual.transform.localRotation=rotation;
                    visual.transform.localPosition=Vector3.down*(baseHeight/2)+rotation*Vector3.up*(visual.sprite.bounds.size.y*renderScale.y*layoutScale/2)
                        +Vector3.right*(wobble*swayDistance*unrest);
                }
                visual.color=renderColor;
                // The clickable area follows the displayed cutout after spacing/scaling.
                var drawn=visual.bounds;
                hitbox.size=drawn.size;
                hitbox.offset=transform.InverseTransformPoint(drawn.center);
            }
            if (!connection) return;
            connection.enabled = connectedTo && visible && connectedTo.visible;
            if (connection.enabled) { connection.SetPosition(0,transform.position); connection.SetPosition(1,connectedTo.transform.position); }
        }
    }
}
