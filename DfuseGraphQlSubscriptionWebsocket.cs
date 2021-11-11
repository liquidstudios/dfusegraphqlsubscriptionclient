using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class DfuseGraphQlSubscriptionWebSocket : WebSocket
{
    public Queue<string> MessageQueue = new Queue<string>();

    public event Action OnSubscriptionHandshakeComplete;
    public event Action<string> OnSubscriptionDataReceived;
    public event Action OnSubscriptionCanceled;

    public DfuseGraphQlSubscriptionWebSocket(string url, Dictionary<string, string> headers = null) : base(url.Replace("http", "ws"), headers)
    {
        OnMessage += OnMessageReceived;
    }

    public DfuseGraphQlSubscriptionWebSocket(string url, string subprotocol, Dictionary<string, string> headers = null) : base(url.Replace("http", "ws"), subprotocol, headers)
    {
        OnMessage += OnMessageReceived;
    }

    public DfuseGraphQlSubscriptionWebSocket(string url, List<string> subprotocols, Dictionary<string, string> headers = null) : base(url.Replace("http", "ws"), subprotocols, headers)
    {
        OnMessage += OnMessageReceived;
    }

    public DfuseGraphQlSubscriptionWebSocket(string url, string protocol = "graphql-ws", string authToken = null) : base(url.Replace("http", "ws"), new List<string>() { protocol }, (!string.IsNullOrEmpty(authToken) ? new Dictionary<string, string>() { { "Authorization", "Bearer " + authToken } } : null))
    {
        OnMessage += OnMessageReceived;
    }

    public DfuseGraphQlSubscriptionWebSocket(string url, string protocol = "graphql-ws") : base(url.Replace("http", "ws"), new List<string>() { protocol }, null)
    {
        OnMessage += OnMessageReceived;
    }

    public DfuseGraphQlSubscriptionWebSocket(string url) : base(url.Replace("http", "ws"), new List<string>() { "graphql-ws" }, null)
    {
        OnMessage += OnMessageReceived;
    }

    public async Task ConnectAndSubscribe(string graphQlQuery, string socketId = "1")
    {
        if (string.IsNullOrEmpty(graphQlQuery))
            throw new Exception("graphQlQuery is null or empty, you have to provide a query to subscribe");

        try
        {
            Init();
            SendQuery(socketId, graphQlQuery);

            await this.Connect();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnMessageReceived(byte[] data)
    {
        try
        {
            var message = Encoding.UTF8.GetString(data ?? throw new ApplicationException("Something went wrong, received data was null"));
            if (OnSubscriptionDataReceived == null)  // Don't handle data if event-handler not set
                return;

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
                        SubscriptionHandshakeComplete();
                        break;
                    }
                case "error":
                    {
                        Debug.Log("The handshake failed. Error: " + message);
                        break;
                    }
                case "connection_error":
                    {
                        Debug.Log("The handshake failed. Error: " + message);
                        break;
                    }
                case "data":
                    {
                        SubscriptionDataReceived(message);
                        break;
                    }
                case "ka":
                    {
                        Debug.Log(message);
                        break;
                    }
                case "subscription_fail":
                    {
                        Debug.Log("The subscription failed");
                        throw new Exception("The subscription failed");
                    }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void Init()
    {
        MessageQueue.Enqueue("{\"type\":\"connection_init\",\"payload\":{\"Authorization\":\"\"}}");
    }

    private void SendQuery(string id, string query)
    {
        MessageQueue.Enqueue(JsonConvert.SerializeObject(new { id = $"{id}", type = "start", payload = new { query = query } }));
    }


    public void SubscriptionHandshakeComplete()
    {
        OnSubscriptionHandshakeComplete?.Invoke();
    }

    public void SubscriptionDataReceived(string data)
    {
        OnSubscriptionDataReceived?.Invoke(data);
    }

    public void SubscriptionCanceled()
    {
        OnSubscriptionCanceled?.Invoke();
    }
}
