// Code written by Gabriel Mailhot, 29/06/2026.
// Some OpenAI-"compatible" endpoints (e.g. Chub / Mars) stream their reply as Server-Sent Events even when the
// request asks for a single non-streamed object (stream:false), so the body arrives as a series of "data: {chunk}"
// lines instead of one JSON object. Our parser expects one object and fails with "Unexpected character ... d"
// (the leading 'd' of "data:"). This transformer detects that streamed shape and rebuilds it into the standard
// single-object envelope our parser already understands, by concatenating each chunk's delta. It is a defensive
// net: when stream:false is honoured (the common case), LooksLikeServerSentEvents is false and nothing here runs.

#region

using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace NpcMemoryService.Core.LlmClient.OpenRouter
{
   /// <summary>
   ///   Normalizes a non-conforming chat-completions body into the standard single-object OpenAI envelope.
   ///   Currently handles Server-Sent-Events (streamed) responses, which some endpoints return even when asked
   ///   not to: the <c>data: {chunk}</c> lines are reassembled into one
   ///   <c>{ "choices": [ { "message": { "content": ... }, "finish_reason": ... } ], "usage": ... }</c> object.
   /// </summary>
   public static class ChatResponseTransformer
   {
      /// <summary>
      ///   When <paramref name="body" /> is a streamed (SSE) response, rebuilds it into the standard
      ///   single-object envelope and returns true; otherwise returns false and leaves the body to be parsed
      ///   as-is. The rebuilt object carries the concatenated message content, the last finish_reason seen, and
      ///   the usage block if any chunk reported one. Malformed lines are skipped rather than failing the whole.
      /// </summary>
      public static bool TryNormalizeStreamedResponse(string? body, out string normalizedJson)
      {
         normalizedJson = string.Empty;
         if (!LooksLikeServerSentEvents(body)) return false;

         var content = new StringBuilder();
         string? finishReason = null;
         JObject? usage = null;
         var sawChunk = false;

         foreach (string payload in EnumerateDataPayloads(body!))
         {
            JObject chunk;
            try { chunk = JObject.Parse(payload); }
            catch { continue; } // a malformed data line is skipped, not fatal

            sawChunk = true;
            JToken? choice = chunk["choices"]?[0];
            if (choice != null)
            {
               // Streaming chunks carry the text under delta.content; some endpoints send a full message.content.
               string? piece = choice["delta"]?["content"]?.Value<string>()
                            ?? choice["message"]?["content"]?.Value<string>();
               if (piece != null) content.Append(piece);

               string? fr = choice["finish_reason"]?.Value<string>();
               if (!string.IsNullOrEmpty(fr)) finishReason = fr;
            }

            if (chunk["usage"] is JObject u) usage = (JObject) u.DeepClone();
         }

         if (!sawChunk) return false; // looked like SSE but held nothing parseable — let the caller report the raw body

         var choiceObject = new JObject {
            ["message"] = new JObject {["role"] = "assistant", ["content"] = content.ToString()}
         };
         if (finishReason != null) choiceObject["finish_reason"] = finishReason;

         var root = new JObject {["choices"] = new JArray {choiceObject}};
         if (usage != null) root["usage"] = usage;

         normalizedJson = root.ToString(Formatting.None);

         return true;
      }

      #region private

      private static bool LooksLikeServerSentEvents(string? body)
      {
         if (string.IsNullOrWhiteSpace(body)) return false;

         // The first non-whitespace content is an SSE field ("data:" / "event:" / a ":" comment), not a JSON
         // object or array. A genuine single JSON object starts with '{'.
         string trimmed = body!.TrimStart();

         return trimmed.StartsWith("data:") || trimmed.StartsWith("event:") || trimmed.StartsWith(":");
      }

      private static IEnumerable<string> EnumerateDataPayloads(string body)
      {
         foreach (string raw in body.Split('\n'))
         {
            string line = raw.Trim();
            if (!line.StartsWith("data:")) continue;

            string payload = line.Substring("data:".Length).Trim();
            if (payload.Length == 0 || payload == "[DONE]") continue;

            yield return payload;
         }
      }

      #endregion
   }
}
