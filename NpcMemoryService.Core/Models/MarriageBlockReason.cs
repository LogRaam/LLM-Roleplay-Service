// Code written by Gabriel Mailhot, 23/07/2026.
// Player report (Nexus, 2026-07-23): a player courted Ira, was sent by her mother to capture a city, took it,
// returned for the family's blessing and got it, then went to Ira and "married" her in dialogue. Nothing
// happened mechanically. The machinery was right (the marry action is taught only when the game can honour it,
// and the bridge re-validates before sealing), and the prompt even carried an anti-empty-promise guard for the
// not-yet-eligible case. But that guard said only that marriage is "a formal, family-gated matter": it never
// said WHICH gate was shut. So the player did everything the mod visibly asked of him and hit a wall nobody,
// not even the NPC he was speaking to, could name.
// This enum is that missing word. The host resolves exactly one reason and the NPC speaks it in character, so
// an invisible rule becomes a scene: "my mother has blessed it, but you and I are still near-strangers".

namespace NpcMemoryService.Core.Models
{
   /// <summary>Why a marriage between the player and this NPC cannot be sealed right now.</summary>
   public enum MarriageBlockReason
   {
      /// <summary>Nothing bars it: the match is on offer (or the question does not arise, e.g. the NPC is already wed to someone else).</summary>
      None,

      /// <summary>The player already has a spouse. No second marriage exists in law.</summary>
      PlayerAlreadyWed,

      /// <summary>The NPC is of the player's own clan, which custom forbids.</summary>
      SameClan,

      /// <summary>The two cannot be wed under the game's own rules at all (age, sex, an existing marriage on the NPC's side).</summary>
      NotMarriageable,

      /// <summary>One of the two is a captive. Vows are not exchanged through the bars of a cell.</summary>
      Captive,

      /// <summary>Their peoples are on opposite sides of an active war, which the game itself forbids marrying across.</summary>
      AtWar,

      /// <summary>
      ///   The one gate that is about THEM rather than about law or circumstance: the NPC does not yet hold the
      ///   player dear enough. This is the reason the reporting player hit, and the only one he could plausibly
      ///   have fixed had anyone told him: he had built the FAMILY's goodwill through a quest, while the bride's
      ///   own regard never moved (a quest's relation reward goes to the giver).
      /// </summary>
      RegardTooLow
   }
}
