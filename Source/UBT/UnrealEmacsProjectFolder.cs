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

namespace UnrealBuildTool
{
	/// <summary>
	/// Represents a folder within the master project.
	/// </summary>
	internal class UnrealEmacsProjectFolder : MasterProjectFolder
	{
		/// <summary>
		/// Creates a new instance of the Unreal Emacs project folder object.
		/// </summary>
		/// <param name="InitOwnerProjectFileGenerator">Project file generator that owns this object.</param>
		/// <param name="InitFolderName">Name for this folder.</param>
		public UnrealEmacsProjectFolder(ProjectFileGenerator InitOwnerProjectFileGenerator, string InitFolderName) :
			base(InitOwnerProjectFileGenerator, InitFolderName)
		{
		}
	}
}
