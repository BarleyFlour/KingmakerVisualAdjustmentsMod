﻿using UnityEngine;
using UnityModManagerNet;
using Kingmaker.Blueprints;
using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.ResourceLinks;
using static VisualAdjustments.Settings;
using Kingmaker.PubSubSystem;
using Kingmaker.Visual.Sound;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.Blueprints.Root;
using Kingmaker.Utility;
using Kingmaker.Blueprints.Classes;
using HarmonyLib;
using Kingmaker.Cheats;

namespace VisualAdjustments
{
#if DEBUG
    [EnableReloading]
#endif
    public class CharInfo
    {
        public string GUID;
        public string Name;
    }
    public class Main
    {
        const float DefaultLabelWidth = 200f;
        const float DefaultSliderWidth = 300f;
        public static UnityModManager.ModEntry.ModLogger logger;
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Log(string msg)
        {
            if (logger != null) logger.Log(msg);
        }
        public static void Error(Exception ex)
        {
            if (logger != null) logger.Log(ex.ToString());
        }
        public static void Error(string msg)
        {
            if (logger != null) logger.Log(msg);
        }
        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry ModEntry;
        /// public static ReferenceArrayProxy<BlueprintCharacterClass,BlueprintCharacterClassReference> classes = Game.Instance.BlueprintRoot.Progression.CharacterClasses;
        /// public static string[] classes;
        public static List<CharInfo> classes = new List<CharInfo> {};
        /*public static string[] classes = new string[] {
            "Default",
            "Alchemist",
            "Barbarian",
            "Bard",
            "Cleric",
            "Druid",
            "Fighter",
            "Inquisitor",
            "Kineticist",
            "Magus",
            "Monk",
            "Paladin",
            "Ranger",
            "Rogue",
            "Slayer",
            "Sorcerer",
            "Wizard",
            "None"
        };*/
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                ModEntry = modEntry;
                logger = modEntry.Logger;
                settings = Settings.Load(modEntry);
                var harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
#if DEBUG
                modEntry.OnUnload = Unload;
#endif
            }
            catch (Exception e)
            {
                Log(e.ToString() + "\n" + e.StackTrace);
                throw e;
            }
            return true;
        }
        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            new Harmony(modEntry.Info.Id).UnpatchAll(modEntry.Info.Id);
            return true;
        }
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
        // Called when the mod is turned to on/off.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            enabled = value;
            return true; // Permit or not.
        }
        public static void Asd()
        {
            if(classes.Count == 0)
            {
                ///Main.logger.Log("bru");
                foreach (BlueprintCharacterClass c in Utilities.GetScriptableObjects<BlueprintCharacterClass>())
                {
                    if (!c.PrestigeClass && c.ComponentsArray.Length != 0 && !c.IsMythic && !c.ToString().Contains("Mythic") && !c.ToString().Contains("Animal") && !c.ToString().Contains("Scion"))
                    {
                        try
                        {
                            var charinfo = new CharInfo();
                            charinfo.Name = c.Name;
                            charinfo.GUID = c.AssetGuid;
                            if (classes.Count == 0)
                            {
                                var charinf = new CharInfo();
                                charinf.Name = "None";
                                var charinf2 = new CharInfo();
                                charinf2.Name = "Default";
                                classes.Add(charinf);
                                classes.Add(charinf2);
                            }
                            else
                            {
                                if (!classes.Any(asd => asd.Name == charinfo.Name))
                                {
                                    classes.Add(charinfo);
                                }
                            }
                            /*if(!classes.Contains(c.ToString()))
                            { */
                            /*classes.AddItem(c.ToString());
                            Main.logger.Log(c.ToString());*/
                            ///}
                        }
                        catch (Exception e) { Main.logger.Log(e.ToString()); }
                    }
                }
            }
        }
        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            try
            {
                if (!enabled) return;
                Asd();
                ///Main.logger.Log(classes.Count.ToString());
                /*foreach(CharInfo s in classes)
                {
                    Main.logger.Log(s.Name + s.GUID);
                }*/

                if (Game.Instance.Player.PartyCharacters != null)
                {
                    foreach (UnitEntityData unitEntityData in Game.Instance.Player.PartyCharacters)
                    {
                        Settings.CharacterSettings characterSettings = settings.GetCharacterSettings(unitEntityData);
                        if (characterSettings == null)
                        {
                            characterSettings = new CharacterSettings();
                            characterSettings.characterName = unitEntityData.CharacterName;
                            var chinf = new CharInfo();
                            chinf.Name = "Default";
                            chinf.GUID = unitEntityData.Descriptor.Progression.GetEquipmentClass().AssetGuid;
                            characterSettings.classOutfit = chinf;
                            settings.AddCharacterSettings(unitEntityData, characterSettings);
                        }
                        GUILayout.Space(4f);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(string.Format("{0}", unitEntityData.CharacterName), "box", GUILayout.Width(DefaultLabelWidth));
                        characterSettings.showClassSelection = GUILayout.Toggle(characterSettings.showClassSelection, "Select Outfit", GUILayout.ExpandWidth(false));
                        if (unitEntityData.Descriptor.Doll != null)
                        {
                            characterSettings.showDollSelection = GUILayout.Toggle(characterSettings.showDollSelection, "Select Doll", GUILayout.ExpandWidth(false));
                        }
                        else
                        {
                            characterSettings.showDollSelection = GUILayout.Toggle(characterSettings.showDollSelection, "Select Doll", GUILayout.ExpandWidth(false));
                        }
                        characterSettings.showEquipmentSelection = GUILayout.Toggle(characterSettings.showEquipmentSelection, "Select Equipment", GUILayout.ExpandWidth(false));
                        characterSettings.showOverrideSelection = GUILayout.Toggle(characterSettings.showOverrideSelection, "Select Overrides", GUILayout.ExpandWidth(false));
                        ///characterSettings.ReloadStuff = GUILayout.Toggle(characterSettings.ReloadStuff, "Reload", GUILayout.ExpandWidth(false));
#if (DEBUG)
                        characterSettings.showInfo = GUILayout.Toggle(characterSettings.showInfo, "Show Info", GUILayout.ExpandWidth(false));
#endif
                        GUILayout.EndHorizontal();
                        /* if (characterSettings.ReloadStuff == true)
                         {
                             CharacterManager.UpdateModel(unitEntityData.View);
                         }*/
                        if (characterSettings.showClassSelection)
                        {
                            ChooseClassOutfit(characterSettings, unitEntityData);
                            GUILayout.Space(5f);
                        }
                        if (unitEntityData.Descriptor.Doll != null && characterSettings.showDollSelection)
                        {
                            ChooseDoll(unitEntityData);
                            GUILayout.Space(5f);
                        }
                        if (unitEntityData.Descriptor.Doll == null && characterSettings.showDollSelection)
                        {
                            ChooseCompanionColor(characterSettings, unitEntityData);
                            GUILayout.Space(5f);
                        }
                        if (characterSettings.showEquipmentSelection)
                        {
                            ChooseEquipment(unitEntityData, characterSettings);
                            GUILayout.Space(5f);
                        }
                        if (characterSettings.showOverrideSelection)
                        {
                            ChooseEquipmentOverride(unitEntityData, characterSettings);
                            GUILayout.Space(5f);
                        }
#if (DEBUG)
                        if (characterSettings.showInfo)
                        {
                            InfoManager.ShowInfo(unitEntityData);
                            GUILayout.Space(5f);
                        }
#endif
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.ToString() + " " + e.StackTrace);
            }
        }
        static void ChooseClassOutfit(CharacterSettings characterSettings, UnitEntityData unitEntityData)
        {
            var focusedStyle = new GUIStyle(GUI.skin.button);
            focusedStyle.normal.textColor = Color.yellow;
            focusedStyle.focused.textColor = Color.yellow;
            GUILayout.BeginHorizontal();
            foreach (var _class in classes)
            {
                if (_class.Name == "Magus")
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                var style = characterSettings.classOutfit == _class ? focusedStyle : GUI.skin.button;
                if (GUILayout.Button(_class.Name, style, GUILayout.Width(100f)))
                {
                    characterSettings.classOutfit = _class;
                    CharacterManager.RebuildCharacter(unitEntityData);
                    unitEntityData.View.UpdateClassEquipment();
                }
            }
            GUILayout.EndHorizontal();
        }
        static void ChoosePortrait(UnitEntityData unitEntityData)
        {
            if (unitEntityData.Portrait.IsCustom)
            {
                var key = unitEntityData.Descriptor.UISettings.CustomPortraitRaw.CustomId;
                var currentIndex = DollResourcesManager.CustomPortraits.IndexOf(key);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Portrait  ", GUILayout.Width(DefaultLabelWidth));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, DollResourcesManager.CustomPortraits.Count, GUILayout.Width(DefaultSliderWidth)), 0);
                if (GUILayout.Button("Prev", GUILayout.Width(45)) && currentIndex >= 0)
                {
                    newIndex = currentIndex - 1;
                }
                if (GUILayout.Button("Next", GUILayout.Width(45)) && currentIndex < DollResourcesManager.CustomPortraits.Count - 1)
                {
                    newIndex = currentIndex + 1;
                }
                if (GUILayout.Button("Use Normal",GUILayout.Width(DefaultLabelWidth)))
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(ResourcesLibrary.TryGetBlueprint<BlueprintPortrait>("621ada02d0b4bf64387babad3a53067b"));
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                    return;
                }
                var value = newIndex >= 0 && newIndex < DollResourcesManager.CustomPortraits.Count ? DollResourcesManager.CustomPortraits[newIndex] : null;
                GUILayout.Label(" " + value, GUILayout.ExpandWidth(false));

                GUILayout.EndHorizontal();
                if (newIndex != currentIndex && value != null)
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(new PortraitData(value));
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                }
            }
            else
            {
                var key = unitEntityData.Descriptor.UISettings.PortraitBlueprint?.name;
                var currentIndex = DollResourcesManager.Portrait.IndexOfKey(key ?? "");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Portrait ", GUILayout.Width(DefaultLabelWidth));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, DollResourcesManager.Portrait.Count, GUILayout.Width(DefaultSliderWidth)), 0);
                if (GUILayout.Button("Prev", GUILayout.Width(45)) && currentIndex >= 0)
                {
                    newIndex = currentIndex - 1;
                }
                if (GUILayout.Button("Next", GUILayout.Width(45)) && currentIndex < DollResourcesManager.Portrait.Count - 1)
                {
                    newIndex = currentIndex + 1;
                }
                if (GUILayout.Button("Use Custom", GUILayout.Width(DefaultLabelWidth)))
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(CustomPortraitsManager.Instance.CreateNewOrLoadDefault());
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                    return;
                }
                var value = newIndex >= 0 && newIndex < DollResourcesManager.Portrait.Count ? DollResourcesManager.Portrait.Values[newIndex] : null;
                GUILayout.Label(" " + value, GUILayout.ExpandWidth(false));

                GUILayout.EndHorizontal();
                if (newIndex != currentIndex && value != null)
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(value);
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                }
            }
        }
        static void ChooseAsks(UnitEntityData unitEntityData)
        {
            int currentIndex = -1;
            if (unitEntityData.Descriptor.CustomAsks != null)
            {
                currentIndex = DollResourcesManager.Asks.IndexOfKey(unitEntityData.Descriptor.CustomAsks.name);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Custom Voice ", GUILayout.Width(DefaultLabelWidth));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, -1, DollResourcesManager.Asks.Count - 1, GUILayout.Width(DefaultSliderWidth)), 0);
            if (GUILayout.Button("Prev", GUILayout.Width(45)) && currentIndex >= 0)
            {
                newIndex = currentIndex - 1;
            }
            if (GUILayout.Button("Next", GUILayout.Width(45)) && currentIndex < DollResourcesManager.Asks.Count)
            {
                newIndex = currentIndex + 1;
            }
            var value = (newIndex >= 0 && newIndex < DollResourcesManager.Asks.Count) ? DollResourcesManager.Asks.Values[newIndex] : null;
            if (GUILayout.Button("Preview", GUILayout.ExpandWidth(false)))
            {
                var component = value?.GetComponent<UnitAsksComponent>();
                if (component != null && component.PreviewSound != "")
                {
                    component.PlayPreview();
                }
                else if (component != null && component.Selected.HasBarks)
                {
                    var bark = component.Selected.Entries.Random();
                    AkSoundEngine.PostEvent(bark.AkEvent, unitEntityData.View.gameObject);
                }
            }
            GUILayout.Label(" " + (value?.name ?? "None"), GUILayout.ExpandWidth(false));


            GUILayout.EndHorizontal();
            if (newIndex != currentIndex)
            {
                unitEntityData.Descriptor.CustomAsks = value;
                unitEntityData.View?.UpdateAsks();
            }
        }
        static void ChooseFromList<T>(string label, IReadOnlyList<T> list, ref int currentIndex, Action onChoose)
        {
            if (list.Count == 0) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " ", GUILayout.Width(DefaultLabelWidth));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, list.Count - 1, GUILayout.Width(DefaultSliderWidth)), 0);
            GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (newIndex != currentIndex && newIndex < list.Count)
            {
                currentIndex = newIndex;
                onChoose();
            }
        }
        static void ChooseFromList2<T>(string label, IReadOnlyList<T> list, ref int currentIndex, Action onChoose)
        {
            if (list.Count == 0) return;
           /// GUILayout.BeginHorizontal();
            GUILayout.Label(label + " ", GUILayout.Width(DefaultLabelWidth));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, list.Count - 1, GUILayout.Width(DefaultSliderWidth)), 0);
            GUILayout.Label(" " + newIndex,GUILayout.Width(15f));
           /// GUILayout.EndHorizontal();
            if (newIndex != currentIndex && newIndex < list.Count)
            {
                currentIndex = newIndex;
                onChoose();
            }
        }
        static void ChooseEEL(UnitEntityData unitEntityData, DollState doll, string label, EquipmentEntityLink[] links, EquipmentEntityLink link, Action<EquipmentEntityLink> setter)
        {
            if (links.Length == 0)
            {
                GUILayout.Label($"Missing equipment for {label}");
            }
            var index = links.ToList().FindIndex((eel) => eel != null && eel.AssetId == link?.AssetId);
            ChooseFromList(label, links, ref index, () => {
                setter(links[index]);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                CharacterManager.RebuildCharacter(unitEntityData);
            });
        }
        static void ChooseRamp(UnitEntityData unitEntityData, DollState doll, string label, List<Texture2D> textures, int currentRamp, Action<int> setter)
        {
            ChooseFromList(label, textures, ref currentRamp, () => {
                setter(currentRamp);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                CharacterManager.RebuildCharacter(unitEntityData);
            });
        }
        static void ChooseRace(UnitEntityData unitEntityData, DollState doll)
        {
            var currentRace = doll.Race;
            var races = BlueprintRoot.Instance.Progression.CharacterRaces;
            var racelist = BlueprintRoot.Instance.Progression.CharacterRaces.ToArray<BlueprintRace>();
            ///var index = Array.FindIndex(racelist, (race) => race == currentRace);
            var index = Array.FindIndex(racelist, (race) => race == currentRace);
            GUILayout.BeginHorizontal();
            ChooseFromList2("Race", racelist, ref index, () => {
                doll.SetRace(racelist[index]);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                CharacterManager.RebuildCharacter(unitEntityData);
            });
            GUILayout.Label(" " + racelist[index].Name);
            GUILayout.EndHorizontal();
        }
       /* static void ChooseRace(UnitEntityData unitEntityData, DollState doll)
        {
            var currentRace = doll.Race;
            var racess = new List<BlueprintRace> { };
            foreach(BlueprintRace race in BlueprintRoot.Instance.Progression.CharacterRaces)
            {
                racess.AddItem(race);
            }
            var races = racess.ToArray();
            foreach(BlueprintRace race in races)
            {
                Main.logger.Log(race.NameForAcronym);
            }
            /// var index = Array.FindIndex(races, (race) => race == currentRace);
            var index = 1;
            GUILayout.BeginHorizontal();
            ChooseFromList("Race", races, ref index, () => {
                doll.SetRace(races[index]);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                CharacterManager.RebuildCharacter(unitEntityData);
            });
            GUILayout.Label(" " + races[index].Name);
            GUILayout.EndHorizontal();
        }*/



        static void ChooseVisualPreset(UnitEntityData unitEntityData, DollState doll, string label, BlueprintRaceVisualPreset[] presets,
            BlueprintRaceVisualPreset currentPreset)
        {
            var index = Array.FindIndex(presets, (vp) => vp == currentPreset);
            ChooseFromList(label, presets, ref index, () => {
                doll.SetRacePreset(presets[index]);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                CharacterManager.RebuildCharacter(unitEntityData);
            });
        }
        static void ChooseDoll(UnitEntityData unitEntityData)
        {
            try
            {
                if (!unitEntityData.IsMainCharacter && !unitEntityData.IsCustomCompanion() && GUILayout.Button("Destroy Doll", GUILayout.Width(DefaultLabelWidth)))
                {
                    unitEntityData.Descriptor.Doll = null;
                    unitEntityData.Descriptor.ForcceUseClassEquipment = false;
                    CharacterManager.RebuildCharacter(unitEntityData);
                }
                var doll = DollResourcesManager.GetDoll(unitEntityData);
                var race = doll.Race;
                var gender = unitEntityData.Gender;
                CustomizationOptions customizationOptions = gender != Gender.Male ? race.FemaleOptions : race.MaleOptions;
                ChooseRace(unitEntityData, doll);
                ChooseEEL(unitEntityData, doll, "Face", customizationOptions.Heads, doll.Head.m_Link, (EquipmentEntityLink ee) => doll.SetHead(ee));
                ChooseEEL(unitEntityData, doll, "Hair", customizationOptions.Hair, doll.Hair.m_Link, (EquipmentEntityLink ee) => doll.SetHair(ee));
                if (customizationOptions.Beards.Length > 0) ChooseEEL(unitEntityData, doll, "Beards", customizationOptions.Beards, doll.Beard.m_Link, (EquipmentEntityLink ee) => doll.SetBeard(ee));
                if (customizationOptions.Horns.Length > 0) ChooseEEL(unitEntityData, doll, "Horns", customizationOptions.Horns, doll.Horn.m_Link, (EquipmentEntityLink ee) => doll.SetHorn(ee));
                ChooseRamp(unitEntityData, doll, "Hair Color", doll.GetHairRamps(), doll.HairRampIndex, (int index) => doll.SetHairColor(index));
                ChooseRamp(unitEntityData, doll, "Skin Color", doll.GetSkinRamps(), doll.SkinRampIndex, (int index) => doll.SetSkinColor(index));
                ChooseRamp(unitEntityData, doll, "Horn Color", doll.GetHornsRamps(), doll.HornsRampIndex, (int index) => doll.SetHornsColor(index));
                ChooseRamp(unitEntityData, doll, "Primary Outfit Color", doll.GetOutfitRampsPrimary(), doll.EquipmentRampIndex, (int index) => doll.SetEquipColors(index, doll.EquipmentRampIndexSecondary));
                ChooseRamp(unitEntityData, doll, "Secondary Outfit Color", doll.GetOutfitRampsSecondary(), doll.EquipmentRampIndexSecondary, (int index) => doll.SetEquipColors(doll.EquipmentRampIndex, index));
                ReferenceArrayProxy<BlueprintRaceVisualPreset, BlueprintRaceVisualPresetReference> presets = doll.Race.Presets;
                BlueprintRaceVisualPreset racePreset = doll.RacePreset;
                /*if (unitEntityData.Descriptor.LeftHandedOverride == true && GUILayout.Button("Set Right Handed", GUILayout.Width(DefaultLabelWidth)))
                {
                    unitEntityData.Descriptor.LeftHandedOverride = false;
                    unitEntityData.Descriptor.Doll = doll.CreateData();
                    ViewManager.ReplaceView(unitEntityData, null);
                    unitEntityData.View.HandsEquipment.HandleEquipmentSetChanged();
                }
                else if (unitEntityData.Descriptor.LeftHandedOverride == false && GUILayout.Button("Set Left Handed", GUILayout.Width(DefaultLabelWidth)))
                {
                    unitEntityData.Descriptor.LeftHandedOverride = true;
                    unitEntityData.Descriptor.Doll = doll.CreateData();
                    ViewManager.ReplaceView(unitEntityData, null);
                    unitEntityData.View.HandsEquipment.HandleEquipmentSetChanged();
                }*/
                ChoosePortrait(unitEntityData);
                if (unitEntityData.IsMainCharacter || unitEntityData.IsCustomCompanion()) ChooseAsks(unitEntityData);
            }
            catch(Exception e) { Main.logger.Log(e.ToString()); }
        }

        static void ChooseCompanionColor(CharacterSettings characterSettings, UnitEntityData unitEntityData)
        {
            if (GUILayout.Button("Create Doll", GUILayout.Width(DefaultLabelWidth)))
            {
                var race = unitEntityData.Descriptor.Progression.Race;
                var options = unitEntityData.Descriptor.Gender == Gender.Male ? race.MaleOptions : race.FemaleOptions;
                var dollState = new DollState();
                dollState.SetRace(unitEntityData.Descriptor.Progression.Race); //Race must be set before class
                                                                               //This is a hack to work around harmony not allowing calls to the unpatched method
                CharacterManager.disableEquipmentClassPatch = true;
                dollState.SetClass(unitEntityData.Descriptor.Progression.GetEquipmentClass());
                CharacterManager.disableEquipmentClassPatch = false;
                dollState.SetGender(unitEntityData.Descriptor.Gender);
                dollState.SetRacePreset(race.Presets[0]);
                unitEntityData.Descriptor.LeftHandedOverride = false;
                if (options.Hair.Length > 0) dollState.SetHair(options.Hair[0]);
                if (options.Heads.Length > 0) dollState.SetHead(options.Hair[0]);
                if (options.Beards.Length > 0) dollState.SetBeard(options.Hair[0]);
                dollState.Validate();
                unitEntityData.Descriptor.Doll = dollState.CreateData();
                unitEntityData.Descriptor.ForcceUseClassEquipment = true;
                CharacterManager.RebuildCharacter(unitEntityData);
            }
            GUILayout.Label("Note: Colors only applies to non-default outfits, the default companion custom voice is None");
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Primary Outfit Color ", GUILayout.Width(DefaultLabelWidth));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(characterSettings.companionPrimary, -1, 35, GUILayout.Width(DefaultSliderWidth)), 0);
                GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (newIndex != characterSettings.companionPrimary)
                {
                    characterSettings.companionPrimary = newIndex;
                    CharacterManager.UpdateModel(unitEntityData.View);
                }
            }
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Secondary Outfit Color ", GUILayout.Width(DefaultLabelWidth));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(characterSettings.companionSecondary, -1, 35, GUILayout.Width(DefaultSliderWidth)), 0);
                GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (newIndex != characterSettings.companionSecondary)
                {
                    characterSettings.companionSecondary = newIndex;
                    CharacterManager.UpdateModel(unitEntityData.View);
                }
            }
            ChoosePortrait(unitEntityData);
            ChooseAsks(unitEntityData);
        }
        static void ChooseToggle(string label, ref bool currentValue, Action onChoose)
        {
            bool newValue = GUILayout.Toggle(currentValue, label, GUILayout.ExpandWidth(false));
            if (newValue != currentValue)
            {
                currentValue = newValue;
                onChoose();
            }
        }
        static void ChooseEquipment(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            void onHideEquipment()
            {
                CharacterManager.RebuildCharacter(unitEntityData);
                CharacterManager.UpdateModel(unitEntityData.View);
            }
            void onHideBuff()
            {
                foreach (var buff in unitEntityData.Buffs) buff.ClearParticleEffect();
                unitEntityData.SpawnBuffsFxs();
            }
            void onWeaponChanged()
            {
               /// if(unitEntityData.View.HandsEquipment.GetWeaponModel(false). != characterSettings.overrideWeapons)
                unitEntityData.View.HandsEquipment.UpdateAll();
            }
            ChooseToggle("Hide Cap", ref characterSettings.hideCap, onHideEquipment);
            ChooseToggle("Hide Helmet", ref characterSettings.hideHelmet, onHideEquipment);
            ChooseToggle("Hide Glasses", ref characterSettings.hideGlasses, onHideEquipment);
            ChooseToggle("Hide Shirt", ref characterSettings.hideShirt, onHideEquipment);
            ChooseToggle("Hide Class Backpack", ref characterSettings.hideClassCloak, onHideEquipment);
            ///ChooseToggle("Hide Item Cloak", ref characterSettings.hideItemCloak, onHideEquipment);
            ChooseToggle("Hide Armor", ref characterSettings.hideArmor, onHideEquipment);
            ChooseToggle("Hide Bracers", ref characterSettings.hideBracers, onHideEquipment);
            ChooseToggle("Hide Gloves", ref characterSettings.hideGloves, onHideEquipment);
            ChooseToggle("Hide Boots", ref characterSettings.hideBoots, onHideEquipment);
            ChooseToggle("Hide Inactive Weapons", ref characterSettings.hideWeapons, onWeaponChanged);
            ChooseToggle("Hide Belt Slots", ref characterSettings.hideBeltSlots, onWeaponChanged);
            ChooseToggle("Hide Quiver", ref characterSettings.hideQuiver, onWeaponChanged);
            ChooseToggle("Hide Weapon Enchantments", ref characterSettings.hideWeaponEnchantments, onWeaponChanged);
            ChooseToggle("Hide Wings", ref characterSettings.hideWings, onHideBuff);
            ChooseToggle("Hide Horns", ref characterSettings.hideHorns, onHideEquipment);
            ChooseToggle("Hide Tail", ref characterSettings.hideTail, onHideEquipment);
        }

        /*
         * m_Size is updated from GetSizeScale (EntityData.Descriptor.State.Size) and 
         * is with m_OriginalScale to adjust the transform.localScale 
         * Adjusting GetSizeScale will effect character corpulence and cause gameplay sideeffects
         * Changing m_OriginalScale will effect ParticlesSnapMap.AdditionalScale
         */
        static void ChooseSizeAdditive(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Additive Scale Factor", GUILayout.Width(300));
            var sizeModifier = GUILayout.HorizontalSlider(characterSettings.additiveScaleFactor, -4, 4, GUILayout.Width(DefaultSliderWidth));
            if (!characterSettings.overrideScaleFloatMode) sizeModifier = (int)sizeModifier;
            characterSettings.additiveScaleFactor = sizeModifier;
            var sign = sizeModifier >= 0 ? "+" : "";
            GUILayout.Label($" {sign}{sizeModifier:0.##}", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
        static void ChooseSizeOverride(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Override Scale Factor", GUILayout.Width(300));
            var sizeModifier = GUILayout.HorizontalSlider(characterSettings.overrideScaleFactor, 0, 8, GUILayout.Width(DefaultSliderWidth));
            if (!characterSettings.overrideScaleFloatMode) sizeModifier = (int)sizeModifier;
            characterSettings.overrideScaleFactor = sizeModifier;
            GUILayout.Label($" {(Size)(sizeModifier)}", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
        static void ChooseEquipmentOverride(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            void onEquipment()
            {
                CharacterManager.RebuildCharacter(unitEntityData);
                CharacterManager.UpdateModel(unitEntityData.View);
            }
            GUILayout.Label("Equipment", "box", GUILayout.Width(DefaultLabelWidth));
            void onView() => ViewManager.ReplaceView(unitEntityData, characterSettings.overrideView);
            Util.ChooseSlider("Override Helm", EquipmentResourcesManager.Helm, ref characterSettings.overrideHelm, onEquipment);
            Util.ChooseSlider("Override Shirt", EquipmentResourcesManager.Shirt, ref characterSettings.overrideShirt, onEquipment);
            Util.ChooseSlider("Override Glasses", EquipmentResourcesManager.Glasses, ref characterSettings.overrideGlasses, onEquipment);
            Util.ChooseSlider("Override Armor", EquipmentResourcesManager.Armor, ref characterSettings.overrideArmor, onEquipment);
            Util.ChooseSlider("Override Bracers", EquipmentResourcesManager.Bracers, ref characterSettings.overrideBracers, onEquipment);
            Util.ChooseSlider("Override Gloves", EquipmentResourcesManager.Gloves, ref characterSettings.overrideGloves, onEquipment);
            Util.ChooseSlider("Override Boots", EquipmentResourcesManager.Boots, ref characterSettings.overrideBoots, onEquipment);
            Util.ChooseSlider("Override Tattoos", EquipmentResourcesManager.Tattoos, ref characterSettings.overrideTattoo, onEquipment);
            GUILayout.Label("Weapons", "box", GUILayout.Width(DefaultLabelWidth));
            foreach (var kv in EquipmentResourcesManager.Weapons)
            {
                var animationStyle = kv.Key;
                var weaponLookup = kv.Value;
                characterSettings.overrideWeapons.TryGetValue(animationStyle, out BlueprintRef currentValue);
                void onWeapon()
                {
                    characterSettings.overrideWeapons[animationStyle] = currentValue;
                    unitEntityData.View.HandsEquipment.UpdateAll();
                }
                Util.ChooseSlider($"Override {animationStyle} ", weaponLookup, ref currentValue, onWeapon);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Main Weapon Enchantments", "box", GUILayout.Width(DefaultLabelWidth));
            if (GUILayout.Button("Add Enchantment", GUILayout.ExpandWidth(false)))
            {
                characterSettings.overrideMainWeaponEnchantments.Add(null);
            }
            GUILayout.EndHorizontal();
            void onWeaponEnchantment()
            {
                unitEntityData.View.HandsEquipment.UpdateAll();
            }
            for (int i = 0; i < characterSettings.overrideMainWeaponEnchantments.Count; i++)
            {
                Util.ChooseSliderList($"Override Main Hand", EquipmentResourcesManager.WeaponEnchantments,
                    characterSettings.overrideMainWeaponEnchantments, i, onWeaponEnchantment);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Offhand Weapon Enchantments", "box", GUILayout.Width(DefaultLabelWidth));
            if (GUILayout.Button("Add Enchantment", GUILayout.ExpandWidth(false)))
            {
                characterSettings.overrideOffhandWeaponEnchantments.Add("");
            }
            GUILayout.EndHorizontal();
            for (int i = 0; i < characterSettings.overrideOffhandWeaponEnchantments.Count; i++)
            {
                Util.ChooseSliderList($"Override Off Hand", EquipmentResourcesManager.WeaponEnchantments,
                    characterSettings.overrideOffhandWeaponEnchantments, i, onWeaponEnchantment);
            }
            GUILayout.Label("View", "box", GUILayout.Width(DefaultLabelWidth));
            Util.ChooseSlider("Override View", EquipmentResourcesManager.Units, ref characterSettings.overrideView, onView);
            void onChooseScale()
            {
                HarmonyLib.Traverse.Create(unitEntityData.View).Field("m_Scale").SetValue(unitEntityData.View.GetSizeScale() + 0.01f);
            }
            GUILayout.Label("Scale", "box", GUILayout.Width(DefaultLabelWidth));
            GUILayout.BeginHorizontal();
            ChooseToggle("Enable Override Scale", ref characterSettings.overrideScale, onChooseScale);
            ChooseToggle("Restrict to polymorph", ref characterSettings.overrideScaleShapeshiftOnly, onChooseScale);
            ChooseToggle("Use Additive Factor", ref characterSettings.overrideScaleAdditive, onChooseScale);
            ChooseToggle("Use Cheat Mode", ref characterSettings.overrideScaleCheatMode, onChooseScale);
            ChooseToggle("Use Continuous Factor", ref characterSettings.overrideScaleFloatMode, onChooseScale);
            GUILayout.Space(10f);
            GUILayout.EndHorizontal();
            if (characterSettings.overrideScale && characterSettings.overrideScaleAdditive) ChooseSizeAdditive(unitEntityData, characterSettings);
            if (characterSettings.overrideScale && !characterSettings.overrideScaleAdditive) ChooseSizeOverride(unitEntityData, characterSettings);
        }
    }

}
