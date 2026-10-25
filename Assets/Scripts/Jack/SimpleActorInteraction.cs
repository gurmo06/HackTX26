using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace JackGame
{
    public class SimpleActorInteraction : MonoBehaviour
    {
        public WorldEntity entity;
        public GameManager game;
        void Update()
        {
            if(!entity.visible || game.Busy || game.Won) return;
            var pointer=Pointer.current;
            if(pointer==null || !pointer.press.wasPressedThisFrame) return;
            if(EventSystem.current) {
                var hits=new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position=pointer.position.ReadValue() },hits);
                if(hits.Count>0) return;
            }
            Vector2 position=game.levels.sceneCamera.ScreenToWorldPoint(pointer.position.ReadValue());
            if(entity.hitbox.OverlapPoint(position)) game.Interact(entity);
        }
    }
}
