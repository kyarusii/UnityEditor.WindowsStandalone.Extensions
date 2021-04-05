// Decompiled with JetBrains decompiler
// Type: ProfilerBlock
// Assembly: UnityEditor.WindowsStandalone.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 161E4C45-38AF-4CE8-9288-D3AC9704C0E2
// Assembly location: C:\Program Files\Unity\Hub\Editor\2020.3.2f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\UnityEditor.WindowsStandalone.Extensions.dll

using System;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;

[StructLayout(LayoutKind.Sequential, Size = 1)]
internal struct ProfilerBlock : IDisposable
{
  public ProfilerBlock(string name) => Profiler.BeginSample(name);

  public void Dispose() => Profiler.EndSample();
}
