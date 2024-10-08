﻿using Newtonsoft.Json;
using Sdl.Community.DeepLMTProvider.Helpers;
using Sdl.Community.DeepLMTProvider.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Sdl.Community.DeepLMTProvider
{
    public static class GlobalSettings
    {
        private static string FileName { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Constants.DeepLDataPath, "GlobalSettings.json");

        public static string GetLastUsedGlossaryId((string, string) languagePair)
        {
            var glossaryIds = LoadGlossaryIds();
            glossaryIds.TryGetValue(languagePair, out var glossaryId);
            return glossaryId;
        }

        public static void SetLastUsedGlossaryIds(Dictionary<(string, string), string> glossaryIds)
        {
            var glossaryIdsDictionary = LoadGlossaryIds();
            foreach (var languagePair in glossaryIds.Keys)
                glossaryIdsDictionary[languagePair] = glossaryIds[languagePair];
            SaveGlossaryIds(glossaryIdsDictionary);
        }

        private static Dictionary<(string, string), string> LoadGlossaryIds()
        {
            if (!File.Exists(FileName)) return new();

            try
            {
                var globalSettingsSerialized = File.ReadAllLines(FileName);
                return DeserializeGlossaryIds(globalSettingsSerialized);
            }
            catch
            {
                File.Delete(FileName);
                MessageBox.Show(
                    PluginResources
                        .GlobalSettings_LoadGlossaryIds_Global_settings_deserialization_failed__Settings_were_reset_to_default_,
                    PluginResources.GlobalSettings_LoadGlossaryIds_Global_settings, MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return new Dictionary<(string, string), string>();
            }
        }

        private static Dictionary<(string, string), string> DeserializeGlossaryIds(string[] globalSettingsSerialized)
        {
            var glossaryIds = new Dictionary<(string, string), string>(globalSettingsSerialized.Length);
            foreach (var line in globalSettingsSerialized)
            {
                var keyValuePair = line.Split(':');

                if (keyValuePair.Length < 2) return glossaryIds;

                var lpSplit = keyValuePair[0].TrimStart(@"{""(".ToCharArray()).TrimEnd(@")""".ToCharArray()).Split(',');
                var source = lpSplit[0];
                var target = lpSplit[1].Trim();

                var glossaryId = keyValuePair[1].TrimStart(@"""".ToCharArray()).TrimEnd(@"""}".ToCharArray());
                glossaryIds[(source, target)] = glossaryId == "null" ? null : glossaryId;
            }

            return glossaryIds;
        }

        private static void SaveGlossaryIds(Dictionary<(string, string), string> glossaryIdsDictionary)
        {
            var serializedGlossaryIds = JsonConvert.SerializeObject(glossaryIdsDictionary);

            var directoryPath = Path.GetDirectoryName(FileName);
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            if (!File.Exists(FileName)) File.Create(FileName).Close();
            File.WriteAllText(FileName, serializedGlossaryIds);
        }
    }
}