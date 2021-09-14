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
using Tools.DotNETCommon;

namespace UnrealBuildTool
{
	/// <summary>
	/// Unreal Emacs project files.
	/// </summary>
	internal class UnrealEmacsProjectFile : ProjectFile
	{
		/// <summary>
		/// Creates a new instance of the Unreal Emacs project generator.
		/// </summary>
		/// <param name="InProjectFilePath">
		/// The path to the project file, relative to the master project file.
		/// </param>
		public UnrealEmacsProjectFile(FileReference InProjectFilePath)
			: base(InProjectFilePath)
		{
		}

		/// <summary>
		/// Writes a project file to disk.
		/// </summary>
		/// <param name="InPlatforms">The platforms to write the project files for.</param>
		/// <param name="InConfigurations">The configurations to add to the project files.</param>
		/// <param name="PlatformProjectGenerators">The registered platform project generators.</param>
		/// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
		public override bool WriteProjectFile(List<UnrealTargetPlatform> InPlatforms,
			List<UnrealTargetConfiguration> InConfigurations,
			PlatformProjectGeneratorCollection PlatformProjectGenerators)
		{
			return true;
		}

		private void WriteBinariesMetadata(JsonWriter Writer, IEnumerable<UEBuildBinary> Binaries)
		{
			Writer.WriteArrayStart("Binaries");
			foreach (UEBuildBinary Binary in Binaries)
			{
				Writer.WriteObjectStart();

				Writer.WriteStringArrayField("Files", Binary.OutputFilePaths.Select(FP => FP.FullName));
				Writer.WriteValue("Type", Binary.Type.ToString());

				// TODO: Write modules in the future?

				Writer.WriteObjectEnd();
			}

			Writer.WriteArrayEnd();
		}
	}
}