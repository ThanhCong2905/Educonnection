# Auth design QA

final result: passed

Final verification performed on the running application at http://localhost:5187, using headless Microsoft Edge (Chromium) through Playwright with user authorization.

Reference: SVG exports extracted to `design-reference/auth` from the supplied KLTN archive. Implementation: ASP.NET Core MVC at localhost:5198. Initial comparison: desktop 1284 × 1110; mobile 360/390 × 844 and tablet 768 × 844. Evidence in `tmp/auth-screenshots`.

## First comparison

- P1 resolved: Login input placeholders overlapped leading icons because the shared input selector overrode icon padding. Fixed selector specificity; final captures show distinct icon and input text slots.
- P2 resolved: Login card sat approximately 18 px too high. Removed vertical transform and tightened field spacing; final card is x=402, y=255.4, width=480, height=518.1 versus the reference approximately x=402, y=260, width=480, height=514.
- P2 resolved: Selected Học sinh option wrapped on the 360 px registration form. Final mobile captures show all role labels on one line, with password fields stacked and no overflow.
- P2 resolved: Landing badge omitted the source verification mark. Final capture uses the supplied badge asset, cropped from the source SVG.
- P2 resolved: Initial credential-error implementation added a boxed alert and shifted the form. It now replaces the welcome subtitle with the source-style red error text and outlines both credential fields in red. Verified against the supplied error SVG in a combined comparison image.

## Intentional differences

- Registration password help follows SRS: 15–64 characters and no character-class requirement; the SVG shows an older 8-character rule.
- Forgot/reset/change-password and account overview screens extend the supplied form styling because these states were not included in the archive.
- The separately exported footer is shared across pages; standalone registration and landing references do not include that footer in their frame.
- The responsive mobile layout stacks password fields; no mobile source frame was supplied.

## Initial interaction evidence

Playwright: role selection, password reveal/hide, confirmation matching, incorrect-login response, all images loaded, no horizontal overflow at 360/390/768 px, no page errors or failed page asset responses.

Backend HTTP checks: 20 primary + 12 edge checks passed, including all three registration roles, duplicate email/phone, Admin tampering, CSRF, password changes/reset/reuse rejection, login aliases, account/IP delay, locked account and inactivity expiry.

## Final evidence

- `tmp/auth-screenshots/login-comparison-final.png`, `register-comparison-final.png`, `home-comparison-final.png`, `login-error-comparison-final.png`: combined source and implementation comparisons.
- `tmp/auth-screenshots/verification.json`: final Playwright results; no browser exceptions or failed page assets, no horizontal overflow at 360/390/768 px.
- Additional regression passed: a captured old cookie stays revoked after the account is unlocked again.
- Final build: zero warnings and errors; SQL health response Healthy; generated test accounts remaining: zero.
- All supplied logo/icon assets are local, non-empty, loaded successfully and placed in their intended slots. Fonts are served locally.

## Remaining P3 polish

- Small typography/baseline and native checkbox differences remain between browser rendering and the outlined SVG exports.
- Registration content spacing is slightly taller than the static frame; it preserves the reference card width and accommodates the current SRS helper and accessible form controls.

No remaining P0/P1/P2 findings in the account screens.
