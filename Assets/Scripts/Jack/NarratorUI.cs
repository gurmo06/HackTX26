using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JackGame
{
    public class NarratorUI : MonoBehaviour
    {
        public TMP_Text heading, dialogue, status, objective, inventoryText, lastRequest, victoryText;
        public TMP_InputField input;
        public ScrollRect dialogueScroll;
        public Button submit, next, restart, skipCutscene, revealText;
        public Image beanIcon, eggIcon, storyFade;
        public GameObject victoryPanel;
        public GameManager game;
        public bool typewriterEnabled=true;
        [Range(10,160)] public float charactersPerSecond=55;
        Coroutine typing;
        public bool IsTyping => typing!=null;
        public float FadeAlpha => storyFade ? storyFade.color.a : 0;
        void Start()
        {
            heading.text="Narrator";
            submit.onClick.AddListener(Submit); input.onSubmit.AddListener(_=>Submit());
            next.onClick.AddListener(game.Next); restart.onClick.AddListener(game.Restart);
            if(skipCutscene) skipCutscene.onClick.AddListener(()=>game.cutscenes.Skip());
            if(revealText) revealText.onClick.AddListener(RevealDialogue);
        }
        void Update()
        {
            var keys=Keyboard.current;
            if(game.InCutscene && keys!=null) {
                if(keys.escapeKey.wasPressedThisFrame) game.cutscenes.Skip();
                else if(keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame) game.Next();
            }
            if(game.RequestInFlight) status.text="Narrator is thinking"+new string('.',1+Mathf.FloorToInt(Time.unscaledTime*2)%3);
        }
        void Submit() { if(!game.Busy && !game.Won && !string.IsNullOrWhiteSpace(input.text)) { var value=input.text; input.text=""; game.Submit(value); } }
        public void Say(string text,string mood="neutral")
        {
            RevealDialogue(); dialogue.text=text; dialogue.maxVisibleCharacters=int.MaxValue;
            Canvas.ForceUpdateCanvases(); dialogue.ForceMeshUpdate();
            dialogue.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(dialogueScroll.viewport.rect.height,dialogue.preferredHeight+8));
            dialogueScroll.verticalNormalizedPosition=1;
            heading.color=mood=="smug" ? new Color(.49f,.22f,.13f) : new Color(.24f,.18f,.13f);
            if(typewriterEnabled && gameObject.activeInHierarchy) typing=StartCoroutine(TypeText(dialogue.textInfo.characterCount));
        }
        IEnumerator TypeText(int length)
        {
            float shown=0; dialogue.maxVisibleCharacters=0;
            while(shown<length) { shown+=Time.unscaledDeltaTime*charactersPerSecond; dialogue.maxVisibleCharacters=Mathf.FloorToInt(shown); if(revealText) revealText.gameObject.SetActive(!game.InCutscene); yield return null; }
            dialogue.maxVisibleCharacters=int.MaxValue; typing=null; if(revealText) revealText.gameObject.SetActive(false);
        }
        public void RevealDialogue() { if(typing!=null) StopCoroutine(typing); typing=null; if(dialogue) dialogue.maxVisibleCharacters=int.MaxValue; if(revealText) revealText.gameObject.SetActive(false); }
        public void SetFade(float alpha) { if(storyFade) storyFade.color=new Color(.045f,.035f,.065f,Mathf.Clamp01(alpha)); }
        public void Refresh(string message=null)
        {
            bool cinematic=game.InCutscene;
            input.interactable=!game.Busy && !game.Won; submit.interactable=input.interactable;
            input.gameObject.SetActive(!cinematic && !game.Won); submit.gameObject.SetActive(!cinematic && !game.Won);
            if(cinematic) status.text=game.cutscenes.WaitingForNarration ? "Next to continue · Esc to skip" : "Next reveals text · Esc skips the scene";
            else if(message!=null) status.text=message;
            else if(game.RequestInFlight) status.text="Narrator is thinking...";
            objective.text=cinematic ? game.cutscenes.Title : game.levels.Current.objective+(game.ObjectiveConfirmed ? " · COMPLETE" : "");
            objective.color=game.ObjectiveConfirmed && !cinematic ? new Color(.7f,1,.65f) : Color.white;
            var items=game.inventory.Snapshot(); inventoryText.text="Satchel: "+(items.Length==0 ? "empty" : string.Join(", ",items));
            beanIcon.color=game.inventory.Has("beans") ? Color.white : new Color(1,1,1,.25f);
            eggIcon.color=game.inventory.Has("goose") ? Color.white : new Color(1,1,1,.25f);
            next.gameObject.SetActive(cinematic); next.interactable=cinematic;
            next.GetComponentInChildren<TMP_Text>(true).text="Next";
            if(skipCutscene) skipCutscene.gameObject.SetActive(cinematic);
            restart.gameObject.SetActive(true);
            victoryPanel.SetActive(game.Won);
            victoryText.text="HAPPILY EVER AFTER?\n\nJack brought the golden goose home.\nThe first egg paid the rent.\nThe second paid the goose's consultancy fee.\n\nRestart to turn back to page one.";
        }
    }
}
