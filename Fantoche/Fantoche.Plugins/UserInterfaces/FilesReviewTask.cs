using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.UserInterfaces;

public class FilesReviewParameters
{
    /// <summary>What the user is asked to check, shown above the files.</summary>
    public string Title { get; set; } = "";
    public List<FileRef> Files { get; set; } = [];
}

public class FilesReviewResult
{
    /// <summary>The files the user kept, in the order they were given.</summary>
    public List<FileRef> Files { get; set; } = [];
}

/// <summary>
/// The user previews files, the PDF of an export for instance, and keeps the ones the run carries
/// on with : the page of <c>UserInterfaces/FilesReview</c>.
/// </summary>
public class FilesReviewTask : BaseUserInterfaceTask<FilesReviewParameters, FilesReviewResult>
{
    public override string PageFolder => "UserInterfaces/FilesReview";
}
