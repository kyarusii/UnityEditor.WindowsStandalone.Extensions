// Decompiled with JetBrains decompiler
// Type: UnityEditor.WindowsStandalone.WindowsStandaloneIL2CppNativeCodeBuilder
// Assembly: UnityEditor.WindowsStandalone.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 161E4C45-38AF-4CE8-9288-D3AC9704C0E2
// Assembly location: C:\Program Files\Unity\Hub\Editor\2020.3.2f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\UnityEditor.WindowsStandalone.Extensions.dll

using System;
using System.IO;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.WindowsStandalone
{
  public class WindowsStandaloneIL2CppNativeCodeBuilder : Il2CppNativeCodeBuilder
  {
    private readonly string _architecture;

    public WindowsStandaloneIL2CppNativeCodeBuilder(
      BuildTarget target,
      string baselibLibraryDirectory) : base(baselibLibraryDirectory)
    {
      //this.\u002Ector(baselibLibraryDirectory);
      BuildTarget buildTarget = target;
      if (buildTarget != (BuildTarget)5)
      {
        if (buildTarget != (BuildTarget)19)
          throw new ArgumentException("Unexpected target: " + target.ToString());
        this._architecture = "x64";
      }
      else
        this._architecture = "x86";
    }

    public override string CompilerPlatform => "WindowsDesktop";

    public override string CompilerArchitecture => this._architecture;

    public override string CacheDirectory => Paths.Combine(new string[3]
    {
      Path.GetFullPath(Application.dataPath),
      "..",
      "Library"
    });
  }
}
