using Harmony;
using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.GUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using VRage.FileSystem;
using VRage.Plugins;
using VRage.Utils;

namespace SpeedLimitFix
{
    public enum UpdateCheckStatus
    {
        NotStarted,
        InProgress,
        Done,
        Failed
    }

    public class SpeedLimitFix : IPlugin
    {
        const string TextOutdated = @"New version of speed limit fix is available. Do you want to check it now?";
        const string TextDefaultDisabled = @"This version of speed limit fix was disabled because of compatibility issues. Please, uninstall it";
        const string TextOutdatedTitle = @"Speed Limit Fix is outdated";
        const string TextDisabledTitle = @"Speed Limit Fix is disabled";

        Version fixVersion = new Version("1.0");
        string configPath = null;

        public static SpeedLimitFix Shared = null;
        public bool isEnabled = true;

        bool MenuVisible = false;
        Downloader updater = null;
        UpdateCheckStatus updateStatus = UpdateCheckStatus.NotStarted;

        Uri latestVersionUrl = null;
        string disabledMessage = null;
        bool isUptodate = true;

        bool messageShown = false;
        bool needToShowMessage = false;

        public void Init(object gameInstance)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
            Log.Write("Init");
#endif

            Shared = this;

            configPath = Path.Combine(MyFileSystem.UserDataPath, "speedlimitfix.cfg");
            if (File.Exists(configPath))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(configPath);
                    ReadUpdateStatus(doc);
                }
                catch
                {
                }
            }

            updater = new Downloader();
            updater.UpdateCheckDone = UpdateCheckDone;
                        
            var harmony = HarmonyInstance.Create("name.krypt.speedlimit");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        private void UpdateCheckDone(XmlDocument doc)
        {
#if DEBUG
            Log.WriteFormat("UpdateCheckDone: {0}", doc);
#endif
            if (doc == null)
            {
                updateStatus = UpdateCheckStatus.Failed;
            } 
            else
            {
                updateStatus = UpdateCheckStatus.Done;
                doc.Save(configPath);
                ReadUpdateStatus(doc);
            }

            TryShowUpdateMessage();
        }

        private void ReadUpdateStatus(XmlDocument doc)
        {
            string versionLatestStr = doc.SelectSingleNode("update/latest/text()")?.Value;
            var versionLatest = new Version(versionLatestStr);

            isUptodate = fixVersion >= versionLatest;
            XmlElement disablingNode = doc.SelectSingleNode(string.Format("update/disabled[@version='{0}']", fixVersion.ToString())) as XmlElement;
            isEnabled = disablingNode == null;
            if (!isEnabled)
            {
                disabledMessage = disablingNode.InnerText;
                disabledMessage = string.IsNullOrEmpty(disabledMessage) ? TextDefaultDisabled : disabledMessage;
            }

            string urlStr = doc.SelectSingleNode("update/url/text()")?.Value;
            Uri.TryCreate(urlStr, UriKind.Absolute, out latestVersionUrl);
            if (latestVersionUrl != null && latestVersionUrl.Scheme != Uri.UriSchemeHttps)
            {
                latestVersionUrl = null;
            }

            needToShowMessage = !isEnabled || !isUptodate;
        }


        private void ShowUpdateMessage()
        {
            StringBuilder msg = new StringBuilder();
            if (!isEnabled)
            {
                msg.Append(disabledMessage);
                if (!isUptodate)
                {
                    msg.Append("\n\n");
                }
            }

            if (!isUptodate)
            {
                msg.Append(TextOutdated);
            }

            StringBuilder title = new StringBuilder(isUptodate ? TextDisabledTitle : TextOutdatedTitle);

            MyGuiSandbox.AddScreen(
                MyGuiSandbox.CreateMessageBox(
                    MyMessageBoxStyleEnum.Info,
                    isUptodate ? MyMessageBoxButtonsType.OK : MyMessageBoxButtonsType.YES_NO,
                    msg,
                    title,
                    noButtonText: MyStringId.GetOrCompute("Later"),
                    callback : isUptodate ? default(Action<MyGuiScreenMessageBox.ResultEnum>) : MessageCallback,
                    canHideOthers : true));
        }

        private void TryShowUpdateMessage()
        {
            if (MenuVisible & needToShowMessage & !messageShown)
            {
                messageShown = true;
                ShowUpdateMessage();
            }
        }

        private void MessageCallback(MyGuiScreenMessageBox.ResultEnum result)
        {
            if (result == MyGuiScreenMessageBox.ResultEnum.YES && latestVersionUrl != null)
            {
                Process.Start(latestVersionUrl.ToString());
            }
        }

        public void Update() { }

        public void Dispose() { }

        internal void MenuShown()
        {
            MenuVisible = true;
            if (updateStatus == UpdateCheckStatus.NotStarted)
            {
                updater.StartUpdateCheckAsync();
                updateStatus = UpdateCheckStatus.InProgress;
            }

            TryShowUpdateMessage();
        }

        internal void MenuHidden()
        {
            MenuVisible = false;
        }
    }


    [HarmonyPatch(typeof(MyGuiScreenMainMenu), "OnShow", new Type[] { } )]
    class MyGuiScreenMainMenu_OnShow_patch
    {
        public static void Postfix(ref MyGuiScreenMainMenu __instance)
        {
            if (__instance.GetType() == typeof(MyGuiScreenMainMenu))
            {
                SpeedLimitFix.Shared.MenuShown();
            }
        }
    }

    [HarmonyPatch(typeof(MyGuiScreenMainMenu), "OnHide", new Type[] { })]
    class MyGuiScreenMainMenu_OnHide_patch
    {
        public static void Postfix(ref MyGuiScreenMainMenu __instance)
        {
            if (__instance.GetType() == typeof(MyGuiScreenMainMenu))
            {
                SpeedLimitFix.Shared.MenuHidden();
            }
        }
    }

    [HarmonyPatch(typeof(MyPhysicsBody), "set_RigidBody", new Type[] { typeof(HkRigidBody) })]
    class MyPhysicsBody_RigidBody_patch
    {
        public static void Postfix(ref MyPhysicsBody __instance)
        {
            if (SpeedLimitFix.Shared.isEnabled && __instance.RigidBody != null)
            {
                if (MySession.Static.OnlineMode == VRage.Game.MyOnlineModeEnum.OFFLINE)
                {
                    __instance.RigidBody.MaxLinearVelocity = MyGridPhysics.ShipMaxLinearVelocity() + 100;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MyPhysicsBody), "set_RigidBody2", new Type[] { typeof(HkRigidBody) })]
    class MyPhysicsBody_RigidBody2_patch
    {
        public static void Postfix(ref MyPhysicsBody __instance)
        {
            if (SpeedLimitFix.Shared.isEnabled && __instance.RigidBody2 != null)
            {

                if (MySession.Static.OnlineMode == VRage.Game.MyOnlineModeEnum.OFFLINE)
                {
                    __instance.RigidBody2.MaxLinearVelocity = MyGridPhysics.ShipMaxLinearVelocity() + 100;
                }
            }
        }
    }
}

