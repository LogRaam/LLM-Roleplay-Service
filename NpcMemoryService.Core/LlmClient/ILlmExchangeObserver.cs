// Code written by Gabriel Mailhot, 14/11/2026.
// A seat that sees each finished exchange: what was asked and what came back. Gabriel (14/11/2026) wants a "report a
// mistake" button whose report carries the whole context: the prompt the character was given and the model's raw reply,
// so a player report can be diagnosed the first time. The wire observer sees requests only, and only while a diagnostic
// listens; this one sees the answer too.
//
// Null by default; whoever attaches one decides what it keeps. Implementations MUST NOT throw and MUST NOT block: the
// client calls this on the request path, guards it, and discards anything it raises.

#region

using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.LlmClient
{
   /// <summary>Sees each finished exchange with the model: the request as asked, the response as it came back, and the model used.</summary>
   public interface ILlmExchangeObserver
   {
      void OnExchange(LlmRequest request, LlmResponse response, string model);
   }
}
