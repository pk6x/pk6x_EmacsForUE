using System.Collections.Generic;
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
		public UnrealEmacsProjectFile(FileReference InProjectFilePath) : base(InProjectFilePath)
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
			// We do nothing at the moment, however there are plans to extend this.
			return true;
		}
	}
}