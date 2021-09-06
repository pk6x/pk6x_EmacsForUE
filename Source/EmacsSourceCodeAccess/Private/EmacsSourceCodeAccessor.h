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

#pragma once

#include "CoreMinimal.h"
#include "ISourceCodeAccessor.h"

class FEmacsSourceCodeAccessor: public ISourceCodeAccessor
{
public:
	/**
	 * Not necessary to call unless you know you're changing the state of any installed compilers.
	 * If that's the case, you should call this before checking the state the installed state of the
	 * compilers.
	 */
	virtual void RefreshAvailability() override;

	/**
	 * Check if we can currently access source code
	 * @return true if source code can be accessed
	 */
	virtual bool CanAccessSourceCode() const override;

	/**
	 * Get the name of this source code accessor - used as a unique identifier
	 * @return the name of this accessor
	 */
	virtual FName GetFName() const override;

	/**
	 * Get the name text for this source code accessor
	 * @return the name text of this accessor
	 */
	virtual FText GetNameText() const override;

	/**
	 * Get the description text for this source code accessor
	 * @return the description text of this accessor
	 */
	virtual FText GetDescriptionText() const override;

	/**
	 * Open the current code solution for editing
	 * @return true if successful
	 */
	virtual bool OpenSolution() override;

	/**
	 * Open the code solution at a specific path for editing
	 * @param InSolutionPath	Path to project directory
	 * @return true if successful
	 */
	virtual bool OpenSolutionAtPath(const FString &InSolutionPath) override;

	/**
	 * Determine if the source code solution for the given accessor already exists
	 * @return true if solution files exist
	 */
	virtual bool DoesSolutionExist() const override;

	/**
	 * Opens a file in the correct running instance of this code accessor at a line and optionally to a column.
	 * @param	FullPath	Full path to the file to open
	 * @param	LineNumber	Line number to open the file at
	 * @param	ColumnNumber	Column number to open the file at
	 * @return true if successful
	 */
	virtual bool OpenFileAtLine(const FString &FullPath, int32 LineNumber, int32 ColumnNumber = 0) override;

	/**
	 * Opens a group of source files.
	 * @param	AbsoluteSourcePaths		Array of paths to files to open
	 */
	virtual bool OpenSourceFiles(const TArray<FString> &AbsoluteSourcePaths) override;

	/**
	 * Add a group of source files to the current solution/project/workspace
	 * @param	AbsoluteSourcePaths		Array of paths to files to open
	 * @param	AvailableModules		Array of known module locations (.Build.cs files) - you can get
	 * this from calling FSourceCodeNavigation::GetSourceFileDatabase().GetModuleNames() if in the editor
	 * @return true if the files could be added, false otherwise
	 */
	virtual bool
	AddSourceFiles(const TArray<FString> &AbsoluteSourcePaths, const TArray<FString> &AvailableModules) override;

	/**
	 * Saves all open code documents if they need to be saved.
	 * Will block if there is any read-only files open that need to be saved.
	 * @return true if successful
	 */
	virtual bool SaveAllOpenDocuments() const override;

	/**
	 * Tick this source code accessor
	 * @param DeltaTime Delta time (in seconds) since the last call to Tick
	 */
	virtual void Tick(const float DeltaTime) override;

private:
	FString     FindEmacsDirectory() const;
	FProcHandle RunEmacs(const FString &Arguments) const;
	bool        EvalEmacsCommand(const FString &Lisp, FString &Response) const;

	FORCEINLINE FString ShellQuoteArgument(const FString &Argument) const
	{
		return FString::Printf(TEXT("\"%s\""), *Argument);
	}

	bool    bHasEmacsInstalled = false;
	FString EmacsLocation;
	FString EmacsClientLocation;
};
