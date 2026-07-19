// Code written by Gabriel Mailhot, 23/06/2026.

#region

using System.Collections.Generic;
using NpcMemoryService.Core.Prompts;

#endregion

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Captures the volatile, per-encounter state that the LLM needs to react
   ///   appropriately: where the conversation takes place, the diplomatic
   ///   situation, the player's standing with respect to the NPC, and how much
   ///   time has passed since they last met.
   ///   This is INJECTED into the volatile section of the system prompt so the
   ///   LLM can adapt its tone without us having to bake it into the persistent
   ///   <see cref="NpcProfile" />.
   /// </summary>
   public sealed class EncounterContext
   {
      /// <summary>
      ///   How aggressively to trim the system prompt for a small / short-context model (the "Compact prompt"
      ///   option). <see cref="LeanPromptLevel.Full" /> (default) sends everything; <see cref="LeanPromptLevel.Lean" />
      ///   drops the heavy flavour/context sections so the prompt fits a ~4k–8k context and the model still replies.
      /// </summary>
      public LeanPromptLevel LeanLevel { get; init; }

      /// <summary>An empty context (all fields Unknown). Use when no game state is available.</summary>
      public static EncounterContext Empty { get; } = new();

      /// <summary>
      ///   ONE generic, host-composed, prompt-ready block of extra action teachings — the extension
      ///   surface every future conversation-action verb rides on, instead of a dedicated
      ///   <c>EncounterContext</c> field and a hardcoded <c>PromptBuilder</c> section per verb. The
      ///   consumer (the game mod) gathers its own eligibility facts, decides which verbs currently
      ///   apply, and composes their full teaching text (including the <c>[ACTION]</c> format each
      ///   one expects) into this single string. Rendered verbatim, right after the core GAME ACTIONS
      ///   section, when non-blank. Null/empty means no extra verb is currently taught.
      ///   Per-conversation stable (does not change turn to turn for the same NPC), so it sits in the
      ///   prefix-cacheable region of the prompt. SKIPPED in <see cref="LeanPromptLevel.Lean" />: a
      ///   small model gets only the minimal action contract, not the extended verb set.
      /// </summary>
      public string? ExtraActionTeachings { get; init; }

      /// <summary>
      ///   The PLAYER's appearance in the THIRD person (Subject voice), composed by the host from derived body
      ///   facts plus any authored prose. Rendered under THE PLAYER so the NPC pictures the player consistently
      ///   and stops inventing their looks. Null/empty renders nothing.
      /// </summary>
      public string? PlayerAppearance { get; init; }

      /// <summary>
      ///   The SPEAKING NPC's OWN appearance in the SECOND person (Self voice), so it grounds its self-image and
      ///   never invents contradictory looks. Rendered near the NPC's identity. Null/empty renders nothing.
      /// </summary>
      public string? NpcSelfAppearance { get; init; }

      /// <summary>
      ///   True when a bandit captor has ALREADY dealt with the player (his profile carries memories of
      ///   them), so the "brigands do not know your name" perception no longer applies: he greets a captive
      ///   he knows of old, by name. False on a first encounter, where learning the name in-scene stays part
      ///   of the fiction.
      /// </summary>
      public bool CaptorKnowsPlayer { get; init; }

      /// <summary>
      ///   The GROUNDED quest menu: the tasks this giver can truly hand out RIGHT NOW, pre-validated
      ///   against the live world by the host (a real infested hideout, a real enemy lord, a real war),
      ///   one "- type (target_field: Name): cue" line each. When non-blank, the quest teaching offers
      ///   ONLY these, targets verbatim, so a registered quest succeeds by construction. Null/empty
      ///   falls back to the generic type catalogue (host supplied no scan). Per-conversation stable.
      /// </summary>
      public string? ViableQuestMenu { get; init; }

      /// <summary>
      ///   Who is acting this beat: the lead captor, another single member of the band taking
      ///   their turn, or the remaining members acting on the prisoner together at once.
      /// </summary>
      public CaptiveAggressorKind AggressorKind { get; init; } = CaptiveAggressorKind.Lead;

      /// <summary>
      ///   True when this NPC is one of the player's own companions, in the party, free of an errand and
      ///   skilled enough to be sent — so the gather_news offer (dispatch them to bring back word of the
      ///   realm) is taught. The game bridge re-validates every guard before actually dispatching.
      /// </summary>
      public bool CanGatherNews { get; init; }

      /// <summary>
      ///   The captor's intent for this encounter. Only meaningful when
      ///   <see cref="PlayerStatus" /> == <see cref="PlayerStatusVsNpc.Captive" />.
      ///   Controls the specific framing injected into the captive prompt section
      ///   and the scene-opening cue sent to the LLM as the first stimulus.
      /// </summary>
      public CaptiveSceneIntent CaptiveIntent { get; init; } = CaptiveSceneIntent.Interrogation;

      /// <summary>
      ///   True when the captor is a faceless bandit/pirate rather than a named lord.
      ///   Bandits live apart from the world of nobles: they do NOT know the player's
      ///   name, clan, or titles. The prompt suppresses the player's identity and lets
      ///   the bandit only SUSPECT high birth from outward signs (see
      ///   <see cref="PlayerLooksWealthy" /> / <see cref="PlayerLooksImportant" />).
      /// </summary>
      public bool CaptorIsBandit { get; init; }

      /// <summary>
      ///   True when the captor is a BRIGAND by station, whether a faceless bandit (<see cref="CaptorIsBandit" />)
      ///   or a named, persistent bandit boss (a nemesis) who is still a member of a bandit clan. Governs the
      ///   coarse VOICE overlay in a captive scene (crude, blunt, unlettered, no courtly eloquence or scheming),
      ///   so a nemesis does not read like the lord his profiled traits would otherwise make him. Distinct from
      ///   <see cref="CaptorIsBandit" />: a nemesis keeps his name and his memory of the player (he is not
      ///   faceless), so the "does not know who you are" perception stays gated on <see cref="CaptorIsBandit" />.
      /// </summary>
      public bool CaptorIsBrigand { get; init; }

      /// <summary>
      ///   The asking price (denars) for hiring this NPC into the player's party, when
      ///   they are a recruitable companion — computed game-side from the same model the
      ///   vanilla tavern hire uses. Null = not recruitable (a lord, a notable, already
      ///   in service, or the player's clan has no room): the recruitment section and
      ///   the join_party action are then not taught at all.
      /// </summary>
      public int? CompanionAskingPrice { get; init; }

      /// <summary>
      ///   Set when talking to one of the player's companions who feels strongly (warmly or bitterly)
      ///   about ANOTHER of the player's companions — a one-line directive inviting them to bring that
      ///   camp friendship or feud up with the player. Null when they hold no strong opinion of a peer.
      /// </summary>
      public string? CompanionCampNote { get; init; }

      /// <summary>
      ///   Set when talking to one of the player's OWN companions who has grown unhappy in their service:
      ///   a ready directive for them to voice their discontent (and, at the worst, that they mean to
      ///   leave). Null when the companion is content or this is not one of the player's companions.
      /// </summary>
      public string? CompanionMoodNote { get; init; }

      /// <summary>
      ///   Why this companion asked the player for a private audience (the popup's "Let us speak of it" opened a
      ///   real conversation). <see cref="CompanionAudienceReason.None" /> for an ordinary talk. Drives the
      ///   companion's opening turn and, for <see cref="CompanionAudienceReason.Retirement" />, the in-chat surface
      ///   that lets the player grant their leave (the <c>retire</c> action) or persuade them to stay.
      /// </summary>
      public CompanionAudienceReason CompanionAudience { get; init; }

      /// <summary>
      ///   Only meaningful with <see cref="CompanionAudience" /> = Retirement: true when this companion is LANDED
      ///   (a governor or party-leader), so their retirement is to STEP BACK from the field and tend their
      ///   holdings while remaining in the clan; false when landless, so it is to leave the player's service.
      /// </summary>
      public bool AudienceRetirementIsLanded { get; init; }

      /// <summary>
      ///   Set on the first conversation after this companion has RETURNED from a news errand: a ready
      ///   directive + the news they gathered, for them to deliver in their own voice. Consumed once (the
      ///   host clears it after this build), so it colours only the homecoming exchange. Null otherwise.
      /// </summary>
      public string? CompanionNewsReport { get; init; }

      /// <summary>
      ///   True when THIS NPC is one of the player's own companions currently AWAY on an errand (the player has
      ///   ridden out and met them on the road / in a town). It lets the LLM accept an order to abandon the
      ///   errand and come home — emitting the recall_companion action. The game bridge then actually brings
      ///   them back: a verified deed, never a hollow "yes, I'll return" that the engine ignores.
      /// </summary>
      public bool CompanionOnErrand { get; init; }

      /// <summary>
      ///   Which party, if either, could conceive from a completed vaginal act this scene — drives the
      ///   impregnation_risk teaching OUTSIDE captivity (consensual intimacy), for both a female player and a
      ///   female NPC. None when the pairing is same-sex or neither is a fertile female. The captive path
      ///   teaches conception separately (it is always the captive female player who conceives).
      /// </summary>
      public ConceptionRisk ConceptionRisk { get; init; }

      /// <summary>
      ///   True when the NPC and the player share a deep enough romantic bond (≥ Intimate,
      ///   reputation ≥ 60) that they could form a consort bond — a committed partnership
      ///   that is real but not recognised by Calradian law. Available whether or not the
      ///   player is already wed. When true, the prompt teaches the <c>take_as_consort</c>
      ///   action alongside (or instead of) the legal-marriage path.
      /// </summary>
      public bool ConsortEligible { get; init; }

      /// <summary>
      ///   True when this NPC is one of the player's own companions, bonded deeply enough through shared service
      ///   that they could become the player's discreet SECRET LOVER (an intimate bond kept hidden to avoid
      ///   jealousy), via the <c>take_as_secret_lover</c> action. Distinct from <see cref="ConsortEligible" />
      ///   (an open, committed bond). Adult-gated in the prompt.
      /// </summary>
      public bool SecretLoverEligible { get; init; }

      /// <summary>
      ///   Ready-to-inject hint about heroes the player mentioned in their last message.
      ///   Null when no hero names were detected. Built by the game-side resolver so the
      ///   NPC can accurately answer questions about third parties — friends, enemies, or
      ///   strangers — and ask for clarification when several people share a name.
      /// </summary>
      public string? ContextualNames { get; init; }

      /// <summary>
      ///   Where this conversation takes place and who holds it — e.g. "You are in Pravend, a town
      ///   held by Derthert's clan; it is not your own holding, you are here as a visitor." Stops
      ///   an NPC from claiming another lord's hall as their own (tester report). Null when the
      ///   conversation is not in a settlement, or ownership is unknown.
      /// </summary>
      public string? CurrentLocationNote { get; init; }

      /// <summary>
      ///   Ready-to-inject geography grounding, built game-side from the engine's own map data: the
      ///   encyclopedia blurb of the settlement the NPC stands in, plus compact facts (owner, culture,
      ///   bearing) for any place named in the conversation, so the NPC never invents where places sit
      ///   relative to one another. Null when there is nothing concrete to ground.
      /// </summary>
      public string? GeographyNote { get; init; }

      /// <summary>True when this conversation happens aboard a ship on open water (player party at sea).</summary>
      public bool AtSea { get; init; }

      /// <summary>
      ///   Ready-to-inject "first impression" guidance for a barely-known NPC: how their traits and the player's
      ///   standing colour their OPENING TONE (warm, reserved, arrogant, suspicious, demeaning), built game-side.
      ///   It steers tone only, never the relation numbers (deeds still govern regard). Null for an already-known
      ///   NPC or an ordinary neutral opening.
      /// </summary>
      public string? OpeningDisposition { get; init; }

      public int DaysSinceLastMeeting { get; init; } = -1;

      /// <summary>
      ///   A prompt-ready note of how long it has been since the NPC last saw the player (host-resolved via
      ///   <c>MeetingGapDescriber</c>), or null when the gap is too short or unknown to be worth remarking on.
      ///   Surfaced only for a genuinely long absence, with restraint guidance, so NPCs stop opening every
      ///   conversation with the elapsed time (a short gap is unremarkable when the player travels the map fast).
      /// </summary>
      public string? MeetingGapNote { get; init; }

      /// <summary>
      ///   A present-tense line telling the NPC how they are at the player's side right now (riding in the
      ///   party, marching in the army, or sharing a town), host-resolved from the live presence tier. Null when
      ///   the NPC is away. Rendered plainly, without the "time apart" restraint the gap note carries: a shared
      ///   road is a bond to lean into, not a lapse to excuse. It is why a companion travelling with the player
      ///   no longer opens as though abandoned for a season between two ports.
      /// </summary>
      public string? PresenceNote { get; init; }

      /// <summary>
      ///   A prompt-ready list of the LORD captives the player currently holds (name + faction),
      ///   or null/empty when they hold none. Lets a lord demand a captive as the price of a favor:
      ///   a match the player already holds is an immediate hand-over, one they do not is a
      ///   capture-and-deliver task. Drives the deliver_prisoner / give_prisoner teaching.
      /// </summary>
      public string? HeldLordPrisoners { get; init; }

      /// <summary>
      ///   Set on the FIRST turn of a conversation the NPC initiated by riding out to intercept the
      ///   player: a directive to open on that footing (a territorial challenge, or a reaction to
      ///   fresh news) rather than waiting on the player. Null in an ordinary conversation. Consumed
      ///   by the host so it only colours the opening turn.
      /// </summary>
      public string? InterceptionReason { get; init; }

      /// <summary>
      ///   A one-line FEEDBACK note for the model when its PREVIOUS reply emitted a [QUEST] or [ACTION] block
      ///   the host could not register (unknown / ungroundable target, unparseable). Without it, only the player
      ///   was ever told, so a weak model kept referencing a task the game never recorded. The host sets this on
      ///   the turn AFTER the refusal and consumes it, so it nudges once. Null in an ordinary turn.
      /// </summary>
      public string? LlmFormatFeedbackNote { get; init; }

      /// <summary>
      ///   When set, this conversation is a rival suitor's COURTSHIP-DUEL challenge, and this holds the courted
      ///   hero's name. The speaking NPC (the rival) opens by declaring his love for her, his knowledge of the
      ///   player's interest, and his demand to settle it with steel. Null in an ordinary conversation.
      /// </summary>
      public string? CourtshipRivalLadyName { get; init; }

      /// <summary>
      ///   Negotiation Phase 3 (adult-gated): true when this NPC is the exploiter archetype who may offer
      ///   the female player a favour in exchange for intimacy (a man of low scruple, unrelated). Off by
      ///   default; the prompt section double-checks the adult tier.
      /// </summary>
      public bool IntimacyBargainEligible { get; init; }

      /// <summary>
      ///   Phase 3: true when this NPC already HOLDS such leverage over the player from a past
      ///   transaction — he remembers, and may press for more on the same terms.
      /// </summary>
      public bool IntimacyLeverageActive { get; init; }

      /// <summary>
      ///   Set when this NPC is the mother of a still-hidden bastard of the player's, to one of
      ///   "mercenary" / "longing" / "resentful" / "pragmatic" — her disposition toward him. The prompt
      ///   teaches her to raise the child in that register (extort, long, reproach, or ask for its keep).
      ///   Null when she is not such a mother. Adult-gated by the prompt section.
      /// </summary>
      public string? BastardMotherTone { get; init; }

      /// <summary>The gold a mercenary bastard-mother would demand for her silence (0 otherwise) — for the prompt.</summary>
      public int BastardBlackmailDemand { get; init; }

      /// <summary>
      ///   True when the player is the NPC's captive AND the witnesses present are
      ///   active participants rather than bystanders (Sprint 17 collective captive scene).
      ///   Causes the prompt to instruct the NPC to involve the witnesses explicitly.
      /// </summary>
      public bool IsCollectiveCaptiveScene { get; init; }

      /// <summary>
      ///   When set, the captive scene's VICTIM is this named companion of the player (held alongside them), not
      ///   the player themselves: the captor torments the companion to break the watching, bound player. Null for
      ///   the ordinary player-as-victim scene. Drives <c>AppendCaptiveCompanionRules</c>.
      /// </summary>
      public string? CaptiveVictimName { get; init; }

      /// <summary>
      ///   Only meaningful with <see cref="CaptiveVictimName" /> set: true when the captor is forcing the PLAYER to
      ///   harm their own companion (coerced perpetrator), false when the player is a restrained witness to the
      ///   captor's men doing it. Either way, refusal escalates the cruelty rather than ending the scene.
      /// </summary>
      public bool PlayerCoercedAgainstCompanion { get; init; }

      /// <summary>
      ///   Only meaningful with <see cref="CaptiveVictimName" /> set: true when the captive companion is a woman,
      ///   false when a man. Lets the prompt refer to the victim with the correct pronouns throughout, instead of
      ///   the model defaulting them to the player's gender (a reported bug — the victim's sex was confused with
      ///   the player's).
      /// </summary>
      public bool CaptiveVictimIsFemale { get; init; }

      /// <summary>
      ///   True when the PLAYER is the captor and the NPC is their prisoner in a deliberate captor scene (the
      ///   inverse of the captive scene). The player directs; the NPC reacts as a captive of their own nature.
      ///   Drives <c>AppendPlayerCaptorSceneRules</c>; the player's intent is carried by <see cref="CaptiveIntent" />.
      /// </summary>
      public bool IsCaptorScene { get; init; }

      /// <summary>
      ///   Stance mechanical behaviour-gating (increment 1, Fear): true when the held captive is frightened
      ///   enough of the player, per the consumer's coercion gate, that a freedom bargain should ask for
      ///   notably harsher terms than pride alone would allow. Only meaningful alongside
      ///   <see cref="PlayerStatusVsNpc.NpcIsCaptive" />; the consumer computes this from the SAME pure
      ///   policy the game bridge re-checks before actually releasing the captive, so the prompt's ask and
      ///   the bridge's law never disagree. This is advice only: the bridge still re-validates everything
      ///   and plants the hidden grudge on an actual yield. Default false.
      /// </summary>
      public bool CaptiveFearCoerced { get; init; }

      /// <summary>
      ///   Captivity release (2026-07-05): true when the captor could be TALKED into freeing the captive
      ///   player (a disposed, non-vendetta captor, per <c>CaptorReleasePolicy</c>). Teaches the captor the
      ///   release_player action; the bridge re-validates and makes the freeing real. Default false.
      /// </summary>
      public bool CaptorAllowsPersuasionRelease { get; init; }

      /// <summary>
      ///   Captivity release (2026-07-05): true when the captor would accept a RANSOM in gold for the
      ///   player's freedom (any non-vendetta captor). Reserved for the ransom path; the gold handling is a
      ///   follow-up, so the current increment teaches persuasion release only. Default false.
      /// </summary>
      public bool CaptorAllowsRansomRelease { get; init; }

      /// <summary>
      ///   Native/vanilla quests (from the game's own quest journal) that THIS NPC gave the player and are
      ///   still open, prebuilt into a prompt note by the host (the SDK cannot read the engine quest manager).
      ///   Stops the NPC acting baffled about a task they themselves set outside CR dialogue. Null when none.
      /// </summary>
      public string NativeQuestNote { get; init; }

      /// <summary>
      ///   Nemesis pillar (2026-07-05): set when THIS captor is a nemesis who has recaptured the player after
      ///   a prior escape, so the prompt has him acknowledge it in character ("you slipped me once, not
      ///   again") and play colder/more careful. Null otherwise.
      /// </summary>
      public string NemesisRecaptureNote { get; init; }

      /// <summary>
      ///   True on the final beat of a captive scene continuation (mirrors
      ///   <see cref="IsLastWitnessExchange" /> for the 15C loop). When set, the prompt
      ///   instructs the NPC to bring the scene to a definitive conclusion this turn —
      ///   resolve the act and dismiss the prisoner with end_conversation — rather than
      ///   continuing to escalate.
      /// </summary>
      public bool IsFinalSceneBeat { get; init; }

      /// <summary>
      ///   True on the final witness-exchange turn before control returns to the player
      ///   (Sprint 15C, cap enforcement). The prompt instructs the NPC not to pose a
      ///   question to another NPC — they may close the beat or redirect to the player.
      /// </summary>
      public bool IsLastWitnessExchange { get; init; }

      /// <summary>
      ///   True on a turn fired by the round-table group mode: the floor is open to everyone present, so the
      ///   NPC should speak at more length than a normal reply and may address the other people present by name,
      ///   not only the player. Set by the game-side round-table orchestrator; false in ordinary turns.
      /// </summary>
      public bool IsRoundTableTurn { get; init; }

      /// <summary>
      ///   True when this turn is an automatic NPC response to a witness who just
      ///   reacted (Sprint 15C). The final user message in the session is a witness
      ///   statement ([Name]: …), not a player message. When set, the prompt instructs
      ///   the NPC to address the witness rather than the player.
      /// </summary>
      public bool IsWitnessExchangeTurn { get; init; }

      /// <summary>
      ///   Set ONLY when the player's last message asks where to find a companion to recruit,
      ///   so the NPC answers with REAL wanderers instead of inventing one. A bulleted list of
      ///   actual hireable wanderers and where they are (host-gathered) when any are known; an
      ///   empty string when the player asked but the NPC truly knows of none about (the prompt
      ///   then sends them to the taverns); null when the player did not ask, so the section is
      ///   skipped entirely and costs no tokens.
      /// </summary>
      public string? KnownWanderers { get; init; }

      /// <summary>
      ///   True when the game confirms this lord could be persuaded to leave their own house and
      ///   join the player's clan — a genuinely deep bond, the player's house outranking theirs,
      ///   and the lord vulnerable (landless, their clan broken, or estranged from their liege).
      ///   When true, the prompt teaches the <c>join_clan</c> action; the bridge re-checks every
      ///   gate before any defection is sealed. Only ever set for lords, never wanderers.
      /// </summary>
      public bool LordRecruitEligible { get; init; }

      /// <summary>
      ///   True when the family's formal blessing has already been granted for a marriage
      ///   between the player and this NPC (A.2 was completed). Used to inform the prompt
      ///   so the NPC can reference the blessing (or note its absence) in their dialogue.
      ///   Only meaningful when <see cref="LoveMatchEligible" /> is true.
      /// </summary>
      public bool LoveMatchBlessed { get; init; }

      /// <summary>
      ///   True when the NPC THEMSELVES is a viable marriage candidate for the player:
      ///   player is single, NPC is of the right sex, adult, within age gap, unmarried,
      ///   from a different clan, AND the personal relation is deep enough (≥50).
      ///   When true, <see cref="AppendLoveMatchProposal" /> teaches the <c>marry</c> action.
      /// </summary>
      public bool LoveMatchEligible { get; init; }

      /// <summary>
      ///   True when THIS NPC's own spouse IS the player themselves. Distinguishes the "married to
      ///   the player" case from the ordinary "married to some third party" case in the RELATIONSHIP
      ///   STATUS &amp; CONSENT block: with a third-party spouse, intimacy with the player is an act of
      ///   infidelity gated behind deep trust; when the spouse IS the player, intimacy is marital and
      ///   natural, and none of the stranger-resistance thresholds or infidelity framing apply. Fixes
      ///   a player report: after <c>cr.marry</c> the NPC still treated the player as a third party
      ///   distinct from their own spouse.
      /// </summary>
      public bool NpcSpouseIsPlayer { get; init; }

      /// <summary>
      ///   Ready-to-inject block describing a marriage the player could seek from THIS NPC's
      ///   house: the unmarried, suitable kin of the NPC's clan (and the NPC themselves when
      ///   eligible), built game-side so the NPC names only real, marriageable people. Null
      ///   when a marriage request does not apply — the NPC is not their clan's head, has no
      ///   eligible kin, the player is already wed, or this is the player's own clan. When
      ///   present, the prompt teaches the NPC to grant or withhold the family's blessing.
      /// </summary>
      public string? MarriageProspects { get; init; }

      /// <summary>
      ///   Name of the kingdom the NPC belongs to when the player is free (not already
      ///   sworn to any kingdom) and the NPC is a lord who could extend a mercenary
      ///   contract on the kingdom's behalf. Null when the player is already in a kingdom,
      ///   the NPC has no kingdom, or the NPC is not a lord. When non-null, the prompt
      ///   teaches the NPC the join_as_mercenary action.
      /// </summary>
      public string? MercenaryOfferKingdom { get; init; }

      /// <summary>Display name of the faction holding this NPC captive, for the rescue bargain. Null when free.</summary>
      public string? NpcCaptorName { get; init; }

      /// <summary>
      ///   What the NPC is currently doing on the campaign map (laying siege, marching
      ///   with an army toward an objective, traveling to a settlement). Null when it
      ///   cannot be read with confidence — the prompt then tells the NPC not to invent
      ///   a destination rather than feeding it a guess it would treat as fact.
      /// </summary>
      public string? NpcCurrentActivity { get; init; }

      /// <summary>
      ///   True when this NPC is a captive of someone OTHER than the player (a lord held in an enemy
      ///   dungeon the player is visiting). Lets the captive bargain to be broken out — a rescue deed,
      ///   verified when the player actually frees them. Distinct from PlayerStatus.NpcIsCaptive (which
      ///   is the player's OWN captive, handled by the immediate free_prisoner bargain).
      /// </summary>
      public bool NpcIsPrisonerOfAnother { get; init; }

      /// <summary>
      ///   The player's rank in the world-wide tournament leaderboard (1 = champion,
      ///   0 = never won a tournament / not on the leaderboard). A rank ≤ 10 means the
      ///   player has a genuine reputation as a tournament fighter — the kind that turns
      ///   heads for reasons that have nothing to do with clan prestige.
      /// </summary>
      public int PlayerArenaRank { get; init; } = 0;

      /// <summary>Total tournament victories for the player. Accompanies <see cref="PlayerArenaRank" />.</summary>
      public int PlayerArenaWins { get; init; } = 0;

      /// <summary>
      ///   The player's clan tier (0–6). Tier 0 means no clan or a landless household;
      ///   tier 3+ represents genuine nobility with a fief. Used to signal to NPCs
      ///   whether the player is a credible prospect for an official match (marriage).
      /// </summary>
      public int PlayerClanTier { get; init; } = 0;

      /// <summary>
      ///   The total troop count in the player's own party right now — or of the whole army when the
      ///   player leads one (companions and hero party members are counted separately by the game; this
      ///   is the rank-and-file roster). 0 = not provided (the player is a captive, or no main party).
      ///   Fed to ANY face-to-face interlocutor: the force at the player's back is a visible fact, and
      ///   the prompt renders it as a coarse qualitative band (see PromptBuilder), never the exact
      ///   figure, so no metagame number leaks. Fixes a reported immersion break: NPCs taunting the
      ///   player for "approaching alone" while an army stood behind them.
      /// </summary>
      public int PlayerPartyTroopCount { get; init; } = 0;

      /// <summary>
      ///   Pre-formatted "what is said of you" block — deeds of the PLAYER's that word has carried to
      ///   this NPC, attributed to the very person they are speaking with, each tagged with how it lands
      ///   on them ("(this cut against you or your own)" / "(this served your side)"). Null when none
      ///   have reached them. The influence loop: the world reflects the player's own acts back at them.
      ///   Distinct from <see cref="WorldRumorsBlock" />, which is third-party news the player is not in.
      /// </summary>
      public string? PlayerDeedsHeard { get; init; }

      /// <summary>
      ///   Name of the player's father, or null if unknown. Injected so an NPC that
      ///   refers to the player's parentage uses the real name instead of inventing one.
      ///   A "(the late …)" form is built game-side when the parent is deceased.
      /// </summary>
      public string? PlayerFatherName { get; init; }

      /// <summary>
      ///   True when the player carries enough political weight (high clan standing /
      ///   influence) that word of someone important may have reached even the wilds —
      ///   so a bandit might suspect a valuable, ransomable noble without knowing who.
      /// </summary>
      public bool PlayerLooksImportant { get; init; }

      /// <summary>
      ///   True when the player's gear is rich enough that a captor could guess they are
      ///   no common traveler (fine armor, a costly mount, a noble's weapons). Only used
      ///   to shape a bandit captor's suspicion; never reveals the actual name.
      /// </summary>
      public bool PlayerLooksWealthy { get; init; }

      /// <summary>Name of the player's mother, or null if unknown. See <see cref="PlayerFatherName" />.</summary>
      public string? PlayerMotherName { get; init; }

      /// <summary>
      ///   DEPRECATED — intentionally left unfed (always null). It once told a recruitable
      ///   NPC the player's exact gold so it would not seal an unaffordable hire, but an NPC
      ///   cannot see the player's liquid coin and naming the sum to the denar broke
      ///   immersion. Affordability is now enforced by the game bridge at execution time, not
      ///   foreseen in the prompt. Kept only to preserve the model's shape; do not re-feed it.
      /// </summary>
      public int? PlayerPurseGold { get; init; }

      /// <summary>
      ///   Name of the player's current spouse, or null if the player is single or widowed.
      ///   Used to inject the player's marital status into the NPC's consent section so the
      ///   NPC can react according to their own character — refusing to enable infidelity,
      ///   being indifferent, or finding the forbidden element appealing.
      /// </summary>
      public string? PlayerSpouseName { get; init; }

      public PlayerStatusVsNpc PlayerStatus { get; init; } = PlayerStatusVsNpc.Unknown;

      /// <summary>
      ///   The realm that binds the two of them, when one of them RULES it: the kingdom the player has sworn
      ///   to (<see cref="PlayerStatusVsNpc.Vassal" />) or the one the player rules and this NPC serves
      ///   (<see cref="PlayerStatusVsNpc.Liege" />). Null when unknown or when neither applies, in which case
      ///   the feudal note falls back to its bare form. Grounds the conclusion so a wrong one is visible.
      /// </summary>
      public string? SharedRealmName { get; init; }

      /// <summary>
      ///   A pre-computed prose note describing the power differential between the player's
      ///   realm and this NPC's position — fief counts, war state, and who holds the
      ///   stronger hand. Null when the balance is roughly even, the context cannot be
      ///   determined, or neither party belongs to a kingdom. When non-null, the prompt
      ///   teaches the NPC to factor this imbalance into how they speak: not servility,
      ///   but calibrated realism. Relation is warmth; power is the fear/respect axis.
      /// </summary>
      public string? PowerBalanceNote { get; init; }

      /// <summary>
      ///   True when the player has requested a private audience this turn.
      ///   Injected once — cleared after the NPC responds (accepted or refused).
      /// </summary>
      public bool PrivacyRequested { get; init; }

      /// <summary>
      ///   True when the player just resisted, negotiated, or otherwise intervened: the captor
      ///   should REACT to it within the current beat rather than ignore it, but the scene's
      ///   structure still holds.
      /// </summary>
      public bool ReactToPlayerIntervention { get; init; }

      /// <summary>
      ///   The single greatest realm-level happening the NPC has heard of right now — a war declared, a
      ///   peace struck — phrased as the talk of the whole realm. CR's light take on "kingdom statements":
      ///   the big news everyone is abuzz with, distinct from the personal/local <see cref="WorldRumorsBlock" />.
      ///   Null when no such realm event has reached them.
      /// </summary>
      public string? RealmNewsLine { get; init; }

      public SceneType Scene { get; init; } = SceneType.Unknown;

      /// <summary>
      ///   The current dramatic stage of a captive scene, advanced by the host's scene director
      ///   and injected as a single per-turn directive (Intro → Initiate → Intensify → Climax →
      ///   Conclude). Steering the STRUCTURE from outside the model — one named beat at a time,
      ///   with a reminder of what is already done — stops it looping a beat or cramming the arc.
      /// </summary>
      public CaptiveSceneStage SceneStage { get; init; } = CaptiveSceneStage.Intro;

      /// <summary>
      ///   When this NPC is secretly plotting against a rival and could draw the player in as a
      ///   discreet agent, the target's name; null otherwise. When set, the prompt teaches the
      ///   <c>scheme_assist</c> action so the NPC may sound the player out, and the host re-checks
      ///   eligibility before the player's help is applied. Only set for a still-secret plot whose
      ///   plotter trusts the player and is not aimed at the player's own ally.
      /// </summary>
      public string? SchemeAgentTargetName { get; init; }

      /// <summary>
      ///   True when a still-secret scheme is aimed at THIS NPC — the player may warn them of it.
      ///   The NPC has no firm knowledge of the plot; the prompt teaches <c>scheme_heed</c> so they
      ///   can act ONLY on a credible warning from the player, never volunteer paranoia. On heed the
      ///   host drags the scheme into the light, giving the target a chance to counter it.
      /// </summary>
      public bool SchemeTargetsThisNpc { get; init; }

      /// <summary>
      ///   The NPC's posture toward the player, as one line per axis that is not neutral (warmth,
      ///   trust, respect, fear), pre-composed by the consumer's StanceDescriptor. Null when the NPC
      ///   is wholly neutral. Injected so the NPC speaks in keeping with every axis at once — they
      ///   may dislike the player yet respect and fear them.
      /// </summary>
      public string? StanceNote { get; init; }

      /// <summary>
      ///   How the NPC's posture inclines them to ACT toward the player this meeting — composed by the
      ///   consumer from its stance-consequence policy (cold-shoulder, secret murderous intent, eager to aid,
      ///   ready to warn of a plot…). Distinct from <see cref="StanceNote" /> (what they feel): this is what
      ///   they would DO, and it shapes whether they deal, help, stonewall, or stay falsely civil. Null when
      ///   nothing is warranted.
      /// </summary>
      public string? StanceConsequenceHint { get; init; }

      /// <summary>
      ///   The dominant, resolvable grievance(s) this NPC nurses against the player — the Grudges pillar,
      ///   coexisting with (not diluted into) <see cref="StanceNote" />'s blended regard. Host-composed
      ///   (the consumer's GrudgeNarrator, at most a per-NPC cap of dominant grudges joined by newlines),
      ///   already mirroring the knownness gate: a grudge the NPC KNOWS may be named openly, one they
      ///   conceal instructs the NPC to stay smooth and never name the true cause. Rendered verbatim, right
      ///   after <see cref="StanceConsequenceHint" />. Null when the NPC nurses no live grudge worth voicing.
      ///   Per-conversation stable (does not change turn to turn), so it sits in the prefix-cacheable prefix.
      /// </summary>
      public string? GrudgeNote { get; init; }

      /// <summary>
      ///   Grudges pillar tone arbiter: a short clause appended right after the CURRENT STANCE regard line
      ///   (<see cref="NpcProfile.ReputationWithPlayer" />) when the dominant live grudge's tone ruling is
      ///   Tempered or Dominates, pointing the LLM at <see cref="GrudgeNote" /> before it even reaches it.
      ///   Host-composed (the consumer's GrudgeToneArbiter); null when no live grudge shadows the regard at
      ///   all, so the regard line renders exactly as before. Per-conversation stable, like <see cref="GrudgeNote" />.
      /// </summary>
      public string? RegardShadowNote { get; init; }

      /// <summary>
      ///   The Appreciations pillar's positive mirror of <see cref="GrudgeNote" />: the dominant LIVE
      ///   kindness(es) this NPC feels toward the player, host-composed (the consumer's
      ///   AppreciationNarrator, at most a per-NPC cap joined by newlines), each one already TARNISHED by
      ///   the holder's own dominant live grudge (a fresh grievance sours a standing warmth — see
      ///   AppreciationPolicy.TarnishedMagnitude) before rendering, so a line that tarnishes to zero is
      ///   dropped rather than shown at full warmth. Rendered right after <see cref="GrudgeNote" />. Null
      ///   when the NPC feels no live kindness worth voicing this turn. Per-conversation stable, like
      ///   <see cref="GrudgeNote" />.
      /// </summary>
      public string? AppreciationNote { get; init; }

      /// <summary>
      ///   Divorce, Phase 2b: set when this NPC is the player's OWN spouse and is currently pressing a
      ///   divorce demand (host-composed, mirroring <see cref="GrudgeNote" />'s additive-field pattern,
      ///   with the accept_divorce / decline_divorce teaching folded in rather than routed through
      ///   <see cref="ExtraActionTeachings" />, since it is a single, stable, per-conversation directive
      ///   naming exactly one NPC's demand, not a composed set of independently-eligible verbs). Rendered
      ///   verbatim right after <see cref="ExtraActionTeachings" />; SKIPPED in <see cref="LeanPromptLevel.Lean" />
      ///   like that field (a small model still gets the demand voiced in dialogue via the NPC's own
      ///   history/letters, but not the extended [ACTION] contract). Null when this NPC is not the
      ///   player's demanding spouse.
      /// </summary>
      public string? SpouseDivorceDemandNote { get; init; }

      /// <summary>
      ///   Divorce, Phase 2a: set when this NPC IS the player's own living spouse, teaching the player-
      ///   initiated end_own_marriage action (host-composed, same additive-field pattern as
      ///   <see cref="SpouseDivorceDemandNote" />, folding its own [ACTION] teaching in directly rather than
      ///   routed through <see cref="ExtraActionTeachings" />). Coexists cleanly with
      ///   <see cref="SpouseDivorceDemandNote" /> when the spouse is ALSO demanding: both notes may render
      ///   together, since accepting the spouse's demand and the player choosing to end it themselves are
      ///   not contradictory. Rendered verbatim right after <see cref="SpouseDivorceDemandNote" />; SKIPPED
      ///   in <see cref="LeanPromptLevel.Lean" /> like that field. Null when this NPC is not the player's
      ///   spouse.
      /// </summary>
      public string? PlayerEndOwnMarriageNote { get; init; }

      /// <summary>
      ///   Divorce escalation: set when this NPC IS the player's own living spouse AND their estrangement
      ///   is currently under way (<c>SpouseDivorceDemandBehavior.IsEstranging</c>), after repeated
      ///   refusals of their Phase 2b demand. Host-composed, the same additive-field pattern as
      ///   <see cref="SpouseDivorceDemandNote" />, folding the accept_divorce <c>[ACTION]</c> teaching in
      ///   directly (decline_divorce no longer applies here: an estrangement already under way is not
      ///   something to refuse in words, only to accept or to genuinely repair through deeds). Rendered
      ///   verbatim right after <see cref="PlayerEndOwnMarriageNote" />; SKIPPED in
      ///   <see cref="LeanPromptLevel.Lean" /> like the sibling notes. Mutually exclusive with
      ///   <see cref="SpouseDivorceDemandNote" /> in practice (beginning the estrangement clears the
      ///   pending-demand flag), though nothing here depends on that. Null when this NPC's estrangement is
      ///   not active.
      /// </summary>
      public string? SpouseEstrangementNote { get; init; }

      /// <summary>
      ///   A full replacement for the BEHAVIOR GUIDELINES section, matched to the NPC's social station
      ///   (gang leader, town notable, wanderer...). Host-composed from the station guideline files; when
      ///   null or blank the builder falls back to the global guidelines override, then to the built-in
      ///   noble register. Fixes the "gang leader waving guards away from her throne" class of bug: the
      ///   voice register finally follows the speaker's station, not the lordly default. Per-conversation
      ///   stable, so it sits in the prefix-cacheable region like the rest of the guidelines.
      /// </summary>
      public string? StationGuidelinesOverride { get; init; }

      public DiplomaticStatus WarStatus { get; init; } = DiplomaticStatus.Unknown;

      /// <summary>
      ///   NPCs present during this encounter who can hear the conversation.
      ///   When non-empty, the prompt instructs the NPC to adjust candor accordingly.
      ///   Null or empty list means the conversation is private.
      /// </summary>
      public IReadOnlyList<WitnessEntry>? Witnesses { get; init; }

      /// <summary>
      ///   Pre-formatted "what you've heard" block — recent world happenings this NPC would
      ///   plausibly know of (their faction, their region, recency), drawn from the world-event
      ///   store (v1.2). One line each, prefixed "- ". Null when nothing relevant has reached
      ///   them. Injected as hearsay so the NPC can reference current events without claiming
      ///   firsthand knowledge.
      /// </summary>
      public string? WorldRumorsBlock { get; init; }

      /// <summary>
      ///   Produces a natural-language description of this encounter, suitable
      ///   for injection into the LLM system prompt. Returns an empty string
      ///   when all fields are Unknown.
      /// </summary>
      public string ToPromptDescription()
      {
         var parts = new List<string>();

         switch (Scene)
         {
            case SceneType.Outdoor: parts.Add("The encounter takes place outdoors, on the open road or wilderness."); break;
            case SceneType.Settlement: parts.Add("The encounter takes place inside a settlement."); break;
            case SceneType.Keep: parts.Add("The encounter takes place within the walls of a keep."); break;
            case SceneType.Tavern: parts.Add("The encounter takes place in a tavern, surrounded by patrons."); break;
            case SceneType.Dungeon: parts.Add("The encounter takes place in a dungeon."); break;
            case SceneType.Battlefield: parts.Add("The encounter takes place on a battlefield, immediately after combat."); break;
         }

         switch (WarStatus)
         {
            case DiplomaticStatus.AtWar: parts.Add("Your faction and the player's faction are currently at war."); break;
            case DiplomaticStatus.AtPeace: parts.Add("Your factions are at peace."); break;
            case DiplomaticStatus.Allied: parts.Add("Your factions are formally allied or the same."); break;
         }

         switch (PlayerStatus)
         {
            case PlayerStatusVsNpc.Captive: parts.Add("The player is currently your captive."); break;
            case PlayerStatusVsNpc.NpcIsCaptive: parts.Add("You are currently the player's captive."); break;
            case PlayerStatusVsNpc.ClanMember: parts.Add("The player belongs to your own clan."); break;
            // Both feudal notes state WHO RULES WHAT alongside the conclusion, whenever the realm is known.
            // A bare conclusion is unfalsifiable: it shipped inverted once, contradicted the NPC's own
            // "Holds royal authority over a kingdom." rank line, and the model resolved the contradiction
            // the wrong way (an Aserai Sultan denied being Sultan and called his new vassal "my liege").
            // Naming the realm and its ruler gives the model something to check the conclusion against.
            case PlayerStatusVsNpc.Vassal:
               parts.Add(string.IsNullOrWhiteSpace(SharedRealmName)
                  ? "The player is your sworn vassal."
                  : $"You rule {SharedRealmName}, and the player has sworn to it: they are your vassal.");

               break;
            case PlayerStatusVsNpc.Liege:
               parts.Add(string.IsNullOrWhiteSpace(SharedRealmName)
                  ? "The player is your liege lord."
                  : $"The player rules {SharedRealmName}, the realm you serve: they are your liege lord.");

               break;
            // Paid service, never fealty: the engine stores a mercenary contract the same way it stores an
            // oath, so the prompt has to draw the line the data does not. What follows from it is the whole
            // relationship: no loyalty is owed, the contract ends, and they may take the enemy's coin next.
            case PlayerStatusVsNpc.Mercenary:
               parts.Add(string.IsNullOrWhiteSpace(SharedRealmName)
                  ? "The player's clan fights under a mercenary contract, for pay and not by oath."
                  : $"The player's clan is hired to {SharedRealmName}, for pay and not by oath: mercenaries, not sworn vassals.");

               break;
            case PlayerStatusVsNpc.Employer:
               parts.Add(string.IsNullOrWhiteSpace(SharedRealmName)
                  ? "Your clan fights for the player's realm under a mercenary contract, for pay and not by oath."
                  : $"The player rules {SharedRealmName}, which hires your clan: they are your paymaster, not your liege, and the contract will end.");

               break;
            case PlayerStatusVsNpc.SameRealm:
               parts.Add(string.IsNullOrWhiteSpace(SharedRealmName)
                  ? "The player is sworn to the same realm as you."
                  : $"You and the player are both sworn to {SharedRealmName}: fellow vassals of the same crown, neither commanding the other.");

               break;
            case PlayerStatusVsNpc.Stranger: parts.Add("You have never met this person before."); break;
            case PlayerStatusVsNpc.Free: /* default, no special note */ break;
         }

         // The time-since-last-meeting note is rendered separately (PromptBuilder.AppendEncounterContext) with
         // its own restraint guidance, and only for a long-enough gap — see EncounterContext.MeetingGapNote.

         return string.Join(" ", parts);
      }
   }

   /// <summary>
   ///   The dramatic beats of a captive scene, walked by the host's scene director. Each captor
   ///   turn performs exactly one stage; the director advances it so the model cannot loop a beat.
   /// </summary>
   public enum CaptiveSceneStage
   {
      Intro, // brought before the captor; intent stated, menace — no physical act yet
      Initiate, // the aggression begins — the first act
      Intensify, // escalation — a different / harder act, never a repeat
      Climax, // the aggressor finishes / reaches satisfaction
      Conclude, // the scene wraps: prisoner removed, end_conversation

      // ── Appended 01/07/2026 for director arc variety (see CaptiveSceneArcPolicy in the mod).
      //    Transient per-turn scene state, never persisted, but still appended at the end. ──

      /// <summary>
      ///   Between Intro and the physical act: the captor circles, inspects, closes distance, and
      ///   builds dread through word, gaze, and the smallest touch, without beginning the act proper.
      /// </summary>
      RisingTension,

      /// <summary>
      ///   After the final Climax, before Conclude: nothing new starts; the beat lingers on the
      ///   immediate consequence (the prisoner's state, parting words, any mark left, the room going
      ///   cold again) before they are sent back.
      /// </summary>
      Aftermath
   }

   /// <summary>Who acts this beat in a (possibly collective) captive scene.</summary>
   public enum CaptiveAggressorKind
   {
      Lead, // the chief / lead captor
      AnotherSingle, // another single member of the band takes their turn
      GroupTogether // the remaining members act on the prisoner together, at once
   }

   public enum PlayerStatusVsNpc
   {
      Unknown,
      Stranger,
      Free,
      Captive,
      NpcIsCaptive,
      ClanMember,
      Vassal,
      Liege,
      Mercenary,

      /// <summary>
      ///   Both serve the same crown, neither commanding the other: fellow vassals. Appended last so any
      ///   ordinal-sensitive reader keeps its footing.
      /// </summary>
      SameRealm,

      /// <summary>
      ///   The player's realm employs THIS NPC's clan as mercenaries. The player is their paymaster, which
      ///   is emphatically not their liege: no fealty is owed in this direction either.
      /// </summary>
      Employer
   }

   public enum DiplomaticStatus
   {
      Unknown,
      AtPeace,
      AtWar,
      Allied
   }

   public enum SceneType
   {
      Unknown,
      Outdoor,
      Settlement,
      Keep,
      Tavern,
      Dungeon,
      Battlefield
   }

   /// <summary>Which party, if any, could conceive from a completed vaginal act in the current scene.</summary>
   public enum ConceptionRisk
   {
      None,
      PlayerCanConceive,
      NpcCanConceive
   }
}