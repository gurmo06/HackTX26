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
        public WorldSnapshot Snapshot() => new WorldSnapshot { id=id, archetype=archetype, state=state, x=transform.position.x, y=transform.position.y, size=size, stability=stability, climbable=climbable, locked=locked, visible=visible };
        public void Refresh()
        {
            var bounds = visual.sprite.bounds.size;
            visual.transform.localScale = Vector3.one * (baseHeight * size / Mathf.Max(0.01f, bounds.y));
            visual.transform.localRotation = Quaternion.Euler(0, 0, stability < 0.5f ? -68 : 0);
            visual.color = locked ? new Color(0.7f,0.49f,0.35f) : stability < 0.5f ? new Color(1,0.82f,0.65f) : Color.white;
            visual.enabled = visible;
            hitbox.enabled = visible;
            hitbox.size = new Vector2(Mathf.Max(0.5f, bounds.x * visual.transform.localScale.x), Mathf.Min(4,baseHeight*size));
            label.text = displayName + (climbable ? " · climbable" : "") + (stability < 0.5f ? " · fallen" : "") + (locked ? " · locked" : "");
            label.gameObject.SetActive(visible);
            label.transform.localPosition = new Vector3(0, -Mathf.Min(1f,baseHeight*size/2)+0.05f, 0);
        }
        public void Connect(WorldEntity other, Material material)
        {
            connectedTo = other;
            if (!connection) { connection = gameObject.AddComponent<LineRenderer>(); connection.sharedMaterial=material; connection.startWidth=connection.endWidth=0.06f; connection.positionCount=2; connection.sortingOrder=6; connection.startColor=connection.endColor=new Color(0.8f,0.6f,0.3f); }
        }
        void LateUpdate()
        {
            if (!connection) return;
            connection.enabled = connectedTo && visible && connectedTo.visible;
            if (connection.enabled) { connection.SetPosition(0,transform.position); connection.SetPosition(1,connectedTo.transform.position); }
        }
    }
}
