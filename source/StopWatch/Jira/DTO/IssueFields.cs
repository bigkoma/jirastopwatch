/**************************************************************************
Copyright 2016 Carsten Gehling

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**************************************************************************/
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StopWatch
{
    internal class IssueFields
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
        // In Jira Cloud v3 description is a rich ADT json; keep as JsonElement to avoid deserialization errors
        [JsonPropertyName("description")]
        public JsonElement? Description { get; set; }
        [JsonPropertyName("timetracking")]
        public TimetrackingFields Timetracking { get; set; }
        [JsonPropertyName("project")]
        public ProjectFields Project { get; set; }
        [JsonPropertyName("status")]
        public StatusFields Status { get; set; }
        [JsonPropertyName("created")]
        public string Created { get; set; }
    }

    internal class TimetrackingFields
    {
        [JsonPropertyName("remainingEstimate")]
        public string RemainingEstimate { get; set; }
        [JsonPropertyName("remainingEstimateSeconds")]
        public int RemainingEstimateSeconds { get; set; }
    }

    internal class ProjectFields
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } 
    }

    internal class StatusFields
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
