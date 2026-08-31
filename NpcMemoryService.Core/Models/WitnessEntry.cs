// Code written by Gabriel Mailhot, 03/06/2026.
// Sprint multi-NPC: lightweight per-encounter witness descriptor.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Describes a single witness present during an encounter. Injected into the
   ///   system prompt so the NPC adjusts candor and behaviour based on who is watching.
   ///   Kept intentionally lightweight — name and relation only; full profiles are not
   ///   needed for passive witnesses.
   /// </summary>
   public sealed class WitnessEntry
   {
      /// <summary>Display name as it appears in the prompt (e.g. "Derthert").</summary>
      public required string Name { get; init; }

      /// <summary>
      ///   How this witness relates to the NPC being spoken to, from the NPC's
      ///   perspective. Examples: "your liege", "a rival lord", "your clan member",
      ///   "a lord you regard with quiet distrust".
      /// </summary>
      public required string RelationToNpc { get; init; }

      /// <summary>True when the witness is one of the player's own companions.</summary>
      public bool IsPlayerCompanion { get; init; }

      /// <summary>
      ///   The witness's sex, when known (player report 2026-08-31: in a multi-person scene the model guessed
      ///   each witness's sex from their NAME alone and got it wrong, voicing a female companion as "he").
      ///   Nullable because the synthetic flavour witnesses (a soldier, an apprentice, a notable's customer)
      ///   have no <c>Hero</c> to read it from; null = not stated, and the prompt then carries no gender clause
      ///   for that witness at all.
      /// </summary>
      public bool? IsFemale { get; init; }

      /// <summary>
      ///   True when this "witness" is in fact the captive-scene VICTIM — a companion held alongside the player
      ///   whom the captor torments. They are present and voiced (portrait + <c>[WITNESS_REACTION]</c>), but they
      ///   are not one of the captor's men: the host excludes them from the aggressor / audience tallies, and the
      ///   prompt frames them as the bound victim with a voice of their own, not a bystander adjusting their candor.
      /// </summary>
      public bool IsCaptiveVictim { get; init; }

      /// <summary>
      ///   Bring-participants-to-a-captor-scene, increment 2 (2026-08-21): true when this "witness" is in fact
      ///   ANOTHER of the player's own prisoners, brought into a player-as-captor scene so the player can use one
      ///   against the other (pressure, blackmail). Distinct from <see cref="IsPlayerCompanion" /> (a free
      ///   participant who chose to come and may act as a captor in their own right) and from
      ///   <see cref="IsCaptiveVictim" /> (the scene's OWN bound victim held by a hostile captor): a brought
      ///   captive is on the CAPTIVE side of a scene the PLAYER runs. They are coerced, present under duress, and
      ///   cannot leave; the host excludes them from the "outnumbered by several captors" tally
      ///   (<c>AppendPlayerCaptorSceneRules</c>) and from the acting-companion teaching
      ///   (<c>AppendCompanionActingOnCaptive</c>), and the prompt frames them as a subordinate, frightened
      ///   captive with no standing of their own, never a free witness and never a threat to the player.
      /// </summary>
      public bool IsBroughtCaptive { get; init; }

      /// <summary>
      ///   Bannerlord <c>Hero.StringId</c> — used by the mod to resolve the witness's
      ///   portrait when displaying <c>[WITNESS_REACTION]</c> messages in the chat window.
      ///   Null for manually seeded console witnesses.
      /// </summary>
      public string? HeroStringId { get; init; }

      /// <summary>
      ///   A short character descriptor (archetype / trait) so the main NPC can voice
      ///   this witness's reactions true to their nature — an aloof witness reacts
      ///   differently from an impulsive one. Null when no profile is available.
      /// </summary>
      public string? Persona { get; init; }

      /// <summary>
      ///   A council seat's true whereabouts relative to the player right now (e.g. "at your side in your
      ///   party", "keeping to Pravend"), read from live Hero state by the mod's CouncilRosterResolver. Council
      ///   bug fix: a seated member is gathered on availability alone, with no distance/travel test, so this is
      ///   the only thing that stops a member who is NOT actually travelling with the player from being voiced
      ///   as though they were. Null for an ordinary (non-council) witness, where the concept does not apply.
      /// </summary>
      public string? PresenceStatus { get; init; }

      /// <summary>
      ///   This witness's OWN memory of the player, a short compressed recall (their salient history and any
      ///   agreements), so the main speaker's single call can voice their reaction true to what THEY remember,
      ///   not just their name and persona (player report 2026-08-23: a companion who had agreed to something in
      ///   a prior one-on-one answered, in a group scene, as if she had never heard of it). The mod builds it
      ///   from that witness's already-compressed profile events. Null when no profile is available.
      /// </summary>
      public string? Memory { get; init; }

      /// <summary>
      ///   Equipment-awareness pillar (2026-08-29): this witness's notable equipped gear, one short Subject-voice
      ///   clause ("He bears a masterwork sword."), composed by the host via the pure EquipmentNotabilityPolicy
      ///   (CalradiaRemembers.Logic). Kept compact by design: a natural spot on the existing witness line rather
      ///   than a new per-person block. Null when nothing about this witness's gear stands out.
      /// </summary>
      public string? Gear { get; init; }
   }
}
