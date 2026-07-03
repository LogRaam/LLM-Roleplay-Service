// Code written by Gabriel Mailhot, 03/07/2026.
// Extension Surface, Actions volet (spike): the shape a future public Actions.Register(...) API would
// accept from a third-party mod, mirroring Volet A's Feed.Record. An external mod fills these three
// callbacks; it does its own engine work (whatever ITS OWN game state needs) inside Execute, keyed off
// the plain ids in the VerbFacts it receives. The host (Calradia Remembers) only ever calls these Funcs
// and routes by Name; it never reaches into the external mod's state, and the external mod never reaches
// into the host's (the same one-way, validated-at-the-door philosophy as the world-event feed).
//
// NOT YET a public entry point: no Actions.Register(...) validation pipeline exists yet (source id, quota,
// name-collision checks in the Feed.Record style): that is the next increment. This spike only proves an
// ExternalVerbContract can sit in the SAME internal registry as the host's own verbs (see the mod's
// CalradiaRemembers.Verbs.ExternalVerbAdapter, which wraps one as an IConversationVerb).

using System;

namespace NpcMemoryService.Core.Extension
{
   /// <summary>
   ///   An externally-defined conversation verb: its name (the [ACTION] "type:" it answers to), whether it
   ///   currently applies, what it teaches the LLM, and how it executes. Every callback is engine-agnostic:
   ///   it reasons purely over <see cref="VerbFacts" /> / <see cref="ActionRequest" />, never a host type.
   /// </summary>
   public sealed class ExternalVerbContract
   {
      /// <summary>The action vocabulary name (matches the [ACTION] "type:" the LLM emits).</summary>
      public string Name { get; set; }

      /// <summary>True when this verb currently applies and should be taught / may be executed.</summary>
      public Func<VerbFacts, bool> IsEligible { get; set; }

      /// <summary>The prompt text taught to the LLM while this verb is eligible.</summary>
      public Func<VerbFacts, string> TeachingText { get; set; }

      /// <summary>Executes the verb; the external mod does its own engine work here, keyed off the ids in the request's facts.</summary>
      public Func<ActionRequest, ActionResult> Execute { get; set; }
   }
}
