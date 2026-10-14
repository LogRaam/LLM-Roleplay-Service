// Code written by Gabriel Mailhot, 11/10/2026.
// The character prompt's sections, keyed, in the stock order: increment 1 of the Prompt Composer (DESIGN.md in the mod
// repository). Each entry is exactly what BuildSystemPrompt wrote at that place before, gate included; the golden prompts
// (NpcMemoryServiceTests/GoldenPromptTests) prove the prompt is identical, character for character. The comments that
// explained the order in BuildSystemPrompt moved here with their sections.

#region

using System.Collections.Generic;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   public sealed partial class PromptBuilder
   {
      /// <summary>
      ///   The character prompt's sections in the stock order: the order BuildSystemPrompt writes them in. Keys are a
      ///   contract (NpcMemoryServiceTests/Golden/section_keys.txt): never renamed once shipped.
      /// </summary>
      public static IReadOnlyList<PromptSectionSpec> CharacterSections { get; } = new List<PromptSectionSpec>
      {
         // ── Permission preamble: highest framing weight (read first) ────────
         Section("adult_framing_preamble", PromptZone.Prefix, (b, sb, s) => b.AppendAdultFramingPreamble(sb)),
         // ── Static prefix: identical for every NPC in the session ──────────
         Section("format_instructions", PromptZone.Prefix, (b, sb, s) => b.AppendFormatInstructions(sb, s.Context, s.Lean), PromptSectionLock.Sealed),
         Section("dialogue_style", PromptZone.Prefix, (b, sb, s) => b.AppendDialogueStyle(sb, s.Context)),
         Section("prose_craft", PromptZone.Prefix, (b, sb, s) => { if (LeanPromptPolicy.Include(PromptSection.ProseCraftBlocklist, s.Lean)) AppendProseCraft(sb); }),
         Section("lean_prose_craft", PromptZone.Prefix, (b, sb, s) => { if (!(LeanPromptPolicy.Include(PromptSection.ProseCraftBlocklist, s.Lean))) AppendLeanProseCraft(sb); }),
         Section("world_description", PromptZone.Prefix, (b, sb, s) => { if (LeanPromptPolicy.Include(PromptSection.WorldNarrative, s.Lean)) b.AppendWorldDescription(sb, s.Vars); }),
         // THE PLAYER moved to the end of the prompt (fkasad, 10/10/2026): see AppendPlayerDescription's call below.
         // Behavior guidelines are station-matched (lord, notable, wanderer, gang leader), so this is the
         // FIRST block that differs between two NPCs of the same session. It sits AFTER the world and player
         // blocks, which are identical for every NPC, so the cross-NPC shared prefix runs as long as possible
         // before it splits: a modder with a large custom world.txt (TOR, reported 2026-09-01) saw the stranded
         // world block cost prefix-cache reuse when switching characters. Still inside the cacheable prefix
         // (above the CURRENT ENCOUNTER marker) and above the per-NPC identity that follows.
         Section("behavior_guidelines", PromptZone.Prefix, (b, sb, s) => { if (LeanPromptPolicy.UseFullBehaviorGuidelines(s.Lean)) b.AppendBehaviorGuidelines(sb, s.Context, s.Vars); }),
         Section("lean_behavior_guidelines", PromptZone.Prefix, (b, sb, s) => { if (!(LeanPromptPolicy.UseFullBehaviorGuidelines(s.Lean))) b.AppendLeanBehaviorGuidelines(sb, s.Context, s.Vars); }),
         // ── Per-NPC identity ─────────────────────────────────────────────────
         Section("identity", PromptZone.Prefix, (b, sb, s) => AppendIdentity(sb, s.Npc, s.Context)),
         // Who the player IS to this character: sworn, hired, kin, captive, or bound by nothing. It sits
         // beside the identity because it is the same kind of fact and changes on the same scale (an oath, a
         // war), and ABOVE the marker because it used to ride below it and was billed fresh every turn for a
         // fact that had not moved. See PlayerStandingNote for the report that put it here.
         Section("player_standing", PromptZone.Prefix, (b, sb, s) => AppendPlayerStanding(sb, s.Context, s.Lean)),
         Section("npc_self_appearance", PromptZone.Prefix, (b, sb, s) => b.AppendNpcSelfAppearance(sb, s.Context)),
         // Voice first, then motive: the backstory says who they SOUND like, the conviction what they WANT.
         // Both ride the same lean gate, because an authored character is the whole reason a player authored one:
         // dropping it to save tokens on a small model would silently return them the generic NPC they replaced.
         Section("authored_backstory", PromptZone.Prefix, (b, sb, s) => { if (LeanPromptPolicy.Include(PromptSection.AuthoredBackstory, s.Lean)) AppendAuthoredBackstory(sb, s.Npc); }),
         Section("authored_conviction", PromptZone.Prefix, (b, sb, s) => { if (LeanPromptPolicy.Include(PromptSection.AuthoredBackstory, s.Lean)) AppendAuthoredConviction(sb, s.Npc); }),
         Section("relationships", PromptZone.Prefix, (b, sb, s) => { if (LeanPromptPolicy.Include(PromptSection.Relationships, s.Lean)) AppendRelationships(sb, s.Npc); }),
         Section("romantic_context", PromptZone.Prefix, (b, sb, s) => b.AppendRomanticContext(sb, s.Npc, s.Context)),
         Section("intimacy_consent_rules", PromptZone.Prefix, (b, sb, s) => b.AppendIntimacyConsentRules(sb, s.Npc, s.Context), PromptSectionLock.Sealed),
         Section("social_attraction_instructions", PromptZone.Prefix, (b, sb, s) => b.AppendSocialAttractionInstructions(sb, s.Npc, s.Context)),
         Section("discovered_traits", PromptZone.Prefix, (b, sb, s) => b.AppendDiscoveredTraits(sb, s.Npc, s.Lean)),
         Section("inherited_note", PromptZone.Prefix, (b, sb, s) => AppendInheritedNote(sb, s.Npc)),
         Section("background_context", PromptZone.Prefix, (b, sb, s) => { if (LeanPromptPolicy.Include(PromptSection.CulturalBackground, s.Lean)) AppendBackgroundContext(sb, s.Npc); }),
         Section("history", PromptZone.Prefix, (b, sb, s) => AppendHistory(sb, s.Npc, s.World.CurrentDay, LeanPromptPolicy.MemoryEventLimit(s.Lean), s.Context?.NpcSpouseIsPlayer == true, s.Context?.NpcIsPlayerHousehold == true, s.Context?.HistoryKnownThrough, b.PlayerName)),
         // A captor holding the player prisoner is not a quest-giver: listing the player's tasks
         // here let a bandit captor mistake a "clear the bandits" quest for one HE gave, and torture
         // the prisoner for "failing" it. Quests have no place in a captive scene. SuppressQuests withholds
         // them the same way for other turns where a quest does not belong (2026-08-23: NSFW mode, so an
         // intimate scene never mints a quest from a vow).
         Section("active_quests", PromptZone.Prefix, (b, sb, s) => { if (s.Context?.PlayerStatus != PlayerStatusVsNpc.Captive && s.Context?.SuppressQuests != true) b.AppendActiveQuests(sb, s.Npc, s.Context?.QuestsKnownThrough); }),
         Section("native_quest_note", PromptZone.Prefix, (b, sb, s) => { if (s.Context?.PlayerStatus != PlayerStatusVsNpc.Captive && s.Context?.SuppressQuests != true) b.AppendNativeQuestNote(sb, s.Context); }),
         Section("no_appointments", PromptZone.Prefix, (b, sb, s) => AppendNoAppointments(sb)),
         Section("current_stance", PromptZone.Prefix, (b, sb, s) => AppendCurrentStance(sb, s.Npc)),
         Section("regard_shadow_note", PromptZone.Prefix, (b, sb, s) => AppendRegardShadowNote(sb, s.Context)),
         Section("stance_note", PromptZone.Prefix, (b, sb, s) => AppendStanceNote(sb, s.Context)),
         Section("stance_consequence", PromptZone.Prefix, (b, sb, s) => AppendStanceConsequence(sb, s.Context)),
         Section("grudge_note", PromptZone.Prefix, (b, sb, s) => AppendGrudgeNote(sb, s.Context)),
         Section("appreciation_note", PromptZone.Prefix, (b, sb, s) => AppendAppreciationNote(sb, s.Context)),
         Section("standing_accords", PromptZone.Prefix, (b, sb, s) => AppendStandingAccords(sb, s.Context, s.Lean)),
         Section("player_letters", PromptZone.Prefix, (b, sb, s) => AppendPlayerLetters(sb, s.Npc, s.World.CurrentDay)),
         Section("witnesses", PromptZone.Prefix, (b, sb, s) => AppendWitnesses(sb, s.Context, s.Lean)),
         Section("brought_captives", PromptZone.Prefix, (b, sb, s) => b.AppendBroughtCaptives(sb, s.Context, s.Lean)),
         Section("recruitment", PromptZone.Prefix, (b, sb, s) => AppendRecruitment(sb, s.Context)),
         Section("mercenary_offer", PromptZone.Prefix, (b, sb, s) => AppendMercenaryOffer(sb, s.Context)),
         Section("mercenary_end", PromptZone.Prefix, (b, sb, s) => AppendMercenaryEnd(sb, s.Context)),
         Section("vassal_offer", PromptZone.Prefix, (b, sb, s) => AppendVassalOffer(sb, s.Context)),
         Section("mediate_peace_offer", PromptZone.Prefix, (b, sb, s) => AppendMediatePeaceOffer(sb, s.Context)),
         Section("appoint_governor_offer", PromptZone.Prefix, (b, sb, s) => AppendAppointGovernorOffer(sb, s.Context)),
         Section("assign_party_role_offer", PromptZone.Prefix, (b, sb, s) => AppendAssignPartyRoleOffer(sb, s.Context)),
         Section("rejoin_party_offer", PromptZone.Prefix, (b, sb, s) => AppendRejoinPartyOffer(sb, s.Context)),
         Section("dispatch_mission_offer", PromptZone.Prefix, (b, sb, s) => AppendDispatchMissionOffer(sb, s.Context)),
         Section("grant_fief_offer", PromptZone.Prefix, (b, sb, s) => AppendGrantFiefOffer(sb, s.Context)),
         Section("revoke_fief_offer", PromptZone.Prefix, (b, sb, s) => AppendRevokeFiefOffer(sb, s.Context)),
         Section("expel_from_clan_offer", PromptZone.Prefix, (b, sb, s) => AppendExpelFromClanOffer(sb, s.Context)),
         Section("grant_stipend_offer", PromptZone.Prefix, (b, sb, s) => AppendGrantStipendOffer(sb, s.Context)),
         Section("swear_oath_offer", PromptZone.Prefix, (b, sb, s) => AppendSwearOathOffer(sb, s.Context)),
         Section("follow_me", PromptZone.Prefix, (b, sb, s) => AppendFollowMe(sb, s.Context)),
         Section("dismiss_escort", PromptZone.Prefix, (b, sb, s) => AppendDismissEscort(sb, s.Context)),
         Section("ride_with_me", PromptZone.Prefix, (b, sb, s) => AppendRideWithMe(sb, s.Context)),
         Section("part_ways", PromptZone.Prefix, (b, sb, s) => AppendPartWays(sb, s.Context)),
         Section("duel_challenge", PromptZone.Prefix, (b, sb, s) => AppendDuelChallenge(sb, s.Context)),
         Section("post_duel_context", PromptZone.Prefix, (b, sb, s) => AppendPostDuelContext(sb, s.Context)),
         Section("lord_recruitment", PromptZone.Prefix, (b, sb, s) => AppendLordRecruitment(sb, s.Context)),
         Section("scheme_recruitment", PromptZone.Prefix, (b, sb, s) => AppendSchemeRecruitment(sb, s.Context)),
         Section("scheme_warning", PromptZone.Prefix, (b, sb, s) => AppendSchemeWarning(sb, s.Context)),
         Section("love_match_proposal", PromptZone.Prefix, (b, sb, s) => AppendLoveMatchProposal(sb, s.Npc, s.Context)),
         Section("consort_proposal", PromptZone.Prefix, (b, sb, s) => AppendConsortProposal(sb, s.Context)),
         Section("secret_lover_proposal", PromptZone.Prefix, (b, sb, s) => AppendSecretLoverProposal(sb, s.Context)),
         Section("open_relationship_proposal", PromptZone.Prefix, (b, sb, s) => AppendOpenRelationshipProposal(sb, s.Context)),
         Section("plural_acceptance", PromptZone.Prefix, (b, sb, s) => AppendPluralAcceptance(sb, s.Context)),
         Section("partner_lover_known", PromptZone.Prefix, (b, sb, s) => AppendPartnerLoverKnown(sb, s.Context)),
         Section("held_secret", PromptZone.Prefix, (b, sb, s) => AppendHeldSecret(sb, s.Npc, s.Context)),
         Section("give_item", PromptZone.Prefix, (b, sb, s) => AppendGiveItem(sb, s.Context, s.Lean)),
         // Dropped entirely in Lean (a small local model has no headroom for a favor it is unlikely to use), and
         // gated on WarStatus being resolved (task 6d): a live faction lord has a diplomatic WarStatus computed by
         // the host, while a companion, bandit, or commoner leaves it at the Unknown default: the closest existing
         // "is this a lord" signal in EncounterContext, so a companion chat no longer gets taught a prisoner bargain
         // it can never use.
         // Never to a character at war with the player: the bargain is a task (deliver_prisoner), and an enemy sets the
         // player no task (fkasad, 29/09/2026).
         Section("deliver_prisoner", PromptZone.Prefix, (b, sb, s) => { if (LeanPromptPolicy.Include(PromptSection.DeliverPrisonerOffer, s.Lean) && s.Context?.WarStatus is DiplomaticStatus.AtPeace or DiplomaticStatus.Allied) AppendDeliverPrisoner(sb, s.Context); }),
         Section("prisoner_freedom_bargain", PromptZone.Prefix, (b, sb, s) => AppendPrisonerFreedomBargain(sb, s.Context)),
         Section("ordinary_prisoner_execution_rule", PromptZone.Prefix, (b, sb, s) => AppendOrdinaryPrisonerExecutionRule(sb, s.Context)),
         Section("recruit_prisoner", PromptZone.Prefix, (b, sb, s) => AppendRecruitPrisoner(sb, s.Context)),
         Section("prisoner_cannot_defect", PromptZone.Prefix, (b, sb, s) => AppendPrisonerCannotDefect(sb, s.Context)),
         Section("recruit_notable", PromptZone.Prefix, (b, sb, s) => AppendRecruitNotable(sb, s.Context)),
         Section("take_into_service", PromptZone.Prefix, (b, sb, s) => AppendTakeIntoService(sb, s.Context)),
         Section("prisoner_rescue_bargain", PromptZone.Prefix, (b, sb, s) => AppendPrisonerRescueBargain(sb, s.Context)),
         Section("teach_skill", PromptZone.Prefix, (b, sb, s) => AppendTeachSkill(sb, s.Context)),
         // gather_news was folded into dispatch_mission (2026-08-05): AppendDispatchMissionOffer above is now the
         // SOLE errand offer, so a companion in the party no longer sees two overlapping errand teachings.
         Section("companion_recall_offer", PromptZone.Prefix, (b, sb, s) => AppendCompanionRecallOffer(sb, s.Context)),
         Section("companion_news_report", PromptZone.Prefix, (b, sb, s) => AppendCompanionNewsReport(sb, s.Context)),
         Section("companion_mood_note", PromptZone.Prefix, (b, sb, s) => AppendCompanionMoodNote(sb, s.Context)),
         Section("companion_audience", PromptZone.Prefix, (b, sb, s) => AppendCompanionAudience(sb, s.Context)),
         Section("companion_camp_note", PromptZone.Prefix, (b, sb, s) => AppendCompanionCampNote(sb, s.Context)),
         Section("companion_briefing_scene", PromptZone.Prefix, (b, sb, s) => AppendCompanionBriefingScene(sb, s.Context)),
         Section("intimacy_bargain", PromptZone.Prefix, (b, sb, s) => b.AppendIntimacyBargain(sb, s.Context)),
         Section("bastard_mother", PromptZone.Prefix, (b, sb, s) => b.AppendBastardMother(sb, s.Context)),
         Section("marriage", PromptZone.Prefix, (b, sb, s) => AppendMarriage(sb, s.Context)),
         Section("matchmaker", PromptZone.Prefix, (b, sb, s) => AppendMatchmaker(sb, s.Context)),
         // The player's chosen writing voice. Last of the cacheable prefix: it is stable for the whole
         // conversation, so it must never sit after the marker below or it would bust the cache every turn.
         Section("narrative_style", PromptZone.Prefix, (b, sb, s) => AppendNarrativeStyle(sb, s.Context?.NarrativeStyle, s.Vars)),
         // ── Dynamic tail (changes every turn): AppendEncounterContext emits EncounterSectionHeading, the
         // literal marker NpcChatService splits the cacheable prefix on. Everything from here on is per-turn
         // volatile (day count, time of day, scene stage, witness turn flags) and MUST stay after the marker,
         // or it invalidates the prefix cache on every single turn of a long captive scene. ─────────────────
         Section("encounter_context", PromptZone.Marker, (b, sb, s) => AppendEncounterContext(sb, s.Context)),
         // WHO SOUGHT WHOM, and the two one-shot notes. All three describe THIS turn (the first two are consumed
         // on read by the host, so they are present on one turn and gone on the next), and they sat above the
         // marker until 19/09/2026: the same defect IsConversationOpening had, which killed the prefix cache on
         // turn two. Below the marker they cost nothing and the standing line can be restated every turn.
         Section("who_sought_whom", PromptZone.Tail, (b, sb, s) => AppendWhoSoughtWhom(sb, s.Context)),
         Section("interception", PromptZone.Tail, (b, sb, s) => AppendInterception(sb, s.Context)),
         Section("format_feedback", PromptZone.Tail, (b, sb, s) => AppendFormatFeedback(sb, s.Context)),
         Section("held_prisoners_brief", PromptZone.Tail, (b, sb, s) => AppendHeldPrisonersBrief(sb, s.Context, s.Lean)),
         // A captive's terms and how readily she bargains: her fear can move within the conversation, so per turn.
         Section("captive_terms", PromptZone.Tail, (b, sb, s) => AppendCaptiveTerms(sb, s.Context)),
         // An item held out THIS turn, with its worth and the character's purse: a per-turn fact (3chmidt, 13/10/2026).
         Section("item_on_offer", PromptZone.Tail, (b, sb, s) => AppendItemOnOffer(sb, s.Context)),
         // The knowledge that changes under the character (the hour, the sky, where they stand) belongs here
         // with the rest of the per-turn tail, not up in the identity block - see AppendVolatileKnowledge.
         Section("volatile_knowledge", PromptZone.Tail, (b, sb, s) => AppendVolatileKnowledge(sb, s.Context)),
         Section("day_mood", PromptZone.Tail, (b, sb, s) => AppendDayMood(sb, s.Context)),
         Section("world_state", PromptZone.Tail, (b, sb, s) => AppendWorldState(sb, s.World)),
         Section("at_sea_note", PromptZone.Tail, (b, sb, s) => AppendAtSeaNote(sb, s.Context)),
         Section("player_dress_note", PromptZone.Tail, (b, sb, s) => AppendPlayerDressNote(sb, s.Context)),
         Section("courtship_challenge", PromptZone.Tail, (b, sb, s) => AppendCourtshipChallenge(sb, s.Context)),
         Section("witness_turn_directives", PromptZone.Tail, (b, sb, s) => b.AppendWitnessTurnDirectives(sb, s.Context)),
         Section("scene_stage_directive", PromptZone.Tail, (b, sb, s) => { if (b.ShouldAppendCaptiveStageDirective(s.Context)) AppendSceneStageDirective(sb, s.Context, b.PlayerIsFemale); }),
         Section("scene_style_directive", PromptZone.Tail, (b, sb, s) => AppendSceneStyleDirective(sb, s.Context)),
         // How often narration is wanted, which depends on WHICH BEAT this is: a per-turn fact, so it
         // belongs here and nowhere above.
         Section("narration_frequency", PromptZone.Tail, (b, sb, s) => b.AppendNarrationFrequency(sb, s.Context), PromptSectionLock.Sealed),
         Section("power_balance", PromptZone.Tail, (b, sb, s) => AppendPowerBalance(sb, s.Context)),
         Section("player_gender_context", PromptZone.Tail, (b, sb, s) => b.AppendPlayerGenderContext(sb, s.Npc, s.Context)),
         // Hoisted out of AppendEncounterContext so it still renders when encounterContext is null, and kept
         // late (near the language mirror) so recency reinforces the anti-confabulation guard.
         Section("stay_within_what_you_know", PromptZone.Tail, (b, sb, s) => AppendStayWithinWhatYouKnow(sb, s.Lean), PromptSectionLock.Required),
         Section("player_action_narration", PromptZone.Tail, (b, sb, s) => AppendPlayerActionNarration(sb, s.Lean), PromptSectionLock.Required),
         // Kept beside its two siblings at the very end of the prompt, where recency is highest: the three of
         // them are the anti-confabulation wall, one guarding what the NPC promises, one what a stage direction
         // can settle, and this one what the NPC is willing to believe.
         Section("claims_are_not_proof", PromptZone.Tail, (b, sb, s) => AppendClaimsAreNotProof(sb, s.Lean), PromptSectionLock.Required),
         Section("language_mirror", PromptZone.Tail, (b, sb, s) => b.AppendLanguageMirror(sb, s.Lean)),
         // THE PLAYER, near the end (fkasad, Nexus 10/10/2026: the player's description "is landing in the middle of
         // the prompt... the very first thing any average/small llm will forget after a few turns"). Gabriel: move it.
         // The character the NPC is facing reads last but for the output contract and the modder's post-history,
         // which stay the final word. Below the cache marker, so it is re-sent uncached every turn: a priced choice
         // (quality outranks cost); it also lets the cross-NPC shared prefix run past the world block unbroken.
         Section("player_description", PromptZone.Tail, (b, sb, s) => b.AppendPlayerDescription(sb, s.Context, s.Vars, s.Lean)),
         Section("heard_now", PromptZone.Tail, (b, sb, s) => b.AppendHeardNow(sb, s.Vars)),
         // A short, forceful restatement of the machine-read contract, placed at the very end (highest recency)
         // because the full format teaching sits ~10k tokens up in the cached prefix: a weaker model that follows
         // instructions loosely (dialogue only, no [ACTION]/[EVENT]) is far more likely to emit the blocks when
         // the rule is the last thing it reads before generating.
         Section("role_anchor", PromptZone.Tail, (b, sb, s) => b.AppendRoleAnchor(sb, s.Npc, s.Context, s.Lean), PromptSectionLock.Required),
         Section("format_reminder", PromptZone.Tail, (b, sb, s) => AppendFormatReminder(sb, s.Lean, styleActive: !string.IsNullOrWhiteSpace(s.Context?.NarrativeStyle), authoredVoiceActive: !string.IsNullOrWhiteSpace(s.Npc?.AuthoredBackstory)), PromptSectionLock.Required),
         // Last of all (highest recency): the modder's own post-history instructions, if any.
         Section("post_history_instructions", PromptZone.Tail, (b, sb, s) => b.AppendPostHistoryInstructions(sb, s.Vars)),
      };

      private static PromptSectionSpec Section(string key, PromptZone zone, System.Action<PromptBuilder, StringBuilder, PromptBuildState> render,
                                               PromptSectionLock @lock = PromptSectionLock.None)
         => new(key, zone, @lock, render);
   }
}
