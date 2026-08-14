// Code written by Gabriel Mailhot, 14/08/2026.
// Unified Action Catalog, Stage 1: the mod's bridge (BannerlordGameStateBridge.ExecuteAction) dispatches far more
// action verbs than the Action Interpreter was ever taught, so a real deed the LLM narrates can silently never
// fire. This file is the SINGLE source of truth for every verb the bridge (and its ChatViewModel chat-flow
// controls) actually dispatch, built by reading each executor directly. Consumed by
// ActionInterpreterPromptBuilder to teach the interpreter every action beyond the five core reactive signals
// already hand-tuned there, and by an in-game self-test (action_catalog_parity) asserting this list never drifts
// from BannerlordGameStateBridge.HandledActionTypes.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>
   ///   One named parameter a <see cref="GameActionSpec" /> accepts, with a short human-readable meaning so the
   ///   interpreter prompt can explain what value belongs there.
   /// </summary>
   public sealed class GameActionParam
   {
      public GameActionParam(string name, string meaning)
      {
         Name = name;
         Meaning = meaning;
      }

      /// <summary>The parameter's key exactly as read from <c>GameAction.Parameters</c> by the bridge.</summary>
      public string Name { get; }

      /// <summary>A short human-readable explanation of what value belongs in this parameter.</summary>
      public string Meaning { get; }
   }

   /// <summary>
   ///   One action verb the game bridge (or its ChatViewModel chat-flow controls) actually dispatches: its
   ///   <see cref="Type" /> key, a one-line factual <see cref="Description" /> of the deed, and the
   ///   <see cref="Parameters" /> it reads. Immutable, built once from the catalog's static data.
   /// </summary>
   public sealed class GameActionSpec
   {
      public GameActionSpec(string type, string description, IReadOnlyList<GameActionParam> parameters)
      {
         Type = type;
         Description = description;
         Parameters = parameters ?? new List<GameActionParam>();
      }

      /// <summary>The action's <c>type:</c> value, exactly as the bridge switch (or ChatViewModel) matches it.</summary>
      public string Type { get; }

      /// <summary>A one-line, factual description of what the deed IS, derived from the bridge's own executor.</summary>
      public string Description { get; }

      /// <summary>The named parameters this action accepts, in the order the bridge reads them. Empty when none.</summary>
      public IReadOnlyList<GameActionParam> Parameters { get; }
   }

   /// <summary>
   ///   The SINGLE source of truth for every action verb the mod's game bridge
   ///   (<c>BannerlordGameStateBridge.ExecuteAction</c>) and its ChatViewModel chat-flow controls
   ///   (<c>witness_leaves</c>, <c>request_privacy</c>, <c>retire</c>) actually dispatch. Every description and parameter list
   ///   here was read directly from the bridge's own executors and the IConversationVerb classes it routes to via
   ///   VerbRegistry, never invented: a wrong entry here would teach the LLM a wrong deed.
   ///   <para>
   ///     Consumed by <see cref="Prompts.ActionInterpreterPromptBuilder" /> to teach every action beyond the five
   ///     core reactive signals (change_relation, end_conversation, give_gold, take_gold, [EVENT]) already taught,
   ///     by hand, with carefully-tuned wording. Also consumed, on the mod side, by an in-game self-test asserting
   ///     this list stays in lockstep with <c>BannerlordGameStateBridge.HandledActionTypes</c>, an engine-bound set
   ///     that cannot be unit-tested from this pure SDK.
   ///   </para>
   ///   <para>
   ///     <c>end_conversation</c> is deliberately ABSENT from this catalog: it is a ChatViewModel chat-flow control
   ///     like <c>witness_leaves</c>/<c>request_privacy</c>, but it is already one of the five hand-tuned core
   ///     signals, so teaching it again from here would be redundant.
   ///   </para>
   ///   <para>
   ///     <c>gather_news</c> is a DEPRECATED backward-compat alias of <c>dispatch_mission</c> (2026-08-05): the
   ///     bridge switch still dispatches it so a stray legacy emission still resolves, but it is no longer actively
   ///     taught in the 1:1 prose prompt. Its description says so plainly, steering the model toward
   ///     <c>dispatch_mission</c> instead, while keeping catalog/bridge parity intact.
   ///   </para>
   /// </summary>
   public static class GameActionCatalog
   {
      private static readonly IReadOnlyList<GameActionSpec> _all = BuildAll();
      private static readonly IReadOnlyCollection<string> _types = BuildTypes();

      /// <summary>Every action spec, in a stable order mirroring the bridge switch, then the two chat-flow controls.</summary>
      public static IReadOnlyList<GameActionSpec> All => _all;

      /// <summary>Every action's <see cref="GameActionSpec.Type" />, for parity checks against the bridge's own authoritative set.</summary>
      public static IReadOnlyCollection<string> Types => _types;

      #region private

      private static IReadOnlyList<GameActionSpec> BuildAll()
      {
         return new List<GameActionSpec> {
            Spec("change_relation",
               "Shifts the NPC's own personal regard for the player by a signed amount (cooldown, cap, and captivity floor are re-applied by the gate before it lands).",
               new GameActionParam("delta", "signed integer, the proposed relation shift")),
            Spec("give_gold",
               "The NPC hands the player a sum of denars from their own purse (clamped to what they actually hold).",
               new GameActionParam("amount", "whole number of denars")),
            Spec("take_gold",
               "The player pays, hands over, or is made to surrender a sum of denars to the NPC (clamped to the player's purse).",
               new GameActionParam("amount", "whole number of denars")),
            Spec("pay_blackmail",
               "The player settles, in full, a bastard-mother's outstanding demand for her silence; the bridge itself looks up and pays the exact owed sum."),
            Spec("join_party",
               "A free, recruitable wanderer agrees to take service as a companion in the player's own party for an agreed hiring price.",
               new GameActionParam("price", "the agreed denars (optional; defaults to the vanilla asking price, clamped to 75-125% of it)")),
            Spec("join_as_mercenary",
               "The player's clan enlists as paid mercenary swords under this NPC's kingdom."),
            Spec("end_mercenary",
               "The player's clan's current mercenary contract is dissolved (the mirror of join_as_mercenary)."),
            Spec("join_as_vassal",
               "This kingdom's ruler swears the player's clan into full vassalage under their banner (a permanent oath of fealty, distinct from a mercenary contract)."),
            Spec("mediate_peace",
               "The player, as mediator, brokers peace between the NPC's own realm (the NPC must be its ruler) and a named enemy realm; the player's own faction need not be involved.",
               new GameActionParam("target_faction", "the enemy realm the peace is brokered with")),
            Spec("join_clan",
               "A lord (not a clan leader) forsakes their own house and swears into the player's clan."),
            Spec("scheme_assist",
               "The player throws in with the NPC's own secret scheme against a rival, advancing the plot and warming the plotter toward the player."),
            Spec("scheme_heed",
               "The NPC heeds the player's warning of a secret plot against them, exposing the scheme and warming toward the player."),
            Spec("marry",
               "The NPC and the player are wed in law (a real MarriageAction.Apply). A wanderer companion match is first promoted from Wanderer to Lord of the player's clan so the engine accepts the union."),
            Spec("take_as_consort",
               "The NPC and the player openly name a committed bond between them, a mod-tracked status short of legal marriage; the player may already be wed."),
            Spec("take_as_secret_lover",
               "A faithful companion and the player welcome a discreet, hidden romantic bond; deliberately silent (no world-event, no chronicle) so the secrecy holds."),
            Spec("open_relationship",
               "The player's committed partner (spouse, or a consort/committed bond) agrees to reciprocal open terms, each free to love another."),
            Spec("close_relationship",
               "The player's committed partner revokes previously-agreed open terms; she will take no new lovers (an existing affair, if any, is untouched)."),
            Spec("end_affair",
               "The NPC's current secret affair ends at the player's asking, when the NPC agrees; the lover link and every affair-tracking field are cleared."),
            Spec("give_item",
               "The player gives one item from their own party inventory to the NPC, who may equip it on the spot if it fits and suits them.",
               new GameActionParam("item", "the item's name, matched case-insensitively against the player's roster")),
            Spec("give_prisoner",
               "The player hands over the exact captive an outstanding deliver-prisoner bargain named; the bridge resolves which held prisoner satisfies it and settles the bargain's reward."),
            Spec("free_prisoner",
               "The player releases a captive they hold, honouring a struck bargain for their freedom (Fear/Respect may mark the release as fear-coerced rather than a mercy)."),
            Spec("execute_prisoner",
               "The player kills, at their own hand, a hero prisoner they currently hold."),
            Spec("execute_player",
               "The captor holding the player prisoner ends the player's life. Hardcore-only, MCM opt-in required, and irreversible: every guard is re-checked live before it lands."),
            Spec("turn_nemesis",
               "A tracked nemesis the player holds prisoner is spared, freed, sworn into the player's clan and party, and their vendetta closed for good."),
            Spec("recruit_prisoner",
               "A captured hero prisoner the player holds is persuaded to switch sides: freed from captivity and sworn straight into the player's clan and party."),
            Spec("recruit_notable",
               "A settlement notable (gang leader, headman, merchant, artisan, rural notable, or preacher) leaves their post and swears into the player's clan as a companion; their role passes to a successor in the same settlement, and a starting relation bonus is applied."),
            Spec("grant_blessing",
               "The NPC, as head of their clan, consents to the player marrying the named kin of their house (or the NPC themselves).",
               new GameActionParam("hero", "the name of the NPC's kin (or the NPC) the blessing covers")),
            Spec("arrange_marriage",
               "The player weds one of their own unwed kin to one of the NPC's house, a match in which the player is neither spouse.",
               new GameActionParam("player_kin", "the player's own unwed kin to be wed"),
               new GameActionParam("target_kin", "the NPC's house's unwed kin (or the NPC) to be wed")),
            Spec("appoint_governor",
               "The player names the NPC, who is of the player's own clan, to govern one of the clan's fiefs that currently has no governor.",
               new GameActionParam("target_fief", "the town or castle the NPC will govern")),
            Spec("assign_party_role",
               "The player sets the NPC, a companion riding in their own party, to a duty within it.",
               new GameActionParam("target_role", "one of: scout, engineer, quartermaster, surgeon")),
            Spec("rejoin_party",
               "An away companion agrees to come back to the player's party, stepping down first from any post (e.g. a governorship) they hold elsewhere."),
            Spec("dispatch_mission",
               "The player sends the NPC, a companion in their own party, out on an errand; the game itself picks the destination, never the model.",
               new GameActionParam("target_mission", "one of: gathernews, spy, steal, barter, envoy"),
               new GameActionParam("about", "optional: the realm, town, or lord the errand concerns; omitted when left open")),
            Spec("grant_fief",
               "The player, as sovereign, grants one of their own crown fiefs to the NPC's house (a vassal of a separate clan in the player's kingdom).",
               new GameActionParam("target_fief", "the town or castle granted")),
            Spec("revoke_fief",
               "The player, as sovereign, strips a fief back from the NPC's (separate) vassal house, returning it to the crown; plants a grave grudge.",
               new GameActionParam("target_fief", "the town or castle revoked")),
            Spec("expel_from_clan",
               "The player casts the NPC, a companion of their own clan, out of it entirely; the NPC becomes a Fugitive."),
            Spec("grant_stipend",
               "The player puts the NPC, a companion of their own clan, on a recurring daily wage, funded up front for a fixed tranche of days.",
               new GameActionParam("target_amount", "the daily denars requested, clamped to the policy's [50, 500] range")),
            Spec("swear_oath",
               "The NPC swears a verifiable, tracked oath to the player: to pay a sum, to keep the peace with a named faction, or to fight at their side within a deadline.",
               new GameActionParam("oath_kind", "one of: pay_gold, keep_peace, protect"),
               new GameActionParam("target_amount", "the denars owed, only when oath_kind is pay_gold"),
               new GameActionParam("target_faction", "the house to keep peace with, only when oath_kind is keep_peace")),
            Spec("harm_prisoner",
               "The captor inflicts real physical harm on the player they hold captive, deducting HP (Hardcore-only, a live captive scene required, capped per scene and never lethal).",
               new GameActionParam("severity", "light/mild (default), moderate/heavy, or severe/grievous")),
            Spec("impregnation_risk",
               "Flags that a vaginal act was carried to completion this turn; the fertile female of the pairing has a small chance to conceive. Silent: never surfaces a chat outcome."),
            Spec("gather_news",
               "DEPRECATED backward-compat alias of dispatch_mission (2026-08-05): the same errand dispatch under the old 'errand'/'about' vocabulary. No longer actively taught; prefer dispatch_mission.",
               new GameActionParam("errand", "optional: news/scout/steal/trade/envoy; blank defaults to gathernews"),
               new GameActionParam("about", "optional: the realm, town, or lord the errand concerns")),
            Spec("reassure_companion",
               "The player sincerely addresses an unhappy companion's voiced grievance, softening their discontent a little."),
            Spec("recall_companion",
               "The player orders an away companion to abandon their errand and return; the companion is actually brought home, not merely told to."),
            Spec("follow_me",
               "A lord leading their own party agrees to have it escort the player's across the map for a while, keeping their own troops and command (never joining the player's party or clan)."),
            Spec("dismiss_escort",
               "An active escort begun by follow_me ends early, before its term runs out."),
            Spec("ride_with_me",
               "A lord or lady without a field party of their own agrees to ride inside the player's own party for a time, keeping their own clan (no marriage, no clan-join)."),
            Spec("part_ways",
               "A retainer riding in the player's party via ride_with_me leaves and returns to their own clan and business."),
            Spec("give_influence",
               "A lord lends the player some of their own clan's political weight at court, a costed favour gated on the lord's Trust in the player."),
            Spec("lend_troops",
               "An NPC leading their own party gives the player some of their own rank-and-file soldiers, a permanent reinforcement (never a loan that returns), gated on Trust."),
            Spec("give_troops",
               "The NPC, whose own party runs under-strength, accepts soldiers the player offers to reinforce their ranks."),
            Spec("spend_influence",
               "The NPC accepts influence the player spends from their own clan to back the NPC's clan at court."),
            Spec("sway_opinion",
               "The player's case genuinely moves the NPC's own regard for a named third party (not the player), for or against.",
               new GameActionParam("target", "the third party's name, exactly as known to the NPC"),
               new GameActionParam("stance", "against or for")),
            Spec("release_prisoner",
               "The player intercedes and the NPC frees a named third-party hero prisoner their OWN clan holds; the captive walks free (the player does not take custody).",
               new GameActionParam("target", "the captive's name, exactly as listed among the NPC's held prisoners")),
            Spec("buy_prisoner",
               "The NPC pays the player gold to purchase a named hero captive the player's own party holds, taking them into their own custody.",
               new GameActionParam("target", "the captive's name, exactly as listed among the player's held prisoners"),
               new GameActionParam("price", "the agreed denars, clamped around the game's own ransom valuation")),
            Spec("sell_prisoner",
               "The NPC hands the player a named hero captive their own clan holds, for the player's gold.",
               new GameActionParam("target", "the captive's name, exactly as listed among the NPC's held prisoners"),
               new GameActionParam("price", "the agreed denars, clamped around the game's own ransom valuation")),
            Spec("end_marriage",
               "The player persuades the NPC to BEGIN ending their OWN marriage (never the player's own spouse); a slow estrangement that plays out over days, not an instant act."),
            Spec("make_amends",
               "The player sincerely apologises, or earnestly offers in words, to mend a grievance the NPC KNOWINGLY holds against them; some grudges need a gift or a deed instead, never mere words."),
            Spec("pledge_against",
               "A lord, moved by genuine standing enmity, vows to move against a named rival: launches a real scheme (slander or sabotage) and records a tracked commitment. A political act only, never a declaration of war.",
               new GameActionParam("target", "the rival's name, never the NPC's own close kin")),
            Spec("accept_divorce",
               "The player consents to their own spouse's pending divorce demand (or ongoing estrangement); the marriage is dissolved by mutual accord, with a soft relation settling."),
            Spec("decline_divorce",
               "The player firmly and explicitly refuses their own spouse's divorce demand in this very exchange, deepening the wound between them."),
            Spec("end_own_marriage",
               "The player, in free text, chooses to end their OWN marriage immediately; mutual or repudiated is decided live from the spouse's own pending demand and relation, driving soft or harsh consequences."),
            Spec("witness_leaves",
               "The player asks one of THEIR OWN companion witnesses, named, to leave the conversation; only a witness marked as a player companion may leave this way (the NPC's own side is cleared through request_privacy instead).",
               new GameActionParam("name", "the departing companion's name, matched tolerantly against the present witnesses")),
            Spec("request_privacy",
               "The NPC accepts or refuses the player's request (button or free text) for a private audience; accepting clears every witness present. A prisoner-player's own request is always honoured regardless of the emitted result.",
               new GameActionParam("result", "accepted or refused")),
            Spec("retire",
               "In a companion retirement audience only, the player has granted this war-weary companion their leave, so the companion steps back from service (executed at the conversation's close, with the player's blessing). Never emitted for any other request, nor once the companion has agreed to stay on.")
         };
      }

      private static IReadOnlyCollection<string> BuildTypes()
      {
         var types = new List<string>();

         foreach (GameActionSpec spec in _all)
            types.Add(spec.Type);

         return types;
      }

      /// <summary>Builds one spec from a type, a description, and zero or more parameters (an array satisfies <see cref="IReadOnlyList{T}" /> directly).</summary>
      private static GameActionSpec Spec(string type, string description, params GameActionParam[] parameters)
         => new GameActionSpec(type, description, parameters);

      #endregion
   }
}
