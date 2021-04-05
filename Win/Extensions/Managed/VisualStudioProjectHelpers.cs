// Decompiled with JetBrains decompiler
// Type: UnityEditor.Windows.VisualStudioProjectHelpers
// Assembly: UnityEditor.WindowsStandalone.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 161E4C45-38AF-4CE8-9288-D3AC9704C0E2
// Assembly location: C:\Program Files\Unity\Hub\Editor\2020.3.2f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\UnityEditor.WindowsStandalone.Extensions.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using UnityEditor.Utils;
using UnityEditorInternal;

namespace UnityEditor.Windows
{
  internal class VisualStudioProjectHelpers
  {
    public static void WriteIl2CppOutputProject(
      BuildTargetGroup buildTargetGroup,
      string il2CppOutputProjectDirectory,
      string projectTemplate,
      string additionalDefines,
      IIl2CppPlatformProvider il2cppPlatformProvider)
    {
      string targetPath = Path.Combine(il2CppOutputProjectDirectory, "Il2CppOutputProject.vcxproj");
      string[] cppOutputProject;
      using (new ProfilerBlock("VisualStudioProjectHelpers.FindSourceFilesForIl2CppOutputProject"))
        cppOutputProject = VisualStudioProjectHelpers.FindSourceFilesForIl2CppOutputProject(il2CppOutputProjectDirectory);
      string projectItems;
      using (new ProfilerBlock("VisualStudioProjectHelpers.MakeProjectItems"))
        projectItems = VisualStudioProjectHelpers.MakeProjectItems((IEnumerable<string>) cppOutputProject, il2CppOutputProjectDirectory, true);
      string filterItems;
      using (new ProfilerBlock("VisualStudioProjectHelpers.MakeFilterItems"))
        filterItems = VisualStudioProjectHelpers.MakeFilterItems((IEnumerable<string>) cppOutputProject, il2CppOutputProjectDirectory);
      using (new ProfilerBlock("VisualStudioProjectHelpers.WriteIl2CppOutputProjectFile"))
        VisualStudioProjectHelpers.WriteIl2CppOutputProjectFile(buildTargetGroup, projectTemplate, projectItems, filterItems, targetPath, additionalDefines, il2cppPlatformProvider);
    }

    private static string[] FindSourceFilesForIl2CppOutputProject(
      string il2CppOutputProjectDirectory)
    {
      IEnumerable<string> first = ((IEnumerable<string>) new string[2]
      {
        ".c",
        ".cpp"
      }).SelectMany<string, string>((Func<string, IEnumerable<string>>) (extension => (IEnumerable<string>) Directory.GetFiles(Paths.Combine(new string[3]
      {
        il2CppOutputProjectDirectory,
        "Source",
        "il2cppOutput"
      }), "*" + extension, SearchOption.AllDirectories)));
      IEnumerable<string> second1 = Enumerable.Empty<string>();
      string cppPluginsFolder = Paths.Combine(new string[3]
      {
        il2CppOutputProjectDirectory,
        "Source",
        "CppPlugins"
      });
      if (Directory.Exists(cppPluginsFolder))
        second1 = ((IEnumerable<string>) new string[3]
        {
          ".c",
          ".cpp",
          ".h"
        }).SelectMany<string, string>((Func<string, IEnumerable<string>>) (extension => (IEnumerable<string>) Directory.GetFiles(cppPluginsFolder, "*" + extension, SearchOption.AllDirectories)));
      IEnumerable<string> second2 = ((IEnumerable<string>) new string[3]
      {
        ".c",
        ".cpp",
        ".h"
      }).SelectMany<string, string>((Func<string, IEnumerable<string>>) (extension => (IEnumerable<string>) Directory.GetFiles(Path.Combine(il2CppOutputProjectDirectory, "IL2CPP"), "*" + extension, SearchOption.AllDirectories)));
      return first.Concat<string>(second1).Concat<string>(second2).Where<string>((Func<string, bool>) (path => !path.Contains("IL2CPP\\MapFileParser"))).ToArray<string>();
    }

    private static void WriteIl2CppOutputProjectFile(
      BuildTargetGroup buildTargetGroup,
      string projectTemplate,
      string projectItems,
      string filterItems,
      string targetPath,
      string additionalDefines,
      IIl2CppPlatformProvider il2cppPlatformProvider)
    {
      string[] buildingIl2CppArguments = IL2CPPUtils.GetBuildingIL2CPPArguments(il2cppPlatformProvider, buildTargetGroup);
      string[] debuggerIl2CppArguments = IL2CPPUtils.GetDebuggerIL2CPPArguments(il2cppPlatformProvider, buildTargetGroup);
      string str1 = ((IEnumerable<string>) debuggerIl2CppArguments).Any<string>() ? ((IEnumerable<string>) debuggerIl2CppArguments).Aggregate<string>((Func<string, string, string>) ((x, y) => x + " " + y)) : string.Empty;
      IEnumerable<string> second = ((IEnumerable<string>) additionalDefines.Split(';')).Select<string, string>((Func<string, string>) (d => "--additional-defines=" + d));
      string str2 = ((IEnumerable<string>) buildingIl2CppArguments).Concat<string>(second).Aggregate<string>((Func<string, string, string>) ((x, y) => x + " " + y));
      string netProfileArgument = IL2CPPUtils.ApiCompatibilityLevelToDotNetProfileArgument(PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup));
      string str3 = ((IEnumerable<string>) IL2CPPUtils.GetBuilderDefinedDefines(il2cppPlatformProvider, buildTargetGroup)).Aggregate<string>((Func<string, string, string>) ((x, y) => x + ";" + y)) + ";" + additionalDefines;
      string contents1 = string.Format(projectTemplate, (object) projectItems, (object) str3, (object) str2, (object) netProfileArgument, (object) str1);
      FileUtil.DeleteFileOrDirectory(targetPath);
      File.WriteAllText(targetPath, contents1, Encoding.UTF8);
      string path = targetPath + ".filters";
      string contents2 = string.Format(VisualStudioProjectHelpers.GetFiltersTemplate(), (object) filterItems);
      FileUtil.DeleteFileOrDirectory(path);
      File.WriteAllText(path, contents2, Encoding.UTF8);
    }

    public static string MakeProjectItems(
      IEnumerable<string> files,
      string projectDirectory,
      bool excludeFromResourceIndex,
      string pathPrefix = "",
      Dictionary<string, string> predeterminedTags = null,
      Dictionary<string, string> additionalItemAttributes = null)
    {
      StringBuilder stringBuilder = new StringBuilder();
      foreach (string file1 in files)
      {
        string withoutExtension = Path.GetFileNameWithoutExtension(file1);
        string file2 = VisualStudioProjectHelpers.EscapeXML(pathPrefix + Paths.MakeRelativePath(projectDirectory, file1));
        string fileTag = VisualStudioProjectHelpers.DetermineFileTag(file1, predeterminedTags);
        string additionalAttributes = VisualStudioProjectHelpers.DetermineAdditionalAttributes(file2, additionalItemAttributes);
        stringBuilder.AppendLine("    <" + fileTag + " Include=\"" + file2 + "\"" + additionalAttributes + ">");
        switch (fileTag)
        {
          case "ClCompile":
            if (Path.GetFileName(file1).Equals("pch.cpp", StringComparison.InvariantCultureIgnoreCase))
            {
              stringBuilder.AppendLine("      <PrecompiledHeader>Create</PrecompiledHeader>");
              goto case "ClInclude";
            }
            else
              goto case "ClInclude";
          case "ClInclude":
            if (Path.GetFileNameWithoutExtension(file1).EndsWith(".xaml", StringComparison.InvariantCultureIgnoreCase))
            {
              stringBuilder.AppendFormat("      <DependentUpon>{0}</DependentUpon>{1}", (object) withoutExtension, (object) Environment.NewLine);
              break;
            }
            break;
          case "None":
            if (!string.Equals(Path.GetExtension(file1), ".pfx", StringComparison.OrdinalIgnoreCase) && !string.Equals(Path.GetExtension(file1), ".debug", StringComparison.OrdinalIgnoreCase))
            {
              stringBuilder.AppendLine("      <DeploymentContent>true</DeploymentContent>");
              if (excludeFromResourceIndex)
                stringBuilder.AppendLine("      <ExcludeFromResourceIndex>true</ExcludeFromResourceIndex>");
              break;
            }
            break;
          case "Reference":
            if (Path.GetExtension(file1) == ".winmd")
            {
              stringBuilder.AppendLine("      <IsWinMDFile>true</IsWinMDFile>");
              break;
            }
            break;
          case "AppxManifest":
          case "Page":
            stringBuilder.AppendLine("      <SubType>Designer</SubType>");
            break;
        }
        stringBuilder.AppendFormat("    </{0}>{1}", (object) fileTag, (object) Environment.NewLine);
      }
      return stringBuilder.ToString();
    }

    public static string MakeFilterItems(
      IEnumerable<string> files,
      string UserProjectDirectory,
      string pathPrefix = "",
      Dictionary<string, string> predeterminedTags = null)
    {
      StringBuilder stringBuilder1 = new StringBuilder();
      Dictionary<string, string> dictionary = new Dictionary<string, string>((IEqualityComparer<string>) StringComparer.InvariantCultureIgnoreCase);
      foreach (string file in files)
      {
        string path = Paths.MakeRelativePath(UserProjectDirectory, file);
        string fileTag = VisualStudioProjectHelpers.DetermineFileTag(file, predeterminedTags);
        string directoryName = Path.GetDirectoryName(path);
        string str = VisualStudioProjectHelpers.EscapeXML(pathPrefix + path);
        if (string.IsNullOrEmpty(directoryName))
        {
          stringBuilder1.AppendFormat("    <{0} Include=\"{1}\" />{2}", (object) fileTag, (object) str, (object) Environment.NewLine);
        }
        else
        {
          if (!dictionary.ContainsKey(directoryName))
            dictionary.Add(directoryName, Guid.NewGuid().ToString());
          stringBuilder1.AppendFormat("    <{0} Include=\"{1}\">{2}", (object) fileTag, (object) str, (object) Environment.NewLine);
          stringBuilder1.AppendFormat("      <Filter>{0}</Filter>{1}", (object) directoryName, (object) Environment.NewLine);
          stringBuilder1.AppendFormat("    </{0}>{1}", (object) fileTag, (object) Environment.NewLine);
        }
      }
      HashSet<string> stringSet = new HashSet<string>();
      foreach (KeyValuePair<string, string> keyValuePair in dictionary)
      {
        for (string directoryName = Path.GetDirectoryName(keyValuePair.Key); !string.IsNullOrEmpty(directoryName); directoryName = Path.GetDirectoryName(directoryName))
        {
          if (!dictionary.ContainsKey(directoryName))
            stringSet.Add(directoryName);
        }
      }
      foreach (string key in stringSet)
        dictionary.Add(key, Guid.NewGuid().ToString());
      StringBuilder stringBuilder2 = new StringBuilder();
      foreach (KeyValuePair<string, string> keyValuePair in dictionary)
      {
        stringBuilder2.AppendFormat("    <Filter Include=\"{0}\">{1}", (object) keyValuePair.Key, (object) Environment.NewLine);
        stringBuilder2.AppendFormat("      <UniqueIdentifier>{{{0}}}</UniqueIdentifier>{1}", (object) keyValuePair.Value, (object) Environment.NewLine);
        stringBuilder2.AppendFormat("    </Filter>{0}", (object) Environment.NewLine);
      }
      return stringBuilder2?.ToString() + stringBuilder1.ToString();
    }

    private static string DetermineFileTag(
      string file,
      Dictionary<string, string> predeterminedTags)
    {
      string str;
      if (predeterminedTags != null && predeterminedTags.TryGetValue(file, out str))
        return str;
      if (Path.GetFileName(file).Equals("App.xaml", StringComparison.InvariantCultureIgnoreCase))
        return "ApplicationDefinition";
      switch (Path.GetExtension(file))
      {
        case ".appxmanifest":
          return "AppxManifest";
        case ".c":
        case ".cpp":
          return "ClCompile";
        case ".h":
          return "ClInclude";
        case ".res":
          return "Resource";
        case ".winmd":
          return "Reference";
        case ".xaml":
          return "Page";
        default:
          return "None";
      }
    }

    private static string DetermineAdditionalAttributes(
      string file,
      Dictionary<string, string> additionalItemAttributes)
    {
      string str;
      return additionalItemAttributes == null || !additionalItemAttributes.TryGetValue(file, out str) ? string.Empty : " " + str;
    }

    public static string GetFiltersTemplate() => string.Join(Environment.NewLine, new string[5]
    {
      "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
      "<Project ToolsVersion=\"14.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">",
      "  <ItemGroup>",
      "{0}  </ItemGroup>",
      "</Project>"
    });

    public static string EscapeXML(string str) => SecurityElement.Escape(str.Replace("'", "%27"));
  }
}
