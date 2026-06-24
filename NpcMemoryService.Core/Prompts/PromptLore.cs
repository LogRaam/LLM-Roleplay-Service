// Code written by Gabriel Mailhot, 23/06/2026.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Prompts
{
    /// <summary>
    ///   The setting's name, adjective, and which cultures read as patriarchal — used by the prompt sections.
    ///   Defaults to Calradia so the SDK behaves unchanged on its own; the host (mod) overwrites these at
    ///   launch from an editable <c>ModuleData/setting.json</c>, which makes Calradia Remembers reskinnable
    ///   for total-overhaul mods (The Old Realms and the like). Kept static + mutable so the many static
    ///   prompt sections can read it without threading setting config through every method signature.
    /// </summary>
    public static class PromptLore
    {
        /// <summary>The world's name in prose ("Calradia").</summary>
        public static string WorldName = "Calradia";

        /// <summary>The world's adjective ("Calradian").</summary>
        public static string WorldAdjective = "Calradian";

        /// <summary>Culture / faction name fragments whose men are skeptical of female authority.</summary>
        public static IReadOnlyList<string> PatriarchalCultures =
            new[] { "Vlandia", "Northern Empire", "Western Empire", "Aserai" };
    }
}
