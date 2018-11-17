using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.ResourceLinks;
using Kingmaker.View.Equipment;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.Visual.Decals;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Sound;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace VisualAdjustments
{
    public class InfoManager
    {
        public class EquipmentEntityInfo
        {
            public string type = "Unknown";
            public string raceGenderCombos = "";
            public EquipmentEntityLink eel = null;
            public bool expanded = false;
        }
        static readonly Dictionary<string, EquipmentEntityInfo> lookup = new Dictionary<string, EquipmentEntityInfo>();
        static bool loaded = false;
        static bool showWeapons = false;
        static bool showArmor = false;
        static bool showFx = false;
        static bool showAsks = false;
        static bool showPortrait = false;
        static void AddLinks(EquipmentEntityLink[] links, string type, Race race, Gender gender)
        {
            foreach (var link in links)
            {
                var ee = link.Load();
                if (lookup.ContainsKey(ee.name))
                {
                    lookup[ee.name].raceGenderCombos += ", " + race + gender;
                }
                else
                {
                    lookup[ee.name] = new EquipmentEntityInfo
                    {
                        type = type,
                        raceGenderCombos = "" + race + gender,
                        eel = link
                    };
                }
            }
        }
        static void Init()
        {
            var races = ResourcesLibrary.GetBlueprints<BlueprintRace>();
            var racePresets = ResourcesLibrary.GetBlueprints<BlueprintRaceVisualPreset>();
            var classes = ResourcesLibrary.GetBlueprints<BlueprintCharacterClass>();
            foreach (var race in races)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    CustomizationOptions customizationOptions = gender != Gender.Male ? race.FemaleOptions : race.MaleOptions;
                    AddLinks(customizationOptions.Heads, "Head", race.RaceId, gender);
                    AddLinks(customizationOptions.Hair, "Hair", race.RaceId, gender);
                    AddLinks(customizationOptions.Beards, "Beards", race.RaceId, gender);
                    AddLinks(customizationOptions.Eyebrows, "Eyebrows", race.RaceId, gender);
                }
            }
            foreach (var racePreset in racePresets)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    var raceSkin = racePreset.Skin;
                    if (raceSkin == null) continue;
                    AddLinks(raceSkin.GetLinks(gender, racePreset.RaceId), "Skin", racePreset.RaceId, gender);
                }
            }
            foreach (var _class in classes)
            {
                foreach (var race in races)
                {
                    foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                    {
                        AddLinks(_class.GetClothesLinks(gender, race.RaceId).ToArray(), "ClassOutfit", race.RaceId, gender);
                    }
                }
            }
            var gear = ResourcesLibrary.GetBlueprints<KingmakerEquipmentEntity>();
            foreach (var race in races)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    foreach (var kee in gear)
                    {
                        AddLinks(kee.GetLinks(gender, race.RaceId), "Armor", race.RaceId, gender);
                    }
                }
            }
            loaded = true;
        }
        public static void ShowInfo(UnitEntityData unitEntityData)
        {
            if (!loaded) Init();
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            if (GUILayout.Button("Rebuild Character"))
            {
                CharacterManager.RebuildCharacter(unitEntityData);
            }
            if (GUILayout.Button("Rebuild Outfit"))
            {
                var bakedCharacter = unitEntityData.View.CharacterAvatar.BakedCharacter;
                unitEntityData.View.CharacterAvatar.BakedCharacter = null;
                unitEntityData.View.CharacterAvatar.RebuildOutfit();
                unitEntityData.View.CharacterAvatar.BakedCharacter = bakedCharacter;
            }
            if (GUILayout.Button("Remove Outfit"))
            {
                unitEntityData.View.CharacterAvatar.RemoveOutfit();
            }
            if (GUILayout.Button("Update Class Equipment"))
            {
                var bakedCharacter = unitEntityData.View.CharacterAvatar.BakedCharacter;
                unitEntityData.View.CharacterAvatar.BakedCharacter = null;
                bool useClassEquipment = unitEntityData.Descriptor.ForcceUseClassEquipment;
                unitEntityData.Descriptor.ForcceUseClassEquipment = true;
                unitEntityData.View.UpdateClassEquipment();
                unitEntityData.Descriptor.ForcceUseClassEquipment = useClassEquipment;
                unitEntityData.View.CharacterAvatar.BakedCharacter = bakedCharacter;
            }
            if (GUILayout.Button("Update Body Equipment"))
            {
                var bakedCharacter = unitEntityData.View.CharacterAvatar.BakedCharacter;
                unitEntityData.View.CharacterAvatar.BakedCharacter = null;
                unitEntityData.View.UpdateBodyEquipmentModel();
                unitEntityData.View.CharacterAvatar.BakedCharacter = bakedCharacter;
            }
            if (GUILayout.Button("Mark Dirty"))
            {
                unitEntityData.View.CharacterAvatar.IsDirty = true;
            }
            if (GUILayout.Button("Update Model"))
            {
                CharacterManager.UpdateModel(unitEntityData.View);
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            showArmor = GUILayout.Toggle(showArmor, "Show Armor");
            showWeapons = GUILayout.Toggle(showWeapons, "Show Weapons");
            showFx = GUILayout.Toggle(showFx, "Show FX");
            showPortrait = GUILayout.Toggle(showPortrait, "Show Portrait");
            showAsks = GUILayout.Toggle(showAsks, "Show Asks");
            GUILayout.EndHorizontal();
            if (showArmor) ShowArmorInfo(unitEntityData);
            if (showWeapons) ShowWeaponInfo(unitEntityData);
            if (showFx) ShowFxInfo(unitEntityData);
            if (showPortrait) ShowPortraitInfo(unitEntityData);
            if (showAsks) ShowAsksInfo(unitEntityData);

        }
        static void ShowArmorInfo(UnitEntityData unitEntityData)
        {
            var character = unitEntityData.View.CharacterAvatar;
            GUILayout.Label("Equipment", GUILayout.Width(300));
            foreach (var ee in character.EquipmentEntities.ToArray())
            {
                EquipmentEntityInfo settings = lookup.ContainsKey(ee.name) ? lookup[ee.name] : new EquipmentEntityInfo();
                GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                GUILayout.Label(
                        String.Format("{0}:{1}:{2}:{3}:P{4}:S{5}", ee.name, settings.type, ee.BodyParts.Count, ee.OutfitParts.Count, 
                            character.GetPrimaryRampIndex(ee), character.GetSecondaryRampIndex(ee)),
                        GUILayout.ExpandWidth(false));
                if (GUILayout.Button("Remove"))
                {
                    character.RemoveEquipmentEntity(ee);
                }
                if (GUILayout.Button("Log Parts"))
                {
                    LogEquipmentEntity(ee, settings.raceGenderCombos);
                }
                settings.expanded = GUILayout.Toggle(settings.expanded, "Expand", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (settings.expanded)
                {
                    foreach (var bodypart in ee.BodyParts.ToArray())
                    {
                        GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                        GUILayout.Label(String.Format(" BP {0}:{1}", bodypart.RendererPrefab.name, bodypart.Type), GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("Remove"))
                        {
                            ee.BodyParts.Remove(bodypart);
                        }
                        GUILayout.EndHorizontal();
                    }
                    foreach (var outfitpart in ee.OutfitParts.ToArray())
                    {
                        GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                        var prefab = Traverse.Create(outfitpart).Field("m_Prefab").GetValue<GameObject>();
                        GUILayout.Label(String.Format(" OP {0}:{1}", prefab?.name, outfitpart.Special), GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("Remove"))
                        {
                            ee.OutfitParts.Remove(outfitpart);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }
        static void ShowAsksInfo(UnitEntityData unitEntityData)
        {
            var asks = unitEntityData.Descriptor.Asks;
            var customAsks = unitEntityData.Descriptor.CustomAsks;
            var overrideAsks = unitEntityData.Descriptor.OverrideAsks;
            GUILayout.Label($"Current Asks: {asks?.name}, Display: {asks?.DisplayName}");
            GUILayout.Label($"Current CustomAsks: {customAsks?.name}, Display: {customAsks?.DisplayName}");
            GUILayout.Label($"Current OverrideAsks: {overrideAsks?.name}, Display: {overrideAsks?.DisplayName}");
            foreach (var blueprint in ResourcesLibrary.GetBlueprints<BlueprintUnitAsksList>())
            {
                GUILayout.Label($"Asks: {blueprint}, Display: {blueprint.DisplayName}");
            }

        }
        static void ShowPortraitInfo(UnitEntityData unitEntityData)
        {
            var portrait = unitEntityData.Descriptor.Portrait;
            var portraitBP = unitEntityData.Descriptor.UISettings.PortraitBlueprint;
            var uiPortrait = unitEntityData.Descriptor.UISettings.Portrait;
            var CustomPortrait = unitEntityData.Descriptor.UISettings.CustomPortrait;
            GUILayout.Label($"Portrait Blueprint: {portraitBP}, {portraitBP?.name}");
            GUILayout.Label($"Descriptor Portrait: {portrait}, isCustom {portrait?.IsCustom}");
            GUILayout.Label($"UI Portrait: {portrait}, isCustom {portrait?.IsCustom}");
            GUILayout.Label($"Custom Portrait: {portrait}, isCustom {portrait?.IsCustom}");
            foreach (var blueprint in DollResourcesManager.Portrait.Values)
            {
                GUILayout.Label($"Portrait Blueprint: {blueprint}");
            }
        }
        static void ShowHandslotInfo(HandSlot handSlot)
        {
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            var pItem = handSlot != null && handSlot.HasItem ? handSlot.Item : null;
            GUILayout.Label(string.Format("Slot {0}, {1}, Active {2}", 
                pItem?.Name, pItem?.GetType(), handSlot?.Active), GUILayout.Width(500));
            /*if (GUILayout.Button("Active"))
            {
                Traverse.Create(handSlot).Property("Active").SetValue(!handSlot.Active);
            }
            if (GUILayout.Button("Disabled"))
            {
                Traverse.Create(handSlot).Property("Disabled").SetValue(!handSlot.Disabled);
            }*/
            if (GUILayout.Button("Remove"))
            {
                handSlot.RemoveItem();
            }
            GUILayout.EndHorizontal();
        }
            static void ShowUnitViewHandSlotData(UnitViewHandSlotData handData)
        {
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            
            GUILayout.Label(string.Format("Data {0} Slot {1} Active {2}", handData?.VisibleItem?.Name, handData?.VisualSlot, handData?.IsActiveSet), GUILayout.Width(500));
            if (GUILayout.Button("Unequip"))
            {
                handData.Unequip();
            }
            if (GUILayout.Button("Swap Slot"))
            {
                handData.VisualSlot += 1;
                if(handData.VisualSlot == UnitEquipmentVisualSlotType.Quiver) handData.VisualSlot = 0;
                handData.Owner.View.HandsEquipment.UpdateAll();
            }
            if (GUILayout.Button("ShowItem 0"))
            {
                handData.ShowItem(false);
            }
            if (GUILayout.Button("ShowItem 1"))
            {
                handData.ShowItem(true);
            }
            GUILayout.EndHorizontal();
        }
        static void ShowWeaponInfo(UnitEntityData unitEntityData)
        {
            GUILayout.Label("Weapons", GUILayout.Width(300));
            var hands = unitEntityData.View.HandsEquipment;
            foreach (var kv in hands.Sets)
            {
                ShowHandslotInfo(kv.Key.PrimaryHand);
                ShowUnitViewHandSlotData(kv.Value.MainHand);
                ShowHandslotInfo(kv.Key.SecondaryHand);
                ShowUnitViewHandSlotData(kv.Value.OffHand);
            }
        }
        static void LogEquipmentEntity(EquipmentEntity ee, string raceGenderCombo)
        {
            Main.DebugLog(string.Format("\tee: {0}", ee.name, raceGenderCombo));
            foreach (var bodypart in ee.BodyParts)
            {
                var renderer = bodypart.SkinnedRenderer;
                Main.DebugLog(string.Format("\t\tbodypart: {0}:{1}:{2}", bodypart.ToString(), bodypart.Type, renderer?.ToString()));
            }
            foreach (var outfitpart in ee.OutfitParts)
            {
                var go = Traverse.Create(outfitpart).Field("m_Prefab").GetValue<GameObject>();
                Main.DebugLog(string.Format("\t\toutfitpart: {0}:{1}:{2}", outfitpart.ToString(), outfitpart.Special, go));
            }
            //Main.DebugLog(JsonUtility.ToJson(ee));
        }
        //Refer FxHelper.SpawnFxOnGameObject
        static void ShowFxInfo(UnitEntityData unitEntityData)
        {
            GUILayout.Label("Global");
            for (int i = FxHelper.FxRoot.childCount - 1; i >= 0; i--)
            {
                var fx = FxHelper.FxRoot.GetChild(i);
                GUILayout.BeginHorizontal();
                GUILayout.Label("FX: " + fx.name, GUILayout.Width(400));
                if (GUILayout.Button("Destroy"))
                {
                    GameObject.Destroy(fx.gameObject);
                }
                GUILayout.EndHorizontal();
            }

            var spawnOnStart = unitEntityData.View.GetComponent<SpawnFxOnStart>();
            if (spawnOnStart)
            {
                GUILayout.Label("Spawn on Start");
                GUILayout.Label("FxOnStart " + spawnOnStart.FxOnStart?.Load()?.name, GUILayout.Width(400));
                GUILayout.Label("FXFxOnDeath " + spawnOnStart.FxOnStart?.Load()?.name, GUILayout.Width(400));
            }
            GUILayout.Label("Decals");
            var decals = Traverse.Create(unitEntityData.View).Field("m_Decals").GetValue<List<FxDecal>>();
            for (int i = decals.Count - 1; i >= 0; i--)
            {
                var decal = decals[i];
                GUILayout.Label("Decal: " + decal.name, GUILayout.Width(400));
                if (GUILayout.Button("Destroy"))
                {
                    GameObject.Destroy(decal.gameObject);
                    decals.RemoveAt(i);
                }
            }
        }
    }
}