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

#include "EmacsSourceCodeAccessor.h"

#include "GenericPlatform/GenericPlatformMisc.h"
#include "Misc/CString.h"
#include "Misc/Paths.h"

#define LOCTEXT_NAMESPACE "EmacsSourceCodeAccessor"

DEFINE_LOG_CATEGORY_STATIC(LogEmacs, Log, All);

// How the plug-in searches for Emacs executables
// ----------------------------------------------
// 1. If environment variable UNREAL_EMACS_EMACSDIR is set, it uses its value as a directory where Emacs client and
//    Emacs executable could be found.
// 2. Otherwise it uses hard-coded default values per platform. Refer to FindEmacsDirectory function for details.
// 3. For GNU/Linux and macOS it then looks for "emacsclient" and "emacs" programs in that folder. For Windows it looks
//    for "emacsclientw" and "runemacs".

/**
 * Not necessary to call unless you know you're changing the state of any installed compilers.
 * If that's the case, you should call this before checking the state the installed state of the
 * compilers.
 */
void FEmacsSourceCodeAccessor::RefreshAvailability()
{
	FString EmacsDirectory = FindEmacsDirectory();
	FPaths::NormalizeDirectoryName(EmacsDirectory);
	if (EmacsDirectory.IsEmpty() || !FPaths::DirectoryExists(EmacsDirectory)) {
		bHasEmacsInstalled  = false;
		EmacsLocation       = TEXT("");
		EmacsClientLocation = TEXT("");
		return;
	}

#if PLATFORM_LINUX || PLATFORM_MAC
	EmacsLocation       = FPaths::Combine(EmacsDirectory, TEXT("emacs"));
	EmacsClientLocation = FPaths::Combine(EmacsDirectory, TEXT("emacsclient"));
#elif PLATFORM_WINDOWS
	EmacsLocation       = FPaths::Combine(EmacsDirectory, TEXT("runemacs.exe"));
	EmacsClientLocation = FPaths::Combine(EmacsDirectory, TEXT("emacsclientw.exe"));

	EmacsLocation.ReplaceInline(TEXT("/"), TEXT("\\"));
	EmacsClientLocation.ReplaceInline(TEXT("/"), TEXT("\\"));
#endif
	bHasEmacsInstalled = FPaths::FileExists(EmacsLocation) && FPaths::FileExists(EmacsClientLocation);
}

/**
 * Check if we can currently access source code.
 * @return true if source code can be accessed.
 */
bool FEmacsSourceCodeAccessor::CanAccessSourceCode() const
{
	return bHasEmacsInstalled;
}

/**
 * Get the name of this source code accessor - used as a unique identifier.
 * @return the name of this accessor.
 */
FName FEmacsSourceCodeAccessor::GetFName() const
{
	// This is the ID stored in the Config/DefaultEditorSettings.ini
	// when a user selects a source code accessor in the "Source Code"
	// config section:
	//
	// [/Script/SourceCodeAccess.SourceCodeAccessSettings]
	// PreferredAccessor=UnrealEmacs
	//
	// UBT uses this ID to find which project generator to use.
	// The editor invokes UBT with this ID when a user selects
	// "Refresh XXX Project".
	return FName("UnrealEmacs");
}

/**
 * Get the name text for this source code accessor.
 * @return the name text of this accessor.
 */
FText FEmacsSourceCodeAccessor::GetNameText() const
{
	// This is the name shown in the dropdown list of source code editors in the Editor.
	return LOCTEXT("EmacsDisplayName", "Emacs");
}

/**
 * Get the description text for this source code accessor.
 * @return the description text of this accessor.
 */
FText FEmacsSourceCodeAccessor::GetDescriptionText() const
{
	return LOCTEXT("EmacsDisplayDesc", "Open source files in Emacs");
}

/**
 * Open the current code solution for editing.
 * @return true if successful.
 */
bool FEmacsSourceCodeAccessor::OpenSolution()
{
	if (!bHasEmacsInstalled) {
		return false;
	}

	const FString ProjectFileLocation = FPaths::ConvertRelativePathToFull(FPaths::GetProjectFilePath());

	return OpenSolutionAtPath(ProjectFileLocation);
}

/**
 * Open the code solution at a specific path for editing.
 * @param InSolutionPath Path to project directory.
 * @return true if successful
 */
bool FEmacsSourceCodeAccessor::OpenSolutionAtPath(const FString &InSolutionPath)
{
	// TODO: Make sure InSolutionPath is a correct path to a uproject file.
	FProcHandle EmacsProcess = RunEmacs(*ShellQuoteArgument(InSolutionPath));
	if (!EmacsProcess.IsValid()) {
		UE_LOG(LogEmacs, Warning, TEXT("Failed to open solution '%s'"), *InSolutionPath);

		return false;
	}

	return true;
}

/**
 * Determine if the source code solution for the given accessor already exists.
 * @return true if solution files exist.
 */
bool FEmacsSourceCodeAccessor::DoesSolutionExist() const
{
	const FString ProjectFileLocation = FPaths::ConvertRelativePathToFull(FPaths::GetProjectFilePath());

	return FPaths::FileExists(ProjectFileLocation);
}

/**
 * Opens a file in the correct running instance of this code accessor at a line and optionally to a column.
 * @param	FullPath	Full path to the file to open.
 * @param	LineNumber	Line number to open the file at.
 * @param	ColumnNumber	Column number to open the file at.
 * @return true if successful.
 */
bool FEmacsSourceCodeAccessor::OpenFileAtLine(const FString &FullPath, int32 LineNumber, int32 ColumnNumber)
{
	if (!bHasEmacsInstalled) {
		return false;
	}

	FString Arguments = FString::Printf(TEXT("+%d:%d %s"), LineNumber, ColumnNumber, *ShellQuoteArgument(FullPath));
	FProcHandle EmacsProcess = RunEmacs(Arguments);

	if (!EmacsProcess.IsValid()) {
		UE_LOG(LogEmacs, Warning, TEXT("Failed to open file '%s'"), *FullPath);
		FPlatformProcess::CloseProc(EmacsProcess);

		return false;
	}

	return true;
}

/**
 * Opens a group of source files.
 * @param AbsoluteSourcePaths Array of paths to files to open.
 */
bool FEmacsSourceCodeAccessor::OpenSourceFiles(const TArray<FString> &AbsoluteSourcePaths)
{
	if (!bHasEmacsInstalled) {
		return false;
	}

	// Shell-quote all source paths
	FString QuotedSourceLocations;
	for (const auto &SourcePath: AbsoluteSourcePaths) {
		QuotedSourceLocations.Append(ShellQuoteArgument(SourcePath));
		QuotedSourceLocations.Append(TEXT(" "));
	}
	QuotedSourceLocations.TrimStartAndEndInline();

	FProcHandle EmacsProcess = RunEmacs(QuotedSourceLocations);
	if (!EmacsProcess.IsValid()) {
		UE_LOG(LogEmacs, Warning, TEXT("Failed to open source files %s"), *QuotedSourceLocations);
		FPlatformProcess::CloseProc(EmacsProcess);

		return false;
	}

	return true;
}

/**
 * Add a group of source files to the current solution/project/workspace
 * @param	AbsoluteSourcePaths		Array of paths to files to open
 * @param	AvailableModules		Array of known module locations (.Build.cs files) - you can get this
 * from calling FSourceCodeNavigation::GetSourceFileDatabase().GetModuleNames() if in the editor
 * @return true if the files could be added, false otherwise
 */
bool FEmacsSourceCodeAccessor::AddSourceFiles(
	const TArray<FString> &AbsoluteSourcePaths,
	const TArray<FString> &AvailableModules)
{
	return true;
}

/**
 * Saves all open code documents if they need to be saved.
 * Will block if there is any read-only files open that need to be saved.
 * @return true if successful
 */
bool FEmacsSourceCodeAccessor::SaveAllOpenDocuments() const
{
	// TODO: Not sure when this is called.
	bool bStatus = false;
	/*
	        // The command returns:
	        // - "[$PROJECT_NAME] Saved $N buffers"
	        // - "[$PROJECT_NAME] No buffers need saving"
	        const FString SaveAllProjectBuffersCommand = TEXT("(projectile-save-project-buffers)");

	        UE_LOG(LogEmacs, Warning, TEXT("Asked to save all open documents."));

	        FString Response;
	        bStatus = EvalEmacsCommand(SaveAllProjectBuffersCommand, Response);

	        UE_LOG(LogEmacs, Warning, TEXT("Sent command to Emacs. The status is: %d. Response is '%s'"), bStatus,
	   *Response);
	*/

	return bStatus;
}

/**
 * Tick this source code accessor
 * @param DeltaTime Delta time (in seconds) since the last call to Tick
 */
void FEmacsSourceCodeAccessor::Tick(const float DeltaTime)
{
}

FString FEmacsSourceCodeAccessor::FindEmacsDirectory() const
{
	FString UserDefinedEmacsLocation = FPlatformMisc::GetEnvironmentVariable(TEXT("UNREAL_EMACS_EMACSDIR"));
	if (!UserDefinedEmacsLocation.IsEmpty()) {
		return UserDefinedEmacsLocation;
	}

#if PLATFORM_MAC || PLATFORM_LINUX
	return TEXT("/usr/local/bin");
#elif PLATFORM_WINDOWS
	return TEXT("C:/Program Files/Emacs/x86_64/bin");
#else
	return TEXT("");
#endif
}

FProcHandle FEmacsSourceCodeAccessor::RunEmacs(const FString &Arguments) const
{
#if PLATFORM_WINDOWS
	// On Windows emacsclient(-w) fails if path to the alternate editor has spaces even if the path is quoted.
	FString FinalArguments = TEXT("-q -n --alternate-editor= ");
#else
	FString FinalArguments = FString::Printf(TEXT("-q -n -a %s "), *ShellQuoteArgument(EmacsLocation));
#endif
	FinalArguments.Append(Arguments);
	FinalArguments.TrimStartAndEndInline();

	// UE_LOG(LogEmacs, Warning, TEXT("Running Emacs with args '%s'"), *FinalArguments);

	return FPlatformProcess::CreateProc(
		*EmacsClientLocation,
		*FinalArguments,
		true,
		false,
		false,
		nullptr,
		0,
		nullptr,
		nullptr);
}

bool FEmacsSourceCodeAccessor::EvalEmacsCommand(const FString &Lisp, FString &Response) const
{
	FString FinalArguments = TEXT("--no-wait --eval ");
	FinalArguments.Append(ShellQuoteArgument(Lisp));
	FinalArguments.TrimStartAndEndInline();

	FString StdOut;
	FString StdErr;
	bool bStatus = FPlatformProcess::ExecProcess(*EmacsClientLocation, *FinalArguments, nullptr, &StdOut, &StdErr);

	StdOut.TrimStartAndEndInline();
	StdErr.TrimStartAndEndInline();

	// UE_LOG(LogEmacs, Warning, TEXT("Emacs command '%s' result:\n'%s'\n'%s'"), *Lisp, *StdOut, *StdErr);
	Response = StdOut;

	return bStatus;
}

#undef LOCTEXT_NAMESPACE
