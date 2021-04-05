// Decompiled with JetBrains decompiler
// Type: UnityEditor.WindowsStandalone.TargetExtension
// Assembly: UnityEditor.WindowsStandalone.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 161E4C45-38AF-4CE8-9288-D3AC9704C0E2
// Assembly location: C:\Program Files\Unity\Hub\Editor\2020.3.2f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\UnityEditor.WindowsStandalone.Extensions.dll

using System.IO;
using UnityEditor.Modules;
using UnityEditor.Utils;

namespace UnityEditor.WindowsStandalone
{
  internal class TargetExtension : DefaultPlatformSupportModule
  {
    private bool m_AreIl2CppPlayersInstalled;

    public TargetExtension()
    {
      //base.\u002Ector();
      this.m_AreIl2CppPlayersInstalled = this.AreIl2CppPlayersInstalled();
    }

    private bool AreIl2CppPlayersInstalled()
    {
      string playbackEngineDirectory = BuildPipeline.GetPlaybackEngineDirectory((BuildTarget) 5, (BuildOptions) 0);
      string[] strArray = new string[4]
      {
        "win32_development_il2cpp",
        "win32_nondevelopment_il2cpp",
        "win64_development_il2cpp",
        "win64_nondevelopment_il2cpp"
      };
      foreach (string str in strArray)
      {
        if (File.Exists(Paths.Combine(new string[4]
        {
          playbackEngineDirectory,
          "Variations",
          str,
          "UnityPlayer.dll"
        })))
          return true;
      }
      return false;
    }

    public override IBuildPostprocessor CreateBuildPostprocessor() => (IBuildPostprocessor) new WindowsDesktopStandalonePostProcessor(this.m_AreIl2CppPlayersInstalled);

    public override IScriptingImplementations CreateScriptingImplementations() => (IScriptingImplementations) new DesktopStandalonePostProcessor.ScriptingImplementations();

    public override IBuildWindowExtension CreateBuildWindowExtension() => (IBuildWindowExtension) new WindowsStandaloneBuildWindowExtension(this.m_AreIl2CppPlayersInstalled);

    public override IPluginImporterExtension CreatePluginImporterExtension() => (IPluginImporterExtension) new DesktopPluginImporterExtension();

    public override string TargetName => "WindowsStandalone";

    public override string JamTarget => "WindowsStandaloneEditorExtensions";
  }
}
