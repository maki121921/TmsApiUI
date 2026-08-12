TMS API Versioning Policy
1. Breaking Changes

A change is breaking when an existing client may stop working or behave differently without changing its code.

Breaking changes include:

Removing an existing response field.
Renaming an existing response field.
Changing the meaning or type of an existing field.
Changing an HTTP status code that existing clients depend on.
Tightening validation so a request that previously worked is rejected.
Changing the default sort order of an existing endpoint.
Removing or changing an existing endpoint's behavior.

Breaking changes require a new API version.

2. Additive Changes

A change is non-breaking when existing clients can continue working without modification.

Examples include:

Adding a new optional response field.
Adding a new endpoint.
Adding a new optional query parameter.
Adding additional information that existing clients can safely ignore.

Additive changes can be released in the current API version unless they change existing behavior.

3. Sunset Window

When a new API version is released, the previous version will remain available for a minimum of 6 months.

This gives rural training centres and other clients using quarterly maintenance schedules enough time to test and migrate.

The exact shutdown date will be communicated through the API's deprecation headers and other communication channels.

4. Communication

Deprecation information will be communicated from the day the new version is released.

The deprecated version will provide:

Deprecation header.
Sunset header with the planned retirement date.
Link header identifying the successor API version.

The team will also:

Add the change to the CHANGELOG.
Email every team that holds an API key.
Send a calendar invitation for the V1 shutdown date.

Clients are responsible for migrating before the announced sunset date.

5. Skipping Versions

Clients do not have to migrate through every API version.

For example, a client using V1 may migrate directly to V3:

V1 → V3

It does not have to use V2 first:

V1 → V2 → V3

Each API version is an independent contract, and clients should migrate to the version that best meets their requirements.

Versioning Principle

TMS treats API contracts as commitments to existing clients. Breaking changes require a new version, while safe additive changes should remain compatible. Every deprecated version receives a clear sunset date and migration path so clients have enough information and time to move safely.