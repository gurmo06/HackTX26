using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace JackGame
{
    public class WorldRegistry : MonoBehaviour
    {
        public ArtCatalog art;
        public Transform actorRoot;
        public GameManager game;
        public readonly List<WorldEntity> entities = new List<WorldEntity>();
        public const int MaxEntities = 24;
        static Sprite cardboard;
        public static readonly string[] SpawnArchetypes = { "ladder", "crate", "rope", "beanstalk", "beans", "goose", "generic", "platform", "stone" };
        public WorldEntity Find(string id) => entities.Find(e => e && e.id == id);
        public WorldSnapshot[] Snapshot() => entities.Where(e => e).Select(e => e.Snapshot()).ToArray();
        public void Clear()
        {
            foreach (var e in entities) if (e) { e.gameObject.SetActive(false); Destroy(e.gameObject); }
            entities.Clear();
        }
        public bool Remove(WorldEntity e)
        {
            if (!e || e.essential) return false;
            entities.Remove(e); e.gameObject.SetActive(false); Destroy(e.gameObject); return true;
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
            entities.Add(e); e.Refresh(); return e;
        }
    }
}
