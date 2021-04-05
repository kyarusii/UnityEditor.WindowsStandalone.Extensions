// Decompiled with JetBrains decompiler
// Type: UnityEditor.WindowsStandalone.UserBuildSettings
// Assembly: UnityEditor.WindowsStandalone.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 161E4C45-38AF-4CE8-9288-D3AC9704C0E2
// Assembly location: C:\Program Files\Unity\Hub\Editor\2020.3.2f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\UnityEditor.WindowsStandalone.Extensions.dll

namespace UnityEditor.WindowsStandalone
{
  public static class UserBuildSettings
  {
    private static readonly string kSettingCopyPDBFiles = "CopyPDBFiles";
    private const string kSettingCreateSolution = "CreateSolution";

    public static bool copyPDBFiles
    {
      get => EditorUserBuildSettings.GetPlatformSettings(DesktopStandaloneUserBuildSettings.PlatformName, UserBuildSettings.kSettingCopyPDBFiles).ToLower() == "true";
      set => EditorUserBuildSettings.SetPlatformSettings(DesktopStandaloneUserBuildSettings.PlatformName, UserBuildSettings.kSettingCopyPDBFiles, value.ToString().ToLower());
    }

    public static bool createSolution
    {
      get => EditorUserBuildSettings.GetPlatformSettings(DesktopStandaloneUserBuildSettings.PlatformName, "CreateSolution").ToLower() == "true";
      set => EditorUserBuildSettings.SetPlatformSettings(DesktopStandaloneUserBuildSettings.PlatformName, "CreateSolution", value.ToString().ToLower());
    }
  }
}
