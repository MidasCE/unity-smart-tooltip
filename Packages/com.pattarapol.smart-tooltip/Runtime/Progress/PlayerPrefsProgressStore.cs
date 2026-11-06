using System.Collections.Generic;
using SmartTooltip.Core;
using UnityEngine;

namespace SmartTooltip
{
    public sealed class PlayerPrefsProgressStore : ITooltipProgressStore
    {
        const char Separator = '\n';

        readonly string key;
        readonly HashSet<string> seen;

        /// <param name="key">Use a different key per player if one device can have many accounts.</param>
        public PlayerPrefsProgressStore(string key = "smarttooltip.seen")
        {
            this.key = key;
            var stored = PlayerPrefs.GetString(key, string.Empty);
            seen = new HashSet<string>(stored.Split(new[] { Separator }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        public bool HasSeen(string ruleId) => seen.Contains(ruleId);

        public void MarkSeen(string ruleId)
        {
            if (seen.Add(ruleId))
                Save();
        }

        public void ResetAll()
        {
            seen.Clear();
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        void Save()
        {
            PlayerPrefs.SetString(key, string.Join(Separator.ToString(), seen));
            PlayerPrefs.Save();
        }
    }
}
