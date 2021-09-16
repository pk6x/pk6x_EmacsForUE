// Copyright (c) 2021 Oleksandr Manenko
//
// Permission is  hereby granted, free  of charge,  to any person  obtaining a
// copy of this software and  associated documentation files (the "Software"),
// to deal in  the Software without restriction,  including without limitation
// the rights  to use, copy,  modify, merge, publish,  distribute, sublicense,
// and/or  sell copies  of the  Software, and  to permit  persons to  whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this  permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS",  WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING  BUT NOT LIMITED  TO THE WARRANTIES  OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR  COPYRIGHT HOLDERS  BE LIABLE  FOR ANY  CLAIM, DAMAGES  OR OTHER
// LIABILITY, WHETHER  IN AN  ACTION OF CONTRACT,  TORT OR  OTHERWISE, ARISING
// FROM,  OUT OF  OR IN  CONNECTION  WITH THE  SOFTWARE  OR THE  USE OR  OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tools.DotNETCommon;

namespace UnrealBuildTool
{
	/// <summary>
	/// Generator of Unreal Emacs project files.
	/// </summary>
	internal class UnrealEmacsProjectFileGenerator : ProjectFileGenerator
	{
		#region Fields and properties

		/// <summary>
		/// File extension for project files we'll be generating.
		/// </summary>
		public override string ProjectFileExtension
		{
			get { return ".uemacs"; }
		}

		/// <summary>
		/// Absolute path to the Unreal Emacs project directory.
		/// </summary>
		private DirectoryReference EmacsProjectDirectory;

		/// <summary>
		/// Absolute path to the Unreal Engine root directory.
		/// </summary>
		private DirectoryReference EngineRootDirectory;

		/// <summary>
		/// A map of a source file to its compilation command.
		/// </summary>
		private readonly Dictionary<FileReference, string> FileToCompileCommand =
			new Dictionary<FileReference, string>();

		/// <summary>
		/// Whether the project file has to be as small as possible or human readable.
		/// </summary>
		private readonly bool bCompactProjectFile = false;

		/// <summary>
		/// Project format version.
		/// </summary>
		private readonly string ProjectFileFormatVersion = "1.0";

		private readonly FileReference LinuxBuildScript;
		private readonly FileReference MacBuildScript;
		private readonly FileReference WindowsBuildScript;

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new instance of the Unreal Emacs project generator.
		/// </summary>
		/// <param name="InOnlyGameProject">The project file passed in on the command line.</param>
		/// <param name="InArguments">
		/// The command line arguments used to customize default behavior of the generator.
		/// </param>
		public UnrealEmacsProjectFileGenerator(FileReference InOnlyGameProject, CommandLineArguments InArguments)
			: base(InOnlyGameProject)
		{
			// Ignoring command-line arguments at the moment.

			// Store paths to build scripts so that we can write them to the Unreal Emacs project file later
			DirectoryReference BatchFilesDir = DirectoryReference.Combine(UnrealBuildTool.EngineDirectory,
				"Build", "BatchFiles");
			LinuxBuildScript   = FileReference.Combine(BatchFilesDir, "Linux", "Build.sh");
			MacBuildScript     = FileReference.Combine(BatchFilesDir, "Mac", "Build.sh");
			WindowsBuildScript = FileReference.Combine(BatchFilesDir, "Build.bat");
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Generates Unreal Emacs project files for all available (and buildable) engine and game targets.
		/// Does not actually build anything.
		/// </summary>
		/// <param name="PlatformProjectGenerators">The registered platform project generators.</param>
		/// <param name="Arguments">Command-line arguments.</param>
		public override bool GenerateProjectFiles(PlatformProjectGeneratorCollection PlatformProjectGenerators,
			string[] Arguments)
		{
			if (OnlyGameProject == null)
			{
				// Unreal Emacs has no support of multiple projects at the moment.
				Log.TraceWarning(
					"Unreal Emacs can build game projects only at the moment. Skipping project generation.");

				return false;
			}

			return base.GenerateProjectFiles(PlatformProjectGenerators, Arguments);
		}

		/// <summary>
		/// Clean project files.
		/// </summary>
		/// <param name="InMasterProjectDirectory">The master project directory.</param>
		/// <param name="InMasterProjectName">The name of the master project.</param>
		/// <param name="InIntermediateProjectFilesDirectory">The intermediate path of project files.</param>
		public override void CleanProjectFiles(DirectoryReference InMasterProjectDirectory,
			string InMasterProjectName,
			DirectoryReference InIntermediateProjectFilesDirectory)
		{
			// TODO: Do not delete files generated by `ue.el`.
			DirectoryReference.Delete(InMasterProjectDirectory);
		}

		/// <summary>
		/// Allocates a generator-specific master project folder object.
		/// </summary>
		/// <param name="OwnerProjectFileGenerator">Project file generator that owns this object.</param>
		/// <param name="FolderName">Name for this folder.</param>
		/// <returns>The newly allocated project folder object.</returns>
		public override MasterProjectFolder AllocateMasterProjectFolder(
			ProjectFileGenerator OwnerProjectFileGenerator, string FolderName)
		{
			return new UnrealEmacsProjectFolder(OwnerProjectFileGenerator, FolderName);
		}

		/// <summary>
		/// Allocates a generator-specific project file object.
		/// </summary>
		/// <param name="InitFilePath">Path to the project file.</param>
		/// <returns>The newly allocated project file object.</returns>
		protected override ProjectFile AllocateProjectFile(FileReference InitFilePath)
		{
			return new UnrealEmacsProjectFile(InitFilePath);
		}

		protected override bool WriteProjectFiles(PlatformProjectGeneratorCollection PlatformProjectGenerators)
		{
			ProjectFile GameProjectFile = GeneratedProjectFiles.FirstOrDefault(IsGameProjectFile);
			if (GameProjectFile == null)
			{
				Log.TraceError("Cannot find a game project. Aborting.");

				return false;
			}

			EngineRootDirectory   = UnrealBuildTool.RootDirectory;
			EmacsProjectDirectory = DirectoryReference.Combine(GameProjectFile.BaseDir, ".uemacs");
			DirectoryReference.CreateDirectory(EmacsProjectDirectory);

			// Filter out platforms we cannot build
			List<UnrealTargetPlatform> BuildablePlatforms = SupportedPlatforms
				.Where(IsPlatformSupported)
				.ToList();

			WriteEmacsProject(OnlyGameProject, BuildablePlatforms, SupportedConfigurations,
				GameProjectFile.ProjectTargets);

			WriteCompilationDatabase(FileReference.Combine(GameProjectFile.BaseDir, "compile_commands.json"));

			return true;
		}

		/// <summary>
		/// Writes the master project file.
		/// </summary>
		/// <param name="UBTProject">The UnrealBuildTool project.</param>
		/// <param name="PlatformProjectGenerators">The platform project file generators.</param>
		/// <returns><c>true</c> if operation was successful, <c>false</c> otherwise.</returns>
		protected override bool WriteMasterProjectFile(ProjectFile UBTProject,
			PlatformProjectGeneratorCollection PlatformProjectGenerators)
		{
			// There is no master project file for Unreal Emacs
			return true;
		}

		protected override void AddTargetForIntellisense(UEBuildTarget Target)
		{
			base.AddTargetForIntellisense(Target);

			if (Target.ProjectFile != OnlyGameProject)
			{
				return;
			}

			FileReference         ClangCompilerPath  = FindClangCompiler(Target);
			CppCompileEnvironment CompileEnvironment = Target.CreateCompileEnvironmentForProjectFiles();

			foreach (UEBuildBinary Binary in Target.Binaries)
			{
				GenerateCompileCommandsForBinary(Binary, Target, CompileEnvironment, ClangCompilerPath);
			}
		}

		#endregion

		#region Methods

		private bool IsGameProjectFile(ProjectFile ProjectFile)
		{
			return ProjectFile.BaseDir == OnlyGameProject.Directory;
		}

		private static bool IsPlatformSupported(UnrealTargetPlatform Platform)
		{
			if (!UEBuildPlatform.IsPlatformAvailable(Platform))
			{
				return false;
			}

			return UEBuildPlatform
				.GetPlatformGroups(BuildHostPlatform.Current.Platform)
				.Any(Group => IsPlatformSupported(Platform, Group));
		}

		private static bool IsPlatformSupported(UnrealTargetPlatform Platform, UnrealPlatformGroup Group)
		{
			return UEBuildPlatform.IsPlatformInGroup(Platform, Group) &&
			       Group != UnrealPlatformGroup.Desktop;
		}

		private void WriteEmacsProject(FileReference ProjectFile, List<UnrealTargetPlatform> Platforms,
			List<UnrealTargetConfiguration> Configurations, List<ProjectTarget> Targets)
		{
			FileReference ProjectFileLocation = FileReference.Combine(EmacsProjectDirectory, "project.json");

			JsonWriterStyle WriterStyle = bCompactProjectFile ? JsonWriterStyle.Compact : JsonWriterStyle.Readable;
			using (JsonWriter Writer = new JsonWriter(ProjectFileLocation, WriterStyle))
			{
				Writer.WriteObjectStart();

				Writer.WriteValue("Version", ProjectFileFormatVersion);

				WriteEmacsProjectMetadata(Writer, ProjectFile, Platforms, Configurations, Targets);
				WriteEmacsProjectEngineMetadata(Writer, ProjectFile);

				Writer.WriteObjectEnd();
			}
		}

		private void WriteEmacsProjectMetadata(
			JsonWriter Writer,
			FileReference ProjectFile,
			List<UnrealTargetPlatform> Platforms,
			List<UnrealTargetConfiguration> Configurations,
			List<ProjectTarget> Targets)
		{
			string ProjectName = ProjectFile.GetFileNameWithoutAnyExtensions();
			Writer.WriteObjectStart("Project");

			Writer.WriteValue("Name", ProjectName);
			Writer.WriteValue("File", ProjectFile.FullName);

			WriteEmacsProjectTargets(Writer, Platforms, Configurations, Targets);

			Writer.WriteObjectEnd(); // Project
		}

		private void WriteEmacsProjectTargets(JsonWriter Writer, List<UnrealTargetPlatform> Platforms,
			List<UnrealTargetConfiguration> Configurations, List<ProjectTarget> Targets)
		{
			Writer.WriteObjectStart("Targets");

			foreach (UnrealTargetPlatform Platform in Platforms)
			{
				foreach (UnrealTargetConfiguration Configuration in SupportedConfigurations)
				{
					foreach (ProjectTarget Target in Targets)
					{
						UEBuildTarget BuildTarget;

						try
						{
							BuildTarget = CreateBuildTarget(Platform, Configuration, Target);
						}
						catch (BuildException e)
						{
							Log.TraceVerbose(e.Message);
							continue;
						}

						WriteEmacsProjectTarget(Writer, BuildTarget);
					}
				}
			}

			Writer.WriteObjectEnd(); // Project/Targets
		}

		private void WriteEmacsProjectTarget(JsonWriter Writer, UEBuildTarget Target)
		{
			UEBuildBinary Binary = Target.Binaries.First(B => B.Type == UEBuildBinaryType.Executable);

			string TargetName     = Target.TargetName;
			string TargetType     = Target.TargetType.ToString();
			string Configuration  = Target.Configuration.ToString();
			string Platform       = Target.Platform.ToString();
			string TargetFullName = Target.ReceiptFileName.GetFileNameWithoutAnyExtensions();
			string BinaryFile     = Binary.OutputFilePaths.First().FullName;

			Writer.WriteObjectStart(TargetFullName);

			Writer.WriteValue("Name", TargetName);
			Writer.WriteValue("Configuration", Configuration);
			Writer.WriteValue("Platform", Platform);
			Writer.WriteValue("Type", TargetType);
			Writer.WriteValue("Binary", BinaryFile);

			WriteEmacsProjectTargetTasks(Writer, Target, BinaryFile);

			Writer.WriteObjectEnd(); // Project/Targets/$TargetName$
		}

		private void WriteEmacsProjectTargetTasks(JsonWriter Writer, UEBuildTarget Target, string BinaryFile)
		{
			Writer.WriteObjectStart("Tasks");
			
			Writer.WriteValue("Build", GetBuildCommand(Target));
			Writer.WriteValue("Run", GetRunCommand(Target, BinaryFile));
		
			Writer.WriteObjectEnd(); // Project/Targets/$TargetName$/Tasks
		}

		private void WriteEmacsProjectEngineMetadata(JsonWriter Writer, FileReference ProjectFile)
		{
			ProjectDescriptor ProjectDescriptor = ProjectDescriptor.FromFile(ProjectFile);

			Writer.WriteObjectStart("Engine");

			Writer.WriteValue("Version", ProjectDescriptor.EngineAssociation);
			Writer.WriteValue("Root", UnrealBuildTool.RootDirectory.FullName);

			// TODO: Path to the unreal header tool? This could be useful if we want to add new files and generate
			//       "*.generated.h" files automatically.

			Writer.WriteObjectStart("Scripts");
			Writer.WriteValue("Build", GetBuildScriptLocation().FullName);
			Writer.WriteObjectEnd(); // Engine/Scripts

			Writer.WriteObjectEnd(); // Engine
		}

		/// <summary>
		/// Writes Clang compilation database to the file under the given path. 
		/// </summary>
		/// <param name="CompilationDatabasePath">Path to the 'compile_commands.json' file.</param>
		private void WriteCompilationDatabase(FileReference CompilationDatabasePath)
		{
			using (JsonWriter Writer = new JsonWriter(CompilationDatabasePath, JsonWriterStyle.Compact))
			{
				Writer.WriteArrayStart();
				foreach (KeyValuePair<FileReference, string> FileCommandPair in FileToCompileCommand
					.OrderBy(x => x.Key.FullName))
				{
					Writer.WriteObjectStart();
					Writer.WriteValue("file", FileCommandPair.Key.FullName);
					Writer.WriteValue("command", FileCommandPair.Value);
					Writer.WriteValue("directory", EngineRootDirectory.FullName);
					Writer.WriteObjectEnd();
				}

				Writer.WriteArrayEnd();
			}
		}

		/// <summary>
		/// Generates Clang compile commands for the given binary and adds them to
		/// the <see cref="FileToCompileCommand"/> dictionary.
		/// </summary>
		/// <param name="Binary">A binary to generate compile commands for.</param>
		/// <param name="Target">The target's binary.</param>
		/// <param name="GlobalCompileEnvironment">The global compile environment to copy settings from.</param>
		/// <param name="ClangCompilerPath">The path to the clang++ executable.</param>
		private void GenerateCompileCommandsForBinary(UEBuildBinary Binary, UEBuildTarget Target,
			CppCompileEnvironment GlobalCompileEnvironment, FileReference ClangCompilerPath)
		{
			CppCompileEnvironment BinaryCompileEnvironment = Binary.CreateBinaryCompileEnvironment(
				GlobalCompileEnvironment);
			foreach (UEBuildModuleCPP Module in Binary.Modules.OfType<UEBuildModuleCPP>())
			{
				GenerateCompileCommandsForModule(Module, Target.Rules, BinaryCompileEnvironment, ClangCompilerPath);
			}
		}

		/// <summary>
		/// Generates Clang compile commands for the given C++ module and adds them to
		/// the <see cref="FileToCompileCommand"/> dictionary.
		/// </summary>
		/// <param name="Module">The C++ module to generate compile commands for.</param>
		/// <param name="TargetRules">The target rules to configure the module's compile environment.</param>
		/// <param name="BinaryCompileEnvironment">The compile environment to copy settings from.</param>
		/// <param name="ClangCompilerPath">The path to the clang++ executable.</param>
		private void GenerateCompileCommandsForModule(UEBuildModuleCPP Module, ReadOnlyTargetRules TargetRules, 
			CppCompileEnvironment BinaryCompileEnvironment, FileReference ClangCompilerPath)
		{
			CppCompileEnvironment ModuleCompileEnvironment = Module.CreateModuleCompileEnvironment(
				TargetRules, BinaryCompileEnvironment);

			StringBuilder CompileCommand = new StringBuilder($"\"{ClangCompilerPath.FullName}\" ");

			string ForceIncludeFormat  = GetForceIncludeFormatTemplate();
			string SystemIncludeFormat = GetSystemIncludeFormatTemplate();
			string UserIncludeFormat   = GetUserIncludeFormatTemplate();
			string DefinitionFormat    = GetDefinitionFormatTemplate();

			string CppStandard        = GetCppStandardCompilerSwitch(ModuleCompileEnvironment.CppStandard);
			string SystemCompileFlags = GetSystemCompileFlags();

			CompileCommand.AppendFormat(" {0}", CppStandard);
			CompileCommand.AppendFormat(" {0}", SystemCompileFlags);

			foreach (FileItem File in ModuleCompileEnvironment.ForceIncludeFiles)
			{
				CompileCommand.AppendFormat(ForceIncludeFormat, File.FullName);
			}

			foreach (DirectoryReference Path in ModuleCompileEnvironment.UserIncludePaths)
			{
				CompileCommand.AppendFormat(UserIncludeFormat, Path);
			}

			foreach (DirectoryReference Path in ModuleCompileEnvironment.SystemIncludePaths)
			{
				CompileCommand.AppendFormat(SystemIncludeFormat, Path);
			}

			foreach (string Definition in ModuleCompileEnvironment.Definitions)
			{
				CompileCommand.AppendFormat(DefinitionFormat, Definition);
			}

			UEBuildModuleCPP.InputFileCollection InputFiles = Module.FindInputFiles(
				TargetRules.Platform, new Dictionary<DirectoryItem, FileItem[]>());

			List<FileItem> SourceFiles = new List<FileItem>();
			SourceFiles.AddRange(InputFiles.CPPFiles);
			SourceFiles.AddRange(InputFiles.CCFiles);

			foreach (FileItem File in SourceFiles)
			{
				FileToCompileCommand[File.Location] = $"{CompileCommand} \"{File.FullName}\"";
			}
		}

		private string GetSystemCompileFlags()
		{
			UnrealTargetPlatform HostPlatform = BuildHostPlatform.Current.Platform;

			if (HostPlatform == UnrealTargetPlatform.Mac)
			{
				return "-x objective-c++ -stdlib=libc++";
			}

			return "";
		}

		private string GetCppStandardCompilerSwitch(CppStandardVersion Version)
		{
			if (IsNonWindowsHost())
			{
				switch (Version)
				{
					case CppStandardVersion.Cpp14:   return "-std=c++14";
					case CppStandardVersion.Cpp17:   return "-std=c++17";
					case CppStandardVersion.Default: return "-std=c++17";
					case CppStandardVersion.Latest:  return "-std=c++20";
				}
			}
			else
			{
				switch (Version)
				{
					case CppStandardVersion.Cpp14:   return "/std:c++14";
					case CppStandardVersion.Cpp17:   return "/std:c++17";
					case CppStandardVersion.Default: return "/std:c++17";
					case CppStandardVersion.Latest:  return "/std:c++latest";
				}
			}

			return "";
		}

		private string GetForceIncludeFormatTemplate()
		{
			return IsNonWindowsHost() ? " -include \"{0}\"" : " /FI\"{0}\"";
		}
		
		private string GetDefinitionFormatTemplate()
		{
			return IsNonWindowsHost() ? " -D\"{0}\"" : " /D\"{0}\"";
		}
		
		private string GetUserIncludeFormatTemplate()
		{
			return IsNonWindowsHost() ? " -I\"{0}\"" : " /I\"{0}\"";
		}
		
		private string GetSystemIncludeFormatTemplate()
		{
			return IsNonWindowsHost() ? " -I\"{0}\"" : " /I\"{0}\"";
		}

		private bool IsNonWindowsHost()
		{
			UnrealTargetPlatform HostPlatform = BuildHostPlatform.Current.Platform;

			return HostPlatform != UnrealTargetPlatform.Win64 && HostPlatform != UnrealTargetPlatform.Win32;
		}

		private UEBuildTarget CreateBuildTarget(UnrealTargetPlatform Platform, UnrealTargetConfiguration Configuration,
			ProjectTarget Target)
		{
			string DefaultArchitecture = UEBuildPlatform
				.GetBuildPlatform(Platform)
				.GetDefaultArchitecture(Target.UnrealProjectFilePath);

			TargetDescriptor TargetDescriptor = new TargetDescriptor(
				Target.UnrealProjectFilePath,
				Target.Name,
				Platform,
				Configuration,
				DefaultArchitecture,
				null);

			return UEBuildTarget.Create(TargetDescriptor, false, false);
		}

		/// <summary>
		/// Searches for the Clang compiler for the given platform.
		/// </summary>
		/// <param name="Target">The build platform to use to search for the Clang compiler.</param>
		/// <returns>The path to the Clang compiler.</returns>
		private static FileReference FindClangCompiler(UEBuildTarget Target)
		{
			// TODO: Check the case when Clang is not available.
			UnrealTargetPlatform HostPlatform = BuildHostPlatform.Current.Platform;

			if (HostPlatform == UnrealTargetPlatform.Win64)
			{
				VCEnvironment Environment = VCEnvironment.Create(WindowsCompiler.Clang, Target.Platform,
					Target.Rules.WindowsPlatform.Architecture, null,
					Target.Rules.WindowsPlatform.WindowsSdkVersion, null);

				return FileReference.FromString(Environment.CompilerPath.FullName);
			}
			else if (HostPlatform == UnrealTargetPlatform.Linux)
			{
				return FileReference.FromString(LinuxCommon.WhichClang());
			}
			else if (HostPlatform == UnrealTargetPlatform.Mac)
			{
				MacToolChainSettings Settings     = new MacToolChainSettings(false);
				DirectoryReference   ToolchainDir = DirectoryReference.FromString(Settings.ToolchainDir);

				return FileReference.Combine(ToolchainDir, "clang++");
			}
			else
			{
				return FileReference.FromString("clang++");
			}
		}
		
		private FileReference GetBuildScriptLocation()
		{
			UnrealTargetPlatform HostPlatform = BuildHostPlatform.Current.Platform;

			if (HostPlatform == UnrealTargetPlatform.Linux)
			{
				return LinuxBuildScript;
			}
			else if (HostPlatform == UnrealTargetPlatform.Mac)
			{
				return MacBuildScript;
			}
			else if (HostPlatform == UnrealTargetPlatform.Win64 || HostPlatform == UnrealTargetPlatform.Win32)
			{
				return WindowsBuildScript;
			}

			throw new BuildException("Unsupported host platform {0}", HostPlatform);
		}

		private string GetShellScriptExecutor()
		{
			UnrealTargetPlatform HostPlatform = BuildHostPlatform.Current.Platform;
			
			if (HostPlatform == UnrealTargetPlatform.Linux || HostPlatform == UnrealTargetPlatform.Mac)
			{
				return "bash";
			}
			else if (HostPlatform == UnrealTargetPlatform.Win64 || HostPlatform == UnrealTargetPlatform.Win32)
			{
				return "call";
			}
			else
			{
				throw new BuildException("Unsupported host platform {0}", HostPlatform);
			}
		}

		private string GetBuildCommand(UEBuildTarget Target)
		{
			string Shell              = GetShellScriptExecutor();
			string BuildScript        = GetBuildScriptLocation().FullName;
			string ProjectFile        = Target.ProjectFile.FullName;
			string TargetName         = Target.TargetName;
			string Platform           = Target.Platform.ToString();
			string Configuration      = Target.Configuration.ToString();
			string ProjectCommandLine = $"-project=\"{ProjectFile}\"";

			string Command = $"{Shell} \"{BuildScript}\" {TargetName} {Platform} {Configuration} {ProjectCommandLine}";

			return Command;
		}

		private string GetRunCommand(UEBuildTarget Target, string BinaryFile)
		{
			if (Target.TargetType == TargetType.Editor)
			{
				string ProjectFile = Target.ProjectFile.FullName;
				
				return $"\"{BinaryFile}\" \"{ProjectFile}\"";
			}

			return $"\"{BinaryFile}\"";
		}

		#endregion
	}
}