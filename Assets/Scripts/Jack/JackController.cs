using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JackGame
{
    public class JackController : MonoBehaviour
    {
        public SpriteRenderer visual;
        public Sprite idle, walk1, walk2, climb;
        public GameManager game;
        public float speed = 3;
        float ascent;
        Coroutine failedClimb;
        Vector3 failedClimbStart;
        SpriteRenderer carriedGoose;
        public float LayoutWidth {
            get {
                float width=1.35f;
                foreach(var frame in new[]{idle,walk1,walk2,climb}) if(frame) width=Mathf.Max(width,1.8f*frame.bounds.size.x/frame.bounds.size.y+.1f);
                return game.inventory.Has("goose") ? Mathf.Max(2f,width) : width;
            }
        }
        public void BeginAscent() { ascent=1.2f; }
        public void StopAscent() { ascent=0; if(failedClimb!=null) { StopCoroutine(failedClimb); failedClimb=null; transform.position=failedClimbStart; } SetSprite(idle); }
        public void PlayFailedClimb()
        {
            StopAscent(); failedClimbStart=transform.position;
            failedClimb=StartCoroutine(FailedClimb());
        }
        IEnumerator FailedClimb()
        {
            SetSprite(climb);
            yield return StoryMotion.Move(transform,failedClimbStart+Vector3.up*.65f,.35f);
            SetSprite(idle);
            yield return StoryMotion.Move(transform,failedClimbStart,.25f);
            failedClimb=null;
        }
        void SetSprite(Sprite sprite)
        {
            visual.sprite=sprite ? sprite : idle;
            visual.transform.localScale=Vector3.one*(1.8f/visual.sprite.bounds.size.y);
        }
        void LateUpdate()
        {
            if(!game || !game.inventory) return;
            if(game.inventory.Has("goose")) {
                if(!carriedGoose) { var go=new GameObject("Carried golden goose"); go.transform.SetParent(transform,false); carriedGoose=go.AddComponent<SpriteRenderer>(); carriedGoose.sprite=game.world.art.Get("golden_goose"); carriedGoose.sharedMaterial=visual.sharedMaterial; carriedGoose.sortingOrder=14; go.transform.localScale=Vector3.one*(.65f/carriedGoose.sprite.bounds.size.y); }
                carriedGoose.gameObject.SetActive(true); carriedGoose.transform.localPosition=new Vector3(visual.flipX ? -.5f : .5f,.15f+Mathf.Sin(Time.time*3)*.025f,0);
            } else if(carriedGoose) carriedGoose.gameObject.SetActive(false);
        }
        void Update()
        {
            if(failedClimb!=null) return;
            if(ascent>0) { ascent-=Time.deltaTime; transform.position+=Vector3.up*Time.deltaTime*1.5f; SetSprite(climb); return; }
            var keys=Keyboard.current;
            if(!game || game.Busy || game.Won || game.ui.input.isFocused || keys==null) { SetSprite(idle); return; }
            float direction=(keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0)-(keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
            var p=transform.position; p.x=Mathf.Clamp(p.x+direction*speed*Time.deltaTime,-6.8f,6.8f); transform.position=p;
            if(direction!=0) visual.flipX=direction<0;
            SetSprite(direction==0 ? idle : Mathf.FloorToInt(Time.time*7)%2==0 ? walk1 : walk2);
            if(keys.eKey.wasPressedThisFrame) {
                WorldEntity closest=null; float distance=2.6f;
                foreach(var e in game.world.entities) if(e && e.visible) { var d=Vector2.Distance(transform.position,e.transform.position); if(d<distance) { closest=e; distance=d; } }
                if(closest) game.Interact(closest);
            }
        }
    }
}
