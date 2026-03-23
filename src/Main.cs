using System.IO;
using System.Reflection;

using MelonLoader;
using MelonLoader.Utils;

using UnityEngine;

using BoneLib;
using BoneLib.BoneMenu;

using Il2CppSLZ.Marrow;

using NEP.DOOMLAB.WAD;
using NEP.DOOMLAB.Game;
using NEP.DOOMLAB.Entities;
using NEP.DOOMLAB.Rendering;
using NEP.DOOMLAB.Sound;
using Il2CppSLZ.Marrow.Warehouse;

namespace NEP.DOOMLAB
{
    public static class BuildInfo
    {
        public const string Name = "DOOMLAB"; // Name of the Mod.  (MUST BE SET)
        public const string Description = "Hell has encroached into MythOS. Pick up Doomguy's skills and slay endless demons."; // Description for the Mod.  (Set as null if none)
        public const string Author = "Not Enough Photons, adamdev"; // Author of the Mod.  (MUST BE SET)
        public const string Company = "Not Enough Photons"; // Company that made the Mod.  (Set as null if none)
        public const string Version = "1.1.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class Main : MelonMod
    {
        public AssetBundle bundle;
        public static GameObject mobjTemplate;
        public static Material unlitMaterial;
        public static Mobj player;
        public static SurfaceDataCard mobjSurfaceData;

        public static readonly string UserDataDirectory = MelonEnvironment.UserDataDirectory;
        public static readonly string TeamDirectory = Path.Combine(UserDataDirectory, "Not Enough Photons");
        public static readonly string ModDirectory = Path.Combine(TeamDirectory, "DOOMLAB");
        public static readonly string IWADDirectory = Path.Combine(ModDirectory, "IWADS");
        public static readonly string PWADDirectory = Path.Combine(ModDirectory, "PWADS");
        
        public static Texture2D MissingSprite;

        public override void OnInitializeMelon()
        {
            Directory.CreateDirectory(ModDirectory);
            Directory.CreateDirectory(IWADDirectory);
            Directory.CreateDirectory(PWADDirectory);

            var assembly = Assembly.GetExecutingAssembly();
            string fileName = HelperMethods.IsAndroid() ? "doomlab_quest.pack" : "doomlab_pcvr.pack";
            bundle = HelperMethods.LoadEmbeddedAssetBundle(assembly, "NEP.DOOMLAB.Resources." + fileName);
            mobjTemplate = bundle.LoadPersistentAsset<GameObject>("[MOBJ] - Null");
            MissingSprite = bundle.LoadPersistentAsset<Texture2D>("faila0");
            unlitMaterial = bundle.LoadPersistentAsset<Material>("mat_unlit");

            new WADManager();
            WADManager.Instance.LoadWAD(WADManager.Instance.GetIWAD());
            FrameBuilder.GenerateTable();

            DoomGame game = new DoomGame();

            Hooking.OnLevelLoaded += OnSceneLoaded;

            BoneMenuStuff();
        }

        public override void OnUpdate() => DoomGame.Instance.Update();

        public void OnSceneLoaded(LevelInfo info)
        {
            DoomGame.Instance.gameTic = 0;

            new SoundManager();
            new MobjManager();

            if(player != null)
            {
                Mobj.ComponentCache.RemoveInstance(player.gameObject.GetInstanceID());
            }

            player = Player.PhysicsRig.m_head.gameObject.AddComponent<Mobj>();
            player.gameObject.AddComponent<DoomPlayer>();
            player.flags ^= MobjFlags.MF_SOLID;
            player.flags ^= MobjFlags.MF_SHOOTABLE;
            player.playerHealth = Player.RigManager.GetComponent<Player_Health>();
            Mobj.ComponentCache.AddInstance(player.gameObject.GetInstanceID(), player);

            if(mobjSurfaceData == null)
            {
                AssetWarehouse.Instance.TryGetDataCard(new Barcode("SLZ.Backlot.SurfaceDataCard.Blood"), out SurfaceDataCard card);
                mobjSurfaceData = card;
            }
        }

        internal void BoneMenuStuff()
        {
            Page menuCategory = Page.Root.CreatePage("Not Enough Photons", Color.white);
            var doomCategory = menuCategory.CreatePage("DOOMLAB", Color.white);

            var gameFlagsCategory = doomCategory.CreatePage("Game Settings", Color.white);
            var debugCategory = doomCategory.CreatePage("Debug", Color.white);

            gameFlagsCategory.CreateBool("Disable Thinking", Color.white, false, (value) => Settings.DisableAI = value);
            gameFlagsCategory.CreateBool("No Target", Color.white, false, null);
            gameFlagsCategory.CreateBool("Fast Monsters", Color.white, false, (value) => 
            {
                Settings.FastMonsters = value;
                DoomGame.Instance.UpdateFastMonsters(Settings.FastMonsters);
            });
            gameFlagsCategory.CreateBool("Respawn Monsters", Color.white, false, (value) => Settings.RespawnMonsters = value);
            gameFlagsCategory.CreateFunction("Clear Corpses", Color.red, () => DoomGame.DeleteCorpses());

            debugCategory.CreateFloat("Projectile Pruning Distance", Color.white, 128f, 32f, 0f, 4096f, (value) => Settings.ProjectilePruneDistance = value);
            debugCategory.CreateBool("MOBJ Debug Stats", Color.white, false, (value) => Settings.EnableMobjDebug = value);
            debugCategory.CreateBool("MOBJ Debug Lines", Color.white, false, (value) => Settings.EnableMobjDebugLines = value);
            debugCategory.CreateBool("MOBJ Debug Colliders", Color.white, false, (value) => Settings.EnableMobjDebugColliders = value);

            var wadCategory = doomCategory.CreatePage("WADS", Color.white);
            string[] iwadNames = WADManager.Instance.GetWADsInFolder(WADFile.WADType.IWAD, false);
            string[] pwadNames = WADManager.Instance.GetWADsInFolder(WADFile.WADType.PWAD, false);

            for (int i = 0; i < iwadNames.Length; i++)
            {
                int index = i;
                wadCategory.CreateFunction(WADManager.Instance.GetWADFileName(iwadNames[index], true), Color.white, () =>
                {
                    WADManager.Instance.LoadWAD(WADManager.Instance.IWADS[index]);
                    FrameBuilder.GenerateTable();
                    MobjRenderer.LoadSpriteDefs();
                    SoundManager.Instance.LoadWADAudio(WADManager.Instance.LoadedWAD.sounds);
                });
            }

            for (int i = 0; i < pwadNames.Length; i++)
            {
                int index = i;
                wadCategory.CreateFunction(WADManager.Instance.GetWADFileName(pwadNames[index], true), Color.white, () =>
                {
                    WADManager.Instance.LoadWAD(WADManager.Instance.PWADS[index]);
                    FrameBuilder.GenerateTable();
                    MobjRenderer.LoadSpriteDefs();
                    SoundManager.Instance.LoadWADAudio(WADManager.Instance.LoadedWAD.sounds);
                });
            }
        }
    }
}
