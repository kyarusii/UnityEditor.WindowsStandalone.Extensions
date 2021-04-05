// Decompiled with JetBrains decompiler
// Type: UnityEditor.WindowsStandalone.WindowsStandaloneUnityLinkerPlatformProvider
// Assembly: UnityEditor.WindowsStandalone.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 161E4C45-38AF-4CE8-9288-D3AC9704C0E2
// Assembly location: C:\Program Files\Unity\Hub\Editor\2020.3.2f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\UnityEditor.WindowsStandalone.Extensions.dll

using System;
using UnityEditorInternal;

namespace UnityEditor.WindowsStandalone
{
  internal class WindowsStandaloneUnityLinkerPlatformProvider : BaseUnityLinkerPlatformProvider
  {
    private readonly bool m_CreateSolution;

    public WindowsStandaloneUnityLinkerPlatformProvider(BuildTarget target, bool createSolution) : base(target)
    {
      //this.\u002Ector(target);
      this.m_CreateSolution = createSolution;
    }

    public override string Platform => "WindowsDesktop";

    public override string Architecture
    {
      get
      {
        if (this.m_CreateSolution)
          throw new ArgumentException("Architecture cannot be known when creating a solution for the user to build");
        return this.m_Target == (BuildTarget)19 ? "x64" : "x86";
      }
    }
  }
}
