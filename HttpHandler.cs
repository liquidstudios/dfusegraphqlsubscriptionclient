using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NativeWebSocket;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace GraphQlClient.Core
{
    public class HttpHandler
	{
        public static event Action OnRequestBegin;
        public static event Action<Exception> OnRequestFailed;
        public static event Action<string> OnRequestEnded;

        public static async Task<UnityWebRequest> PostAsync(string url, string details, string authToken = null){
            var jsonData = JsonConvert.SerializeObject(new{query = details});
            var postData = Encoding.ASCII.GetBytes(jsonData);
            var request = UnityWebRequest.Post(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(authToken)) 
                request.SetRequestHeader("Authorization", "Bearer " + authToken);
            
            OnRequestBegin?.Invoke();
            
            try{
                await request.SendWebRequest();
            }
            catch(Exception e){
                OnRequestFailed?.Invoke(e);
            }
			Debug.Log(request.downloadHandler.text);
            
            OnRequestEnded?.Invoke(request.downloadHandler.text);
            return request;
        }

        #region Websocket

		public static async Task<DfuseGraphQlSubscriptionWebSocket> WebsocketConnect(string subscriptionUrl, string details, string authToken = null, string socketId = "1", string protocol = "graphql-ws"){
			var subUrl = subscriptionUrl.Replace("http", "ws");
			var id = socketId;
            var cws = new DfuseGraphQlSubscriptionWebSocket(subUrl, new List<string>(){protocol}, (!string.IsNullOrEmpty(authToken) ? new Dictionary<string, string>(){{"Authorization", "Bearer " + authToken}} : null));
            try{
				await cws.Connect();
                cws.OnMessage += (data) => OnMessageReceived(cws, data);
                if (cws.State == WebSocketState.Open)
					Debug.Log("connected");
				await WebsocketInit(cws);
				await WebsocketSend(cws, id, details);
			}
			catch (Exception e){
				Debug.Log("woe " + e.Message);
			}

			return cws;
		}
		
		public static async Task<DfuseGraphQlSubscriptionWebSocket> WebsocketConnect(DfuseGraphQlSubscriptionWebSocket cws, string subscriptionUrl, string details, string socketId = "1"){
			var subUrl = subscriptionUrl.Replace("http", "ws");
			var id = socketId;
            cws = new DfuseGraphQlSubscriptionWebSocket(subUrl);
            try
            {
                cws.OnMessage += (data) => OnMessageReceived(cws, data);
				await cws.Connect();
				if (cws.State == WebSocketState.Open)
					Debug.Log("connected");
				await WebsocketInit(cws);
				await WebsocketSend(cws, id, details);
			}
			catch (Exception e){
				Debug.Log("woe " + e.Message);
			}

			return cws;
		}

        private static void OnMessageReceived(DfuseGraphQlSubscriptionWebSocket cws, byte[] data)
        {
            var message = Encoding.UTF8.GetString(data ?? throw new ApplicationException("data = null"));

            if (string.IsNullOrEmpty(message))
                return;
            JObject obj;
            try
            {
                obj = JObject.Parse(message);
            }
            catch (JsonReaderException e)
            {
                throw new ApplicationException(e.Message);
            }

            var subType = (string)obj["type"];
            switch (subType)
            {
                case "connection_ack":
                {
                    Debug.Log("init_success, the handshake is complete");
                    cws.SubscriptionHandshakeComplete();
                    break;
                }
                case "error":
                {
                    throw new ApplicationException("The handshake failed. Error: " + message);
                }
                case "connection_error":
                {
                    throw new ApplicationException("The handshake failed. Error: " + message);
                }
                case "data":
                {
                    cws.SubscriptionDataReceived(message);
                    break;
                }
                case "ka":
                {
                    Debug.Log(message);
                    break;
                }
                case "subscription_fail":
                {
                    throw new ApplicationException("The subscription data failed");
                }

            }

		}

        static async Task WebsocketInit(DfuseGraphQlSubscriptionWebSocket cws){
            await cws.SendText("{\"type\":\"connection_init\"}");
		}
		
		static async Task WebsocketSend(DfuseGraphQlSubscriptionWebSocket cws, string id, string details){
            await cws.SendText(JsonConvert.SerializeObject(new { id = $"{id}", type = "start", payload = new { query = details } }));
		}

        public static async Task WebsocketDisconnect(DfuseGraphQlSubscriptionWebSocket cws, string socketId = "1"){ 
			await cws.SendText($"{{\"type\":\"stop\",\"id\":\"{socketId}\"}}");
			await cws.Close();
            cws.SubscriptionCanceled();
        }
		
		#endregion

		#region Utility

		public static string FormatJson(string json)
        {
            var parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

		#endregion
	}

    /// <summary>
    /// Class to implement async / await on a UnityWebRequest class
    /// </summary>
    public class UnityWebRequestAwaiter : INotifyCompletion
    {
        private UnityWebRequestAsyncOperation asyncOp;
        private Action continuation;

        public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp)
        {
            this.asyncOp = asyncOp;
            asyncOp.completed += OnRequestCompleted;
        }

        public bool IsCompleted { get { return asyncOp.isDone; } }

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;
        }

        private void OnRequestCompleted(AsyncOperation obj)
        {
            if (continuation != null)
                continuation();
        }
    }

    /// <summary>
    /// Extender to augment UnityWebRequest class
    /// </summary>
    public static class ExtensionMethods
    {
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            return new UnityWebRequestAwaiter(asyncOp);
        }
    }
}
