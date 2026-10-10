namespace HappyPaws.Domain.Articles;

/// <summary>
/// Where an article stands with the public. Worked out from its dates every time it is read, never
/// stored, so the status and the public reads cannot disagree.
/// </summary>
public enum ArticleStatus
{
    /// <summary>Never published, or taken off the site. Only staff see it.</summary>
    Draft,

    /// <summary>Has a publication time that has not come yet. Hidden from the public until then.</summary>
    Scheduled,

    /// <summary>On the public site.</summary>
    Published,
}
