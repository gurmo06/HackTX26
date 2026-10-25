using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace JackGame
{
    public class NarratorClient : MonoBehaviour
    {
        public bool UseMockNarrator = true;
        public string ApiUrl = "/api/narrate";
        [Tooltip("At least 55 seconds to allow the backend two 25-second attempts.")]
        [Range(55,90)] public int TimeoutSeconds = 55;
        string lastStatus;
        public string StatusText => UseMockNarrator ? "Offline narrator" : lastStatus ?? "Live mode · awaiting first response";
        UnityWebRequest active;
        public void Cancel() { if(active!=null) { active.Abort(); active.Dispose(); active=null; } }
        void OnDestroy() => Cancel();
        public IEnumerator Request(NarratorRequest data, Action<NarratorResponse,string> completed)
        {
            if(UseMockNarrator) { yield return new WaitForSecondsRealtime(.25f); completed(NarratorMock.Respond(data),"Offline narrator"); yield break; }
            UnityWebRequest request=null;
            string setupError=null;
            try {
                var url=ApiUrl;
                if(url.StartsWith("/") && !string.IsNullOrEmpty(Application.absoluteURL)) url=new Uri(new Uri(Application.absoluteURL),url).AbsoluteUri;
                request=new UnityWebRequest(url,"POST");
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(data)));
                request.downloadHandler=new DownloadHandlerBuffer(); request.timeout=Mathf.Max(55,TimeoutSeconds);
                request.SetRequestHeader("Content-Type","application/json");
            } catch(Exception ex) { setupError=ex.GetType().Name; }
            if(setupError!=null) { request?.Dispose(); lastStatus="Offline fallback: check API URL"; completed(NarratorMock.Respond(data),lastStatus); yield break; }
            active=request;
            UnityWebRequestAsyncOperation operation=null;
            try { operation=request.SendWebRequest(); } catch(Exception) { setupError="Connection failed"; }
            if(operation!=null) yield return operation;
            NarratorResponse response=null;
            string status="Live narrator";
            try {
                if(setupError!=null || request.result!=UnityWebRequest.Result.Success || request.downloadHandler.data.Length>32768) throw new InvalidOperationException();
                response=JsonUtility.FromJson<NarratorResponse>(request.downloadHandler.text);
                if(response==null || string.IsNullOrWhiteSpace(response.narration) || response.narration.Length>350 || response.actions==null || response.actions.Length>3) throw new InvalidOperationException();
                if(response.explanation=="offline" || response.explanation=="api_unavailable") {
                    status=response.explanation=="offline" ? "Offline fallback: server live mode disabled or key missing" : ProviderFailure(request.GetResponseHeader("X-Narrator-Error"));
                    response=NarratorMock.Respond(data);
                }
            } catch(Exception) { response=NarratorMock.Respond(data); status="Offline fallback: connection or response error"; }
            finally { if(active==request) active=null; request.Dispose(); }
            lastStatus=status; completed(response,status);
        }
        static string ProviderFailure(string reason)
        {
            switch(reason) {
                case "provider_busy": return "Offline fallback: Gemini is busy · try again shortly";
                case "provider_quota": return "Offline fallback: Gemini quota or rate limit";
                case "provider_auth": return "Offline fallback: check server API key / permissions";
                case "provider_model": return "Offline fallback: configured Gemini model unavailable";
                case "provider_request": return "Offline fallback: Gemini rejected the server request";
                case "provider_timeout": return "Offline fallback: Gemini timed out";
                default: return "Offline fallback: Gemini request failed";
            }
        }
    }
}
