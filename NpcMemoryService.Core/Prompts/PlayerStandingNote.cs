// Code written by Gabriel Mailhot, 18/09/2026.
// What the player IS to this character, stated as a fact instead of left to be guessed.
//
// Player report (zhenja1zhenja1, Nexus, 17/09/2026). He beat a lord in a tournament and the lord wrote to
// him afterwards, ending the letter with, in his words, "I know you ride alongside my retinue, be good to
// them or something like that, but I am not in their retinue nor am I anywhere close to them."
//
// Nothing was inverted. The standing was resolved correctly: PlayerStandingPolicy returned Free, which is
// what a man who owes this lord nothing IS. The whole of the defect is what Free did next, which was
// NOTHING: the switch that renders the standing had `case Free: /* default, no special note */ break;`.
//
// That is the exact failure this mod already named in its own 2.5.9 changelog, about a different subject:
// the model "does not read silence as 'you do not know this', it reads silence as 'nobody mentioned it'
// and fills the gap". A letter is precisely where a lord reaches for a frame to write from, and the
// commonest relationship in Calradia had no words at all, so he invented one. The mod already answers this
// same class elsewhere, in as many words: THE PLAYER RIDES ALONE, "do not invent riders, guards or servants
// for them". It had never said the matching thing about service.
//
// TWO CHOICES WORTH WRITING DOWN.
//
// The sentence lives HERE rather than inline, because the tests for PlayerStandingPolicy deliberately assert
// the sentence the model is shown and not the enum value (the vassal/liege pair shipped inverted once, and
// asserting the enum would have declared the inversion correct). Moving the text without moving what those
// tests read would quietly turn them back into a restatement of the convention.
//
// And it is rendered ABOVE the CURRENT ENCOUNTER marker, in the cacheable prefix, where it used to ride
// below it. Who two people are to each other does not change from turn to turn; it changes when an oath is
// sworn or a war begins, so billing it fresh on every turn paid for a fact that had not moved.
//
// WHAT IT COSTS, measured rather than assumed. For a standing that already had words (vassal, liege, hired,
// fellow vassal) this is a straight move of 70 to 130 characters out of the per-turn tail and into the
// prefix: cheaper every turn after the first. For Free, which is the commonest standing in the game, the
// tail gave up nothing because it said nothing, so the prefix grows by about 500 characters, roughly 125
// tokens, written once per conversation and read from cache thereafter. That is the price of the fix and it
// is worth naming plainly: a turn is not free, it is paid once instead of never having been said.

#region

using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   The one sentence that says where the player stands with this character: sworn, hired, kin, captive,
   ///   or bound by nothing at all. Null when the host could not answer, which is the only case where
   ///   silence is honest.
   /// </summary>
   public static class PlayerStandingNote
   {
      /// <summary>The heading the prompt puts this under. Distinct from the block describing the force the player commands.</summary>
      public const string Heading = "WHERE THE PLAYER STANDS WITH YOU:";

      /// <summary>
      ///   The standing, as the model reads it. <paramref name="lean" /> trades the reasoning for the bare
      ///   fact on a small model; the fact itself is never dropped, because the fact is the fix.
      /// </summary>
      public static string? Sentence(EncounterContext? context, bool lean = false)
      {
         if (context == null) return null;

         string realm = (context.SharedRealmName ?? "").Trim();
         bool named = realm.Length > 0;

         switch (context.PlayerStatus)
         {
            case PlayerStatusVsNpc.Captive: return "The player is currently your captive.";
            case PlayerStatusVsNpc.NpcIsCaptive: return "You are currently the player's captive.";
            case PlayerStatusVsNpc.ClanMember: return "The player belongs to your own clan.";

            // Both feudal notes state WHO RULES WHAT alongside the conclusion, whenever the realm is known.
            // A bare conclusion is unfalsifiable: it shipped inverted once, contradicted the NPC's own
            // "Holds royal authority over a kingdom." rank line, and the model resolved the contradiction the
            // wrong way (an Aserai Sultan denied being Sultan and called his new vassal "my liege"). Naming
            // the realm and its ruler gives the model something to check the conclusion against.
            case PlayerStatusVsNpc.Vassal:
               return named
                  ? $"You rule {realm}, and the player has sworn to it: they are your vassal."
                  : "The player is your sworn vassal.";

            case PlayerStatusVsNpc.Liege:
               return named
                  ? $"The player rules {realm}, the realm you serve: they are your liege lord."
                  : "The player is your liege lord.";

            // Paid service, never fealty: the engine stores a mercenary contract the same way it stores an
            // oath, so the prompt has to draw the line the data does not. What follows from it is the whole
            // relationship: no loyalty is owed, the contract ends, and they may take the enemy's coin next.
            case PlayerStatusVsNpc.Mercenary:
               return named
                  ? $"The player's clan is hired to {realm}, for pay and not by oath: mercenaries, not sworn vassals."
                  : "The player's clan fights under a mercenary contract, for pay and not by oath.";

            case PlayerStatusVsNpc.Employer:
               return named
                  ? $"The player rules {realm}, which hires your clan: they are your paymaster, not your liege, and the contract will end."
                  : "Your clan fights for the player's realm under a mercenary contract, for pay and not by oath.";

            case PlayerStatusVsNpc.SameRealm:
               return named
                  ? $"You and the player are both sworn to {realm}: fellow vassals of the same crown, neither commanding the other."
                  : "The player is sworn to the same realm as you.";

            // A stranger is ALSO bound by nothing, and that half used to go unsaid here too: "never met" alone
            // leaves the service question open, and an open question is what gets filled in.
            case PlayerStatusVsNpc.Stranger:
               return lean
                  ? "You have never met this person. No oath or service binds either of you to the other."
                  : "You have never met this person before, and nothing binds the two of you: no oath either way, "
                  + "no service, no shared crown. They are a stranger, and strangers owe each other nothing.";

            // THE REPORTED CASE, and the commonest standing in the game. What it must never do again is say
            // nothing at all.
            case PlayerStatusVsNpc.Free:
               return lean
                  ? "No oath binds you to the player or them to you: not your retainer, not of your house, not in your service."
                  : "No oath or service binds the two of you, in either direction. The player is not of your house "
                  + "and holds no post in your service; they ride with none of your men, and none of your men ride "
                  + "with them. You owe them nothing and they owe you nothing, and what passes between you passes "
                  + "between free parties. Never call them your retainer, your sworn man, one of your company, or "
                  + "someone who rides under your banner, and never speak or WRITE as though they answered to you.";

            default: return null; // Unknown: the host could not answer, and inventing one here would be the same bug
         }
      }
   }
}
