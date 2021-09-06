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
	internal class UnrealEmacsProjectFileGenerator : ProjectFileGenerator
	{
		#region Fields and properties

		/// <summary>
		/// File extension for project files we'll be generating.
		///
		/// In case of Unreal Emacs, a project 'file' is a directory, which name is '.uemacs'.
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

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new instance of the Unreal Emacs project generator.
		/// </summary>
		/// <param name="InOnlyGameProject">The project file passed in on the command line.</param>
		/// <param name="InArguments">
		/// The command line arguments used to customize default behavior of the generator.
		/// </param>
		public UnrealEmacsProjectFileGenerator(FileReference InOnlyGameProject,
			CommandLineArguments InArguments) : base(InOnlyGameProject)
		{
			// Ignoring command-line arguments at the moment.
		}

		#endregion

		#region Overrides

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

		/// <summary>
		/// Writes the master project file.
		/// </summary>
		/// <param name="UBTProject">The UnrealBuildTool project.</param>
		/// <param name="PlatformProjectGenerators">The platform project file generators.</param>
		/// <returns><c>true</c> if operation was successful, <c>false</c> otherwise.</returns>
		protected override bool WriteMasterProjectFile(ProjectFile UBTProject,
			PlatformProjectGeneratorCollection PlatformProjectGenerators)
		{
			EngineRootDirectory   = UnrealBuildTool.RootDirectory;
			EmacsProjectDirectory = DirectoryReference.Combine(MasterProjectPath, ProjectFileExtension);

			// The directory is unused for now but in the future it will contain project specific data
			// so that Emacs code can use it.
			DirectoryReference.CreateDirectory(EmacsProjectDirectory);
			FileReference CompilationDatabasePath = FileReference.Combine(MasterProjectPath,
				"compile_commands.json");
			WriteCompilationDatabase(CompilationDatabasePath, FileToCompileCommand);

			return true;
		}

		protected override void AddTargetForIntellisense(UEBuildTarget Target)
		{
			base.AddTargetForIntellisense(Target);

			if (Target.ProjectFile != OnlyGameProject)
			{
				return;
			}

			FileReference ClangCompilerPath = FindClangCompiler(Target);
			CppCompileEnvironment GlobalCompileEnvironment =
				Target.CreateCompileEnvironmentForProjectFiles();

			foreach (UEBuildBinary Binary in Target.Binaries)
			{
				GenerateCompileCommandsForBinary(Binary, Target, GlobalCompileEnvironment,
					ClangCompilerPath);
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Writes Clang compilation database to the file under the given path. 
		/// </summary>
		/// <param name="CompilationDatabasePath">Path to the 'compile_commands.json' file.</param>
		/// <param name="FileToCompileCommand">
		/// An enumerable collection of File->CompilationCommand pairs.
		/// </param>
		private void WriteCompilationDatabase(FileReference CompilationDatabasePath,
			IEnumerable<KeyValuePair<FileReference, string>> FileToCompileCommand)
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
		/// <param name="Target">The <see cref="Binary"/>'s target.</param>
		/// <param name="GlobalCompileEnvironment">The global compile environment to copy settings from.</param>
		/// <param name="ClangCompilerPath">The path to the clang++ executable.</param>
		private void GenerateCompileCommandsForBinary(UEBuildBinary Binary, UEBuildTarget Target,
			CppCompileEnvironment GlobalCompileEnvironment, FileReference ClangCompilerPath)
		{
			CppCompileEnvironment BinaryCompileEnvironment = Binary.CreateBinaryCompileEnvironment(
				GlobalCompileEnvironment);
			foreach (UEBuildModuleCPP Module in Binary.Modules.OfType<UEBuildModuleCPP>())
			{
				GenerateCompileCommandsForModule(Module, Target.Rules, BinaryCompileEnvironment,
					ClangCompilerPath);
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
			foreach (FileItem File in ModuleCompileEnvironment.ForceIncludeFiles)
			{
				CompileCommand.AppendFormat(" -include \"{0}\"", File.FullName);
			}

			foreach (string Definition in ModuleCompileEnvironment.Definitions)
			{
				CompileCommand.AppendFormat(" -D\"{0}\"", Definition);
			}

			foreach (DirectoryReference Path in ModuleCompileEnvironment.UserIncludePaths)
			{
				CompileCommand.AppendFormat(" -I\"{0}\"", Path);
			}

			foreach (DirectoryReference Path in ModuleCompileEnvironment.SystemIncludePaths)
			{
				CompileCommand.AppendFormat(" -I\"{0}\"", Path);
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
				MacToolChainSettings Settings = new MacToolChainSettings(false);
				DirectoryReference ToolchainDir = DirectoryReference.FromString(Settings.ToolchainDir);

				return FileReference.Combine(ToolchainDir, "clang++");
			}
			else
			{
				return FileReference.FromString("clang++");
			}
		}

		#endregion
	}
}