// Code written by Gabriel Mailhot, 18/07/2026.
// Adult-prompt audit M11. "Captive scene" and "sexual scene" are not the same thing, and the mod kept
// re-deciding which was which: the prompt builder had its own list to withhold the sexualized beat
// directives, the chat had another to withhold the sexual scene setup, and the model switch had none at
// all, so a ransom or a political reckoning was served by the specialised erotic model. One list, here,
// beside the enum it classifies.
//
// Named apart from the mod's own CalradiaRemembers.Logic.CaptiveIntentRules (which answers different
// questions: the memory verb, the body toll, the grudge severity) because both are in scope at the chat
// call sites. This one lives in the SDK because the prompt builder needs it and cannot see the mod.

namespace NpcMemoryService.Core.Models
{
    /// <summary>What KIND of scene a captive intent opens, as distinct from the fact of captivity.</summary>
    public static class CaptiveSceneIntentRules
    {
        /// <summary>
        ///   True when this intent opens a SEXUAL scene. The four exceptions are confrontations that happen
        ///   to involve a prisoner: a lord's reckoning (a political settling of accounts) and the three
        ///   bandit menace intents, whose own rules state outright "This is NOT a sexual scene". Everything
        ///   else (interrogation, desire, domination, torture, training, reward) escalates physically and is
        ///   governed by the captive scene machinery.
        /// </summary>
        public static bool IsSexual(CaptiveSceneIntent intent)
        {
            switch (intent)
            {
                case CaptiveSceneIntent.Reckoning:
                case CaptiveSceneIntent.Extortion:
                case CaptiveSceneIntent.Intimidation:
                case CaptiveSceneIntent.Revenge:
                    return false;
                default:
                    return true;
            }
        }
    }
}
