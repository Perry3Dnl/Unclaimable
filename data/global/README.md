# Global reserved-name datasets

Global datasets are always active and are not controlled by `Options.Languages`.
They are intended for protected identities and platform-facing names whose meaning is not tied to one localized language pack.

Current global categories:

- `brands` — companies, products, consumer brands and other impersonation-sensitive names;
- `technology` — technology companies, platforms, products and ecosystems;
- `security` — cybersecurity, incident-response, vulnerability and security-research identities;
- `automation` — automated service identities, runners, bots, workflow and orchestration names;
- `legal` — legal, privacy, intellectual-property and regulatory-response identities;
- `commerce` — merchant, seller, storefront, order and transaction-facing identities;
- `community` — official community programs, ambassadors, hubs and member-relations identities;
- `other` — protected platform-facing names that do not fit cleanly into another category.

Each dataset uses schema version `1` and declares `"language": "global"` explicitly.

When adding values, prefer a specific category over `other`, avoid duplicates across existing localized datasets, and keep ordinary personal names claimable whenever possible.
