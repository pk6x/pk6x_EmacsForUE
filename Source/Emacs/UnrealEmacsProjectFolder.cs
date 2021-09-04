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