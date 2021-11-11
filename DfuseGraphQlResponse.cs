using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DfuseGraphQl
{
    public partial class DfuseGraphQlResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("payload")]
        public Payload Payload { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public partial class Payload
    {
        [JsonProperty("data")]
        public PayloadData Data { get; set; }
    }

    public partial class PayloadData
    {
        [JsonProperty("searchTransactionsForward")]
        public SearchTransactionsForward SearchTransactionsForward { get; set; }
    }

    public partial class SearchTransactionsForward
    {
        [JsonProperty("undo")]
        public bool Undo { get; set; }

        [JsonProperty("trace")]
        public Trace Trace { get; set; }
    }

    public partial class Trace
    {
        [JsonProperty("matchingActions")]
        public MatchingAction[] MatchingActions { get; set; }
    }

    public partial class MatchingAction
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("account")]
        public string Account { get; set; }

        [JsonProperty("authorization")]
        public Authorization[] Authorization { get; set; }

        [JsonProperty("dbOps")]
        public DbOp[] DbOps { get; set; }

        [JsonProperty("json")]
        public object Json { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }

    public partial class Authorization
    {
        [JsonProperty("actor")]
        public string Actor { get; set; }

        [JsonProperty("permission")]
        public string Permission { get; set; }
    }

    public partial class DbOp
    {
        [JsonProperty("oldJSON")]
        public OldJson OldJson { get; set; }

        [JsonProperty("newJSON")]
        public NewJson NewJson { get; set; }

        [JsonProperty("key")]
        public TableKey Key { get; set; }
    }

    public partial class TableKey
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("table")]
        public string Table { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }
    }

    public partial class NewJson
    {
        [JsonProperty("object")]
        public object Object { get; set; }

        [JsonProperty("error")]
        public object Error { get; set; }
    }

    public partial class OldJson
    {
        [JsonProperty("object")]
        public object Object { get; set; }

        [JsonProperty("error")]
        public object Error { get; set; }
    }

}
