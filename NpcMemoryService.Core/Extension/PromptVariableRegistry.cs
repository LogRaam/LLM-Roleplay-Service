// Code written by Gabriel Mailhot, 28/08/2026.
// Extension Surface, Prompt Variables volet (increment 4): the engine-agnostic store a third-party mod's
// facade (CalradiaRemembers.CrPrompt) writes to and NpcMemoryService.Core.Prompts.PromptBuilder reads from,
// so a mod can contribute a live {{name}} prompt variable without a Harmony patch on BuildPromptVariables.

using System;
using System.Collections.Generic;

namespace NpcMemoryService.Core.Extension
{
   /// <summary>
   ///   Holds the registered <c>name -&gt; provider</c> table for live prompt variables. A provider is a pure
   ///   function of <see cref="PromptVarFacts" /> to the variable's current text; it is called once per
   ///   prompt build (see <see cref="Compose" />). This is the engine-agnostic SDK side of the door: the CR
   ///   facade (<c>CrPrompt</c>) is the public, supported entry point a third-party mod actually calls.
   /// </summary>
   public static class PromptVariableRegistry
   {
      private static readonly object Gate = new object();
      private static readonly Dictionary<string, Func<PromptVarFacts, string>> Providers
         = new Dictionary<string, Func<PromptVarFacts, string>>(StringComparer.OrdinalIgnoreCase);

      /// <summary>
      ///   Registers (or replaces) the provider for <paramref name="name" />. A null or blank name, or a null
      ///   <paramref name="provider" />, is silently ignored (mirrors <c>GameActionCatalog.Register</c>'s
      ///   never-throw stance). Registering the same name again REPLACES the prior provider: the last
      ///   registration wins, so a mod can freely re-register on reload without leaking the old delegate.
      /// </summary>
      public static void Register(string name, Func<PromptVarFacts, string> provider)
      {
         if (string.IsNullOrWhiteSpace(name) || provider == null) return;

         lock (Gate)
         {
            Providers[name] = provider;
         }
      }

      /// <summary>Removes the provider registered under <paramref name="name" />. Returns whether one was present.</summary>
      public static bool Unregister(string name)
      {
         if (string.IsNullOrWhiteSpace(name)) return false;

         lock (Gate)
         {
            return Providers.Remove(name);
         }
      }

      /// <summary>Test-isolation reset. SDK-internal only: the mod assembly cannot call this (see CrPrompt's own cleanup note).</summary>
      internal static void Clear()
      {
         lock (Gate)
         {
            Providers.Clear();
         }
      }

      /// <summary>
      ///   Invokes every registered provider against <paramref name="facts" /> and collects the non-null
      ///   results into a <c>name -&gt; value</c> map. A provider that throws is caught and skipped: a
      ///   third-party addon's bug must never break prompt building for every conversation, the same
      ///   guarded-invoke stance the CR host takes with its own conversation-event subscribers. The provider
      ///   list is snapshotted under the lock BEFORE any provider runs, so a provider that registers or
      ///   unregisters another during its own call cannot mutate the collection this loop is iterating.
      ///   Returns an empty dictionary when nothing is registered.
      /// </summary>
      internal static IReadOnlyDictionary<string, string> Compose(PromptVarFacts facts)
      {
         List<KeyValuePair<string, Func<PromptVarFacts, string>>> snapshot;

         lock (Gate)
         {
            snapshot = new List<KeyValuePair<string, Func<PromptVarFacts, string>>>(Providers);
         }

         var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

         foreach (KeyValuePair<string, Func<PromptVarFacts, string>> entry in snapshot)
         {
            try
            {
               string? value = entry.Value(facts);
               if (value != null) result[entry.Key] = value;
            }
            catch
            {
               // Skipped: one buggy third-party provider must never break prompt building for the rest.
            }
         }

         return result;
      }
   }
}
