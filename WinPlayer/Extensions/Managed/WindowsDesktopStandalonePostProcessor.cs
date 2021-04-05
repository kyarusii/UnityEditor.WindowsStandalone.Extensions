// Decompiled with JetBrains decompiler
// Type: UnityEditor.WindowsStandalone.WindowsDesktopStandalonePostProcessor
// Assembly: UnityEditor.WindowsStandalone.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 161E4C45-38AF-4CE8-9288-D3AC9704C0E2
// Assembly location: C:\Program Files\Unity\Hub\Editor\2020.3.2f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\UnityEditor.WindowsStandalone.Extensions.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.CrashReporting;
using UnityEditor.Modules;
using UnityEditor.Utils;
using UnityEditor.Windows;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.WindowsStandalone
{
  internal class WindowsDesktopStandalonePostProcessor : DesktopStandalonePostProcessor
  {
    private const string kWhatToDoWhenProjectIsNotSafeToOverwrite = "Consider building your project into an empty directory.";
    private static string kSolutionOutputFolder = "build/bin";

    public WindowsDesktopStandalonePostProcessor(bool hasIl2CppPlayers) : base(hasIl2CppPlayers)
    {
        //DesktopStandalonePostProcessor(hasIl2CppPlayers);
    }

    protected override void CheckSafeProjectOverwrite(BuildPostProcessArgs args)
    {
      if (DesktopStandalonePostProcessor.GetInstallingIntoBuildsFolder(args))
        return;
      string destinationFolder = base.GetDestinationFolder(args);
      if (base.GetCreateSolution(args))
      {
        if (File.Exists((string) args.installPath))
          throw new UnityException("Build path contains project built without \"Create Visual Studio Solution\" option, which is incompatible with current build settings. Consider building your project into an empty directory.");
        if (this.UseIl2Cpp)
        {
          string path1 = Path.Combine(destinationFolder, WindowsDesktopStandalonePostProcessor.kSolutionOutputFolder);
          string[] strArray = new string[6]
          {
            "x64/Debug",
            "x64/Release",
            "x64/Master",
            "x86/Debug",
            "x86/Release",
            "x86/Master"
          };
          foreach (string path2 in strArray)
          {
            if (WindowsDesktopStandalonePostProcessor.IsMonoInFolder(Path.Combine(path1, path2)))
              throw new UnityException("Build path contains project built with Mono scripting backend, while current project is using IL2CPP scripting backend. Consider building your project into an empty directory.");
          }
        }
        else if (Directory.Exists(Path.Combine(destinationFolder, "Il2CppOutputProject")))
          throw new UnityException("Build path contains project built with IL2CPP scripting backend, while current project is using Mono scripting backend. Consider building your project into an empty directory.");
      }
      else
      {
        string pathSafeProductName = DesktopStandalonePostProcessor.GetPathSafeProductName(args);
        string path1 = Path.Combine(destinationFolder, pathSafeProductName + ".sln");
        string path2 = Path.Combine(destinationFolder, "UnityCommon.props");
        if (File.Exists(path1) || File.Exists(path2))
          throw new UnityException("Build path contains project built with \"Create Visual Studio Solution\" option, which is incompatible with current build settings. Consider building your project into an empty directory.");
        if (this.UseIl2Cpp)
        {
          if (WindowsDesktopStandalonePostProcessor.IsMonoInFolder(destinationFolder))
            throw new UnityException("Build path contains project built with Mono scripting backend, while current project is using IL2CPP scripting backend. Consider building your project into an empty directory.");
        }
        else if (File.Exists(Path.Combine(destinationFolder, "GameAssembly.dll")))
          throw new UnityException("Build path contains project built with IL2CPP scripting backend, while current project is using Mono scripting backend. Consider building your project into an empty directory.");
      }
    }

    private static bool IsMonoInFolder(string destinationFolder) => Directory.Exists(Path.Combine(destinationFolder, "Mono")) || Directory.Exists(Path.Combine(destinationFolder, "MonoBleedingEdge"));

    private string GetDestinationExeName(BuildPostProcessArgs args) => Path.GetFileName((string) args.installPath);

    private static void RenameWindowsPlayerProject(
      string stagingArea,
      string projectName,
      HashSet<string> filesToNotOverwrite)
    {
      string str1 = Path.Combine(stagingArea, projectName);
      string str2 = Path.Combine(str1, "WindowsPlayer.vcxproj");
      string str3 = Path.Combine(str1, projectName + ".vcxproj");
      FileUtil.DeleteFileOrDirectory(str3);
      FileUtil.MoveFileOrDirectory(str2, str3);
      filesToNotOverwrite.Add(str3);
      foreach (string file in Directory.GetFiles(str1, "*", SearchOption.AllDirectories))
      {
        switch (Path.GetFileName(file))
        {
          case "Main.cpp":
          case "PrecompiledHeader.cpp":
          case "PrecompiledHeader.h":
          case "WindowsPlayer.ico":
          case "WindowsPlayer.manifest":
          case "WindowsPlayer.rc":
          case "WindowsPlayerVersion.rc":
          case "resource.h":
            filesToNotOverwrite.Add(file);
            break;
        }
      }
    }

    private static void WriteProjectPropsFile(
      string stagingArea,
      string projectName,
      bool useIl2Cpp)
    {
      string path = FileUtil.CombinePaths(new string[2]
      {
        stagingArea,
        "UnityCommon.props"
      });
      string str = useIl2Cpp ? "il2cpp" : "mono";
      using (StreamWriter streamWriter = new StreamWriter(path))
      {
        streamWriter.WriteLine("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
        streamWriter.WriteLine("  <PropertyGroup>");
        streamWriter.WriteLine("    <ProjectName>" + VisualStudioProjectHelpers.EscapeXML(projectName) + "</ProjectName>");
        streamWriter.WriteLine("    <ScriptingBackend>" + str + "</ScriptingBackend>");
        streamWriter.WriteLine("  </PropertyGroup>");
        streamWriter.WriteLine("</Project>");
      }
    }

    private static void WriteSolutionFile(
      string stagingArea,
      string projectName,
      bool isHeadlessMode,
      HashSet<string> filesToNotOverwrite,
      bool useIl2Cpp)
    {
      string path = Path.Combine(stagingArea, projectName + ".sln");
      string contents = string.Format(useIl2Cpp ? WindowsDesktopStandalonePostProcessor.WindowsPlayerSolutionTemplateIl2Cpp : WindowsDesktopStandalonePostProcessor.WindowsPlayerSolutionTemplateMono, (object) projectName, isHeadlessMode ? (object) "Server" : (object) "");
      File.WriteAllText(path, contents);
      filesToNotOverwrite.Add(path);
    }

    private static void CopyPlayerIcon(string stagingArea, string projectName)
    {
      string str1 = "Temp/WindowsPlayer.staging.ico";
      Vector2Int[] vector2IntArray = new Vector2Int[10]
      {
        new Vector2Int(256, 256),
        new Vector2Int(128, 128),
        new Vector2Int(96, 96),
        new Vector2Int(64, 64),
        new Vector2Int(48, 48),
        new Vector2Int(40, 40),
        new Vector2Int(32, 32),
        new Vector2Int(24, 24),
        new Vector2Int(20, 20),
        new Vector2Int(16, 16)
      };
      if (!IconUtility.SaveIcoForPlatform(str1, (BuildTargetGroup) 1, vector2IntArray))
        return;
      string str2 = Paths.Combine(new string[3]
      {
        stagingArea,
        projectName,
        "WindowsPlayer.ico"
      });
      FileUtil.DeleteFileOrDirectory(str2);
      FileUtil.MoveFileOrDirectory(str1, str2);
    }

    private string GetDestinationDataFolderRelativePath(BuildPostProcessArgs args) => base.GetCreateSolution(args) ? WindowsDesktopStandalonePostProcessor.kSolutionOutputFolder + "/" + DesktopStandalonePostProcessor.GetPathSafeProductName(args) + "_Data" : Path.GetFileNameWithoutExtension(this.GetDestinationExeName(args)) + "_Data";

    protected override void RenameFilesInStagingArea(BuildPostProcessArgs args)
    {
      string str = (string) args.stagingArea + "/" + this.GetDestinationDataFolderRelativePath(args);
      FileUtil.MoveFileOrDirectory((string) args.stagingAreaData, str);
      ((BuildReport) args.report).RecordFilesMoved((string) args.stagingAreaData, str);
    }

    protected override string GetDestinationFolderForInstallingIntoBuildsFolder(
      BuildPostProcessArgs args)
    {
      return Paths.Combine(new string[2]
      {
        this.GetVariationFolder(args),
        "DataSource"
      });
    }

    private static void CopyVariationNativeBinaries(
      BuildPostProcessArgs args,
      string variation,
      string stagingDestination,
      bool copyPlayerExe,
      bool failIfNotBuilt,
      bool forSolutionBuild,
      bool useIl2Cpp)
    {
      string str1 = (string) args.playerPackage + "/Variations/" + variation;
      string path1 = (string) args.stagingArea + "/" + stagingDestination;
      if (!Directory.Exists(str1))
      {
        if (failIfNotBuilt)
          throw new DirectoryNotFoundException("Could not find " + str1 + " for building Windows Standalone player. Did you build this variation?");
      }
      else
      {
        List<string> source = new List<string>();
        source.Add("(?:\\.lib$)");
        source.Add("(?:\\.exp$)");
        source.Add("(?:\\.ilk$)");
        if (!copyPlayerExe || DesktopStandalonePostProcessor.IsHeadlessMode(args))
        {
          source.Add("(?:[/\\\\]WindowsPlayer.exe$)");
          source.Add("(?:[/\\\\]WindowsPlayer_[^/\\\\]+$)");
        }
        if (!copyPlayerExe || !DesktopStandalonePostProcessor.IsHeadlessMode(args))
        {
          source.Add("(?:[/\\\\]WindowsPlayerHeadless.exe$)");
          source.Add("(?:[/\\\\]WindowsPlayerHeadless_[^/\\\\]+$)");
        }
        if (!UserBuildSettings.copyPDBFiles)
          source.Add("(?:\\.pdb$)");
        string str2 = source.Any<string>() ? string.Join("|", source.ToArray()) : (string) null;
        FileUtil.CopyDirectoryFiltered(str1, path1, true, str2, false);
        if (useIl2Cpp & forSolutionBuild)
          FileUtil.CopyFileOrDirectory(Path.Combine(str1, "baselib.dll.lib"), Path.Combine(path1, "baselib.dll.lib"));
        if (useIl2Cpp)
          return;
        FileUtil.CopyDirectoryFiltered(str1 + "/MonoBleedingEdge", path1 + "/MonoBleedingEdge", true, (string) null, true);
      }
    }

    protected override void CopyVariationFolderIntoStagingArea(
      BuildPostProcessArgs args,
      HashSet<string> filesToNotOverwrite)
    {
      FileUtil.CopyDirectoryFiltered(this.GetVariationFolder(args) + "/Data", (string) args.stagingAreaData, true, (Func<string, bool>) (f => this.CopyPlayerFilter(f, args)), true);
      if (base.GetCreateSolution(args))
      {
        string str = this.UseIl2Cpp ? "il2cpp" : "mono";
        List<KeyValuePair<string, string>> keyValuePairList = new List<KeyValuePair<string, string>>(8);
        keyValuePairList.Add(new KeyValuePair<string, string>("win32_development_" + str, "x86/Debug"));
        keyValuePairList.Add(new KeyValuePair<string, string>("win32_development_" + str, "x86/Release"));
        keyValuePairList.Add(new KeyValuePair<string, string>("win32_nondevelopment_" + str, "x86/Master"));
        keyValuePairList.Add(new KeyValuePair<string, string>("win64_development_" + str, "x64/Debug"));
        keyValuePairList.Add(new KeyValuePair<string, string>("win64_development_" + str, "x64/Release"));
        keyValuePairList.Add(new KeyValuePair<string, string>("win64_nondevelopment_" + str, "x64/Master"));
        if (this.UseIl2Cpp)
        {
          keyValuePairList.Add(new KeyValuePair<string, string>("win32_nondevelopment_" + str, "x86/MasterWithLTCG"));
          keyValuePairList.Add(new KeyValuePair<string, string>("win64_nondevelopment_" + str, "x64/MasterWithLTCG"));
        }
        foreach (KeyValuePair<string, string> keyValuePair in keyValuePairList)
          WindowsDesktopStandalonePostProcessor.CopyVariationNativeBinaries(args, keyValuePair.Key, WindowsDesktopStandalonePostProcessor.kSolutionOutputFolder + "/" + keyValuePair.Value, false, false, true, this.UseIl2Cpp);
        WindowsDesktopStandalonePostProcessor.CopyPlayerSolutionIntoStagingArea(args, filesToNotOverwrite, this.UseIl2Cpp);
      }
      else
      {
        string variationName = this.GetVariationName(args);
        WindowsDesktopStandalonePostProcessor.CopyVariationNativeBinaries(args, variationName, string.Empty, true, true, false, this.UseIl2Cpp);
        string str1 = Path.Combine((string) args.stagingArea, DesktopStandalonePostProcessor.IsHeadlessMode(args) ? "WindowsPlayerHeadless.exe" : "WindowsPlayer.exe");
        if (!IconUtility.AddIconToWindowsExecutable(str1))
          throw new BuildFailedException("Failed to add a custom icon to the executable.");
        ((BuildReport) args.report).RecordFileAdded(str1, CommonRoles.executable);
        ((BuildReport) args.report).RecordFileAdded((string) args.stagingArea + "/UnityPlayer.dll", CommonRoles.engineLibrary);
        ((BuildReport) args.report).RecordFileAdded((string) args.stagingArea + "/UnityCrashHandler" + ((int)args.target == 19 ? "64" : "32") + ".exe", CommonRoles.crashHandler);
        foreach (string file in Directory.GetFiles((string) args.stagingArea, "*.pdb", SearchOption.TopDirectoryOnly))
          ((BuildReport) args.report).RecordFileAdded((string) args.stagingArea + "/" + Path.GetFileName(file), CommonRoles.debugInfo);
        string str2 = (string) args.stagingArea + "/" + this.GetDestinationExeName(args);
        FileUtil.MoveFileOrDirectory(str1, str2);
        ((BuildReport) args.report).RecordFilesMoved(str1, str2);
      }
      DesktopStandalonePostProcessor.RecordCommonFiles(args, this.GetVariationFolder(args), (string) args.stagingArea);
    }

    private static void CopyPlayerSolutionIntoStagingArea(
      BuildPostProcessArgs args,
      HashSet<string> filesToNotOverwrite,
      bool useIl2Cpp)
    {
      string pathSafeProductName = DesktopStandalonePostProcessor.GetPathSafeProductName(args);
      string str1 = Paths.Combine(new string[3]
      {
        (string) args.playerPackage,
        "Source",
        "WindowsPlayer"
      });
      string str2 = Path.Combine((string) args.stagingArea, "UnityPlayerStub");
      string str3 = Path.Combine((string) args.stagingArea, pathSafeProductName);
      FileUtil.DeleteFileOrDirectory(str2);
      FileUtil.DeleteFileOrDirectory(str3);
      FileUtil.CopyDirectory(Paths.Combine(new string[2]
      {
        str1,
        "UnityPlayerStub"
      }), str2, true);
      FileUtil.CopyDirectory(Paths.Combine(new string[2]
      {
        str1,
        "WindowsPlayer"
      }), str3, true);
      WindowsDesktopStandalonePostProcessor.RenameWindowsPlayerProject((string) args.stagingArea, pathSafeProductName, filesToNotOverwrite);
      WindowsDesktopStandalonePostProcessor.WriteProjectPropsFile((string) args.stagingArea, pathSafeProductName, useIl2Cpp);
      WindowsDesktopStandalonePostProcessor.WriteSolutionFile((string) args.stagingArea, pathSafeProductName, DesktopStandalonePostProcessor.IsHeadlessMode(args), filesToNotOverwrite, useIl2Cpp);
      WindowsDesktopStandalonePostProcessor.CopyPlayerIcon((string) args.stagingArea, pathSafeProductName);
    }

    protected override void CopyDataForBuildsFolder(BuildPostProcessArgs args) => FileUtil.CopyDirectoryRecursive(Paths.Combine(new string[3]
    {
      this.GetVariationFolder(args),
      "Data",
      "Resources"
    }), Paths.Combine(new string[2]
    {
      GetDestinationFolderForInstallingIntoBuildsFolder(args),
      "Resources"
    }), true);

    protected override bool GetCreateSolution(BuildPostProcessArgs args) => !DesktopStandalonePostProcessor.GetInstallingIntoBuildsFolder(args) && UserBuildSettings.createSolution;

    protected override void DeleteDestination(BuildPostProcessArgs args)
    {
      bool deleteSucceeded = true;
      if (!base.GetCreateSolution(args))
      {
        deleteSucceeded = this.DeleteDestinationForBinaryBuild(args, deleteSucceeded);
      }
      else
      {
        string destinationFolder = base.GetDestinationFolder(args);
        string str1 = Path.Combine(destinationFolder, WindowsDesktopStandalonePostProcessor.kSolutionOutputFolder);
        if (Directory.Exists(str1))
        {
          deleteSucceeded = FileUtil.DeleteFileOrDirectory(str1);
          if (!deleteSucceeded)
            deleteSucceeded = this.DeleteFilesInDirectoryRecursive(str1);
        }
        if (deleteSucceeded && this.UseIl2Cpp)
        {
          string str2 = Path.Combine(destinationFolder, "Il2CppOutputProject");
          if (Directory.Exists(str2))
          {
            deleteSucceeded = FileUtil.DeleteFileOrDirectory(str2);
            if (!deleteSucceeded)
              deleteSucceeded = this.DeleteFilesInDirectoryRecursive(str2);
          }
        }
      }
      if (!deleteSucceeded)
        throw new IOException("Failed to prepare target build directory. Is a built game instance running?");
    }

    private bool DeleteFilesInDirectoryRecursive(string directory)
    {
      bool flag = true;
      string[] strArray1 = (string[]) null;
      string[] strArray2 = (string[]) null;
      try
      {
        strArray1 = Directory.GetFiles(directory);
        strArray2 = Directory.GetDirectories(directory);
      }
      catch (DirectoryNotFoundException ex)
      {
      }
      if (strArray1 != null)
      {
        foreach (string str in strArray1)
          flag &= FileUtil.DeleteFileOrDirectory(str);
      }
      if (strArray2 != null)
      {
        foreach (string directory1 in strArray2)
          flag &= this.DeleteFilesInDirectoryRecursive(directory1);
      }
      return flag;
    }

    private bool DeleteDestinationForBinaryBuild(BuildPostProcessArgs args, bool deleteSucceeded)
    {
      if (File.Exists((string) args.installPath))
        deleteSucceeded = FileUtil.DeleteFileOrDirectory((string) args.installPath);
      string fullDataFolderPath = this.GetFullDataFolderPath(args);
      if (deleteSucceeded && (File.Exists(fullDataFolderPath) || Directory.Exists(fullDataFolderPath)))
        deleteSucceeded = FileUtil.DeleteFileOrDirectory(fullDataFolderPath);
      if (deleteSucceeded)
      {
        string destinationFolder = base.GetDestinationFolder(args);
        if (Directory.Exists(destinationFolder))
        {
          foreach (string file in Directory.GetFiles(destinationFolder, "*.pdb"))
          {
            if (deleteSucceeded)
              deleteSucceeded = FileUtil.DeleteFileOrDirectory(file);
            else
              break;
          }
        }
        else if (File.Exists(destinationFolder))
          deleteSucceeded = FileUtil.DeleteFileOrDirectory(destinationFolder);
      }
      return deleteSucceeded;
    }

    private static string ToWindowsPath(string path)
    {
      if (path.Length > 1 && path[0] == '/' && path[1] == '/')
        path = "\\\\" + path.Substring(2);
      return path;
    }

    protected override string GetDestinationFolder(BuildPostProcessArgs args) => WindowsDesktopStandalonePostProcessor.ToWindowsPath(base.GetDestinationFolder(args));

    private string GetFullDataFolderPath(BuildPostProcessArgs args) => Path.Combine(base.GetDestinationFolder(args), this.GetDestinationDataFolderRelativePath(args));

    protected override string GetStagingAreaPluginsFolder(BuildPostProcessArgs args) => (string) args.stagingArea + "/Data/Plugins";

    protected override string PlatformStringFor(BuildTarget target)
    {
      BuildTarget buildTarget = target;
      if ((int)buildTarget == 5)
        return "win32";
      if ((int)buildTarget == 19)
        return "win64";
      throw new ArgumentException("Unexpected target: " + target.ToString());
    }

    protected override IIl2CppPlatformProvider GetPlatformProvider(
      BuildPostProcessArgs args)
    {
      BuildTarget target = (BuildTarget) args.target;
      if ((int)target != 5 && (int)target != 19)
        throw new Exception("Build target not supported.");
      return base.GetCreateSolution(args) ? (IIl2CppPlatformProvider) new WindowsStandaloneIl2CppPlatformProvider((BuildTarget) args.target, (string) args.stagingAreaData, (BuildReport) args.report, true, "$(SolutionDir)build\\bin\\$(PlatformTarget)\\$(Configuration)") : (IIl2CppPlatformProvider) new WindowsStandaloneIl2CppPlatformProvider((BuildTarget) args.target, (string) args.stagingAreaData, (BuildReport) args.report, false, this.GetVariationFolder(args));
    }

    public override void LaunchPlayer(BuildLaunchPlayerArgs args)
    {
    }

    public override bool SupportsInstallInBuildFolder() => PlayerSettings.GetScriptingBackend((BuildTargetGroup) 1) == 0;

    public override bool SupportsScriptsOnlyBuild() => true;

    public override string GetExtension(BuildTarget target, BuildOptions options) => "exe";

    protected override void ProcessPlatformSpecificIL2CPPOutput(BuildPostProcessArgs args)
    {
      FileUtil.DeleteFileOrDirectory(Path.Combine((string) args.stagingArea, "GameAssembly.exp"));
      FileUtil.DeleteFileOrDirectory(Path.Combine((string) args.stagingArea, "GameAssembly.lib"));
      FileUtil.DeleteFileOrDirectory(Path.Combine((string) args.stagingArea, "GameAssembly.map"));
      if (UserBuildSettings.copyPDBFiles)
        return;
      FileUtil.MoveFileOrDirectory(Path.Combine((string) args.stagingArea, "GameAssembly.pdb"), Paths.Combine(new string[3]
      {
        (string) args.stagingArea,
        DesktopStandalonePostProcessor.GetIl2CppDataBackupFolderName(args),
        "GameAssembly.pdb"
      }));
    }

    protected override void ProcessSymbolFiles(BuildPostProcessArgs args)
    {
      if (!CrashReportingSettings.enabled)
        return;
      string str = Path.GetDirectoryName((string) args.installPath).Replace('\\', '/');
      UnityEditor.CrashReporting.CrashReporting.UploadSymbolsInPath(UnityEditor.CrashReporting.CrashReporting.GetUsymUploadAuthToken(), str, "GameAssembly.pdb", string.Empty, InternalEditorUtility.inBatchMode && !UserBuildSettings.copyPDBFiles);
    }

    protected override void WriteIl2CppOutputProject(
      BuildPostProcessArgs args,
      string il2cppOutputProjectDirectory,
      IIl2CppPlatformProvider il2cppPlatformProvider)
    {
      string additionalDefines = "UNITY_WIN=1;UNITY_STANDALONE_WIN=1;PLATFORM_WIN=1;PLATFORM_STANDALONE_WIN=1";
      VisualStudioProjectHelpers.WriteIl2CppOutputProject((BuildTargetGroup) 1, il2cppOutputProjectDirectory, WindowsDesktopStandalonePostProcessor.Il2CppOutputProjectTemplate, additionalDefines, il2cppPlatformProvider);
    }

    private static string WindowsPlayerSolutionTemplateMono => string.Join("\r\n", new string[52]
    {
      "Microsoft Visual Studio Solution File, Format Version 12.00",
      "# Visual Studio 14",
      "VisualStudioVersion = 14.0.25420.1",
      "MinimumVisualStudioVersion = 10.0.40219.1",
      "Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"{0}\", \"{0}\\{0}.vcxproj\", \"{{57571793-BF11-47DB-A9BA-049C9AA938C7}}\"",
      "    ProjectSection(ProjectDependencies) = postProject",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}} = {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}",
      "    EndProjectSection",
      "EndProject",
      "Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"UnityData\", \"{0}\\UnityData.vcxitems\", \"{{9CC07CB0-D6B5-4B2E-990C-F644CBC7AE58}}\"",
      "EndProject",
      "Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"UnityPlayerStub\", \"UnityPlayerStub\\UnityPlayerStub.vcxproj\", \"{{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}\"",
      "EndProject",
      "Global",
      "    GlobalSection(SolutionConfigurationPlatforms) = preSolution",
      "        Debug|x64 = Debug|x64",
      "        Debug|x86 = Debug|x86",
      "        Master|x64 = Master|x64",
      "        Master|x86 = Master|x86",
      "        Release|x64 = Release|x64",
      "        Release|x86 = Release|x86",
      "    EndGlobalSection",
      "    GlobalSection(ProjectConfigurationPlatforms) = postSolution",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Debug|x64.ActiveCfg = Debug{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Debug|x64.Build.0 = Debug{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Debug|x86.ActiveCfg = Debug{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Debug|x86.Build.0 = Debug{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Master|x64.ActiveCfg = Master{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Master|x64.Build.0 = Master{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Master|x86.ActiveCfg = Master{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Master|x86.Build.0 = Master{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Release|x64.ActiveCfg = Release{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Release|x64.Build.0 = Release{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Release|x86.ActiveCfg = Release{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Release|x86.Build.0 = Release{1}|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Debug|x64.ActiveCfg = Debug|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Debug|x64.Build.0 = Debug|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Debug|x86.ActiveCfg = Debug|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Debug|x86.Build.0 = Debug|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Master|x64.ActiveCfg = Master|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Master|x64.Build.0 = Master|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Master|x86.ActiveCfg = Master|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Master|x86.Build.0 = Master|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Release|x64.ActiveCfg = Release|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Release|x64.Build.0 = Release|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Release|x86.ActiveCfg = Release|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Release|x86.Build.0 = Release|Win32",
      "    EndGlobalSection",
      "    GlobalSection(SolutionProperties) = preSolution",
      "        HideSolutionNode = FALSE",
      "    EndGlobalSection",
      "EndGlobal"
    }).Replace("    ", "\t");

    private static string WindowsPlayerSolutionTemplateIl2Cpp => string.Join("\r\n", new string[81]
    {
      "Microsoft Visual Studio Solution File, Format Version 12.00",
      "# Visual Studio 14",
      "VisualStudioVersion = 14.0.25420.1",
      "MinimumVisualStudioVersion = 10.0.40219.1",
      "Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"{0}\", \"{0}\\{0}.vcxproj\", \"{{57571793-BF11-47DB-A9BA-049C9AA938C7}}\"",
      "    ProjectSection(ProjectDependencies) = postProject",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}} = {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}} = {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}",
      "    EndProjectSection",
      "EndProject",
      "Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"UnityData\", \"{0}\\UnityData.vcxitems\", \"{{9CC07CB0-D6B5-4B2E-990C-F644CBC7AE58}}\"",
      "EndProject",
      "Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"UnityPlayerStub\", \"UnityPlayerStub\\UnityPlayerStub.vcxproj\", \"{{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}\"",
      "EndProject",
      "Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"Il2CppOutputProject\", \"Il2CppOutputProject\\Il2CppOutputProject.vcxproj\", \"{{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}\"",
      "EndProject",
      "Global",
      "    GlobalSection(SolutionConfigurationPlatforms) = preSolution",
      "        Debug|x64 = Debug|x64",
      "        Debug|x86 = Debug|x86",
      "        Master|x64 = Master|x64",
      "        Master|x86 = Master|x86",
      "        MasterWithLTCG|x64 = MasterWithLTCG|x64",
      "        MasterWithLTCG|x86 = MasterWithLTCG|x86",
      "        Release|x64 = Release|x64",
      "        Release|x86 = Release|x86",
      "    EndGlobalSection",
      "    GlobalSection(ProjectConfigurationPlatforms) = postSolution",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Debug|x64.ActiveCfg = Debug{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Debug|x64.Build.0 = Debug{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Debug|x86.ActiveCfg = Debug{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Debug|x86.Build.0 = Debug{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Master|x64.ActiveCfg = Master{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Master|x64.Build.0 = Master{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Master|x86.ActiveCfg = Master{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Master|x86.Build.0 = Master{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.MasterWithLTCG|x64.ActiveCfg = Master{1}WithLTCG|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.MasterWithLTCG|x64.Build.0 = Master{1}WithLTCG|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.MasterWithLTCG|x86.ActiveCfg = Master{1}WithLTCG|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.MasterWithLTCG|x86.Build.0 = Master{1}WithLTCG|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Release|x64.ActiveCfg = Release{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Release|x64.Build.0 = Release{1}|x64",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Release|x86.ActiveCfg = Release{1}|Win32",
      "        {{57571793-BF11-47DB-A9BA-049C9AA938C7}}.Release|x86.Build.0 = Release{1}|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Debug|x64.ActiveCfg = Debug|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Debug|x64.Build.0 = Debug|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Debug|x86.ActiveCfg = Debug|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Debug|x86.Build.0 = Debug|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Master|x64.ActiveCfg = Master|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Master|x64.Build.0 = Master|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Master|x86.ActiveCfg = Master|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Master|x86.Build.0 = Master|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.MasterWithLTCG|x64.ActiveCfg = MasterWithLTCG|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.MasterWithLTCG|x64.Build.0 = MasterWithLTCG|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.MasterWithLTCG|x86.ActiveCfg = MasterWithLTCG|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.MasterWithLTCG|x86.Build.0 = MasterWithLTCG|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Release|x64.ActiveCfg = Release|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Release|x64.Build.0 = Release|x64",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Release|x86.ActiveCfg = Release|Win32",
      "        {{9B743A06-56DC-42C2-99B1-B11DE6C02BBD}}.Release|x86.Build.0 = Release|Win32",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Debug|x64.ActiveCfg = Debug|x64",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Debug|x64.Build.0 = Debug|x64",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Debug|x86.ActiveCfg = Debug|Win32",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Debug|x86.Build.0 = Debug|Win32",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Master|x64.ActiveCfg = Master|x64",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Master|x64.Build.0 = Master|x64",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Master|x86.ActiveCfg = Master|Win32",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Master|x86.Build.0 = Master|Win32",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.MasterWithLTCG|x64.ActiveCfg = MasterWithLTCG|x64",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.MasterWithLTCG|x64.Build.0 = MasterWithLTCG|x64",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.MasterWithLTCG|x86.ActiveCfg = MasterWithLTCG|Win32",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.MasterWithLTCG|x86.Build.0 = MasterWithLTCG|Win32",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Release|x64.ActiveCfg = Release|x64",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Release|x64.Build.0 = Release|x64",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Release|x86.ActiveCfg = Release|Win32",
      "        {{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}.Release|x86.Build.0 = Release|Win32",
      "    EndGlobalSection",
      "    GlobalSection(SolutionProperties) = preSolution",
      "        HideSolutionNode = FALSE",
      "    EndGlobalSection",
      "EndGlobal"
    }).Replace("    ", "\t");

    private static string Il2CppOutputProjectTemplate => string.Join("\r\n", new string[93]
    {
      "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
      "<Project DefaultTargets=\"Build\" ToolsVersion=\"14.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">",
      "  <ItemGroup Label=\"ProjectConfigurations\">",
      "    <ProjectConfiguration Include=\"Debug|Win32\">",
      "      <Configuration>Debug</Configuration>",
      "      <Platform>Win32</Platform>",
      "    </ProjectConfiguration>",
      "    <ProjectConfiguration Include=\"Debug|x64\">",
      "      <Configuration>Debug</Configuration>",
      "      <Platform>x64</Platform>",
      "    </ProjectConfiguration>",
      "    <ProjectConfiguration Include=\"Master|Win32\">",
      "      <Configuration>Master</Configuration>",
      "      <Platform>Win32</Platform>",
      "    </ProjectConfiguration>",
      "    <ProjectConfiguration Include=\"Master|x64\">",
      "      <Configuration>Master</Configuration>",
      "      <Platform>x64</Platform>",
      "    </ProjectConfiguration>",
      "    <ProjectConfiguration Include=\"MasterWithLTCG|Win32\">",
      "      <Configuration>MasterWithLTCG</Configuration>",
      "      <Platform>Win32</Platform>",
      "    </ProjectConfiguration>",
      "    <ProjectConfiguration Include=\"MasterWithLTCG|x64\">",
      "      <Configuration>MasterWithLTCG</Configuration>",
      "      <Platform>x64</Platform>",
      "    </ProjectConfiguration>",
      "    <ProjectConfiguration Include=\"Release|Win32\">",
      "      <Configuration>Release</Configuration>",
      "      <Platform>Win32</Platform>",
      "    </ProjectConfiguration>",
      "    <ProjectConfiguration Include=\"Release|x64\">",
      "      <Configuration>Release</Configuration>",
      "      <Platform>x64</Platform>",
      "    </ProjectConfiguration>",
      "  </ItemGroup>",
      "  <ItemGroup>",
      "{0}  </ItemGroup>",
      "  <PropertyGroup Label=\"Globals\">",
      "    <ProjectGUID>{{AD94B4B8-6457-4DD9-9BBF-416F66AB4BBC}}</ProjectGUID>",
      "    <Keyword>MakeFileProj</Keyword>",
      "  </PropertyGroup>",
      "  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.Default.props\" />",
      "  <PropertyGroup>",
      "    <ConfigurationType>Makefile</ConfigurationType>",
      "    <PlatformToolset>v140</PlatformToolset>",
      "  </PropertyGroup>",
      "  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.props\" />",
      "  <ImportGroup Label=\"ExtensionSettings\">",
      "  </ImportGroup>",
      "  <ImportGroup Label=\"Shared\">",
      "  </ImportGroup>",
      "  <ImportGroup Label=\"PropertySheets\">",
      "    <Import Project=\"$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props\" Condition=\"exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')\" Label=\"LocalAppDataPlatform\" />",
      "  </ImportGroup>",
      "  <PropertyGroup Label=\"UserMacros\" />",
      "  <PropertyGroup Condition=\"'$(Configuration)'=='Debug'\">",
      "    <NMakePreprocessorDefinitions>_DEBUG;DEBUG;{1}</NMakePreprocessorDefinitions>",
      "    <BuilderConfiguration>Debug</BuilderConfiguration>",
      "  </PropertyGroup>",
      "  <PropertyGroup Condition=\"'$(Configuration)'=='Release' OR '$(Configuration)'=='Master'\">",
      "    <NMakePreprocessorDefinitions>_NDEBUG;NDEBUG;{1}</NMakePreprocessorDefinitions>",
      "    <BuilderConfiguration>Release</BuilderConfiguration>",
      "  </PropertyGroup>",
      "  <PropertyGroup Condition=\"'$(Configuration)'=='MasterWithLTCG'\">",
      "    <NMakePreprocessorDefinitions>_NDEBUG;NDEBUG;{1}</NMakePreprocessorDefinitions>",
      "    <BuilderConfiguration>ReleasePlus</BuilderConfiguration>",
      "  </PropertyGroup>",
      "  <PropertyGroup Condition=\"'$(Configuration)'=='Debug' OR '$(Configuration)'=='Release'\">",
      "    <Il2CppDebuggerFlags>{4}</Il2CppDebuggerFlags>",
      "  </PropertyGroup>",
      "  <PropertyGroup Condition=\"'$(Configuration)'=='Master' OR '$(Configuration)'=='MasterWithLTCG'\">",
      "    <Il2CppDebuggerFlags></Il2CppDebuggerFlags>",
      "  </PropertyGroup>",
      "  <PropertyGroup Condition=\"'$(Platform)'=='Win32'\">",
      "    <BuilderArchitecture>x86</BuilderArchitecture>",
      "  </PropertyGroup>",
      "  <PropertyGroup Condition=\"'$(Platform)'=='x64'\">",
      "    <BuilderArchitecture>x64</BuilderArchitecture>",
      "  </PropertyGroup>",
      "  <PropertyGroup>",
      "    <OutDir>$(SolutionDir)\\build\\bin\\$(BuilderArchitecture)\\$(Configuration)\\</OutDir>",
      "    <IntDir>$(SolutionDir)\\build\\obj\\il2cppOutputProject\\$(BuilderArchitecture)\\$(Configuration)\\</IntDir>",
      "    <NMakeOutput>$(OutDir)GameAssembly.dll</NMakeOutput>",
      "    <IncludePath>$(VC_IncludePath);$(UniversalCRT_IncludePath);$(WindowsSDK_IncludePath);$(ProjectDir)\\IL2CPP\\libil2cpp;$(ProjectDir)\\IL2CPP\\external\\bdwgc\\include</IncludePath>",
      "    <NMakeBuildCommandLine>\"$(ProjectDir)\\IL2CPP\\build\\deploy\\netcoreapp3.1\\il2cpp.exe\" --libil2cpp-static --compile-cpp -architecture=$(BuilderArchitecture) -configuration=$(BuilderConfiguration) -platform=WindowsDesktop -outputpath=\"$(NMakeOutput)\" --data-folder=\"$(OutDir)\\\" -cachedirectory=\"$(IntDir)\\\" -generatedcppdir=\"$(ProjectDir)\\Source\" $(Il2CppDebuggerFlags) {2} -dotnetprofile={3} -verbose --map-file-parser=\"$(ProjectDir)\\IL2CPP\\MapFileParser\\MapFileParser.exe\"</NMakeBuildCommandLine>",
      "    <NMakeReBuildCommandLine>$(NMakeBuildCommandLine) -forcerebuild</NMakeReBuildCommandLine>",
      "    <NMakeCleanCommandLine>del \"$(NMakeOutput)\"</NMakeCleanCommandLine>",
      "  </PropertyGroup>",
      "  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.targets\" />",
      "  <ImportGroup Label=\"ExtensionTargets\">",
      "  </ImportGroup>",
      "</Project>"
    });
  }
}
