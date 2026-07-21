// Quick-win hardening of the captive-scene prompts (audit 2026-07-17, P4.1–P4.3):
//  - P4.1: the sexualized stage/intensity beats must never be injected into the NON-sexual bandit
//    scenes (Extortion / Intimidation / Revenge) — until now only Reckoning was excluded, so a
//    Climax beat could land in a ransom shakedown.
//  - P4.2: the stale "question" line claimed the player could not interrupt unless the model asked
//    one, which the code contradicts (it pauses after EVERY beat).
//  - P4.3: "let's move on" was taught as in-fiction yielding; it is now read as the PLAYER stepping
//    out of the fantasy, to be honored by closing the scene in character.

// 2026-07-19, Gabriel in play: penetration arrived abruptly, one beat carrying the threat, the contact and
// the whole act at once, so the scene had no interior for the player to live through. The arc machinery
// (CaptiveSceneArcPolicy) already paces the PRELIMINARIES, which is what makes a brutal deserter differ
// from a patient lord, and it was working as designed. What was missing was a bound on the ACT ITSELF, so
// every arc, even a slow one, could collapse it into a single moment. See the gravity-ladder tests below.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptiveScenePromptHardeningTests
   {
      private const string StageDirective = "THE BEAT TO PERFORM THIS TURN";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string BuildCaptivePrompt(CaptiveSceneIntent intent)
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true};
         var context = new EncounterContext {
            Scene = SceneType.Dungeon,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = intent,
            SceneStage = CaptiveSceneStage.Climax
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // Play-test 2026-07-20 (Gabriel): a scene kept one man's boot grinding the prisoner's hands WHILE he took
      // her from behind, kept the captor "on the log" while he stood over her, and destroyed "the last" bones in
      // those hands four separate times. The act rules police WHAT happens and the phrase rule polices the WORDS;
      // nothing policed whether the bodies could physically be where the prose put them, which is what breaks the
      // illusion fastest. These four clauses are that guard.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_physical_continuity_is_required_of_the_bodies()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         prompt.Should().Contain("ONE MAN, ONE PLACE");          // no man in two places at once
         prompt.Should().Contain("YOUR OWN STANCE IS CONTINUOUS"); // not seated and standing in one breath
         prompt.Should().Contain("INJURY IS ONE-WAY");            // never re-break what is already destroyed
         prompt.Should().Contain("A MAN STUCK ON ONE ACT IS A PROP"); // vary the DEED, not merely the wording
      }

      // The escape trigger must stay NARROW (Gabriel 2026-07-20): only a genuine bid to GET AWAY fires the real
      // escape encounter (a whole combat mission the chat closes for). A fight, a headbutt, or defiance that is
      // not an attempt to flee must stay an ordinary in-chat beat, or every scuffle would rip the player out of
      // the scene into combat. This test guards that the prompt draws that line.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_escape_is_taught_THEN_only_a_bid_to_flee_counts_not_mere_resistance()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         prompt.Should().Contain("type: escape_attempt");
         prompt.Should().Contain("An escape attempt is a bid to GET AWAY");
         prompt.Should().Contain("Do NOT treat");
         prompt.Should().Contain("mere resistance");
      }


      private static string BuildCaptivePromptWithFocus(string focus)
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true};
         var context = new EncounterContext {
            Scene = SceneType.Dungeon,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            SceneStage = CaptiveSceneStage.Intro,
            CaptiveSceneFocus = focus
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      private static string BuildCollectiveCaptivePrompt()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true};
         var context = new EncounterContext {
            Scene = SceneType.Dungeon,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            SceneStage = CaptiveSceneStage.Climax,
            IsCollectiveCaptiveScene = true,
            Witnesses = new System.Collections.Generic.List<WitnessEntry> {
               new() {Name = "A soldier", RelationToNpc = "one of your men, hungry for a turn"},
               new() {Name = "Another soldier", RelationToNpc = "one of your men, impatient"}
            }
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // P4.1: the bandit menace scenes are declared NOT sexual ("This is NOT a sexual scene"), so
      // the sexualized beat machine ("reach your satisfaction", "INTENSITY 5/5") must stay out of
      // all three non-sexual bandit intents, exactly as it already did for Reckoning.
      [Test]
      public void GIVEN_a_non_sexual_bandit_intent_WHEN_built_THEN_no_stage_directive_is_injected()
      {
         foreach (CaptiveSceneIntent intent in new[] {
                     CaptiveSceneIntent.Extortion,
                     CaptiveSceneIntent.Intimidation,
                     CaptiveSceneIntent.Revenge,
                     CaptiveSceneIntent.Reckoning
                  })
         {
            string prompt = BuildCaptivePrompt(intent);

            prompt.Should().NotContain(StageDirective, $"intent {intent} is non-sexual and must not walk the beats");
            prompt.Should().NotContain("INTENSITY THIS BEAT", $"intent {intent} is non-sexual and must not carry an intensity cue");
         }
      }

      // The positive control: a sexual intent still walks the stage machine, at full Climax
      // intensity — the exclusion above is scoped, not a blanket removal.
      [Test]
      public void GIVEN_a_sexual_intent_WHEN_built_THEN_the_stage_directive_is_still_injected()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.PersonalDesire);

         prompt.Should().Contain(StageDirective);
         prompt.Should().Contain("INTENSITY THIS BEAT: 5/5");
      }

      // P4.2: the new wording tells the model the truth about the agency — the player ALWAYS gets to
      // answer after a beat (the game offers it whatever the model writes), and a question is an
      // invitation, never a requirement. The stale "cannot interrupt" claim must be gone.
      [Test]
      public void GIVEN_a_captive_scene_with_a_stage_directive_WHEN_built_THEN_the_question_line_tells_the_truth_about_agency()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.PersonalDesire);

         prompt.Should().Contain("The player ALWAYS gets the chance to answer after your");
         prompt.Should().Contain("withholding one never silences the player");
         prompt.Should().NotContain("cannot interrupt");
      }

      // P4.3: a vague in-character yielding ("fine", "as you wish") still reads as the prisoner
      // giving in, but a request to LEAVE the scene is the player stepping out of the fantasy and
      // must close the scene in character. The old "interpret it as yielding and proceed" teaching —
      // the riskiest line in the file — must be gone.
      [Test]
      public void GIVEN_a_personal_desire_captive_scene_WHEN_built_THEN_leaving_the_scene_is_honored_not_read_as_yielding()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.PersonalDesire);

         prompt.Should().Contain("A vague in-character yielding");
         prompt.Should().Contain("the PLAYER stepping out of the");
         prompt.Should().Contain("bringing the scene to its close in character");
         prompt.Should().NotContain("Interpret it as yielding and proceed");
      }

      // THE GRAVITY LADDER (Gabriel 2026-07-19). Handling is a threat and may fill a beat; penetration is the
      // gravest act and must be reached by degrees, because that is where the dread lives. Without the bound
      // a beat opened on pressure and ended fully seated, which spends the tension it existed to build.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_a_grave_act_advances_by_one_degree_per_beat()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         prompt.Should().Contain("ONE");
         prompt.Should().Contain("BEAT ADVANCES IT BY ONE DEGREE ONLY");
         prompt.Should().Contain("the approach");
         prompt.Should().Contain("the threshold");
         prompt.Should().Contain("and only in a LATER beat, fully");
      }

      // The player's turn is not an interruption of the act, it is PART of its pacing: the answer belongs
      // between the degrees. A rule that slowed the act but let the captor take every degree unanswered
      // would read as a monologue and lose exactly what the slowing was for.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_the_player_answers_between_the_degrees()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         prompt.Should().Contain("Stop at the end of each degree and let the player answer");
         prompt.Should().Contain("belong BETWEEN the degrees");
      }

      // It must stay a PACE, not a script: Gabriel was explicit that his worked example must not become the
      // same scene every time. Pins that the ladder is not to be announced or numbered in the prose.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_the_ladder_is_a_pace_and_not_a_script()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         prompt.Should().Contain("Do NOT number these or announce them");
         prompt.Should().Contain("the shape differs every scene");
      }

      // The distinction the ladder rests on: lesser physical acts are NOT slowed. A rule that paced
      // everything would make a scene crawl and would blur the difference between a threat and the grave act.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_lesser_acts_may_still_fill_a_whole_beat()
      {
         BuildCaptivePrompt(CaptiveSceneIntent.Domination)
            .Should().Contain("these are THREAT, and one of them may occupy a whole beat");
      }

      // THE CLOSING CONTRADICTION (Gabriel, in play 2026-07-19). The old step 2 told the captor to show the
      // prisoner "released from their bonds and taken back to their cell", and a model obeyed it exactly:
      // "cut her loose and drag her back to the wagon". Nobody unties a prisoner they are returning to
      // captivity, and the scene then resumed with her still held, so the instruction contradicted itself.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_the_closing_never_asks_for_the_prisoner_to_be_unbound()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         prompt.Should().NotContain("released from their bonds");
         prompt.Should().Contain("do NOT cut their bonds, free them, or let them walk away");
      }

      // The other half of the same line presumed a keep. A bandit band holds people in a wagon on the road,
      // so the place is the CAPTOR's to choose rather than a cell the mod asserts exists.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_the_place_of_confinement_is_not_presumed_to_be_a_cell()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         prompt.Should().Contain("secured again wherever YOU");
         prompt.Should().Contain("a baggage wagon");
         prompt.Should().NotContain("ALWAYS returned to their cell");
      }

      // Gabriel 2026-07-19: the prisoner only ever perceived the captor's words and the gestures done to
      // them. A predator undone by his own pleasure is more frightening, and the prisoner can see it.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_the_aggressors_own_body_is_shown_too()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         prompt.Should().Contain("SHOW THE AGGRESSOR'S OWN BODY TOO");
         prompt.Should().Contain("his own body gives");
      }

      // Brute force builds fear on its own, and the scenes leaned almost wholly on sexual contact.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_brute_force_is_taught_as_its_own_tool_of_terror()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         prompt.Should().Contain("BRUTE FORCE IS A TOOL OF TERROR");
         prompt.Should().Contain("a captor does not only take");
         prompt.Should().Contain("A backhand across the face");
      }

      // The accompanying soldiers read as props; the prisoner must FEEL outnumbered. Only a collective
      // scene teaches this, so a solo scene must NOT carry it (there is no one else to give space to).
      [Test]
      public void GIVEN_a_collective_captive_scene_WHEN_built_THEN_the_other_men_are_told_to_take_real_space()
      {
         string collective = BuildCollectiveCaptivePrompt();
         string solo = BuildCaptivePrompt(CaptiveSceneIntent.Domination);

         collective.Should().Contain("GIVE THEM REAL SPACE");
         collective.Should().Contain("AT THE MERCY OF SEVERAL MEN");
         solo.Should().NotContain("GIVE THEM REAL SPACE");
      }

      // Ratified B (Gabriel 2026-07-19): the driving focus used to flavour only turn 1 and evaporate, so
      // every scene reverted to the same escalate-to-a-physical-climax spine. It now governs the WHOLE scene.
      [Test]
      public void GIVEN_a_scene_focus_WHEN_built_THEN_it_is_carried_as_a_governing_throughline()
      {
         string prompt = BuildCaptivePromptWithFocus("an interrogation dressed as intimacy");

         prompt.Should().Contain("THE THROUGHLINE OF THIS SCENE");
         prompt.Should().Contain("an interrogation dressed as intimacy");
         prompt.Should().Contain("it governs every turn, not just the opening");
      }

      // The heart of B: a psychological focus may stay psychological and END there, not be forced to a
      // physical peak. Losing this would put us right back to every scene being the same act.
      [Test]
      public void GIVEN_a_scene_focus_WHEN_built_THEN_a_non_physical_resolution_is_permitted()
      {
         string prompt = BuildCaptivePromptWithFocus("silence and stillness stretched past bearing");

         prompt.Should().Contain("may live and END as a thing of the mind");
      }

      // Gabriel's refinement (2026-07-19): NO rail in EITHER direction. A psychological throughline may also
      // tip into the physical when the scene genuinely charges toward it. The block must free the scene both
      // ways, forbidding only the reflex of an assumed penetrative peak, or we would just build a new rail.
      [Test]
      public void GIVEN_a_psychological_focus_WHEN_built_THEN_it_may_still_tip_into_the_physical_if_earned()
      {
         string prompt = BuildCaptivePromptWithFocus("a war of wills");

         prompt.Should().Contain("it may tip into the");
         prompt.Should().Contain("WHEN the scene genuinely charges toward it");
         prompt.Should().Contain("What is wrong is only");
      }

      // Gabriel's experience rule: an unmet demand must not loop. The scene MOVES and RESOLVES.
      [Test]
      public void GIVEN_a_scene_focus_WHEN_built_THEN_an_unmet_demand_must_not_loop()
      {
         string prompt = BuildCaptivePromptWithFocus("breaking their spirit through humiliation");

         prompt.Should().Contain("MAKE IT AN EXPERIENCE THAT MOVES");
         prompt.Should().Contain("you do NOT ask again and again forever");
      }

      // The block is opt-in: a scene with no focus (a non-sexual bandit intent, or before one is picked)
      // must not carry an empty throughline header.
      [Test]
      public void GIVEN_no_scene_focus_WHEN_built_THEN_no_throughline_block_is_rendered()
      {
         BuildCaptivePrompt(CaptiveSceneIntent.Domination)
            .Should().NotContain("THE THROUGHLINE OF THIS SCENE");
      }

      // Gabriel 2026-07-19 (refined from a storylog): a participating soldier's OWN action, the sensation
      // it forces, and his own words belong in HIS block as a full stage direction; the host must NOT
      // narrate each man's individual act. The earlier version told the host to narrate it, and the host
      // ended up doing everything while the men grunted.
      [Test]
      public void GIVEN_a_collective_captive_scene_WHEN_built_THEN_each_soldiers_act_is_owned_by_his_own_block()
      {
         string prompt = BuildCollectiveCaptivePrompt();

         prompt.Should().Contain("WHO OWNS WHICH TEXT");
         prompt.Should().Contain("go in HIS [WITNESS_REACTION], attributed to him");
         // Strengthened 2026-07-20: the host was writing a man's act in his block AND echoing it in the
         // narration (Gabriel's log), which is what makes the host look like it did everything. The rule now
         // forbids the echo explicitly, so the test pins the prohibition, not just the ownership.
         prompt.Should().Contain("Do NOT narrate a man's individual act in YOUR [NARRATION]");
         prompt.Should().Contain("NOT EVEN a summary or an echo");
         prompt.Should().Contain("your narration does not mention it again");
      }

      // The other half: the host's narration keeps the COMBINED, shared experience that no single man owns,
      // so the scene still has a narrator for "two men at once" and the setting.
      [Test]
      public void GIVEN_a_collective_captive_scene_WHEN_built_THEN_the_narration_keeps_the_combined_experience()
      {
         BuildCollectiveCaptivePrompt()
            .Should().Contain("the prisoner's COMBINED experience that no one man");
      }

      // Gabriel 2026-07-19: scenes named the acts but rarely described the body itself the way erotic
      // literature does, which flattened immersion. The explicit captive path now carries the novelistic
      // anatomy licence.
      [Test]
      public void GIVEN_an_explicit_captive_scene_WHEN_built_THEN_novelistic_anatomy_description_is_licensed()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.PersonalDesire);

         prompt.Should().Contain("DESCRIBE THE BODY AS A NOVEL WOULD");
         prompt.Should().Contain("neither coy and euphemistic nor cold and clinical");
      }

      // Play-test 2026-07-20 (Gabriel): every physical attempt the player typed ("I resist", "I try to fight
      // back") was absorbed with no effect, because the model was ADJUDICATING the struggle it was also
      // writing. The mod now draws the verdict itself (CaptiveActionOutcomePolicy) and hands it down as a
      // fixed fact. These three assertions pin the two branches and, above all, that the model is forbidden
      // from reversing the verdict, which is exactly what it did when left to judge.
      [Test]
      public void GIVEN_a_pre_drawn_success_WHEN_built_THEN_the_model_is_told_the_attempt_lands_and_may_not_reverse_it()
      {
         string prompt = BuildCaptivePromptWithVerdict(true);

         prompt.Should().Contain("THE PLAYER'S ATTEMPT THIS TURN IS ALREADY DECIDED, AND NOT BY YOU");
         prompt.Should().Contain("the attempt LANDS");
         prompt.Should().Contain("NEVER negate it, soften it,");
      }

      // The failing branch has its own trap: a model told "it fails" tends to answer with a beating that
      // escalates on its own. The prompt must keep the failure OWNED by the attempt, and it must still bar
      // the model from generously letting it half-work.
      [Test]
      public void GIVEN_a_pre_drawn_failure_WHEN_built_THEN_the_model_is_told_the_attempt_does_not_land()
      {
         string prompt = BuildCaptivePromptWithVerdict(false);

         prompt.Should().Contain("THE PLAYER'S ATTEMPT THIS TURN IS ALREADY DECIDED, AND NOT BY YOU");
         prompt.Should().Contain("FAILS");
         prompt.Should().NotContain("the attempt LANDS");
      }

      // The verdict is drawn ONLY on a real player turn (a synthetic scene cue has no attempt to judge), so
      // the absence of a verdict must leave the prompt untouched: a stale "already decided" note on a turn
      // with no attempt would invite the model to invent one.
      [Test]
      public void GIVEN_no_verdict_drawn_WHEN_built_THEN_no_attempt_directive_is_injected()
      {
         BuildCaptivePrompt(CaptiveSceneIntent.Domination)
            .Should().NotContain("THE PLAYER'S ATTEMPT THIS TURN IS ALREADY DECIDED");
      }

      private static string BuildCaptivePromptWithVerdict(bool succeeds)
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true};
         var context = new EncounterContext {
            Scene = SceneType.Dungeon,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            PlayerAttemptSucceeds = succeeds
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }
   }
}
