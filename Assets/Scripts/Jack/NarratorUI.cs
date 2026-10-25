using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JackGame
{
    public class NarratorUI : MonoBehaviour
    {
        public TMP_Text heading, dialogue, status, objective, inventoryText, lastRequest, victoryText;
        public TMP_InputField input;
        public ScrollRect dialogueScroll;
        public Button submit, next, restart;
        public Image beanIcon, eggIcon;
        public GameObject victoryPanel;
        public GameManager game;
        void Start()
        {
            heading.text="Narrator";
            submit.onClick.AddListener(Submit);
            input.onSubmit.AddListener(_=>Submit());
            next.onClick.AddListener(game.Next);
            restart.onClick.AddListener(game.Restart);
        }
        void Submit() { if(!game.Busy && !game.Won && !string.IsNullOrWhiteSpace(input.text)) { var value=input.text; input.text=""; game.Submit(value); } }
        public void Say(string text, string mood="neutral")
        {
            dialogue.text=text;
            // Keep long responses readable on narrow viewports; drag/wheel the parchment text.
            Canvas.ForceUpdateCanvases();
            dialogue.ForceMeshUpdate();
            dialogue.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(dialogueScroll.viewport.rect.height,dialogue.preferredHeight+8));
            dialogueScroll.verticalNormalizedPosition=1;
            heading.color=mood=="smug" ? new Color(.49f,.22f,.13f) : new Color(.24f,.18f,.13f);
        }
        public void Refresh(string message=null)
        {
            input.interactable=!game.Busy && !game.Won; submit.interactable=input.interactable;
            if(game.Busy) status.text="Narrator is thinking..."; else if(message!=null) status.text=message;
            objective.text=game.levels.Current.objective;
            inventoryText.text="Satchel: "+string.Join(", ",game.inventory.Snapshot());
            beanIcon.color=game.inventory.Has("beans") ? Color.white : new Color(1,1,1,.25f);
            eggIcon.color=game.inventory.Has("goose") ? Color.white : new Color(1,1,1,.25f);
            next.gameObject.SetActive(game.levels.GoalMet && !game.Busy && !game.Won);
            next.GetComponentInChildren<TMP_Text>(true).text=game.levels.Beat==4 ? "Escape!" : "Next chapter";
            restart.gameObject.SetActive(!game.Busy && (game.Won || game.Turn>0));
            victoryPanel.SetActive(game.Won);
            victoryText.text="HAPPILY EVER AFTER*\n\nJack escapes with the golden goose.\n*The cow has requested legal representation.\n\nUse Restart to tell this questionable tale again.";
        }
    }
}
