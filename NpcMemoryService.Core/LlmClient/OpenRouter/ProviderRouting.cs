// Code written by Gabriel Mailhot, 13/07/2026.
// OpenRouter serves the same model from many providers, and several of them run their own output moderation:
// generation simply STOPS the moment profanity appears, so the reply arrives chopped mid-sentence and the player
// reads it as the MOD censoring them (player request: "I would love to be able to force a specific provider, and
// OpenRouter seems to allow a way to do that with their API"). The client already retries once on a
// content_filter finish; pinning a provider that does not moderate is the fix upstream of that.

#region

using System;
using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.LlmClient.OpenRouter
{
   /// <summary>Parses the player's provider pin into the slug list OpenRouter's <c>provider.order</c> expects.</summary>
   public static class ProviderRouting
   {
      /// <summary>
      ///   The provider slugs from a comma-separated player entry, trimmed, blanks dropped, duplicates removed
      ///   (case-insensitively) while preserving the order the player wrote them in, because that order IS the
      ///   preference. An empty or missing entry yields no slugs at all, which leaves OpenRouter's own routing
      ///   untouched: the pin is strictly opt-in.
      /// </summary>
      public static string[] ParseSlugs(string? csv)
      {
         if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();

         var slugs = new List<string>();
         foreach (string raw in csv!.Split(','))
         {
            string slug = raw.Trim();

            if (slug.Length == 0) continue;
            if (slugs.Exists(s => string.Equals(s, slug, StringComparison.OrdinalIgnoreCase))) continue;

            slugs.Add(slug);
         }

         return slugs.ToArray();
      }
   }
}
