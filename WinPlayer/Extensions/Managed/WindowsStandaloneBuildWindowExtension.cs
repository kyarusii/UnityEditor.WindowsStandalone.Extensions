// Decompiled with JetBrains decompiler
// Type: UnityEditor.WindowsStandalone.WindowsStandaloneBuildWindowExtension
// Assembly: UnityEditor.WindowsStandalone.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 161E4C45-38AF-4CE8-9288-D3AC9704C0E2
// Assembly location: C:\Program Files\Unity\Hub\Editor\2020.3.2f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\UnityEditor.WindowsStandalone.Extensions.dll

using System;
using UnityEngine;

namespace UnityEditor.WindowsStandalone
{
  internal class WindowsStandaloneBuildWindowExtension : DesktopStandaloneBuildWindowExtension
  {
    private GUIContent m_CopyPdbFiles;
    private GUIContent m_CreateSolutionText;

    public WindowsStandaloneBuildWindowExtension(bool areIl2CppPlayersInstalled) : base(areIl2CppPlayersInstalled)
    {
        //base.\u002Ector(areIl2CppPlayersInstalled);
    }

    public override void ShowPlatformBuildOptions()
    {
      base.ShowPlatformBuildOptions();
      UserBuildSettings.copyPDBFiles = EditorGUILayout.Toggle(this.m_CopyPdbFiles, UserBuildSettings.copyPDBFiles, new GUILayoutOption[0]);
      EditorGUI.DisabledScope disabledScope = new EditorGUI.DisabledScope(EditorUserBuildSettings.installInBuildFolder);
      //((EditorGUI.DisabledScope) ref disabledScope).\u002Ector(EditorUserBuildSettings.installInBuildFolder);
      try
      {
        UserBuildSettings.createSolution = EditorGUILayout.Toggle(this.m_CreateSolutionText, UserBuildSettings.createSolution, new GUILayoutOption[0]);
      }
      finally
      {
        disabledScope.Dispose();
      }
    }

    protected override RuntimePlatform GetHostPlatform() => RuntimePlatform.WindowsEditor;

    protected override string GetHostPlatformName() => "Windows";

    public override bool EnabledBuildAndRunButton() => !UserBuildSettings.createSolution;
  }
}
