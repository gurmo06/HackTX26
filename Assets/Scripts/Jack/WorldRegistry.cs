using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace JackGame
{
    [DefaultExecutionOrder(100)]
    public class WorldRegistry : MonoBehaviour
    {
        public ArtCatalog art;
        public Transform actorRoot;
        public GameManager game;
        public readonly List<WorldEntity> entities = new List<WorldEntity>();
        readonly List<WorldEntity> retiring=new List<WorldEntity>();
        public const int MaxEntities = 24;
        [Min(0)] public float spriteGap=.16f;
        class LayoutItem
        {
            public WorldEntity entity;
            public Transform root;
            public float width;
        }
        // Fit gameplay cutouts into horizontal slots using their full animated widths.
        // The background-aligned stalk remains fixed. Logical size/stability are untouched.
        void LateUpdate()
        {
            if(!game || !game.levels || game.levels.CinematicCamera || !actorRoot.gameObject.activeInHierarchy) return;
            ArrangeSprites();
        }
        public void ArrangeSprites()
        {
            var items=entities.Where(e=>e && e.visible).Select(e=>new LayoutItem { entity=e,root=e.transform,width=e.LayoutWidth }).ToList();
            var jack=game.levels.jack;
            if(jack && jack.gameObject.activeInHierarchy) items.Add(new LayoutItem { root=jack.transform,width=jack.LayoutWidth });
            var plant=items.Find(i=>i.entity && i.entity.uprightBeanstalk);
            if(plant!=null) {
                items.Remove(plant); plant.entity.layoutScale=1;
                float x=plant.root.position.x, half=plant.width/2;
                Pack(items.Where(i=>i.root.position.x<x).ToList(),-6.8f,x-half-spriteGap);
                Pack(items.Where(i=>i.root.position.x>=x).ToList(),x+half+spriteGap,6.8f);
            } else Pack(items,-6.8f,6.8f);
            foreach(var e in entities) if(e && e.visible) e.RenderPose();
        }
        void Pack(List<LayoutItem> items,float left,float right)
        {
            if(items.Count==0) return;
            items.Sort((a,b)=>a.root.position.x.CompareTo(b.root.position.x));
            float fixedWidth=items.Where(i=>!i.entity).Sum(i=>i.width);
            float propWidth=items.Where(i=>i.entity).Sum(i=>i.width);
            float gap=Mathf.Min(spriteGap,Mathf.Max(0,(right-left-fixedWidth)/(items.Count*3)));
            float scale=Mathf.Min(1,Mathf.Max(.001f,right-left-fixedWidth-gap*(items.Count-1))/Mathf.Max(.001f,propWidth));
            foreach(var item in items) if(item.entity) { item.entity.layoutScale=scale; item.width*=scale; }
            float remaining=items.Sum(i=>i.width)+gap*(items.Count-1);
            float cursor=left;
            foreach(var item in items) {
                float min=cursor+item.width/2, max=right-remaining+item.width/2;
                var pos=item.root.position; pos.x=Mathf.Clamp(pos.x,min,Mathf.Max(min,max)); item.root.position=pos;
                cursor=pos.x+item.width/2+gap; remaining-=item.width+gap;
            }
        }
        static Sprite cardboard;
        public static readonly string[] SpawnArchetypes = { "ladder", "crate", "rope", "beanstalk", "beans", "goose", "generic", "platform", "stone" };
        public WorldEntity Find(string id) => entities.Find(e => e && e.id == id);
        public WorldSnapshot[] Snapshot() => entities.Where(e => e).Select(e => e.Snapshot()).ToArray();
        public void Clear()
        {
            StopAllCoroutines(); // Cancel pending removal effects before destroying a chapter.
            foreach (var e in entities) if (e) { e.gameObject.SetActive(false); Destroy(e.gameObject); }
            entities.Clear();
            foreach(var e in retiring) if(e) { e.gameObject.SetActive(false); Destroy(e.gameObject); }
            retiring.Clear();
        }
        public bool Remove(WorldEntity e)
        {
            if (!e || e.essential) return false;
            entities.Remove(e); retiring.Add(e); StartCoroutine(Retire(e)); return true;
        }
        IEnumerator Retire(WorldEntity entity)
        {
            yield return entity.Disappear(); retiring.Remove(entity);
            if(entity) { entity.gameObject.SetActive(false); Destroy(entity.gameObject); }
        }
        public string SpriteKey(string archetype)
        {
            switch(archetype) {
                case "beanstalk": return "beanstalk_segment";
                case "beans": return "BeanIcon";
                case "goose": return "golden_goose";
                case "platform": return "wooden_platform";
                case "stone": return "stone_block";
                case "generic": return "";
                default: return archetype;
            }
        }
        public WorldEntity Spawn(string id, string archetype, string label, Vector2 position, float height=1.5f, string spriteKey=null, bool essential=false)
        {
            if (entities.Count >= MaxEntities || Find(id)) return null;
            var go = new GameObject(id); go.transform.SetParent(actorRoot,false); go.transform.position=position;
            var e = go.AddComponent<WorldEntity>(); e.id=id; e.archetype=archetype; e.displayName=label; e.baseHeight=height; e.essential=essential;
            var visual = new GameObject("Art"); visual.transform.SetParent(go.transform,false);
            e.visual=visual.AddComponent<SpriteRenderer>(); e.visual.sortingOrder=(archetype=="npc" || archetype=="giant" || archetype=="cow") ? 10 : 4;
            e.visual.sharedMaterial=art.worldMaterial;
            e.visual.sprite=art.Get(spriteKey ?? SpriteKey(archetype));
            if (!e.visual.sprite) {
                if (!cardboard) { var texture = new Texture2D(2,2); texture.SetPixels(new[]{new Color(.8f,.64f,.4f),new Color(.9f,.76f,.53f),new Color(.9f,.76f,.53f),new Color(.8f,.64f,.4f)}); texture.Apply(); cardboard=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2); }
                e.visual.sprite=cardboard;
            }
            var labelObject = new GameObject("Label"); labelObject.transform.SetParent(go.transform,false);
            e.label=labelObject.AddComponent<TextMeshPro>(); e.label.font=art.bodyFont; e.label.fontSize=2.5f; e.label.alignment=TextAlignmentOptions.Center; e.label.richText=false; e.label.color=new Color(1,.96f,.8f);
            e.label.rectTransform.sizeDelta=new Vector2(4,0.7f); e.label.GetComponent<MeshRenderer>().sortingOrder=25;
            e.hitbox=go.AddComponent<BoxCollider2D>();
            var interaction=go.AddComponent<SimpleActorInteraction>(); interaction.entity=e; interaction.game=game;
            entities.Add(e); e.Refresh(); e.Appear(); return e;
        }
    }
}
