// Decompiled with JetBrains decompiler
// Type: UnityEditor.WindowsStandalone.WindowsStandaloneIl2CppPlatformProvider
// Assembly: UnityEditor.WindowsStandalone.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 161E4C45-38AF-4CE8-9288-D3AC9704C0E2
// Assembly location: C:\Program Files\Unity\Hub\Editor\2020.3.2f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\UnityEditor.WindowsStandalone.Extensions.dll

using System;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;

namespace UnityEditor.WindowsStandalone
{
  internal class WindowsStandaloneIl2CppPlatformProvider : BaseIl2CppPlatformProvider
  {
    private readonly bool m_CreateSolution;
    private const string kBaseCacheDirectory = "Library/Il2cppBuildCache/Windows";

    internal WindowsStandaloneIl2CppPlatformProvider(
      BuildTarget target,
      string dataFolder,
      BuildReport report,
      bool createSolution,
      string baselibLibraryDirectory) :base(target, Path.Combine(dataFolder, "Libraries"), report, baselibLibraryDirectory)
    {
      //this.\u002Ector(target, Path.Combine(dataFolder, "Libraries"), report, baselibLibraryDirectory);
      this.m_CreateSolution = createSolution;
    }

    public override string nativeLibraryFileName => "GameAssembly.dll";

    public override string[] includePaths => new string[0];

    public override string il2cppBuildCacheDirectory
    {
      get
      {
        if (this.m_CreateSolution)
          return "Library/Il2cppBuildCache/Windows/VSProject";
        BuildTarget target = this.target;
        if (target == (BuildTarget)5)
          return "Library/Il2cppBuildCache/Windows/x86";
        if (target == (BuildTarget)19)
          return "Library/Il2cppBuildCache/Windows/x64";
        throw new ArgumentException("Unexpected target: " + this.target.ToString());
      }
    }

    public override Il2CppNativeCodeBuilder CreateIl2CppNativeCodeBuilder() => !this.m_CreateSolution ? (Il2CppNativeCodeBuilder) new WindowsStandaloneIL2CppNativeCodeBuilder(this.target, this.baselibLibraryDirectory) : (Il2CppNativeCodeBuilder) null;

    public override BaseUnityLinkerPlatformProvider CreateUnityLinkerPlatformProvider() => (BaseUnityLinkerPlatformProvider) new WindowsStandaloneUnityLinkerPlatformProvider(this.target, this.m_CreateSolution);

    public override bool supportsManagedDebugging => true;
  }
}
