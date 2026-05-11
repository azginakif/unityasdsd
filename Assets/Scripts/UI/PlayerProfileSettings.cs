using UnityEngine;

namespace MobilOfl.UI
{
    public static class PlayerProfileSettings
    {
        private const string PlayerNameKey = "mobilofl.playername";
        private const string DefaultPlayerName = "Dedektif";
        private const int MaxNameLength = 18;

        public static string LoadPlayerName()
        {
            return Sanitize(PlayerPrefs.GetString(PlayerNameKey, DefaultPlayerName));
        }

        public static void SavePlayerName(string playerName)
        {
            PlayerPrefs.SetString(PlayerNameKey, Sanitize(playerName));
            PlayerPrefs.Save();
        }

        public static string Sanitize(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return DefaultPlayerName;
            }

            var sanitized = playerName.Trim();
            if (sanitized.Length > MaxNameLength)
            {
                sanitized = sanitized.Substring(0, MaxNameLength);
            }

            return sanitized;
        }
    }
}
