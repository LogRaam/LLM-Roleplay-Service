// Code written by Gabriel Mailhot, 28/11/2026.
// Stage 3 of the group-scene plan, the composed intro (Calradia Remembers docs/design/group-chat/integration-plan-
// 2026-11-28.md). Gabriel, 28/11/2026: "au début de chaque CHAT on ajoute l'intro ... ça met en scène". Before the first
// line a composer reads what the game knows (the place, the hour, the season, how the player comes, a battle they just
// fought together) and opens the scene; it also gives the character what weighs on them today and what could rub with the
// player. His rule: the composer "chooses among true facts and stages them; it invents nothing the game tracks".
//
// This is the hidden half: the concern and the tension, handed to the character's prompt for that one conversation. It
// lives on the EncounterContext only (never on the profile, never saved), is frozen at the opening, and is null on every
// conversation that was not composed, so the prompt of every other conversation is unchanged.

namespace NpcMemoryService.Core.Models
{
   /// <summary>What a composed opening left the character for this conversation only, kept from the player.</summary>
   public sealed class ComposedOpening
   {
      /// <summary>What weighs on the character today and what they want from this meeting without saying it.</summary>
      public string? Concern { get; init; }

      /// <summary>What in their true history with the player, or in how the player came, could rub.</summary>
      public string? Tension { get; init; }

      /// <summary>True when there is anything to show.</summary>
      public bool HasContent => !string.IsNullOrWhiteSpace(Concern) || !string.IsNullOrWhiteSpace(Tension);
   }
}
