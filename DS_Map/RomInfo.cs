using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using DSPRE.Resources;
using System;
using DSPRE.ROMFiles;

namespace DSPRE {

    /// <summary>
    /// Class to store ROM data from GEN IV Pok�mon games
    /// </summary>

    public class RomInfo {
        private static string folderSuffix = "_DSPRE_contents";
        public static string romID { get; private set; }
        public static string fileName { get; private set; }
        public static string workDir { get; private set; }
        public static string arm9Path { get; private set; }
        public static string overlayTablePath { get; set; }
        public static string overlayPath { get; set; }
        public static string gameVersion { get; private set; }
        public static string gameLanguage { get; private set; }
        public static string gameFamily { get; private set; }
        public static string gameName { get; private set; }

        public static uint arm9spawnOffset { get; private set; }
        public static int initialMoneyOverlayNumber { get; private set; }
        public static uint initialMoneyOverlayOffset { get; private set; }

        public static int cameraTblOverlayNumber { get; private set; }
        public static uint[] cameraTblOffsetsToRAMaddress { get; private set; }

        public static uint headerTableOffset { get; private set; }
        public static uint conditionalMusicTableOffsetToRAMAddress { get; internal set; }
        public static uint OWTableOffset { get; internal set; }
        public static string OWtablePath { get; private set; }

        public static int nullEncounterID { get; private set; }
        public static int attackNamesTextNumber { get; private set; }
        public static int[] pokemonNamesTextNumbers { get; private set; }
        public static int itemNamesTextNumber { get; private set; }
        public static int itemScriptFileNumber { get; internal set; }
        public static int trainerClassMessageNumber { get; private set; }
        public static int trainerNamesMessageNumber { get; private set; }
        public static int locationNamesTextNumber { get; private set; }

        public static string internalNamesLocation { get; private set; }
        public static readonly byte internalNameLength = 16;
        public static int cameraSize { get; private set; }

        public static Dictionary<List<uint>, (Color background, Color foreground)> MapCellsColorDictionary { get; private set; }
        public static Dictionary<ushort, string> ScriptCommandNamesDict { get; private set; }
        public static Dictionary<ushort, byte[]> ScriptCommandParametersDict { get; private set; }
        public static Dictionary<ushort, string> ScriptActionNamesDict { get; private set; }
        public static SortedDictionary<uint, (uint spriteID, ushort properties)> OverworldTable { get; private set; }
        public static uint[] overworldTableKeys { get; private set; }
        public static Dictionary<uint, string> ow3DSpriteDict { get; private set; }

        public enum DirNames : byte {
            synthOverlay,            
            dynamicHeaders,
            
            textArchives,
            matrices,

            maps,
            exteriorBuildingModels,
            buildingConfigFiles,
            buildingTextures,
            mapTextures,
            areaData,

            eventFiles,
            OWSprites,

            scripts,
            encounters,

            trainerProperties,
            trainerParty,
            trainerGraphics,

            interiorBuildingModels
        };
        public static Dictionary<DirNames, (string packedDir, string unpackedDir)> gameDirs { get; private set; }


        #region Constructors (1)
        public RomInfo(string id, string romName, bool useSuffix = true) {
            romID = id;
            fileName = romName;

            if (!useSuffix) {
                folderSuffix = "";
            }

            workDir = Path.GetDirectoryName(romName) + "\\" + Path.GetFileNameWithoutExtension(romName) + folderSuffix + "\\";
            arm9Path = workDir + @"arm9.bin";
            overlayTablePath = workDir + @"y9.bin";
            overlayPath = workDir + "overlay";
            internalNamesLocation = workDir + @"data\fielddata\maptable\mapname.bin";

            LoadGameVersion();
            if (gameVersion is null) {
                return;
            }

            LoadGameFamily();
            LoadGameName();
            LoadGameLanguage();

            SetNarcDirs();
            SetHeaderTableOffset();
            SetNullEncounterID();

            SetAttackNamesTextNumber();
            SetPok�monNamesTextNumber();
            SetItemNamesTextNumber();
            SetItemScriptFileNumber();
            SetLocationNamesTextNumber();
            SetTrainerNamesMessageNumber();
            SetTrainerClassMessageNumber();

            /* System */
            ScriptCommandNamesDict = BuildCommandNamesDatabase(gameVersion);
            ScriptCommandParametersDict = BuildCommandParametersDatabase(gameVersion);
            ScriptActionNamesDict = BuildActionNamesDatabase(gameVersion);
        }
        #endregion

        #region Methods (22)
        public static Dictionary<ushort, string> BuildCommandNamesDatabase(string gameVer) {
            Dictionary<ushort, string> commonDictionaryNames;
            Dictionary<ushort, string> specificDictionaryNames;

            switch (gameFamily) {
                case "DP":
                    commonDictionaryNames = PokeDatabase.ScriptEditor.DPPtScrCmdNames;
                    specificDictionaryNames = PokeDatabase.ScriptEditor.DPScrCmdNames;
                    break;
                case "Plat":
                    commonDictionaryNames = PokeDatabase.ScriptEditor.DPPtScrCmdNames;
                    specificDictionaryNames = PokeDatabase.ScriptEditor.PlatScrCmdNames;
                    break;
                default:
                    commonDictionaryNames = PokeDatabase.ScriptEditor.HGSSScrCmdNames;
                    specificDictionaryNames = PokeDatabase.ScriptEditor.CustomScrCmdNames;
                    break;
            }
            return commonDictionaryNames.Concat(specificDictionaryNames).ToLookup(x => x.Key, x => x.Value).ToDictionary(x => x.Key, g => g.First());
        }
        public static Dictionary<ushort, byte[]> BuildCommandParametersDatabase(string gameVer) {
            Dictionary<ushort, byte[]> commonDictionaryParams;
            Dictionary<ushort, byte[]> specificDictionaryParams;

            switch (gameFamily) {
                case "DP":
                    commonDictionaryParams = PokeDatabase.ScriptEditor.DPPtScrCmdParameters;
                    specificDictionaryParams = PokeDatabase.ScriptEditor.DPScrCmdParameters;
                    break;
                case "Plat":
                    commonDictionaryParams = PokeDatabase.ScriptEditor.DPPtScrCmdParameters;
                    specificDictionaryParams = PokeDatabase.ScriptEditor.PlatScrCmdParameters;
                    break;
                default:
                    commonDictionaryParams = PokeDatabase.ScriptEditor.HGSSScrCmdParameters;
                    specificDictionaryParams = PokeDatabase.ScriptEditor.CustomScrCmdParameters;
                    break;
            }
            return commonDictionaryParams.Concat(specificDictionaryParams).ToLookup(x => x.Key, x => x.Value).ToDictionary(x => x.Key, g => g.First());
        }
        public static Dictionary<ushort, string> BuildActionNamesDatabase(string gameVer) {
            switch (gameFamily) {
                case "DP":
                case "Plat":
                    return PokeDatabase.ScriptEditor.movementsDictIDName;
                default:
                    var commonDictionaryParams = PokeDatabase.ScriptEditor.movementsDictIDName;
                    var customDictionaryParams = PokeDatabase.ScriptEditor.customMovementsDictIDName;
                    return commonDictionaryParams.Concat(customDictionaryParams).ToLookup(x => x.Key, x => x.Value).ToDictionary(x => x.Key, g => g.First());
            }
        }
        public static void Set3DOverworldsDict() {
            ow3DSpriteDict = new Dictionary<uint, string>() {
                [91] = "brown_sign",
                [92] = "red_sign",
                [93] = "gray_sign",
                [94] = "route_sign",
                [95] = "blue_sign", //to fix this one (gym_sign)
                [96] = "blue_sign",
                [101] = "dawn_platinum",
                //[174] = "dppt_suitcase",
            };
        }
        public static void SetHeaderTableOffset() {
            switch (gameFamily) {
                case "DP":
                    switch (gameLanguage) {
                        case "ENG":
                            headerTableOffset = 0xEEDBC;
                            break;
                        case "ESP":
                            headerTableOffset = 0xEEE08;
                            break;
                        case "ITA":
                            headerTableOffset = 0xEED70;
                            break;
                        case "FRA":
                            headerTableOffset = 0xEEDFC;
                            break;
                        case "GER":
                            headerTableOffset = 0xEEDCC;
                            break;
                        case "JAP":
                            headerTableOffset = 0xF0D68;
                            break;
                    }
                    break;
                case "Plat":
                    switch (gameLanguage) {
                        case "ENG":
                            headerTableOffset = 0xE601C;
                            break;
                        case "ESP":
                            headerTableOffset = 0xE60B0;
                            break;
                        case "ITA":
                            headerTableOffset = 0xE6038;
                            break;
                        case "FRA":
                            headerTableOffset = 0xE60A4;
                            break;
                        case "GER":
                            headerTableOffset = 0xE6074;
                            break;
                        case "JAP":
                            headerTableOffset = 0xE56F0;
                            break;
                    }
                    break;
                case "HGSS":
                    switch (gameLanguage) {
                        case "ENG":
                            headerTableOffset = 0xF6BE0;
                            break;
                        case "ESP":
                            headerTableOffset = gameVersion == "HG" ? 0xF6BC8 : (uint)0xF6BD0;
                            break;
                        case "ITA":
                            headerTableOffset = 0xF6B58;
                            break;
                        case "FRA":
                            headerTableOffset = 0xF6BC4;
                            break;
                        case "GER":
                            headerTableOffset = 0xF6B94;
                            break;
                        case "JAP":
                            headerTableOffset = 0xF6390;
                            break;
                    }
                    break;
            }
        }
        public static void SetupSpawnSettings() {
            switch (gameFamily) {
                case "DP":
                    initialMoneyOverlayNumber = 52;
                    initialMoneyOverlayOffset = 0x1E4;
                    switch (gameLanguage) {
                        case "ENG":
                            arm9spawnOffset = 0xF2B9C;
                            break;
                        case "ESP":
                            arm9spawnOffset = 0xF2BE8;
                            break;
                        case "ITA":
                            arm9spawnOffset = 0xF2B50;
                            break;
                        case "FRA":
                            arm9spawnOffset = 0xF2BDC;
                            break;
                        case "GER":
                            arm9spawnOffset = 0xF2BAC;
                            break;
                        case "JAP":
                            arm9spawnOffset = 0xF4B48;
                            break;
                    }
                    break;
                case "Plat":
                    initialMoneyOverlayNumber = 57;
                    initialMoneyOverlayOffset = 0x1EC;
                    switch (gameLanguage) {
                        case "ENG":
                            arm9spawnOffset = 0xEA12C;
                            break;
                        case "ESP":
                            arm9spawnOffset = 0xEA1C0;
                            break;
                        case "ITA":
                            arm9spawnOffset = 0xEA148;
                            break;
                        case "FRA":
                            arm9spawnOffset = 0xEA1B4;
                            break;
                        case "GER":
                            arm9spawnOffset = 0xEA184;
                            break;
                        case "JAP":
                            arm9spawnOffset = 0xE9800;
                            break;
                    }
                    break;
                case "HGSS":
                    initialMoneyOverlayNumber = 36;
                    initialMoneyOverlayOffset = 0x2FC;
                    switch (gameLanguage) {
                        case "ENG":
                            arm9spawnOffset = 0xFA17C;
                            break;
                        case "ESP":
                            arm9spawnOffset = gameVersion == "HG" ? 0xFA164 : (uint)0xFA16C;
                            break;
                        case "ITA":
                            arm9spawnOffset = 0xFA0F4;
                            break;
                        case "FRA":
                            arm9spawnOffset = 0xFA160;
                            break;
                        case "GER":
                            arm9spawnOffset = 0xFA130;
                            break;
                        case "JAP":
                            arm9spawnOffset = 0xF992C;
                            break;
                    }
                    break;
            }
        }
        public static void PrepareCameraData() {
            switch (gameFamily) {
                case "DP":
                    cameraTblOverlayNumber = 5;
                    cameraTblOffsetsToRAMaddress = gameLanguage.Equals("JAP") ? (new uint[] { 0x4C50 }) : (new uint[] { 0x4908 });
                    cameraSize = 24;
                    break;
                case "Plat":
                    cameraTblOverlayNumber = 5;
                    cameraTblOffsetsToRAMaddress = new uint[] { 0x4E24 };
                    cameraSize = 24;
                    break;
                case "HGSS":
                    cameraTblOverlayNumber = 1;
                    cameraSize = 36;
                    switch (gameLanguage) {
                        case "ENG":
                        case "ESP":
                        case "FRA":
                        case "GER":
                        case "ITA":
                            cameraTblOffsetsToRAMaddress = new uint[] { 0x532C, 0x547C };
                            break;
                        case "JAP":
                            cameraTblOffsetsToRAMaddress = new uint[] { 0x5324, 0x5474 };
                            break;
                    }
                    break;
            }
        }
        public static void SetOWtable() {
            switch (gameFamily) {
                case "DP":
                    OWtablePath = workDir + "overlay" + "\\" + "overlay_0005.bin";
                    switch (gameLanguage) { // Go to the beginning of the overworld table
                        case "ENG":
                            OWTableOffset = 0x22BCC;
                            break;
                        case "JAP":
                            OWTableOffset = 0x23BB8;
                            break;
                        default:
                            OWTableOffset = 0x22B84;
                            break;
                    }
                    break;
                case "Plat":
                    OWtablePath = workDir + "overlay" + "\\" + "overlay_0005.bin";
                    switch (gameLanguage) { // Go to the beginning of the overworld table
                        case "ITA":
                            OWTableOffset = 0x2BC44;
                            break;
                        case "FRA":
                        case "ESP":
                            OWTableOffset = 0x2BC3C;
                            break;
                        case "GER":
                            OWTableOffset = 0x2BC50;
                            break;
                        case "JAP":
                            OWTableOffset = 0x2BA24;
                            break;
                        default:
                            OWTableOffset = 0x2BC34;
                            break;
                    }
                    break;
                case "HGSS":
                    OWtablePath = workDir + "overlay" + "\\" + "overlay_0001.bin";
                    OWTableOffset = 0x21BA8;
                    break;
            }
        }
        public static void SetConditionalMusicTableOffsetToRAMAddress() {
            switch (gameFamily) {
                case "HGSS":
                    switch (gameLanguage) {                            
                        case "ESP":
                            conditionalMusicTableOffsetToRAMAddress = gameVersion == "HG" ? (uint)0x667D0 : 0x667D8;
                            break;
                        case "ENG":
                        case "ITA":
                        case "FRA":
                        case "GER":
                            conditionalMusicTableOffsetToRAMAddress = 0x667D8;
                            break;
                        case "JAP":
                            conditionalMusicTableOffsetToRAMAddress = 0x66238;
                            break;
                    }
                break;
            }
        }
        private void SetItemScriptFileNumber() {
            switch (gameFamily) {
                case "DP":
                    itemScriptFileNumber = 370;
                    break;
                case "Plat":
                    itemScriptFileNumber = 404;
                    break;
                default:
                    itemScriptFileNumber = 141;
                    break;
            }
        }
        private void SetNullEncounterID() {
            switch (gameFamily) {
                case "DP":
                case "Plat":
                    nullEncounterID = ushort.MaxValue;
                    break;
                case "HGSS":
                    nullEncounterID = Byte.MaxValue;
                    break;
            }
        }
        private void SetAttackNamesTextNumber() {
            switch (gameFamily) {
                case "DP":
                    attackNamesTextNumber = 588;
                    break;
                case "Plat":
                    attackNamesTextNumber = 647;
                    break;
                default:
                    attackNamesTextNumber = gameLanguage == "JAP" ? 739 : 750;
                    break;
            }
        }
        private void SetItemNamesTextNumber() {
            switch (gameFamily) {
                case "DP":
                    itemNamesTextNumber = 344;
                    break;
                case "Plat":
                    itemNamesTextNumber = 392;
                    break;
                default:
                    itemNamesTextNumber = gameLanguage == "JAP" ? 219 : 222;
                    break;
            }
        }
        private void SetLocationNamesTextNumber() {
            switch (gameFamily) {
                case "DP":
                    locationNamesTextNumber = 382;
                    break;
                case "Plat":
                    locationNamesTextNumber = 433;
                    break;
                default:
                    locationNamesTextNumber = gameLanguage == "JAP" ? 272 : 279;
                    break;
            }
        }
        private void SetPok�monNamesTextNumber() {
            switch (gameFamily) {
                case "DP":
                    pokemonNamesTextNumbers = new int[2] { 362, 363 };
                    break;
                case "Plat":
                    pokemonNamesTextNumbers = new int[7] { 412, 413, 712, 713, 714, 715, 716 }; //413?
                    break;
                case "HGSS":
                    pokemonNamesTextNumbers = new int[7] { 237, 238, 817, 818, 819, 820, 821 }; //238?
                    break;
            }
        }
        private void SetTrainerNamesMessageNumber() {
            switch (gameFamily) {
                case "DP":
                    trainerNamesMessageNumber = 559;
                    if (gameLanguage.Equals("JAP")) {
                        trainerNamesMessageNumber -= 9;
                    }
                    break;
                case "Plat":
                    trainerNamesMessageNumber = 618;
                    break;
                default:
                    trainerNamesMessageNumber = 729;
                    if (gameLanguage == "JAP") {
                        trainerNamesMessageNumber -= 10;
                    }
                    break;
            }
        }
        private void SetTrainerClassMessageNumber() {
            switch (gameFamily) {
                case "DP":
                    trainerClassMessageNumber = 560;
                    if (gameLanguage.Equals("JAP")) {
                        trainerClassMessageNumber -= 9;
                    }
                    break;
                case "Plat":
                    trainerClassMessageNumber = 619;
                    break;
                default:
                    trainerClassMessageNumber = 730;
                    if (gameLanguage.Equals("JAP")) {
                        trainerClassMessageNumber -= 10;
                    }
                    break;
            }
        }

        public string GetBuildingModelsDirPath(bool interior) => interior ? gameDirs[DirNames.interiorBuildingModels].unpackedDir : gameDirs[DirNames.exteriorBuildingModels].unpackedDir;
        public string GetRomNameFromWorkdir() => workDir.Substring(0, workDir.Length - folderSuffix.Length - 1);
        public static int GetHeaderCount() => (int)new FileInfo(internalNamesLocation).Length / internalNameLength;
        public static List<string> GetLocationNames() => new TextArchive(locationNamesTextNumber).messages;
        public string[] GetSimpleTrainerNames() => new TextArchive(trainerNamesMessageNumber).messages.ToArray();
        public string[] GetTrainerClassNames() => new TextArchive(trainerClassMessageNumber).messages.ToArray();
        public string[] GetItemNames() => new TextArchive(itemNamesTextNumber).messages.ToArray();
        public string[] GetItemNames(int startIndex, int count) => new TextArchive(itemNamesTextNumber).messages.GetRange(startIndex, count).ToArray();
        public string[] GetPok�monNames() => new TextArchive(pokemonNamesTextNumbers[0]).messages.ToArray();
        public string[] GetAttackNames() => new TextArchive(attackNamesTextNumber).messages.ToArray();
        public int GetAreaDataCount() => Directory.GetFiles(gameDirs[DirNames.areaData].unpackedDir).Length;
        public int GetMapTexturesCount() => Directory.GetFiles(gameDirs[DirNames.mapTextures].unpackedDir).Length;
        public int GetBuildingTexturesCount() => Directory.GetFiles(gameDirs[DirNames.buildingTextures].unpackedDir).Length;
        public int GetMatrixCount() => Directory.GetFiles(gameDirs[DirNames.matrices].unpackedDir).Length;
        public int GetTextArchivesCount() => Directory.GetFiles(gameDirs[DirNames.textArchives].unpackedDir).Length;
        public int GetMapCount() => Directory.GetFiles(gameDirs[DirNames.maps].unpackedDir).Length;
        public int GetEventCount() => Directory.GetFiles(gameDirs[DirNames.eventFiles].unpackedDir).Length;
        public int GetScriptCount() => Directory.GetFiles(gameDirs[DirNames.scripts].unpackedDir).Length;
        public int GetBuildingCount(bool interior) => Directory.GetFiles(GetBuildingModelsDirPath(interior)).Length;
        #endregion

        #region System Methods
        private void LoadGameVersion() {
            try {
                gameVersion = PokeDatabase.System.versionsDict[romID];
            } catch (KeyNotFoundException) {
                MessageBox.Show("The ROM you attempted to load is not supported.\nYou can only load Gen IV Pok�mon ROMS, for now.", "Unsupported ROM",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadGameName() {
            switch (gameVersion) {
                case "D":
                    gameName = "Diamond";
                    break;
                case "P":
                    gameName = "Pearl";
                    break;
                case "Plat":
                    gameName = "Platinum";
                    break;
                case "HG":
                    gameName = "HeartGold";
                    break;
                case "SS":
                    gameName = "SoulSilver";
                    break;
            }
        }
        private void LoadGameLanguage() {
            switch (romID) {
                case "ADAE":
                case "APAE":
                case "CPUE":
                case "IPKE":
                case "IPGE":
                    gameLanguage = "ENG";
                    break;

                case "ADAS":
                case "APAS":
                case "CPUS":
                case "IPKS":
                case "IPGS":
                case "LATA":
                    gameLanguage = "ESP";
                    break;

                case "ADAI":
                case "APAI":
                case "CPUI":
                case "IPKI":
                case "IPGI":
                    gameLanguage = "ITA";
                    break;

                case "ADAF":
                case "APAF":
                case "CPUF":
                case "IPKF":
                case "IPGF":
                    gameLanguage = "FRA";
                    break;

                case "ADAD":
                case "APAD":
                case "CPUD":
                case "IPKD":
                case "IPGD":
                    gameLanguage = "GER";
                    break;

                default:
                    gameLanguage = "JAP";
                    break;
            }
        }
        private void LoadGameFamily() {
            switch (gameVersion) {
                case "D":
                case "P":
                    gameFamily = "DP";
                    break;
                case "Plat":
                    gameFamily = "Plat";
                    break;
                case "HG":
                case "SS":
                    gameFamily = "HGSS";
                    break;
            }
        }
        private void SetNarcDirs() {
            Dictionary<DirNames, string> packedDirsDict = null;
            switch (gameFamily) {
                case "DP":
                    string suffix = "";
                    if (!gameLanguage.Equals("JAP"))
                        suffix = "_release";

                    packedDirsDict = new Dictionary<DirNames, string>() {
                        [DirNames.synthOverlay] = @"data\data\weather_sys.narc",
                        [DirNames.textArchives] = @"data\msgdata\msg.narc",

                        [DirNames.matrices] = @"data\fielddata\mapmatrix\map_matrix.narc",

                        [DirNames.maps] = @"data\fielddata\land_data\land_data" + suffix + ".narc",
                        [DirNames.exteriorBuildingModels] = @"data\fielddata\build_model\build_model.narc",
                        [DirNames.buildingConfigFiles] = @"data\fielddata\areadata\area_build_model\area_build.narc",
                        [DirNames.buildingTextures] = @"data\fielddata\areadata\area_build_model\areabm_texset.narc",
                        [DirNames.mapTextures] = @"data\fielddata\areadata\area_map_tex\map_tex_set.narc",
                        [DirNames.areaData] = @"data\fielddata\areadata\area_data.narc",

                        [DirNames.eventFiles] = @"data\fielddata\eventdata\zone_event" + suffix + ".narc",
                        [DirNames.OWSprites] = @"data\data\mmodel\mmodel.narc",

                        [DirNames.scripts] = @"data\fielddata\script\scr_seq" + suffix + ".narc",

                        [DirNames.trainerProperties] = @"data\poketool\trainer\trdata.narc",
                        [DirNames.trainerParty] = @"data\poketool\trainer\trpoke.narc",
                        [DirNames.trainerGraphics] = @"data\poketool\trgra\trfgra.narc",

                        [DirNames.encounters] = @"data\fielddata\encountdata\" + char.ToLower(gameVersion[0]) + '_' + "enc_data.narc"
                    };
                    break;
                case "Plat":
                    packedDirsDict = new Dictionary<DirNames, string>() {
                        [DirNames.synthOverlay] = @"data\data\weather_sys.narc",
                        [DirNames.dynamicHeaders] = @"data\debug\cb_edit\d_test.narc",

                        [DirNames.textArchives] = @"data\msgdata\" + gameVersion.Substring(0, 2).ToLower() + '_' + "msg.narc",

                        [DirNames.matrices] = @"data\fielddata\mapmatrix\map_matrix.narc",

                        [DirNames.maps] = @"data\fielddata\land_data\land_data.narc",
                        [DirNames.exteriorBuildingModels] = @"data\fielddata\build_model\build_model.narc",
                        [DirNames.buildingConfigFiles] = @"data\fielddata\areadata\area_build_model\area_build.narc",
                        [DirNames.buildingTextures] = @"data\fielddata\areadata\area_build_model\areabm_texset.narc",
                        [DirNames.mapTextures] = @"data\fielddata\areadata\area_map_tex\map_tex_set.narc",
                        [DirNames.areaData] = @"data\fielddata\areadata\area_data.narc",

                        [DirNames.eventFiles] = @"data\fielddata\eventdata\zone_event.narc",
                        [DirNames.OWSprites] = @"data\data\mmodel\mmodel.narc",

                        [DirNames.scripts] = @"data\fielddata\script\scr_seq.narc",

                        [DirNames.trainerProperties] = @"data\poketool\trainer\trdata.narc",
                        [DirNames.trainerParty] = @"data\poketool\trainer\trpoke.narc",
                        [DirNames.trainerGraphics] = @"data\poketool\trgra\trfgra.narc",

                        [DirNames.encounters] = @"data\fielddata\encountdata\" + gameVersion.Substring(0, 2).ToLower() + '_' + "enc_data.narc"
                    };
                    break;
                case "HGSS":
                    packedDirsDict = new Dictionary<DirNames, string>() {
                        [DirNames.synthOverlay] = @"data\a\0\2\8",
                        [DirNames.dynamicHeaders] = @"data\a\0\5\0",

                        [DirNames.textArchives] = @"data\a\0\2\7",

                        [DirNames.matrices] = @"data\a\0\4\1",

                        [DirNames.maps] = @"data\a\0\6\5",
                        [DirNames.exteriorBuildingModels] = @"data\a\0\4\0",
                        [DirNames.buildingConfigFiles] = @"data\a\0\4\3",
                        [DirNames.buildingTextures] = @"data\a\0\7\0",
                        [DirNames.mapTextures] = @"data\a\0\4\4",
                        [DirNames.areaData] = @"data\a\0\4\2",

                        [DirNames.eventFiles] = @"data\a\0\3\2",
                        [DirNames.OWSprites] = @"data\a\0\8\1",

                        [DirNames.scripts] = @"data\a\0\1\2",
                        //ENCOUNTERS FOLDER DEPENDS ON VERSION
                        [DirNames.trainerProperties] = @"data\a\0\5\5",
                        [DirNames.trainerParty] = @"data\a\0\5\6",
                        [DirNames.trainerGraphics] = @"data\a\0\5\8",

                        [DirNames.interiorBuildingModels] = @"data\a\1\4\8"
                    };

                    //Encounter archive is different for SS 
                    packedDirsDict[DirNames.encounters] = gameVersion == "HG" ? @"data\a\0\3\7" : @"data\a\1\3\6";
                    break;
            }

            gameDirs = new Dictionary<DirNames, (string packedDir, string unpackedDir)>();
            foreach (KeyValuePair<DirNames, string> kvp in packedDirsDict) {
                gameDirs.Add(kvp.Key, (workDir + kvp.Value, workDir + @"unpacked" + '\\' + kvp.Key.ToString()));
            }
        }
        public static void LoadMapCellsColorDictionary() {
            switch (gameFamily) {
                case "DP":
                case "Plat":
                    MapCellsColorDictionary = PokeDatabase.System.MatrixCellColors.DPPtmatrixColorsDict;
                    break;
                case "HGSS":
                    MapCellsColorDictionary = PokeDatabase.System.MatrixCellColors.HGSSmatrixColorsDict;
                    break;
            }
        }
        public void SetMapCellsColorDictionary(Dictionary<List<uint>, (Color background, Color foreground)> dict) {
            MapCellsColorDictionary = dict;
        }
        public static void ReadOWTable() {
            OverworldTable = new SortedDictionary<uint, (uint spriteID, ushort properties)>();
            switch (gameFamily) {
                case "DP":
                case "Plat":
                    using (BinaryReader idReader = new BinaryReader(new FileStream(OWtablePath, FileMode.Open))) {
                        idReader.BaseStream.Position = OWTableOffset;

                        uint entryID = idReader.ReadUInt32();
                        idReader.BaseStream.Position -= 4;
                        while ((entryID = idReader.ReadUInt32()) != 0xFFFF) {
                            uint spriteID = idReader.ReadUInt32();
                            (uint spriteID, ushort properties) tup = (spriteID, 0x0000);
                            OverworldTable.Add(entryID, tup);
                        }
                    }
                    break;
                case "HGSS":
                    using (BinaryReader idReader = new BinaryReader(new FileStream(OWtablePath, FileMode.Open))) {
                        idReader.BaseStream.Position = OWTableOffset;

                        ushort entryID = idReader.ReadUInt16();
                        idReader.BaseStream.Position -= 2;
                        while ((entryID = idReader.ReadUInt16()) != 0xFFFF) {
                            uint spriteID = idReader.ReadUInt16();
                            ushort properties = idReader.ReadUInt16();
                            (uint spriteID, ushort properties) tup = (spriteID, properties);
                            OverworldTable.Add(entryID, tup);
                        }
                    }
                    break;
            }
            foreach (uint k in ow3DSpriteDict.Keys) {
                OverworldTable.Add(k, (0x3D3D, 0x3D3D)); //ADD 3D overworld data (spriteID and properties are dummy values)
            }
            overworldTableKeys = OverworldTable.Keys.ToArray();
        }
        #endregion
    }
}