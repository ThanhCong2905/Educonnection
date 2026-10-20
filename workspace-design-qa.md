# Workspace design QA

final result: passed

## Scope and visual target

Four page groups for Học sinh, Phụ huynh and Gia sư: home, search, profile detail and learning/teaching needs. Visual direction comes from the supplied EduConnect SVG search/profile exports; role navigation and homepage behavior follow the user's subsequent instructions.

The implementation intentionally uses a sidebar with the avatar at its bottom instead of the source top navigation. Colors, rounded cards, pink actions, typography and profile tabs retain the source direction. This is an adapted application, not a pixel-identical reproduction of the static mockups.

## Evidence

- Screenshots captured through Playwright/Chromium at desktop 1440 × 1000 and responsive 360/768 × 844, in `design-reference/workspace-qa`.
- `tmp/workspace-screenshots/search-reference-comparison.png` combines the source search SVG render and the implementation screenshot for visual comparison. Individual screenshots were also inspected at their native width.
- Synthetic users and listings existed only in an isolated SQL UI-test database. The development database contains no generated profile/listing data.

## Findings and fixes

1. Tutor own-profile sidebar originally presented learner connection actions. Replaced with Edit profile / Review status and verified in the final tutor screenshots.
2. Comparison controls could appear in profile-tab cards and carry a selected tray outside search. Restricted selection controls and tray to the search page; the final browser test asserts zero comparison inputs and a hidden tray on profile detail.
3. Tutor home originally used the learner greeting. Updated the heading and role-specific explanation for tutors.
4. Search originally defaulted its unit label to per-session while the unfiltered query included other units. Added All units and explicit unit validation whenever a maximum fee is provided.

## Functional validation

- 12 xUnit checks exercise Service and real SQL in automatically created/disposed test databases: ownership, account/profile status, forged approval, draft/public state, transactions and rollback, stale RowVersion, and review reset after editing.
- 15 Playwright checks pass: three roles, parent learner fields, approval gate, tutor profile submission, publishing needs, home fallback, grade/fee filters, maximum-three comparison, profile tabs, private learner data exclusion, forbidden role access, mobile/tablet layout and no page JavaScript errors.
- Build passes with zero warnings and errors.

## Deliberate scope limits

- Compatibility percent is not fabricated; the homepage has an empty/calculation-pending state and a component slot for future validated scores.
- Connections, classes, messaging, notifications, admin approval and report workflow are subsequent modules; controls identify them as coming soon.
- No public ratings, unverified academic statistics, arbitrary fee ranges or a default 90-minute session are carried over from the reference.
- Avatars use initials when no uploaded image is available. There are no placeholder stock tutor accounts in the application data.
- No full accessibility-conformance claim: keyboard paths, ARIA relationships and assistive-technology testing should be expanded alongside the remaining modules.

No remaining P0/P1/P2 visual blockers for the four delivered page groups.
