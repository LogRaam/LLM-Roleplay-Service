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
      public GameActionSpec(string type, string description, IReadOnlyList<string> tells,
         IReadOnlyList<string> antiPatterns, IReadOnlyList<GameActionParam> parameters)
      {
         Type = type;
         Description = description;
         Tells = tells ?? new List<string>();
         AntiPatterns = antiPatterns ?? new List<string>();
         Parameters = parameters ?? new List<GameActionParam>();
      }

      /// <summary>The action's <c>type:</c> value, exactly as the bridge switch (or ChatViewModel) matches it.</summary>
      public string Type { get; }

      /// <summary>A one-line, factual description of what the deed IS, derived from the bridge's own executor.</summary>
      public string Description { get; }

      /// <summary>
      ///   Concept-level cues for how this deed READS in the NPC's prose (not lexical triggers): the meaning a
      ///   prompt teaches the model to recognise as this action genuinely happening in the reply. Both the Action
      ///   Interpreter and the 1:1 prose prompt draw from these, so the "when to emit" knowledge lives in one place.
      /// </summary>
      public IReadOnlyList<string> Tells { get; }

      /// <summary>
      ///   Concept-level false positives to withhold on (not lexical triggers): the cases that resemble this deed
      ///   but must NOT emit it, above all the recurring three, something merely narrated but not done, a future or
      ///   conditional promise rather than a completed act, and a neighbouring verb this one is confused with.
      /// </summary>
      public IReadOnlyList<string> AntiPatterns { get; }

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
      private static readonly List<GameActionSpec> _registered = new List<GameActionSpec>();
      private static readonly object _sync = new object();
      private static IReadOnlyList<GameActionSpec> _combinedAll = _all;
      private static IReadOnlyCollection<string> _combinedTypes = _types;

      /// <summary>
      ///   Every action spec the interpreter is taught, built-ins first (in their existing bridge-mirroring order),
      ///   then every externally <see cref="Register" />-ed spec, in registration order. A stable, cached snapshot:
      ///   rebuilt only when something registers, never mutated in place.
      /// </summary>
      public static IReadOnlyList<GameActionSpec> All => _combinedAll;

      /// <summary>
      ///   Every action's <see cref="GameActionSpec.Type" />, built-ins then registered externals, for parity checks
      ///   against the bridge's own authoritative set.
      /// </summary>
      public static IReadOnlyCollection<string> Types => _combinedTypes;

      /// <summary>The built-in specs only, exactly as authored in this file, excluding anything externally registered.</summary>
      public static IReadOnlyList<GameActionSpec> BuiltIn => _all;

      /// <summary>The built-in <see cref="GameActionSpec.Type" /> keys only, excluding anything externally registered.</summary>
      public static IReadOnlyCollection<string> BuiltInTypes => _types;

      /// <summary>
      ///   Registers one externally-defined action so it is appended to <see cref="All" />/<see cref="Types" /> and
      ///   taught to the interpreter alongside the built-ins, without a third-party mod needing to reach into this
      ///   catalog's internals by reflection. Returns false, as a no-op, when <paramref name="spec" /> is null, its
      ///   <see cref="GameActionSpec.Type" /> is null or empty, or that Type already exists among the built-ins or an
      ///   already-registered external (an exact, case-sensitive match, the same way the bridge switch matches a
      ///   type). Dedup matters: a duplicate Type would teach the LLM the same action twice under one name, and a
      ///   Type colliding with a built-in could silently shadow it with a third-party definition the bridge does not
      ///   actually dispatch that way.
      /// </summary>
      public static bool Register(GameActionSpec spec)
      {
         if (spec == null || string.IsNullOrEmpty(spec.Type))
            return false;

         lock (_sync)
         {
            foreach (GameActionSpec existing in _all)
               if (string.Equals(existing.Type, spec.Type, System.StringComparison.Ordinal))
                  return false;

            foreach (GameActionSpec existing in _registered)
               if (string.Equals(existing.Type, spec.Type, System.StringComparison.Ordinal))
                  return false;

            _registered.Add(spec);
            RebuildCombined();
            return true;
         }
      }

      /// <summary>
      ///   Test-isolation reset: empties every externally-registered action and restores <see cref="All" />/
      ///   <see cref="Types" /> to the built-in set alone. Never touches the built-ins themselves. Exists so
      ///   registration state (a static, process-wide list) does not leak between test runs or across a full reload.
      /// </summary>
      internal static void ClearRegistered()
      {
         lock (_sync)
         {
            _registered.Clear();
            RebuildCombined();
         }
      }

      #region private

      private static void RebuildCombined()
      {
         var combinedAll = new List<GameActionSpec>(_all.Count + _registered.Count);
         combinedAll.AddRange(_all);
         combinedAll.AddRange(_registered);
         _combinedAll = combinedAll;

         var combinedTypes = new List<string>(combinedAll.Count);
         foreach (GameActionSpec spec in combinedAll)
            combinedTypes.Add(spec.Type);
         _combinedTypes = combinedTypes;
      }

      private static IReadOnlyList<GameActionSpec> BuildAll()
      {
         return new List<GameActionSpec> {
            Spec("change_relation",
               "Shifts the NPC's own personal regard for the player by a signed amount (cooldown, cap, and captivity floor are re-applied by the gate before it lands).",
               tells: new[] {
                  "the NPC's own feelings toward the player perceptibly warm or cool in this reply, a general shift in regard"
               },
               antiPatterns: new[] {
                  "a specific, verifiable pledge (to pay, to keep peace, to fight alongside), which is swear_oath rather than a mere shift of feeling",
                  "the NPC's view of a NAMED THIRD PARTY moving, which is sway_opinion, not their own regard for the player",
                  "a warm reaction to the player's sincere apology for a grievance the NPC KNOWINGLY held, which is make_amends (change_relation may accompany it, never replace it)",
                  "a shift merely narrated as having happened earlier, or that might happen later, rather than felt now in this reply"
               },
               new GameActionParam("delta", "signed integer, the proposed relation shift")),
            Spec("give_gold",
               "The NPC hands the player a sum of denars from their own purse (clamped to what they actually hold).",
               tells: new[] {
                  "the NPC actually parts with a stated sum of their OWN coin to the player, the transfer completed here in this reply"
               },
               antiPatterns: new[] {
                  "the NPC only speaks OF wealth, prices, cost, or poverty, with no coin actually leaving their hands",
                  "a future or conditional payment (owed later, or promised in return for a deed) rather than a completed transfer this turn",
                  "the PLAYER giving or paying the NPC, which is the mirror verb take_gold"
               },
               new GameActionParam("amount", "whole number of denars")),
            Spec("take_gold",
               "The player pays, hands over, or is made to surrender a sum of denars to the NPC (clamped to the player's purse).",
               tells: new[] {
                  "the player's own coin genuinely leaves their purse to the NPC, the transfer completed here in this reply"
               },
               antiPatterns: new[] {
                  "the NPC only speaking OF a price, tribute, or debt owed, with no coin actually changing hands",
                  "a future or conditional payment (owed later, or demanded in return for a deed) rather than a completed transfer this turn",
                  "the NPC giving or paying the player, which is the mirror verb give_gold"
               },
               new GameActionParam("amount", "whole number of denars")),
            Spec("pay_blackmail",
               "The player settles, in full, a bastard-mother's outstanding demand for her silence; the bridge itself looks up and pays the exact owed sum.",
               tells: new[] {
                  "the player actually settles a bastard-mother's silence demand in full, the debt closed here in this reply"
               },
               antiPatterns: new[] {
                  "the demand merely being restated, threatened, or discussed, without the player actually paying it off",
                  "a promise to pay the demand eventually rather than the debt being settled now",
                  "an ordinary debt repaid to a merchant, lord, or any other NPC, or any payment not settling THIS bastard-mother's silence demand, which is take_gold, never pay_blackmail"
               }),
            Spec("join_party",
               "A free, recruitable wanderer agrees to take service as a companion in the player's own party for an agreed hiring price.",
               tells: new[] {
                  "a free wanderer explicitly agrees, in this reply, to take service in the player's party for a price"
               },
               antiPatterns: new[] {
                  "the wanderer merely considering, haggling over, or declining the offer, without actually agreeing to join",
                  "an NPC who already leads a party or holds a house agreeing to ride along instead, which is ride_with_me or follow_me",
                  "swearing into the player's CLAN itself rather than merely joining their party as a hired companion, which is join_clan",
                  "a settlement NOTABLE leaving a POST (with a successor, no hiring price) to follow the player, which is recruit_notable, never join_party"
               },
               new GameActionParam("price", "the agreed denars (optional; defaults to the vanilla asking price, clamped to 75-125% of it)")),
            Spec("join_as_mercenary",
               "The player's clan enlists as paid mercenary swords under this NPC's kingdom.",
               tells: new[] {
                  "the NPC's kingdom actually takes the player's clan on as paid mercenary swords in this reply"
               },
               antiPatterns: new[] {
                  "the kingdom merely discussing, weighing, or refusing the mercenary offer, with no contract actually struck",
                  "a promise to consider the contract later rather than it being struck now",
                  "a permanent oath of vassalage rather than a paid mercenary contract, which is join_as_vassal"
               }),
            Spec("end_mercenary",
               "The player's clan's current mercenary contract is dissolved (the mirror of join_as_mercenary).",
               tells: new[] {
                  "an existing mercenary contract is actually dissolved, ending in this reply"
               },
               antiPatterns: new[] {
                  "the NPC merely grumbling about or threatening to end the contract, without it actually being dissolved",
                  "a future intention to end the contract at some later date rather than dissolving it now",
                  "entering a NEW mercenary contract rather than dissolving the current one, which is the mirror verb join_as_mercenary"
               }),
            Spec("join_as_vassal",
               "This kingdom's ruler swears the player's clan into full vassalage under their banner (a permanent oath of fealty, distinct from a mercenary contract).",
               tells: new[] {
                  "the kingdom's own ruler swears the player's clan into full, permanent vassalage in this reply"
               },
               antiPatterns: new[] {
                  "the ruler merely discussing or weighing an offer of vassalage without actually swearing it",
                  "a paid mercenary contract rather than a permanent oath of fealty, which is join_as_mercenary",
                  "an NPC who is not their kingdom's own ruler purporting to grant vassalage"
               }),
            Spec("mediate_peace",
               "The player, as mediator, brokers peace between the NPC's own realm (the NPC must be its ruler) and a named enemy realm; the player's own faction need not be involved.",
               tells: new[] {
                  "the NPC, as ruler of their own realm, actually accepts peace terms the player brokered with a named enemy realm, in this reply"
               },
               antiPatterns: new[] {
                  "the NPC merely discussing or wishing for peace without actually accepting brokered terms",
                  "an NPC who is not their realm's own ruler purporting to broker or accept peace on its behalf",
                  "a promise to consider peace later rather than the accord being struck now"
               },
               new GameActionParam("target_faction", "the enemy realm the peace is brokered with")),
            Spec("join_clan",
               "A lord (not a clan leader) forsakes their own house and swears into the player's clan.",
               tells: new[] {
                  "a NOBLE LORD (one with their own house or fief, not a settlement figure) explicitly forsakes that house and swears into the player's clan in this reply"
               },
               antiPatterns: new[] {
                  "the lord merely weighing or discussing the idea without actually swearing in",
                  "a settlement NOTABLE (headman, gang leader, artisan) leaving their post to join, which is recruit_notable, never join_clan",
                  "a clan's own LEADER purporting to join (only a non-leader lord can), or a free wanderer merely joining as a hired companion, which is join_party",
                  "a future promise to join later rather than the oath being sworn now"
               }),
            Spec("scheme_assist",
               "The player throws in with the NPC's own secret scheme against a rival, advancing the plot and warming the plotter toward the player.",
               tells: new[] {
                  "the player commits, in this reply, to actually help advance the NPC's own secret scheme against a rival"
               },
               antiPatterns: new[] {
                  "the NPC merely describing or hinting at the scheme without the player actually throwing in",
                  "a vague future offer to help later rather than committing to it now",
                  "the NPC (a lord) vowing to move against a rival of their OWN, which is pledge_against, not the player joining a scheme",
                  "the NPC heeding a WARNING of a plot against themselves instead, which is the mirror verb scheme_heed"
               }),
            Spec("scheme_heed",
               "The NPC heeds the player's warning of a secret plot against them, exposing the scheme and warming toward the player.",
               tells: new[] {
                  "the NPC actually heeds the player's warning in this reply, exposing the plot laid against themselves"
               },
               antiPatterns: new[] {
                  "the NPC merely hearing the warning but dismissing or doubting it, without acting on it",
                  "a promise to look into the warning later rather than heeding it now",
                  "the player joining the NPC's OWN scheme against someone else instead, which is the mirror verb scheme_assist"
               }),
            Spec("marry",
               "The NPC and the player are wed in law (a real MarriageAction.Apply). A wanderer companion match is first promoted from Wanderer to Lord of the player's clan so the engine accepts the union.",
               tells: new[] {
                  "the NPC and the player are actually wed in law, the vows completed in this reply"
               },
               antiPatterns: new[] {
                  "a proposal, courtship, or discussion of marriage, without the wedding itself actually taking place",
                  "a lesser, uncommitted, or unofficial bond rather than a real legal marriage, which are take_as_consort or take_as_secret_lover",
                  "a future promise or intention to wed rather than the vows being completed now"
               }),
            Spec("take_as_consort",
               "The NPC and the player openly name a committed bond between them, a mod-tracked status short of legal marriage; the player may already be wed.",
               tells: new[] {
                  "the NPC and the player openly and explicitly name themselves as committed to one another, short of marriage, in this reply"
               },
               antiPatterns: new[] {
                  "mere flirting, courtship, or affection, without an OPEN, explicit naming of the committed bond",
                  "a real legal marriage rather than a status short of it, which is marry",
                  "a DISCREET, hidden bond rather than an openly-named one, which is take_as_secret_lover"
               }),
            Spec("take_as_secret_lover",
               "A faithful companion and the player welcome a discreet, hidden romantic bond; deliberately silent (no world-event, no chronicle) so the secrecy holds.",
               tells: new[] {
                  "a faithful companion and the player quietly welcome a discreet, hidden romantic bond in this reply"
               },
               antiPatterns: new[] {
                  "an OPENLY named bond rather than a deliberately hidden one, which is take_as_consort",
                  "mere flirting or attraction, without the two of them actually welcoming the secret bond",
                  "an NPC who is not a faithful companion of the player's own party"
               }),
            Spec("open_relationship",
               "The player's committed partner (spouse, or a consort/committed bond) agrees to reciprocal open terms, each free to love another.",
               tells: new[] {
                  "the player's own committed partner explicitly agrees to open, reciprocal terms in this reply"
               },
               antiPatterns: new[] {
                  "the partner merely discussing or considering the idea, without actually agreeing to it",
                  "revoking previously-agreed open terms rather than agreeing to them, which is the mirror verb close_relationship",
                  "an NPC who is not the player's own committed partner (spouse or consort)"
               }),
            Spec("close_relationship",
               "The player's committed partner revokes previously-agreed open terms; she will take no new lovers (an existing affair, if any, is untouched).",
               tells: new[] {
                  "the player's own committed partner explicitly revokes previously-agreed open terms in this reply"
               },
               antiPatterns: new[] {
                  "the partner merely expressing discomfort or jealousy, without actually revoking the open terms",
                  "AGREEING to open terms rather than revoking them, which is the mirror verb open_relationship",
                  "ending an existing affair itself, which the description says stays untouched here; that is end_affair"
               }),
            Spec("end_affair",
               "The NPC's current secret affair ends at the player's asking, when the NPC agrees; the lover link and every affair-tracking field are cleared.",
               tells: new[] {
                  "the NPC agrees, in this reply, to actually end their current secret affair at the player's asking"
               },
               antiPatterns: new[] {
                  "the NPC merely discussing, being confronted about, or refusing to end the affair",
                  "a promise to end it eventually rather than the affair link actually being cleared now",
                  "revoking open-relationship terms rather than ending a secret affair outright, which is close_relationship"
               }),
            Spec("give_item",
               "The player gives one item from their own party inventory to the NPC, who may equip it on the spot if it fits and suits them.",
               tells: new[] {
                  "the player actually hands over a named item from their own inventory to the NPC, the transfer completed here in this reply"
               },
               antiPatterns: new[] {
                  "the item merely being discussed, admired, or offered in words, without actually changing hands",
                  "the NPC DECLINING the offered item ('I cannot take such a blade from you', 'keep it'), which completes no transfer",
                  "a future promise to give the item later rather than the handover happening now",
                  "gold or a prisoner changing hands instead of a physical item, which are give_gold or give_prisoner/sell_prisoner"
               },
               new GameActionParam("item", "the item's name, matched case-insensitively against the player's roster")),
            Spec("give_prisoner",
               "The player hands over the exact captive an outstanding deliver-prisoner bargain named; the bridge resolves which held prisoner satisfies it and settles the bargain's reward.",
               tells: new[] {
                  "the player actually hands over the bargained-for captive in this reply, settling an outstanding deliver-prisoner bargain"
               },
               antiPatterns: new[] {
                  "the handover merely being discussed or promised, without the captive actually changing hands",
                  "a captive sold for a NEW price rather than fulfilling an already-struck deliver-prisoner bargain, which is sell_prisoner",
                  "releasing the captive to freedom rather than into custody, which are free_prisoner or release_prisoner"
               }),
            Spec("free_prisoner",
               "The player releases a captive they hold, honouring a struck bargain for their freedom (Fear/Respect may mark the release as fear-coerced rather than a mercy).",
               tells: new[] {
                  "the player actually releases their own held captive in this reply, honouring a struck bargain for their freedom"
               },
               antiPatterns: new[] {
                  "the release merely being discussed, promised, or bargained for, without actually happening",
                  "the NPC's OWN side freeing a third-party captive rather than the player releasing one they hold, which is release_prisoner",
                  "handing the captive into someone else's custody rather than setting them fully at liberty, which are give_prisoner or sell_prisoner"
               }),
            Spec("execute_prisoner",
               "The player kills, at their own hand, a hero prisoner they currently hold.",
               tells: new[] {
                  "the player actually kills, at their own hand, a hero prisoner they hold, in this reply"
               },
               antiPatterns: new[] {
                  "a mere threat, ultimatum, or discussion of execution, without the killing actually happening",
                  "the CAPTOR killing the player instead, which is the mirror verb execute_player",
                  "releasing or trading the prisoner rather than killing them, which are free_prisoner, give_prisoner, or sell_prisoner"
               }),
            Spec("execute_player",
               "The captor holding the player prisoner ends the player's life. Hardcore-only, MCM opt-in required, and irreversible: every guard is re-checked live before it lands.",
               tells: new[] {
                  "the captor actually ends the player's life in this reply, an irreversible act against their captive"
               },
               antiPatterns: new[] {
                  "a mere threat or intimidation of execution, without the killing actually happening",
                  "the PLAYER killing a prisoner they hold instead, which is the mirror verb execute_prisoner",
                  "ordinary harm or punishment that stops short of killing, which is harm_prisoner"
               }),
            Spec("turn_nemesis",
               "A tracked nemesis the player holds prisoner is spared, freed, sworn into the player's clan and party, and their vendetta closed for good.",
               tells: new[] {
                  "the player's tracked nemesis is spared, freed, and actually sworn into the player's own clan and party in this reply, the vendetta closed",
                  "the nemesis speaks or performs an OATH OF SERVICE in this reply ('my sword is yours', 'I will ride with you', kneeling in fealty): sparing or freeing him WITHOUT that oath is not a turn, however grateful he sounds"
               },
               antiPatterns: new[] {
                  "the nemesis merely being spared or spoken with, without the vendetta actually being closed and them being sworn in",
                  "the nemesis accepting mercy but REFUSING service ('do not think it buys my sword', 'our quarrel is not mended'), which completes nothing",
                  "a hero prisoner who is NOT the player's tracked nemesis switching sides instead, which is recruit_prisoner",
                  "killing or releasing the nemesis without recruiting them, which are execute_prisoner or free_prisoner"
               }),
            Spec("recruit_prisoner",
               "A captured hero prisoner the player holds is persuaded to switch sides: freed from captivity and sworn straight into the player's clan and party.",
               tells: new[] {
                  "a held hero prisoner is actually persuaded and sworn into the player's own clan and party in this reply",
                  "the prisoner ACCEPTS the offer in this reply ('I will serve', 'my oath to you', 'I am yours'): interest, temptation, or being persuaded-at WITHOUT accepting completes nothing"
               },
               antiPatterns: new[] {
                  "the prisoner merely being persuaded-at (offered, argued with), without actually switching sides",
                  "the prisoner DECLINING or being bound by a prior oath ('my oath to my own liege still binds me'), however tempting he finds the offer",
                  "the player's own tracked NEMESIS switching sides instead, which is the distinct verb turn_nemesis",
                  "a settlement NOTABLE joining the clan rather than a held hero prisoner, which is recruit_notable"
               }),
            Spec("recruit_notable",
               "A settlement notable (gang leader, headman, merchant, artisan, rural notable, or preacher) leaves their post and swears into the player's clan as a companion; their role passes to a successor in the same settlement, and a starting relation bonus is applied.",
               tells: new[] {
                  "a settlement NOTABLE (a headman, gang leader, artisan, merchant, or the like, NOT a noble lord) actually leaves their post, with a successor taking it up, and swears into the player's clan as a companion in this reply"
               },
               antiPatterns: new[] {
                  "the notable merely being courted or considering the offer, without actually leaving their post",
                  "the notable dangling a LATER departure ('perhaps when the harvest is in', 'not before then', 'one day I might'), still at their post today",
                  "a captured hero PRISONER switching sides rather than a free settlement notable, which is recruit_prisoner",
                  "a NOBLE LORD forsaking their own house to join the clan instead (no post, no successor), which is the distinct verb join_clan",
                  "a free wanderer with no post, hired into the party for a price, which is join_party, never recruit_notable"
               }),
            Spec("grant_blessing",
               "The NPC, as head of their clan, consents to the player marrying the named kin of their house (or the NPC themselves).",
               tells: new[] {
                  "the NPC, as head of their own clan, actually consents to the named marriage in this reply"
               },
               antiPatterns: new[] {
                  "the NPC merely discussing or weighing the match, without actually granting consent",
                  "an NPC who is not head of their own clan purporting to grant the blessing",
                  "the marriage itself being performed here rather than merely the consent being granted; the wedding itself is marry"
               },
               new GameActionParam("hero", "the name of the NPC's kin (or the NPC) the blessing covers")),
            Spec("arrange_marriage",
               "The player weds one of their own unwed kin to one of the NPC's house, a match in which the player is neither spouse.",
               tells: new[] {
                  "TWO OTHER PEOPLE wed in this reply, the player marrying off one of their OWN kin to the NPC's house, the player themselves NEITHER of the two spouses"
               },
               antiPatterns: new[] {
                  "the match merely being proposed or discussed, without the wedding actually taking place",
                  "the PLAYER being one of the two spouses (the player weds the NPC), which is marry, never arrange_marriage",
                  "consent for the match being granted without the wedding itself occurring, which is grant_blessing"
               },
               new GameActionParam("player_kin", "the player's own unwed kin to be wed"),
               new GameActionParam("target_kin", "the NPC's house's unwed kin (or the NPC) to be wed")),
            Spec("appoint_governor",
               "The player names the NPC, who is of the player's own clan, to govern one of the clan's fiefs that currently has no governor.",
               tells: new[] {
                  "the player actually names the NPC to govern a specific ungoverned fief of their own clan, in this reply"
               },
               antiPatterns: new[] {
                  "the appointment merely being discussed or considered, without actually being made",
                  "an NPC who is not of the player's own clan being appointed",
                  "granting the fief itself to a separate vassal house instead, which is the distinct verb grant_fief"
               },
               new GameActionParam("target_fief", "the town or castle the NPC will govern")),
            Spec("assign_party_role",
               "The player sets the NPC, a companion riding in their own party, to a duty within it.",
               tells: new[] {
                  "the player actually sets the NPC companion to a specific duty within their own party in this reply; record it even though the companion merely thanks the player or accepts, the duty set is the deed, not merely a change_relation"
               },
               antiPatterns: new[] {
                  "the assignment merely being discussed or suggested, without actually being made",
                  "sending the companion out on an external errand rather than assigning them an in-party duty, which is dispatch_mission",
                  "an NPC who is not currently riding in the player's own party being assigned a role"
               },
               new GameActionParam("target_role", "one of: scout, engineer, quartermaster, surgeon")),
            Spec("rejoin_party",
               "An away companion agrees to come back to the player's party, stepping down first from any post (e.g. a governorship) they hold elsewhere.",
               tells: new[] {
                  "an away companion actually agrees to come back and rejoin the player's party in this reply, stepping down from any outside post"
               },
               antiPatterns: new[] {
                  "the companion merely being asked or considering the return, without actually agreeing",
                  "a companion coming home from a dispatched ERRAND rather than stepping down from an outside post such as a governorship, which is recall_companion",
                  "a companion leaving to take up a post rather than stepping down from one to return"
               }),
            Spec("dispatch_mission",
               "The player sends the NPC, a companion in their own party, out on an errand; the game itself picks the destination, never the model.",
               tells: new[] {
                  "the player actually sends the NPC companion out on an errand in this reply"
               },
               antiPatterns: new[] {
                  "the errand merely being discussed or suggested, without the companion actually being sent out",
                  "assigning the companion an in-party duty rather than sending them out on an external errand, which is assign_party_role",
                  "the deprecated gather_news alias vocabulary; prefer recognising this deed under the current verb dispatch_mission"
               },
               new GameActionParam("target_mission", "one of: gathernews, spy, steal, barter, envoy"),
               new GameActionParam("about", "optional: the realm, town, or lord the errand concerns; omitted when left open")),
            Spec("grant_fief",
               "The player, as sovereign, grants one of their own crown fiefs to the NPC's house (a vassal of a separate clan in the player's kingdom).",
               tells: new[] {
                  "the player, as sovereign, actually grants a crown fief to the NPC's vassal house in this reply"
               },
               antiPatterns: new[] {
                  "the grant merely being discussed, requested, or promised, without actually being made",
                  "stripping a fief back from a vassal rather than granting one, which is the mirror verb revoke_fief",
                  "a player who is not sovereign purporting to grant crown land"
               },
               new GameActionParam("target_fief", "the town or castle granted")),
            Spec("revoke_fief",
               "The player, as sovereign, strips a fief back from the NPC's (separate) vassal house, returning it to the crown; plants a grave grudge.",
               tells: new[] {
                  "the player, as sovereign, actually strips a fief back from the NPC's vassal house in this reply",
                  "the NPC acknowledging that the player-sovereign has JUST stripped their fief ('taken back to the crown', 'stripped from my house', their steward already escorted from the gates): the revocation is the PLAYER's deed and counts the moment the reply treats it as done"
               },
               antiPatterns: new[] {
                  "the revocation merely being threatened or discussed, without actually being carried out",
                  "granting a fief rather than stripping one back, which is the mirror verb grant_fief",
                  "a player who is not sovereign purporting to revoke crown land"
               },
               new GameActionParam("target_fief", "the town or castle revoked")),
            Spec("expel_from_clan",
               "The player casts the NPC, a companion of their own clan, out of it entirely; the NPC becomes a Fugitive.",
               tells: new[] {
                  "the player actually casts the NPC companion out of their own clan in this reply, ending their membership; record it as expel_from_clan even as the NPC then departs and the exchange closes, the casting-out is the deed, not merely an end_conversation"
               },
               antiPatterns: new[] {
                  "the expulsion merely being threatened or discussed, without actually being carried out",
                  "an NPC who is not currently a member of the player's own clan being expelled",
                  "a companion merely leaving to pursue an errand or an outside post, rather than being cast out entirely"
               }),
            Spec("grant_stipend",
               "The player puts the NPC, a companion of their own clan, on a recurring daily wage, funded up front for a fixed tranche of days.",
               tells: new[] {
                  "the player actually puts the NPC companion on a recurring daily wage in this reply, funded up front; record it as grant_stipend even though the NPC thanks the player for it, the wage set is the deed, not merely a change_relation"
               },
               antiPatterns: new[] {
                  "the stipend merely being discussed or requested, without actually being granted",
                  "a one-time payment rather than a recurring, funded daily wage, which is give_gold instead",
                  "an NPC who is not a companion of the player's own clan receiving the stipend"
               },
               new GameActionParam("target_amount", "the daily denars requested, clamped to the policy's [50, 500] range")),
            Spec("swear_oath",
               "The NPC swears a verifiable, tracked oath to the player: to pay a sum, to keep the peace with a named faction, or to fight at their side within a deadline.",
               tells: new[] {
                  "the NPC binds themselves to the player with a pledge that NAMES a sum, a faction, a side, or a term in this reply: to pay a set sum, to keep peace with a named house, or to fight at the player's side. The vow counts NOW even though what is sworn lies in the future (see the vow rule)"
               },
               antiPatterns: new[] {
                  "a vague reassurance, hope, or good intention with nothing concrete and verifiable actually pledged",
                  "an oath explicitly DECLINED or deferred ('I swear no oath today', 'I will weigh it before any sword touches a shoulder'), however precisely the would-be oath is named",
                  "the NPC recalling a past oath, or someone else's oath, rather than swearing one now",
                  "a mere warming of feeling toward the player (that is change_relation), not a bound, trackable commitment"
               },
               new GameActionParam("oath_kind", "one of: pay_gold, keep_peace, protect"),
               new GameActionParam("target_amount", "the denars owed, only when oath_kind is pay_gold"),
               new GameActionParam("target_faction", "the house to keep peace with, only when oath_kind is keep_peace")),
            Spec("harm_prisoner",
               "The captor inflicts real physical harm on the player they hold captive, deducting HP (Hardcore-only, a live captive scene required, capped per scene and never lethal).",
               tells: new[] {
                  "the captor actually inflicts real physical harm on the captive player in this reply"
               },
               antiPatterns: new[] {
                  "a mere threat or intimidation of harm, without the injury actually being inflicted",
                  "harm severe enough to kill the player, which is the distinct, separately-gated verb execute_player",
                  "the player being merely frightened or humiliated in words, with no real physical injury actually dealt"
               },
               new GameActionParam("severity", "light/mild (default), moderate/heavy, or severe/grievous")),
            Spec("impregnation_risk",
               "Flags that a vaginal act was carried to completion this turn; the fertile female of the pairing has a small chance to conceive. Silent: never surfaces a chat outcome.",
               tells: new[] {
                  "a vaginal act between the pairing is actually carried to completion in this reply"
               },
               antiPatterns: new[] {
                  "the act merely being initiated, discussed, or interrupted, without being carried to completion",
                  "a non-vaginal act, which carries no conception risk and should not raise this flag",
                  "a future or hypothetical encounter rather than one completed in this very reply"
               }),
            Spec("gather_news",
               "DEPRECATED backward-compat alias of dispatch_mission (2026-08-05): the same errand dispatch under the old 'errand'/'about' vocabulary. No longer actively taught; prefer dispatch_mission.",
               tells: new[] {
                  "an errand is dispatched under this deprecated alias's old vocabulary, functionally identical to dispatch_mission"
               },
               antiPatterns: new[] {
                  "new emissions of an errand dispatch, which should prefer the current verb dispatch_mission instead of this deprecated alias",
                  "the errand merely being discussed without a companion actually being sent out",
                  "an in-party duty assignment rather than an external errand, which is assign_party_role"
               },
               new GameActionParam("errand", "optional: news/scout/steal/trade/envoy; blank defaults to gathernews"),
               new GameActionParam("about", "optional: the realm, town, or lord the errand concerns")),
            Spec("reassure_companion",
               "The player sincerely addresses an unhappy companion's voiced grievance, softening their discontent a little.",
               tells: new[] {
                  "the player sincerely and directly addresses the companion's own voiced grievance in this reply, softening it"
               },
               antiPatterns: new[] {
                  "a generic pleasantry or change of subject that does not actually address the companion's specific grievance",
                  "the grievance merely being acknowledged as existing, without the player actually working to soften it",
                  "a formal apology for a specific wrong the NPC knowingly holds against the player, which is the distinct verb make_amends"
               }),
            Spec("recall_companion",
               "The player orders an away companion to abandon their errand and return; the companion is actually brought home, not merely told to.",
               tells: new[] {
                  "an away companion is actually brought home in this reply, abandoning their dispatched errand"
               },
               antiPatterns: new[] {
                  "the player merely asking or wishing the companion would return, without the companion actually being brought home",
                  "a companion returning from an outside POST such as a governorship rather than from a dispatched errand, which is rejoin_party",
                  "the companion instead being sent out on a NEW errand, which is the mirror verb dispatch_mission"
               }),
            Spec("follow_me",
               "A lord leading their own party agrees to have it escort the player's across the map for a while, keeping their own troops and command (never joining the player's party or clan).",
               tells: new[] {
                  "a lord leading their own field party explicitly agrees, in this reply, to escort the player's party for a while, keeping their own command"
               },
               antiPatterns: new[] {
                  "the lord merely being asked or considering the escort, without actually agreeing",
                  "a lord WITHOUT a field party of their own riding inside the player's party instead, which is the distinct verb ride_with_me",
                  "the escort ending rather than beginning, which is dismiss_escort"
               }),
            Spec("dismiss_escort",
               "An active escort begun by follow_me ends early, before its term runs out.",
               tells: new[] {
                  "an active escort begun by follow_me actually ends early in this reply, before its term runs out"
               },
               antiPatterns: new[] {
                  "the escort merely being complained about or discussed, without actually being dismissed",
                  "beginning a NEW escort rather than ending an existing one, which is follow_me",
                  "a retainer leaving the player's own party instead, which is the distinct verb part_ways"
               }),
            Spec("ride_with_me",
               "A lord or lady without a field party of their own agrees to ride inside the player's own party for a time, keeping their own clan (no marriage, no clan-join).",
               tells: new[] {
                  "a lord or lady without a party of their own explicitly agrees, in this reply, to ride inside the player's own party for a time"
               },
               antiPatterns: new[] {
                  "the NPC merely being asked or considering the offer, without actually agreeing",
                  "the NPC DECLINING outright or putting the player off ('No, I think not', 'my place is elsewhere'), however warmly the invitation was made",
                  "a lord who leads their OWN field party escorting the player's instead, which is the distinct verb follow_me",
                  "swearing into the player's clan rather than merely riding along while keeping their own clan, which is join_clan"
               }),
            Spec("part_ways",
               "A retainer riding in the player's party via ride_with_me leaves and returns to their own clan and business.",
               tells: new[] {
                  "a retainer riding along via ride_with_me actually leaves the player's party and returns to their own clan in this reply; record it as part_ways even as the conversation then closes, the leaving is the deed, not merely an end_conversation"
               },
               antiPatterns: new[] {
                  "the retainer merely grumbling about wanting to leave, without actually departing",
                  "an escorting lord's party ending its escort instead, which is the distinct verb dismiss_escort",
                  "beginning to ride along rather than leaving, which is the mirror verb ride_with_me"
               }),
            Spec("give_influence",
               "A lord lends the player some of their own clan's political weight at court, a costed favour gated on the lord's Trust in the player.",
               tells: new[] {
                  "a lord actually lends the player some of their own clan's political influence in this reply"
               },
               antiPatterns: new[] {
                  "a future or conditional offer of influence ('perhaps', 'when the moment is right', 'not today', 'I will think on it') is NOT influence lent this turn",
                  "the lord merely discussing or weighing lending influence, without any actually changing hands now",
                  "the NPC accepting influence the PLAYER spends on their behalf instead, which is the mirror verb spend_influence",
                  "soldiers or gold changing hands rather than political weight, which are lend_troops/give_troops or give_gold"
               }),
            Spec("lend_troops",
               "An NPC leading their own party gives the player some of their own rank-and-file soldiers, a permanent reinforcement (never a loan that returns), gated on Trust.",
               tells: new[] {
                  "an NPC leading their own party actually hands the player some of their own rank-and-file soldiers in this reply, a permanent transfer"
               },
               antiPatterns: new[] {
                  "the NPC merely offering or discussing troops, without actually transferring them",
                  "the NPC ACCEPTING soldiers the player offers instead, which is the mirror verb give_troops",
                  "political influence or gold changing hands rather than soldiers, which are give_influence or give_gold"
               }),
            Spec("give_troops",
               "The NPC, whose own party runs under-strength, accepts soldiers the player offers to reinforce their ranks.",
               tells: new[] {
                  "the NPC, running under-strength, actually accepts soldiers the player offers in this reply, the soldiers changing ranks now"
               },
               antiPatterns: new[] {
                  "the offer merely being discussed, without the NPC actually accepting the soldiers",
                  "a DEFERRED or conditional acceptance ('perhaps I will ask you for them later', 'for now my ranks hold', 'when my numbers thin'), which completes nothing this turn",
                  "the NPC GIVING soldiers to the player instead, which is the mirror verb lend_troops",
                  "an NPC whose party is not under-strength accepting reinforcement it does not need"
               }),
            Spec("spend_influence",
               "The NPC accepts influence the player spends from their own clan to back the NPC's clan at court.",
               tells: new[] {
                  "the NPC actually accepts influence the player spends on their behalf in this reply, backing the NPC's clan at court"
               },
               antiPatterns: new[] {
                  "the backing merely being offered or discussed, without the NPC actually accepting it",
                  "a lord LENDING the player influence instead, which is the mirror verb give_influence",
                  "gold or troops changing hands rather than political influence, which are give_gold or lend_troops/give_troops"
               }),
            Spec("sway_opinion",
               "The player's case genuinely moves the NPC's own regard for a named third party (not the player), for or against.",
               tells: new[] {
                  "the NPC's own view of a NAMED third party actually shifts in this reply because of what the player argued, warming to them or turning against them"
               },
               antiPatterns: new[] {
                  "the NPC's regard for the PLAYER changing, which is change_relation; sway_opinion is only ever about a third party",
                  "the NPC restating an opinion they already held, with no movement the player's words actually caused",
                  "the player merely naming a third party while the NPC's stance toward them does not move"
               },
               new GameActionParam("target", "the third party's name, exactly as known to the NPC"),
               new GameActionParam("stance", "against or for")),
            Spec("release_prisoner",
               "The player intercedes and the NPC frees a named third-party hero prisoner their OWN clan holds; the captive walks free (the player does not take custody).",
               tells: new[] {
                  "the NPC grants or orders the freeing of a specific captive their own side holds, letting that named prisoner walk free at the player's urging, in this reply"
               },
               antiPatterns: new[] {
                  "the NPC merely weighing, discussing, or refusing a release without actually granting it",
                  "the NPC handing the captive INTO the player's custody (that is sell_prisoner or a deliver bargain), rather than setting them at liberty",
                  "freeing the PLAYER, or a captive the PLAYER holds, which are different verbs"
               },
               new GameActionParam("target", "the captive's name, exactly as listed among the NPC's held prisoners")),
            Spec("buy_prisoner",
               "The NPC pays the player gold to purchase a named hero captive the player's own party holds, taking them into their own custody.",
               tells: new[] {
                  "the NPC actually pays the player gold and takes a named captive the player holds into their own custody, in this reply"
               },
               antiPatterns: new[] {
                  "the purchase merely being offered or negotiated, without the captive actually changing custody",
                  "the NPC SELLING a captive of their own to the player instead, which is the mirror verb sell_prisoner",
                  "the captive merely being released to freedom rather than taken into the NPC's custody, which are free_prisoner or release_prisoner"
               },
               new GameActionParam("target", "the captive's name, exactly as listed among the player's held prisoners"),
               new GameActionParam("price", "the agreed denars, clamped around the game's own ransom valuation")),
            Spec("sell_prisoner",
               "The NPC hands the player a named hero captive their own clan holds, for the player's gold.",
               tells: new[] {
                  "the NPC actually hands the player a named captive their own clan holds, for the player's gold, in this reply"
               },
               antiPatterns: new[] {
                  "the sale merely being offered or negotiated, without the captive actually changing hands",
                  "the NPC BUYING a captive from the player instead, which is the mirror verb buy_prisoner",
                  "the captive being freed outright rather than sold into the player's custody, which is release_prisoner"
               },
               new GameActionParam("target", "the captive's name, exactly as listed among the NPC's held prisoners"),
               new GameActionParam("price", "the agreed denars, clamped around the game's own ransom valuation")),
            Spec("end_marriage",
               "The player persuades the NPC to BEGIN ending their OWN marriage (never the player's own spouse); a slow estrangement that plays out over days, not an instant act.",
               tells: new[] {
                  "the NPC agrees, in this reply, to begin ending their own marriage to someone other than the player, a slow estrangement starting now"
               },
               antiPatterns: new[] {
                  "the NPC merely voicing marital unhappiness, without actually agreeing to begin ending it",
                  "the PLAYER's own marriage ending instead, which is the distinct verb end_own_marriage",
                  "an instant, completed divorce rather than the start of a slow estrangement, which this verb only begins"
               }),
            Spec("make_amends",
               "The player sincerely apologises, or earnestly offers in words, to mend a grievance the NPC KNOWINGLY holds against them; some grudges need a gift or a deed instead, never mere words.",
               tells: new[] {
                  "the player sincerely apologises or earnestly offers, in words, to mend a specific grievance the NPC knowingly holds against them, in this reply"
               },
               antiPatterns: new[] {
                  "a generic pleasantry or comfort that does not address a specific, knowingly-held grievance",
                  "a grudge that needs a gift or deed rather than words alone, per the description, so a mere apology should not resolve it",
                  "a grievance settled by a PAYMENT or a deed rather than by the player's spoken apology or earnest words, which is take_gold (or no action at all), not make_amends",
                  "softening a companion's voiced unhappiness in general, which is the distinct verb reassure_companion"
               }),
            Spec("pledge_against",
               "A lord, moved by genuine standing enmity, vows to move against a named rival: launches a real scheme (slander or sabotage) and records a tracked commitment. A political act only, never a declaration of war.",
               tells: new[] {
                  "a lord, out of genuine enmity, actually vows to move against a named rival in this reply, launching a real scheme"
               },
               antiPatterns: new[] {
                  "the lord merely grumbling about the rival, without actually vowing a tracked scheme against them",
                  "a vague someday-resentment with no vow of action ('one day, perhaps, he will answer for it', 'today is not that day'), which launches no scheme",
                  "a declaration of war, which the description explicitly excludes; this is a political scheme only",
                  "the rival being the lord's own close kin, which the description rules out",
                  "the PLAYER joining the NPC's OWN existing scheme instead of the NPC vowing their own, which is scheme_assist"
               },
               new GameActionParam("target", "the rival's name, never the NPC's own close kin")),
            Spec("accept_divorce",
               "The player consents to their own spouse's pending divorce demand (or ongoing estrangement); the marriage is dissolved by mutual accord, with a soft relation settling.",
               tells: new[] {
                  "the player actually consents, in this reply, to their own spouse's pending divorce demand or estrangement"
               },
               antiPatterns: new[] {
                  "the demand merely being acknowledged, without the player actually consenting to it",
                  "firmly refusing the demand instead, which is the mirror verb decline_divorce",
                  "the player unilaterally ending the marriage themselves rather than consenting to the spouse's demand, which is end_own_marriage"
               }),
            Spec("decline_divorce",
               "The player firmly and explicitly refuses their own spouse's divorce demand in this very exchange, deepening the wound between them.",
               tells: new[] {
                  "the player firmly and explicitly refuses their own spouse's divorce demand in this very exchange"
               },
               antiPatterns: new[] {
                  "a hesitant or ambiguous response that does not amount to a firm, explicit refusal",
                  "consenting to the demand instead, which is the mirror verb accept_divorce",
                  "the player merely avoiding the topic rather than explicitly refusing it in this exchange"
               }),
            Spec("end_own_marriage",
               "The player, in free text, chooses to end their OWN marriage immediately; mutual or repudiated is decided live from the spouse's own pending demand and relation, driving soft or harsh consequences.",
               tells: new[] {
                  "the player explicitly chooses, in this reply, to end their own marriage immediately"
               },
               antiPatterns: new[] {
                  "the player merely voicing unhappiness or complaint, without actually choosing to end the marriage",
                  "the NPC's own marriage to someone else beginning to end instead, which is the distinct verb end_marriage",
                  "consenting to or refusing the SPOUSE's demand rather than the player unilaterally ending it, which are accept_divorce or decline_divorce"
               }),
            Spec("witness_leaves",
               "The player asks one of THEIR OWN companion witnesses, named, to leave the conversation; only a witness marked as a player companion may leave this way (the NPC's own side is cleared through request_privacy instead).",
               tells: new[] {
                  "the player asks a named companion witness of their own to leave the conversation, in this reply"
               },
               antiPatterns: new[] {
                  "the player merely wishing for privacy in general, without naming one of their own companion witnesses to leave",
                  "clearing the NPC's OWN side's witnesses instead, which is the distinct verb request_privacy",
                  "a witness leaving on their own initiative rather than at the player's asking"
               },
               new GameActionParam("name", "the departing companion's name, matched tolerantly against the present witnesses")),
            Spec("request_privacy",
               "The NPC accepts or refuses the player's request (button or free text) for a private audience; accepting clears every witness present. A prisoner-player's own request is always honoured regardless of the emitted result.",
               tells: new[] {
                  "the NPC explicitly accepts or refuses the player's request for a private audience in this reply"
               },
               antiPatterns: new[] {
                  "the request merely being made, without the NPC actually responding to it one way or the other",
                  "the player dismissing one of their OWN named companion witnesses instead, which is the distinct verb witness_leaves",
                  "privacy being assumed or implied rather than the NPC's accept or refuse actually being recorded"
               },
               new GameActionParam("result", "accepted or refused")),
            Spec("retire",
               "In a companion retirement audience only, the player has granted this war-weary companion their leave, so the companion steps back from service (executed at the conversation's close, with the player's blessing). Never emitted for any other request, nor once the companion has agreed to stay on.",
               tells: new[] {
                  "in a companion's own retirement audience, the player explicitly grants the war-weary companion their leave in this reply"
               },
               antiPatterns: new[] {
                  "this being emitted outside a dedicated companion retirement audience, which the description explicitly rules out",
                  "the companion having already agreed to stay on, after which this must never fire",
                  "the companion merely being expelled or dismissed rather than granted a blessed, voluntary leave, which is expel_from_clan"
               }),
            Spec("teach_skill",
               "The NPC, genuinely skilled themselves, actually instructs the player in a skill this very reply (a stance corrected, a grip adjusted, a trick of the trade shown and practiced); the bridge re-validates the NPC's own mastery and grants a bounded amount of real skill XP.",
               tells: new[] {
                  "the NPC actively INSTRUCTS the player in a skill in this reply, a hands-on lesson or demonstration the player is shown and practices right now, not merely spoken advice"
               },
               antiPatterns: new[] {
                  "a mere PROMISE of a future lesson ('I will teach you once we make camp', 'ask me again when there is time'), rather than a lesson genuinely given this turn",
                  "the PLAYER teaching the NPC instead, the wrong direction for this verb",
                  "a skill merely mentioned, admired, or discussed in passing, with no actual lesson or demonstration given",
                  "praise or encouragement about the player's existing skill, which is change_relation, not an act of teaching"
               },
               new GameActionParam("skill", "the skill taught, e.g. One Handed, Riding, Trade, Medicine"),
               new GameActionParam("amount", "advisory whole number of skill XP; the bridge decides the real amount from the teacher's own mastery"))
         };
      }

      private static IReadOnlyCollection<string> BuildTypes()
      {
         var types = new List<string>();

         foreach (GameActionSpec spec in _all)
            types.Add(spec.Type);

         return types;
      }

      /// <summary>
      ///   Builds one enriched spec: the factual <paramref name="description" />, the concept-level
      ///   <paramref name="tells" /> (how the deed reads in prose) and <paramref name="antiPatterns" /> (the
      ///   false positives to withhold on), and zero or more <paramref name="parameters" />. Both prompt builders
      ///   render from this single source, so the emission quality is authored once here.
      /// </summary>
      private static GameActionSpec Spec(string type, string description, string[] tells, string[] antiPatterns,
         params GameActionParam[] parameters)
         => new GameActionSpec(type, description, tells, antiPatterns, parameters);

      #endregion
   }
}
