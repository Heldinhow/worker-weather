# Blacksmith.sh — Clone Design

Date: 2026-02-20

Goal
----
Produce a functionally and visually identical rebuild of https://www.blacksmith.sh using our Next.js + React stack so the result is maintainable, testable, and deployable.

Chosen approach
---------------
Rebuild complete in our stack (Next.js, TypeScript, TailwindCSS). This balances fidelity, maintainability, and long-term control. We will reproduce visuals, interactions, and public content. Any backend endpoints or proprietary integrations in the original will be mocked or reimplemented with equivalent behavior.

Architecture (high-level)
------------------------
- Frontend: Next.js (app or pages router), TypeScript, React components. Pages rendered with SSG (next export) where possible; fallback to SSR/ISR for dynamic content.
- Styling: TailwindCSS for rapid, consistent styling. Use utility classes and small design-system tokens for colors/spacing.
- Assets: Store in /public/assets; optimize images to WebP/AVIF and provide responsive srcset.
- Interactions: Use CSS for simple transitions, and Framer Motion for complex animations.
- Forms & APIs: Implement minimal API routes in /pages/api for form submissions or server-side needs. Use local mocks if original external APIs are private.
- Testing: Unit tests with Jest + React Testing Library, E2E smoke tests with Playwright.

Key components
--------------
- Layout: Header, Footer, global styles, metadata
- Hero: Large visual banner with CTA(s)
- FeatureGrid: Cards describing product/features
- Pricing/CTA sections: Recreate spacing, typography, and buttons
- Footer & legal links
- Forms: contact or signup flow (client validation + API route)

Data flow
---------
1. Static content (copy of public pages): stored in file-based pages and/or MD/JSON content files under /content.
2. Client interactions (forms): validate on client, post to /api/contact which returns success/failure JSON.
3. Images/fonts: served from /public/assets; fonts loaded with next/font or @font-face with preloading for LCP.

Error handling
--------------
- Forms: show inline validation, global toast for server errors, retry option.
- API routes: return structured { ok: boolean, error?: string } payloads; status codes 200/400/500 accordingly.
- Build/runtime: log build warnings, fail on critical missing assets.

Testing strategy
----------------
- Unit: Snapshot + behavior tests for Header, Hero, FeatureCard, and Form components.
- Integration: Render key pages and assert important text, buttons, and links exist.
- E2E: Playwright smoke test that loads the home page, verifies hero text, clicks CTA, and submits form (mocked endpoint).

Deliverables
------------
1. Source files in standard Next.js layout (pages/app, components/, public/assets)
2. Tests (jest + playwright) and CI-ready npm scripts
3. README with build & run instructions
4. Design doc and implementation plan (this file + implementation plan file)

Estimates
---------
- Pages & visuals: 1–3 days
- Interactions & forms: +1–2 days
- Testing & polish: +1–2 days
Total: 3–7 days depending on page count and integrations.

Risks & legal
--------------
- Copyright: Reproducing exact copy of a site may violate copyright/trademark. Confirm permission to reproduce protected content (text, images, icons, fonts).
- Third-party integrations that are private will be approximated—loss of fidelity possible.

Next steps
----------
1. Confirm permission to reproduce content or decide to replicate only look & feel.
2. Approve this design. After approval I'll generate a detailed implementation plan and begin work (I have created an implementation plan file alongside this design).
