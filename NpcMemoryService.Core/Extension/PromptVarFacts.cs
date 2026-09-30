// Code written by Gabriel Mailhot, 28/08/2026.
// Extension Surface, Prompt Variables volet (increment 4): the engine-agnostic FACTS a live prompt-variable
// provider reasons over. Mirrors the shape VerbFacts already set for actions: plain string ids, no
// TaleWorlds dependency, so a third-party mod's provider can run without ever seeing the host's engine
// types.

using System.Collections.Generic;

namespace NpcMemoryService.Core.Extension
{
   /// <summary>
   ///   The engine-agnostic facts a registered prompt-variable provider (see
   ///   <see cref="PromptVariableRegistry" />) reasons over. The provider re-resolves any engine object it
   ///   actually needs (its own Hero, kingdom, and so on) from <see cref="NpcId" /> itself; the host
   ///   (Calradia Remembers) never hands over a live engine object here.
   ///   <para>
   ///     Deliberately minimal: <c>PromptBuilder.BuildPromptVariables</c> only has the <c>NpcProfile</c> and
   ///     the current <c>EncounterContext</c> on hand when it composes variables, never a live Hero, so this
   ///     shape promises only what that call site can actually fill. It does not carry an "IsLord" or
   ///     "IsPrisoner" flag the way <see cref="VerbFacts" /> does, because neither is available at that
   ///     point, and a facts shape that lied about what it could populate would be worse than a smaller one.
   ///   </para>
   /// </summary>
   public sealed class PromptVarFacts
   {
      /// <summary>The conversation partner's stable engine id (a Hero StringId on the Bannerlord side).</summary>
      public string NpcId { get; set; } = string.Empty;

      /// <summary>
      ///   The partner's PERSONAL REGARD for the player: CR's own ledger (-100 to 100), built in conversation, never the
      ///   game's relation (which the prompt calls "your clan's standing"). tashmetu (29/09/2026): the name made a
      ///   bridge author doubt which number it was.
      /// </summary>
      public int RelationToPlayer { get; set; }

      /// <summary>
      ///   True when the prompt this value goes into is being built Compact (the player's "Compact prompt for small
      ///   models"): CR is trimming its own sections to fit a short context, and a provider should shrink its text
      ///   too. It is the level of THIS prompt, not the setting alone (a letter is always built in full). Asked for
      ///   by tashmetu, 25/09/2026, who was reading the host's settings by reflection to find it.
      /// </summary>
      public bool Lean { get; set; }

      /// <summary>
      ///   True when this value is for ONE SEAT of a council table (the council_seat.txt template, since 2.6.2), not a
      ///   private word. One prompt voices the whole table there, and "you" means the PLAYER in it, so write about this
      ///   person in the third person (their name, he or she), never as "you". <see cref="NpcId" /> is the seat.
      ///   Asked for by tashmetu (Bellum Civile bridge, 26/09/2026).
      /// </summary>
      public bool CouncilSeat { get; set; }

      /// <summary>
      ///   Free-form extra facts a provider may need that the base shape doesn't name, keyed by the
      ///   provider's own convention (mirrors <see cref="VerbFacts.Extra" />).
      /// </summary>
      public IReadOnlyDictionary<string, string>? Extra { get; set; }
   }
}
