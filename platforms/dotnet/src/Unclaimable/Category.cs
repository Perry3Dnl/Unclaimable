namespace Unclaimable;

/// <summary>
/// Identifies a built-in reserved-name dataset category that can be enabled or disabled per checker.
/// All categories are enabled by default.
/// </summary>
public enum Category
{
    /// <summary>Authentication and sign-in identities.</summary>
    Authentication = 0,
    /// <summary>Automation, workflow, runner, and bot identities.</summary>
    Automation = 1,
    /// <summary>Company, product, and consumer-brand identities.</summary>
    Brands = 2,
    /// <summary>Commerce, merchant, order, and transaction identities.</summary>
    Commerce = 3,
    /// <summary>Communication and messaging identities.</summary>
    Communications = 4,
    /// <summary>Community and member-relations identities.</summary>
    Community = 5,
    /// <summary>Developer tooling and engineering identities.</summary>
    Developer = 6,
    /// <summary>Finance, billing, and payment identities.</summary>
    Finance = 7,
    /// <summary>Governance and administrative-process identities.</summary>
    Governance = 8,
    /// <summary>Identity, verification, and account identities.</summary>
    Identity = 9,
    /// <summary>Infrastructure, hosting, and platform-operation identities.</summary>
    Infrastructure = 10,
    /// <summary>Legal, privacy, and regulatory identities.</summary>
    Legal = 11,
    /// <summary>Moderation and enforcement identities.</summary>
    Moderation = 12,
    /// <summary>Official platform-facing identities.</summary>
    Official = 13,
    /// <summary>Operational and service-management identities.</summary>
    Operations = 14,
    /// <summary>Protected names that do not fit another built-in category.</summary>
    Other = 15,
    /// <summary>Localized profanity entries.</summary>
    Profanity = 16,
    /// <summary>Role and privilege identities.</summary>
    Roles = 17,
    /// <summary>Security and incident-response identities.</summary>
    Security = 18,
    /// <summary>Support, help, trust, and safety identities.</summary>
    Support = 19,
    /// <summary>System and platform-internal identities.</summary>
    System = 20,
    /// <summary>Technology companies, products, platforms, and ecosystems.</summary>
    Technology = 21
}
