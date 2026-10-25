using UnityEngine;

namespace JackGame
{
    // Feet remain anchored while transparent cutout frames swap and idle poses animate.
    public class StoryActor : MonoBehaviour
    {
        public SpriteRenderer visual;
        public Sprite idle, walk1, walk2, climb;
        public float height=1.8f, bobAmount=.035f, breathing=.015f, swayDegrees;
        public bool walking, climbing, hopping, sleeping;
        public Vector3 FootPosition { get => transform.position; set => transform.position=value; }
        public void Pose()
        {
            var frame=climbing && climb ? climb : walking && walk1 && walk2 ? (Mathf.FloorToInt(Time.unscaledTime*7)%2==0 ? walk1 : walk2) : idle;
            if(!frame) return;
            visual.sprite=frame;
            float wave=Mathf.Sin(Time.unscaledTime*(sleeping ? 1.4f : 2.5f));
            float squash=sleeping ? 1+wave*breathing : 1;
            float hop=hopping ? Mathf.Abs(Mathf.Sin(Time.unscaledTime*3))*bobAmount*4 : walking ? Mathf.Abs(wave)*bobAmount : sleeping ? 0 : wave*bobAmount;
            float scale=height/Mathf.Max(.01f,frame.bounds.size.y);
            visual.transform.localScale=new Vector3(scale,scale*squash,scale);
            visual.transform.localPosition=new Vector3(0,height*squash/2+hop,0);
            visual.transform.localRotation=Quaternion.Euler(0,0,wave*swayDegrees);
        }
        void LateUpdate() => Pose();
    }
}
